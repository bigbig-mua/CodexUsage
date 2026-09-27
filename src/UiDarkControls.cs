using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal sealed class UiDarkChoice : Control
    {
        internal sealed class ChoiceItems : Collection<object>
        {
            private readonly UiDarkChoice owner;
            internal ChoiceItems(UiDarkChoice value) { owner = value; }
            internal void AddRange(object[] values) { foreach (object value in values) Add(value); }
            protected override void InsertItem(int index, object item) { base.InsertItem(index, item); owner.ItemsChanged(); }
            protected override void SetItem(int index, object item) { base.SetItem(index, item); owner.ItemsChanged(); }
            protected override void RemoveItem(int index) { base.RemoveItem(index); owner.ItemsChanged(); }
            protected override void ClearItems() { base.ClearItems(); owner.ItemsChanged(); }
        }

        private int selectedIndex = -1;
        private bool hovering;
        private ToolStripDropDown popup;
        private UiChoiceList list;
        internal readonly ChoiceItems Items;
        internal int ItemHeight = 22;
        internal bool GlassSurface;
        internal event EventHandler SelectedIndexChanged;
        internal int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int next = Math.Max(-1, Math.Min(Items.Count - 1, value));
                if (selectedIndex == next) return;
                selectedIndex = next; Invalidate();
                AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }
        internal bool IsDroppedDown { get { return popup != null && popup.Visible; } }
        internal bool DropDownContains(Point point) { return IsDroppedDown && popup.Bounds.Contains(point); }

        internal UiDarkChoice()
        {
            Items = new ChoiceItems(this);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Theme.Card; ForeColor = Theme.Text;
            TabStop = true; Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.ComboBox;
        }

        private void ItemsChanged()
        {
            if (SelectedIndex >= Items.Count) SelectedIndex = Items.Count - 1;
            CloseDropDown(); Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            DetailsForm form = FindForm() as DetailsForm;
            if (!GlassSurface || form == null || !form.PaintGlass(e.Graphics, this)) base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            float radius = Math.Max(4, Height * .23f);
            DetailsForm form = FindForm() as DetailsForm;
            bool glass = GlassSurface && form != null && form.HasGlassBackground;
            Color surface = glass ? (Theme.IsDark ? Color.FromArgb(85, 32, 42, 56) : Color.FromArgb(68, 255, 255, 255)) : Theme.Card;
            RectangleF bounds = new RectangleF(.5f, .5f, Width - 1, Height - 1);
            Theme.Rounded(g, bounds, radius, surface,
                glass ? (Color?)null : Focused || IsDroppedDown ? Theme.Aqua : hovering ? Theme.Muted : Theme.Border);
            if (glass) using (GraphicsPath path = Theme.Round(bounds, radius)) DetailsForm.DrawGlassRim(g, path, Height, Focused || IsDroppedDown);
            int pad = Math.Max(7, Height / 4);
            Rectangle text = new Rectangle(pad, 0, Math.Max(1, Width - pad - Height), Height);
            string value = SelectedIndex < 0 ? UiText.T("暂无额度", "No windows") : Convert.ToString(Items[SelectedIndex]);
            TextRenderer.DrawText(g, value, Font, text, Enabled ? Theme.Text : Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            float x = Width - Height * .53f, y = Height * .47f, arrow = Math.Max(3, Height * .12f);
            using (Pen pen = new Pen(Enabled ? Theme.Muted : Theme.Border, Math.Max(1.2f, Height / 22f)))
            { pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; g.DrawLines(pen, new PointF[] { new PointF(x - arrow, y - arrow / 2), new PointF(x, y + arrow / 2), new PointF(x + arrow, y - arrow / 2) }); }
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { if (!Enabled) CloseDropDown(); Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left && Enabled) { Focus(); if (IsDroppedDown) CloseDropDown(); else OpenDropDown(); } }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End || key == Keys.Enter || key == Keys.Space || key == Keys.F4 || base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled) return;
            if (e.KeyCode == Keys.F4 || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || (e.Alt && e.KeyCode == Keys.Down))
            { if (IsDroppedDown) CloseDropDown(); else OpenDropDown(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            { SelectedIndex = Math.Max(0, SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1)); e.Handled = true; }
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.End)
            { SelectedIndex = e.KeyCode == Keys.Home ? 0 : Items.Count - 1; e.Handled = true; }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Enabled && Items.Count > 0) SelectedIndex = Math.Max(0, SelectedIndex - Math.Sign(e.Delta));
            HandledMouseEventArgs handled = e as HandledMouseEventArgs; if (handled != null) handled.Handled = true;
            base.OnMouseWheel(e);
        }

        internal void OpenDropDown()
        {
            if (!Enabled || Items.Count == 0 || IsDroppedDown) return;
            DisposePopup();
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int rowHeight = Math.Max(Font.Height + 10, ItemHeight + 6);
            int visible = Math.Min(8, Items.Count);
            int height = Math.Min(visible * rowHeight + 8, Math.Max(rowHeight + 8, area.Height / 2));
            list = new UiChoiceList(this, rowHeight);
            list.Font = Font; list.Size = new Size(Math.Max(Width, Math.Min(100, area.Width - 24)), height);
            ToolStripControlHost host = new ToolStripControlHost(list);
            host.Margin = Padding.Empty; host.Padding = Padding.Empty; host.AutoSize = false; host.Size = list.Size;
            popup = new ToolStripDropDown();
            popup.Renderer = new PopupRenderer();
            popup.AutoSize = true; popup.Padding = new Padding(1); popup.Margin = Padding.Empty;
            popup.BackColor = Theme.Border; popup.DropShadowEnabled = false;
            popup.Items.Add(host);
            popup.Closed += delegate { Invalidate(); AccessibilityNotifyClients(AccessibleEvents.StateChange, -1); };
            popup.Show(this, new Point(0, Height + 3));
            list.Focus(); Invalidate(); AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
        }
        internal void CloseDropDown() { if (popup != null && popup.Visible) popup.Close(); }
        internal void Commit(int index) { CloseDropDown(); SelectedIndex = index; if (!IsDisposed && CanFocus) Focus(); }
        private void DisposePopup()
        { if (popup != null) { popup.Dispose(); popup = null; list = null; } }
        protected override void Dispose(bool disposing) { if (disposing) DisposePopup(); base.Dispose(disposing); }
        private sealed class PopupRenderer : ToolStripRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { e.Graphics.Clear(Theme.Card); }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            { using (Pen pen = new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1); }
        }
        protected override AccessibleObject CreateAccessibilityInstance() { return new ChoiceAccessibility(this); }
        private sealed class ChoiceAccessibility : ControlAccessibleObject
        {
            private readonly UiDarkChoice choice;
            internal ChoiceAccessibility(UiDarkChoice value) : base(value) { choice = value; }
            public override string Value { get { return choice.SelectedIndex < 0 ? UiText.T("暂无额度", "No windows") : Convert.ToString(choice.Items[choice.SelectedIndex]); } set { } }
            public override string DefaultAction { get { return UiText.T("展开选项", "Show options"); } }
            public override AccessibleStates State { get { return base.State | (choice.IsDroppedDown ? AccessibleStates.Expanded : AccessibleStates.Collapsed); } }
            public override void DoDefaultAction() { choice.OpenDropDown(); }
        }

        private sealed class UiChoiceList : Control
        {
            private readonly UiDarkChoice owner;
            private readonly int rowHeight;
            private int active;
            private int first;
            internal UiChoiceList(UiDarkChoice choice, int row)
            {
                owner = choice; rowHeight = row; active = Math.Max(0, owner.SelectedIndex);
                BackColor = Theme.Card; ForeColor = Theme.Text; TabStop = true;
                AccessibleName = owner.AccessibleName + UiText.T("选项", " options"); AccessibleRole = AccessibleRole.List;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            }
            private int VisibleRows { get { return Math.Max(1, (Height - 8) / rowHeight); } }
            private void Reveal()
            { first = Math.Min(first, active); if (active >= first + VisibleRows) first = active - VisibleRows + 1; first = Math.Max(0, first); Invalidate(); }
            protected override void OnGotFocus(EventArgs e) { Reveal(); base.OnGotFocus(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(Theme.Card);
                for (int row = 0; row < VisibleRows && first + row < owner.Items.Count; row++)
                {
                    int index = first + row;
                    Rectangle rect = new Rectangle(4, 4 + row * rowHeight, Width - 8, rowHeight);
                    if (index == active) Theme.Rounded(e.Graphics, rect, 5, Theme.Border, null);
                    Rectangle text = new Rectangle(rect.Left + 10, rect.Top, rect.Width - 34, rect.Height);
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(owner.Items[index]), Font, text, Theme.Text,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                    if (index == owner.SelectedIndex) TextRenderer.DrawText(e.Graphics, "✓", Font, new Rectangle(rect.Right - 24, rect.Top, 20, rect.Height), Theme.Aqua, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                if (owner.Items.Count > VisibleRows)
                {
                    int h = Math.Max(12, (Height - 8) * VisibleRows / owner.Items.Count);
                    int y = 4 + (Height - 8 - h) * first / Math.Max(1, owner.Items.Count - VisibleRows);
                    Theme.Rounded(e.Graphics, new RectangleF(Width - 4, y, 2, h), 1, Theme.Muted, null);
                }
            }
            protected override void OnMouseMove(MouseEventArgs e)
            { int index = first + (e.Y - 4) / rowHeight; if (e.Y >= 4 && e.Y < 4 + VisibleRows * rowHeight && index < owner.Items.Count && index != active) { active = index; Invalidate(); } base.OnMouseMove(e); }
            protected override void OnMouseUp(MouseEventArgs e)
            { base.OnMouseUp(e); int index = first + (e.Y - 4) / rowHeight; if (e.Button == MouseButtons.Left && e.Y >= 4 && e.Y < 4 + VisibleRows * rowHeight && index >= 0 && index < owner.Items.Count) owner.Commit(index); }
            protected override bool IsInputKey(Keys keyData) { return true; }
            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                Keys key = keyData & Keys.KeyCode;
                if (key == Keys.Escape || key == Keys.F4) { owner.CloseDropDown(); owner.Focus(); return true; }
                if (key == Keys.Enter || key == Keys.Space) { owner.Commit(active); return true; }
                if (key == Keys.Tab) { owner.CloseDropDown(); owner.Parent.SelectNextControl(owner, (keyData & Keys.Shift) == 0, true, true, true); return true; }
                if (key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown)
                {
                    if (key == Keys.Home) active = 0;
                    else if (key == Keys.End) active = owner.Items.Count - 1;
                    else active += key == Keys.Up ? -1 : key == Keys.Down ? 1 : key == Keys.PageUp ? -VisibleRows : VisibleRows;
                    active = Math.Max(0, Math.Min(owner.Items.Count - 1, active)); Reveal(); return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                first = Math.Max(0, Math.Min(owner.Items.Count - VisibleRows, first - Math.Sign(e.Delta) * 3));
                active = Math.Max(first, Math.Min(first + VisibleRows - 1, active)); Invalidate();
                HandledMouseEventArgs handled = e as HandledMouseEventArgs; if (handled != null) handled.Handled = true;
            }
            protected override AccessibleObject CreateAccessibilityInstance() { return new ListAccessibility(this); }
            private sealed class ListAccessibility : ControlAccessibleObject
            {
                private readonly UiChoiceList view;
                internal ListAccessibility(UiChoiceList value) : base(value) { view = value; }
                public override int GetChildCount() { return view.owner.Items.Count; }
                public override AccessibleObject GetChild(int index)
                { return index >= 0 && index < GetChildCount() ? new ItemAccessibility(view, index, this) : null; }
            }
            private sealed class ItemAccessibility : AccessibleObject
            {
                private readonly UiChoiceList view;
                private readonly int index;
                private readonly AccessibleObject parent;
                internal ItemAccessibility(UiChoiceList value, int item, AccessibleObject container) { view = value; index = item; parent = container; }
                public override string Name { get { return index < view.owner.Items.Count ? Convert.ToString(view.owner.Items[index]) : String.Empty; } set { } }
                public override AccessibleRole Role { get { return AccessibleRole.ListItem; } }
                public override AccessibleObject Parent { get { return parent; } }
                public override string DefaultAction { get { return UiText.T("选择", "Select"); } }
                public override void DoDefaultAction() { view.owner.Commit(index); }
                public override Rectangle Bounds
                {
                    get
                    {
                        if (index < view.first || index >= view.first + view.VisibleRows) return Rectangle.Empty;
                        return view.RectangleToScreen(new Rectangle(4, 4 + (index - view.first) * view.rowHeight, view.Width - 8, view.rowHeight));
                    }
                }
                public override AccessibleStates State
                {
                    get
                    {
                        return AccessibleStates.Selectable | (index == view.owner.SelectedIndex ? AccessibleStates.Selected : AccessibleStates.None)
                            | (index == view.active ? AccessibleStates.Focused : AccessibleStates.None)
                            | (index < view.first || index >= view.first + view.VisibleRows ? AccessibleStates.Offscreen : AccessibleStates.None);
                    }
                }
            }
        }
    }

    internal sealed class UiDarkScrollPanel : Panel
    {
        private int contentHeight;
        private int offset;
        private bool dragging;
        private int dragStart;
        private int offsetStart;
        internal float ScaleFactor = 1;
        internal int ScrollOffset { get { return offset; } }
        internal int ContentWidth { get { return Math.Max(1, ClientSize.Width - (int)Math.Ceiling(15 * ScaleFactor)); } }
        private int MaximumOffset { get { return Math.Max(0, contentHeight - ClientSize.Height); } }
        internal UiDarkScrollPanel()
        {
            AutoScroll = false; TabStop = true; BackColor = Theme.Background;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            AccessibleRole = AccessibleRole.Pane;
        }
        internal void SetContentHeight(int height) { contentHeight = Math.Max(0, height); SetOffset(offset); Invalidate(); }
        internal void SetOffset(int value)
        {
            int next = Math.Max(0, Math.Min(MaximumOffset, value));
            int delta = next - offset;
            offset = next;
            if (delta != 0) foreach (Control child in Controls) child.Top -= delta;
            Invalidate();
        }
        internal void EnsureVisible(Control child)
        {
            if (child == null || child.Parent != this) return;
            if (child.Top < 0) SetOffset(offset + child.Top);
            else if (child.Bottom > ClientSize.Height) SetOffset(offset + child.Bottom - ClientSize.Height);
        }
        protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); e.Control.MouseWheel += ChildWheel; }
        protected override void OnControlRemoved(ControlEventArgs e) { e.Control.MouseWheel -= ChildWheel; base.OnControlRemoved(e); }
        private void ChildWheel(object sender, MouseEventArgs e) { ScrollWheel(e); }
        private void ScrollWheel(MouseEventArgs e)
        {
            SetOffset(offset - Math.Sign(e.Delta) * Math.Max(1, (int)(48 * ScaleFactor)));
            HandledMouseEventArgs handled = e as HandledMouseEventArgs; if (handled != null) handled.Handled = true;
        }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollWheel(e); }
        private Rectangle Thumb()
        {
            int trackHeight = Math.Max(1, Height - 4);
            int h = Math.Min(trackHeight, Math.Max((int)(28 * ScaleFactor), trackHeight * ClientSize.Height / Math.Max(1, contentHeight)));
            int y = 2 + (trackHeight - h) * offset / Math.Max(1, MaximumOffset);
            int w = Math.Max(5, (int)(6 * ScaleFactor));
            return new Rectangle(Width - w - 2, y, w, h);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (MaximumOffset <= 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle thumb = Thumb();
            Theme.Rounded(e.Graphics, new RectangleF(thumb.X, 2, thumb.Width, Height - 4), thumb.Width / 2f, Theme.Card, null);
            Theme.Rounded(e.Graphics, thumb, thumb.Width / 2f, dragging || Focused ? Theme.Aqua : Theme.Muted, null);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return; Focus();
            if (MaximumOffset <= 0 || e.X < ContentWidth) return;
            Rectangle thumb = Thumb();
            if (thumb.Contains(e.Location)) { dragging = true; dragStart = e.Y; offsetStart = offset; Capture = true; }
            else SetOffset(offset + (e.Y < thumb.Top ? -1 : 1) * ClientSize.Height);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (!dragging) return;
            int travel = Math.Max(1, Height - 4 - Thumb().Height);
            SetOffset(offsetStart + (int)((long)(e.Y - dragStart) * MaximumOffset / travel));
        }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; Capture = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) dragging = false; base.OnMouseCaptureChanged(e); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); SetOffset(offset); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Up || key == Keys.Down || key == Keys.PageUp || key == Keys.PageDown || key == Keys.Home || key == Keys.End || base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Home) SetOffset(0);
            else if (e.KeyCode == Keys.End) SetOffset(MaximumOffset);
            else if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.PageUp) SetOffset(offset + (e.KeyCode == Keys.PageDown ? 1 : -1) * ClientSize.Height);
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) SetOffset(offset + (e.KeyCode == Keys.Down ? 1 : -1) * (int)(36 * ScaleFactor));
            else return;
            e.Handled = true;
        }
    }
}

