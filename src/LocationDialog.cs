using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CodexQuotaLite
{
    internal sealed class LocationDialog : Form
    {
        private readonly NumericUpDown latitude = new NumericUpDown();
        private readonly NumericUpDown longitude = new NumericUpDown();
        private readonly Label preview = new Label();

        internal double Latitude { get { return (double)latitude.Value; } }
        internal double Longitude { get { return (double)longitude.Value; } }

        internal LocationDialog(double? currentLatitude, double? currentLongitude)
        {
            Text = UiText.T("日出日落位置", "Sunrise and sunset location");
            ClientSize = new Size(340, 182);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Microsoft YaHei UI", 9f);
            BackColor = Theme.Card; ForeColor = Theme.Text;

            Label latitudeLabel = NewLabel(UiText.T("纬度（北为正）", "Latitude (north +)"), 16, 14, 150, 24);
            Label longitudeLabel = NewLabel(UiText.T("经度（东为正）", "Longitude (east +)"), 16, 50, 150, 24);
            ConfigureNumber(latitude, -90, 90, currentLatitude, 174, 14);
            ConfigureNumber(longitude, -180, 180, currentLongitude, 174, 50);
            preview.Bounds = new Rectangle(16, 88, 308, 36);
            preview.ForeColor = Theme.Muted;
            preview.TextAlign = ContentAlignment.MiddleLeft;
            latitude.ValueChanged += delegate { UpdatePreview(); };
            longitude.ValueChanged += delegate { UpdatePreview(); };

            Button save = NewButton(UiText.T("保存", "Save"), 156, 138, DialogResult.OK);
            Button cancel = NewButton(UiText.T("取消", "Cancel"), 248, 138, DialogResult.Cancel);
            Controls.AddRange(new Control[] { latitudeLabel, longitudeLabel, latitude, longitude, preview, save, cancel });
            AcceptButton = save; CancelButton = cancel;
            UpdatePreview();
        }

        private Label NewLabel(string text, int x, int y, int width, int height)
        {
            return new Label { Text = text, Bounds = new Rectangle(x, y, width, height),
                TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Text };
        }

        private Button NewButton(string text, int x, int y, DialogResult result)
        {
            return new Button { Text = text, Bounds = new Rectangle(x, y, 76, 28),
                DialogResult = result, BackColor = Theme.Background, ForeColor = Theme.Text,
                FlatStyle = FlatStyle.Flat };
        }

        private static void ConfigureNumber(NumericUpDown control, int min, int max, double? value, int x, int y)
        {
            control.Bounds = new Rectangle(x, y, 150, 26);
            control.Minimum = min; control.Maximum = max;
            control.DecimalPlaces = 4; control.Increment = 0.0001m;
            control.Value = value.HasValue ? Math.Max(min, Math.Min(max, (decimal)value.Value)) : 0;
            control.BackColor = Theme.Background; control.ForeColor = Theme.Text;
            control.ThousandsSeparator = false;
        }

        private void UpdatePreview()
        {
            DateTime sunrise, sunset;
            preview.Text = SolarTheme.TryEvents(DateTime.Today, Latitude, Longitude, out sunrise, out sunset)
                ? UiText.T("本机时间：日出 ", "System time: sunrise ") + sunrise.ToString("HH:mm", CultureInfo.CurrentCulture) +
                  UiText.T(" · 日落 ", " · sunset ") + sunset.ToString("HH:mm", CultureInfo.CurrentCulture)
                : UiText.T("今天此位置无日出或日落", "No sunrise or sunset here today");
        }
    }
}
