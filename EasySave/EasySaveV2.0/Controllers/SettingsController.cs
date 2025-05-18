using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;



namespace EasySaveV2._0.Controllers
{
    public class SettingsController
    {
        private readonly string _settingsFile;
        private readonly JsonSerializerOptions _jsonOptions;
        private Settings _settings;
        public Config.AppSettings Load() => Config.LoadSettings();

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
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            File.WriteAllText(_settingsFile, json);
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

        public void SetBusinessSoftware(List<string> software)
        {
            _settings.BusinessSoftware = software;
            SaveSettings();
        }

        public IEnumerable<string> GetEncryptionExtensions() =>
            Load().EncryptionExtensions;

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

        private bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
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
        public void SaveEncryptionKey(string key)
        {
            var s = Config.LoadSettings();
            s.EncryptionKey = key;
            Config.SaveSettings(s);
        }

        public void SaveExtensions(IEnumerable<string> exts)
        {
            var s = Config.LoadSettings();
            s.EncryptionExtensions = exts.ToList();
            Config.SaveSettings(s);
        }
        public void SetEncryptionKey(string key)
        {
            var s = Load();
            s.EncryptionKey = key;
            Config.SaveSettings(s);
        }

        public void SetEncryptionExtensions(IEnumerable<string> exts)
        {
            var s = Load();
            s.EncryptionExtensions = exts.ToList();
            Config.SaveSettings(s);
        }
    }
} 