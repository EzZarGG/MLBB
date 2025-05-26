using System;

namespace EasySaveV3._0.Patterns.Observer
{
    public interface IBackupObserver
    {
        void OnBackupStateChanged(string backupName, string state, int progress);
        void OnBackupCompleted(string backupName, bool success);
        void OnBackupError(string backupName, string errorMessage);
    }
} 