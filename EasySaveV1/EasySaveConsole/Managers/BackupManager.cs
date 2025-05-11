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

        public IReadOnlyList<Backup> Jobs => _jobs;

        private Dictionary<string, StateModel> LoadOrInitializeStates()
        {
            var states = new Dictionary<string, StateModel>();

            // Initialize states for all existing jobs
            foreach (var job in _jobs)
            {
                states[job.Name] = StateModel.CreateInitialState(job.Name);
            }

            // Try to load existing state file if it exists
            if (File.Exists(_stateFile))
            {
                try
                {
                    var json = File.ReadAllText(_stateFile);
                    var loadedStates = JsonSerializer.Deserialize<List<StateModel>>(json, _jsonOptions);

                    if (loadedStates != null)
                    {
                        foreach (var state in loadedStates)
                        {
                            // Update existing jobs with their saved state
                            if (states.ContainsKey(state.Name))
                            {
                                states[state.Name] = state;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If there's an error reading the state file, continue with initialized states
                    _logger.LogAdminAction("System", "ERROR", $"Failed to load state file: {ex.Message}");
                }
            }

            // Save the initial state file
            SaveStates(states.Values.ToList());

            return states;
        }
    }


}
