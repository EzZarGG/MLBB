using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Linq;

namespace EasySaveLogging
{
    // Enum representing the log format type
    public enum LogFormat
    {
        JSON,
        XML
    }

    // Represents a single entry in the log.
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public long FileSize { get; set; }
        public long TransferTime { get; set; }  // ms
        public long EncryptionTime { get; set; }  // ms, <0 = erreur
        public string Message { get; set; }
        public string LogType { get; set; }
        public string ActionType { get; set; }
    }


    // Represents a collection of log entries for XML serialization
    [XmlRoot("Logs")]
    public class LogEntryCollection
    {
        [XmlElement("LogEntry")]
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();
    }

    // Singleton logger that writes entries to a file in JSON or XML format.
    public class Logger
    {
        // Lazy singleton instance
        private static readonly Lazy<Logger> _instance =
            new Lazy<Logger>(() => new Logger());

        // Path to the log file
        private string _logFilePath;

        // Current log format (default is JSON)
        private LogFormat _logFormat = LogFormat.JSON;

        // JSON serializer options (indented, camel-case properties)
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // XML serializer for log entries
        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(LogEntryCollection));

        // Private constructor to enforce singleton pattern
        private Logger() { }

        // Gets the singleton instance of the Logger.
        public static Logger GetInstance() => _instance.Value;

        // Gets the current log format.
        public LogFormat CurrentFormat => _logFormat;

        // Sets the format for writing logs.
        // <param name="format">The log format to use</param>
        public void SetLogFormat(LogFormat format)
        {
            if (_logFormat != format)
            {
                _logFormat = format;

                // Convert existing logs if file exists
                if (File.Exists(_logFilePath))
                {
                    ConvertLogFileFormat();
                }
                else
                {
                    // Create a new empty log file with the selected format
                    InitializeLogFile();
                }

                // Log the format change
                LogAdminAction("System", "FORMAT_CHANGE", $"Log format changed to {format}");
            }
        }

        // Sets the file path for the log file. Creates directory and file if they do not exist.
        public void SetLogFilePath(string path)
        {
            _logFilePath = path;
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            InitializeLogFile();
        }

        // Initializes the log file with an empty structure based on the current format.
        private void InitializeLogFile()
        {
            if (File.Exists(_logFilePath))
                return;

            if (_logFormat == LogFormat.JSON)
            {
                File.WriteAllText(_logFilePath, "[]"); // Initialize with empty JSON array
            }
            else // XML format
            {
                using var writer = new StreamWriter(_logFilePath);
                _xmlSerializer.Serialize(writer, new LogEntryCollection());
            }
        }

        // Converts the log file from one format to another.
        private void ConvertLogFileFormat()
        {
            // Read all entries from the current file
            var entries = ReadAllEntries();

            // Change file extension based on format
            string newFilePath = Path.ChangeExtension(_logFilePath, _logFormat == LogFormat.JSON ? ".json" : ".xml");

            // Save in the new format
            if (_logFormat == LogFormat.JSON)
            {
                File.WriteAllText(newFilePath, JsonSerializer.Serialize(entries, _jsonOpts));
            }
            else // XML format
            {
                using var writer = new StreamWriter(newFilePath);
                var collection = new LogEntryCollection { Entries = entries };
                _xmlSerializer.Serialize(writer, collection);
            }

            // If the new file path is different, update the file path
            if (newFilePath != _logFilePath)
            {
                // Try to delete the old file
                try
                {
                    if (File.Exists(_logFilePath))
                        File.Delete(_logFilePath);
                }
                catch
                {
                    // Silently ignore deletion errors
                }

                // Update the file path
                _logFilePath = newFilePath;
            }
        }

        // Creates a log entry for a file transfer operation.
        public void CreateLog(string backupName,
                              TimeSpan transferTime,
                              long fileSize,
                              DateTime date,
                              string sourcePath,
                              string targetPath,
                              string logType,
                              long encryptionTime)
        {
            var entry = new LogEntry
            {
                Timestamp = date,
                BackupName = backupName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTime = (long)transferTime.TotalMilliseconds,
                EncryptionTime = encryptionTime,
                Message = logType == "ERROR"
                                 ? "Error during transfer"
                                 : "File transferred",
                LogType = logType,
                ActionType = "FILE_TRANSFER"
            };

            AddLogEntry(entry);
        }

        // Logs an administrative action (e.g., start/stop backup, configuration changes).
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

        // Adds a new entry to the log file in a thread-safe manner.
        private void AddLogEntry(LogEntry entry)
        {
            lock (_instance)
            {
                var entries = ReadAllEntries();
                entries.Add(entry);
                SaveEntries(entries);
            }
        }

        // Reads all entries from the log file.
        private List<LogEntry> ReadAllEntries()
        {
            if (!File.Exists(_logFilePath))
                return new List<LogEntry>();

            try
            {
                if (_logFormat == LogFormat.JSON)
                {
                    var json = File.ReadAllText(_logFilePath);
                    return JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOpts)
                           ?? new List<LogEntry>();
                }
                else // XML format
                {
                    using var reader = new StreamReader(_logFilePath);
                    if (reader.BaseStream.Length == 0)
                        return new List<LogEntry>();

                    var collection = (LogEntryCollection)_xmlSerializer.Deserialize(reader);
                    return collection?.Entries ?? new List<LogEntry>();
                }
            }
            catch
            {
                // If there's an error reading the file, return an empty list
                return new List<LogEntry>();
            }
        }

        // Saves all entries to the log file.
        private void SaveEntries(List<LogEntry> entries)
        {
            if (_logFormat == LogFormat.JSON)
            {
                File.WriteAllText(_logFilePath, JsonSerializer.Serialize(entries, _jsonOpts));
            }
            else // XML format
            {
                using var writer = new StreamWriter(_logFilePath);
                var collection = new LogEntryCollection { Entries = entries };
                _xmlSerializer.Serialize(writer, collection);
            }
        }

        // Reads the entire log file and writes the contents to the console.
        public void DisplayLogs()
        {
            if (File.Exists(_logFilePath))
            {
                var content = File.ReadAllText(_logFilePath);
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine("No log file found.");
            }
        }
    }
}