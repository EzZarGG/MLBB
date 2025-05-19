using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySaveV2._0.Managers
{
    public class BackupManager
    {
        private readonly List<Backup> _backups;
        private readonly LogController _logController;
        private readonly string _stateFile;
        private readonly Dictionary<string, StateModel> _jobStates;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _backupFilePath;

        public BackupManager()
        {
            _backupFilePath = "backups.json";
            _backups = new List<Backup>();
            _stateFile = "states.json";
            _jobStates = new Dictionary<string, StateModel>();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _logController = new LogController();
            LoadOrInitializeStates();
            LoadBackups();
        }

        public IReadOnlyList<Backup> Jobs => _backups;

        private void LoadOrInitializeStates()
        {
            if (File.Exists(_stateFile))
            {
                try
                {
                    var json = File.ReadAllText(_stateFile);
                    var states = JsonSerializer.Deserialize<List<StateModel>>(json, _jsonOptions);
                    if (states != null)
                    {
                        foreach (var state in states)
                        {
                            _jobStates[state.Name] = state;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logController.LogAdminAction("System", "ERROR", $"Error loading states: {ex.Message}");
                }
            }

            // Initialize states for any backups without states
            foreach (var backup in _backups)
            {
                if (!_jobStates.ContainsKey(backup.Name))
                {
                    _jobStates[backup.Name] = StateModel.CreateInitialState(backup.Name);
                }
            }
        }

        private void LoadBackups()
        {
            if (File.Exists(_backupFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_backupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<Backup>>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        _backups.AddRange(loaded);
                    }
                }
                catch (Exception ex)
                {
                    _logController.LogAdminAction("System", "ERROR", $"Error loading backups: {ex.Message}");
                }
            }
        }

        private void SaveStates(List<StateModel> states)
        {
            try
            {
                var json = JsonSerializer.Serialize(states, _jsonOptions);
                File.WriteAllText(_stateFile, json);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error saving states: {ex.Message}");
            }
        }

        private void SaveBackups()
        {
            try
            {
                var json = JsonSerializer.Serialize(_backups, _jsonOptions);
                File.WriteAllText(_backupFilePath, json);
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error saving backups: {ex.Message}");
            }
        }

        private void UpdateJobState(string jobName, Action<StateModel> updateAction)
        {
            if (_jobStates.TryGetValue(jobName, out var state))
            {
                updateAction(state);
                SaveStates(_jobStates.Values.ToList());
            }
        }

        public bool AddJob(Backup job)
        {
            if (_backups.Any(b => b.Name == job.Name))
            {
                _logController.LogAdminAction(job.Name, "ERROR", "A backup with this name already exists");
                return false;
            }

            _backups.Add(job);
            _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
            SaveBackups();
            SaveStates(_jobStates.Values.ToList());
            _logController.LogAdminAction(job.Name, "CREATE", $"Backup job created: {job.Name}");
            return true;
        }

        public bool RemoveJob(string name)
        {
            var backup = _backups.FirstOrDefault(b => b.Name == name);
            if (backup == null)
            {
                _logController.LogAdminAction(name, "ERROR", "Backup not found");
                return false;
            }

            _backups.Remove(backup);
            _jobStates.Remove(name);
            SaveBackups();
            SaveStates(_jobStates.Values.ToList());
            _logController.LogAdminAction(name, "DELETE", $"Backup job deleted: {name}");
            return true;
        }

        public bool UpdateJob(string name, Backup updated)
        {
            var index = _backups.FindIndex(b => b.Name == name);
            if (index == -1)
            {
                _logController.LogAdminAction(name, "ERROR", "Backup not found");
                return false;
            }

            _backups[index] = updated;

            if (name != updated.Name && _jobStates.ContainsKey(name))
            {
                var state = _jobStates[name];
                _jobStates.Remove(name);
                state.Name = updated.Name;
                _jobStates[updated.Name] = state;
            }

            SaveBackups();
            SaveStates(_jobStates.Values.ToList());
            _logController.LogAdminAction(updated.Name, "UPDATE", $"Backup job updated: {name} to {updated.Name}");
            return true;
        }

        public Backup GetJob(string name)
        {
            return _backups.FirstOrDefault(b => b.Name == name);
        }

        public StateModel GetJobState(string name)
        {
            if (_jobStates.TryGetValue(name, out var state))
            {
                return state;
            }
            return StateModel.CreateInitialState(name);
        }

        public async Task ExecuteJob(string name)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup '{name}' not found.");
            }

            var state = GetJobState(name);
            state.Status = "Active";
            SaveStates(_jobStates.Values.ToList());

            _logController.LogAdminAction(name, "START", $"Starting backup: {name}");

            try
            {
                var files = await Task.Run(() => Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories));
                state.TotalFilesCount = files.Length;
                state.TotalFilesSize = await Task.Run(() => files.Sum(f => new FileInfo(f).Length));
                state.FilesRemaining = state.TotalFilesCount;
                state.BytesRemaining = state.TotalFilesSize;
                SaveStates(_jobStates.Values.ToList());

                foreach (var sourceFile in files)
                {
                    var relativePath = Path.GetRelativePath(backup.SourcePath, sourceFile);
                    var targetFile = Path.Combine(backup.TargetPath, relativePath);
                    var sourceInfo = new FileInfo(sourceFile);
                    var shouldCopy = true;

                    if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase) && File.Exists(targetFile))
                    {
                        var targetInfo = new FileInfo(targetFile);
                        if (sourceInfo.LastWriteTime <= targetInfo.LastWriteTime)
                        {
                            shouldCopy = false;
                        }
                    }

                    if (shouldCopy)
                    {
                        var dir = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        var startTime = DateTime.Now;
                        await Task.Run(() => File.Copy(sourceFile, targetFile, true));
                        var duration = DateTime.Now - startTime;

                        _logController.LogFileOperation(name, sourceFile, targetFile, sourceInfo.Length);
                    }

                    state.CurrentSourceFile = sourceFile;
                    state.CurrentTargetFile = targetFile;
                    state.FilesRemaining--;
                    state.BytesRemaining -= sourceInfo.Length;
                    state.LastActionTime = DateTime.Now;
                    SaveStates(_jobStates.Values.ToList());
                }

                state.Status = "Inactive";
                _logController.LogAdminAction(name, "COMPLETE", $"Backup completed: {name}");
            }
            catch (Exception ex)
            {
                state.Status = "Error";
                _logController.LogAdminAction(name, "ERROR", $"Backup failed: {ex.Message}");
                throw;
            }
            finally
            {
                SaveStates(_jobStates.Values.ToList());
            }
        }
    }
} 