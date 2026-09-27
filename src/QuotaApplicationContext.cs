using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    public sealed class QuotaApplicationContext : ApplicationContext
    {
        private readonly IQuotaSource source;
        private readonly SettingsStore store;
        private readonly ResetFeed resetFeed;
        private readonly Action<Rectangle, Rectangle, Rectangle> placementObserver;
        private readonly AppSettings settings;
        private readonly WidgetForm widget;
        private readonly DetailsForm details;
        private readonly NotifyIcon tray;
        private Icon trayIcon;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem visibilityItem;
        private readonly ToolStripMenuItem refreshItem;
        private readonly System.Windows.Forms.Timer refreshTimer;
        private readonly System.Windows.Forms.Timer clockTimer;
        private readonly System.Windows.Forms.Timer taskbarTimer;
        private readonly System.Windows.Forms.Timer hoverTimer;
        private readonly HoverDismissState hoverDismiss = new HoverDismissState();
        private readonly TaskbarVisibilityState taskbarVisibility = new TaskbarVisibilityState();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Dictionary<string, DateTimeOffset> resetRefreshAttempts = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        private QuotaSnapshot snapshot;
        private bool busy;
        private bool stopping;
        private bool disposed;
        private string error;
        private string settingsError;
        private bool widgetEnabled = true;
        private Rectangle currentTaskbar;
        private int appliedTaskbarScale;
        private bool initialized;
        private ForegroundMonitor foregroundMonitor;
        private bool taskbarUpdateQueued;

        public QuotaApplicationContext(IQuotaSource quotaSource, SettingsStore settingsStore, AppSettings appSettings, Action<Rectangle, Rectangle, Rectangle> observePlacement = null)
        {
            if (quotaSource == null) throw new ArgumentNullException("quotaSource");
            if (settingsStore == null) throw new ArgumentNullException("settingsStore");
            source = quotaSource;
            store = settingsStore;
            placementObserver = observePlacement;
            settings = appSettings ?? new AppSettings();
            settings.ScalePercent = 100;
            UiText.Language = settings.Language;
            Theme.Apply(ResolveDarkTheme());
            widget = new WidgetForm();
            details = new DetailsForm(settings);
            resetFeed = new ResetFeed(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsage", "reset-announcement.json"));
            details.SetResetFeed(resetFeed);
            MainForm = widget;
            widget.ApplyScale(settings.ScalePercent);
            widget.TopMost = true;
            details.TopMost = false;
            RestorePosition();
            widget.DetailRequested += delegate { ShowDetails(); };
            details.RefreshRequested += async delegate { await RefreshAsync(); };
            details.SettingsChanged += delegate { ApplySettings(); };
            widget.Shown += async delegate { await RefreshAsync(); };
            widget.FormClosing += delegate { Stop(); };
            menu = new ContextMenuStrip();
            menu.BackColor = Theme.Card; menu.ForeColor = Theme.Text;
            menu.Renderer = new ToolStripProfessionalRenderer(new MenuColors());
            visibilityItem = new ToolStripMenuItem("隐藏浮条", null, delegate { ToggleWidget(); });
            refreshItem = new ToolStripMenuItem("立即刷新", null, async delegate { await RefreshAsync(); });
            menu.Items.Add(visibilityItem);
            menu.Items.Add(new ToolStripMenuItem("查看用量详情", null, delegate { ShowDetails(); }));
            menu.Items.Add(refreshItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("退出", null, delegate { ExitThread(); }));
            menu.Opening += delegate { visibilityItem.Text = widgetEnabled ? UiText.T("隐藏额度条", "Hide widget") : UiText.T("显示额度条", "Show widget"); refreshItem.Enabled = !busy; };
            widget.ContextMenuStrip = menu;
            menu.Closed += delegate { QueueTaskbarUpdate(); };
            details.Deactivate += delegate { QueueTaskbarUpdate(); };
            trayIcon = Theme.CreateIcon();
            tray = new NotifyIcon();
            tray.Icon = trayIcon;
            tray.Text = "Codex 额度小窗";
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { widgetEnabled = true; RestorePosition(); widget.Show(); ShowDetails(); };
            tray.Visible = true;
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5 * 60 * 1000;
            refreshTimer.Tick += async delegate { await RefreshAsync(); };
            clockTimer = new System.Windows.Forms.Timer();
            clockTimer.Interval = 60 * 1000;
            clockTimer.Tick += async delegate { await ClockTickAsync(); };
            taskbarTimer = new System.Windows.Forms.Timer();
            taskbarTimer.Interval = 1000;
            taskbarTimer.Tick += delegate { UpdateTaskbar(); };
            hoverTimer = new System.Windows.Forms.Timer();
            hoverTimer.Interval = 100;
            hoverTimer.Tick += delegate {
                Point pointer = Cursor.Position;
                widget.TrackHintPointer(pointer);
                bool inside = (widget.Visible && widget.Bounds.Contains(pointer)) || details.ContainsPointer(pointer);
                if (!details.EditingLocation && hoverDismiss.ShouldDismiss(DateTimeOffset.UtcNow, details.Visible, inside, menu.Visible)) details.Hide();
            };
            refreshTimer.Start();
            clockTimer.Start();
            taskbarTimer.Start();
            hoverTimer.Start();
            Render();
            widget.Show();
            initialized = true;
            foregroundMonitor = new ForegroundMonitor(QueueTaskbarUpdate);
            UpdateTaskbar();
        }

        private void QueueTaskbarUpdate()
        {
            if (stopping || !initialized || taskbarUpdateQueued || widget.IsDisposed) return;
            taskbarUpdateQueued = true;
            try
            {
                widget.BeginInvoke((MethodInvoker)delegate {
                    taskbarUpdateQueued = false;
                    if (!stopping) UpdateTaskbar();
                });
            }
            catch (InvalidOperationException) { taskbarUpdateQueued = false; }
        }

        private void RestorePosition()
        {
            widget.TopMost = true;
            if (UpdateTaskbar()) return;
            widget.ApplyScale(settings.ScalePercent);
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            widget.Location = new Point(area.Right - widget.Width - 20, area.Bottom - widget.Height - 20);
            widget.Bounds = Theme.Clamp(widget.Bounds, area);
        }

        private bool UpdateTaskbar()
        {
            if (stopping) return false;
            Rectangle taskbar, notification; bool visible;
            if (!TaskbarPlacement.TryRead(out taskbar, out visible, out notification)) return false;
            if (taskbar != currentTaskbar || appliedTaskbarScale != settings.ScalePercent)
            {
                currentTaskbar = taskbar; appliedTaskbarScale = settings.ScalePercent;
                widget.ApplyTaskbarScale(settings.ScalePercent, taskbar);
            }
            Rectangle placement = TaskbarPlacement.Place(taskbar, widget.Size, notification);
            if (widget.Bounds != placement)
            {
                widget.Bounds = placement;
                if (placementObserver != null) placementObserver(taskbar, notification, placement);
            }
            if (!initialized) return true;
            bool show = widgetEnabled && (taskbarVisibility.Observe(visible) || details.Visible) && !TaskbarPlacement.ForegroundIsFullscreen(widget.Handle, details.IsHandleCreated ? details.Handle : IntPtr.Zero);
            if (show && !widget.Visible) widget.Show();
            else if (!show && widget.Visible) { widget.Hide(); details.Hide(); }
            if (show) TaskbarPlacement.KeepAboveTaskbar(widget.Handle);
            return true;
        }

        private QuotaWindow SelectedWindow()
        {
            if (snapshot == null || snapshot.Windows == null || snapshot.Windows.Count == 0) return null;
            foreach (QuotaWindow candidate in snapshot.Windows) if (candidate.Id == settings.SelectedWindowId) return candidate;
            // Parser places the main Codex bucket before specialized buckets.
            return snapshot.Windows[0];
        }

        private void ShowDetails()
        {
            if (stopping) return;
            widgetEnabled = true;
            if (!widget.Visible) widget.Show();
            Render();
            details.ShowAnchored(widget);
        }

        private void ToggleWidget()
        {
            widgetEnabled = !widgetEnabled;
            if (!widgetEnabled) { widget.Hide(); details.Hide(); }
            else { RestorePosition(); widget.Show(); }
        }

        private void ApplySettings()
        {
            settings.ScalePercent = 100;
            settings.Language = details.SelectedLanguage;
            settings.ThemeMode = details.SelectedThemeMode;
            UiText.Language = settings.Language;
            UpdateTheme();
            if (details.SelectedWindowId != null) settings.SelectedWindowId = details.SelectedWindowId;
            widget.TopMost = true;
            details.TopMost = false;
            details.ApplyScale(settings.ScalePercent);
            UpdateTaskbar();
            SaveSettings();
            if (details.Visible) details.ShowAnchored(widget);
        }

        private void SaveSettings()
        {
            try { settingsError = store.Save(settings) ? null : "设置未能保存，重启后可能恢复默认。"; }
            catch { settingsError = "设置未能保存，重启后可能恢复默认。"; }
            Render();
        }

        private async Task RefreshAsync()
        {
            if (busy || stopping) return;
            RefreshResetAnnouncement();
            busy = true;
            Render();
            try
            {
                QuotaSnapshot value = await source.FetchAsync(cancellation.Token);
                if (stopping) return;
                if (value == null) throw new InvalidOperationException("未获取到额度数据，请稍后刷新。");
                snapshot = value;
                error = null;
            }
            catch (OperationCanceledException) { if (!stopping) error = "读取已取消，请重试。"; }
            catch (InvalidOperationException exception)
            {
                if (!stopping) error = String.IsNullOrWhiteSpace(exception.Message) ? "读取失败，请检查 Codex 登录状态后重试。" : exception.Message;
            }
            catch { if (!stopping) error = "暂时无法读取额度，请检查 Codex 登录状态及网络后重试。"; }
            finally { busy = false; if (!stopping) Render(); }
        }

        private async void RefreshResetAnnouncement()
        {
            await resetFeed.RefreshAsync(cancellation.Token);
            if (!stopping) details.SetResetFeed(resetFeed);
        }

        private async Task ClockTickAsync()
        {
            if (stopping) return;
            UpdateTheme();
            Render();
            if (busy || snapshot == null || snapshot.Windows == null) return;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool refreshNeeded = false;
            foreach (QuotaWindow window in snapshot.Windows)
            {
                if (!window.IsResetPending(now) || !window.ResetsAtUtc.HasValue) continue;
                string id = window.Id ?? String.Empty;
                DateTimeOffset previous;
                DateTimeOffset reset = window.ResetsAtUtc.Value;
                if (resetRefreshAttempts.TryGetValue(id, out previous) && previous == reset) continue;
                resetRefreshAttempts[id] = reset;
                refreshNeeded = true;
            }
            // One request covers all windows; a repeated expired response waits for
            // the normal five-minute retry or an explicit user refresh.
            if (refreshNeeded) await RefreshAsync();
        }

        private bool ResolveDarkTheme()
        {
            if (settings.ThemeMode == "dark") return true;
            return settings.ThemeMode == "auto" && settings.Latitude.HasValue && settings.Longitude.HasValue &&
                SolarTheme.IsDark(DateTime.Now, settings.Latitude.Value, settings.Longitude.Value);
        }

        private void UpdateTheme()
        {
            bool dark = ResolveDarkTheme();
            if (Theme.IsDark == dark) return;
            Theme.Apply(dark);
            widget.BackColor = Theme.Background;
            details.ApplyTheme();
            menu.BackColor = Theme.Card; menu.ForeColor = Theme.Text;
            Icon previous = trayIcon;
            trayIcon = Theme.CreateIcon();
            tray.Icon = trayIcon;
            if (previous != null) previous.Dispose();
            Render();
        }

        private void Render()
        {
            if (stopping) return;
            menu.Items[0].Text = widgetEnabled ? UiText.T("隐藏额度条", "Hide widget") : UiText.T("显示额度条", "Show widget");
            menu.Items[1].Text = UiText.T("查看用量详情", "Usage details");
            menu.Items[2].Text = UiText.T("立即刷新", "Refresh");
            menu.Items[4].Text = UiText.T("退出", "Exit");
            QuotaWindow selected = SelectedWindow();
            bool stale = snapshot != null && (!String.IsNullOrEmpty(error) || DateTimeOffset.UtcNow - snapshot.FetchedAtUtc > TimeSpan.FromMinutes(10));
            string combined = !String.IsNullOrEmpty(settingsError) ? settingsError : error;
            widget.SetState(selected, snapshot == null ? "Codex" : snapshot.PlanLabel, stale, busy, combined);
            details.SetState(snapshot, selected == null ? null : selected.Id, stale, busy, error, settingsError);
            refreshItem.Enabled = !busy;
            string tooltip = UiText.AppName + (snapshot == null ? "" : " · " + UiText.Plan(snapshot.PlanLabel)) + (selected == null ? "" : " · " + UiText.WindowLabel(selected.Label) + " " + (selected.IsResetPending(DateTimeOffset.UtcNow) ? UiText.T("待更新", "Pending") : Theme.Percent(selected.RemainingPercent)));
            if (stale) tooltip += UiText.T("（已过期）", " (Out of date)");
            tray.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
        }

        private void Stop()
        {
            if (stopping) return;
            stopping = true;
            if (foregroundMonitor != null) foregroundMonitor.Dispose();
            refreshTimer.Stop(); clockTimer.Stop(); taskbarTimer.Stop();
            hoverTimer.Stop();
            cancellation.Cancel();
            tray.Visible = false;
            details.Shutdown();
            source.Dispose();
        }

        protected override void ExitThreadCore()
        {
            Stop();
            if (!widget.IsDisposed) widget.Close();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                Stop();
                refreshTimer.Dispose(); clockTimer.Dispose(); taskbarTimer.Dispose();
                hoverTimer.Dispose();
                tray.Dispose(); trayIcon.Dispose(); menu.Dispose();
                details.Dispose(); widget.Dispose();
                cancellation.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class MenuColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Theme.Card; } }
            public override Color ImageMarginGradientBegin { get { return Theme.Card; } }
            public override Color ImageMarginGradientMiddle { get { return Theme.Card; } }
            public override Color ImageMarginGradientEnd { get { return Theme.Card; } }
            public override Color MenuItemSelected { get { return Theme.Border; } }
            public override Color MenuItemBorder { get { return Theme.Blue; } }
            public override Color MenuBorder { get { return Theme.Border; } }
            public override Color CheckBackground { get { return Theme.Border; } }
            public override Color CheckSelectedBackground { get { return Theme.Border; } }
            public override Color CheckPressedBackground { get { return Theme.Border; } }
        }
    }
}
