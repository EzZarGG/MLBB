using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySaveV2._0.Controllers
{
    public class SettingsController
    {
        private const string SETTINGS_FILE = "settings.json";
        private Settings _settings;

        public SettingsController()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
        }

        private void SaveSettings()
        {
        }

        public List<string> GetBusinessSoftware()
        {
            return new List<string>();
        }

        public void AddBusinessSoftware(string softwareName)
        {
        }

        public void RemoveBusinessSoftware(string softwareName)
        {
        }

        public List<string> GetEncryptionExtensions()
        {
            return new List<string>();
        }

        public void AddEncryptionExtension(string extension)
        {
        }

        public void RemoveEncryptionExtension(string extension)
        {
        }

        public bool IsBusinessSoftwareRunning()
        {
            return false;
        }

        public string GetRunningBusinessSoftware()
        {
            return string.Empty;
        }
    }

    public class Settings
    {
        public string BusinessSoftware { get; set; } = "";
        public HashSet<string> EncryptedExtensions { get; set; } = new HashSet<string>();
    }
} 