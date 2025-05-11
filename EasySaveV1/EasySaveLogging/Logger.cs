using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySaveLogging.Logger
{
    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string BackupName { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public long FileSize { get; set; }
        public long TransferTime { get; set; }
        public string Message { get; set; }
        public string LogType { get; set; }
        public string ActionType { get; set; } // Nouveau champ pour le type d'action
    }

    
}