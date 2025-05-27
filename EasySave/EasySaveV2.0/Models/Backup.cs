/// <summary>
/// Represents a backup job configuration in the EasySave application.
/// Contains all necessary information to define and track a backup operation.
/// </summary>
namespace EasySaveV2._0.Models
{
    /// <summary>
    /// Defines a backup job with its configuration and current state.
    /// Used for both job definition and progress tracking.
    /// </summary>
    public class Backup
    {
        /// <summary>
        /// Unique identifier for the backup job.
        /// Must be non-empty and unique across all backup jobs.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Source directory path to be backed up.
        /// Must be a valid directory path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Target directory path where the backup will be stored.
        /// Must be a valid directory path with write permissions.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Type of backup operation.
        /// Valid values: "Full" (complete backup) or "Differential" (incremental backup).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Total size of all files to be backed up in bytes.
        /// Updated during backup preparation.
        /// </summary>
        public long FileLength { get; set; }

        /// <summary>
        /// Current status of the backup job.
        /// Common values: "pending", "active", "paused", "completed", "error".
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Current progress of the backup operation as a percentage.
        /// Range: 0-100.
        /// </summary>
        public int Progress { get; set; } = 0;

        /// <summary>
        /// Whether files should be encrypted during backup.
        /// When true, files are encrypted using AES encryption.
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// Returns the backup job name as its string representation.
        /// </summary>
        /// <returns>The name of the backup job</returns>
        public override string ToString() => Name;
    }
}
