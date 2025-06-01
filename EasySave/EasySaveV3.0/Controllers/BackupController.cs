using EasySaveLogging;
using EasySaveV3._0.Managers;
using EasySaveV3._0.Models;

namespace EasySaveV3._0.Controllers
{
    /// <summary>
    /// Controller responsible for managing backup operations.
    /// Handles backup creation, execution, and monitoring.
    /// </summary>
    public class BackupController : IDisposable
    {
        private readonly BackupManager _backupManager;
        private readonly SettingsController _settingsController;
        private readonly LogController _logController;
        private readonly LanguageManager _languageManager;
        private bool _isDisposed;
        public event EventHandler<string>? BusinessSoftwareDetected;
        public event EventHandler<FileProgressEventArgs>? FileProgressChanged;
        public event EventHandler<EncryptionProgressEventArgs>? EncryptionProgressChanged;

        /// <summary>
        /// Initializes a new instance of the BackupController class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when required components fail to initialize.</exception>
        public BackupController()
        {
            try
            {
                _backupManager = new BackupManager();
                _settingsController = SettingsController.Instance;
                _logController = LogController.Instance;
                _languageManager = LanguageManager.Instance;

                if (_backupManager == null || _settingsController == null || _logController == null || _languageManager == null)
                {
                    throw new InvalidOperationException(_languageManager?.GetTranslation("error.componentInitFailed") ?? "Failed to initialize components");
                }

                // Subscribe to BackupManager events
                _backupManager.BusinessSoftwareDetected += (sender, jobName) => BusinessSoftwareDetected?.Invoke(this, jobName);
                _backupManager.FileProgressChanged += (sender, e) => FileProgressChanged?.Invoke(this, e);
                _backupManager.EncryptionProgressChanged += (sender, e) => EncryptionProgressChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager?.GetTranslation("error.controllerInitFailed") ?? "Controller initialization failed", ex);
            }
        }

        /// <summary>
        /// Validates source and destination paths for a backup operation.
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="destinationPath">Destination directory path</param>
        /// <exception cref="ArgumentException">Thrown when paths are invalid</exception>
        private void ValidatePaths(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException(_languageManager.GetTranslation("error.sourcePathEmpty"));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException(_languageManager.GetTranslation("error.destinationPathEmpty"));

            if (!Directory.Exists(sourcePath))
                throw new ArgumentException(_languageManager.GetTranslation("error.sourcePathNotFound"));

            try
            {
                // Ensure destination directory exists or can be created
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Test write access to destination
                var testFile = Path.Combine(destinationPath, "test.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.destinationPathAccess"), ex);
            }

