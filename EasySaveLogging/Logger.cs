using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySaveLogging
{
    // Enum representing the log format type
    public enum LogFormat
    {
        JSON,
        XML
    }

    // Enum representing debug log levels
    public enum DebugLogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    // Represents a single entry in the log.
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
        public string? TargetPath { get; set; }
        public long? FileSize { get; set; }
        public long? TransferTime { get; set; }  // ms
        public long? EncryptionTime { get; set; }  // ms, <0 = error
        public string Message { get; set; } = string.Empty;
        public string LogType { get; set; } = "INFO";
        public string ActionType { get; set; } = string.Empty;
        public string? BackupType { get; set; }  // Type de sauvegarde (Full ou Differential)

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BackupName))
                throw new ArgumentException("BackupName cannot be empty");
            if (string.IsNullOrWhiteSpace(Message))
                throw new ArgumentException("Message cannot be empty");
            if (string.IsNullOrWhiteSpace(ActionType))
                throw new ArgumentException("ActionType cannot be empty");
            if (Timestamp == default)
                Timestamp = DateTime.Now;
        }
    }

    // Represents a collection of log entries for XML serialization
    [XmlRoot("Logs")]
    public class LogEntryCollection
    {
        [XmlElement("LogEntry")]
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();
    }

    // Configuration for log rotation
    public class LogRotationConfig
    {
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB default
        public int MaxFiles { get; set; } = 5; // Keep 5 files by default
        public bool RotateByDate { get; set; } = true; // Rotate daily by default
    }

    // Singleton logger that writes entries to a file in JSON or XML format.
    public class Logger
    {
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private static readonly object _fileLock = new object();

        private string _logFilePath;
        private LogFormat _currentFormat;
        private DebugLogLevel _debugLevel = DebugLogLevel.Info;
        private static readonly string DEBUG_LOG_FILE = Path.Combine(AppContext.BaseDirectory, "debug.log");

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(LogEntry));

        private Logger()
        {
            _currentFormat = LogFormat.JSON;
            var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _logFilePath = Path.Combine(logDir, "log.json");
            InitializeLogFile();
        }

        public static Logger GetInstance()
        {
            lock (_lock)
            {
                return _instance ??= new Logger();
            }
        }

        public LogFormat CurrentFormat => _currentFormat;

        public string GetLogFilePath() => _logFilePath;

        public void SetDebugLevel(DebugLogLevel level)
        {
            _debugLevel = level;
            DebugLog($"Debug level set to {level}", DebugLogLevel.Info);
        }

        private static void DebugLog(string message, DebugLogLevel level = DebugLogLevel.Debug)
        {
            if (level == DebugLogLevel.None) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(DEBUG_LOG_FILE, logMessage);
            }
            catch
            {
                // Ignore debug logging errors
            }
        }

        public void SetLogFilePath(string path)
        {
            lock (_fileLock)
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _logFilePath = path;
                InitializeLogFile();
            }
        }

        public void SetLogFormat(LogFormat format)
        {
            lock (_fileLock)
            {
                if (_currentFormat != format)
                {
                    _currentFormat = format;
                    string newPath = Path.ChangeExtension(_logFilePath, format == LogFormat.JSON ? ".json" : ".xml");
                    if (newPath != _logFilePath)
                    {
                        _logFilePath = newPath;
                        InitializeLogFile();
                    }
                }
            }
        }

        private void InitializeLogFile()
        {
            if (!File.Exists(_logFilePath))
            {
                if (_currentFormat == LogFormat.JSON)
                {
                    File.WriteAllText(_logFilePath, "[]");
                }
                else
                {
                    using var writer = new StreamWriter(_logFilePath, false, System.Text.Encoding.UTF8);
                    writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    writer.WriteLine("<Logs></Logs>");
                }
            }
        }

        public void AddLogEntry(LogEntry entry)
        {
            try
            {
                entry.Validate();
                lock (_fileLock)
                {
                    switch (_currentFormat)
                    {
                        case LogFormat.JSON:
                            AppendJsonLog(entry);
                            break;
                        case LogFormat.XML:
                            AppendXmlLog(entry);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error adding log entry: {ex.Message}", DebugLogLevel.Error);
                throw new InvalidOperationException($"Failed to add log entry: {ex.Message}", ex);
            }
        }

        private void AppendJsonLog(LogEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, _jsonOpts);
            string content = File.ReadAllText(_logFilePath);
            var entries = JsonSerializer.Deserialize<List<LogEntry>>(content, _jsonOpts) ?? new List<LogEntry>();
            entries.Add(entry);
            File.WriteAllText(_logFilePath, JsonSerializer.Serialize(entries, _jsonOpts));
        }

        private void AppendXmlLog(LogEntry entry)
        {
            string content = File.ReadAllText(_logFilePath);
            content = content.Replace("</Logs>", "");
            using var writer = new StreamWriter(_logFilePath, false, System.Text.Encoding.UTF8);
            writer.Write(content);
            _xmlSerializer.Serialize(writer, entry);
            writer.WriteLine();
            writer.Write("</Logs>");
        }

        public void CreateLog(string backupName, TimeSpan transferTime, long fileSize, DateTime date,
            string sourcePath, string targetPath, string logType, long encryptionTime)
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
                Message = logType == "ERROR" ? "Error during transfer" : "File transferred",
                LogType = logType,
                ActionType = "FILE_TRANSFER"
            };

            AddLogEntry(entry);
        }

        public void LogAdminAction(string backupName, string actionType, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName ?? string.Empty,
                Message = message,
                LogType = "INFO",
                ActionType = actionType
            };

            AddLogEntry(entry);
        }

        public List<LogEntry> ReadAllEntries()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_logFilePath))
                {
                    return new List<LogEntry>();
                }

                try
                {
                    string content = File.ReadAllText(_logFilePath);
                    if (string.IsNullOrEmpty(content))
                    {
                        return new List<LogEntry>();
                    }

                    if (_currentFormat == LogFormat.JSON)
                    {
                        return JsonSerializer.Deserialize<List<LogEntry>>(content, _jsonOpts) ?? new List<LogEntry>();
                    }
                    else
                    {
                        using var reader = new StringReader(content);
                        var entries = new List<LogEntry>();
                        while (reader.Peek() != -1)
                        {
                            var entry = _xmlSerializer.Deserialize(reader) as LogEntry;
                            if (entry != null)
                            {
                                entries.Add(entry);
                            }
                        }
                        return entries;
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error reading log entries: {ex.Message}", DebugLogLevel.Error);
                    return new List<LogEntry>();
                }
            }
        }

        public void DisplayLogs(string? backupName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = ReadAllEntries();
                var filteredEntries = entries.Where(e =>
                {
                    if (!string.IsNullOrEmpty(backupName) && e.BackupName != backupName)
                        return false;
                    if (startDate.HasValue && e.Timestamp < startDate.Value)
                        return false;
                    if (endDate.HasValue && e.Timestamp > endDate.Value)
                        return false;
                    return true;
                });

                foreach (var entry in filteredEntries)
                {
                    Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.LogType} - {entry.BackupName}");
                    Console.WriteLine($"Action: {entry.ActionType}");
                    Console.WriteLine($"Message: {entry.Message}");
                    if (entry.FileSize > 0)
                    {
                        Console.WriteLine($"File Size: {FormatFileSize(entry.FileSize.Value)}");
                    }
                    if (entry.TransferTime > 0)
                    {
                        Console.WriteLine($"Transfer Time: {entry.TransferTime}ms");
                    }
                    Console.WriteLine("----------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying logs: {ex.Message}");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}