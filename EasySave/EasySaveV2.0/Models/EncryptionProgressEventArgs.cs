using System;

namespace EasySaveV2._0.Models
{
    public class EncryptionProgressEventArgs : EventArgs
    {
        public string BackupName { get; }
        public string FilePath { get; }
        public TimeSpan Duration { get; }
        public bool Success { get; }
        public string Error { get; }

        public EncryptionProgressEventArgs(string backupName, string filePath, TimeSpan duration, bool success, string error = null)
        {
            BackupName = backupName;
            FilePath = filePath;
            Duration = duration;
            Success = success;
            Error = error;
        }
    }
} 