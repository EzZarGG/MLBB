using System.Collections.Generic;
using System.IO;

namespace EasySaveV2._0.Models
{
    public class BackupJob
    {
        public string Name { get; set; }
        public List<FileInfo> FilesToSave { get; set; }
        public string TargetDirectory { get; set; }
    }
}