            // Check if paths are the same
            if (Path.GetFullPath(sourcePath).Equals(Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(_languageManager.GetTranslation("error.samePaths"));
        }

        public bool IsCryptoSoftRunning()
        {
            const string mutexName = @"Global\CryptoConsole_MonoInstance";

            if (Mutex.TryOpenExisting(mutexName, out Mutex? existingMutex))
            {
                existingMutex.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// Creates a new backup job.
        /// </summary>
        /// <param name="name">Name of the backup job</param>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="destinationPath">Destination directory path</param>
        /// <param name="type">Type of backup (Full or Differential)</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when backup creation fails</exception>
        public void CreateBackup(string name, string sourcePath, string destinationPath, string type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            if (!type.Equals("Full", StringComparison.OrdinalIgnoreCase) && 
                !type.Equals("Differential", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(_languageManager.GetTranslation("error.invalidBackupType"));

            try
            {
                ValidatePaths(sourcePath, destinationPath);

                var backup = new Backup
                {
                    Name = name,
                    SourcePath = sourcePath,
                    TargetPath = destinationPath,
                    Type = type,
                    Encrypt = true  // Enable encryption by default
                };

                if (!_backupManager.AddJob(backup))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupExists"));
                }
            }
            catch (Exception ex)
            {
                _logController.LogBackupError(name, type, ex.Message, sourcePath, destinationPath);
                throw;
            }
        }

        /// <summary>
        /// Edits an existing backup job.
        /// </summary>
        /// <param name="name">Current name of the backup job</param>
        /// <param name="sourcePath">New source directory path</param>
        /// <param name="destinationPath">New destination directory path</param>
        /// <param name="type">New backup type</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when backup update fails</exception>
        public void EditBackup(string name, string sourcePath, string destinationPath, string type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            if (!type.Equals("Full", StringComparison.OrdinalIgnoreCase) && 
                !type.Equals("Differential", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(_languageManager.GetTranslation("error.invalidBackupType"));

            try
            {
                var existingBackup = _backupManager.GetJob(name);
                if (existingBackup == null)
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }

                ValidatePaths(sourcePath, destinationPath);

                var updatedBackup = new Backup
                {
                    Name = name,
                    SourcePath = sourcePath,
                    TargetPath = destinationPath,
                    Type = type,
                    FileLength = existingBackup.FileLength
                };

                if (!_backupManager.UpdateJob(name, updatedBackup))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }
            }
            catch (Exception ex)
            {
                _logController.LogBackupError(name, type, ex.Message, sourcePath, destinationPath);
                throw;
            }
        }

        /// <summary>
        /// Deletes a backup job.
        /// </summary>
        /// <param name="name">Name of the backup job to delete</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when backup deletion fails</exception>
        public void DeleteBackup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            try
            {
                if (_backupManager.IsBackupRunning(name))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("error.backupInProgress"));
                }

                if (!_backupManager.RemoveJob(name))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupNotFound"));
                }
            }
            catch (Exception ex)
            {
                var backup = GetBackup(name);
                _logController.LogBackupError(name, backup?.Type ?? "Unknown", ex.Message, backup?.SourcePath, backup?.TargetPath);
                throw;
            }
        }

        /// <summary>
        /// Starts a backup job.
        /// </summary>
        /// <param name="backupName">Name of the backup job to start</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when backup start fails</exception>
        public async Task StartBackup(string backupName)
        {
            try
            {
                // 1) Vérification mono-instance CryptoSoft
                       if (IsCryptoSoftRunning())
                           {
                    throw new InvalidOperationException(
                    _languageManager.GetTranslation("message.cryptoSoftAlreadyRunning")
                               );
                           }
                if (_settingsController.IsBusinessSoftwareRunning())
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.businessSoftwareRunning"));
                }

                if (_backupManager.IsBackupRunning(backupName))
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.backupAlreadyRunning"));
                }

