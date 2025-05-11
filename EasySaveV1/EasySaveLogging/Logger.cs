using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Logger
{
    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public long FileSize { get; set; }
        public long TransferTime { get; set; }
        public string Message { get; set; }
        public string LogType { get; set; }
        public string ActionType { get; set; } 
    }

    public class Logger
    {
        private static readonly Lazy<Logger> _instance =
            new Lazy<Logger>(() => new Logger());

        private string _logFilePath;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private Logger() { }

        public static Logger GetInstance() => _instance.Value;

        public void SetLogFilePath(string path)
        {
            _logFilePath = path;
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, "[]");
        }

        public void CreateLog(string backupName,
                              TimeSpan transferTime,
                              long fileSize,
                              DateTime date,
                              string sourcePath,
                              string targetPath,
                              string logType)
        {
            var entry = new LogEntry
            {
                Timestamp = date,
                BackupName = backupName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTime = (long)transferTime.TotalMilliseconds,
                Message = logType == "ERROR" ? "Error during transfer" : "File transferred",
                LogType = logType,
                ActionType = "FILE_TRANSFER"
            };

            AddLogEntry(entry);
        }

        // Méthode pour journaliser les opérations administratives
        public void LogAdminAction(string backupName, string actionType, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName ?? "",
                SourcePath = "",
                TargetPath = "",
                FileSize = 0,
                TransferTime = 0,
                Message = message,
                LogType = "INFO",
                ActionType = actionType
            };

            AddLogEntry(entry);
        }

        // Méthode commune pour ajouter une entrée de log
        private void AddLogEntry(LogEntry entry)
        {
            lock (_instance)
            {
                var json = File.ReadAllText(_logFilePath);
                var list = JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOpts)
                           ?? new List<LogEntry>();
                list.Add(entry);
                File.WriteAllText(_logFilePath,
                    JsonSerializer.Serialize(list, _jsonOpts));
            }
        }

        public void DisplayLogs()
        {
            var json = File.ReadAllText(_logFilePath);
            Console.WriteLine(json);
        }
    }
}