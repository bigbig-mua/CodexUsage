using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;

namespace CodexQuotaLite
{
    internal static class WidgetRenderer
    {
        internal const int LogicalWidth = 76, LogicalHeight = 40;
        internal const float ValueFontSize = 12;
        // The widget sits on the taskbar, so keep its palette legible in either app theme.
        internal static readonly Color TrayIconSurface = Color.FromArgb(32, 42, 56);
        internal static readonly Color QuotaColor = Color.FromArgb(64, 218, 195);
        internal static readonly Color TimeColor = Color.FromArgb(117, 168, 255);
        private static readonly Color TrackColor = Color.FromArgb(75, 91, 109);
        private static readonly Color MutedColor = Color.FromArgb(174, 191, 211);
        private static readonly Color WarningColor = Color.FromArgb(255, 185, 101);

        internal static string CompactTime(QuotaWindow window, DateTimeOffset now)
        {
            if (window == null || !window.ResetsAtUtc.HasValue) return "—";
            if (window.IsResetPending(now)) return UiText.T("待更新", "Wait");
            TimeSpan left = window.ResetsAtUtc.Value - now;
            int minutes = (int)Math.Min(Int32.MaxValue, Math.Max(1, Math.Ceiling(left.TotalMinutes)));
            return (minutes / 60).ToString(CultureInfo.InvariantCulture) + "h" +
                (minutes % 60).ToString("00", CultureInfo.InvariantCulture) + "m";
        }

        internal static Bitmap Render(Size size, QuotaWindow window, bool stale, bool busy, string error, DateTimeOffset now)
        {
            const int samples = 4;
            using (var large = new Bitmap(size.Width * samples, size.Height * samples, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(large))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float scale = size.Height / (float)LogicalHeight * samples;
                g.ScaleTransform(scale, scale);
                float width = size.Width * samples / scale;
                bool pending = window != null && window.IsResetPending(now);
                Color quotaColor = stale || pending ? MutedColor : QuotaColor;
                DrawDisk(g, new RectangleF(7, 3, 14, 14), pending || window == null ? null : window.RemainingPercent, quotaColor);
                DrawDisk(g, new RectangleF(7, 23, 14, 14), window == null ? null : window.GetTimeRemainingPercent(now), stale ? MutedColor : TimeColor);
                string amount = pending ? UiText.T("待更新", "Wait") : window == null ? "—" : Theme.Percent(window.RemainingPercent);
                RectangleF topText = new RectangleF(24, 2, width - 27, 16);
                RectangleF bottomText = new RectangleF(24, 22, width - 27, 16);
                DrawText(g, amount, topText, ValueFontSize, FontStyle.Bold, quotaColor);
                string time = busy && window == null ? UiText.T("更新中", "Sync") : !String.IsNullOrEmpty(error) ? UiText.T("失败", "Retry") : stale ? UiText.T("已过期", "Stale") : CompactTime(window, now);
                DrawText(g, time, bottomText, ValueFontSize, FontStyle.Bold, stale || !String.IsNullOrEmpty(error) ? WarningColor : TimeColor);
                g.ResetTransform();
                return Reduce(large, size);
            }
        }

        internal static Bitmap RenderGlyph(int size)
        {
            using (var large = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(large))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(size / 8f, size / 8f);
                using (var brush = new SolidBrush(TrayIconSurface)) g.FillEllipse(brush, .5f, .5f, 31, 31);
                DrawDisk(g, new RectangleF(10, 3, 12, 12), 75, QuotaColor);
                DrawDisk(g, new RectangleF(10, 17, 12, 12), 200 / 3.6, TimeColor);
                g.ResetTransform();
                return Reduce(large, new Size(size, size));
            }
        }

        private static Bitmap Reduce(Bitmap source, Size size)
        {
            var result = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(result))
            using (var attributes = new ImageAttributes())
            {
                g.Clear(Color.Transparent); g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source, new Rectangle(Point.Empty, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return result;
        }

        private static void DrawDisk(Graphics g, RectangleF bounds, double? percent, Color color)
        {
            using (var brush = new SolidBrush(TrackColor)) g.FillEllipse(brush, bounds);
            if (!percent.HasValue || percent.Value <= 0) return;
            using (var brush = new SolidBrush(color))
            {
                if (percent.Value >= 100) g.FillEllipse(brush, bounds);
                else g.FillPie(brush, bounds.X, bounds.Y, bounds.Width, bounds.Height, -90, (float)percent.Value * 3.6f);
            }
        }

        private static void DrawText(Graphics g, string text, RectangleF bounds, float size, FontStyle style, Color color)
        {
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat(StringFormat.GenericTypographic))
            {
                format.FormatFlags = StringFormatFlags.NoWrap; format.Trimming = StringTrimming.EllipsisCharacter;
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Far;
                Font font = null;
                try
                {
                    do
                    {
                        if (font != null) font.Dispose();
                        font = new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Pixel);
                        if (g.MeasureString(text, font, PointF.Empty, format).Width <= bounds.Width || size <= 9) break;
                        size -= .5f;
                    } while (true);
                    g.DrawString(text, font, brush, bounds, format);
                }
                finally { if (font != null) font.Dispose(); }
            }
        }
    }
}
