using System;
using System.IO;
using System.Threading.Tasks;

namespace EasySaveV2._0.Patterns.Strategy
{
    public class FullBackupStrategy : IBackupStrategy
    {
        private bool _isCancelled;

        public string Name => "Sauvegarde complète";

        public async Task<bool> ExecuteBackup(string sourcePath, string destinationPath, IProgress<int> progress = null)
        {
            _isCancelled = false;
            try
            {
                if (!Directory.Exists(sourcePath))
                    throw new DirectoryNotFoundException($"Le répertoire source {sourcePath} n'existe pas.");

                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
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
                Console.WriteLine($"Erreur lors de la sauvegarde complète: {ex.Message}");
                return false;
            }
        }

        public void CancelBackup()
        {
            _isCancelled = true;
        }
    }
} 