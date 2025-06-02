using EasySaveV3._0.Models;
using EasySaveV3._0.Controllers;
using System.Text.Json;
using System.Diagnostics;
using EasySaveLogging;
using System.Collections.Concurrent;

namespace EasySaveV3._0.Managers
{
    /// <summary>
    /// Defines the possible states a backup job can be in
    /// </summary>
    public static class JobStatus
    {
        public const string NotStarted = "NotStarted";
        public const string Active = "Active";
        public const string Paused = "Paused";
        public const string Completed = "Completed";
        public const string Error = "Error";
    }

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
        private readonly Logger _logger;               // Main logging service
        private readonly SettingsController _settingsController;  // Manages settings including encryption

        // Thread synchronization primitives
        private readonly object _stateLock = new();     // Lock for state modifications
        private readonly SemaphoreSlim _largeFileSemaphore = new SemaphoreSlim(1, 1);  // Lock for large file operations
        private long _maxLargeFileSizeBytes;  // Maximum file size before using large file semaphore
        private readonly string _cryptoSoftPath;  // Path to CryptoSoft executable

        // Progress tracking events
        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;        // Fired when file operation progress changes
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;  // Fired when encryption progress changes
        public event EventHandler<string>? BusinessSoftwareDetected;  // Fired when business software is detected during backup
        public event EventHandler<string>? BusinessSoftwareResumed;

        // Add thread pool configuration
        private static readonly SemaphoreSlim _priorityThreadPool;
        private static readonly SemaphoreSlim _normalThreadPool;
        private static readonly int _maxPriorityThreads;
        private static readonly int _maxNormalThreads;
        private static readonly int _maxTotalThreads;
        private static readonly object _threadPoolLock = new object();
        private static int _activeThreadCount = 0;

        // Remove global backup semaphore, keep only CryptoSoft semaphore for thread safety
        private static readonly SemaphoreSlim _cryptoSoftSemaphore = new SemaphoreSlim(1, 1);

        // Add a dictionary to track active backups and their resources
        private static readonly ConcurrentDictionary<string, BackupResources> _activeBackups = new();

