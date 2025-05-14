using System;
using System.IO;
using EasySaveV1.EasySaveConsole.Controllers;
using EasySaveLogging;
using EasySaveV1.EasySaveConsole;

namespace EasySaveV1
{
    class Program
{
    static void Main(string[] args)
    {
        // Init Logger
        var logDir = Config.GetLogDirectory();
        var logger = Logger.GetInstance();
        var logFile = Path.Combine(
            logDir,
            DateTime.Today.ToString("yyyy-MM-dd") + ".json"
        );
        logger.SetLogFilePath(logFile);

        // Ensure state file directory exists
        var stateFilePath = Config.GetStateFilePath();
        var stateDir = Path.GetDirectoryName(stateFilePath);
        if (!Directory.Exists(stateDir))
        {
            Directory.CreateDirectory(stateDir);
        }

        // Lancer le Controller
        var controller = new BackupController();
        controller.Start(args);
    }
}
}