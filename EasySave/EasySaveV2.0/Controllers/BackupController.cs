using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasySaveV2._0.Controllers
{
    public class BackupController
    {
        private readonly BackupManager _backupManager;
        private readonly SettingsController _settingsController;
        private readonly LogController _logController;
        private readonly LanguageManager _languageManager;

        public BackupController()
        {
            _backupManager = new BackupManager();
            _settingsController = new SettingsController();
            _logController = new LogController();
            _languageManager = LanguageManager.Instance;
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

            if (!_backupManager.AddJob(backup))
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("message.backupExists"));
            }
        }

        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
            var backup = _backupManager.GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
            }

            var updatedBackup = new Backup
            {
                Name = name,
                SourcePath = sourcePath,
                TargetPath = destinationPath,
                Type = type,
                FileLength = backup.FileLength
            };

            if (!_backupManager.UpdateJob(name, updatedBackup))
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
            }
        }

        public void DeleteBackup(string name)
        {
            if (!_backupManager.RemoveJob(name))
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
            }
        }

        public async Task StartBackup(string name)
        {
            if (_settingsController.IsBusinessSoftwareRunning())
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("message.businessSoftwareRunning"));
            }

            await _backupManager.ExecuteJob(name);
        }

        public List<Backup> GetBackups()
        {
            return new List<Backup>(_backupManager.Jobs);
        }

        public Backup GetBackup(string name)
        {
            return _backupManager.GetJob(name);
        }

        public StateModel GetBackupState(string name)
        {
            return _backupManager.GetJobState(name);
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
    }
}
