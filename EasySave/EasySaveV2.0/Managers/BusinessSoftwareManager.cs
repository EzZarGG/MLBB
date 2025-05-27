using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EasySaveV2._0.Managers
{
    /// <summary>
    /// Manages the list of business process names and detects if they are running.
    /// </summary>
    public static class BusinessSoftwareManager
    {
        private static List<string> _businessNames = new List<string>();

        /// <summary>
        /// Initializes with a collection of process names (may or may not include the .exe extension).
        /// </summary>
        public static void Initialize(IEnumerable<string> processNames)
        {
            if (processNames == null)
                throw new ArgumentNullException(nameof(processNames));

            _businessNames = processNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => Path.GetFileNameWithoutExtension(n).ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns true if at least one of the business processes is currently running.
        /// </summary>
        public static bool IsRunning()
        {
            if (_businessNames == null || !_businessNames.Any())
                return false;

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                // Unable to list processes: consider that none are detected
                return false;
            }

            foreach (var proc in processes)
            {
                try
                {
                    if (_businessNames.Contains(proc.ProcessName.ToLowerInvariant()))
                        return true;
                }
                catch
                {
                    // Unreachable process, ignore it
                }
            }

            return false;
        }
    }
}
