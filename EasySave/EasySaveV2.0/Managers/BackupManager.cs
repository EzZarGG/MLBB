using EasySaveV2._0.Models;
using EasySaveLogging;
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
        private readonly Logger _logger;
        private readonly string _stateFile;
        private readonly Dictionary<string, StateModel> _jobStates;
        private readonly JsonSerializerOptions _jsonOptions;

        public BackupManager()
        {
            _backups = new List<Backup>();
            _logger = Logger.GetInstance();
            _stateFile = Config.GetStateFilePath();
            _jobStates = new Dictionary<string, StateModel>();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            LoadOrInitializeStates();
        }

        public IReadOnlyList<Backup> Jobs => _backups;

        private void LoadOrInitializeStates()
        {
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
                try
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
                catch (Exception ex)
                {
                    _logger.LogAdminAction("System", "ERROR", $"Failed to load state file: {ex.Message}");
                }
            }

            SaveStates(_jobStates.Values.ToList());
        }

        private void SaveStates(List<StateModel> states)
        {
            var json = JsonSerializer.Serialize(states, _jsonOptions);
            File.WriteAllText(_stateFile, json);
        }

        private void UpdateJobState(string jobName, Action<StateModel> updateAction)
        {
            if (!_jobStates.ContainsKey(jobName))
            {
                _jobStates[jobName] = StateModel.CreateInitialState(jobName);
            }

            updateAction(_jobStates[jobName]);
            _jobStates[jobName].LastActionTime = DateTime.Now;
            SaveStates(_jobStates.Values.ToList());
        }

        public bool AddJob(Backup job)
        {
            if (_backups.Count >= 5) return false;
            _backups.Add(job);
            Config.SaveJobs(_backups);

            _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
            SaveStates(_jobStates.Values.ToList());

            _logger.LogAdminAction(job.Name, "CREATE", $"Backup job created: {job.Name}");
            return true;
        }

        public bool RemoveJob(string name)
        {
            var job = _backups.FirstOrDefault(b => b.Name == name);
            if (job == null) return false;

            _backups.Remove(job);
            Config.SaveJobs(_backups);

            if (_jobStates.ContainsKey(name))
            {
                _jobStates.Remove(name);
                SaveStates(_jobStates.Values.ToList());
            }

            _logger.LogAdminAction(name, "DELETE", $"Backup job deleted: {name}");
            return true;
        }

        public bool UpdateJob(string name, Backup updated)
        {
            var idx = _backups.FindIndex(b => b.Name == name);
            if (idx < 0) return false;

            _backups[idx] = updated;
            Config.SaveJobs(_backups);

            if (name != updated.Name && _jobStates.ContainsKey(name))
            {
                var state = _jobStates[name];
                _jobStates.Remove(name);
                state.Name = updated.Name;
                _jobStates[updated.Name] = state;
                SaveStates(_jobStates.Values.ToList());
            }

            _logger.LogAdminAction(updated.Name, "UPDATE", $"Backup job updated: {name} to {updated.Name}");
            return true;
        }

        public Backup GetJob(string name)
        {
            return _backups.FirstOrDefault(b => b.Name == name);
        }

        public StateModel GetJobState(string name)
        {
            return _jobStates.ContainsKey(name) ? _jobStates[name] : null;
        }

        public void ExecuteJobsByIndices(IEnumerable<int> indices)
        {
            foreach (var i in indices)
            {
                if (i >= 1 && i <= _backups.Count)
                {
                    RunBackup(_backups[i - 1]);
                }
            }
        }

        private void RunBackup(Backup job)
        {
            _logger.LogAdminAction(job.Name, "EXECUTE_START", $"Started executing backup job: {job.Name}");

            try
            {
                var allFiles = Directory
                    .EnumerateFiles(job.SourcePath, "*", SearchOption.AllDirectories)
                    .ToList();

                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                int totalFiles = allFiles.Count;

                UpdateJobState(job.Name, state => {
                    state.Status = "Active";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = job.SourcePath;
                    state.CurrentTargetFile = job.TargetPath;
                });

                foreach (var src in allFiles)
                {
                    var rel = Path.GetRelativePath(job.SourcePath, src);
                    var dst = Path.Combine(job.TargetPath, rel);
                    long fileSize = new FileInfo(src).Length;

                    Directory.CreateDirectory(Path.GetDirectoryName(dst));

                    UpdateJobState(job.Name, state => {
                        state.CurrentSourceFile = src;
                        state.CurrentTargetFile = dst;
                    });

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    try
                    {
                        File.Copy(src, dst, true);
                        sw.Stop();
                        _logger.CreateLog(
                            job.Name,
                            sw.Elapsed,
                            fileSize,
                            DateTime.Now,
                            src,
                            dst,
                            "INFO"
                        );

                        UpdateJobState(job.Name, state => {
                            state.FilesRemaining -= 1;
                            state.BytesRemaining -= fileSize;
                        });
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.CreateLog(
                            job.Name,
                            sw.Elapsed.Negate(),
                            0,
                            DateTime.Now,
                            src,
                            dst,
                            "ERROR"
                        );

                        _logger.LogAdminAction(job.Name, "ERROR", $"Error copying file {src}: {ex.Message}");
                    }
                }

                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                _logger.LogAdminAction(job.Name, "EXECUTE_COMPLETE", $"Completed executing backup job: {job.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogAdminAction(job.Name, "ERROR", $"Critical error during backup: {ex.Message}");

                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });
            }
        }
        public List<FileInfo> CollectFiles(Backup backup)
        {
            var files = new List<FileInfo>();
            // Exemple : tous les fichiers du dossier source (y compris sous-dossiers)
            foreach (var path in Directory.EnumerateFiles(backup.SourcePath, "*", SearchOption.AllDirectories))
                files.Add(new FileInfo(path));
            return files;
        }

        public void ShowLogs()
        {
            _logger.DisplayLogs();
        }

        private void SaveJobs()
        {
            Config.SaveJobs(_backups);
        }

        private void LoadStates()
        {
            if (File.Exists(_stateFile))
            {
                try
                {
                    var json = File.ReadAllText(_stateFile);
                    var loadedStates = JsonSerializer.Deserialize<List<StateModel>>(json, _jsonOptions);

                    if (loadedStates != null)
                    {
                        foreach (var state in loadedStates)
                        {
                            _jobStates[state.Name] = state;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogAdminAction("System", "ERROR", $"Failed to load state file: {ex.Message}");
                }
            }
        }
    }
} 