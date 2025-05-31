using System;
using System.IO;
using System.Threading.Tasks;
using EasySaveV3._0.Models;
using EasySaveV3._0.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using EasySaveLogging;
using System.Threading;
using System.Xml.Linq;

namespace EasySaveV3._0.Managers
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
        private readonly LanguageManager _languageManager;
        // Configuration constants
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
        private readonly SettingsController _settingsController;  // Manages settings including encryption

        // Thread synchronization primitives
        private readonly object _stateLock = new();     // Lock for state modifications
        private readonly object _backupLock = new();    // Lock for backup list modifications
        private readonly Dictionary<string, CancellationTokenSource> _jobCancellationTokens = new();  // Cancellation tokens for running jobs

        // Progress tracking events
        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;        // Fired when file operation progress changes
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;  // Fired when encryption progress changes

        public event EventHandler<string>? BusinessSoftwareDetected;
        private readonly string _cryptoSoftPath;

        // Limite de taille (n Ko) et contrôle mutuel des gros transferts
        private readonly SemaphoreSlim _largeFileSemaphore;
        private readonly long _maxLargeFileSizeBytes;


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
            _languageManager = LanguageManager.Instance; 

            _jsonOptions = new JsonSerializerOptions

            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _logController = LogController.Instance;
            _encryptionKey = new EncryptionKey();
            _logger = Logger.GetInstance();
            _settingsController = SettingsController.Instance;

            // Valeur n Ko paramétrable
            _maxLargeFileSizeBytes = _settingsController.MaxLargeFileSizeKB * 1000L;
            _largeFileSemaphore = new SemaphoreSlim(1, 1);
            _cryptoSoftPath = Config.GetCryptoSoftPath();
            if (string.IsNullOrWhiteSpace(_cryptoSoftPath) || !File.Exists(_cryptoSoftPath))
            {
                throw new Exception($"CryptoSoftPath is invalid or file does not exist: '{_cryptoSoftPath}'");
            }
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
                    ActionType = "BACKUP_CREATED"
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
        /// Encrypts a file using XOR encryption via CryptoSoft.
        /// </summary>
        /// <param name="sourceFile">Path to the file to encrypt</param>
        /// <param name="targetFile">Path where the encrypted file will be saved</param>
        private async Task EncryptFileWithCryptoSoftAsync(string sourceFile, string targetFile)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"encrypt \"{sourceFile}\" \"{targetFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();
            if (process.ExitCode < 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"CryptoSoft error: {error}");
            }
        }

        /// <summary>
        /// Decrypts a file using XOR decryption via CryptoSoft.
        /// </summary>
        /// <param name="sourceFile">Path to the encrypted file</param>
        /// <param name="targetFile">Path where the decrypted file will be saved</param>
        private async Task DecryptFileWithCryptoSoftAsync(string sourceFile, string targetFile)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"decrypt \"{sourceFile}\" \"{targetFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();
            if (process.ExitCode < 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"CryptoSoft error: {error}");
            }
        }

        private async Task CopyFileAsync(string sourceFile, string targetFile, string backupName)
        {
          var backup = GetJob(backupName);
          if (backup == null)
              throw new InvalidOperationException($"Backup job '{backupName}' not found");

          // Ensure the target directory exists
          var targetDir = Path.GetDirectoryName(targetFile);
          if (targetDir != null && !Directory.Exists(targetDir))
          {
              Directory.CreateDirectory(targetDir);
              _logger.LogAdminAction(backupName, "DIR_CREATE", $"Created directory: {targetDir}");
          }

          // Determine if this file should be encrypted
          bool shouldEncrypt = backup.Encrypt && _settingsController.ShouldEncryptFile(sourceFile);

          if (shouldEncrypt)
          {
              // Notify UI: encryption started
              EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(backupName, sourceFile, 0));

              try
              {
                  // Log encryption start
                  _logger.AddLogEntry(new LogEntry
                  {
                      Timestamp = DateTime.Now,
                      BackupName = backupName,
                      SourcePath = sourceFile,
                      TargetPath = targetFile,
                      Message = $"Starting encryption of file: {sourceFile}",
                      LogType = "INFO",
                      ActionType = "ENCRYPTION_START"
                  });

                  // Perform encryption
                  await EncryptFileWithCryptoSoftAsync(sourceFile, targetFile);

                  // Log encryption success
                  _logger.AddLogEntry(new LogEntry
                  {
                      Timestamp = DateTime.Now,
                      BackupName = backupName,
                      SourcePath = sourceFile,
                      TargetPath = targetFile,
                      Message = $"Completed encryption of file: {sourceFile}",
                      LogType = "INFO",
                      ActionType = "ENCRYPTION_COMPLETE"
                  });

                  // Notify UI: encryption complete
                  EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(backupName, sourceFile, 100, true));
              }
              catch (Exception ex)
              {
                  // Log encryption failure
                  _logger.AddLogEntry(new LogEntry
                  {
                      Timestamp = DateTime.Now,
                      BackupName = backupName,
                      SourcePath = sourceFile,
                      TargetPath = targetFile,
                      Message = $"Encryption error for file {sourceFile}: {ex.Message}",
                      LogType = "ERROR",
                      ActionType = "ENCRYPTION_ERROR"
                  });

                  // Notify UI: encryption error
                  EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(backupName, sourceFile, 0, true, true, ex.Message));
                  throw;
              }
          }
          else
          {
              // Log that encryption is skipped
              _logger.AddLogEntry(new LogEntry
              {
                  Timestamp = DateTime.Now,
                  BackupName = backupName,
                  SourcePath = sourceFile,
                  TargetPath = targetFile,
                  Message = $"Skipping encryption for file: {sourceFile}",
                  LogType = "INFO",
                  ActionType = "ENCRYPTION_SKIP"
              });

              // Perform normal copy
              await Task.Run(() => File.Copy(sourceFile, targetFile, true));
          }
        }

        /// <summary>
        /// Checks if there are any priority files remaining in the current backup job.
        /// </summary>
        /// <param name="backup">The backup job to check</param>
        /// <returns>True if there are priority files remaining in this job, false otherwise</returns>
        private bool HasPriorityFilesRemaining(Backup backup)
        {
            var priorityExtensions = Config.GetPriorityExtensions();
            var state = GetJobState(backup.Name);
            
            // Skip if job is not active
            if (state == null || state.Status != "Active")
                return false;

            // Get all files in the source directory
            var files = Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories);
            
            // Get the list of already processed files from the state
            var processedFiles = state.ProcessedFiles ?? new HashSet<string>();
            
            // Check if any remaining files have priority extensions
            var hasPriorityFiles = files.Any(f => 
            {
                // Skip already processed files
                if (processedFiles.Contains(f))
                    return false;
                    
                var extension = Path.GetExtension(f).ToLower();
                return priorityExtensions.Contains(extension);
            });

            return hasPriorityFiles;
        }

        /// <summary>
        /// Gets the list of files that need to be processed for a backup job,
        /// respecting the priority of files.
        /// </summary>
        /// <param name="backup">The backup job to process</param>
        /// <returns>List of files to process, ordered by priority</returns>
        private async Task<List<string>> GetFilesToProcessAsync(Backup backup)
        {
            // Get all files in the source directory
            var files = await Task.Run(() => Directory.GetFiles(backup.SourcePath, "*.*", SearchOption.AllDirectories));
            var priorityExtensions = Config.GetPriorityExtensions();
            
            // Sort files by priority - priority files first, then non-priority files
            var sortedFiles = files.OrderByDescending(f => priorityExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();
            
            return sortedFiles;
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
                throw new InvalidOperationException($"Backup job '{name}' not found");

            // Create a new cancellation token for this job
            var cts = new CancellationTokenSource();
            _jobCancellationTokens[name] = cts;

            var startTime = DateTime.Now;
            var filesProcessed = 0;
            var bytesTransferred = 0L;
            var totalTransferTime = 0L;
            var totalEncryptionTime = 0L;
            var hasErrors = false;
            var errorMessages = new List<string>();
            var processedFilesOrder = new List<string>(); 

            try
            {
                // 1) Retrieve the list of files to process, with priorityé
                var sortedFiles = await GetFilesToProcessAsync(backup);

                // 2) Calculate totals
                var totalFiles = sortedFiles.Count;
                var totalBytes = sortedFiles.Sum(f => new FileInfo(f).Length);

                // 3) Log the start of file processing
                var fileList = string.Join("\n", sortedFiles.Select(f => $"- {Path.GetFileName(f)} ({(Config.GetPriorityExtensions().Contains(Path.GetExtension(f).ToLower()) ? "Priority" : "Non-priority")})"));

                _logger.AddLogEntry(new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    Message = $"Starting backup with {totalFiles} files in priority order:\n{fileList}",
                    LogType = "INFO",
                    ActionType = "BACKUP_STARTED"
                });

                // 4) Mettre à jour l’état initial
                UpdateJobState(name, state =>
                {
                    state.Status = "Active";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = backup.SourcePath;
                    state.CurrentTargetFile = backup.TargetPath;
                });

                // 5) Traiter chaque fichier
                foreach (var sourceFile in sortedFiles)
                {
                    // 5.1) Annulation ?
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

                    // 5.2) Gestion du “gros” fichier (> n Ko)
                    bool isLarge = sourceInfo.Length > _maxLargeFileSizeBytes;
                    if (isLarge)
                    {
                        await _largeFileSemaphore.WaitAsync(cts.Token);
                    }

                    try
                    {
                        // 5.3) Differential ?
                        bool shouldCopy = true;
                        if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase)
                            && File.Exists(targetFile))
                        {
                            var targetInfo = new FileInfo(targetFile);
                            shouldCopy = sourceInfo.LastWriteTime > targetInfo.LastWriteTime;
                        }

                        if (shouldCopy)
                        {
                            var stopwatch = Stopwatch.StartNew();
                            try
                            {
                                // Copy and optionally encrypt the file
                                await CopyFileAsync(sourceFile, targetFile, name);
                              
                                totalEncryptionTime += stopwatch.ElapsedMilliseconds;
                                bytesTransferred += sourceInfo.Length;
                                processedFilesOrder.Add(sourceFile);
                                filesProcessed++;

                                // Mettre à jour l’état après copie
                                var progress = (int)((filesProcessed * 100.0) / totalFiles);
                                UpdateJobState(name, state =>
                                {
                                    state.ProgressPercentage = progress;
                                    state.FilesRemaining = totalFiles - filesProcessed;
                                    state.BytesRemaining = totalBytes - bytesTransferred;
                                    state.CurrentSourceFile = sourceFile;
                                    state.CurrentTargetFile = targetFile;
                                });

                                // Réévaluer la liste toutes les 10 copies
                                if (filesProcessed % 10 == 0)
                                {
                                    var updatedFiles = await GetFilesToProcessAsync(backup);
                                    if (updatedFiles.Count > sortedFiles.Count)
                                    {
                                        sortedFiles = updatedFiles;
                                        totalFiles = sortedFiles.Count;
                                        totalBytes = sortedFiles.Sum(f => new FileInfo(f).Length);

                                        UpdateJobState(name, state =>
                                        {
                                            state.TotalFilesCount = totalFiles;
                                            state.TotalFilesSize = totalBytes;
                                            state.FilesRemaining = totalFiles - filesProcessed;
                                            state.BytesRemaining = totalBytes - bytesTransferred;
                                        });
                                    }
                                }
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

                                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                                    name, sourceFile, targetFile, sourceInfo.Length,
                                    (int)(filesProcessed * 100.0 / totalFiles),
                                    bytesTransferred, totalBytes,
                                    filesProcessed, totalFiles,
                                    stopwatch.Elapsed, false
                                ));
                                if (backup.Encrypt)
                                {
                                    EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                                        name, sourceFile, 0, true, true, ex.Message
                                    ));
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (isLarge)
                            _largeFileSemaphore.Release();
                    }
                }

                // 6) Finalisation : marquer Completed
                UpdateJobState(name, state =>
                {
                    state.Status = hasErrors ? "Error" : "Completed";
                    state.ProgressPercentage = hasErrors ? state.ProgressPercentage : 100;
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // 7) Final log
                var finalLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    FileSize = totalBytes,
                    TransferTime = totalTransferTime,
                    EncryptionTime = backup.Encrypt ? totalEncryptionTime : -1,
                    Message = hasErrors
                        ? $"Completed with errors: {string.Join("; ", errorMessages)}"
                        : $"Completed successfully: {filesProcessed} files ({FormatFileSize(bytesTransferred)}) in {totalTransferTime}ms" +
                          (backup.Encrypt ? $" (Encryption: {totalEncryptionTime}ms)" : ""),
                    LogType = hasErrors ? "ERROR" : "INFO",
                    ActionType = hasErrors ? "BACKUP_ERROR" : "BACKUP_COMPLETED"
                 };
                _logger.AddLogEntry(finalLogEntry);
            }
            catch (OperationCanceledException)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = "Paused";
                    state.ProgressPercentage = 0;
                });
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = "Error";
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                    name, backup.SourcePath, backup.TargetPath, 0, 0, 0, 0, 0, 0, TimeSpan.Zero, false
                ));

                var errorLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    Message = $"Backup failed: {ex.Message}",
                    LogType = "ERROR",
                    ActionType = "BACKUP_ERROR"
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
                        await CopyFileAsync(sourceFile, targetFile, name);
                        totalEncryptionTime += stopwatch.ElapsedMilliseconds;
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
                    Timestamp = DateTime.Now,
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
                    Timestamp = DateTime.Now,
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