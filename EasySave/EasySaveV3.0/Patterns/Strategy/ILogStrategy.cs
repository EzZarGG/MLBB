using System;
using System.Threading.Tasks;
using EasySaveV3._0.Models;

namespace EasySaveV3._0.Patterns.Strategy
{
    public interface ILogStrategy
    {
        Task WriteLogAsync(string message, LogLevel level, string source);
        Task<string> ReadLogsAsync(DateTime? startDate = null, DateTime? endDate = null, LogLevel? level = null);
        string GetFileExtension();
    }
} 