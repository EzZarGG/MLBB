using EasySaveV2._0.Models;
using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CryptoLibrary;
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
        private readonly byte[] _encryptionKey;
        private readonly bool _encryptEnabled;
        private readonly HashSet<string> _encryptExtensions;

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
            _encryptionKey = Config.GetEncryptionKey();
            _encryptEnabled = _encryptionKey != null;
            _encryptExtensions = Config.GetEncryptionExtensions();
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
            // Log the start of execution
            _logger.LogAdminAction(job.Name, "EXECUTE_START", $"Started executing backup job: {job.Name}");

            try
            {
                // Gather all files under the source directory
                var allFiles = Directory.EnumerateFiles(job.SourcePath, "*", SearchOption.AllDirectories).ToList();
                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                int totalFiles = allFiles.Count;

                // Initialize state to “Active”
                UpdateJobState(job.Name, state => {
                    state.Status = "Active";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = job.SourcePath;
                    state.CurrentTargetFile = job.TargetPath;
                });

                // Process each file one by one
                foreach (var src in allFiles)
                {
                    var rel = Path.GetRelativePath(job.SourcePath, src);
                    var dst = Path.Combine(job.TargetPath, rel);
                    long fileSize = new FileInfo(src).Length;

                    // Ensure the target folder exists
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));

                    // Update state before copying
                    UpdateJobState(job.Name, state => {
                        state.CurrentSourceFile = src;
                        state.CurrentTargetFile = dst;
                    });

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    long encryptionTime = 0;

                    try
                    {
                        // Copy the file
                        File.Copy(src, dst, overwrite: true);
                        sw.Stop();

                        // If encryption is enabled and the file’s extension matches
                        var ext = Path.GetExtension(src).ToLower();
                        if (_encryptEnabled && _encryptExtensions.Contains(ext))
                        {
                            var cryptDst = dst + ".crypt";
                            encryptionTime = CryptoManager.EncryptFile(dst, cryptDst, _encryptionKey);

                            if (encryptionTime < 0)
                            {
                                _logger.LogAdminAction(job.Name, "ERROR",
                                    $"Erreur de chiffrement {dst} (code {encryptionTime})");
                            }
                            else
                            {
                                _logger.LogAdminAction(job.Name, "INFO",
                                    $"Fichier chiffré en {encryptionTime} ms ? {cryptDst}");
                            }

                            // Remove the unencrypted copy
                            File.Delete(dst);
                        }

                        // Write a log entry including transfer and encryption times
                        _logger.CreateLog(
                            backupName: job.Name,
                            transferTime: sw.Elapsed,
                            fileSize: fileSize,
                            date: DateTime.Now,
                            sourcePath: src,
                            targetPath: dst,
                            logType: "INFO",
                            encryptionTime: encryptionTime
                        );

                        // Update state after successful copy
                        UpdateJobState(job.Name, state => {
                            state.FilesRemaining -= 1;
                            state.BytesRemaining -= fileSize;
                        });
                    }
                    catch (Exception exFile)
                    {
                        sw.Stop();

                        // Log the failed file transfer (negative transfer time) and no encryption
                        _logger.CreateLog(
                            backupName: job.Name,
                            transferTime: sw.Elapsed.Negate(),
                            fileSize: 0,
                            date: DateTime.Now,
                            sourcePath: src,
                            targetPath: dst,
                            logType: "ERROR",
                            encryptionTime: 0
                        );

                        _logger.LogAdminAction(job.Name, "ERROR",
                            $"Error processing file {src}: {exFile.Message}");
                    }
                }

                // Mark job as completed
                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                _logger.LogAdminAction(job.Name, "EXECUTE_COMPLETE",
                    $"Completed executing backup job: {job.Name}");
            }
            catch (Exception ex)
            {
                // Critical failure: log and set job inactive
                _logger.LogAdminAction(job.Name, "ERROR",
                    $"Critical error during backup: {ex.Message}");

                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });
            }
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