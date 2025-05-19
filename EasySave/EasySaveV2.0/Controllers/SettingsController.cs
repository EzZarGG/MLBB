using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;

namespace EasySaveV2._0.Controllers
{
    public class SettingsController
    {
        private readonly string _settingsFile;
        private readonly JsonSerializerOptions _jsonOptions;
        private Settings _settings;

        public SettingsController()
        {
            _settingsFile = Path.Combine(AppContext.BaseDirectory, "settings.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    _settings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions);
                }
            }
            catch (Exception)
            {
                // If there's an error loading settings, create default settings
                _settings = new Settings();
            }

            if (_settings == null)
            {
                _settings = new Settings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception)
            {
                // Log error if needed
            }
        }

        public LogFormat GetCurrentLogFormat()
        {
            return _settings.LogFormat;
        }

        public void SetLogFormat(LogFormat format)
        {
            _settings.LogFormat = format;
            SaveSettings();
        }

        public List<string> GetBusinessSoftware()
        {
            return _settings.BusinessSoftware;
        }

        public void AddBusinessSoftware(string softwareName)
        {
            var software = GetBusinessSoftware();
            if (!software.Contains(softwareName))
            {
                software.Add(softwareName);
                _settings.BusinessSoftware = software;
                SaveSettings();
            }
        }

        public void RemoveBusinessSoftware(string softwareName)
        {
            var software = GetBusinessSoftware();
            if (software.Remove(softwareName))
            {
                _settings.BusinessSoftware = software;
                SaveSettings();
            }
        }

        public void SetBusinessSoftware(List<string> software)
        {
            _settings.BusinessSoftware = software;
            SaveSettings();
        }

        public List<string> GetEncryptionExtensions()
        {
            return _settings.EncryptionExtensions;
        }

        public void AddEncryptionExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.Trim().ToLower();
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                if (!_settings.EncryptionExtensions.Contains(extension))
                {
                    _settings.EncryptionExtensions.Add(extension);
                    SaveSettings();
                }
            }
        }

        public void RemoveEncryptionExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.Trim().ToLower();
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                if (_settings.EncryptionExtensions.Remove(extension))
                {
                    SaveSettings();
                }
            }
        }

        public void SetEncryptionExtensions(List<string> extensions)
        {
            _settings.EncryptionExtensions = extensions;
            SaveSettings();
        }

        public bool IsBusinessSoftwareRunning()
        {
            foreach (var software in _settings.BusinessSoftware)
            {
                if (IsProcessRunning(software))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetRunningBusinessSoftware()
        {
            var businessSoftware = GetBusinessSoftware();
            var processes = Process.GetProcesses();

            foreach (var software in businessSoftware)
            {
                if (processes.Any(p => p.ProcessName.Equals(software, StringComparison.OrdinalIgnoreCase)))
                    return software;
            }

            return string.Empty;
        }

        private bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool ShouldEncryptFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return _settings.EncryptionExtensions.Contains(extension);
        }

        private class Settings
        {
            public LogFormat LogFormat { get; set; } = LogFormat.JSON;
            public List<string> BusinessSoftware { get; set; } = new List<string>();
            public List<string> EncryptionExtensions { get; set; } = new List<string>();
        }
    }
}