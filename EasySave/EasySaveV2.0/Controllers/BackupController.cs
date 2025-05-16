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

        public BackupController()
        {
            _backups = new List<Backup>();
            _settingsController = new SettingsController();
            _logController = new LogController();
            _languageManager = LanguageManager.Instance;
            LoadBackups();
        }

        private void LoadBackups()
        {
        }

        public void CreateBackup(string name, string sourcePath, string destinationPath, string type)
        {
        }

        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
        }

        public void DeleteBackup(string name)
        {
        }

        public async Task StartBackup(string name)
        {
        }

        public void PauseBackup(string name)
        {
        }

        public void ResumeBackup(string name)
        {
        }

        public void StopBackup(string name)
        {
        }

        public List<Backup> GetBackups()
        {
            return _backups;
        }

        public Backup GetBackup(string name)
        {
            return null;
        }

        public void DisplayLogs()
        {
        }

        public void SetLogFormat(LogFormat format)
        {
        }

        public LogFormat GetCurrentLogFormat()
        {
            return LogFormat.JSON;
        }

        private void OnFileProgress(object sender, FileProgressEventArgs e)
        {
        }

        private void OnEncryptionProgress(object sender, EncryptionProgressEventArgs e)
        {
        }
    }
} 