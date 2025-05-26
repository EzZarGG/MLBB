using EasySaveLogging;
using System;
using System.IO;
using System.Windows.Forms;
using EasySaveV3._0.Views;
using EasySaveV3._0.Managers;
using EasySaveV3._0.Models;
using System.Collections.Generic;

namespace EasySaveV3._0.Controllers
{
    /// <summary>
    /// Controller responsible for managing logging operations in the application.
    /// Handles log initialization, format management, and log entry creation.
    /// </summary>
    public class LogController
    {
        // Constants
        private const string LogDirectory = "Logs";
        private const string DefaultLogFileName = "log.json";
        private const long PERFORMANCE_WARNING_THRESHOLD_MS = 5000; // 5 seconds

        // Action Types
        private static class ActionTypes
        {
            public const string BACKUP_CREATED = "BACKUP_CREATED";
            public const string BACKUP_UPDATED = "BACKUP_UPDATED";
            public const string BACKUP_DELETED = "BACKUP_DELETED";
            public const string BACKUP_STARTED = "BACKUP_STARTED";
            public const string BACKUP_PAUSED = "BACKUP_PAUSED";
            public const string BACKUP_RESUMED = "BACKUP_RESUMED";
            public const string BACKUP_STOPPED = "BACKUP_STOPPED";
            public const string BACKUP_COMPLETED = "BACKUP_COMPLETED";
            public const string BACKUP_ERROR = "BACKUP_ERROR";
            public const string FILE_COPY = "FILE_COPY";
            public const string FILE_ENCRYPT = "FILE_ENCRYPT";
            public const string FILE_DECRYPT = "FILE_DECRYPT";
            public const string FILE_SKIPPED = "FILE_SKIPPED";
            public const string BACKUP_UPDATED_SOURCE = "BACKUP_UPDATED_SOURCE";
            public const string BACKUP_UPDATED_TARGET = "BACKUP_UPDATED_TARGET";
            public const string BACKUP_UPDATED_TYPE = "BACKUP_UPDATED_TYPE";
            public const string BACKUP_UPDATED_MULTIPLE = "BACKUP_UPDATED_MULTIPLE";
            public const string PERFORMANCE_WARNING = "PERFORMANCE_WARNING";
        }

        // Singleton instance
        private static LogController? _instance;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        // Dependencies
        private Logger _logger;
        private readonly LanguageManager _languageManager;

