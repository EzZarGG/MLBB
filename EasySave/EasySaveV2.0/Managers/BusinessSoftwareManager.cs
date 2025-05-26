using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EasySaveV2._0.Managers
{
    /// <summary>
    /// Gère la liste des noms de process métier et détecte leur exécution.
    /// </summary>
    public static class BusinessSoftwareManager
    {
        private static List<string> _businessNames = new List<string>();

        /// <summary>
        /// Initialise avec une collection de noms de process (peuvent contenir ou non l'extension .exe).
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
        /// Retourne true si au moins un des processus métier tourne actuellement.
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
                // Impossible de lister les processus : on considère qu'aucun n'est détecté
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
                    // Process inatteignable, on l'ignore
                }
            }

            return false;
        }
    }
}
