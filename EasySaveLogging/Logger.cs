using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides comprehensive logging functionality for the EasySave application.
/// Supports multiple log formats (JSON/XML), log rotation, and debug logging.
/// Implements the Singleton pattern for global access to logging services.
/// </summary>
namespace EasySaveLogging
{
    /// <summary>
    /// Defines the supported formats for log file storage.
    /// </summary>
    public enum LogFormat
    {
        /// <summary>JavaScript Object Notation format</summary>
        JSON,
        /// <summary>Extensible Markup Language format</summary>
        XML
    }

    /// <summary>
    /// Defines the severity levels for debug logging.
    /// Higher values indicate more detailed logging.
    /// </summary>
    public enum DebugLogLevel
    {
        /// <summary>No debug logging</summary>
        None = 0,
        /// <summary>Only error messages</summary>
        Error = 1,
        /// <summary>Errors and warnings</summary>
        Warning = 2,
        /// <summary>Errors, warnings, and general information</summary>
        Info = 3,
        /// <summary>All debug messages including detailed information</summary>
        Debug = 4
    }

    /// <summary>
    /// Represents a single log entry in the system.
    /// Contains detailed information about backup operations and system events.
    /// </summary>
    public class LogEntry
    {
        /// <summary>When the event occurred</summary>
        public DateTime Timestamp { get; set; }
        /// <summary>Name of the backup job associated with this log entry</summary>
        public string BackupName { get; set; } = string.Empty;
        /// <summary>Source path of the backup operation</summary>
        public string? SourcePath { get; set; }
        /// <summary>Target path of the backup operation</summary>
        public string? TargetPath { get; set; }
        /// <summary>Size of the processed file in bytes</summary>
        public long? FileSize { get; set; }
        /// <summary>Time taken for file transfer in milliseconds</summary>
        public long? TransferTime { get; set; }
        /// <summary>Time taken for encryption in milliseconds (negative values indicate errors)</summary>
        public long? EncryptionTime { get; set; }
        /// <summary>Detailed message describing the event</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Severity level of the log entry (INFO, ERROR, etc.)</summary>
        public string LogType { get; set; } = "INFO";
        /// <summary>Type of action performed (BACKUP_START, FILE_TRANSFER, etc.)</summary>
        public string ActionType { get; set; } = string.Empty;
        /// <summary>Type of backup operation (Full or Differential)</summary>
        public string? BackupType { get; set; }

        /// <summary>
        /// Validates the log entry to ensure all required fields are properly set.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when required fields are empty or invalid</exception>
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

    /// <summary>
    /// Collection of log entries for XML serialization.
    /// Used to maintain the root element structure in XML logs.
    /// </summary>
    [XmlRoot("Logs")]
    public class LogEntryCollection
    {
        /// <summary>List of log entries in the collection</summary>
        [XmlElement("LogEntry")]
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();
    }

    /// <summary>
    /// Configuration settings for log file rotation.
    /// Controls when and how log files are archived and cleaned up.
    /// </summary>
    public class LogRotationConfig
    {
        /// <summary>Maximum size of a log file before rotation (default: 10MB)</summary>
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        /// <summary>Maximum number of archived log files to keep (default: 5)</summary>
        public int MaxFiles { get; set; } = 5;
        /// <summary>Whether to rotate logs daily (default: true)</summary>
        public bool RotateByDate { get; set; } = true;
    }

    /// <summary>
    /// Singleton logger implementation that manages application logging.
    /// Supports multiple log formats, log rotation, and debug logging levels.
    /// Thread-safe operations for concurrent access.
    /// </summary>
    public class Logger
    {
        // Singleton instance and synchronization
        private static Logger? _instance;
        private static readonly object _lock = new object();
        private static readonly object _fileLock = new object();

        // Logging configuration
        private string _logFilePath;
        private LogFormat _currentFormat;
        private DebugLogLevel _debugLevel = DebugLogLevel.Info;
        private static readonly string DEBUG_LOG_FILE = Path.Combine(AppContext.BaseDirectory, "debug.log");

        // Serialization settings
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        private readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(LogEntry));

        /// <summary>
        /// Private constructor for singleton pattern.
        /// Initializes logging directory and default log file.
        /// </summary>
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

        /// <summary>
        /// Gets the singleton instance of the logger.
        /// Thread-safe implementation.
        /// </summary>
        /// <returns>The singleton logger instance</returns>
        public static Logger GetInstance()
        {
            lock (_lock)
            {
                return _instance ??= new Logger();
            }
        }

        /// <summary>
        /// Gets the current log file format.
        /// </summary>
        public LogFormat CurrentFormat => _currentFormat;

        /// <summary>
        /// Gets the current log file path.
        /// </summary>
        /// <returns>Path to the current log file</returns>
        public string GetLogFilePath() => _logFilePath;

        /// <summary>
        /// Sets the debug logging level.
        /// Controls the verbosity of debug messages.
        /// </summary>
        /// <param name="level">The new debug logging level</param>
        public void SetDebugLevel(DebugLogLevel level)
        {
            _debugLevel = level;
            DebugLog($"Debug level set to {level}", DebugLogLevel.Info);
        }