        // Class to hold resources for each backup job
        private class BackupResources : IDisposable
        {
            public CancellationTokenSource CancellationTokenSource { get; } = new();
            public Stopwatch Stopwatch { get; } = new();
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    CancellationTokenSource.Dispose();
                    IsDisposed = true;
                }
            }
        }

        // Static constructor to initialize thread pools
        static BackupManager()
        {
            // Configure thread pools based on system resources
            _maxTotalThreads = Environment.ProcessorCount * 2; // Allow 2x CPU cores for I/O operations
            _maxPriorityThreads = Math.Max(1, Environment.ProcessorCount / 2); // Reserve half for priority operations
            _maxNormalThreads = _maxTotalThreads - _maxPriorityThreads;

            _priorityThreadPool = new SemaphoreSlim(_maxPriorityThreads, _maxPriorityThreads);
            _normalThreadPool = new SemaphoreSlim(_maxNormalThreads, _maxNormalThreads);
        }

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
            _logger = Logger.GetInstance();
            _settingsController = SettingsController.Instance;

            // Set large file size limit from settings
            _maxLargeFileSizeBytes = _settingsController.MaxLargeFileSizeKB * 1024L;  // Convert KB to bytes
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
            try
            {
                await _cryptoSoftSemaphore.WaitAsync();
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
                if (process == null)
                {
                    throw new Exception("Failed to start CryptoSoft process");
                }
                await process.WaitForExitAsync();
                if (process.ExitCode < 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"CryptoSoft error: {error}");
                }
            }
            finally
            {
                _cryptoSoftSemaphore.Release();
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

        // Add thread management methods
        private async Task AcquireThreadSlotAsync(bool isPriority)
        {
            var semaphore = isPriority ? _priorityThreadPool : _normalThreadPool;
            await semaphore.WaitAsync();
            
            lock (_threadPoolLock)
            {
                _activeThreadCount++;
                if (_activeThreadCount > _maxTotalThreads)
                {
                    // Log warning if we exceed max threads
                    _logger.AddLogEntry(new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Message = $"Warning: Active thread count ({_activeThreadCount}) exceeds maximum ({_maxTotalThreads})",
                        LogType = "WARNING",
                        ActionType = "THREAD_POOL"
                    });
                }
            }
        }

        private void ReleaseThreadSlot(bool isPriority)
        {
            var semaphore = isPriority ? _priorityThreadPool : _normalThreadPool;
            semaphore.Release();
            
            lock (_threadPoolLock)
            {
                _activeThreadCount--;
            }
        }

        // Create a struct to hold file processing results
        private struct FileProcessingResult
        {
            public bool HasError { get; set; }
            public string? ErrorMessage { get; set; }
            public long BytesTransferred { get; set; }
            public long EncryptionTime { get; set; }
            public bool WasProcessed { get; set; }
        }

        private async Task<FileProcessingResult> ProcessFileAsync(string sourceFile, Backup backup, string name, 
            object jobLock, CancellationToken token)
        {
            var result = new FileProcessingResult();
            
            if (token.IsCancellationRequested)
            {
                lock (jobLock)
                {
                    UpdateJobState(name, state => 
                    {
                        state.Status = JobStatus.Completed;
                    });
                }
                return result;
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
                    _logger.LogAdminAction(name, "DIR_CREATE", $"Created directory: {dir}");
                }

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // Copy and optionally encrypt the file
                    await CopyFileAsync(sourceFile, targetFile, name);
                    
                    result.BytesTransferred = sourceInfo.Length;
                    result.EncryptionTime = stopwatch.ElapsedMilliseconds;
                    result.WasProcessed = true;

                    // Report progress
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        sourceFile,
                        targetFile,
                        sourceInfo.Length,
                        0, // Progress will be calculated by caller
                        0, // Bytes transferred will be calculated by caller
                        0, // Total bytes will be calculated by caller
                        0, // Files processed will be calculated by caller
                        0, // Total files will be calculated by caller
                        stopwatch.Elapsed,
                        true
                    ));
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    result.HasError = true;
                    result.ErrorMessage = $"Error copying file {sourceFile}: {ex.Message}";

                    // Report error progress
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        sourceFile,
                        targetFile,
                        sourceInfo.Length,
                        0, // Progress will be calculated by caller
                        0, // Bytes transferred will be calculated by caller
                        0, // Total bytes will be calculated by caller
                        0, // Files processed will be calculated by caller
                        0, // Total files will be calculated by caller
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

            return result;
        }

        /// <summary>
        /// Executes a backup job, copying and optionally encrypting files.
        /// Handles differential backups, progress tracking, and error management.
        /// </summary>
        /// <param name="name">Name of the backup job to execute</param>
        /// <param name="cancellationToken">Optional token to cancel the operation</param>
        /// <exception cref="InvalidOperationException">Thrown when backup job is not found</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled</exception>
        public async Task ExecuteJob(string name, CancellationToken cancellationToken = default)
        {
            Backup? backup = null;  // Declare backup variable outside try block
            try
            {
                 
                backup = GetJob(name);
                if (backup == null)
                    throw new InvalidOperationException($"Backup job '{name}' not found");

                // Create and register backup resources
                var resources = new BackupResources();
                if (!_activeBackups.TryAdd(name, resources))
                {
                    resources.Dispose();
                    throw new InvalidOperationException($"Backup job '{name}' is already running");
                }

                resources.Stopwatch.Start();
                var startTime = DateTime.Now;
                var totalFiles = 0;
                var totalBytes = 0L;
                var filesProcessed = 0;
                var bytesTransferred = 0L;
                var totalEncryptionTime = 0L;
                var hasErrors = false;
                var errorMessages = new List<string>();
                var processedFilesOrder = new List<string>();
                var jobLock = new object();

                // Get files to process and separate by priority
                var allFiles = await GetFilesToProcessAsync(backup);
                var priorityFiles = allFiles.Where(f => Config.GetPriorityExtensions().Contains(Path.GetExtension(f).ToLower())).ToList();
                var normalFiles = allFiles.Where(f => !Config.GetPriorityExtensions().Contains(Path.GetExtension(f).ToLower())).ToList();
                
                lock (jobLock)
                {
                    totalFiles = allFiles.Count;
                    totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                }

                // Log the start of file processing with priority information
                var fileList = string.Join("\n", 
                    priorityFiles.Select(f => $"- {Path.GetFileName(f)} (Priority)").Concat(
                    normalFiles.Select(f => $"- {Path.GetFileName(f)} (Non-priority)")));

                _logger.AddLogEntry(new LogEntry
                {
                    Timestamp = startTime,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    Message = $"Starting backup with {totalFiles} files ({priorityFiles.Count} priority, {normalFiles.Count} non-priority):\n{fileList}",
                    LogType = "INFO",
                    ActionType = "BACKUP_STARTED"
                });

                // Update initial state
                UpdateJobState(name, state =>
                {
                    state.Status = JobStatus.Active;
                    state.ProgressPercentage = 0;
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = backup.SourcePath;
                    state.CurrentTargetFile = backup.TargetPath;
                });

                // Process priority files first
                if (priorityFiles.Any())
                {
                    var priorityOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _maxPriorityThreads,
                        CancellationToken = cancellationToken
                    };

                    var priorityResults = await Task.WhenAll(
                        priorityFiles.Select(async sourceFile =>
                        {
                            try
                            {
                                bool alreadySignaledPause = false;
                                while (_settingsController.IsBusinessSoftwareRunning())
                                {
                                    if (!alreadySignaledPause)
                                    {
                                        BusinessSoftwareDetected?.Invoke(this, name);
                                        alreadySignaledPause = true;
                                    }

                                    UpdateJobState(name, state =>
                                    {
                                        if (state.Status != JobStatus.Paused)
                                        {
                                            state.Status = JobStatus.Paused;
                                            _logger.LogAdminAction(name, "BACKUP_PAUSED",
                                                                  "Backup job paused (business software detected)");
                                        }
                                    });

                                    await Task.Delay(500, cancellationToken);
                                }
                                UpdateJobState(name, state =>
                                {
                                    state.Status = JobStatus.Active;
                                    _logger.LogAdminAction(name, "BACKUP_RESUMED",
                                                          "Backup job resumed (business software stopped)");
                                });
                                BusinessSoftwareResumed?.Invoke(this, name);


                                await AcquireThreadSlotAsync(true);
                                var result = await ProcessFileAsync(sourceFile, backup, name, jobLock, cancellationToken);
                                
                                lock (jobLock)
                                {
                                    if (result.WasProcessed)
                                    {
                                        filesProcessed++;
                                        bytesTransferred += result.BytesTransferred;
                                        if (backup.Encrypt)
                                        {
                                            totalEncryptionTime += result.EncryptionTime;
                                        }
                                        processedFilesOrder.Add(sourceFile);
                                        
                                        if (result.HasError)
                                        {
                                            hasErrors = true;
                                            errorMessages.Add(result.ErrorMessage!);
                                        }

                                        // Update progress
                                        var progress = (int)((filesProcessed * 100.0) / totalFiles);
                                        UpdateJobState(name, state =>
                                        {
                                            state.ProgressPercentage = progress;
                                            state.FilesRemaining = totalFiles - filesProcessed;
                                            state.BytesRemaining = totalBytes - bytesTransferred;
                                            state.CurrentSourceFile = sourceFile;
                                            state.CurrentTargetFile = Path.Combine(backup.TargetPath, Path.GetRelativePath(backup.SourcePath, sourceFile));
                                        });
                                    }
                                }
                                
                                return result;
                            }
                            finally
                            {
                                ReleaseThreadSlot(true);
                            }
                        })
                    );
                }

                // Process normal files
                if (normalFiles.Any())
                {

                    

                    var normalResults = await Task.WhenAll(
                        normalFiles.Select(async sourceFile =>
                        {
                            try
                            {
                                bool alreadySignaledPause = false;
                                while (_settingsController.IsBusinessSoftwareRunning())
                                {
                                    if (!alreadySignaledPause)
                                    {
                                        BusinessSoftwareDetected?.Invoke(this, name);
                                        alreadySignaledPause = true;
                                    }

                                    UpdateJobState(name, state =>
                                    {
                                        if (state.Status != JobStatus.Paused)
                                        {
                                            state.Status = JobStatus.Paused;
                                            _logger.LogAdminAction(name, "BACKUP_PAUSED",
                                                                  "Backup job paused (business software detected)");
                                        }
                                    });

                                    await Task.Delay(500, cancellationToken);
                                }

                                UpdateJobState(name, state =>
                                {
                                    state.Status = JobStatus.Active;
                                    _logger.LogAdminAction(name, "BACKUP_RESUMED",
                                                          "Backup job resumed (business software stopped)");
                                });
                                BusinessSoftwareResumed?.Invoke(this, name);
                                await AcquireThreadSlotAsync(true);
                                var result = await ProcessFileAsync(sourceFile, backup, name, jobLock, cancellationToken);
                                
                                lock (jobLock)
                                {
                                    if (result.WasProcessed)
                                    {
                                        filesProcessed++;
                                        bytesTransferred += result.BytesTransferred;
                                        if (backup.Encrypt)
                                        {
                                            totalEncryptionTime += result.EncryptionTime;
                                        }
                                        processedFilesOrder.Add(sourceFile);
                                        
                                        if (result.HasError)
                                        {
                                            hasErrors = true;
                                            errorMessages.Add(result.ErrorMessage!);
                                        }

                                        // Update progress
                                        var progress = (int)((filesProcessed * 100.0) / totalFiles);
                                        UpdateJobState(name, state =>
                                        {
                                            state.ProgressPercentage = progress;
                                            state.FilesRemaining = totalFiles - filesProcessed;
                                            state.BytesRemaining = totalBytes - bytesTransferred;
                                            state.CurrentSourceFile = sourceFile;
                                            state.CurrentTargetFile = Path.Combine(backup.TargetPath, Path.GetRelativePath(backup.SourcePath, sourceFile));
                                        });
                                    }
                                }
                                
                                return result;
                            }

                            finally
                            {
                                ReleaseThreadSlot(false);
                            }
                        })
                    );
                }

                // Update final state
                UpdateJobState(name, state =>
                {
                    state.Status = cancellationToken.IsCancellationRequested ? JobStatus.Completed : 
                                 (hasErrors ? JobStatus.Error : JobStatus.Completed);
                    state.ProgressPercentage = 100;
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Create final log entry
                var finalLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    BackupType = backup.Type,
                    SourcePath = backup.SourcePath,
                    TargetPath = backup.TargetPath,
                    FileSize = bytesTransferred,
                    TransferTime = resources.Stopwatch.ElapsedMilliseconds,
                    EncryptionTime = backup.Encrypt ? totalEncryptionTime : -1,
                    Message = hasErrors 
                        ? $"Backup completed with errors: {string.Join("; ", errorMessages)}\n"
                        : $"Backup completed successfully. Processed {filesProcessed} files ({FormatFileSize(bytesTransferred)}) in {resources.Stopwatch.ElapsedMilliseconds:F0}ms" + 
                          (backup.Encrypt ? $" (Encryption time: {totalEncryptionTime}ms)" : "") + "\n" +
                          "Files processed in order:\n" +
                          string.Join("\n", processedFilesOrder.Select((f, index) => 
                          {
                              var extension = Path.GetExtension(f).ToLower();
                              var isPriority = Config.GetPriorityExtensions().Contains(extension);
                              return $"{index + 1}. {Path.GetFileName(f)} ({(isPriority ? "Priority" : "Non-priority")})";
                          })),
                    LogType = hasErrors ? "ERROR" : "INFO",
                    ActionType = hasErrors ? "BACKUP_ERROR" : "BACKUP_COMPLETED"
                 };
                _logger.AddLogEntry(finalLogEntry);
            }
            catch (OperationCanceledException)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = JobStatus.Completed;
                    state.CurrentSourceFile = string.Empty;
                    state.CurrentTargetFile = string.Empty;
                });

                var cancelLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    BackupType = backup?.Type ?? "Unknown",  // Use null-conditional operator
                    SourcePath = backup?.SourcePath ?? string.Empty,  // Use null-conditional operator
                    TargetPath = backup?.TargetPath ?? string.Empty,  // Use null-conditional operator
                    Message = "Backup operation cancelled by user",
                    LogType = "INFO",
                    ActionType = "BACKUP_CANCELLED"
                };
                _logger.AddLogEntry(cancelLogEntry);
                throw;
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = JobStatus.Error;
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                var errorLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    BackupType = backup?.Type ?? "Unknown",  // Use null-conditional operator
                    SourcePath = backup?.SourcePath ?? string.Empty,  // Use null-conditional operator
                    TargetPath = backup?.TargetPath ?? string.Empty,  // Use null-conditional operator
                    Message = $"Backup failed: {ex.Message}",
                    LogType = "ERROR",
                    ActionType = "BACKUP_ERROR"
                };
                _logger.AddLogEntry(errorLogEntry);
                throw;
            }
            finally
            {
                if (_activeBackups.TryRemove(name, out var backupResources))
                {
                    backupResources.Dispose();
                }
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

        // Add method to get thread pool statistics
        public (int ActiveThreads, int MaxTotalThreads, int MaxPriorityThreads, int MaxNormalThreads) GetThreadPoolStats()
        {
            lock (_threadPoolLock)
            {
                return (_activeThreadCount, _maxTotalThreads, _maxPriorityThreads, _maxNormalThreads);
            }
        }

        // Add method to check if a backup is running
        public bool IsBackupRunning(string name)
        {
            return _activeBackups.ContainsKey(name);
        }

        // Add method to get active backup count
        public int GetActiveBackupCount()
        {
            return _activeBackups.Count;
        }

        // Add method to check for business software and raise event
        private void CheckForBusinessSoftware(string jobName)
        {
            if (_settingsController.IsBusinessSoftwareRunning())
            {
                BusinessSoftwareDetected?.Invoke(this, jobName);
            }
        }
    }
}
