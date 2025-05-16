using EasySaveLogging;
using System.Diagnostics;
using EasySaveV2._0.Managers;
using System;
using System.IO;
using System.Windows.Forms;

namespace EasySaveV2._0.Controllers
{
    public class LogController
    {
        private readonly Logger _logger;
        private readonly LanguageManager _languageManager;
        private const string LogDirectory = "Logs";
        private const string DefaultLogFileName = "log.json";

        public LogController()
        {
            _logger = Logger.GetInstance();
            _languageManager = LanguageManager.Instance;
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            string logPath = Path.Combine(LogDirectory, DefaultLogFileName);

            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            _logger.SetLogFilePath(logPath);
        }

        public void LogBackupStart(string backupName)
        {
            _logger.LogAdminAction(backupName, "BACKUP_START", "Backup started");
        }

        public void LogBackupComplete(string backupName)
        {
            _logger.LogAdminAction(backupName, "BACKUP_COMPLETE", "Backup completed successfully");
        }

        public void LogBackupError(string backupName, string error)
        {
            _logger.LogAdminAction(backupName, "BACKUP_ERROR", $"Error during backup: {error}");
        }

        public void LogFileOperation(string backupName, string sourcePath, string destinationPath, long fileSize)
        {
            _logger.CreateLog(
                backupName: backupName,
                transferTime: TimeSpan.Zero, // à remplacer par un temps réel si disponible
                fileSize: fileSize,
                date: DateTime.Now,
                sourcePath: sourcePath,
                targetPath: destinationPath,
                logType: "INFO"
            );
        }

        public void LogBusinessSoftwareDetected(string softwareName)
        {
            _logger.LogAdminAction("System", "BUSINESS_SOFTWARE_DETECTED", $"Business software detected: {softwareName}");
        }

        public void LogEncryptionStart(string backupName)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_START", "Encryption started");
        }

        public void LogEncryptionComplete(string backupName)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_COMPLETE", "Encryption completed");
        }

        public void LogEncryptionError(string backupName, string error)
        {
            _logger.LogAdminAction(backupName, "ENCRYPTION_ERROR", $"Error during encryption: {error}");
        }
        public void LogAdminAction(string backupName, string actionType, string message)
        {
            _logger.LogAdminAction(backupName, actionType, message);
        }

        public void DisplayLogs()
        {
            try
            {
                string logPath = Path.Combine(LogDirectory, Path.ChangeExtension(DefaultLogFileName, _logger.CurrentFormat == LogFormat.JSON ? ".json" : ".xml"));
                if (File.Exists(logPath))
                {
                    Process.Start("notepad.exe", logPath);
                }
                else
                {
                    MessageBox.Show(_languageManager.GetTranslation("NoLogsFound"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{_languageManager.GetTranslation("ErrorOpeningLogs")}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetLogFormat(LogFormat format)
        { 
            _logger.SetLogFormat(format);
        }

        public LogFormat GetCurrentLogFormat()
        {
            return _logger.CurrentFormat;
        }
    }
}
