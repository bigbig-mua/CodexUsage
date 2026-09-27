using System;
using System.IO;
using System.Security;
using System.Text;
using System.Web.Script.Serialization;

namespace CodexQuotaLite
{
    public sealed class AppSettings
    {
        public bool AlwaysOnTop { get; set; }
        public int ScalePercent { get; set; }
        public string Language { get; set; }
        public string SelectedWindowId { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public bool DockToTaskbar { get; set; }
        public int? TaskbarX { get; set; }
        public string ThemeMode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public AppSettings()
        {
            AlwaysOnTop = true;
            ScalePercent = 100;
            Language = "zh";
            DockToTaskbar = true;
            ThemeMode = "light";
        }
    }

    public sealed class SettingsStore
    {
        private readonly string filePath;

        public SettingsStore(string filePath)
        {
            this.filePath = filePath;
        }

        public AppSettings Load()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return new AppSettings();
                }

                string json = File.ReadAllText(filePath, Encoding.UTF8);
                AppSettings settings = new JavaScriptSerializer().Deserialize<AppSettings>(json);
                return Sanitize(settings);
            }
            catch (IOException)
            {
                return new AppSettings();
            }
            catch (UnauthorizedAccessException)
            {
                return new AppSettings();
            }
            catch (SecurityException)
            {
                return new AppSettings();
            }
            catch (ArgumentException)
            {
                return new AppSettings();
            }
            catch (InvalidOperationException)
            {
                return new AppSettings();
            }
            catch (NotSupportedException)
            {
                return new AppSettings();
            }
        }

        public bool Save(AppSettings settings)
        {
            string temporaryPath = null;
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                string fullPath = Path.GetFullPath(filePath);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                {
                    return false;
                }

                Directory.CreateDirectory(directory);
                temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                string json = new JavaScriptSerializer().Serialize(Sanitize(settings));

                using (FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }

                temporaryPath = null;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }

        private static AppSettings Sanitize(AppSettings source)
        {
            AppSettings sanitized = new AppSettings();
            if (source == null)
            {
                return sanitized;
            }

            sanitized.AlwaysOnTop = source.AlwaysOnTop;
            sanitized.ScalePercent = 100;
            sanitized.Language = source.Language == "en" ? "en" : "zh";
            sanitized.SelectedWindowId = string.IsNullOrWhiteSpace(source.SelectedWindowId)
                ? null
                : source.SelectedWindowId.Trim();
            sanitized.X = source.X;
            sanitized.Y = source.Y;
            sanitized.DockToTaskbar = source.DockToTaskbar;
            sanitized.TaskbarX = source.TaskbarX;
            sanitized.ThemeMode = source.ThemeMode == "dark" || source.ThemeMode == "auto" ? source.ThemeMode : "light";
            if (source.Latitude.HasValue && source.Longitude.HasValue &&
                !Double.IsNaN(source.Latitude.Value) && !Double.IsInfinity(source.Latitude.Value) &&
                !Double.IsNaN(source.Longitude.Value) && !Double.IsInfinity(source.Longitude.Value) &&
                source.Latitude.Value >= -90 && source.Latitude.Value <= 90 &&
                source.Longitude.Value >= -180 && source.Longitude.Value <= 180)
            {
                sanitized.Latitude = source.Latitude;
                sanitized.Longitude = source.Longitude;
            }
            return sanitized;
        }
    }
}
