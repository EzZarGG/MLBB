using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EasySaveV2._0.Models;

namespace EasySaveV2._0.Patterns.Strategy
{
    public class JsonLogStrategy : ILogStrategy
    {
        private readonly string _logDirectory;
        private readonly object _logLock = new object();

        public JsonLogStrategy()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            InitializeLogDirectory();
        }

        private void InitializeLogDirectory()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.json");
        }

        public async Task WriteLogAsync(string message, LogLevel level, string source)
        {
            var logEntry = new
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Source = source ?? "Application",
                Message = message
            };

            string logLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
            string logFilePath = GetLogFilePath();

            try
            {
                await File.AppendAllTextAsync(logFilePath, logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture du log JSON: {ex.Message}");
                Console.WriteLine($"Log manquant: {logLine}");
            }
        }

        public async Task<string> ReadLogsAsync(DateTime? startDate = null, DateTime? endDate = null, LogLevel? level = null)
        {
            string logFilePath = GetLogFilePath();
            try
            {
                if (!File.Exists(logFilePath))
                    return "[]";

                var logs = await File.ReadAllLinesAsync(logFilePath);
                var logEntries = logs
                    .Select(line => JsonSerializer.Deserialize<LogEntry>(line))
                    .Where(log => 
                        (!startDate.HasValue || log.Timestamp >= startDate.Value) &&
                        (!endDate.HasValue || log.Timestamp <= endDate.Value) &&
                        (!level.HasValue || log.Level == level.Value.ToString()))
                    .ToList();

                return JsonSerializer.Serialize(logEntries, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture des logs JSON: {ex.Message}");
                return "[]";
            }
        }

        public string GetFileExtension() => ".json";

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string Source { get; set; }
            public string Message { get; set; }
        }
    }
} 