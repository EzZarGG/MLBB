using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySaveLogging;
using EasySaveV1._1.EasySaveConsole.Models;

namespace EasySaveV1._1
{
    public static class Config
    {
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string ConfigFilePath =>
            Path.Combine(AppContext.BaseDirectory, "config.json");

        public static string AppSettingsFilePath =>
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");

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
            public string LogFormat { get; set; } = "JSON";
        }

        // Load application settings
        public static AppSettings LoadSettings()
        {
            if (!File.Exists(AppSettingsFilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(AppSettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, _jsonOpts)
                       ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
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
            return settings.LogFormat.ToUpper() == "XML" ? LogFormat.XML : LogFormat.JSON;
        }

        // Set the log format
        public static void SetLogFormat(LogFormat format)
        {
            var settings = LoadSettings();
            settings.LogFormat = format.ToString();
            SaveSettings(settings);
        }
    }
}