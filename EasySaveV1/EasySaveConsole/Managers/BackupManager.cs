using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySaveV1;
using EasySaveV1.EasySaveConsole.Models;
using EasySaveV1.EasySaveLogging;
using System.Text.Json;

namespace EasySaveV1.EasySaveConsole.Managers
{
    public class BackupManager
    {
        private const int MaxJobs = 5;
        private readonly List<Backup> _jobs;
        private readonly Logger _logger;
        private readonly string _stateFile;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, StateModel> _jobStates;

        public BackupManager()
        {
            _jobs = Config.LoadJobs();
            _logger = Logger.GetInstance();
            _stateFile = Config.GetStateFilePath();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            _jobStates = LoadOrInitializeStates();
        }
    }


}
