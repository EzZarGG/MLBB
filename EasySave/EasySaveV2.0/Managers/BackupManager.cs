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
using EasySaveLogging;
using System.Threading;

namespace EasySaveV2._0.Managers
{
    /// <summary>
    /// Manages backup operations including creation, execution, and monitoring of backup jobs.
    /// Handles file operations, encryption, and logging of backup activities.
    /// Provides comprehensive backup management with support for differential backups,
    /// encryption, progress tracking, and state management.
    /// </summary>
    public class BackupManager
    {
        // Configuration constants
        private const int MAX_BACKUPS = 5;  // Maximum number of backup jobs allowed
        private const int BUFFER_SIZE = 8192;  // 8KB buffer size for file operations

        // File paths for persistent storage
        private readonly string _backupFilePath;  // Path to store backup job configurations
        private readonly string _stateFile;       // Path to store backup job states

        // Core data structures
        private readonly List<Backup> _backups;           // Collection of all backup jobs
        private readonly Dictionary<string, StateModel> _jobStates;  // Current state of each backup job
        private readonly JsonSerializerOptions _jsonOptions;  // JSON serialization settings

        // Service dependencies
        private readonly LogController _logController;  // Handles logging operations
        private readonly EncryptionKey _encryptionKey;  // Manages encryption keys
        private readonly Logger _logger;               // Main logging service

        // Thread synchronization primitives
        private readonly object _stateLock = new();     // Lock for state modifications
        private readonly object _backupLock = new();    // Lock for backup list modifications
        private readonly Dictionary<string, CancellationTokenSource> _jobCancellationTokens = new();  // Cancellation tokens for running jobs

        // Progress tracking events
        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;        // Fired when file operation progress changes
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;  // Fired when encryption progress changes

        /// <summary>
        /// Initializes a new instance of the BackupManager class.
        /// Sets up file paths, loads existing backups and states, and initializes controllers.
        /// </summary>
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
            _logController = LogController.Instance;
            _encryptionKey = new EncryptionKey();
            _logger = Logger.GetInstance();
            LoadBackups();
            LoadOrInitializeStates();
        }

        /// <summary>
        /// Gets the list of all backup jobs.
        /// </summary>
        public IReadOnlyList<Backup> Jobs => _backups;

