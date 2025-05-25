using System;
using System.IO;
using System.Threading.Tasks;

namespace EasySaveV2._0.Patterns.Bridge
{
    public class FileSystemBackupImplementation : IBackupImplementation
    {
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, IProgress<int> progress = null)
        {
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                using (var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[81920];
                    var totalBytes = source.Length;
                    var bytesRead = 0L;
                    int currentBlock;

                    while ((currentBlock = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await destination.WriteAsync(buffer, 0, currentBlock);
                        bytesRead += currentBlock;
                        progress?.Report((int)((bytesRead * 100) / totalBytes));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la copie du fichier: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CopyDirectoryAsync(string sourcePath, string destinationPath, IProgress<int> progress = null)
        {
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
                    var relativePath = file.Substring(sourcePath.Length + 1);
                    var destinationFile = Path.Combine(destinationPath, relativePath);
                    await CopyFileAsync(file, destinationFile);
                    processedFiles++;
                    progress?.Report((int)((float)processedFiles / totalFiles * 100));
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la copie du répertoire: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression du fichier: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteDirectoryAsync(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    await Task.Run(() => Directory.Delete(directoryPath, true));
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression du répertoire: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateDirectoryAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    await Task.Run(() => Directory.CreateDirectory(directoryPath));
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création du répertoire: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            return await Task.Run(() => File.Exists(filePath));
        }

        public async Task<bool> DirectoryExistsAsync(string directoryPath)
        {
            return await Task.Run(() => Directory.Exists(directoryPath));
        }

        public async Task<DateTime> GetLastWriteTimeAsync(string path)
        {
            return await Task.Run(() => File.GetLastWriteTime(path));
        }
    }
} 