using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

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
        private const int MAX_BACKUPS = 5;

        public BackupManager()
        {
            _backupFilePath = Path.Combine(AppContext.BaseDirectory, "backups.json");
            _stateFile = Path.Combine(AppContext.BaseDirectory, "states.json");
            _backups = new List<Backup>();
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
            try
            {
                _logController.LogAdminAction("System", "INIT", "Loading or initializing states...");
                
                // Clear existing states and load backups
                _backups.Clear();
                _backups.AddRange(Config.LoadJobs());

                // Initialize states for all existing jobs
                foreach (var job in _backups)
                {
                    _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
                }

                // Try to load existing state file if it exists
                if (File.Exists(_stateFile))
                {
                    var json = File.ReadAllText(_stateFile);
                    var loadedStates = JsonSerializer.Deserialize<List<StateModel>>(json, _jsonOptions);

                    if (loadedStates != null)
                    {
                        foreach (var state in loadedStates)
                        {
                            if (_jobStates.ContainsKey(state.Name))
                            {
                                _jobStates[state.Name] = state;
                            }
                        }
                    }
                }

                // Save the current states to ensure consistency
                SaveStates(_jobStates.Values.ToList());
                _logController.LogAdminAction("System", "INIT", "States loaded successfully");
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error loading states: {ex.Message}");
                throw;
            }
        }

        private void LoadBackups()
        {
            try
            {
                _logController.LogAdminAction("System", "INIT", "Loading backups...");
                if (File.Exists(_backupFilePath))
                {
                    var json = File.ReadAllText(_backupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<Backup>>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        _backups.AddRange(loaded);
                    }
                }
                _logController.LogAdminAction("System", "INIT", "Backups loaded successfully");
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error loading backups: {ex.Message}");
                throw;
            }
        }

        private void SaveStates(List<StateModel> states)
        {
            try
            {
                var json = JsonSerializer.Serialize(states, _jsonOptions);
                File.WriteAllText(_stateFile, json);
                _logController.LogAdminAction("System", "SAVE", "States saved successfully");
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error saving states: {ex.Message}");
                throw;
            }
        }

        private void SaveBackups()
        {
            try
            {
                var json = JsonSerializer.Serialize(_backups, _jsonOptions);
                File.WriteAllText(_backupFilePath, json);
                Config.SaveJobs(_backups);
                _logController.LogAdminAction("System", "SAVE", "Backups saved successfully");
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction("System", "ERROR", $"Error saving backups: {ex.Message}");
                throw;
            }
        }

        private void UpdateJobState(string jobName, Action<StateModel> updateAction)
        {
            try
            {
                if (!_jobStates.ContainsKey(jobName))
                {
                    _jobStates[jobName] = StateModel.CreateInitialState(jobName);
                }

                updateAction(_jobStates[jobName]);
                _jobStates[jobName].LastActionTime = DateTime.Now;
                SaveStates(_jobStates.Values.ToList());
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(jobName, "ERROR", $"Error updating job state: {ex.Message}");
                throw;
            }
        }

        public bool AddJob(Backup job)
        {
            try
            {
                if (_backups.Count >= MAX_BACKUPS)
                {
                    _logController.LogAdminAction(job.Name, "ERROR", "Maximum number of backup jobs reached");
                    return false;
                }

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
            catch (Exception ex)
            {
                _logController.LogAdminAction(job.Name, "ERROR", $"Error adding backup job: {ex.Message}");
                return false;
            }
        }

        public bool RemoveJob(string name)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.Name == name);
                if (backup == null)
                {
                    _logController.LogAdminAction(name, "ERROR", "Backup not found");
                    return false;
                }

                _backups.Remove(backup);
                if (_jobStates.ContainsKey(name))
                {
                    _jobStates.Remove(name);
                }
                SaveBackups();
                SaveStates(_jobStates.Values.ToList());
                _logController.LogAdminAction(name, "DELETE", $"Backup job deleted: {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error removing backup job: {ex.Message}");
                return false;
            }
        }

        public bool UpdateJob(string name, Backup updated)
        {
            try
            {
                var index = _backups.FindIndex(b => b.Name == name);
                if (index == -1)
                {
                    _logController.LogAdminAction(name, "ERROR", "Backup not found");
                    return false;
                }

                _backups[index] = updated;
                Config.SaveJobs(_backups);

                if (name != updated.Name && _jobStates.ContainsKey(name))
                {
                    var state = _jobStates[name];
                    _jobStates.Remove(name);
                    state.Name = updated.Name;
                    _jobStates[updated.Name] = state;
                    SaveStates(_jobStates.Values.ToList());
                }

                _logController.LogAdminAction(updated.Name, "UPDATE", $"Backup job updated: {name} to {updated.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(name, "ERROR", $"Error updating backup job: {ex.Message}");
                return false;
            }
        }

        public Backup GetJob(string name)
        {
            return _backups.FirstOrDefault(b => b.Name == name);
        }

        public StateModel GetJobState(string name)
        {
            return _jobStates.TryGetValue(name, out var state) ? state : null;
        }

        public async Task ExecuteJob(string name)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup '{name}' not found.");
            }

            _logController.LogAdminAction(name, "EXECUTE_START", $"Starting backup: {name}");

            try
            {
                var files = await Task.Run(() => Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories));
                var totalBytes = files.Sum(f => new FileInfo(f).Length);
                var totalFiles = files.Length;

                UpdateJobState(name, state =>
                {
                    state.Status = "Active";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = backup.SourcePath;
                    state.CurrentTargetFile = backup.TargetPath;
                });

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

                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            await Task.Run(() => File.Copy(sourceFile, targetFile, true));
                            stopwatch.Stop();

                            _logController.CreateLog(
                                name,
                                stopwatch.Elapsed,
                                sourceInfo.Length,
                                DateTime.Now,
                                sourceFile,
                                targetFile,
                                "INFO"
                            );

                            UpdateJobState(name, state =>
                            {
                                state.FilesRemaining--;
                                state.BytesRemaining -= sourceInfo.Length;
                                state.CurrentSourceFile = sourceFile;
                                state.CurrentTargetFile = targetFile;
                            });
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();
                            _logController.CreateLog(
                                name,
                                stopwatch.Elapsed.Negate(),
                                0,
                                DateTime.Now,
                                sourceFile,
                                targetFile,
                                "ERROR"
                            );
                            _logController.LogAdminAction(name, "ERROR", $"Error copying file {sourceFile}: {ex.Message}");
                        }
                    }
                }

                UpdateJobState(name, state =>
                {
                    state.Status = "Completed";
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                _logController.LogAdminAction(name, "EXECUTE_COMPLETE", $"Backup completed: {name}");
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                _logController.LogAdminAction(name, "ERROR", $"Backup failed: {ex.Message}");
                throw;
            }
        }

        public void DisplayLogs()
        {
            _logController.DisplayLogs();
        }
    }
}