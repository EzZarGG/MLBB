using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace EasySaveV3._0.Patterns.Strategy
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        private bool _isCancelled;

        public string Name => "Sauvegarde différentielle";

        public async Task<bool> ExecuteBackup(string sourcePath, string destinationPath, IProgress<int> progress = null)
        {
            _isCancelled = false;
            try
            {
                if (!Directory.Exists(sourcePath))
                    throw new DirectoryNotFoundException($"Le répertoire source {sourcePath} n'existe pas.");

                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                // Obtenir la date de la dernière sauvegarde complète
                var lastFullBackupDate = GetLastFullBackupDate(destinationPath);
                
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                    .Where(f => File.GetLastWriteTime(f) > lastFullBackupDate)
                    .ToArray();

                var totalFiles = files.Length;
                var processedFiles = 0;

                foreach (var file in files)
                {
                    if (_isCancelled)
                        return false;

                    var relativePath = file.Substring(sourcePath.Length + 1);
                    var destinationFile = Path.Combine(destinationPath, relativePath);
                    var destinationDir = Path.GetDirectoryName(destinationFile);

                    if (!Directory.Exists(destinationDir))
                        Directory.CreateDirectory(destinationDir);

                    await Task.Run(() => File.Copy(file, destinationFile, true));
                    processedFiles++;
                    progress?.Report((int)((float)processedFiles / totalFiles * 100));
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log l'erreur
                Console.WriteLine($"Erreur lors de la sauvegarde différentielle: {ex.Message}");
                return false;
            }
        }

        private DateTime GetLastFullBackupDate(string destinationPath)
        {
            try
            {
                // Chercher le fichier de métadonnées de la dernière sauvegarde complète
                var metadataFile = Path.Combine(destinationPath, "full_backup_metadata.txt");
                if (File.Exists(metadataFile))
                {
                    var dateStr = File.ReadAllText(metadataFile);
                    if (DateTime.TryParse(dateStr, out DateTime lastBackupDate))
                    {
                        return lastBackupDate;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture de la date de dernière sauvegarde: {ex.Message}");
            }

            // Si pas de sauvegarde précédente, retourner une date très ancienne
            return DateTime.MinValue;
        }

        public void CancelBackup()
        {
            _isCancelled = true;
        }
    }
} 