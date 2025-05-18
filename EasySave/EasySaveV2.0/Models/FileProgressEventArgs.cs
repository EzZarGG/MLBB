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
        public TimeSpan TransferTime { get; }
        public long EncryptionTime { get; }


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
            TimeSpan transferTime,
            long encryptionTime)
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
            TransferTime = transferTime;
            EncryptionTime = encryptionTime;
        }
    }
} 