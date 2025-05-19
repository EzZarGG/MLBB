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

        public LogController()
        {
            _logger = Logger.GetInstance();
            _languageManager = LanguageManager.Instance;
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            try
            {
                DebugLog("InitializeLogger - Starting initialization");
                
                // Lire le format depuis les settings
                var format = Config.GetLogFormat();
                DebugLog($"InitializeLogger - Format from config: {format}");
                
                // Ne définir le format que s'il est différent
                if (_logger.CurrentFormat != format)
                {
                    DebugLog($"InitializeLogger - Changing format from {_logger.CurrentFormat} to {format}");
                    _logger.SetLogFormat(format);
                }
                DebugLog($"InitializeLogger - Current format: {_logger.CurrentFormat}");
                
                // Utiliser le même chemin que dans Program.cs
                string logFileName = "log" + (format == LogFormat.JSON ? ".json" : ".xml");
                string logPath = Path.Combine(AppContext.BaseDirectory, "Logs", logFileName);
                DebugLog($"InitializeLogger - Setting log path to: {logPath}");
                
                // Supprimer l'ancien fichier application.json s'il existe
                string oldLogPath = Path.Combine(AppContext.BaseDirectory, "Logs", "application.json");
                if (File.Exists(oldLogPath))
                {
                    DebugLog($"InitializeLogger - Deleting old log file: {oldLogPath}");
                    try
                    {
                        File.Delete(oldLogPath);
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"InitializeLogger - Error deleting old log file: {ex.Message}");
                    }
                }
                
                // Ne pas réinitialiser le chemin si c'est déjà le bon
                if (_logger.CurrentFormat == format && File.Exists(logPath))
                {
                    DebugLog("InitializeLogger - Log file already exists with correct format");
                    return;
                }
                
                // Créer le répertoire si nécessaire
                var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    DebugLog("InitializeLogger - Creating log directory");
                    Directory.CreateDirectory(logDir);
                }
                
                // Initialiser le fichier avec le bon format
                _logger.SetLogFilePath(logPath);
                DebugLog("InitializeLogger - Initialization complete");
            }
            catch (Exception ex)
            {
                DebugLog($"InitializeLogger - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to initialize logger: {ex.Message}", ex);
            }
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
                // Lire le format depuis les settings
                var settingsController = new SettingsController();
                var format = settingsController.GetCurrentLogFormat();
                string logFileName = format == LogFormat.JSON ? "log.json" : "log.xml";
                string logPath = Path.Combine(Config.GetLogDirectory(), logFileName);
                // Créer le fichier s'il n'existe pas
                if (!File.Exists(logPath))
                {
                    if (format == LogFormat.JSON)
                        File.WriteAllText(logPath, "[]");
                    else
                        File.WriteAllText(logPath, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Logs></Logs>");
                }
                Process.Start("notepad.exe", logPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{_languageManager.GetTranslation("message.errorOpeningLogs")}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetLogFormat(LogFormat format)
        {
            try
            {
                DebugLog($"SetLogFormat - Changing format to: {format}");
                
                // Sauvegarder d'abord le format dans la config
                Config.SetLogFormat(format);
                
                // Définir le format dans le logger
                _logger.SetLogFormat(format);
                
                // Mettre à jour le chemin du fichier avec la nouvelle extension
                string logFileName = "log" + (format == LogFormat.JSON ? ".json" : ".xml");
                string logPath = Path.Combine(Config.GetLogDirectory(), logFileName);
                DebugLog($"SetLogFormat - Setting log path to: {logPath}");
                
                // Supprimer l'ancien fichier application.json s'il existe
                string oldLogPath = Path.Combine(Config.GetLogDirectory(), "application.json");
                if (File.Exists(oldLogPath))
                {
                    DebugLog($"SetLogFormat - Deleting old log file: {oldLogPath}");
                    try
                    {
                        File.Delete(oldLogPath);
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"SetLogFormat - Error deleting old log file: {ex.Message}");
                    }
                }
                
                // Mettre à jour le chemin du fichier
                _logger.SetLogFilePath(logPath);
                DebugLog("SetLogFormat - Format change complete");
            }
            catch (Exception ex)
            {
                DebugLog($"SetLogFormat - Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to set log format: {ex.Message}", ex);
            }
        }

        public LogFormat GetCurrentLogFormat()
        {
            return _logger.CurrentFormat;
        }
    }
}