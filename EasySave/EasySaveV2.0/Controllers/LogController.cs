using EasySaveLogging;
using System.Diagnostics;
using EasySaveV2._0.Managers;
using System;
using System.Windows.Forms;

namespace EasySaveV2._0.Controllers
{
    public class LogController
    {
        private readonly Logger _logger;
        private readonly LanguageManager _languageManager;

        public LogController()
        {
            _logger = Logger.GetInstance();
            _languageManager = LanguageManager.Instance;
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            _logger.SetLogFilePath(Config.GetLogDirectory() + "/logs.json");
            _logger.SetLogFormat(Config.GetLogFormat());
        }

        public void LogAdminAction(string backupName, string action, string message)
        {
            _logger.LogAdminAction(backupName, action, message);
        }

        public void LogBackupStart(string backupName)
        {
            _logger.LogAdminAction(backupName, "EXECUTE_START", $"Started executing backup job: {backupName}");
        }

        public void LogBackupComplete(string backupName)
        {
            _logger.LogAdminAction(backupName, "EXECUTE_COMPLETE", $"Completed executing backup job: {backupName}");
        }

        public void LogBackupError(string backupName, string error)
        {
            _logger.LogAdminAction(backupName, "ERROR", $"Error during backup: {error}");
        }

        public void LogFileOperation(string backupName, string sourcePath, string destinationPath, long fileSize)
        {
            _logger.CreateLog(
                backupName,
                TimeSpan.Zero, // Transfer time will be calculated by the backup process
                fileSize,
                DateTime.Now,
                sourcePath,
                destinationPath,
                "INFO"
            );
        }

        public void LogBusinessSoftwareDetected(string softwareName)
        {
            _logger.LogAdminAction("System", "BUSINESS_SOFTWARE", $"Business software detected: {softwareName}");
        }

        public void LogEncryptionStart(string backupName)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_START", $"Started encryption for backup: {backupName}");
        }

        public void LogEncryptionComplete(string backupName)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_COMPLETE", $"Completed encryption for backup: {backupName}");
        }

        public void LogEncryptionError(string backupName, string error)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_ERROR", $"Encryption error: {error}");
        }

        public void DisplayLogs()
        {
            _logger.DisplayLogs();
        }

        public void SetLogFormat(LogFormat format)
        {
            _logger.SetLogFormat(format);
            Config.SetLogFormat(format);
        }

        public LogFormat GetCurrentLogFormat()
        {
            return _logger.CurrentFormat;
        }
    }
} 