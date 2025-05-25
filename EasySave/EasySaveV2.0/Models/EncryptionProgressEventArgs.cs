using System;

namespace EasySaveV2._0.Models
{
    /// <summary>
    /// Event arguments for file encryption progress updates.
    /// Provides detailed information about the current state of a file encryption operation.
    /// </summary>
    public class EncryptionProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the backup job this encryption operation belongs to.
        /// </summary>
        public string BackupName { get; }

        /// <summary>
        /// Path of the file being encrypted.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Progress of the encryption operation as a percentage.
        /// Range: 0-100.
        /// </summary>
        public int ProgressPercentage { get; }

        /// <summary>
        /// Indicates whether the encryption operation has completed.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Indicates whether an error occurred during encryption.
        /// If true, check ErrorMessage for details.
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Detailed error message if HasError is true.
        /// Null if no error occurred.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Creates a new instance of EncryptionProgressEventArgs with progress information.
        /// </summary>
        /// <param name="backupName">Name of the backup job</param>
        /// <param name="filePath">Path of the file being encrypted</param>
        /// <param name="progressPercentage">Progress percentage (0-100)</param>
        /// <param name="isComplete">Whether encryption is complete</param>
        /// <param name="hasError">Whether an error occurred</param>
        /// <param name="errorMessage">Detailed error message if applicable</param>
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