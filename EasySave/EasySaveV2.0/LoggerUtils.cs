using System;
using System.IO;
using EasySaveLogging;

namespace EasySaveV2._0
{
    public static class LoggerUtils
    {
        private static bool _isLoggerInitialized = false;

        public static void EnsureLoggerInitialized()
        {
            if (!_isLoggerInitialized)
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                Logger.GetInstance().SetLogFilePath(Path.Combine(logDir, "application.log"));
                _isLoggerInitialized = true;
            }
        }
    }
} 