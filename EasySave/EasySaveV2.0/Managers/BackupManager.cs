using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;

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
        private readonly EncryptionKey _encryptionKey;
        private const int MAX_BACKUPS = 5;
        private const int BUFFER_SIZE = 8192;
        private readonly object _stateLock = new();
        private readonly object _backupLock = new();

        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;

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
            _encryptionKey = new EncryptionKey();
            LoadBackups();
            LoadOrInitializeStates();
        }

        public IReadOnlyList<Backup> Jobs => _backups;

        private void LoadOrInitializeStates()
        {
            try
            {
                _logController.LogAdminAction("System", "INIT", "Loading or initializing states...");
                
                // Initialize states for all existing jobs
                foreach (var job in _backups)
                {
                    if (!string.IsNullOrEmpty(job.Name))
                    {
                        _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
                    }
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
                            if (!string.IsNullOrEmpty(state.Name) && _jobStates.ContainsKey(state.Name))
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
                _backups.Clear();
                
                // First try to load from Config
                var configBackups = Config.LoadJobs();
                if (configBackups != null && configBackups.Any())
                {
                    _backups.AddRange(configBackups);
                    _logController.LogAdminAction("System", "INIT", "Loaded backups from Config");
                }
                // If no backups in Config, try to load from JSON file
                else if (File.Exists(_backupFilePath))
                {
                    var json = File.ReadAllText(_backupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<Backup>>(json, _jsonOptions);
                    if (loaded != null && loaded.Any())
                    {
                        _backups.AddRange(loaded);
                        _logController.LogAdminAction("System", "INIT", "Loaded backups from JSON file");
                    }
                }
                
                // Save backups to both locations to ensure consistency
                if (_backups.Any())
                {
                    SaveBackups();
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
                if (string.IsNullOrEmpty(jobName))
                {
                    throw new ArgumentNullException(nameof(jobName));
                }

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
                if (job == null)
                {
                    throw new ArgumentNullException(nameof(job));
                }

                if (string.IsNullOrEmpty(job.Name))
                {
                    throw new ArgumentException("Job name cannot be null or empty", nameof(job));
                }

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
                _logController.LogAdminAction(job.Name, "ADD", "Backup job added successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(job?.Name ?? "Unknown", "ERROR", $"Error adding backup job: {ex.Message}");
                throw;
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

        private async Task DecryptFileAsync(string sourceFile, string targetFile, string backupName)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                // Read the IV from the beginning of the file
                var iv = new byte[aes.BlockSize / 8];
                await sourceStream.ReadAsync(iv, 0, iv.Length);
                aes.IV = iv;
                aes.Key = _encryptionKey.Key;

                using (var cryptoStream = new CryptoStream(sourceStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var totalBytes = sourceStream.Length - iv.Length;
                    var bytesRead = 0L;
                    int read;

                    while ((read = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await targetStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        var progressPercentage = (int)((bytesRead * 100.0) / totalBytes);
                        EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                            backupName,
                            sourceFile,
                            progressPercentage
                        ));
                    }
                }
            }

            // Trigger completion event
            EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                backupName,
                sourceFile,
                100,
                true
            ));
        }

        private async Task EncryptFileAsync(string sourceFile, string targetFile, string backupName)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey.Key;
                aes.IV = _encryptionKey.IV;

                // Write the IV to the beginning of the file
                await targetStream.WriteAsync(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(targetStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var totalBytes = sourceStream.Length;
                    var bytesRead = 0L;
                    int read;

                    while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await cryptoStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        var progressPercentage = (int)((bytesRead * 100.0) / totalBytes);
                        EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                            backupName,
                            sourceFile,
                            progressPercentage
                        ));
                    }
                }
            }

            // Trigger completion event
            EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                backupName,
                sourceFile,
                100,
                true
            ));
        }

        public async Task ExecuteJob(string name)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup '{name}' not found.");
            }

            _logController.LogAdminAction(name, "EXECUTE_START", $"Started executing backup job: {name}");

            try
            {
                var files = await Task.Run(() => Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories));
                var totalBytes = files.Sum(f => new FileInfo(f).Length);
                var totalFiles = files.Length;
                var bytesTransferred = 0L;
                var filesProcessed = 0;

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
                            if (backup.Encrypt)
                            {
                                await EncryptFileAsync(sourceFile, targetFile, name);
                            }
                            else
                            {
                                await Task.Run(() => File.Copy(sourceFile, targetFile, true));
                            }

                            stopwatch.Stop();

                            _logController.LogFileOperation(
                                name,
                                sourceFile,
                                targetFile,
                                sourceInfo.Length
                            );

                            bytesTransferred += sourceInfo.Length;
                            filesProcessed++;

                            // Trigger FileProgressChanged event
                            FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                                name,
                                sourceFile,
                                targetFile,
                                sourceInfo.Length,
                                (int)(filesProcessed * 100.0 / totalFiles),
                                bytesTransferred,
                                totalBytes,
                                filesProcessed,
                                totalFiles,
                                stopwatch.Elapsed,
                                true
                            ));

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
                            _logController.LogAdminAction(name, "ERROR", $"Error copying file {sourceFile}: {ex.Message}");

                            // Trigger FileProgressChanged event with error
                            FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                                name,
                                sourceFile,
                                targetFile,
                                sourceInfo.Length,
                                (int)(filesProcessed * 100.0 / totalFiles),
                                bytesTransferred,
                                totalBytes,
                                filesProcessed,
                                totalFiles,
                                stopwatch.Elapsed,
                                false
                            ));

                            // Trigger EncryptionProgressChanged event with error if encryption was enabled
                            if (backup.Encrypt)
                            {
                                EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                                    name,
                                    sourceFile,
                                    0,
                                    true,
                                    true,
                                    ex.Message
                                ));
                            }
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

                // Trigger final FileProgressChanged event
                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    name,
                    backup.SourcePath,
                    backup.TargetPath,
                    0,
                    100,
                    totalBytes,
                    totalBytes,
                    totalFiles,
                    totalFiles,
                    TimeSpan.Zero,
                    true
                ));

                _logController.LogAdminAction(name, "EXECUTE_COMPLETE", $"Completed executing backup job: {name}");
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Trigger FileProgressChanged event with error
                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    name,
                    backup.SourcePath,
                    backup.TargetPath,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    TimeSpan.Zero,
                    false
                ));

                _logController.LogAdminAction(name, "ERROR", $"Backup failed: {ex.Message}");
                throw;
            }
        }

        public async Task RestoreJob(string name, string targetPath)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup '{name}' not found.");
            }

            _logController.LogAdminAction(name, "RESTORE_START", $"Started restoring backup job: {name}");

            try
            {
                var files = await Task.Run(() => Directory.GetFiles(backup.TargetPath, "*.*", SearchOption.AllDirectories));
                var totalBytes = files.Sum(f => new FileInfo(f).Length);
                var totalFiles = files.Length;
                var bytesTransferred = 0L;
                var filesProcessed = 0;

                UpdateJobState(name, state =>
                {
                    state.Status = "Restoring";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = backup.TargetPath;
                    state.CurrentTargetFile = targetPath;
                });

                foreach (var sourceFile in files)
                {
                    var relativePath = Path.GetRelativePath(backup.TargetPath, sourceFile);
                    var targetFile = Path.Combine(targetPath, relativePath);
                    var sourceInfo = new FileInfo(sourceFile);

                    var dir = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        if (backup.Encrypt)
                        {
                            await DecryptFileAsync(sourceFile, targetFile, name);
                        }
                        else
                        {
                            await Task.Run(() => File.Copy(sourceFile, targetFile, true));
                        }

                        stopwatch.Stop();

                        _logController.LogFileOperation(
                            name,
                            sourceFile,
                            targetFile,
                            sourceInfo.Length
                        );

                        bytesTransferred += sourceInfo.Length;
                        filesProcessed++;

                        // Trigger FileProgressChanged event
                        FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                            name,
                            sourceFile,
                            targetFile,
                            sourceInfo.Length,
                            (int)(filesProcessed * 100.0 / totalFiles),
                            bytesTransferred,
                            totalBytes,
                            filesProcessed,
                            totalFiles,
                            stopwatch.Elapsed,
                            true
                        ));

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
                        _logController.LogAdminAction(name, "ERROR", $"Error restoring file {sourceFile}: {ex.Message}");

                        // Trigger FileProgressChanged event with error
                        FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                            name,
                            sourceFile,
                            targetFile,
                            sourceInfo.Length,
                            (int)(filesProcessed * 100.0 / totalFiles),
                            bytesTransferred,
                            totalBytes,
                            filesProcessed,
                            totalFiles,
                            stopwatch.Elapsed,
                            false
                        ));

                        // Trigger EncryptionProgressChanged event with error if encryption was enabled
                        if (backup.Encrypt)
                        {
                            EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                                name,
                                sourceFile,
                                0,
                                true,
                                true,
                                ex.Message
                            ));
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

                // Trigger final FileProgressChanged event
                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    name,
                    backup.TargetPath,
                    targetPath,
                    0,
                    100,
                    totalBytes,
                    totalBytes,
                    totalFiles,
                    totalFiles,
                    TimeSpan.Zero,
                    true
                ));

                _logController.LogAdminAction(name, "RESTORE_COMPLETE", $"Completed restoring backup job: {name}");
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Trigger FileProgressChanged event with error
                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    name,
                    backup.TargetPath,
                    targetPath,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    TimeSpan.Zero,
                    false
                ));

                _logController.LogAdminAction(name, "ERROR", $"Restore failed: {ex.Message}");
                throw;
            }
        }

        public void DisplayLogs()
        {
            _logController.DisplayLogs();
        }

        private void OnFileProgress(string backupName, string sourceFile, string targetFile, long bytesCopied, long totalBytes)
        {
            try
            {
                var fileInfo = new FileInfo(sourceFile);
                var progressPercentage = (int)((bytesCopied * 100.0) / totalBytes);
                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    backupName,
                    sourceFile,
                    targetFile,
                    fileInfo.Length,
                    progressPercentage,
                    bytesCopied,
                    totalBytes,
                    1, // filesProcessed
                    1, // totalFiles
                    TimeSpan.Zero,
                    true
                ));
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(backupName, "ERROR", $"Error in file progress event: {ex.Message}");
            }
        }

        private void OnEncryptionProgress(string backupName, string file, int progress)
        {
            try
            {
                EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                    backupName,
                    file,
                    progress,
                    progress >= 100,
                    false,
                    null
                ));
            }
            catch (Exception ex)
            {
                _logController.LogAdminAction(backupName, "ERROR", $"Error in encryption progress event: {ex.Message}");
            }
        }
    }
}