using System;

namespace EasySaveV2._0.Models
{
    public class StateModel
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastActionTime { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending" or "Active" or "Inactive"
        public int TotalFilesCount { get; set; }
        public long TotalFilesSize { get; set; }

        // Progress tracking - only relevant when Status is "Active"
        public int FilesRemaining { get; set; }
        public long BytesRemaining { get; set; }
        public string CurrentSourceFile { get; set; } = string.Empty;
        public string CurrentTargetFile { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }

        // Constructor for initial state
        public static StateModel CreateInitialState(string jobName)
        {
            return new StateModel
            {
                Name = jobName,
                LastActionTime = DateTime.Now,
                Status = "Pending",
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