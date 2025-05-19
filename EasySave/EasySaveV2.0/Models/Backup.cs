namespace EasySaveV2._0.Models
{
    public class Backup
    {
        // Name of the backup job
        public string Name { get; set; } = string.Empty;

        // Source directory path
        public string SourcePath { get; set; } = string.Empty;

        // Target directory path
        public string TargetPath { get; set; } = string.Empty;

        // Backup type: "Full" or "Differential"
        public string Type { get; set; } = string.Empty;

        // Total file length in bytes
        public long FileLength { get; set; }

        // Status of the backup job (default: "pending")
        public string Status { get; set; } = "pending";

        // Progress percentage (default: 0)
        public int Progress { get; set; } = 0;

        // Whether to encrypt files during backup (default: false)
        public bool Encrypt { get; set; } = false;

        // String representation of the backup job
        public override string ToString() => Name;
    }
}
