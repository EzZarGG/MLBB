using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasySaveV2._0.Patterns.Strategy;

namespace EasySaveV2._0.Patterns.Singleton
{
    public sealed class BackupManager
    {
        private static BackupManager _instance;
        private static readonly object _lock = new object();
        private readonly Dictionary<string, BackupJob> _backupJobs;
        private readonly object _jobsLock = new object();

        private BackupManager()
        {
            _backupJobs = new Dictionary<string, BackupJob>();
        }

        public static BackupManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BackupManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void AddBackupJob(string name, string sourcePath, string destinationPath, IBackupStrategy strategy)
        {
            lock (_jobsLock)
            {
                if (_backupJobs.ContainsKey(name))
                    throw new ArgumentException($"Une sauvegarde avec le nom '{name}' existe déjà.");

                _backupJobs[name] = new BackupJob
                {
                    Name = name,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Strategy = strategy,
                    Status = BackupStatus.Pending,
                    Progress = 0,
                    LastBackupDate = null
                };
            }
        }

        public void RemoveBackupJob(string name)
        {
            lock (_jobsLock)
            {
                if (_backupJobs.ContainsKey(name))
                {
                    _backupJobs.Remove(name);
                }
            }
        }

        public async Task<bool> StartBackupAsync(string name, IProgress<int> progress = null)
        {
            BackupJob job;
            lock (_jobsLock)
            {
                if (!_backupJobs.TryGetValue(name, out job))
                    throw new ArgumentException($"Aucune sauvegarde trouvée avec le nom '{name}'.");

                if (job.Status == BackupStatus.Running)
                    throw new InvalidOperationException($"La sauvegarde '{name}' est déjà en cours d'exécution.");

                job.Status = BackupStatus.Running;
                job.Progress = 0;
            }

            try
            {
                var result = await job.Strategy.ExecuteBackup(job.SourcePath, job.DestinationPath, progress);
                
                lock (_jobsLock)
                {
                    job.Status = result ? BackupStatus.Completed : BackupStatus.Failed;
                    job.LastBackupDate = DateTime.Now;
                    if (result) job.Progress = 100;
                }

                return result;
            }
            catch (Exception)
            {
                lock (_jobsLock)
                {
                    job.Status = BackupStatus.Failed;
                }
                throw;
            }
        }

        public void CancelBackup(string name)
        {
            lock (_jobsLock)
            {
                if (_backupJobs.TryGetValue(name, out var job) && job.Status == BackupStatus.Running)
                {
                    job.Strategy.CancelBackup();
                    job.Status = BackupStatus.Cancelled;
                }
            }
        }

        public BackupJob GetBackupJob(string name)
        {
            lock (_jobsLock)
            {
                return _backupJobs.TryGetValue(name, out var job) ? job.Clone() : null;
            }
        }

        public List<BackupJob> GetAllBackupJobs()
        {
            lock (_jobsLock)
            {
                return _backupJobs.Values.Select(j => j.Clone()).ToList();
            }
        }

        public void UpdateBackupProgress(string name, int progress)
        {
            lock (_jobsLock)
            {
                if (_backupJobs.TryGetValue(name, out var job))
                {
                    job.Progress = progress;
                }
            }
        }
    }

    public class BackupJob
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public IBackupStrategy Strategy { get; set; }
        public BackupStatus Status { get; set; }
        public int Progress { get; set; }
        public DateTime? LastBackupDate { get; set; }

        public BackupJob Clone()
        {
            return new BackupJob
            {
                Name = this.Name,
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
                Strategy = this.Strategy,
                Status = this.Status,
                Progress = this.Progress,
                LastBackupDate = this.LastBackupDate
            };
        }
    }

    public enum BackupStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
} 