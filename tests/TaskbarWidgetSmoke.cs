using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class TaskbarWidgetSmoke
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string kind, string title);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr window, uint key, byte alpha, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr window, int index, int value);
    private sealed class GhostWindow : NativeWindow, IDisposable
    {
        internal GhostWindow(Rectangle screen)
        {
            try
            {
                CreateHandle(new CreateParams {
                    Caption = "CodexUsage transparent fullscreen smoke test",
                    Style = unchecked((int)0x80000000) | 0x01000000,
                    ExStyle = 0x08000000 | 0x00080000 | 0x00000080 | 0x00000020,
                    X = screen.X, Y = screen.Y, Width = screen.Width, Height = screen.Height
                });
                Check(SetLayeredWindowAttributes(Handle, 0, 0, 2), "Could not make the test window fully transparent");
            }
            catch { DestroyHandle(); throw; }
        }
        internal void Place(Rectangle bounds)
        {
            Check(SetWindowPos(Handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0070), "Could not place ghost window");
            Check(GetForegroundWindow() != Handle, "Ghost window took focus");
        }
        internal void ClearMaximizedFlag()
        { SetWindowLong(Handle, -16, GetWindowLong(Handle, -16) & ~0x01000000); }
        public void Dispose() { DestroyHandle(); }
    }

    [STAThread]
    public static int Main()
    {
        IntPtr widget = FindWindow(null, "CodexUsage");
        if (widget == IntPtr.Zero || !IsWindowVisible(widget))
        {
            Console.WriteLine("SKIP: CodexUsage widget is not currently visible; leave fullscreen before running this smoke check.");
            return 2;
        }
        try
        {
            Rectangle screen = Screen.FromHandle(widget).Bounds;
            Console.WriteLine("PASS: Live widget starts visible on the taskbar");
            using (var ghost = new GhostWindow(screen))
            {
                ghost.Place(screen);
                Check(IsZoomed(ghost.Handle), "Fixture must retain WS_MAXIMIZE");
                WaitForVisibility(widget, false);
                Console.WriteLine("PASS: Live widget hides for fullscreen with WS_MAXIMIZE");
                ghost.Place(new Rectangle(screen.Left + 100, screen.Top + 100, screen.Width / 2, screen.Height / 2));
                WaitForVisibility(widget, true);
                Console.WriteLine("PASS: Live widget restores after leaving fullscreen");
                ghost.ClearMaximizedFlag();
                ghost.Place(screen);
                Check(!IsZoomed(ghost.Handle), "Fixture must clear WS_MAXIMIZE");
                WaitForVisibility(widget, false);
                Console.WriteLine("PASS: Live widget hides for borderless fullscreen without WS_MAXIMIZE");
            }
            WaitForVisibility(widget, true);
            Console.WriteLine("PASS: Live widget restores after fullscreen window closes");
            return 0;
        }
        catch (Exception exception) { Console.WriteLine("FAIL: " + exception.Message); return 1; }
    }

    private static void WaitForVisibility(IntPtr widget, bool expected)
    {
        Stopwatch deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 5000)
        {
            Application.DoEvents();
            if (IsWindowVisible(widget) == expected) return;
            Thread.Sleep(25);
        }
        throw new InvalidOperationException("Timed out waiting for live widget visibility: " + expected);
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
