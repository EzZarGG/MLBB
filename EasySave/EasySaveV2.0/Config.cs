using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using EasySaveLogging;
using EasySaveV2._0.Models;

namespace EasySaveV2._0
{
    /// <summary>
    /// Manages application configuration and settings.
    /// Handles loading and saving of backup jobs, application settings, and encryption configuration.
    /// Provides centralized access to file paths and environment variables.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// JSON serialization options for consistent formatting and naming.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Gets the path to the main configuration file.
        /// </summary>
        public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "config.json");

        /// <summary>
        /// Gets the path to the application settings file.
        /// </summary>
        public static string AppSettingsFilePath => Path.Combine(AppContext.BaseDirectory, "appsetting.json");

        /// <summary>
        /// Gets the directory path for log files.
        /// Uses environment variable EASYSAVE_LOG_DIR if set, otherwise defaults to application directory.
        /// </summary>
        /// <returns>Path to the log directory</returns>
        public static string GetLogDirectory()
        {
            var dir = Environment.GetEnvironmentVariable("EASYSAVE_LOG_DIR")
                   ?? Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Gets the path to the state file.
        /// Uses environment variable EASYSAVE_STATE_DIR if set, otherwise defaults to application directory.
        /// </summary>
        /// <returns>Path to the state file</returns>
        public static string GetStateFilePath()
        {
            var dir = Environment.GetEnvironmentVariable("EASYSAVE_STATE_DIR")
                   ?? Path.Combine(AppContext.BaseDirectory, "State");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "states.json");
        }

        /// <summary>
        /// Loads the list of backup jobs from the configuration file.
        /// </summary>
        /// <returns>List of configured backup jobs, empty list if file doesn't exist</returns>
        public static List<Backup> LoadJobs()
        {
            if (!File.Exists(ConfigFilePath))
                return new List<Backup>();
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<List<Backup>>(json, _jsonOpts)
                   ?? new List<Backup>();
        }

        /// <summary>
        /// Saves the list of backup jobs to the configuration file.
        /// </summary>
        /// <param name="jobs">List of backup jobs to save</param>
        public static void SaveJobs(List<Backup> jobs)
        {
            var json = JsonSerializer.Serialize(jobs, _jsonOpts);
            File.WriteAllText(ConfigFilePath, json);
        }

        /// <summary>
        /// Application settings structure containing global configuration options.
        /// </summary>
        public class AppSettings
        {
            /// <summary>
            /// Format for log files (JSON or XML).
            /// Defaults to JSON if not specified.
            /// </summary>
            public string LogFormat { get; set; } = "JSON";

            /// <summary>
            /// Encryption key for securing backup files.
            /// Must be at least 8 bytes when converted to UTF8.
            /// </summary>
            public string EncryptionKey { get; set; }

            /// <summary>
            /// List of file extensions that should be encrypted during backup.
            /// Extensions can be specified with or without the leading dot.
            /// </summary>
            public List<string> EncryptionExtensions { get; set; } = new();

            /// <summary>
            /// Path to the CryptoSoft executable for XOR encryption.
            /// </summary>
            public string CryptoSoftPath { get; set; }
        }

        /// <summary>
        /// Loads application settings from the settings file.
        /// Returns default settings if file doesn't exist or is invalid.
        /// </summary>
        /// <returns>Application settings object</returns>
        public static AppSettings LoadSettings()
        {
            if (!File.Exists(AppSettingsFilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(AppSettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOpts);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves application settings to the settings file.
        /// </summary>
        /// <param name="settings">Settings object to save</param>
        public static void SaveSettings(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOpts);
            File.WriteAllText(AppSettingsFilePath, json);
        }

        /// <summary>
        /// Gets the configured log format from application settings.
        /// </summary>
        /// <returns>LogFormat enum value (JSON or XML)</returns>
        public static LogFormat GetLogFormat()
        {
            var settings = LoadSettings();
            return settings.LogFormat.ToUpper() == "XML" ? LogFormat.XML : LogFormat.JSON;
        }

        /// <summary>
        /// Updates the log format in application settings.
        /// </summary>
        /// <param name="format">New log format to use</param>
        public static void SetLogFormat(LogFormat format)
        {
            var settings = LoadSettings();
            settings.LogFormat = format.ToString();
            SaveSettings(settings);
        }

        /// <summary>
        /// Gets the encryption key from application settings.
        /// </summary>
        /// <returns>Encryption key as byte array, empty array if not configured</returns>
        /// <exception cref="InvalidOperationException">Thrown when key is less than 8 bytes</exception>
        public static byte[] GetEncryptionKey()
        {
            var key = LoadSettings().EncryptionKey;
            if (string.IsNullOrWhiteSpace(key))
                return Array.Empty<byte>();

            var b = Encoding.UTF8.GetBytes(key);
            if (b.Length < 8)
                throw new InvalidOperationException("The key must be at least 8 bytes long.");
            return b;
        }

        /// <summary>
        /// Gets the list of file extensions that should be encrypted.
        /// Normalizes extensions to lowercase and ensures they start with a dot.
        /// </summary>
        /// <returns>Set of normalized file extensions</returns>
        public static HashSet<string> GetEncryptionExtensions()
        {
            return LoadSettings()
                       .EncryptionExtensions
                       .Select(e => e.StartsWith(".") ? e.ToLower() : "." + e.ToLower())
                       .ToHashSet();
        }

        /// <summary>
        /// Gets the path to the CryptoSoft executable from application settings.
        /// </summary>
        /// <returns>Path to CryptoSoft executable, or empty string if not set</returns>
        public static string GetCryptoSoftPath()
        {
            var path = LoadSettings().CryptoSoftPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // Vérifier si le chemin existe
            if (!File.Exists(path))
            {
                // Essayer de trouver le fichier dans le dossier de l'application
                var appPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(path));
                if (File.Exists(appPath))
                {
                    return appPath;
                }
                return string.Empty;
            }

            return path;
        }
    }
}
