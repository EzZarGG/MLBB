using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySaveV1.EasySaveLogging
{
    
    /// Represents a single entry in the log.
    
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

    
    /// Singleton logger that writes entries to a JSON file.
    
    public class Logger
    {
        // Lazy singleton instance
        private static readonly Lazy<Logger> _instance =
            new Lazy<Logger>(() => new Logger());

        // Path to the JSON log file
        private string _logFilePath;

        // JSON serializer options (indented, camel-case properties)
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Private constructor to enforce singleton pattern
        private Logger() { }

        
        /// Gets the singleton instance of the Logger.
        
        public static Logger GetInstance() => _instance.Value;

        
        /// Sets the file path for the log file. Creates directory and file if they do not exist.
        
        public void SetLogFilePath(string path)
        {
            _logFilePath = path;
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, "[]"); // Initialize with empty JSON array
        }

        /// Creates a log entry for a file transfer operation.
       
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
                Message = logType == "ERROR"
                                 ? "Error during transfer"
                                 : "File transferred",
                LogType = logType,
                ActionType = "FILE_TRANSFER"
            };

            AddLogEntry(entry);
        }

        
        /// Logs an administrative action (e.g., start/stop backup, configuration changes).
        
        public void LogAdminAction(string backupName, string actionType, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName ?? string.Empty,
                SourcePath = string.Empty,
                TargetPath = string.Empty,
                FileSize = 0,
                TransferTime = 0,
                Message = message,
                LogType = "INFO",
                ActionType = actionType
            };

            AddLogEntry(entry);
        }

        
        /// Adds a new entry to the JSON log file in a thread-safe manner.
       
        private void AddLogEntry(LogEntry entry)
        {
            lock (_instance)
            {
                // Read existing entries
                var json = File.ReadAllText(_logFilePath);
                var list = JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOpts)
                           ?? new List<LogEntry>();

                // Append and save back to file
                list.Add(entry);
                File.WriteAllText(
                    _logFilePath,
                    JsonSerializer.Serialize(list, _jsonOpts)
                );
            }
        }

        
        /// Reads the entire log file and writes the JSON to the console.
        
        public void DisplayLogs()
        {
            var json = File.ReadAllText(_logFilePath);
            Console.WriteLine(json);
        }
    }
}
