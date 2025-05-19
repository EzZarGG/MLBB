using System;

namespace EasySaveV2._0.Models
{
    public class EncryptionProgressEventArgs : EventArgs
    {
        public string BackupName { get; }
        public string FilePath { get; }
        public int ProgressPercentage { get; }
        public bool IsComplete { get; }
        public bool HasError { get; }
        public string ErrorMessage { get; }

        public EncryptionProgressEventArgs(
            string backupName,
            string filePath,
            int progressPercentage,
            bool isComplete = false,
            bool hasError = false,
            string errorMessage = null)
        {
            BackupName = backupName;
            FilePath = filePath;
            ProgressPercentage = progressPercentage;
            IsComplete = isComplete;
            HasError = hasError;
            ErrorMessage = errorMessage;
        }
    }
} 