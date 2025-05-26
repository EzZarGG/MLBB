using System;
using System.IO;
using System.Text.Json;

namespace EasySaveV3._0.Patterns.Singleton
{
    public sealed class ConfigurationManager
    {
        private static ConfigurationManager _instance;
        private static readonly object _lock = new object();
        private string _configPath;
        private dynamic _config;

        private ConfigurationManager()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsetting.json");
            LoadConfiguration();
        }

        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string jsonString = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<dynamic>(jsonString);
                }
                else
                {
                    _config = new { };
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur
                Console.WriteLine($"Erreur lors du chargement de la configuration: {ex.Message}");
                _config = new { };
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, jsonString);
            }
            catch (Exception ex)
            {
                // Log l'erreur
                Console.WriteLine($"Erreur lors de la sauvegarde de la configuration: {ex.Message}");
            }
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                // Implémentation de la récupération de valeur
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                // Implémentation de la définition de valeur
                SaveConfiguration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la définition de la valeur: {ex.Message}");
            }
        }
    }
} 