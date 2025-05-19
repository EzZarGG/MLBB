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

        private static readonly string DEBUG_LOG_FILE = Path.Combine(AppContext.BaseDirectory, "debug.log");

        private static void DebugLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(DEBUG_LOG_FILE, logMessage);
            }
            catch
            {
                // Ignore debug logging errors
            }
        }

        // Private constructor to enforce singleton pattern
        private Logger() 
        { 
            // Initialize with default path
            var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _logFilePath = Path.Combine(logDir, "log.json");
            DebugLog($"Logger constructor - Initialized with path: {_logFilePath}");
        }

        // Gets the singleton instance of the Logger.
        public static Logger GetInstance() => _instance.Value;

        // Gets the current log format.
        public LogFormat CurrentFormat => _logFormat;

        // Sets the file path for the log file. Creates directory and file if they do not exist.
        public void SetLogFilePath(string path)
        {
            DebugLog($"SetLogFilePath - Setting path to: {path}");
            
            // Ensure the directory exists with correct case
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                DebugLog($"SetLogFilePath - Creating directory: {dir}");
                Directory.CreateDirectory(dir);
            }
            
            // If the path has changed, we need to convert the file
            if (_logFilePath != null && _logFilePath != path && File.Exists(_logFilePath))
            {
                DebugLog($"SetLogFilePath - Converting from {_logFilePath} to {path}");
                ConvertLogFileFormat();
            }
            
            // Update the file path
            _logFilePath = path;
            DebugLog($"SetLogFilePath - Updated file path to: {_logFilePath}");
            
            // Initialize the file if it doesn't exist
            InitializeLogFile();
        }

        // Sets the format for writing logs.
        public void SetLogFormat(LogFormat format)
        {
            DebugLog($"SetLogFormat - Changing format from {_logFormat} to {format}");
            
            if (_logFormat != format)
            {
                // Store the old path
                string oldPath = _logFilePath;
                
                // Update the format
                _logFormat = format;
                
                // If we have a file path, update it with the new extension
                if (!string.IsNullOrEmpty(oldPath))
                {
                    string newPath = Path.ChangeExtension(oldPath, format == LogFormat.JSON ? ".json" : ".xml");
                    DebugLog($"SetLogFormat - Updating path from {oldPath} to {newPath}");
                    
                    // Convert the file if it exists
                    if (File.Exists(oldPath))
                    {
                        DebugLog("SetLogFormat - Converting existing log file");
                        ConvertLogFileFormat();
                    }
                    else
                    {
                        _logFilePath = newPath;
                        DebugLog("SetLogFormat - Initializing new log file");
                        InitializeLogFile();
                    }
                }
                
                // Log the format change
                LogAdminAction("System", "FORMAT_CHANGE", $"Log format changed to {format}");
            }
            else
            {
                DebugLog("SetLogFormat - Format unchanged");
            }
        }

        // Initializes the log file with an empty structure based on the current format.
        private void InitializeLogFile()
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                DebugLog("InitializeLogFile - No file path set, initializing default path");
                var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                _logFilePath = Path.Combine(logDir, "log.json");
            }

            if (File.Exists(_logFilePath))
            {
                DebugLog($"InitializeLogFile - File already exists: {_logFilePath}");
                return;
            }

            try
            {
                DebugLog($"InitializeLogFile - Creating new file: {_logFilePath}");
                if (_logFormat == LogFormat.JSON)
                {
                    File.WriteAllText(_logFilePath, "[]"); // Initialize with empty JSON array
                    DebugLog("InitializeLogFile - Created empty JSON file");
                }
                else // XML format
                {
                    var collection = new LogEntryCollection();
                    using (var writer = new StreamWriter(_logFilePath, false, System.Text.Encoding.UTF8))
                    {
                        _xmlSerializer.Serialize(writer, collection);
                        DebugLog("InitializeLogFile - Created empty XML file");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"InitializeLogFile - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to initialize log file: {ex.Message}", ex);
            }
        }

        // Converts the log file from one format to another.
        private void ConvertLogFileFormat()
        {
            try
            {
                DebugLog("ConvertLogFileFormat - Starting conversion");
                
                // Lire les entrées existantes
                var entries = ReadAllEntries();
                DebugLog($"ConvertLogFileFormat - Read {entries.Count} entries");
                
                // Déterminer le nouveau chemin de fichier
                string newFilePath = Path.ChangeExtension(_logFilePath, _logFormat == LogFormat.JSON ? ".json" : ".xml");
                DebugLog($"ConvertLogFileFormat - New file path: {newFilePath}");
                
                // Sauvegarder dans le nouveau format
                if (_logFormat == LogFormat.JSON)
                {
                    var json = JsonSerializer.Serialize(entries, _jsonOpts);
                    File.WriteAllText(newFilePath, json);
                    DebugLog("ConvertLogFileFormat - Saved in JSON format");
                }
                else // XML format
                {
                    var collection = new LogEntryCollection { Entries = entries };
                    using (var writer = new StreamWriter(newFilePath, false, System.Text.Encoding.UTF8))
                    {
                        _xmlSerializer.Serialize(writer, collection);
                        DebugLog("ConvertLogFileFormat - Saved in XML format");
                    }
                }
                
                // Supprimer l'ancien fichier si le chemin a changé
                if (newFilePath != _logFilePath && File.Exists(_logFilePath))
                {
                    try
                    {
                        File.Delete(_logFilePath);
                        DebugLog("ConvertLogFileFormat - Deleted old file");
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"ConvertLogFileFormat - Error deleting old file: {ex.Message}");
                    }
                }
                
                // Mettre à jour le chemin du fichier
                _logFilePath = newFilePath;
                DebugLog("ConvertLogFileFormat - Conversion complete");
            }
            catch (Exception ex)
            {
                DebugLog($"ConvertLogFileFormat - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to convert log file format: {ex.Message}", ex);
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
                try
                {
                    DebugLog($"AddLogEntry - Current format: {_logFormat}, File path: {_logFilePath}");
                    
                    // Read existing entries
                    var entries = ReadAllEntries();
                    DebugLog($"AddLogEntry - Read {entries.Count} existing entries");
                    
                    // Add new entry
                    entries.Add(entry);
                    DebugLog("AddLogEntry - Added new entry");
                    
                    // Save all entries in the current format
                    if (_logFormat == LogFormat.JSON)
                    {
                        var json = JsonSerializer.Serialize(entries, _jsonOpts);
                        File.WriteAllText(_logFilePath, json);
                        DebugLog("AddLogEntry - Saved in JSON format");
                    }
                    else // XML format
                    {
                        var collection = new LogEntryCollection { Entries = entries };
                        using (var writer = new StreamWriter(_logFilePath, false, System.Text.Encoding.UTF8))
                        {
                            _xmlSerializer.Serialize(writer, collection);
                            DebugLog("AddLogEntry - Saved in XML format");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"AddLogEntry - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    throw new InvalidOperationException($"Failed to add log entry: {ex.Message}", ex);
                }
            }
        }

        // Reads all entries from the log file.
        private List<LogEntry> ReadAllEntries()
        {
            if (!File.Exists(_logFilePath))
            {
                DebugLog("ReadAllEntries - File does not exist");
                return new List<LogEntry>();
            }

            try
            {
                DebugLog($"ReadAllEntries - Reading from {_logFilePath}");
                if (_logFormat == LogFormat.JSON)
                {
                    var json = File.ReadAllText(_logFilePath);
                    var entries = JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOpts) ?? new List<LogEntry>();
                    DebugLog($"ReadAllEntries - Read {entries.Count} entries from JSON");
                    return entries;
                }
                else // XML format
                {
                    using (var reader = new StreamReader(_logFilePath, System.Text.Encoding.UTF8))
                    {
                        if (reader.BaseStream.Length == 0)
                        {
                            DebugLog("ReadAllEntries - XML file is empty");
                            return new List<LogEntry>();
                        }

                        var collection = _xmlSerializer.Deserialize(reader) as LogEntryCollection;
                        var entries = collection?.Entries ?? new List<LogEntry>();
                        DebugLog($"ReadAllEntries - Read {entries.Count} entries from XML");
                        return entries;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"ReadAllEntries - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return new List<LogEntry>();
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