namespace EasySaveV1.EasySaveConsole.Models
{
    public class Backup
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string Type { get; set; } // "Full" ou "Differential"
        public long FileLength { get; set; }

        public override string ToString() =>
            $"{Name} [{Type}] : {SourcePath} → {TargetPath}";
    }
}