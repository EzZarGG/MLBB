using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EasySaveV2._0.Controllers
{
    public class BackupController
    {
        private readonly List<Backup> _backups;
        private readonly SettingsController _settingsController;
        private readonly LogController _logController;
        private readonly LanguageManager _languageManager;
        private readonly BackupManager _backupManager;

        public BackupController()
        {
            _backups = new List<Backup>();
            _settingsController = new SettingsController();
            _logController = new LogController();
            _languageManager = LanguageManager.Instance;
            _backupManager = new BackupManager();
            LoadBackups();
        }

        private void LoadBackups()
        {
            _backups.Clear();
            _backups.AddRange(_backupManager.Jobs);
        }

        public void CreateBackup(string name, string sourcePath, string destinationPath, string type)
        {
            var backup = new Backup
            {
                Name = name,
                SourcePath = sourcePath,
                TargetPath = destinationPath,
                Type = type,
                FileLength = 0
            };

            if (_backupManager.AddJob(backup))
            {
                _logController.LogBackupStart(name);
                LoadBackups();
            }
        }

        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
            var backup = new Backup
            {
                Name = name,
                SourcePath = sourcePath,
                TargetPath = destinationPath,
                Type = type,
                FileLength = 0
            };

            if (_backupManager.UpdateJob(name, backup))
            {
                _logController.LogBackupStart(name);
                LoadBackups();
            }
        }

        public void DeleteBackup(string name)
        {
            if (_backupManager.RemoveJob(name))
            {
                _logController.LogBackupComplete(name);
                LoadBackups();
            }
        }

        public async Task StartBackup(string name)
        {
            var backup = _backupManager.GetJob(name);
            if (backup == null) return;

            _logController.LogBackupStart(name);
            await Task.Run(() => _backupManager.ExecuteJobsByIndices(new[] { _backups.FindIndex(b => b.Name == name) + 1 }));
            _logController.LogBackupComplete(name);
        }

        public void PauseBackup(string name)
        {
            _logController.LogAdminAction(name, "PAUSE", "Backup paused");
        }

        public void ResumeBackup(string name)
        {
            _logController.LogAdminAction(name, "RESUME", "Backup resumed");
        }

        public void StopBackup(string name)
        {
            _logController.LogAdminAction(name, "STOP", "Backup stopped");
        }

        public List<Backup> GetBackups()
        {
            return _backups;
        }

        public Backup GetBackup(string name)
        {
            return _backupManager.GetJob(name);
        }

        public void DisplayLogs()
        {
            _logController.DisplayLogs();
        }

        public void SetLogFormat(LogFormat format)
        {
            _logController.SetLogFormat(format);
        }

        public LogFormat GetCurrentLogFormat()
        {
            return _logController.GetCurrentLogFormat();
        }

        private void OnFileProgress(object sender, FileProgressEventArgs e)
        {
            _logController.LogFileOperation(
                e.BackupName,
                e.SourcePath,
                e.TargetPath,
                e.FileSize
            );
        }

        private void OnEncryptionProgress(object sender, EncryptionProgressEventArgs e)
        {
            if (e.IsComplete)
            {
                _logController.LogEncryptionComplete(e.BackupName);
            }
            else if (e.HasError)
            {
                _logController.LogEncryptionError(e.BackupName, e.ErrorMessage);
            }
        }

        public StateModel GetBackupState(string name)
        {
            return _backupManager.GetJobState(name);
        }
    }
} 