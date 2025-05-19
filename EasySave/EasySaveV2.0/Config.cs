using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using EasySaveLogging;
using EasySaveV2._0.Models;

namespace EasySaveV2._0
{
    public static class Config
    {
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "config.json");
        public static string AppSettingsFilePath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");


        public static string GetLogDirectory()
        {
            var dir = Environment.GetEnvironmentVariable("EASYSAVE_LOG_DIR")
                   ?? Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetStateFilePath()
        {
            var dir = Environment.GetEnvironmentVariable("EASYSAVE_STATE_DIR")
                   ?? Path.Combine(AppContext.BaseDirectory, "State");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "state.json");
        }

        public static List<Backup> LoadJobs()
        {
            if (!File.Exists(ConfigFilePath))
                return new List<Backup>();
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<List<Backup>>(json, _jsonOpts)
                   ?? new List<Backup>();
        }

        public static void SaveJobs(List<Backup> jobs)
        {
            var json = JsonSerializer.Serialize(jobs, _jsonOpts);
            File.WriteAllText(ConfigFilePath, json);
        }

        // App settings structure
        public class AppSettings
        {
            public string LogFormat { get; set; } = "JSON"; // Default to JSON
            public string EncryptionKey { get; set; }

            public List<string> EncryptionExtensions { get; set; } = new();
        }

        // Load application settings
        public static AppSettings LoadSettings()
        {
            if (!File.Exists(AppSettingsFilePath))
                return new AppSettings(); // Will use default JSON format

            try
            {
                var json = File.ReadAllText(AppSettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOpts);
                return settings ?? new AppSettings(); // Fallback to default JSON format
            }
            catch
            {
                return new AppSettings(); // Fallback to default JSON format
            }
        }

        // Save application settings
        public static void SaveSettings(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOpts);
            File.WriteAllText(AppSettingsFilePath, json);
        }

        // Get the configured log format
        public static LogFormat GetLogFormat()
        {
            var settings = LoadSettings();
            return settings.LogFormat.ToUpper() == "XML" ? LogFormat.XML : LogFormat.JSON; // Default to JSON if not XML
        }

        // Set the log format
        public static void SetLogFormat(LogFormat format)
        {
            var settings = LoadSettings();
            settings.LogFormat = format.ToString();
            SaveSettings(settings);
        }


        /// <summary>
        /// Retourne la clé de chiffrement (UTF8) ou null si non configurée.
        /// </summary>
        public static byte[] GetEncryptionKey()
        {
            var key = LoadSettings().EncryptionKey;
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var b = Encoding.UTF8.GetBytes(key);
            if (b.Length < 8)
                throw new InvalidOperationException("La clé doit faire au moins 8 octets.");
            return b;
        }

        /// <summary>
        /// Liste des extensions (avec '.') à chiffrer.
        /// </summary>
        public static HashSet<string> GetEncryptionExtensions()
        {
            return LoadSettings()
                       .EncryptionExtensions
                       .Select(e => e.StartsWith(".") ? e.ToLower() : "." + e.ToLower())
                       .ToHashSet();
        }
    }

}
