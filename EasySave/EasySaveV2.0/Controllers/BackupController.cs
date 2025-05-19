using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasySaveV2._0;

namespace EasySaveV2._0.Controllers
{
    public class BackupController
    {
        private readonly BackupManager _backupManager;
        private readonly SettingsController _settingsController;
        private readonly LogController _logController;
        private readonly LanguageManager _languageManager;

        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;

        public BackupController()
        {
            try
            {
                _backupManager = new BackupManager();
                _settingsController = new SettingsController();
                _logController = new LogController();
                _languageManager = LanguageManager.Instance;

                if (_backupManager == null || _settingsController == null || _logController == null || _languageManager == null)
                {
                    throw new InvalidOperationException("Failed to initialize required components");
                }

                // Subscribe to BackupManager events
                _backupManager.FileProgressChanged += OnFileProgress;
                _backupManager.EncryptionProgressChanged += OnEncryptionProgress;
            }
            catch (Exception ex)
            {
                LoggerUtils.EnsureLoggerInitialized();
                Logger.GetInstance().LogAdminAction("System", "ERROR", $"Error initializing BackupController: {ex.Message}");
                throw;
            }
        }

        public void CreateBackup(string name, string sourcePath, string destinationPath, string type)
        {
            try
            {
                var backup = new Backup
                {
                    Name = name,
                    SourcePath = sourcePath,
                    TargetPath = destinationPath,
                    Type = type,
                    FileLength = 0
                };

                if (!_backupManager.AddJob(backup))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupExists"));
                }

                _logController.LogBackupStart(name);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error creating backup: {ex.Message}");
                throw;
            }
        }

        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
            try
            {
                var existingBackup = _backupManager.GetJob(name);
                if (existingBackup == null)
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }

                var updatedBackup = new Backup
                {
                    Name = name,
                    SourcePath = sourcePath,
                    TargetPath = destinationPath,
                    Type = type,
                    FileLength = existingBackup.FileLength
                };

                if (!_backupManager.UpdateJob(name, updatedBackup))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }

                _logController.LogBackupStart(name);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error editing backup: {ex.Message}");
                throw;
            }
        }

        public void DeleteBackup(string name)
        {
            try
            {
                if (!_backupManager.RemoveJob(name))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }

                _logController.LogBackupComplete(name);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error deleting backup: {ex.Message}");
                throw;
            }
        }

        public async Task StartBackup(string name)
        {
            try
            {
                if (_settingsController.IsBusinessSoftwareRunning())
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.businessSoftwareRunning"));
                }

                _logController.LogBackupStart(name);
                await _backupManager.ExecuteJob(name);
                _logController.LogBackupComplete(name);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error starting backup: {ex.Message}");
                throw;
            }
        }

        public void PauseBackup(string name)
        {
            try
            {
                var state = _backupManager.GetJobState(name);
                if (state != null && state.Status == "Active")
                {
                    _logController.LogAdminAction(name, "PAUSE", "Backup paused");
                    // TODO: Implement actual pause functionality
                }
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error pausing backup: {ex.Message}");
                throw;
            }
        }

        public void ResumeBackup(string name)
        {
            try
            {
                var state = _backupManager.GetJobState(name);
                if (state != null && state.Status == "Paused")
                {
                    _logController.LogAdminAction(name, "RESUME", "Backup resumed");
                    // TODO: Implement actual resume functionality
                }
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error resuming backup: {ex.Message}");
                throw;
            }
        }

        public void StopBackup(string name)
        {
            try
            {
                var state = _backupManager.GetJobState(name);
                if (state != null && (state.Status == "Active" || state.Status == "Paused"))
                {
                    _logController.LogAdminAction(name, "STOP", "Backup stopped");
                    // TODO: Implement actual stop functionality
                }
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error stopping backup: {ex.Message}");
                throw;
            }
        }

        public List<Backup> GetBackups()
        {
            try
            {
                if (_backupManager == null)
                {
                    throw new InvalidOperationException("BackupManager is not initialized");
                }
                return new List<Backup>(_backupManager.Jobs);
            }
            catch (Exception ex)
            {
                _logController?.LogAdminAction("System", "ERROR", $"Error getting backups: {ex.Message}");
                throw;
            }
        }

        public Backup GetBackup(string name)
        {
            try
            {
                if (_backupManager == null)
                {
                    throw new InvalidOperationException("BackupManager is not initialized");
                }
                return _backupManager.GetJob(name);
            }
            catch (Exception ex)
            {
                _logController?.LogAdminAction(name, "ERROR", $"Error getting backup: {ex.Message}");
                throw;
            }
        }

        public StateModel GetBackupState(string name)
        {
            try
            {
                if (_backupManager == null)
                {
                    throw new InvalidOperationException("BackupManager is not initialized");
                }
                return _backupManager.GetJobState(name);
            }
            catch (Exception ex)
            {
                _logController?.LogAdminAction(name, "ERROR", $"Error getting backup state: {ex.Message}");
                throw;
            }
        }

        public void DisplayLogs()
        {
            try
            {
                _logController.DisplayLogs();
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error displaying logs: {ex.Message}");
                throw;
            }
        }

        public void SetLogFormat(LogFormat format)
        {
            try
            {
                _logController.SetLogFormat(format);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error setting log format: {ex.Message}");
                throw;
            }
        }

        public LogFormat GetCurrentLogFormat()
        {
            try
            {
                return _logController.GetCurrentLogFormat();
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error getting log format: {ex.Message}");
                throw;
            }
        }

        private void OnFileProgress(object? sender, FileProgressEventArgs e)
        {
            try
            {
                FileProgressChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(e.BackupName, "ERROR", $"Error handling file progress: {ex.Message}");
            }
        }

        private void OnEncryptionProgress(object? sender, EncryptionProgressEventArgs e)
        {
            try
            {
                EncryptionProgressChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(e.BackupName, "ERROR", $"Error handling encryption progress: {ex.Message}");
            }
        }
    }
}