using System;
using System.IO;
using System.Text.Json;
using DesktopLyrics.Models;

namespace DesktopLyrics.Services
{
    public class SettingsService
    {
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string SettingsFolder = Path.Combine(AppDataPath, "LyricBar");
        private static readonly string LegacySettingsFolder = Path.Combine(AppDataPath, "DesktopLyrics");

        private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");
        private static readonly string LegacySettingsFilePath = Path.Combine(LegacySettingsFolder, "settings.json");

        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            Settings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                // 1. Check new LyricBar settings file
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }

                // 2. Fallback to legacy DesktopLyrics settings if upgrading
                if (File.Exists(LegacySettingsFilePath))
                {
                    var json = File.ReadAllText(LegacySettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        // Migrate to new folder
                        SaveSettings();
                        return settings;
                    }
                }
            }
            catch { }

            return new AppSettings();
        }

        public void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }
    }
}
