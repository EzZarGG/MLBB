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
        public void ExecuteJobsByIndices(IEnumerable<int> indices)
        {

            foreach (var i in indices)
                if (i >= 1 && i <= _jobs.Count)
                    RunBackup(_jobs[i - 1]);

        }

        private void RunBackup(Backup job)
        {

            // Log the start of execution
            _logger.LogAdminAction(job.Name, "EXECUTE_START", $"Started executing backup job: {job.Name}");

            try
            {
                // Prepare file list
                var allFiles = Directory
                    .EnumerateFiles(job.SourcePath, "*", SearchOption.AllDirectories)
                    .ToList();

                long totalBytes = allFiles.Sum(f => new FileInfo(f).Length);
                int totalFiles = allFiles.Count;

                UpdateJobState(job.Name, state => {
                    state.Status = "Active";
                    state.TotalFilesCount = totalFiles;
                    state.TotalFilesSize = totalBytes;
                    state.FilesRemaining = totalFiles;
                    state.BytesRemaining = totalBytes;
                    state.CurrentSourceFile = job.SourcePath;
                    state.CurrentTargetFile = job.TargetPath;
                });

                // Affichage immédiat à la console
                Console.WriteLine($"\nJob : {job.Name}");
                Console.WriteLine($"État : Active");
                Console.WriteLine($"Total fichiers : {totalFiles}");
                Console.WriteLine($"Taille totale : {totalBytes} o");
                Console.WriteLine($"Fichiers restants : {totalFiles}");
                Console.WriteLine($"Taille restante : {totalBytes} o");
                Console.WriteLine($"Fichier source en cours : {job.SourcePath}");
                Console.WriteLine($"Destination : {job.TargetPath}");

                // Process each file
                foreach (var src in allFiles)
                {
                    var rel = Path.GetRelativePath(job.SourcePath, src);
                    var dst = Path.Combine(job.TargetPath, rel);
                    long fileSize = new FileInfo(src).Length;

                    // Ensure target directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));

                    // Update state before copying file
                    UpdateJobState(job.Name, state => {
                        state.CurrentSourceFile = src;
                        state.CurrentTargetFile = dst;
                    });

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    try
                    {
                        File.Copy(src, dst, true);
                        sw.Stop();
                        _logger.CreateLog(
                            job.Name,
                            sw.Elapsed,
                            fileSize,
                            DateTime.Now,
                            src,
                            dst,
                            "INFO"
                        );

                        // Update state after successful copy
                        UpdateJobState(job.Name, state => {
                            state.FilesRemaining -= 1;
                            state.BytesRemaining -= fileSize;
                        });
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        _logger.CreateLog(
                            job.Name,
                            sw.Elapsed.Negate(),
                            0,
                            DateTime.Now,
                            src,
                            dst,
                            "ERROR"
                        );

                        // Log the error but continue with next file
                        _logger.LogAdminAction(job.Name, "ERROR", $"Error copying file {src}: {ex.Message}");
                    }
                }

                // Set final state to Inactive
                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.FilesRemaining = 0;
                    state.BytesRemaining = 0;
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });

                // Log successful completion
                _logger.LogAdminAction(job.Name, "EXECUTE_COMPLETE", $"Completed executing backup job: {job.Name}");
            }
            catch (Exception ex)
            {
                // Handle directory not found or other critical errors
                _logger.LogAdminAction(job.Name, "ERROR", $"Critical error during backup: {ex.Message}");

                // Set state to Inactive (but with error indication)
                UpdateJobState(job.Name, state => {
                    state.Status = "Inactive";
                    state.CurrentSourceFile = "";
                    state.CurrentTargetFile = "";
                });
            }
        }

        public void ShowLogs()
        {
            _logger.DisplayLogs();
        }
    }
}