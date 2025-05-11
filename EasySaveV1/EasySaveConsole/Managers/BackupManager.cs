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
    private void SaveStates(List<StateModel> states)
        {
            var json = JsonSerializer.Serialize(states, _jsonOptions);
            File.WriteAllText(_stateFile, json);
        }

        private void UpdateJobState(string jobName, Action<StateModel> updateAction)
        {
            // Ensure the job has a state record
            if (!_jobStates.ContainsKey(jobName))
            {
                _jobStates[jobName] = StateModel.CreateInitialState(jobName);
            }

            // Apply the update
            updateAction(_jobStates[jobName]);

            // Update the last action time
            _jobStates[jobName].LastActionTime = DateTime.Now;

            // Save all states
            SaveStates(_jobStates.Values.ToList());
        }
        public bool AddJob(Backup job)
        {
            if (_jobs.Count >= MaxJobs) return false;
            _jobs.Add(job);
            Config.SaveJobs(_jobs);

            // Initialize state for the new job
            _jobStates[job.Name] = StateModel.CreateInitialState(job.Name);
            SaveStates(_jobStates.Values.ToList());

            // Log the action
            _logger.LogAdminAction(job.Name, "CREATE", $"Backup job created: {job.Name}");

            return true;
        }
        public bool RemoveJob(string name)
        {
            var job = _jobs.FirstOrDefault(b => b.Name == name);
            if (job == null) return false;
            _jobs.Remove(job);
            Config.SaveJobs(_jobs);

            // Remove the job's state
            if (_jobStates.ContainsKey(name))
            {
                _jobStates.Remove(name);
                SaveStates(_jobStates.Values.ToList());
            }

            // Log the action
            _logger.LogAdminAction(name, "DELETE", $"Backup job deleted: {name}");

            return true;
        }

        public bool UpdateJob(string name, Backup updated)
        {
            var idx = _jobs.FindIndex(b => b.Name == name);
            if (idx < 0) return false;

            // Update the job
            _jobs[idx] = updated;
            Config.SaveJobs(_jobs);

            // Update the state if name changed
            if (name != updated.Name && _jobStates.ContainsKey(name))
            {
                var state = _jobStates[name];
                _jobStates.Remove(name);
                state.Name = updated.Name;
                _jobStates[updated.Name] = state;
                SaveStates(_jobStates.Values.ToList());
            }

            // Log the action
            _logger.LogAdminAction(updated.Name, "UPDATE", $"Backup job updated: {name} to {updated.Name}");

            return true;
        }
    }
}
