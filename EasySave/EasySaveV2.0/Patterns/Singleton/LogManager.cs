using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EasySaveV2._0.Patterns.Singleton
{
    public sealed class LogManager
    {
        private static LogManager _instance;
        private static readonly object _lock = new object();
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly object _logLock = new object();

        private LogManager()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _logFilePath = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.json");
            InitializeLogDirectory();
        }

        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private void InitializeLogDirectory()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task LogAsync(string message, LogLevel level = LogLevel.Info, string source = null)
        {
            var logEntry = new
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Source = source ?? "Application",
                Message = message
            };

            string logLine = System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine;

            try
            {
                await File.AppendAllTextAsync(_logFilePath, logLine);
            }
            catch (Exception ex)
            {
                // En cas d'erreur d'écriture, on écrit dans la console
                Console.WriteLine($"Erreur lors de l'écriture du log: {ex.Message}");
                Console.WriteLine($"Log manquant: {logLine}");
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string source = null)
        {
            var logEntry = new
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Source = source ?? "Application",
                Message = message
            };

            string logLine = System.Text.Json.JsonSerializer.Serialize(logEntry) + Environment.NewLine;

            try
            {
                lock (_logLock)
                {
                    File.AppendAllText(_logFilePath, logLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture du log: {ex.Message}");
                Console.WriteLine($"Log manquant: {logLine}");
            }
        }

        public async Task<string> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null, LogLevel? level = null)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                    return "[]";

                var logs = await File.ReadAllLinesAsync(_logFilePath);
                var filteredLogs = logs
                    .Select(line => System.Text.Json.JsonSerializer.Deserialize<dynamic>(line))
                    .Where(log => 
                        (!startDate.HasValue || log.Timestamp >= startDate.Value) &&
                        (!endDate.HasValue || log.Timestamp <= endDate.Value) &&
                        (!level.HasValue || log.Level == level.Value.ToString()))
                    .ToList();

                return System.Text.Json.JsonSerializer.Serialize(filteredLogs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture des logs: {ex.Message}");
                return "[]";
            }
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
} 