using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySaveV2._0.Controllers
{
    public class BackupController
    {
        // List of all backups
        private readonly List<Backup> _backups;
        // Dictionary to track the state of each backup by name
        private readonly Dictionary<string, StateModel> _states;
        // Controller for application settings
        private readonly SettingsController _settingsController;
        // Controller for logging operations
        private readonly LogController _logController;
        // Manager for language/localization
        private readonly LanguageManager _languageManager;
        // Path to the file where backups are saved
        private readonly string _backupFilePath = "backups.json";

        public BackupController()
        {
            _backups = new List<Backup>();
            _states = new Dictionary<string, StateModel>();
            _settingsController = new SettingsController();
            _logController = new LogController();
            _languageManager = LanguageManager.Instance;
            LoadBackups();
        }

        // Load backups from file
        private void LoadBackups()
        {
            if (File.Exists(_backupFilePath))
            {
                var json = File.ReadAllText(_backupFilePath);
                var loaded = JsonSerializer.Deserialize<List<Backup>>(json);
                if (loaded != null)
                    _backups.AddRange(loaded);
            }
        }

        // Save backups to file
        private void SaveBackups()
        {
            var json = JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_backupFilePath, json);
        }

        // Create a new backup
        public void CreateBackup(string name, string sourcePath, string destinationPath, string type)
        {
            if (_backups.Any(b => b.Name == name))
                throw new InvalidOperationException($"A backup with the name '{name}' already exists.");

            var backup = new Backup
            {
                Name = name,
                SourcePath = sourcePath,
                TargetPath = destinationPath,
                Type = type,
                FileLength = 0
            };

            _backups.Add(backup);
            _states[name] = StateModel.CreateInitialState(name);
            SaveBackups();
        }

        // Edit an existing backup
        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
            var backup = _backups.FirstOrDefault(b => b.Name == name);
            if (backup == null)
                throw new InvalidOperationException($"Backup '{name}' not found.");

            backup.SourcePath = sourcePath;
            backup.TargetPath = destinationPath;
            backup.Type = type;
            SaveBackups();
        }

        // Delete a backup
        public void DeleteBackup(string name)
        {
            var backup = _backups.FirstOrDefault(b => b.Name == name);
            if (backup == null)
                throw new InvalidOperationException($"Backup '{name}' not found.");

            _backups.Remove(backup);
            _states.Remove(name);
            SaveBackups();
        }

        // Start a backup process asynchronously
        public async Task StartBackup(string name)
        {
            var backup = GetBackup(name);
            if (backup == null)
                throw new InvalidOperationException($"Backup '{name}' not found.");

            var state = StateModel.CreateInitialState(name);
            state.Status = "Active";
            _states[name] = state;

            _logController.LogBackupStart(name);

            try
            {
                var files = Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories);
                state.TotalFilesCount = files.Length;
                state.TotalFilesSize = files.Sum(f => new FileInfo(f).Length);
                state.FilesRemaining = state.TotalFilesCount;
                state.BytesRemaining = state.TotalFilesSize;

                foreach (var sourceFile in files)
                {
                    var relativePath = Path.GetRelativePath(backup.SourcePath, sourceFile);
                    var targetFile = Path.Combine(backup.TargetPath, relativePath);
                    var sourceInfo = new FileInfo(sourceFile);
                    var shouldCopy = true;

                    // Differential: only copy if file is new or modified
                    if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase) && File.Exists(targetFile))
                    {
                        var targetInfo = new FileInfo(targetFile);
                        if (sourceInfo.LastWriteTime <= targetInfo.LastWriteTime)
                            shouldCopy = false;
                    }

                    if (shouldCopy)
                    {
                        var dir = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        var startTime = DateTime.Now;
                        File.Copy(sourceFile, targetFile, true);
                        var duration = DateTime.Now - startTime;

                        _logController.LogFileOperation(name, sourceFile, targetFile, sourceInfo.Length);
                    }

                    // Update backup state
                    state.CurrentSourceFile = sourceFile;
                    state.CurrentTargetFile = targetFile;
                    state.FilesRemaining--;
                    state.BytesRemaining -= sourceInfo.Length;
                    state.LastActionTime = DateTime.Now;
                }

                state.Status = "Inactive";
                _logController.LogBackupComplete(name);
            }
            catch (Exception ex)
            {
                state.Status = "Error";
                _logController.LogBackupError(name, ex.Message);
            }
        }

        // Pause a backup (to be implemented)
        public void PauseBackup(string name)
        {
            // To be implemented with Task + CancellationTokenSource
            _logController.LogAdminAction(name, "PAUSE", "Pause function not yet implemented.");
        }

        // Resume a backup (to be implemented)
        public void ResumeBackup(string name)
        {
            // To be implemented with queue management
            _logController.LogAdminAction(name, "RESUME", "Resume function not yet implemented.");
        }

        // Stop a backup (to be implemented)
        public void StopBackup(string name)
        {
            // To be implemented with thread cancellation
            _logController.LogAdminAction(name, "STOP", "Stop function not yet implemented.");
        }

        // Get all backups
        public List<Backup> GetBackups() => _backups;

        // Get a backup by name
        public Backup GetBackup(string name) => _backups.FirstOrDefault(b => b.Name == name);

        // Get the state of a backup
        public StateModel GetBackupState(string name)
        {
            if (_states.TryGetValue(name, out var state))
                return state;

            return StateModel.CreateInitialState(name);
        }

        // Display logs
        public void DisplayLogs()
        {
            _logController.DisplayLogs();
        }

        // Set the log format
        public void SetLogFormat(LogFormat format)
        {
            _logController.SetLogFormat(format);
        }

        // Get the current log format
        public LogFormat GetCurrentLogFormat()
        {
            return _logController.GetCurrentLogFormat();
        }

        // Event handler for file progress
        private void OnFileProgress(object sender, FileProgressEventArgs e)
        {
            if (_states.TryGetValue(e.BackupName, out var state))
            {
                state.CurrentSourceFile = e.SourceFile;
                state.CurrentTargetFile = e.TargetFile;
                state.FilesRemaining = e.FilesRemaining;
                state.BytesRemaining = e.BytesRemaining;
                state.LastActionTime = DateTime.Now;
            }
        }

        // Event handler for encryption progress
        private void OnEncryptionProgress(object sender, EncryptionProgressEventArgs e)
        {
            _logController.LogEncryptionStart(e.BackupName);
        }
    }
}
