using System;
using System.Threading.Tasks;

namespace EasySaveV2._0.Patterns.Bridge
{
    public interface IBackupImplementation
    {
        Task<bool> CopyFileAsync(string sourcePath, string destinationPath, IProgress<int> progress = null);
        Task<bool> CopyDirectoryAsync(string sourcePath, string destinationPath, IProgress<int> progress = null);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> DeleteDirectoryAsync(string directoryPath);
        Task<bool> CreateDirectoryAsync(string directoryPath);
        Task<bool> FileExistsAsync(string filePath);
        Task<bool> DirectoryExistsAsync(string directoryPath);
        Task<DateTime> GetLastWriteTimeAsync(string path);
    }
} 