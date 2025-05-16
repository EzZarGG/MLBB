using System;

namespace EasySaveV2._0.Models
{
    public class FileProgressEventArgs : EventArgs
    {
        public string BackupName { get; }
        public string SourceFile { get; }
        public string TargetFile { get; }
        public long FileSize { get; }
        public TimeSpan Duration { get; }
        public bool Success { get; }

        public int FilesRemaining { get; }
        public long BytesRemaining { get; }

        public FileProgressEventArgs(
            string backupName,
            string sourceFile,
            string targetFile,
            long fileSize,
            TimeSpan duration,
            bool success,
            int filesRemaining,
            long bytesRemaining)
        {
            BackupName = backupName;
            SourceFile = sourceFile;
            TargetFile = targetFile;
            FileSize = fileSize;
            Duration = duration;
            Success = success;
            FilesRemaining = filesRemaining;
            BytesRemaining = bytesRemaining;
        }

    }
}