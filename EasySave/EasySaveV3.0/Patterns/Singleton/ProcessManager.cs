using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EasySaveV3._0.Patterns.Singleton
{
    public sealed class ProcessManager
    {
        private static ProcessManager _instance;
        private static readonly object _lock = new object();
        private readonly HashSet<string> _priorityProcesses;
        private readonly HashSet<string> _blockingProcesses;
        private readonly object _processLock = new object();

        private ProcessManager()
        {
            _priorityProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _blockingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadDefaultProcesses();
        }

        public static ProcessManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ProcessManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private void LoadDefaultProcesses()
        {
            // Liste par défaut des processus prioritaires
            _priorityProcesses.Add("notepad.exe");
            _priorityProcesses.Add("word.exe");
            _priorityProcesses.Add("excel.exe");
            _priorityProcesses.Add("powerpnt.exe");

            // Liste par défaut des processus bloquants
            _blockingProcesses.Add("notepad.exe");
            _blockingProcesses.Add("word.exe");
            _blockingProcesses.Add("excel.exe");
            _blockingProcesses.Add("powerpnt.exe");
            _blockingProcesses.Add("chrome.exe");
            _blockingProcesses.Add("firefox.exe");
            _blockingProcesses.Add("msedge.exe");
        }

        public void AddPriorityProcess(string processName)
        {
            lock (_processLock)
            {
                _priorityProcesses.Add(processName.ToLower());
            }
        }

        public void RemovePriorityProcess(string processName)
        {
            lock (_processLock)
            {
                _priorityProcesses.Remove(processName.ToLower());
            }
        }

        public void AddBlockingProcess(string processName)
        {
            lock (_processLock)
            {
                _blockingProcesses.Add(processName.ToLower());
            }
        }

        public void RemoveBlockingProcess(string processName)
        {
            lock (_processLock)
            {
                _blockingProcesses.Remove(processName.ToLower());
            }
        }

        public bool IsProcessPriority(string processName)
        {
            lock (_processLock)
            {
                return _priorityProcesses.Contains(processName.ToLower());
            }
        }

        public bool IsProcessBlocking(string processName)
        {
            lock (_processLock)
            {
                return _blockingProcesses.Contains(processName.ToLower());
            }
        }

        public async Task<bool> AreBlockingProcessesRunningAsync()
        {
            return await Task.Run(() =>
            {
                var runningProcesses = Process.GetProcesses()
                    .Select(p => p.ProcessName.ToLower())
                    .ToList();

                lock (_processLock)
                {
                    return runningProcesses.Any(p => _blockingProcesses.Contains(p));
                }
            });
        }

        public async Task<List<string>> GetRunningPriorityProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var runningProcesses = Process.GetProcesses()
                    .Select(p => p.ProcessName.ToLower())
                    .ToList();

                lock (_processLock)
                {
                    return runningProcesses.Where(p => _priorityProcesses.Contains(p)).ToList();
                }
            });
        }

        public async Task<List<string>> GetRunningBlockingProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var runningProcesses = Process.GetProcesses()
                    .Select(p => p.ProcessName.ToLower())
                    .ToList();

                lock (_processLock)
                {
                    return runningProcesses.Where(p => _blockingProcesses.Contains(p)).ToList();
                }
            });
        }

        public List<string> GetAllPriorityProcesses()
        {
            lock (_processLock)
            {
                return _priorityProcesses.ToList();
            }
        }

        public List<string> GetAllBlockingProcesses()
        {
            lock (_processLock)
            {
                return _blockingProcesses.ToList();
            }
        }
    }
} 