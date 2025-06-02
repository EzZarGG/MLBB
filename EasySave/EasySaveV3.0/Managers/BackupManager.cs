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
        public const string Ready = "Ready";
        public const string NotStarted = "NotStarted";
        public const string Active = "Active";
        public const string Completed = "Completed";
        public const string Error = "Error";
        public const string Paused = "Paused";
        public const string Cancelled = "Cancelled";
        public const string Stopped = "Stopped";
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
        internal static readonly ConcurrentDictionary<string, BackupResources> _activeBackups = new();

        // Class to hold resources for each backup job
        internal class BackupResources : IDisposable
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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            try
            {
                // Ensure thread-safe access to _jobStates
            lock (_stateLock)
            {
                if (_jobStates.TryGetValue(name, out var state))
                {
                    return state;
                }
                return null;
                }
            }
            catch (Exception ex)
            {
                _logController.LogBackupError(name, "Unknown", ex.Message);
                throw; // Re-throw after logging
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

        private async Task CopyFileAsync(string sourceFile, string targetFile, string backupName, CancellationToken token)
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

                  // Perform encryption (this call is not pausable internally with current CryptoSoft implementation)
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
              catch (OperationCanceledException)
              {
                   // Propagate cancellation from EncryptFileWithCryptoSoftAsync
                   throw;
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
                  throw; // Re-throw after logging
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

              // Perform file copy with support for pausing and cancellation
              await CopyFileStreamAsync(sourceFile, targetFile, backupName, token);
          }
        }

        // New method for streaming file copy with pause/cancellation support
        private async Task CopyFileStreamAsync(string sourceFile, string targetFile, string backupName, CancellationToken token)
        {
            // Add artificial delay for demonstration purposes
            await Task.Delay(10000, token); // 10 seconds delay

            // Check for cancellation or pause before starting copy
            while (GetJobState(backupName)?.Status == JobStatus.Paused)
            {
                await Task.Delay(50, token); // Wait while paused, checking more frequently
                token.ThrowIfCancellationRequested(); // Check for stop during pause
            }
            
            // Check for cancellation before proceeding
            token.ThrowIfCancellationRequested();

            long totalBytes = new FileInfo(sourceFile).Length;
            long bytesCopied = 0;
            byte[] buffer = new byte[BUFFER_SIZE];

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    // Check for cancellation or pause before writing each chunk
                    while (GetJobState(backupName)?.Status == JobStatus.Paused)
                    {
                        await Task.Delay(50, token); // Wait while paused, checking more frequently
                        token.ThrowIfCancellationRequested(); // Check for stop during pause
                    }
                    
                    // Check for cancellation before writing
                    token.ThrowIfCancellationRequested();

                    await targetStream.WriteAsync(buffer, 0, bytesRead, token);
                    bytesCopied += bytesRead;

                    // Report progress (percentage based on bytes copied)
                    var progressPercentage = (int)((double)bytesCopied * 100 / totalBytes);
                     FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                            backupName,
                            sourceFile,
                            targetFile,
                            totalBytes,
                            progressPercentage,
                            bytesCopied,
                            totalBytes,
                            0, // Files processed - handled in ExecuteJob
                            0, // Total files - handled in ExecuteJob
                            TimeSpan.Zero, // Time elapsed - handled in ExecuteJob
                            true // Indicate progress
                        ));
                }
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
            CancellationToken token)
        {
            var result = new FileProcessingResult();
            
            // Check for cancellation or pause before processing each file
            while (GetJobState(name)?.Status == JobStatus.Paused)
            {
                await Task.Delay(50, token); // Wait while paused, checking more frequently
                token.ThrowIfCancellationRequested(); // Check for stop during pause
            }
            
            // Check for cancellation before proceeding with processing
            token.ThrowIfCancellationRequested();

            // Add delay to slow down backup progress (already respects token)
            await Task.Delay(10000, token); // Keep this delay as requested for visualization

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
                    // Get current state to calculate progress (optional, could pass directly)
                    var currentState = GetJobState(name);
                    // No need to return result here if currentState is null, let it proceed and potentially error

                    // Copy and optionally encrypt the file
                    // The CopyFileAsync and EncryptFileWithCryptoSoftAsync should also ideally respect the token
                    await CopyFileAsync(sourceFile, targetFile, name, token);
                    
                    result.BytesTransferred = sourceInfo.Length;
                    result.EncryptionTime = stopwatch.ElapsedMilliseconds;
                    result.WasProcessed = true;

                    // Calculate progress based on current state (re-fetch state in case it changed while copying)
                     currentState = GetJobState(name);
                     if (currentState != null)
                     {
                        // Note: Progress calculation based on file count might be less accurate with parallel execution
                        // A byte-based progress calculation would be more precise but more complex.
                        // Using file count for now as it's simpler.
                        var progress = (int)(((currentState.TotalFilesCount - currentState.FilesRemaining) * 100.0) / currentState.TotalFilesCount);

                        // Report progress with calculated values
                        // The actual bytes transferred and files processed are tracked in ExecuteJob and updated in state
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        sourceFile,
                        targetFile,
                        sourceInfo.Length,
                            progress,
                            currentState.TotalFilesSize - currentState.BytesRemaining, // Estimated bytes transferred from state
                            currentState.TotalFilesSize,
                            currentState.TotalFilesCount - currentState.FilesRemaining, // Estimated files processed from state
                            currentState.TotalFilesCount,
                        stopwatch.Elapsed,
                        true
                    ));
                     }
                }
                catch (OperationCanceledException)
                {
                    // Propagate cancellation
                    throw;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    result.HasError = true;
                    result.ErrorMessage = $"Error copying file {sourceFile}: {ex.Message}";

                    // Get current state for error reporting
                    var currentState = GetJobState(name);
                    if (currentState != null)
                    {
                         var progress = (int)(((currentState.TotalFilesCount - currentState.FilesRemaining) * 100.0) / currentState.TotalFilesCount);

                        // Report error progress with calculated values
                    FileProgressChanged?.Invoke(this, new FileProgressEventArgs(
                        name,
                        sourceFile,
                        targetFile,
                        sourceInfo.Length,
                            progress,
                            currentState.TotalFilesSize - currentState.BytesRemaining,
                            currentState.TotalFilesSize,
                            currentState.TotalFilesCount - currentState.FilesRemaining,
                            currentState.TotalFilesCount,
                        stopwatch.Elapsed,
                        false
                    ));

                    if (backup.Encrypt)
                    {
                        EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(
                            name,
                            sourceFile,
                                progress, // Use file progress for encryption progress
                            true,
                            true,
                            ex.Message
                        ));
                        }
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
            BackupResources? resources = null; // Declare resources variable outside try block
            // jobLock is not needed here as state updates are thread-safe via UpdateJobState

            ConcurrentDictionary<string, string> filesForCopy = new ConcurrentDictionary<string, string>(); // Declare outside try block

            try
            {
                CheckForBusinessSoftware(name);  // Check for business software before starting backup
                backup = GetJob(name);
                if (backup == null)
                    throw new InvalidOperationException($"Backup job '{name}' not found");

                // Create and register backup resources
                resources = new BackupResources();
                if (!_activeBackups.TryAdd(name, resources))
                {
                    resources.Dispose();
                    throw new InvalidOperationException($"Backup job '{name}' is already running");
                }

                // Combine the external cancellation token with the internal one
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, resources.CancellationTokenSource.Token);
                var token = linkedCts.Token;

                // Update state to Active immediately
                UpdateJobState(name, state =>
                {
                    state.Status = JobStatus.Active;
                    state.ProgressPercentage = 0;
                    state.CurrentSourceFile = backup.SourcePath;
                    state.CurrentTargetFile = backup.TargetPath;
                });

                resources.Stopwatch.Start();
                var startTime = DateTime.Now;
                var hasErrors = false;
                var errorMessages = new List<string>();

                // Get all files in the source directory, sorted by priority
                var allFiles = await GetFilesToProcessAsync(backup);
                var priorityFiles = allFiles.Where(f => Config.GetPriorityExtensions().Contains(Path.GetExtension(f).ToLower())).ToList();
                var normalFiles = allFiles.Where(f => !Config.GetPriorityExtensions().Contains(Path.GetExtension(f).ToLower())).ToList();
                
                var filesToProcess = new List<string>();
                filesToProcess.AddRange(priorityFiles);
                filesToProcess.AddRange(normalFiles);

                var totalFiles = filesToProcess.Count;
                var totalBytes = filesToProcess.Sum(f => new FileInfo(f).Length);

                var totalEncryptionTime = 0L;

                // Update state with initial job size information
                UpdateJobState(name, state =>
                {
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = ""; // Reset for new phase structure
                    state.CurrentTargetFile = ""; // Reset for new phase structure
                    state.ProgressPercentage = 0; // Reset progress for the start of Phase 1
                });

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

                // PHASE 1: ENCRYPTION
                var encryptedFilesCount = 0;
                var totalFilesToEncrypt = filesToProcess.Count(f => backup.Encrypt && _settingsController.ShouldEncryptFile(f));

                _logger.AddLogEntry(new LogEntry
                {
                     Timestamp = DateTime.Now,
                     BackupName = name,
                     Message = $"Starting Encryption Phase. {totalFilesToEncrypt} files to encrypt.",
                     LogType = "INFO",
                     ActionType = "PHASE_START_ENCRYPTION"
                });

                // Use a dedicated semaphore for encryption phase if needed, or reuse existing thread pools
                // For simplicity, reusing the same thread pools for both phases
                var priorityEncryptionOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _maxPriorityThreads,
                    CancellationToken = token // Use the combined token
                };

                var normalEncryptionOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxNormalThreads,
                    CancellationToken = token // Use the combined token
                };
                
                var priorityEncryptionFiles = filesToProcess.Where(f => priorityFiles.Contains(f)).ToList();
                var normalEncryptionFiles = filesToProcess.Where(f => normalFiles.Contains(f)).ToList();

                // Encrypt priority files first
                 await Parallel.ForEachAsync(priorityEncryptionFiles, priorityEncryptionOptions, async (sourceFile, fileToken) =>
                {
                    // Check for cancellation or pause before processing each file
                    while (GetJobState(name)?.Status == JobStatus.Paused)
                    {
                        await Task.Delay(50, fileToken); // Wait while paused, checking more frequently
                        fileToken.ThrowIfCancellationRequested(); // Check for stop during pause
                    }

                    // Check for cancellation after waiting for pause (if any) and before processing
                    fileToken.ThrowIfCancellationRequested();

                    var shouldEncrypt = backup.Encrypt && _settingsController.ShouldEncryptFile(sourceFile);
                    var targetFileForCopy = sourceFile; // Default to original file for copy

                    if (shouldEncrypt)
                        {
                            try
                            {
                             await AcquireThreadSlotAsync(true); // Acquire priority thread slot

                             // Notify UI: encryption started
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 0));

                             // Log encryption start
                             _logger.AddLogEntry(new LogEntry
                             {
                                 Timestamp = DateTime.Now,
                                 BackupName = name,
                                 SourcePath = sourceFile,
                                 Message = $"Starting encryption of file: {sourceFile}",
                                 LogType = "INFO",
                                 ActionType = "ENCRYPTION_START"
                             });

                             // Determine temporary file path
                             var tempEncryptedDir = Path.Combine(Path.GetTempPath(), "EasySave_Encrypted");
                             if (!Directory.Exists(tempEncryptedDir))
                             {
                                 Directory.CreateDirectory(tempEncryptedDir);
                             }
                             var tempEncryptedFile = Path.Combine(tempEncryptedDir, Path.GetFileName(sourceFile) + ".encrypted");
                             targetFileForCopy = tempEncryptedFile; // Copy from this temp file later

                             var stopwatch = Stopwatch.StartNew();
                             // Perform encryption (this call is not pausable internally with current CryptoSoft implementation)
                             await EncryptFileWithCryptoSoftAsync(sourceFile, tempEncryptedFile);
                             stopwatch.Stop();
                             
                             // Track encryption time
                             lock(_stateLock)
                             {
                                totalEncryptionTime += stopwatch.ElapsedMilliseconds;
                             }

                             // Log encryption success
                             _logger.AddLogEntry(new LogEntry
                             {
                                 Timestamp = DateTime.Now,
                                 BackupName = name,
                                 SourcePath = sourceFile,
                                 TargetPath = tempEncryptedFile,
                                 Message = $"Completed encryption of file: {sourceFile} to {tempEncryptedFile} in {stopwatch.ElapsedMilliseconds}ms",
                                 LogType = "INFO",
                                 ActionType = "ENCRYPTION_COMPLETE"
                             });

                             // Notify UI: encryption complete
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 100, true));
                         }
                         catch (OperationCanceledException)
                         {
                             // Propagate cancellation
                             throw;
                         }
                         catch (Exception ex)
                         {
                             hasErrors = true;
                             errorMessages.Add($"Encryption error for file {sourceFile}: {ex.Message}");

                             // Log encryption failure
                             _logger.AddLogEntry(new LogEntry
                             {
                                 Timestamp = DateTime.Now,
                                 BackupName = name,
                                 SourcePath = sourceFile,
                                 Message = $"Encryption error for file {sourceFile}: {ex.Message}",
                                 LogType = "ERROR",
                                 ActionType = "ENCRYPTION_ERROR"
                             });

                             // Notify UI: encryption error
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 0, true, true, ex.Message));
                         }
                         finally
                         {
                            ReleaseThreadSlot(true); // Release priority thread slot
                            lock(_stateLock)
                            {
                                encryptedFilesCount++;
                                // Update state with encryption progress (based on files encrypted)
                                var encryptionProgress = (int)((encryptedFilesCount * 100.0) / totalFilesToEncrypt);
                                        UpdateJobState(name, state =>
                                        {
                                    state.ProgressPercentage = encryptionProgress / 2;
                                    state.CurrentSourceFile = $"Encrypting: {Path.GetFileName(sourceFile)}";
                                });
                            }
                         }
                    }
                    else
                    {
                         // Log that encryption is skipped
                         _logger.AddLogEntry(new LogEntry
                         {
                             Timestamp = DateTime.Now,
                             BackupName = name,
                             SourcePath = sourceFile,
                             Message = $"Skipping encryption for file: {sourceFile}",
                             LogType = "INFO",
                             ActionType = "ENCRYPTION_SKIP"
                         });
                         lock(_stateLock)
                         {
                             // Still count towards encryption phase progress for files not needing encryption
                             encryptedFilesCount++;
                             var encryptionProgress = (int)((encryptedFilesCount * 100.0) / totalFilesToEncrypt);
                             UpdateJobState(name, state =>
                             {
                                 state.ProgressPercentage = encryptionProgress / 2;
                                 state.CurrentSourceFile = $"Processing (No Encryption): {Path.GetFileName(sourceFile)}";
                                        });
                                    }
                                }
                                
                    // Store the path to use for the copy phase
                    filesForCopy[sourceFile] = targetFileForCopy;
                });

                 // Encrypt normal files
                 await Parallel.ForEachAsync(normalEncryptionFiles, normalEncryptionOptions, async (sourceFile, fileToken) =>
                 {
                     // Check for cancellation or pause before processing each file
                     while (GetJobState(name)?.Status == JobStatus.Paused)
                     {
                         await Task.Delay(50, fileToken); // Wait while paused, checking more frequently
                         fileToken.ThrowIfCancellationRequested(); // Check for stop during pause
                     }

                     // Check for cancellation after waiting for pause (if any) and before processing
                     fileToken.ThrowIfCancellationRequested();

                     // *** Added: Wait if there are priority files remaining ***
                     while (GetJobState(name)?.FilesRemaining > (totalFiles - priorityFiles.Count))
                     {
                          await Task.Delay(100, fileToken); // Wait, checking periodically
                          fileToken.ThrowIfCancellationRequested(); // Check for stop while waiting
                     }
                     // *** End Added ***

                     var shouldEncrypt = backup.Encrypt && _settingsController.ShouldEncryptFile(sourceFile);
                     var targetFileForCopy = sourceFile; // Default to original file for copy

                     if (shouldEncrypt)
                     {
                         try
                         {
                             await AcquireThreadSlotAsync(false); // Acquire normal thread slot

                              // Notify UI: encryption started
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 0));

                              // Log encryption start
                              _logger.AddLogEntry(new LogEntry
                              {
                                  Timestamp = DateTime.Now,
                                  BackupName = name,
                                  SourcePath = sourceFile,
                                  Message = $"Starting encryption of file: {sourceFile}",
                                  LogType = "INFO",
                                  ActionType = "ENCRYPTION_START"
                              });

                             // Determine temporary file path
                             var tempEncryptedDir = Path.Combine(Path.GetTempPath(), "EasySave_Encrypted");
                             if (!Directory.Exists(tempEncryptedDir))
                             {
                                 Directory.CreateDirectory(tempEncryptedDir);
                             }
                             var tempEncryptedFile = Path.Combine(tempEncryptedDir, Path.GetFileName(sourceFile) + ".encrypted");
                             targetFileForCopy = tempEncryptedFile; // Copy from this temp file later

                              var stopwatch = Stopwatch.StartNew();
                              // Perform encryption (this call is not pausable internally with current CryptoSoft implementation)
                              await EncryptFileWithCryptoSoftAsync(sourceFile, tempEncryptedFile);
                              stopwatch.Stop();

                             // Track encryption time
                             lock(_stateLock)
                             {
                                totalEncryptionTime += stopwatch.ElapsedMilliseconds;
                             }

                             // Log encryption success
                              _logger.AddLogEntry(new LogEntry
                              {
                                  Timestamp = DateTime.Now,
                                  BackupName = name,
                                  SourcePath = sourceFile,
                                  TargetPath = tempEncryptedFile,
                                  Message = $"Completed encryption of file: {sourceFile} to {tempEncryptedFile} in {stopwatch.ElapsedMilliseconds}ms",
                                  LogType = "INFO",
                                  ActionType = "ENCRYPTION_COMPLETE"
                              });

                              // Notify UI: encryption complete
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 100, true));
                         }
                         catch (OperationCanceledException)
                         {
                             // Propagate cancellation
                             throw;
                         }
                         catch (Exception ex)
                         {
                             hasErrors = true;
                             errorMessages.Add($"Encryption error for file {sourceFile}: {ex.Message}");

                             // Log encryption failure
                              _logger.AddLogEntry(new LogEntry
                              {
                                  Timestamp = DateTime.Now,
                                  BackupName = name,
                                  SourcePath = sourceFile,
                                  Message = $"Encryption error for file {sourceFile}: {ex.Message}",
                                  LogType = "ERROR",
                                  ActionType = "ENCRYPTION_ERROR"
                              });

                             // Notify UI: encryption error
                             EncryptionProgressChanged?.Invoke(this, new EncryptionProgressEventArgs(name, sourceFile, 0, true, true, ex.Message));
                            }
                            finally
                            {
                             ReleaseThreadSlot(false); // Release normal thread slot
                             lock(_stateLock)
                             {
                                 encryptedFilesCount++;
                                 // Update state with encryption progress (based on files encrypted)
                                 var encryptionProgress = (int)((encryptedFilesCount * 100.0) / totalFilesToEncrypt);
                                 UpdateJobState(name, state =>
                                 {
                                     state.ProgressPercentage = encryptionProgress / 2;
                                     state.CurrentSourceFile = $"Encrypting: {Path.GetFileName(sourceFile)}";
                                 });
                             }
                         }
                     }
                     else
                     {
                         // Log that encryption is skipped
                          _logger.AddLogEntry(new LogEntry
                          {
                              Timestamp = DateTime.Now,
                              BackupName = name,
                              SourcePath = sourceFile,
                              Message = $"Skipping encryption for file: {sourceFile}",
                              LogType = "INFO",
                              ActionType = "ENCRYPTION_SKIP"
                          });
                          lock(_stateLock)
                          {
                              // Still count towards encryption phase progress for files not needing encryption
                              encryptedFilesCount++;
                              var encryptionProgress = (int)((encryptedFilesCount * 100.0) / totalFilesToEncrypt);
                              UpdateJobState(name, state =>
                              {
                                  state.ProgressPercentage = encryptionProgress / 2;
                                  state.CurrentSourceFile = $"Processing (No Encryption): {Path.GetFileName(sourceFile)}";
                              });
                          }
                     }
                    // Store the path to use for the copy phase
                    filesForCopy[sourceFile] = targetFileForCopy;
                 });


                // PHASE 2: COPYING
                var filesCopiedCount = 0;
                var bytesTransferred = 0L;

                _logger.AddLogEntry(new LogEntry
                {
                     Timestamp = DateTime.Now,
                     BackupName = name,
                     Message = $"Starting Copy Phase. Copying {totalFiles} files.",
                     LogType = "INFO",
                     ActionType = "PHASE_START_COPY"
                });

                // Use a dedicated semaphore for copy phase if needed, or reuse existing thread pools
                 var priorityCopyOptions = new ParallelOptions
                 {
                     MaxDegreeOfParallelism = _maxPriorityThreads,
                     CancellationToken = token // Use the combined token
                 };

                 var normalCopyOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _maxNormalThreads,
                     CancellationToken = token // Use the combined token
                 };

                 // Re-sort files for copy phase to maintain priority order
                var priorityCopyFiles = filesToProcess.Where(f => priorityFiles.Contains(f)).ToList();
                var normalCopyFiles = filesToProcess.Where(f => normalFiles.Contains(f)).ToList();

                // Copy priority files first
                await Parallel.ForEachAsync(priorityCopyFiles, priorityCopyOptions, async (originalSourceFile, fileToken) =>
                {
                    // Check for cancellation or pause before processing each file
                    while (GetJobState(name)?.Status == JobStatus.Paused)
                    {
                        await Task.Delay(50, fileToken); // Wait while paused, checking more frequently
                        fileToken.ThrowIfCancellationRequested(); // Check for stop during pause
                    }

                    // Check for cancellation after waiting for pause (if any) and before processing
                    fileToken.ThrowIfCancellationRequested();

                    var sourceFileForCopy = filesForCopy[originalSourceFile]; // Get the path to copy from (original or temp encrypted)
                    var relativePath = Path.GetRelativePath(backup.SourcePath, originalSourceFile);
                    var targetFile = Path.Combine(backup.TargetPath, relativePath);
                    var originalSourceInfo = new FileInfo(originalSourceFile);

                    bool isLargeFile = originalSourceInfo.Length > _maxLargeFileSizeBytes;

                    // Acquire semaphore for large files if applicable
                    if (isLargeFile)
                    {
                        await _largeFileSemaphore.WaitAsync(fileToken);
                    }

                    try
                    {
                        await AcquireThreadSlotAsync(true); // Acquire priority thread slot

                        // Determine if this file needs to be copied (differential backup)
                        var sourceInfo = new FileInfo(sourceFileForCopy); // Use source file for copy (temp or original) for info
                        var shouldCopy = true;
                         if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase) && File.Exists(targetFile))
                         {
                             var targetInfo = new FileInfo(targetFile);
                             // For differential, compare last write time of original source file
                             shouldCopy = originalSourceInfo.LastWriteTime > targetInfo.LastWriteTime;
                         }

                        if(shouldCopy)
                        {
                            // Ensure target directory exists
                            var dir = Path.GetDirectoryName(targetFile);
                            if (dir != null && !Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                                _logger.LogAdminAction(name, "DIR_CREATE", $"Created directory: {dir}");
                            }

                            var stopwatch = Stopwatch.StartNew();
                            // Copy the file using the streaming method (supports pause/cancellation within file)
                            await CopyFileStreamAsync(sourceFileForCopy, targetFile, name, fileToken); // Pass file-specific token for granular cancel
                            stopwatch.Stop();

                            lock(_stateLock)
                            {
                                filesCopiedCount++;
                                bytesTransferred += sourceInfo.Length; // Use size of the file being copied (temp or original)

                                // Update state with copy progress
                                var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                                UpdateJobState(name, state =>
                                {
                                    // Combine progress from both phases (50% encryption, 50% copy)
                                    state.ProgressPercentage = 50 + (copyProgress / 2);
                                    state.FilesRemaining = totalFiles - filesCopiedCount;
                                    state.BytesRemaining = totalBytes - bytesTransferred;
                                    state.CurrentSourceFile = $"Copying: {Path.GetFileName(originalSourceFile)}"; // Use original source name for UI
                                    state.CurrentTargetFile = targetFile;
                                });
                                // Note: FileProgressChanged is now emitted by CopyFileStreamAsync for granular progress
                            }

                            _logger.AddLogEntry(new LogEntry
                            {
                                Timestamp = DateTime.Now,
                                BackupName = name,
                                SourcePath = originalSourceFile,
                                TargetPath = targetFile,
                                Message = $"Successfully copied file: {Path.GetFileName(originalSourceFile)} in {stopwatch.ElapsedMilliseconds}ms",
                                LogType = "INFO",
                                ActionType = "FILE_COPY_COMPLETE"
                            });
                        }
                        else
                        {
                            // File skipped due to differential backup logic
                            lock(_stateLock)
                            {
                                filesCopiedCount++;
                                // Update state even for skipped files to track overall completion
                                var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                                UpdateJobState(name, state =>
                                {
                                     state.ProgressPercentage = 50 + (copyProgress / 2);
                                     state.FilesRemaining = totalFiles - filesCopiedCount;
                                     // Bytes remaining doesn't change for skipped files
                                     state.CurrentSourceFile = $"Skipped (Differential): {Path.GetFileName(originalSourceFile)}";
                                     state.CurrentTargetFile = targetFile;
                                });
                            }
                             _logger.AddLogEntry(new LogEntry
                             {
                                 Timestamp = DateTime.Now,
                                 BackupName = name,
                                 SourcePath = originalSourceFile,
                                 TargetPath = targetFile,
                                 Message = $"File skipped (Differential): {Path.GetFileName(originalSourceFile)}",
                                 LogType = "INFO",
                                 ActionType = "FILE_SKIPPED_DIFFERENTIAL"
                             });
                        }
                    }
                     catch (OperationCanceledException)
                     {
                         // Propagate cancellation
                         throw;
                     }
                    catch (Exception ex)
                    {
                         lock (_stateLock)
                                        {
                                            hasErrors = true;
                              errorMessages.Add($"Copy error for file {originalSourceFile}: {ex.Message}");

                             // Update state on error
                             filesCopiedCount++; // Count the file as processed even on error
                             var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                             UpdateJobState(name, state =>
                             {
                                 state.ProgressPercentage = 50 + (copyProgress / 2);
                                 state.FilesRemaining = totalFiles - filesCopiedCount;
                                 // Bytes remaining doesn't change on error
                                 state.CurrentSourceFile = $"Error Copying: {Path.GetFileName(originalSourceFile)}";
                                 state.CurrentTargetFile = targetFile;
                             });
                         }

                        // Log copy failure
                        _logger.AddLogEntry(new LogEntry
                        {
                            Timestamp = DateTime.Now,
                            BackupName = name,
                            SourcePath = originalSourceFile,
                            TargetPath = targetFile,
                            Message = $"Copy error for file {originalSourceFile}: {ex.Message}",
                            LogType = "ERROR",
                            ActionType = "FILE_COPY_ERROR"
                        });
                    }
                     finally
                     {
                        ReleaseThreadSlot(true); // Release priority thread slot

                         // Release large file semaphore if acquired
                         if (isLargeFile)
                         {
                             _largeFileSemaphore.Release();
                         }

                         // Clean up temporary encrypted file if it exists and copy is complete or failed
                          if (filesForCopy.TryGetValue(originalSourceFile, out var sourcePathUsed) && sourcePathUsed != originalSourceFile && File.Exists(sourcePathUsed))
                          {
                              try
                              {
                                  File.Delete(sourcePathUsed);
                                   _logger.AddLogEntry(new LogEntry
                                  {
                                      Timestamp = DateTime.Now,
                                      BackupName = name,
                                      SourcePath = sourcePathUsed,
                                      Message = $"Cleaned up temporary encrypted file: {Path.GetFileName(sourcePathUsed)}",
                                      LogType = "INFO",
                                      ActionType = "TEMP_FILE_CLEANUP"
                                  });
                              }
                              catch (Exception ex)
                              {
                                   _logger.AddLogEntry(new LogEntry
                                  {
                                      Timestamp = DateTime.Now,
                                      BackupName = name,
                                      SourcePath = sourcePathUsed,
                                      Message = $"Failed to clean up temporary encrypted file {Path.GetFileName(sourcePathUsed)}: {ex.Message}",
                                      LogType = "WARNING",
                                      ActionType = "TEMP_FILE_CLEANUP_FAILED"
                                  });
                              }
                          }
                     }
                });

                // Copy normal files
                 await Parallel.ForEachAsync(normalCopyFiles, normalCopyOptions, async (originalSourceFile, fileToken) =>
                 {
                     // Check for cancellation or pause before processing each file
                     while (GetJobState(name)?.Status == JobStatus.Paused)
                     {
                         await Task.Delay(50, fileToken); // Wait while paused, checking more frequently
                         fileToken.ThrowIfCancellationRequested(); // Check for stop during pause
                     }

                     // Check for cancellation after waiting for pause (if any) and before processing
                     fileToken.ThrowIfCancellationRequested();

                     // *** Added: Wait if there are priority files remaining ***
                     while (GetJobState(name)?.FilesRemaining > (totalFiles - priorityFiles.Count))
                     {
                          await Task.Delay(100, fileToken); // Wait, checking periodically
                          fileToken.ThrowIfCancellationRequested(); // Check for stop while waiting
                     }
                     // *** End Added ***

                     var sourceFileForCopy = filesForCopy[originalSourceFile]; // Get the path to copy from (original or temporary encrypted)
                     var relativePath = Path.GetRelativePath(backup.SourcePath, originalSourceFile);
                     var targetFile = Path.Combine(backup.TargetPath, relativePath);
                     var originalSourceInfo = new FileInfo(originalSourceFile);

                     bool isLargeFile = originalSourceInfo.Length > _maxLargeFileSizeBytes;

                     // Acquire semaphore for large files if applicable
                     if (isLargeFile)
                     {
                         await _largeFileSemaphore.WaitAsync(fileToken);
                     }

                     try
                     {
                         await AcquireThreadSlotAsync(false); // Acquire normal thread slot

                         // Determine if this file needs to be copied (differential backup)
                         var sourceInfo = new FileInfo(sourceFileForCopy); // Use source file for copy (temp or original) for info
                         var shouldCopy = true;
                         if (backup.Type.Equals("Differential", StringComparison.OrdinalIgnoreCase) && File.Exists(targetFile))
                         {
                             var targetInfo = new FileInfo(targetFile);
                              // For differential, compare last write time of original source file
                             shouldCopy = originalSourceInfo.LastWriteTime > targetInfo.LastWriteTime;
                         }

                         if(shouldCopy)
                         {
                             // Ensure target directory exists
                             var dir = Path.GetDirectoryName(targetFile);
                             if (dir != null && !Directory.Exists(dir))
                             {
                                 Directory.CreateDirectory(dir);
                                 _logger.LogAdminAction(name, "DIR_CREATE", $"Created directory: {dir}");
                             }

                             var stopwatch = Stopwatch.StartNew();
                             // Copy the file using the streaming method (supports pause/cancellation within file)
                             await CopyFileStreamAsync(sourceFileForCopy, targetFile, name, fileToken); // Pass file-specific token for granular cancel
                             stopwatch.Stop();

                             lock (_stateLock)
                             {
                                 filesCopiedCount++;
                                 bytesTransferred += sourceInfo.Length; // Use size of the file being copied (temp or original)

                                 // Update state with copy progress
                                 var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                                        UpdateJobState(name, state =>
                                        {
                                     state.ProgressPercentage = 50 + (copyProgress / 2);
                                     state.FilesRemaining = totalFiles - filesCopiedCount;
                                            state.BytesRemaining = totalBytes - bytesTransferred;
                                     state.CurrentSourceFile = $"Copying: {Path.GetFileName(originalSourceFile)}"; // Use original source name for UI
                                     state.CurrentTargetFile = targetFile;
                                 });
                                 // Note: FileProgressChanged is now emitted by CopyFileStreamAsync for granular progress
                             }

                             _logger.AddLogEntry(new LogEntry
                             {
                                 Timestamp = DateTime.Now,
                                 BackupName = name,
                                 SourcePath = originalSourceFile,
                                 TargetPath = targetFile,
                                 Message = $"Successfully copied file: {Path.GetFileName(originalSourceFile)} in {stopwatch.ElapsedMilliseconds}ms",
                                 LogType = "INFO",
                                 ActionType = "FILE_COPY_COMPLETE"
                             });
                         }
                         else
                         {
                              // File skipped due to differential backup logic
                              lock(_stateLock)
                              {
                                  filesCopiedCount++;
                                   // Update state even for skipped files to track overall completion
                                   var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                                   UpdateJobState(name, state =>
                                   {
                                        state.ProgressPercentage = 50 + (copyProgress / 2);
                                        state.FilesRemaining = totalFiles - filesCopiedCount;
                                        // Bytes remaining doesn't change for skipped files
                                        state.CurrentSourceFile = $"Skipped (Differential): {Path.GetFileName(originalSourceFile)}";
                                        state.CurrentTargetFile = targetFile;
                                   });
                              }
                               _logger.AddLogEntry(new LogEntry
                               {
                                   Timestamp = DateTime.Now,
                                   BackupName = name,
                                   SourcePath = originalSourceFile,
                                   TargetPath = targetFile,
                                   Message = $"File skipped (Differential): {Path.GetFileName(originalSourceFile)}",
                                   LogType = "INFO",
                                   ActionType = "FILE_SKIPPED_DIFFERENTIAL"
                               });
                         }
                     }
                     catch (OperationCanceledException)
                     {
                         // Propagate cancellation
                         throw;
                     }
                     catch (Exception ex)
                     {
                         lock (_stateLock)
                         {
                             hasErrors = true;
                             errorMessages.Add($"Copy error for file {originalSourceFile}: {ex.Message}");

                             // Update state on error
                             filesCopiedCount++; // Count the file as processed even on error
                             var copyProgress = (int)((filesCopiedCount * 100.0) / totalFiles);
                             UpdateJobState(name, state =>
                             {
                                 state.ProgressPercentage = 50 + (copyProgress / 2);
                                 state.FilesRemaining = totalFiles - filesCopiedCount;
                                 // Bytes remaining doesn't change on error
                                 state.CurrentSourceFile = $"Error Copying: {Path.GetFileName(originalSourceFile)}";
                                 state.CurrentTargetFile = targetFile;
                             });
                         }

                         // Log copy failure
                         _logger.AddLogEntry(new LogEntry
                         {
                             Timestamp = DateTime.Now,
                             BackupName = name,
                             SourcePath = originalSourceFile,
                             TargetPath = targetFile,
                             Message = $"Copy error for file {originalSourceFile}: {ex.Message}",
                             LogType = "ERROR",
                             ActionType = "FILE_COPY_ERROR"
                         });
                     }
                            finally
                            {
                         ReleaseThreadSlot(false); // Release normal thread slot

                         // Release large file semaphore if acquired
                         if (isLargeFile)
                         {
                             _largeFileSemaphore.Release();
                         }

                         // Clean up temporary encrypted file if it exists and copy is complete or failed
                          if (filesForCopy.TryGetValue(originalSourceFile, out var sourcePathUsed) && sourcePathUsed != originalSourceFile && File.Exists(sourcePathUsed))
                          {
                              try
                              {
                                  File.Delete(sourcePathUsed);
                                   _logger.AddLogEntry(new LogEntry
                                  {
                                      Timestamp = DateTime.Now,
                                      BackupName = name,
                                      SourcePath = sourcePathUsed,
                                      Message = $"Cleaned up temporary encrypted file: {Path.GetFileName(sourcePathUsed)}",
                                      LogType = "INFO",
                                      ActionType = "TEMP_FILE_CLEANUP"
                                  });
                              }
                              catch (Exception ex)
                              {
                                   _logger.AddLogEntry(new LogEntry
                                  {
                                      Timestamp = DateTime.Now,
                                      BackupName = name,
                                      SourcePath = sourcePathUsed,
                                      Message = $"Failed to clean up temporary encrypted file {Path.GetFileName(sourcePathUsed)}: {ex.Message}",
                                      LogType = "WARNING",
                                      ActionType = "TEMP_FILE_CLEANUP_FAILED"
                                  });
                              }
                          }
                     }
                 });


                // Update final state based on cancellation or completion
                token.ThrowIfCancellationRequested(); // Throw if cancellation was requested during file processing

                // If we reach here without cancellation, it's either completed or had errors
                UpdateJobState(name, state =>
                {
                    state.Status = hasErrors ? JobStatus.Error : JobStatus.Completed;
                    state.ProgressPercentage = hasErrors ? state.ProgressPercentage : 100; // Keep last progress on error
                    state.FilesRemaining = totalFiles - filesCopiedCount; // Remaining files reflect copy phase completion
                    state.BytesRemaining = totalBytes - bytesTransferred; // Remaining bytes reflect copy phase completion
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Create final log entry
                var finalLogEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = name,
                    BackupType = backup?.Type ?? "Unknown",  // Use null-conditional operator
                    SourcePath = backup?.SourcePath ?? string.Empty,  // Use null-conditional operator
                    TargetPath = backup?.TargetPath ?? string.Empty,  // Use null-conditional operator
                    FileSize = bytesTransferred,
                    TransferTime = resources.Stopwatch.ElapsedMilliseconds,
                    EncryptionTime = backup?.Encrypt == true ? totalEncryptionTime : -1,
                    Message = hasErrors 
                        ? $"Backup completed with errors: {string.Join("; ", errorMessages)}\n"
                        : $"Backup completed successfully. Processed {filesCopiedCount} files ({FormatFileSize(bytesTransferred)}) in {resources.Stopwatch.ElapsedMilliseconds:F0}ms" +
                          (backup?.Encrypt == true ? $" (Total Encryption time: {totalEncryptionTime}ms)" : ""),
                    LogType = hasErrors ? "ERROR" : "INFO",
                    ActionType = hasErrors ? "BACKUP_ERROR" : "BACKUP_COMPLETED"
                 };
                _logger.AddLogEntry(finalLogEntry);
            }
            catch (OperationCanceledException)
            {
                // This block is reached when token.ThrowIfCancellationRequested() is called
                // The state should already be updated to Cancelled/Stopped in the pause loop check or StopBackup method
                // Ensure the state is updated to Cancelled if not already
                UpdateJobState(name, state =>
                {
                     if(state.Status != JobStatus.Stopped)
                     {
                        state.Status = JobStatus.Cancelled;
                        state.ProgressPercentage = 0; // Reset progress on cancellation
                        state.FilesRemaining = 0;
                        state.BytesRemaining = 0;
                    state.CurrentSourceFile = string.Empty;
                    state.CurrentTargetFile = string.Empty;
                     }
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
                // Do not re-throw if cancellation was intended (pause/stop)
            }
            catch (Exception ex)
            {
                UpdateJobState(name, state =>
                {
                    state.Status = JobStatus.Error;
                    state.ProgressPercentage = 0;
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
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
                throw; // Re-throw to be caught by the UI layer
            }
            finally
            {
                // Ensure resources are disposed and removed from active backups
                if (resources != null && _activeBackups.TryRemove(name, out var backupResources))
                {
                    backupResources.Dispose();
                }
                // Re-enable buttons in UI if they were disabled during execution (this is handled in MainForm, but good to keep in mind)
                // Clean up any remaining temporary encrypted files for this job
                var tempEncryptedDir = Path.Combine(Path.GetTempPath(), "EasySave_Encrypted");
                if (Directory.Exists(tempEncryptedDir))
                {
                     var jobTempFiles = Directory.GetFiles(tempEncryptedDir, Path.GetFileName("*.*.encrypted"))
                                               .Where(f => filesForCopy.Values.Contains(f)).ToList();
                     foreach(var tempFile in jobTempFiles)
                     {
                          try
                          {
                               File.Delete(tempFile);
                                _logger.AddLogEntry(new LogEntry
                               {
                                   Timestamp = DateTime.Now,
                                   BackupName = name,
                                   SourcePath = tempFile,
                                   Message = $"Cleaned up temporary encrypted file in finally block: {Path.GetFileName(tempFile)}",
                                   LogType = "INFO",
                                   ActionType = "TEMP_FILE_CLEANUP_FINALLY"
                               });
                          }
                          catch (Exception ex)
                          {
                               _logger.AddLogEntry(new LogEntry
                               {
                                   Timestamp = DateTime.Now,
                                   BackupName = name,
                                   SourcePath = tempFile,
                                   Message = $"Failed to clean up temporary encrypted file in finally block {Path.GetFileName(tempFile)}: {ex.Message}",
                                   LogType = "WARNING",
                                   ActionType = "TEMP_FILE_CLEANUP_FAILED_FINALLY"
                               });
                          }
                     }
                     // Optionally clean up the temp directory if empty
                    if (!Directory.EnumerateFileSystemEntries(tempEncryptedDir).Any())
                    {
                        try
                        {
                            Directory.Delete(tempEncryptedDir);
                             _logger.AddLogEntry(new LogEntry
                            {
                                Timestamp = DateTime.Now,
                                BackupName = name,
                                SourcePath = tempEncryptedDir,
                                Message = $"Cleaned up empty temporary encrypted directory: {tempEncryptedDir}",
                                LogType = "INFO",
                                ActionType = "TEMP_DIR_CLEANUP_FINALLY"
                            });
                        }
                        catch (Exception ex)
                        {
                             _logger.AddLogEntry(new LogEntry
                            {
                                Timestamp = DateTime.Now,
                                BackupName = name,
                                SourcePath = tempEncryptedDir,
                                Message = $"Failed to clean up empty temporary encrypted directory {tempEncryptedDir}: {ex.Message}",
                                LogType = "WARNING",
                                ActionType = "TEMP_DIR_CLEANUP_FAILED_FINALLY"
                            });
                        }
                    }
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
