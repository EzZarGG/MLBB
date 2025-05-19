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
            string logPath = Path.Combine(Config.GetLogDirectory(), DefaultLogFileName);

            if (!Directory.Exists(Config.GetLogDirectory()))
                Directory.CreateDirectory(Config.GetLogDirectory());

            _logger.SetLogFilePath(logPath);
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

        public void LogFileOperation(string backupName, string sourcePath, string targetPath, long fileSize)
        {
            _logger.CreateLog(
                backupName,
                TimeSpan.Zero, // Durée de transfert non mesurée ici
                fileSize,
                DateTime.Now,
                sourcePath,
                targetPath,
                "INFO",
                0 // Pas de chiffrement
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
            try
            {
                string logPath = Path.Combine(Config.GetLogDirectory(), Path.ChangeExtension(DefaultLogFileName, _logger.CurrentFormat == LogFormat.JSON ? ".json" : ".xml"));
                if (File.Exists(logPath))
                {
                    Process.Start("notepad.exe", logPath);
                }
                else
                {
                    MessageBox.Show(_languageManager.GetTranslation("message.noLogsFound"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{_languageManager.GetTranslation("message.errorOpeningLogs")}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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