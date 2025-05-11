using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySaveV1.EasySaveConsole.Models
{
    public class StateModel
    {
        public string Name { get; set; }
        public DateTime LastActionTime { get; set; }
        public string Status { get; set; } // "Active" or "Inactive"
        public int TotalFilesCount { get; set; }
        public long TotalFilesSize { get; set; }

        // Progress tracking - only relevant when Status is "Active"
        public int FilesRemaining { get; set; }
        public long BytesRemaining { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; }

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
                CurrentSourceFile = "",
                CurrentTargetFile = ""
            };
        }
    }
}
