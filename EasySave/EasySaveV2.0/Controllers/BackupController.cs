using EasySave.Business;
using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using EasySaveV2._0.Notifications;
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
        private readonly BusinessSoftwareManager _bsManager = new BusinessSoftwareManager();
        private readonly Logger _logger = Logger.GetInstance();
        private readonly INotifier _notifier;

        public BackupController(INotifier notifier)
        {
            _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            _backups = new List<Backup>();
            _settingsController = new SettingsController();
            _logController = new LogController();
            _languageManager = LanguageManager.Instance;
            _backupManager = new BackupManager();
            LoadBackups();
        }

        public async Task RunBackupAsync(BackupJob job, CancellationToken ct)
        {
            // Avant tout lancement
            if (_bsManager.IsAnyRunning())
            {
                _logger.LogAdminAction(job.Name, "BACKUP_BLOCKED", "Sauvegarde empêchée : logiciel métier détecté avant démarrage.");
                _notifier.Warn("Un logiciel métier est en cours : sauvegarde bloquée.");
                return;
            }

            foreach (var file in job.FilesToSave)
            {
                // Sauvegarde du fichier
                try
                {
                    var start = DateTime.Now;
                    await SaveFileAsync(file, job.TargetDirectory, ct);
                    var duration = DateTime.Now - start;
                    _logger.CreateLog(job.Name, duration, file.Length, start, file.FullName, job.TargetDirectory, "SUCCESS");
                }
                catch (Exception ex)
                {
                    _logger.CreateLog(job.Name, TimeSpan.Zero, file.Length, DateTime.Now, file.FullName, job.TargetDirectory, "ERROR");
                    _logger.LogAdminAction(job.Name, "ERROR", $"Erreur sur '{file.FullName}': {ex.Message}");
                }

                // Vérifier à nouveau avant le fichier suivant
                if (_bsManager.IsAnyRunning())
                {
                    _logger.LogAdminAction(job.Name, "BACKUP_INTERRUPTED",
                        $"Sauvegarde interrompue après '{file.Name}' : logiciel métier démarré.");
                    _notifier.Warn("Un logiciel métier s'est lancé, sauvegarde interrompue.");
                    return;
                }
            }

            _logger.LogAdminAction(job.Name, "BACKUP_COMPLETED", "Sauvegarde terminée avec succès.");
            _notifier.Info("Sauvegarde achevée.");
        }

        private Task SaveFileAsync(System.IO.FileInfo file, string targetDir, CancellationToken ct)
        {
            // Implémentation existante de la copie…
            throw new NotImplementedException();
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