        /// <summary>
        /// Gets the singleton instance of LogController.
        /// </summary>
        public static LogController Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LogController();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private LogController()
        {
            _logger = Logger.GetInstance();
            _languageManager = LanguageManager.Instance;
            
            if (!_isInitialized)
            {
                InitializeLogger();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Resets the singleton instance.
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Initializes the logger with settings from configuration.
        /// Sets up log format and file path based on application settings.
        /// </summary>
        private void InitializeLogger()
        {
            try
            {
                var format = Config.GetLogFormat();
                
                // Update format only if different
                if (_logger.CurrentFormat != format)
                {
                    _logger.SetLogFormat(format);
                }
                
                // Ensure log directory exists
                string logDir = Path.Combine(AppContext.BaseDirectory, LogDirectory);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                // Update log file path if needed
                string currentPath = _logger.GetLogFilePath();
                string expectedPath = Path.Combine(logDir, "log" + (format == LogFormat.JSON ? ".json" : ".xml"));
                
                if (currentPath != expectedPath)
                {
                    _logger.SetLogFilePath(expectedPath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Creates a standardized log entry with common properties.
        /// </summary>
        private LogEntry CreateLogEntry(
            string backupName,
            string backupType,
            string? sourcePath,
            string? targetPath,
            string message,
            string logType,
            string actionType,
            long fileSize = -1,
            long transferTime = -1,
            long encryptionTime = -1)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName,
                BackupType = backupType,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                Message = message,
                LogType = logType,
                ActionType = actionType
            };

            // Only set optional fields if they are provided (>= 0)
            if (fileSize >= 0) entry.FileSize = fileSize;
            if (transferTime >= 0) entry.TransferTime = transferTime;
            if (encryptionTime >= 0) entry.EncryptionTime = encryptionTime;

            return entry;
        }

        /// <summary>
        /// Maps a message to its corresponding action type.
        /// </summary>
        private string MapMessageToActionType(string message)
        {
            return message switch
            {
                "Backup started" => ActionTypes.BACKUP_STARTED,
                "Backup paused" => ActionTypes.BACKUP_PAUSED,
                "Backup resumed" => ActionTypes.BACKUP_RESUMED,
                "Backup completed" => ActionTypes.BACKUP_COMPLETED,
                "Backup stopped" => ActionTypes.BACKUP_STOPPED,
                "Backup job created" => ActionTypes.BACKUP_CREATED,
                "Backup job deleted" => ActionTypes.BACKUP_DELETED,
                "Backup updated" => ActionTypes.BACKUP_UPDATED,
                "File copied" => ActionTypes.FILE_COPY,
                "File encrypted" => ActionTypes.FILE_ENCRYPT,
                "File decrypted" => ActionTypes.FILE_DECRYPT,
                "File skipped" => ActionTypes.FILE_SKIPPED,
                _ => ActionTypes.BACKUP_UPDATED
            };
        }

        /// <summary>
        /// Logs a performance warning if the operation time exceeds the threshold.
        /// </summary>
        private void LogPerformanceWarningIfNeeded(string backupName, long transferTime, long encryptionTime)
        {
            if (transferTime > PERFORMANCE_WARNING_THRESHOLD_MS || encryptionTime > PERFORMANCE_WARNING_THRESHOLD_MS)
            {
                var warningMessage = _languageManager.GetTranslation("warning.performanceIssue");
                var entry = CreateLogEntry(
                    backupName,
                    "Unknown",
                    null,
                    null,
                    warningMessage,
                    "WARNING",
                    ActionTypes.PERFORMANCE_WARNING
                );
                _logger.AddLogEntry(entry);
            }
        }

        /// <summary>
        /// Logs the start of a backup operation.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        public void LogBackupStart(string backupName, string backupType)
        {
            LogBackupStart(backupName, backupType, "Backup started", null, null);
        }

        /// <summary>
        /// Logs the start of a backup operation with a custom message.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="message">Custom message to log</param>
        public void LogBackupStart(string backupName, string backupType, string message)
        {
            LogBackupStart(backupName, backupType, message, null, null);
        }

        /// <summary>
        /// Logs the start of a backup operation with a custom message and source/target paths.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="message">Custom message to log</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        public void LogBackupStart(string backupName, string backupType, string message, string? sourcePath, string? targetPath)
        {
            try
            {
                var actionType = MapMessageToActionType(message);
                var entry = CreateLogEntry(
                    backupName,
                    backupType,
                    sourcePath,
                    targetPath,
                    _languageManager.GetTranslation(message),
                    "INFO",
                    actionType
                );
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Logs the completion of a backup operation.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        public void LogBackupComplete(string backupName, string backupType)
        {
            LogBackupComplete(backupName, backupType, "Backup completed", null, null);
        }

        /// <summary>
        /// Logs the completion of a backup operation with a custom message.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="message">Custom message to log</param>
        public void LogBackupComplete(string backupName, string backupType, string message)
        {
            LogBackupComplete(backupName, backupType, message, null, null);
        }

        /// <summary>
        /// Logs the completion of a backup operation with a custom message and source/target paths.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="message">Custom message to log</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        public void LogBackupComplete(string backupName, string backupType, string message, string? sourcePath, string? targetPath)
        {
            try
            {
                var actionType = MapMessageToActionType(message);
                var entry = CreateLogEntry(
                    backupName,
                    backupType,
                    sourcePath,
                    targetPath,
                    _languageManager.GetTranslation(message),
                    "INFO",
                    actionType
                );
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Logs an error that occurred during a backup operation.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="error">Error message to log</param>
        public void LogBackupError(string backupName, string backupType, string error)
        {
            LogBackupError(backupName, backupType, error, null, null);
        }

        /// <summary>
        /// Logs an error that occurred during a backup operation with source/target paths.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="error">Error message to log</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        public void LogBackupError(string backupName, string backupType, string error, string? sourcePath, string? targetPath)
        {
            try
            {
                var entry = CreateLogEntry(
                    backupName,
                    backupType,
                    sourcePath,
                    targetPath,
                    _languageManager.GetTranslation("error.backupFailed", error),
                    "ERROR",
                    ActionTypes.BACKUP_ERROR
                );
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Logs a file operation with detailed information.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="backupType">Type of backup (Full or Differential)</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        /// <param name="fileSize">Size of the file in bytes</param>
        /// <param name="transferTime">Time taken for transfer in milliseconds</param>
        /// <param name="encryptionTime">Time taken for encryption in milliseconds (-1 if not encrypted)</param>
        public void LogFileOperation(string backupName, string backupType, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime = -1)
        {
            try
            {
                var actionType = encryptionTime >= 0 ? ActionTypes.FILE_ENCRYPT : ActionTypes.FILE_COPY;
                var message = encryptionTime >= 0 
                    ? _languageManager.GetTranslation("message.fileEncryptedAndCopied", Path.GetFileName(sourcePath))
                    : _languageManager.GetTranslation("message.fileCopied", Path.GetFileName(sourcePath));

                var entry = CreateLogEntry(
                    backupName,
                    backupType,
                    sourcePath,
                    targetPath,
                    message,
                    "INFO",
                    actionType,
                    fileSize,
                    transferTime,
                    encryptionTime
                );
                _logger.AddLogEntry(entry);

                // Log performance warning if needed
                LogPerformanceWarningIfNeeded(backupName, transferTime, encryptionTime);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Displays the log viewer form with optional filtering.
        /// </summary>
        /// <param name="backupName">Optional backup name to filter logs</param>
        /// <param name="startDate">Optional start date to filter logs</param>
        /// <param name="endDate">Optional end date to filter logs</param>
        public void DisplayLogs(string? backupName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var logViewer = new LogViewerForm(_logger, _languageManager))
                {
                    logViewer.ShowDialog();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the format for log files (JSON or XML).
        /// </summary>
        /// <param name="format">Desired log format</param>
        public void SetLogFormat(LogFormat format)
        {
            lock (_lock)
            {
                // Reset both logger and log controller instances
                Logger.ResetInstance();
                ResetInstance();
                
                // Create new instance with updated format
                var newInstance = new LogController();
                newInstance._logger.SetLogFormat(format);
                
                // Update the instance reference
                _instance = newInstance;
            }
        }

        /// <summary>
        /// Sets the path for the log file.
        /// </summary>
        /// <param name="path">Path where log file should be stored</param>
        public void SetLogFilePath(string path)
        {
            _logger.SetLogFilePath(path);
        }

        /// <summary>
        /// Gets the current format of log files.
        /// </summary>
        /// <returns>Current log format (JSON or XML)</returns>
        public LogFormat GetCurrentLogFormat()
        {
            return _logger.CurrentFormat;
        }

        /// <summary>
        /// Logs when a file is skipped during backup (e.g., in differential backup when file hasn't changed).
        /// </summary>
        public void LogFileSkipped(string backupName, string backupType, string sourcePath, string targetPath, string reason)
        {
            try
            {
                var entry = CreateLogEntry(
                    backupName,
                    backupType,
                    sourcePath,
                    targetPath,
                    _languageManager.GetTranslation("message.fileSkipped", Path.GetFileName(sourcePath), reason),
                    "INFO",
                    ActionTypes.FILE_SKIPPED
                );
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }

        /// <summary>
        /// Logs the update of a backup job with detailed change information.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="oldBackup">Original backup configuration</param>
        /// <param name="newBackup">Updated backup configuration</param>
        public void LogBackupUpdate(string backupName, Backup oldBackup, Backup newBackup)
        {
            try
            {
                var changes = new List<string>();
                var actionType = ActionTypes.BACKUP_UPDATED;

                if (oldBackup.SourcePath != newBackup.SourcePath)
                {
                    changes.Add(_languageManager.GetTranslation("message.sourcePathChanged", oldBackup.SourcePath, newBackup.SourcePath));
                    actionType = ActionTypes.BACKUP_UPDATED_SOURCE;
                }

                if (oldBackup.TargetPath != newBackup.TargetPath)
                {
                    changes.Add(_languageManager.GetTranslation("message.targetPathChanged", oldBackup.TargetPath, newBackup.TargetPath));
                    actionType = actionType == ActionTypes.BACKUP_UPDATED_SOURCE ? 
                        ActionTypes.BACKUP_UPDATED_MULTIPLE : ActionTypes.BACKUP_UPDATED_TARGET;
                }

                if (newBackup.Type != oldBackup.Type)
                {
                    changes.Add(_languageManager.GetTranslation("message.backupTypeChanged", oldBackup.Type, newBackup.Type));
                    actionType = actionType != ActionTypes.BACKUP_UPDATED ? 
                        ActionTypes.BACKUP_UPDATED_MULTIPLE : ActionTypes.BACKUP_UPDATED_TYPE;
                }

                var message = changes.Count > 0 
                    ? $"Backup updated - {string.Join(", ", changes)}"
                    : "Backup configuration reviewed (no changes)";

                var entry = CreateLogEntry(
                    backupName,
                    newBackup.Type,
                    newBackup.SourcePath,
                    newBackup.TargetPath,
                    message,
                    "INFO",
                    actionType
                );
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }
    }
}