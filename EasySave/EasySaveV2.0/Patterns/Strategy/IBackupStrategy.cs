using System;
using System.Threading.Tasks;

namespace EasySaveV2._0.Patterns.Strategy
{
    public interface IBackupStrategy
    {
        string Name { get; }
        Task<bool> ExecuteBackup(string sourcePath, string destinationPath, IProgress<int> progress = null);
        void CancelBackup();
    }
} 