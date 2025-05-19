using System;
using EasySaveLogging;
using EasySaveV2._0;
using EasySaveV2._0.Managers;

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
            // Configure the logger output path and format based on app settings
            _logger.SetLogFilePath(Path.Combine(Config.GetLogDirectory(), DateTime.Today.ToString("yyyy-MM-dd") + ".json"));
            _logger.SetLogFormat(Config.GetLogFormat());
        }

        public void LogAdminAction(string backupName, string action, string message)
        {
            _logger.LogAdminAction(backupName, action, message);
        }

        public void LogBackupStart(string backupName)
        {
            LogAdminAction(backupName, "EXECUTE_START", $"Started executing backup job: {backupName}");
        }

        public void LogBackupComplete(string backupName)
        {
            LogAdminAction(backupName, "EXECUTE_COMPLETE", $"Completed executing backup job: {backupName}");
        }

        public void LogBackupError(string backupName, string error)
        {
            LogAdminAction(backupName, "ERROR", $"Error during backup: {error}");
        }

        /// <summary>
        /// Logs a file transfer, including optional encryption time.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="sourcePath">Original file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="fileSize">Size of the file in bytes</param>
        /// <param name="transferTime">Time taken to transfer the file</param>
        /// <param name="encryptionTime">
        /// Time taken to encrypt the file (ms):
        /// 0 = no encryption, >0 = time in ms, <0 = error code
        /// </param>
        public void LogFileOperation(
            string backupName,
            string sourcePath,
            string destinationPath,
            long fileSize,
            TimeSpan transferTime,
            long encryptionTime)
        {
            _logger.CreateLog(
                backupName: backupName,
                transferTime: transferTime,
                fileSize: fileSize,
                date: DateTime.Now,
                sourcePath: sourcePath,
                targetPath: destinationPath,
                logType: "INFO",
                encryptionTime: encryptionTime
            );
        }

        public void LogBusinessSoftwareDetected(string softwareName)
        {
            LogAdminAction("System", "BUSINESS_SOFTWARE", $"Business software detected: {softwareName}");
        }

        public void LogEncryptionStart(string backupName)
        {
            LogAdminAction(backupName, "ENCRYPTION_START", $"Started encryption for backup: {backupName}");
        }

        public void LogEncryptionComplete(string backupName)
        {
            LogAdminAction(backupName, "ENCRYPTION_COMPLETE", $"Completed encryption for backup: {backupName}");
        }

        public void LogEncryptionError(string backupName, string error)
        {
            LogAdminAction(backupName, "ENCRYPTION_ERROR", $"Encryption error: {error}");
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
