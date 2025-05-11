using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySaveV1.EasySaveConsole.Models;

namespace EasySaveV1
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
    }
}