                await _backupManager.ExecuteJob(backupName);
            }
            catch (Exception ex)
            {
                var backup = GetBackup(backupName);
                _logController.LogBackupError(backupName, backup?.Type ?? "Unknown", ex.Message, backup?.SourcePath, backup?.TargetPath);
                throw;
            }
        }

        /// <summary>
        /// Gets the list of all backup jobs.
        /// </summary>
        /// <returns>List of backup jobs</returns>
        /// <exception cref="InvalidOperationException">Thrown when backup list retrieval fails</exception>
        public List<Backup> GetBackups()
        {
            try
            {
                return new List<Backup>(_backupManager.Jobs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.backupListRetrievalFailed"), ex);
            }
        }

        /// <summary>
        /// Gets a specific backup job by name.
        /// </summary>
        /// <param name="name">Name of the backup job to retrieve</param>
        /// <returns>The backup job if found, null otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when backup retrieval fails</exception>
        public Backup? GetBackup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            try
            {
                return _backupManager.GetJob(name);
            }
            catch (Exception ex)
            {
                _logController.LogBackupError(name, "Unknown", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the current state of a backup job.
        /// </summary>
        /// <param name="name">Name of the backup job</param>
        /// <returns>The current state of the backup job, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when state retrieval fails</exception>
        public StateModel? GetBackupState(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(_languageManager.GetTranslation("error.backupNameEmpty"));

            try
            {
                return _backupManager.GetJobState(name);
            }
            catch (Exception ex)
            {
                _logController.LogBackupError(name, "Unknown", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Displays the log viewer.
        /// </summary>
        public void DisplayLogs()
        {
            try
            {
                _logController.DisplayLogs();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.logDisplayFailed"), ex);
            }
        }

        /// <summary>
        /// Sets the format for log files.
        /// </summary>
        /// <param name="format">Desired log format</param>
        public void SetLogFormat(LogFormat format)
        {
            try
            {
                _logController.SetLogFormat(format);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.logFormatChangeFailed"), ex);
            }
        }

        /// <summary>
        /// Gets the current log format.
        /// </summary>
        /// <returns>Current log format</returns>
        public LogFormat GetCurrentLogFormat()
        {
            try
            {
                return _logController.GetCurrentLogFormat();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.logFormatRetrievalFailed"), ex);
            }
        }

        /// <summary>
        /// Starts all backup jobs in parallel.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when backup start fails</exception>
        public async Task StartAllBackups()
        {
            try
            {
                if (IsCryptoSoftRunning())
                    throw new InvalidOperationException(
                    _languageManager.GetTranslation("message.cryptoSoftAlreadyRunning")
                               );
                if (_settingsController.IsBusinessSoftwareRunning())
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.businessSoftwareRunning"));
                }

                var backups = GetBackups();
                var backupTasks = backups
                    .Where(backup => !_backupManager.IsBackupRunning(backup.Name))
                    .Select(backup => _backupManager.ExecuteJob(backup.Name));

                await Task.WhenAll(backupTasks);
            }
            catch (Exception ex)
            {
                _logController.LogBackupError("All", "Unknown", ex.Message, null, null);
                throw;
            }
        }

        /// <summary>
        /// Starts selected backup jobs in parallel.
        /// </summary>
        /// <param name="backupNames">List of backup names to execute</param>
        /// <exception cref="InvalidOperationException">Thrown when backup start fails</exception>
        public async Task StartSelectedBackups(List<string> backupNames)
        {
            try
            {
                if (IsCryptoSoftRunning())
                    throw new InvalidOperationException(
                    _languageManager.GetTranslation("message.cryptoSoftAlreadyRunning")
                               );
                if (_settingsController.IsBusinessSoftwareRunning())
                {
                    throw new InvalidOperationException(_languageManager.GetTranslation("message.businessSoftwareRunning"));
                }

                var backupTasks = backupNames
                    .Where(backupName => !_backupManager.IsBackupRunning(backupName))
                    .Select(backupName => _backupManager.ExecuteJob(backupName));

                await Task.WhenAll(backupTasks);
            }
            catch (Exception ex)
            {
                _logController.LogBackupError("Selected", "Unknown", ex.Message, null, null);
                throw;
            }
        }

        /// <summary>
        /// Gets the current thread pool statistics.
        /// </summary>
        /// <returns>A tuple containing active threads, max total threads, max priority threads, and max normal threads</returns>
        public (int ActiveThreads, int MaxTotalThreads, int MaxPriorityThreads, int MaxNormalThreads) GetThreadPoolStats()
        {
            try
            {
                return _backupManager.GetThreadPoolStats();
            }
            catch (Exception ex)
            {
                _logController.LogBackupError("System", "ThreadPool", $"Failed to get thread pool stats: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the number of currently active backups.
        /// </summary>
        public int GetActiveBackupCount()
        {
            return _backupManager.GetActiveBackupCount();
        }

        /// <summary>
        /// Disposes of the BackupController and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the BackupController and its resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _backupManager.BusinessSoftwareDetected -= (sender, jobName) => BusinessSoftwareDetected?.Invoke(this, jobName);
                    _backupManager.FileProgressChanged -= (sender, e) => FileProgressChanged?.Invoke(this, e);
                    _backupManager.EncryptionProgressChanged -= (sender, e) => EncryptionProgressChanged?.Invoke(this, e);
                    if (_backupManager is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer for BackupController.
        /// </summary>
        ~BackupController()
        {
            Dispose(false);
        }
    }
}