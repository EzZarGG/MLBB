namespace EasySaveV2._0.Models
{
    public class Backup
    {
        // Name of the backup job
        public string Name { get; set; }

        // Source directory path
        public string SourcePath { get; set; }

        // Target directory path
        public string TargetPath { get; set; }

        // Backup type: "Full" or "Differential"
        public string Type { get; set; }

        // Total file length in bytes
        public long FileLength { get; set; }

        // Status of the backup job (default: "pending")
        public string Status { get; set; } = "pending";

        // Progress percentage (default: 0)
        public int Progress { get; set; } = 0;

        // String representation of the backup job
        public override string ToString() =>
            $"{Name} [{Type}] : {SourcePath} → {TargetPath}";
    }
}
