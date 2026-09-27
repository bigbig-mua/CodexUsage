using System;

namespace CodexQuotaLite
{
    // NOAA solar position approximation. Longitude is positive to the east.
    internal static class SolarTheme
    {
        internal static bool IsDark(DateTime localNow, double latitude, double longitude)
        {
            DateTime sunrise, sunset;
            if (!TryEvents(localNow.Date, latitude, longitude, out sunrise, out sunset))
            {
                // Polar and date-boundary cases use the current solar altitude.
                double declination, equation;
                DateTime utc = localNow.ToUniversalTime();
                SolarValues(utc.DayOfYear, out declination, out equation);
                double solarMinutes = utc.TimeOfDay.TotalMinutes + equation + 4 * longitude;
                double hourAngle = Radians((solarMinutes % 1440 + 1440) % 1440 / 4 - 180);
                double altitude = Math.Asin(Math.Sin(Radians(latitude)) * Math.Sin(declination) +
                    Math.Cos(Radians(latitude)) * Math.Cos(declination) * Math.Cos(hourAngle));
                return altitude < Radians(-0.833);
            }
            return localNow < sunrise || localNow >= sunset;
        }

        internal static bool TryEvents(DateTime localDate, double latitude, double longitude, out DateTime sunrise, out DateTime sunset)
        {
            sunrise = DateTime.MinValue; sunset = DateTime.MinValue;
            if (Double.IsNaN(latitude) || Double.IsNaN(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180) return false;
            for (int shift = -1; shift <= 1; shift++)
            {
                DateTime utcDate = localDate.Date.AddDays(shift);
                double declination, equation;
                SolarValues(utcDate.DayOfYear, out declination, out equation);
                double lat = Radians(latitude);
                double cosine = (Math.Cos(Radians(90.833)) / (Math.Cos(lat) * Math.Cos(declination))) - Math.Tan(lat) * Math.Tan(declination);
                if (Double.IsNaN(cosine) || cosine < -1 || cosine > 1) continue;
                double angle = Math.Acos(cosine) * 180 / Math.PI;
                DateTime rise = DateTime.SpecifyKind(utcDate.AddMinutes(720 - 4 * (longitude + angle) - equation), DateTimeKind.Utc).ToLocalTime();
                DateTime set = DateTime.SpecifyKind(utcDate.AddMinutes(720 - 4 * (longitude - angle) - equation), DateTimeKind.Utc).ToLocalTime();
                if (rise.Date == localDate.Date) sunrise = rise;
                if (set.Date == localDate.Date) sunset = set;
            }
            return sunrise != DateTime.MinValue && sunset != DateTime.MinValue;
        }

        private static double Radians(double degrees) { return degrees * Math.PI / 180; }

        private static void SolarValues(int day, out double declination, out double equation)
        {
            double gamma = 2 * Math.PI * (day - 1) / 365.0;
            equation = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma) -
                0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));
            declination = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma) -
                0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma) -
                0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);
        }
    }
}
