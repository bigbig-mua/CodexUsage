using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    public sealed class WidgetForm : Form
    {
        private float scale = 1;
        private QuotaWindow window;
        private QuotaWindow fiveHourWindow;
        private QuotaWindow weeklyWindow;
        private string plan = "Codex";
        private bool stale;
        private bool busy;
        private string error;
        private readonly ToolTip tip = new ToolTip();
        private string hintText = String.Empty;
        private bool hintSuppressed;
        public event EventHandler DetailRequested;

        public WidgetForm()
        {
            Text = UiText.AppName;
            AccessibleName = UiText.T("CodexUsage，单击查看详情", "CodexUsage, click for details");
            AccessibleRole = AccessibleRole.PushButton;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Background;
            DoubleBuffered = true;
            KeyPreview = true;
            Cursor = Cursors.Hand;
            tip.ShowAlways = true;
            tip.InitialDelay = 300;
            tip.ReshowDelay = 300;
            tip.Active = false;
            tip.Popup += delegate(object sender, PopupEventArgs e) {
                e.Cancel = hintSuppressed || !Visible || !Bounds.Contains(Cursor.Position)
                    || Control.MouseButtons != MouseButtons.None
                    || (ContextMenuStrip != null && ContextMenuStrip.Visible);
            };
            ApplyScale(100);
        }

        protected override CreateParams CreateParams
        { get { var value = base.CreateParams; value.ExStyle |= 0x00080000 | 0x00000080 | 0x08000000; return value; } }
        protected override bool ShowWithoutActivation { get { return true; } }

        public void ApplyScale(int percent)
        {
            using (Graphics g = CreateGraphics()) scale = g.DpiX / 96f * percent / 100f;
            ApplyDimensions();
        }
        public void ApplyTaskbarScale(int percent, Rectangle taskbar)
        {
            using (Graphics g = CreateGraphics()) scale = g.DpiX / 96f * percent / 100f;
            scale = Math.Max(.15f, Math.Min(scale, Math.Min((taskbar.Width - 6) / (float)WidgetRenderer.LogicalWidth, (taskbar.Height - 6) / (float)WidgetRenderer.LogicalHeight)));
            ApplyDimensions();
        }
        private void ApplyDimensions()
        {
            ClientSize = new Size(Math.Max(1, (int)Math.Round(WidgetRenderer.LogicalWidth * scale)), Math.Max(1, (int)Math.Round(WidgetRenderer.LogicalHeight * scale)));
            Present();
        }

        public void SetState(QuotaWindow selected, QuotaWindow fiveHour, QuotaWindow weekly, string planLabel, bool expired, bool refreshing, string message)
        {
            window = selected;
            fiveHourWindow = fiveHour;
            weeklyWindow = weekly;
            Text = UiText.AppName;
            AccessibleName = UiText.T("CodexUsage，单击查看详情", "CodexUsage, click for details");
            plan = String.IsNullOrWhiteSpace(planLabel) ? "Codex" : planLabel;
            stale = expired; busy = refreshing; error = message;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string status = UiText.Plan(plan) + ". " + (window == null ? UiText.T("尚无额度数据", "No usage data") : WindowStatus(window, now)) + ". ";
            if (window != null && (weeklyWindow == null || weeklyWindow.Id != window.Id))
                status += "\n" + (weeklyWindow == null
                    ? UiText.T("每周额度，暂无数据", "Weekly, no usage data")
                    : WindowStatus(weeklyWindow, now)) + ". ";
            if (stale) status += UiText.T("上次结果已过期。", "The previous result is out of date. ");
            if (!String.IsNullOrEmpty(error)) status += UiText.Error(error);
            hintText = status + UiText.T("\n单击查看详情 · 右键打开菜单", "\nClick for details · Right-click for menu");
            if (tip.Active && !hintSuppressed) tip.SetToolTip(this, hintText);
            AccessibleDescription = status;
            Present();
        }

        private static string WindowStatus(QuotaWindow value, DateTimeOffset now)
        {
            string label = UiText.WindowLabel(value.Label);
            if (value.IsResetPending(now))
                return label + UiText.T("，已到重置时间，待更新", ", reset reached; awaiting update");
            return label + UiText.T("，剩余额度 ", ", remaining ") + Theme.Percent(value.RemainingPercent) + ", " + Theme.ResetText(value, now);
        }

        internal Bitmap RenderImage()
        { return WidgetRenderer.Render(ClientSize, window, fiveHourWindow, weeklyWindow, stale, busy, error, DateTimeOffset.UtcNow); }
        private void Present()
        {
            if (!IsHandleCreated || !Visible || IsDisposed || ClientSize.Width < 1 || ClientSize.Height < 1) return;
            using (Bitmap image = RenderImage()) LayeredSurface.Present(this, image);
        }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Present(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (Visible) Present(); else DismissHint(); }
        private void DismissHint()
        {
            tip.Active = false;
            if (IsHandleCreated) tip.Hide(this);
        }
        internal void TrackHintPointer(Point pointer)
        {
            if (!Visible || !Bounds.Contains(pointer))
            {
                hintSuppressed = false;
                if (tip.Active) DismissHint();
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (hintSuppressed || Control.MouseButtons != MouseButtons.None) return;
            tip.SetToolTip(this, hintText);
            tip.Active = true;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            DismissHint();
            // A menu can take mouse tracking without the pointer physically leaving.
            if (!Bounds.Contains(Cursor.Position)) hintSuppressed = false;
            base.OnMouseLeave(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            hintSuppressed = true;
            DismissHint();
            base.OnMouseDown(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        { using (Bitmap image = RenderImage()) e.Graphics.DrawImageUnscaled(image, 0, 0); }
        protected override void OnPaintBackground(PaintEventArgs e) { /* The layered surface supplies the complete frame. */ }
        protected override void OnMouseClick(MouseEventArgs e)
        { base.OnMouseClick(e); if (e.Button == MouseButtons.Left && DetailRequested != null) DetailRequested(this, EventArgs.Empty); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            DismissHint();
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            { e.Handled = true; if (DetailRequested != null) DetailRequested(this, EventArgs.Empty); }
        }
        protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
    }
}
