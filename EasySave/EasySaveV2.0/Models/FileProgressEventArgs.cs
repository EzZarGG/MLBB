using System;

namespace EasySaveV2._0.Models
{
    public class FileProgressEventArgs : EventArgs
    {
        public string BackupName { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }
        public long FileSize { get; }
        public int ProgressPercentage { get; }
        public long BytesTransferred { get; }
        public long TotalBytes { get; }
        public int FilesProcessed { get; }
        public int TotalFiles { get; }
        public int FilesRemaining { get; }
        public long BytesRemaining { get; }
        public TimeSpan Duration { get; }
        public bool Success { get; }

        public FileProgressEventArgs(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            int progressPercentage,
            long bytesTransferred,
            long totalBytes,
            int filesProcessed,
            int totalFiles,
            TimeSpan duration,
            bool success)
        {
            BackupName = backupName;
            SourcePath = sourcePath;
            TargetPath = targetPath;
            FileSize = fileSize;
            ProgressPercentage = progressPercentage;
            BytesTransferred = bytesTransferred;
            TotalBytes = totalBytes;
            FilesProcessed = filesProcessed;
            TotalFiles = totalFiles;
            FilesRemaining = totalFiles - filesProcessed;
            BytesRemaining = totalBytes - bytesTransferred;
            Duration = duration;
            Success = success;
        }

        // Constructor overload for simpler progress updates
        public FileProgressEventArgs(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            int progressPercentage,
            long bytesTransferred,
            long totalBytes,
            int filesProcessed,
            int totalFiles)
            : this(backupName, sourcePath, targetPath, fileSize, progressPercentage, 
                  bytesTransferred, totalBytes, filesProcessed, totalFiles, 
                  TimeSpan.Zero, true)
        {
        }
    }
}