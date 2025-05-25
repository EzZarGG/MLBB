using EasySaveLogging;
using System;
using System.IO;
using System.Windows.Forms;
using EasySaveV2._0.Views;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using System.Collections.Generic;

namespace EasySaveV2._0.Controllers
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
        }

        // Singleton instance tracking
        private static bool _isInitialized = false;

        // Dependencies
        private readonly Logger _logger;
        private readonly LanguageManager _languageManager;

        /// <summary>
        /// Initializes a new instance of the LogController class.
        /// Sets up the logger with appropriate format and file path.
        /// </summary>
        public LogController()
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
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = backupType,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    Message = message,
                    LogType = "INFO",
                    ActionType = message switch
                    {
                        "Backup started" => ActionTypes.BACKUP_STARTED,
                        "Backup paused" => ActionTypes.BACKUP_PAUSED,
                        "Backup resumed" => ActionTypes.BACKUP_RESUMED,
                        _ => ActionTypes.BACKUP_STARTED
                    }
                };
                _logger.AddLogEntry(logEntry);
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
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = backupType,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    Message = message,
                    LogType = "INFO",
                    ActionType = message switch
                    {
                        "Backup job created" => ActionTypes.BACKUP_CREATED,
                        "Backup job deleted" => ActionTypes.BACKUP_DELETED,
                        "Backup completed" => ActionTypes.BACKUP_COMPLETED,
                        "Backup stopped" => ActionTypes.BACKUP_STOPPED,
                        _ => ActionTypes.BACKUP_COMPLETED
                    }
                };
                _logger.AddLogEntry(logEntry);
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
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = backupType,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    Message = $"Error during backup: {error}",
                    LogType = "ERROR",
                    ActionType = ActionTypes.BACKUP_ERROR
                };
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
                    ? $"File encrypted and copied: {Path.GetFileName(sourcePath)}"
                    : $"File copied: {Path.GetFileName(sourcePath)}";

                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = backupType,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    FileSize = fileSize,
                    TransferTime = transferTime,
                    EncryptionTime = encryptionTime,
                    Message = message,
                    LogType = "INFO",
                    ActionType = actionType
                };
                _logger.AddLogEntry(entry);
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
            _logger.SetLogFormat(format);
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
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = backupType,
                    SourcePath = sourcePath,
                    TargetPath = targetPath,
                    Message = $"File skipped: {Path.GetFileName(sourcePath)} - {reason}",
                    LogType = "INFO",
                    ActionType = ActionTypes.FILE_SKIPPED
                };
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
                    changes.Add($"Source path: {oldBackup.SourcePath} → {newBackup.SourcePath}");
                    actionType = ActionTypes.BACKUP_UPDATED_SOURCE;
                }

                if (oldBackup.TargetPath != newBackup.TargetPath)
                {
                    changes.Add($"Target path: {oldBackup.TargetPath} → {newBackup.TargetPath}");
                    actionType = actionType == ActionTypes.BACKUP_UPDATED_SOURCE ? 
                        ActionTypes.BACKUP_UPDATED_MULTIPLE : ActionTypes.BACKUP_UPDATED_TARGET;
                }

                if (newBackup.Type != oldBackup.Type)
                {
                    changes.Add($"Backup type: {oldBackup.Type} → {newBackup.Type}");
                    actionType = actionType != ActionTypes.BACKUP_UPDATED ? 
                        ActionTypes.BACKUP_UPDATED_MULTIPLE : ActionTypes.BACKUP_UPDATED_TYPE;
                }

                var message = changes.Count > 0 
                    ? $"Backup updated - {string.Join(", ", changes)}"
                    : "Backup configuration reviewed (no changes)";

                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backupName,
                    BackupType = newBackup.Type,
                    SourcePath = newBackup.SourcePath,
                    TargetPath = newBackup.TargetPath,
                    Message = message,
                    LogType = "INFO",
                    ActionType = actionType
                };
                _logger.AddLogEntry(entry);
            }
            catch (Exception)
            {
                // Ignore logging errors to prevent cascading failures
            }
        }
    }
}