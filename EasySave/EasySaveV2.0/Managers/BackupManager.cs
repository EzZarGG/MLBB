using EasySaveV2._0.Models;
using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySaveV2._0.Managers
{
    public class BackupManager
    {
        private readonly List<Backup> _backups;
        private readonly Logger _logger;
        private readonly string _stateFile;
        private readonly Dictionary<string, StateModel> _jobStates;
        private readonly JsonSerializerOptions _jsonOptions;

        public BackupManager()
        {
            _backups = new List<Backup>();
            _logger = Logger.GetInstance();
            _stateFile = Config.GetStateFilePath();
            _jobStates = new Dictionary<string, StateModel>();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            LoadOrInitializeStates();
        }

        public IReadOnlyList<Backup> Jobs => _backups;

        private void LoadOrInitializeStates()
        {
        }

        private void SaveStates(List<StateModel> states)
        {
        }

        private void UpdateJobState(string jobName, Action<StateModel> updateAction)
        {
        }

        public bool AddJob(Backup job)
        {
            return false;
        }

        public bool RemoveJob(string name)
        {
            return false;
        }

        public bool UpdateJob(string name, Backup updated)
        {
            return false;
        }

        public Backup GetJob(string name)
        {
            return null;
        }

        public StateModel GetJobState(string name)
        {
            return null;
        }

        public void ExecuteJobsByIndices(IEnumerable<int> indices)
        {
        }

        private void RunBackup(Backup job)
        {
        }

        public void ShowLogs()
        {
        }

        private void SaveJobs()
        {
        }

        private void LoadStates()
        {
        }
    }
} 