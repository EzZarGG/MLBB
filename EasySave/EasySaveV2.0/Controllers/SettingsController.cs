using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;

namespace EasySaveV2._0.Controllers
{
    public class SettingsController
    {
        private const string SETTINGS_FILE = "settings.json";
        private Settings _settings;

        public SettingsController()
        {
            _settings = new Settings();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    var json = File.ReadAllText(SETTINGS_FILE);
                    _settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
            }
            catch
            {
                _settings = new Settings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch
            {
                // Log error if needed
            }
        }

        public List<string> GetBusinessSoftware()
        {
            return _settings.BusinessSoftware.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
        }

        public void AddBusinessSoftware(string softwareName)
        {
            var software = GetBusinessSoftware();
            if (!software.Contains(softwareName))
            {
                software.Add(softwareName);
                _settings.BusinessSoftware = string.Join(",", software);
                SaveSettings();
            }
        }

        public void RemoveBusinessSoftware(string softwareName)
        {
            var software = GetBusinessSoftware();
            if (software.Remove(softwareName))
            {
                _settings.BusinessSoftware = string.Join(",", software);
                SaveSettings();
            }
        }

        public List<string> GetEncryptionExtensions()
        {
            return _settings.EncryptedExtensions.ToList();
        }

        public void AddEncryptionExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.Trim().ToLower();
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                _settings.EncryptedExtensions.Add(extension);
                SaveSettings();
            }
        }

        public void RemoveEncryptionExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.Trim().ToLower();
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                if (_settings.EncryptedExtensions.Remove(extension))
                    SaveSettings();
            }
        }

        public bool IsBusinessSoftwareRunning()
        {
            var runningSoftware = GetRunningBusinessSoftware();
            return !string.IsNullOrEmpty(runningSoftware);
        }

        public string GetRunningBusinessSoftware()
        {
            var businessSoftware = GetBusinessSoftware();
            var processes = Process.GetProcesses();

            foreach (var software in businessSoftware)
            {
                if (processes.Any(p => p.ProcessName.Equals(software, System.StringComparison.OrdinalIgnoreCase)))
                    return software;
            }

            return string.Empty;
        }
    }

    public class Settings
    {
        public string BusinessSoftware { get; set; } = "";
        public HashSet<string> EncryptedExtensions { get; set; } = new HashSet<string>();
    }
} 