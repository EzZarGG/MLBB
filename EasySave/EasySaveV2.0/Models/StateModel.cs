using System;

namespace EasySaveV2._0.Models
{
    /// <summary>
    /// Model for tracking the state and progress of a backup job.
    /// Contains both static configuration and dynamic progress information.
    /// </summary>
    public class StateModel
    {
        /// <summary>
        /// Name of the backup job this state belongs to.
        /// Must match the corresponding Backup.Name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the last state change or action.
        /// Updated whenever the backup job's state is modified.
        /// </summary>
        public DateTime LastActionTime { get; set; }

        /// <summary>
        /// Current status of the backup job.
        /// Valid values: "Pending", "Active", "Inactive", "Paused", "Completed", "Error".
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Total number of files to be processed in the backup.
        /// Set during backup initialization.
        /// </summary>
        public int TotalFilesCount { get; set; }

        /// <summary>
        /// Total size in bytes of all files to be processed.
        /// Set during backup initialization.
        /// </summary>
        public long TotalFilesSize { get; set; }

        /// <summary>
        /// Number of files remaining to be processed.
        /// Only relevant when Status is "Active".
        /// </summary>
        public int FilesRemaining { get; set; }

        /// <summary>
        /// Number of bytes remaining to be processed.
        /// Only relevant when Status is "Active".
        /// </summary>
        public long BytesRemaining { get; set; }

        /// <summary>
        /// Path of the file currently being processed.
        /// Empty when no file is being processed.
        /// </summary>
        public string CurrentSourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Path where the current file is being saved.
        /// Empty when no file is being processed.
        /// </summary>
        public string CurrentTargetFile { get; set; } = string.Empty;

        /// <summary>
        /// Overall progress of the backup operation as a percentage.
        /// Range: 0-100.
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Creates a new state model with initial values for a backup job.
        /// </summary>
        /// <param name="jobName">Name of the backup job</param>
        /// <returns>A new StateModel instance with default values</returns>
        public static StateModel CreateInitialState(string jobName)
        {
            return new StateModel
            {
                Name = jobName,
                LastActionTime = DateTime.Now,
                Status = "Ready",
                TotalFilesCount = 0,
                TotalFilesSize = 0,
                FilesRemaining = 0,
                BytesRemaining = 0,
                CurrentSourceFile = string.Empty,
                CurrentTargetFile = string.Empty,
                ProgressPercentage = 0
            };
        }
    }
}