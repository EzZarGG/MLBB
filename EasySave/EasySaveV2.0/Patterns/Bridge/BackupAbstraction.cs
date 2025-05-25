using System;
using System.Threading.Tasks;
using EasySaveV2._0.Patterns.Strategy;

namespace EasySaveV2._0.Patterns.Bridge
{
    public abstract class BackupAbstraction
    {
        protected readonly IBackupImplementation _implementation;
        protected readonly IBackupStrategy _strategy;

        protected BackupAbstraction(IBackupImplementation implementation, IBackupStrategy strategy)
        {
            _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public abstract Task<bool> ExecuteBackupAsync(string sourcePath, string destinationPath, IProgress<int> progress = null);
        public abstract Task<bool> CancelBackupAsync();
    }

    public class ConcreteBackup : BackupAbstraction
    {
        private bool _isCancelled;

        public ConcreteBackup(IBackupImplementation implementation, IBackupStrategy strategy)
            : base(implementation, strategy)
        {
            _isCancelled = false;
        }

        public override async Task<bool> ExecuteBackupAsync(string sourcePath, string destinationPath, IProgress<int> progress = null)
        {
            _isCancelled = false;
            try
            {
                if (!await _implementation.DirectoryExistsAsync(sourcePath))
                    throw new DirectoryNotFoundException($"Le répertoire source {sourcePath} n'existe pas.");

                if (!await _implementation.DirectoryExistsAsync(destinationPath))
                    await _implementation.CreateDirectoryAsync(destinationPath);

                return await _strategy.ExecuteBackup(sourcePath, destinationPath, progress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de la sauvegarde: {ex.Message}");
                return false;
            }
        }

        public override Task<bool> CancelBackupAsync()
        {
            _isCancelled = true;
            _strategy.CancelBackup();
            return Task.FromResult(true);
        }
    }
} 