        /// <summary>
        /// Loads or initializes the state for all backup jobs.
        /// Attempts to load existing states from persistent storage,
        /// falls back to initial states if no saved state exists.
        /// </summary>
        private void LoadOrInitializeStates()
        {
            try
            {
                lock (_stateLock)
                {
                    // Load existing states if available
                    if (File.Exists(_stateFile))
                    {
                        var json = File.ReadAllText(_stateFile);
                        var loadedStates = JsonSerializer.Deserialize<List<StateModel>>(json, _jsonOptions);

                        if (loadedStates != null)
                        {
                            foreach (var state in loadedStates.Where(s => !string.IsNullOrEmpty(s.Name)))
                            {
                                _jobStates[state.Name] = state;
                            }
                        }
                    }

                    // Initialize states only for jobs that don't have a state yet
                    foreach (var job in _backups.Where(j => !string.IsNullOrEmpty(j.Name) && !_jobStates.ContainsKey(j.Name)))
                    {
                        _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
                    }

                    // Sauvegarder les états
                    SaveStates(_jobStates.Values.ToList());
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Loads backup jobs from configuration or JSON file.
        /// Prioritizes loading from Config, falls back to JSON file if needed.
        /// Ensures data consistency by saving loaded backups.
        /// </summary>
        private void LoadBackups()
        {
            try
            {
                _backups.Clear();
                
                // Try loading from Config first
                var configBackups = Config.LoadJobs();
                if (configBackups?.Any() == true)
                {
                    _backups.AddRange(configBackups);
                }
                // Fall back to JSON file if no backups in Config
                else if (File.Exists(_backupFilePath))
                {
                    var json = File.ReadAllText(_backupFilePath);
                    var loaded = JsonSerializer.Deserialize<List<Backup>>(json, _jsonOptions);
                    if (loaded?.Any() == true)
                    {
                        _backups.AddRange(loaded);
                    }
                }
                
                if (_backups.Any())
                {
                    SaveBackups();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Saves the current state of all backup jobs to persistent storage.
        /// Serializes job states to JSON format for durability.
        /// </summary>
        private void SaveStates(List<StateModel> states)
        {
            try
            {
                lock (_stateLock)
                {
                    var json = JsonSerializer.Serialize(states, _jsonOptions);
                    File.WriteAllText(_stateFile, json);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Saves the current list of backup jobs to both JSON file and Config.
        /// Ensures backup configurations are persisted across application restarts.
        /// </summary>
        private void SaveBackups()
        {
            try
            {
                var json = JsonSerializer.Serialize(_backups, _jsonOptions);
                File.WriteAllText(_backupFilePath, json);
                Config.SaveJobs(_backups);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Updates the state of a specific backup job.
        /// Thread-safe operation that persists state changes.
        /// </summary>
        /// <param name="jobName">Name of the backup job to update</param>
        /// <param name="updateAction">Action to perform on the job's state</param>
        /// <exception cref="ArgumentNullException">Thrown when jobName is null or empty</exception>
        public void UpdateJobState(string name, Action<StateModel> updateAction)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            try
            {
                lock (_stateLock)
                {
                    if (!_jobStates.ContainsKey(name))
                    {
                        _jobStates[name] = StateModel.CreateInitialState(name);
                    }

                    updateAction(_jobStates[name]);
                    _jobStates[name].LastActionTime = DateTime.Now;
                    
                    // Sauvegarder immédiatement l'état
                    var states = _jobStates.Values.ToList();
                    var json = JsonSerializer.Serialize(states, _jsonOptions);
                    File.WriteAllText(_stateFile, json);
                }
            }
            catch (Exception ex)
            {
                var existingBackup = GetJob(name);
                _logController.LogBackupError(name, existingBackup?.Type ?? "Unknown", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Adds a new backup job to the manager.
        /// Validates job parameters and ensures uniqueness of job names.
        /// </summary>
        /// <param name="job">The backup job configuration to add</param>
        /// <returns>True if the job was added successfully, false if maximum jobs reached or name exists</returns>
        /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
        /// <exception cref="ArgumentException">Thrown when job name is null or empty</exception>
        public bool AddJob(Backup job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (string.IsNullOrEmpty(job.Name))
            {
                throw new ArgumentException("Job name cannot be null or empty", nameof(job));
            }

            try
            {
                if (_backups.Count >= MAX_BACKUPS)
                {
                    var existingBackup = GetJob(job.Name);
                    _logController.LogBackupError(job.Name, existingBackup?.Type ?? "Unknown", "Maximum number of backup jobs reached");
                    return false;
                }

                if (_backups.Any(b => b.Name == job.Name))
                {
                    var existingBackup = GetJob(job.Name);
                    _logController.LogBackupError(job.Name, existingBackup?.Type ?? "Unknown", "A backup with this name already exists");
                    return false;
                }

                _backups.Add(job);
                _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
                SaveBackups();
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    BackupType = job.Type,
                    SourcePath = job.SourcePath,
                    TargetPath = job.TargetPath,
                    Message = "Backup job created",
                    LogType = "INFO",
                    ActionType = "BACKUP_START"
                };
                _logger.AddLogEntry(logEntry);
                return true;
            }
            catch (Exception ex)
            {
                var existingBackup = GetJob(job.Name);
                _logController.LogBackupError(job.Name, existingBackup?.Type ?? "Unknown", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Removes a backup job from the manager.
        /// Cleans up associated resources and logs the deletion.
        /// </summary>
        /// <param name="name">Name of the backup job to remove</param>
        /// <returns>True if the job was removed successfully, false if not found</returns>
        public bool RemoveJob(string name)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.Name == name);
                if (backup == null)
                {
                    var existingBackup = GetJob(name);
                    _logController.LogBackupError(name, existingBackup?.Type ?? "Unknown", "Backup not found");
                    return false;
                }

                // Create a deletion log before removing the backup
                var deleteLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = backup.Name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    Message = $"Backup job deleted. Details: Type={backup.Type}, Source={backup.SourcePath}, Target={backup.TargetPath}",
                    LogType = "INFO",
                    ActionType = "BACKUP_DELETE"  // New action type for deleting
                };
                _logger.AddLogEntry(deleteLogEntry);

                // Remove the backup
                _backups.Remove(backup);
                _jobStates.Remove(name);
                SaveBackups();
                SaveStates(_jobStates.Values.ToList());

                return true;
            }
            catch (Exception ex)
            {
                var existingBackup = GetJob(name);
                _logController.LogBackupError(name, existingBackup?.Type ?? "Unknown", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates an existing backup job with new settings.
        /// Maintains job history and logs configuration changes.
        /// </summary>
        /// <param name="name">Current name of the backup job</param>
        /// <param name="updated">Updated backup job configuration</param>
        /// <returns>True if the job was updated successfully, false if not found</returns>
        public bool UpdateJob(string name, Backup updated)
        {
            try
            {
                var index = _backups.FindIndex(b => b.Name == name);
                if (index == -1)
                {
                    var existingBackup = GetJob(name);
                    _logController.LogBackupError(name, existingBackup?.Type ?? "Unknown", "Backup not found");
                    return false;
                }

                // Save the old configuration for log
                var oldBackup = _backups[index];

                // Update the backup
                _backups[index] = updated;
                Config.SaveJobs(_backups);

                // Update state if job name changed
                if (name != updated.Name && _jobStates.ContainsKey(name))
                {
                    var state = _jobStates[name];
                    _jobStates.Remove(name);
                    state.Name = updated.Name;
                    _jobStates[updated.Name] = state;
                    SaveStates(_jobStates.Values.ToList());
                }

                // Create a new log for the edit
                var updateLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = updated.Name,
                    BackupType = updated.Type,
                    SourcePath = updated.SourcePath,
                    TargetPath = updated.TargetPath,
                    Message = $"Backup job edited",
                    LogType = "INFO",
                    ActionType = "BACKUP_EDIT"
                };
                _logger.AddLogEntry(updateLogEntry);
                return true;
            }
            catch (Exception ex)
            {
                var existingBackup = GetJob(name);
                _logController.LogBackupError(name, existingBackup?.Type ?? "Unknown", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves a backup job by its name.
        /// </summary>
        /// <param name="name">Name of the backup job to retrieve</param>
        /// <returns>The backup job if found, null otherwise</returns>
        public Backup? GetJob(string name)
        {
            return _backups.FirstOrDefault(b => b.Name == name);
        }

        /// <summary>
        /// Gets the current state of a backup job.
        /// </summary>
        /// <param name="name">Name of the backup job</param>
        /// <returns>The current state of the backup job, null if not found</returns>
        public StateModel? GetJobState(string name)
        {
            lock (_stateLock)
            {
                if (_jobStates.TryGetValue(name, out var state))
                {
                    return state;
                }
                return null;
            }
        }

        /// <summary>
        /// Decrypts a file using AES encryption.
        /// Tracks and reports decryption progress.
        /// </summary>
        /// <param name="sourceFile">Path to the encrypted file</param>
        /// <param name="targetFile">Path where the decrypted file will be saved</param>
        /// <param name="backupName">Name of the backup job for progress tracking</param>
        private async Task DecryptFileAsync(string sourceFile, string targetFile, string backupName)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                // Read IV from the beginning of the file
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

            EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                backupName,
                sourceFile,
                100,
                true
            ));
        }

        /// <summary>
        /// Encrypts a file using AES encryption.
        /// Tracks and reports encryption progress.
        /// </summary>
        /// <param name="sourceFile">Path to the file to encrypt</param>
        /// <param name="targetFile">Path where the encrypted file will be saved</param>
        /// <param name="backupName">Name of the backup job for progress tracking</param>
        private async Task EncryptFileAsync(string sourceFile, string targetFile, string backupName)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey.Key;
                aes.IV = _encryptionKey.IV;

                // Write IV to the beginning of the file
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

            EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                backupName,
                sourceFile,
                100,
                true
            ));
        }

        /// <summary>
        /// Executes a backup job, copying and optionally encrypting files.
        /// Handles differential backups, progress tracking, and error management.
        /// </summary>
        /// <param name="name">Name of the backup job to execute</param>
        /// <exception cref="InvalidOperationException">Thrown when backup job is not found</exception>
        public async Task ExecuteJob(string name)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup job '{name}' not found");
            }

            // Create a new cancellation token for this job
            var cts = new CancellationTokenSource();
            _jobCancellationTokens[name] = cts;

            var startTime = DateTime.Now;
            var totalFiles = 0;
            var totalBytes = 0L;
            var filesProcessed = 0;
            var bytesTransferred = 0L;
            var totalTransferTime = 0L;
            var totalEncryptionTime = 0L;
            var hasErrors = false;
            var errorMessages = new List<string>();

            try
            {
                // Get all files to process
                var files = await Task.Run(() => Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories));
                totalFiles = files.Length;
                totalBytes = files.Sum(f => new FileInfo(f).Length);

                // Update initial state to Active
                UpdateJobState(name, state =>
                {
                    state.Status = "Active";
                    state.ProgressPercentage = 0;
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = backup.SourcePath;
                    state.CurrentTargetFile = backup.TargetPath;
                });

                // Process each file
                foreach (var sourceFile in files)
                {
                    // Check for cancellation
                    if (cts.Token.IsCancellationRequested)
                    {
                        UpdateJobState(name, state => 
                        {
                            state.Status = "Paused";
                            state.ProgressPercentage = (int)((filesProcessed * 100.0) / totalFiles);
                        });
                        return;
                    }

                    var relativePath = Path.GetRelativePath(backup.SourcePath, sourceFile);
                    var targetFile = Path.Combine(backup.TargetPath, relativePath);
                    var sourceInfo = new FileInfo(sourceFile);
                    var shouldCopy = true;

                    // Check if file needs to be copied (differential backup)
                    if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase) && File.Exists(targetFile))
                    {
                        var targetInfo = new FileInfo(targetFile);
                        shouldCopy = sourceInfo.LastWriteTime > targetInfo.LastWriteTime;
                    }

                    if (shouldCopy)
                    {
                        // Ensure target directory exists
                        var dir = Path.GetDirectoryName(targetFile);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            // Copy and optionally encrypt the file
                            if (backup.Encrypt)
                            {
                                await EncryptFileAsync(sourceFile, targetFile, name);
                                totalEncryptionTime += stopwatch.ElapsedMilliseconds;
                            }
                            else
                            {
                                await Task.Run(() => File.Copy(sourceFile, targetFile, true), cts.Token);
                            }

                            stopwatch.Stop();
                            totalTransferTime += stopwatch.ElapsedMilliseconds;
                            bytesTransferred += sourceInfo.Length;
                            filesProcessed++;

                            // Update progress after each file
                            var progress = (int)((filesProcessed * 100.0) / totalFiles);
                            
                            UpdateJobState(name, state =>
                            {
                                state.ProgressPercentage = progress;
                                state.FilesRemaining = totalFiles - filesProcessed;
                                state.BytesRemaining = totalBytes - bytesTransferred;
                                state.CurrentSourceFile = sourceFile;
                                state.CurrentTargetFile = targetFile;
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            stopwatch.Stop();
                            UpdateJobState(name, state => 
                            {
                                state.Status = "Paused";
                                state.ProgressPercentage = (int)((filesProcessed * 100.0) / totalFiles);
                            });
                            return;
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();
                            hasErrors = true;
                            errorMessages.Add($"Error copying file {sourceFile}: {ex.Message}");

                            // Report error progress
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

                // Update final state to Completed
                UpdateJobState(name, state =>
                {
                    state.Status = "Completed";
                    state.ProgressPercentage = 100;
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = string.Empty;
                    state.CurrentTargetFile = string.Empty;
                });

                // Do not send final progress event if backup completed normally
                if (hasErrors)
                {
                    // Send progress event only in case of error
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        backup.SourcePath,
                        backup.TargetPath,
                        0,
                        (int)(filesProcessed * 100.0 / totalFiles),  // Keep the last real percentage
                        bytesTransferred,
                        totalBytes,
                        filesProcessed,
                        totalFiles,
                        TimeSpan.Zero,
                        false
                    ));
                }
                else {
                    // Send final progress event with completed status and 100% progress
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        backup.SourcePath,
                        backup.TargetPath,
                        0,
                        100, // Ensure progress is 100% on completion
                        totalBytes,
                        totalBytes,
                        totalFiles,
                        totalFiles,
                        TimeSpan.Zero,
                        true // Indicate success
                    ));
                }

                // Create final log entry
                var logEntry = new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    FileSize = totalBytes,
                    TransferTime = totalTransferTime,
                    EncryptionTime = backup.Encrypt ? totalEncryptionTime : -1,
                    Message = hasErrors 
                        ? $"Backup completed with errors: {string.Join("; ", errorMessages)}"
                        : $"Backup completed successfully. Processed {filesProcessed} files ({FormatFileSize(bytesTransferred)}) in {totalTransferTime}ms" + 
                          (backup.Encrypt ? $" (Encryption time: {totalEncryptionTime}ms)" : ""),
                    LogType = hasErrors ? "ERROR" : "INFO",
                    ActionType = "BACKUP_EXECUTION"
                };
                _logger.AddLogEntry(logEntry);
            }
            catch (OperationCanceledException)
            {
                UpdateJobState(name, state => 
                {
                    state.Status = "Paused";
                    state.ProgressPercentage = (int)((filesProcessed * 100.0) / totalFiles);
                });
            }
            catch (Exception ex)
            {
                // Update state on error
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = string.Empty;
                    state.CurrentTargetFile = string.Empty;
                });

                // Report error progress
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

                // Create error log entry
                var errorLogEntry = new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    Message = $"Backup failed: {ex.Message}",
                    LogType = "ERROR",
                    ActionType = "BACKUP_EXECUTION"
                };
                _logger.AddLogEntry(errorLogEntry);
                throw;
            }
            finally
            {
                _jobCancellationTokens.Remove(name);
            }
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string.
        /// Converts to appropriate unit (B, KB, MB, GB, TB).
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted string with appropriate unit</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Restores a backup to a specified target location.
        /// Handles decryption if the backup was encrypted.
        /// </summary>
        /// <param name="name">Name of the backup to restore</param>
        /// <param name="targetPath">Path where the backup will be restored</param>
        /// <exception cref="InvalidOperationException">Thrown when backup is not found</exception>
        public async Task RestoreJob(string name, string targetPath)
        {
            var backup = GetJob(name);
            if (backup == null)
            {
                throw new InvalidOperationException($"Backup '{name}' not found.");
            }

            var startTime = DateTime.Now;
            var totalFiles = 0;
            var totalBytes = 0L;
            var filesProcessed = 0;
            var bytesTransferred = 0L;
            var totalTransferTime = 0L;
            var totalEncryptionTime = 0L;
            var hasErrors = false;
            var errorMessages = new List<string>();

            try
            {
                // Get all files to restore
                var files = await Task.Run(() => Directory.GetFiles(backup.TargetPath, "*.*", SearchOption.AllDirectories));
                totalFiles = files.Length;
                totalBytes = files.Sum(f => new FileInfo(f).Length);

                // Update initial state
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

                // Process each file
                foreach (var sourceFile in files)
                {
                    var relativePath = Path.GetRelativePath(backup.TargetPath, sourceFile);
                    var targetFile = Path.Combine(targetPath, relativePath);
                    var sourceInfo = new FileInfo(sourceFile);

                    // Ensure target directory exists
                    var dir = Path.GetDirectoryName(targetFile);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        // Copy and optionally decrypt the file
                        if (backup.Encrypt)
                        {
                            await DecryptFileAsync(sourceFile, targetFile, name);
                            totalEncryptionTime += stopwatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            await Task.Run(() => File.Copy(sourceFile, targetFile, true));
                        }

                        stopwatch.Stop();
                        totalTransferTime += stopwatch.ElapsedMilliseconds;
                        bytesTransferred += sourceInfo.Length;
                        filesProcessed++;

                        // Update progress
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
                        hasErrors = true;
                        errorMessages.Add($"Error restoring file {sourceFile}: {ex.Message}");

                        // Report error progress
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

                // Update final state
                UpdateJobState(name, state =>
                {
                    state.Status = hasErrors ? "Error" : "Completed";
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Report final progress
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
                    !hasErrors
                ));

                // Create final log entry
                var logEntry = new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.TargetPath,
                    TargetPath = targetPath,
                    FileSize = totalBytes,
                    TransferTime = totalTransferTime,
                    EncryptionTime = backup.Encrypt ? totalEncryptionTime : -1,
                    Message = hasErrors 
                        ? $"Restore completed with errors: {string.Join("; ", errorMessages)}"
                        : $"Restore completed successfully. Processed {filesProcessed} files ({FormatFileSize(bytesTransferred)}) in {totalTransferTime}ms" + 
                          (backup.Encrypt ? $" (Decryption time: {totalEncryptionTime}ms)" : ""),
                    LogType = hasErrors ? "ERROR" : "INFO",
                    ActionType = "BACKUP_RESTORE"
                };
                _logger.AddLogEntry(logEntry);
            }
            catch (Exception ex)
            {
                // Update state on error
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Report error progress
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

                // Create error log entry
                var errorLogEntry = new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    SourcePath = backup.TargetPath,
                    TargetPath = targetPath,
                    Message = $"Restore failed: {ex.Message}",
                    LogType = "ERROR",
                    ActionType = "BACKUP_RESTORE"
                };
                _logger.AddLogEntry(errorLogEntry);
                throw;
            }
        }

        /// <summary>
        /// Displays all logs using the log controller.
        /// Provides access to the backup operation history.
        /// </summary>
        public void DisplayLogs()
        {
            _logger.DisplayLogs();
        }
    }
}