        /// <summary>
        /// Writes a debug message to the debug log file.
        /// Respects the current debug level setting.
        /// </summary>
        /// <param name="message">The debug message to log</param>
        /// <param name="level">The severity level of the debug message</param>
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
                // Silently fail for debug logging to prevent cascading errors
            }
        }

        /// <summary>
        /// Sets a new path for the log file.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="path">The new path for the log file</param>
        public void SetLogFilePath(string path)
        {
            lock (_fileLock)
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                _logFilePath = path;
                InitializeLogFile();
            }
        }

        /// <summary>
        /// Changes the log file format and updates the file extension accordingly.
        /// </summary>
        /// <param name="format">The new log format to use</param>
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

        /// <summary>
        /// Initializes a new log file with the appropriate format structure.
        /// Creates the file if it doesn't exist.
        /// </summary>
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

        /// <summary>
        /// Adds a new log entry to the current log file.
        /// Thread-safe operation that handles both JSON and XML formats.
        /// </summary>
        /// <param name="entry">The log entry to add</param>
        /// <exception cref="InvalidOperationException">Thrown when log entry addition fails</exception>
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

        /// <summary>
        /// Appends a log entry to a JSON format log file.
        /// Maintains the JSON array structure.
        /// </summary>
        /// <param name="entry">The log entry to append</param>
        private void AppendJsonLog(LogEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, _jsonOpts);
            string content = File.ReadAllText(_logFilePath);
            var entries = JsonSerializer.Deserialize<List<LogEntry>>(content, _jsonOpts) ?? new List<LogEntry>();
            entries.Add(entry);
            File.WriteAllText(_logFilePath, JsonSerializer.Serialize(entries, _jsonOpts));
        }

        /// <summary>
        /// Appends a log entry to an XML format log file.
        /// Maintains proper XML structure and formatting.
        /// </summary>
        /// <param name="entry">The log entry to append</param>
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

        /// <summary>
        /// Creates and adds a log entry for a file transfer operation.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="transferTime">Time taken for the transfer</param>
        /// <param name="fileSize">Size of the transferred file</param>
        /// <param name="date">When the transfer occurred</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        /// <param name="logType">Type of log entry (INFO/ERROR)</param>
        /// <param name="encryptionTime">Time taken for encryption (if applicable)</param>
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

        /// <summary>
        /// Creates and adds a log entry for an administrative action.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="actionType">Type of administrative action</param>
        /// <param name="message">Description of the action</param>
        public void LogAdminAction(string backupName, string actionType, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName,
                Message = message,
                LogType = "INFO",
                ActionType = actionType
            };

            AddLogEntry(entry);
        }

        /// <summary>
        /// Reads all log entries from the current log file.
        /// </summary>
        /// <returns>List of all log entries</returns>
        /// <exception cref="InvalidOperationException">Thrown when log file reading fails</exception>
        public List<LogEntry> ReadAllEntries()
        {
            try
            {
                lock (_fileLock)
                {
                    if (!File.Exists(_logFilePath))
                        return new List<LogEntry>();

                    string content = File.ReadAllText(_logFilePath);
                    return _currentFormat switch
                    {
                        LogFormat.JSON => JsonSerializer.Deserialize<List<LogEntry>>(content, _jsonOpts) ?? new List<LogEntry>(),
                        LogFormat.XML => DeserializeXmlLogs(content),
                        _ => throw new InvalidOperationException($"Unsupported log format: {_currentFormat}")
                    };
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error reading log entries: {ex.Message}", DebugLogLevel.Error);
                throw new InvalidOperationException($"Failed to read log entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Displays log entries with optional filtering.
        /// </summary>
        /// <param name="backupName">Filter by backup job name</param>
        /// <param name="startDate">Filter by start date</param>
        /// <param name="endDate">Filter by end date</param>
        public void DisplayLogs(string? backupName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var entries = ReadAllEntries();
            var filtered = entries.Where(e =>
                (backupName == null || e.BackupName == backupName) &&
                (startDate == null || e.Timestamp >= startDate) &&
                (endDate == null || e.Timestamp <= endDate)
            ).OrderBy(e => e.Timestamp);

            foreach (var entry in filtered)
            {
                Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.LogType}] {entry.BackupName}: {entry.Message}");
                if (entry.FileSize.HasValue)
                    Console.WriteLine($"  Size: {FormatFileSize(entry.FileSize.Value)}");
                if (entry.TransferTime.HasValue)
                    Console.WriteLine($"  Transfer Time: {entry.TransferTime.Value}ms");
                if (entry.EncryptionTime.HasValue && entry.EncryptionTime.Value >= 0)
                    Console.WriteLine($"  Encryption Time: {entry.EncryptionTime.Value}ms");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Deserializes XML log entries from a string.
        /// </summary>
        /// <param name="content">XML content to deserialize</param>
        /// <returns>List of deserialized log entries</returns>
        private List<LogEntry> DeserializeXmlLogs(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<LogEntry>();

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

        /// <summary>
        /// Formats a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted string with appropriate unit</returns>
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