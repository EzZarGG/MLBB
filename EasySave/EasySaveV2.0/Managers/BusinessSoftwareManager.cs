using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace EasySave.Business
{
    public class BusinessSoftwareManager
    {
        private const string ConfigFile = "business_software.json";
        private readonly EasySaveLogging.Logger _logger = EasySaveLogging.Logger.GetInstance();

        public List<string> SoftwareNames { get; private set; } = new List<string>();

        public BusinessSoftwareManager()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!File.Exists(ConfigFile))
            {
                SoftwareNames = new List<string> { "Calculator" };
                SaveConfig();
                return;
            }
            var json = File.ReadAllText(ConfigFile);
            SoftwareNames = JsonSerializer.Deserialize<List<string>>(json)
                            ?? new List<string>();
        }

        public void SaveConfig()
        {
            var json = JsonSerializer.Serialize(SoftwareNames, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        public void Add(string processName)
        {
            if (!SoftwareNames.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                SoftwareNames.Add(processName);
                SaveConfig();
                _logger.LogAdminAction("Config", "ADD_BUSINESS_SOFTWARE", $"Ajout du logiciel métier '{processName}'");
            }
        }

        public void Remove(string processName)
        {
            if (SoftwareNames.RemoveAll(x => x.Equals(processName, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                SaveConfig();
                _logger.LogAdminAction("Config", "REMOVE_BUSINESS_SOFTWARE", $"Suppression du logiciel métier '{processName}'");
            }
        }

        public bool IsAnyRunning()
        {
            foreach (var name in SoftwareNames)
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    _logger.LogAdminAction("Backup", "BUSINESS_SOFTWARE_DETECTED", $"Détection du processus métier '{name}'");
                    return true;
                }
            }
            return false;
        }
    }
}
