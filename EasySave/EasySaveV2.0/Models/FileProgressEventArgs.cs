using System;

namespace EasySaveV2._0.Models
{
    /// <summary>
    /// Provides detailed progress information for file transfer operations.
    /// Used to track and report the status of individual file transfers during backup.
    /// </summary>
    public class FileProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the backup job this progress update belongs to.
        /// </summary>
        public string BackupName { get; }

        /// <summary>
        /// Path of the source file being transferred.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Path where the file is being transferred to.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// Size of the current file in bytes.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// Overall progress of the transfer as a percentage.
        /// Range: 0-100.
        /// </summary>
        public int ProgressPercentage { get; }

        /// <summary>
        /// Total number of bytes transferred so far.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        /// Total number of bytes to be transferred.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Number of files processed so far.
        /// </summary>
        public int FilesProcessed { get; }

        /// <summary>
        /// Total number of files to be processed.
        /// </summary>
        public int TotalFiles { get; }

        /// <summary>
        /// Number of files remaining to be processed.
        /// Calculated as TotalFiles - FilesProcessed.
        /// </summary>
        public int FilesRemaining { get; }

        /// <summary>
        /// Number of bytes remaining to be transferred.
        /// Calculated as TotalBytes - BytesTransferred.
        /// </summary>
        public long BytesRemaining { get; }

        /// <summary>
        /// Duration of the current transfer operation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Whether the current transfer operation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Creates a new instance of FileProgressEventArgs with complete progress information.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        /// <param name="fileSize">Size of the current file</param>
        /// <param name="progressPercentage">Overall progress percentage</param>
        /// <param name="bytesTransferred">Total bytes transferred</param>
        /// <param name="totalBytes">Total bytes to transfer</param>
        /// <param name="filesProcessed">Number of files processed</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="duration">Transfer duration</param>
        /// <param name="success">Whether the transfer was successful</param>
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

        /// <summary>
        /// Creates a new instance of FileProgressEventArgs with basic progress information.
        /// Duration is set to zero and success is assumed to be true.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="targetPath">Target file path</param>
        /// <param name="fileSize">Size of the current file</param>
        /// <param name="progressPercentage">Overall progress percentage</param>
        /// <param name="bytesTransferred">Total bytes transferred</param>
        /// <param name="totalBytes">Total bytes to transfer</param>
        /// <param name="filesProcessed">Number of files processed</param>
        /// <param name="totalFiles">Total number of files</param>
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