using System;
using System.Threading.Tasks;
using EasySaveV3._0.Patterns.Strategy;
using EasySaveV3._0.Patterns.Factory;
using EasySaveV3._0.Models;

namespace EasySaveV3._0.Patterns.Singleton
{
    public sealed class LogManager
    {
        private static LogManager _instance;
        private static readonly object _lock = new object();
        private ILogStrategy _logStrategy;

        private LogManager()
        {
            // Par défaut, on utilise JSON
            _logStrategy = LogStrategyFactory.CreateStrategy(LogType.Json);
        }

        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogManager();
                        }
                    }
                }
                return _instance;
            }
        }

        public void SetLogStrategy(LogType logType)
        {
            _logStrategy = LogStrategyFactory.CreateStrategy(logType);
        }

        public void SetLogStrategy(string logType)
        {
            _logStrategy = LogStrategyFactory.CreateStrategy(logType);
        }

        public async Task LogAsync(string message, LogLevel level = LogLevel.Info, string source = null)
        {
            await _logStrategy.WriteLogAsync(message, level, source);
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string source = null)
        {
            _logStrategy.WriteLogAsync(message, level, source).Wait();
        }

        public async Task<string> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null, LogLevel? level = null)
        {
            return await _logStrategy.ReadLogsAsync(startDate, endDate, level);
        }

        public string GetCurrentLogFileExtension()
        {
            return _logStrategy.GetFileExtension();
        }
    }
} 