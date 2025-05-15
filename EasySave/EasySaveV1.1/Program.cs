using System;
using System.IO;
using EasySaveV1._1.EasySaveConsole.Controllers;
using EasySaveLogging;
using EasySaveV1._1.EasySaveConsole;
using EasySaveV1._1;

namespace EasySaveV1._1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Init Logger
            var logDir = Config.GetLogDirectory();
            var logger = Logger.GetInstance();

            // Get configured log format
            var logFormat = Config.GetLogFormat();

            // Set appropriate file extension based on log format
            string fileExtension = logFormat == LogFormat.JSON ? ".json" : ".xml";
            var logFile = Path.Combine(
                logDir,
                DateTime.Today.ToString("yyyy-MM-dd") + fileExtension
            );
            logger.SetLogFilePath(logFile);

            // Set the log format
            logger.SetLogFormat(logFormat);

            // Ensure state file directory exists
            var stateFilePath = Config.GetStateFilePath();
            var stateDir = Path.GetDirectoryName(stateFilePath);
            if (!Directory.Exists(stateDir))
            {
                Directory.CreateDirectory(stateDir);
            }

            // Launch the controller
            var controller = new BackupController();
            controller.Start(args);
        }
    }
}