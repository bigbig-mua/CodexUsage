using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CodexQuotaLite;

internal static class TaskbarTests
{
    private static int testsRun;
    private static int failures;

    [STAThread]
    public static int Main()
    {
        Run("Borderless video and image fullscreen hide the widget", BorderlessFullscreen);
        Run("Fullscreen retains the maximized flag", MaximizedFullscreen);
        Run("Regular maximized windows leave the taskbar available", RegularMaximized);
        Run("Auto-hide maximization keeps its title bar or edge gap", AutoHideMaximized);
        Run("Fullscreen on another monitor does not hide this widget", OtherMonitor);
        Run("Negative monitor coordinates and spanning fullscreen work", NegativeAndSpanningMonitors);
        Run("Leaving fullscreen restores the visibility decision", LeaveFullscreen);
        Run("Partial and empty rectangles are not fullscreen", PartialAndEmpty);
        Run("Native borderless window is detected without activation", NativeBorderless);
        Run("Native WS_MAXIMIZE fullscreen is detected", NativeMaximizedFullscreen);
        Run("Native captioned maximized window is not fullscreen", NativeCaptionedMaximized);
        Run("Hidden native windows are ignored", NativeHidden);
        Console.WriteLine("RESULT: {0} passed, {1} failed", testsRun - failures, failures);
        return failures == 0 ? 0 : 1;
    }

    private static readonly Rectangle MainScreen = new Rectangle(0, 0, 1920, 1080);

    private static void BorderlessFullscreen()
    {
        Check(TaskbarPlacement.CoversScreen(MainScreen, MainScreen, MainScreen, false), "Borderless fullscreen was missed");
    }

    private static void MaximizedFullscreen()
    {
        Check(TaskbarPlacement.CoversScreen(Rectangle.Inflate(MainScreen, 8, 8), MainScreen, MainScreen, true),
            "A retained maximized flag must not exclude fullscreen media");
    }

    private static void RegularMaximized()
    {
        Rectangle workArea = new Rectangle(0, 0, 1920, 1040);
        Check(!TaskbarPlacement.CoversScreen(Rectangle.Inflate(workArea, 8, 8), new Rectangle(0, 31, 1920, 1009), MainScreen, true),
            "Ordinary maximization must leave the taskbar widget available");
    }

    private static void AutoHideMaximized()
    {
        Rectangle outer = Rectangle.Inflate(MainScreen, 8, 8);
        Check(!TaskbarPlacement.CoversScreen(outer, new Rectangle(0, 31, 1920, 1049), MainScreen, true), "Title bar was ignored");
        Check(!TaskbarPlacement.CoversScreen(outer, new Rectangle(0, 0, 1920, 1078), MainScreen, true), "Auto-hide activation gap was ignored");
    }

    private static void OtherMonitor()
    {
        Rectangle other = new Rectangle(1920, 0, 2560, 1440);
        Check(!TaskbarPlacement.CoversScreen(other, other, MainScreen, false), "Other-monitor fullscreen hid this taskbar");
        Check(!TaskbarPlacement.CoversScreen(other, other, MainScreen, true), "Maximized fullscreen on another monitor hid this taskbar");
    }

    private static void NegativeAndSpanningMonitors()
    {
        Rectangle leftScreen = new Rectangle(-2560, -160, 2560, 1440);
        Check(TaskbarPlacement.CoversScreen(leftScreen, leftScreen, leftScreen, true), "Negative screen coordinates were rejected");
        Rectangle spanning = Rectangle.FromLTRB(-2560, -160, 1920, 1440);
        Check(TaskbarPlacement.CoversScreen(spanning, spanning, MainScreen, true), "Spanning fullscreen was rejected");
    }

    private static void LeaveFullscreen()
    {
        Check(TaskbarPlacement.CoversScreen(MainScreen, MainScreen, MainScreen, true), "Fullscreen entry was missed");
        Rectangle restored = new Rectangle(100, 100, 1000, 700);
        Check(!TaskbarPlacement.CoversScreen(restored, restored, MainScreen, false), "Fullscreen exit still hides the widget");
    }

    private static void PartialAndEmpty()
    {
        Rectangle partial = new Rectangle(0, 0, 1920, 1079);
        Check(!TaskbarPlacement.CoversScreen(partial, partial, MainScreen, false), "A one-pixel gap was treated as fullscreen");
        Check(!TaskbarPlacement.CoversScreen(Rectangle.Empty, Rectangle.Empty, MainScreen, false), "Empty window was accepted");
        Check(!TaskbarPlacement.CoversScreen(MainScreen, MainScreen, Rectangle.Empty, false), "Empty screen was accepted");
    }

    private static void NativeBorderless()
    {
        using (var window = new OffscreenWindow(unchecked((int)0x80000000)))
            Check(TaskbarPlacement.WindowIsFullscreen(window.Handle, window.TestScreen), "Native borderless window was missed");
    }

    private static void NativeMaximizedFullscreen()
    {
        using (var window = new OffscreenWindow(unchecked((int)0x80000000) | 0x01000000))
        {
            Check(IsZoomed(window.Handle), "The native fixture must retain WS_MAXIMIZE");
            Check(TaskbarPlacement.WindowIsFullscreen(window.Handle, window.TestScreen), "Native maximized fullscreen was missed");
        }
    }

    private static void NativeCaptionedMaximized()
    {
        using (var window = new OffscreenWindow(0x00CF0000 | 0x01000000))
        {
            Check(IsZoomed(window.Handle), "The native fixture must be maximized");
            Check(!TaskbarPlacement.WindowIsFullscreen(window.Handle, window.TestScreen), "Captioned native window was misclassified");
        }
    }

    private static void NativeHidden()
    {
        using (var window = new OffscreenWindow(unchecked((int)0x80000000)))
        {
            Check(TaskbarPlacement.WindowIsFullscreen(window.Handle, window.TestScreen), "Visible fixture was not detected");
            ShowWindow(window.Handle, 0);
            Check(!TaskbarPlacement.WindowIsFullscreen(window.Handle, window.TestScreen), "Hidden window was still detected");
        }
    }

    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

    // Native fixtures stay outside the virtual desktop and never take focus.
    private sealed class OffscreenWindow : NativeWindow, IDisposable
    {
        internal readonly Rectangle TestScreen;
        internal OffscreenWindow(int style)
        {
            Rectangle desktop = SystemInformation.VirtualScreen;
            TestScreen = new Rectangle(desktop.Left - 2048, desktop.Top - 2048, 640, 480);
            IntPtr foreground = GetForegroundWindow();
            try
            {
                CreateHandle(new CreateParams {
                    Caption = "CodexUsage offscreen taskbar test",
                    Style = style, ExStyle = 0x00000080 | 0x08000000,
                    X = TestScreen.X, Y = TestScreen.Y, Width = TestScreen.Width, Height = TestScreen.Height
                });
                Check(SetWindowPos(Handle, IntPtr.Zero, TestScreen.X, TestScreen.Y, TestScreen.Width, TestScreen.Height, 0x0074),
                    "Could not show offscreen native fixture");
                Check(GetForegroundWindow() == foreground, "Native test fixture took focus");
            }
            catch { DestroyHandle(); throw; }
        }
        public void Dispose() { DestroyHandle(); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Run(string name, Action test)
    {
        testsRun++;
        try { test(); Console.WriteLine("PASS: " + name); }
        catch (Exception exception) { failures++; Console.WriteLine("FAIL: {0} -- {1}", name, exception.Message); }
    }
}
