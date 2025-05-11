using System;
using System.Collections.Generic;
using EasySaveV1.EasySaveConsole.Managers;
using EasySaveV1.EasySaveConsole.Models;
using EasySaveV1.EasySaveConsole.Views;

namespace EasySaveV1.EasySaveConsole.Controllers
{
    public class BackupController
    {
        private readonly BackupManager _manager; // Manages backup jobs
        private readonly BackupView _view; // Handles user interface interactions
        private string _language = "en"; // Default language for the application

        public BackupController()
        {
            _manager = new BackupManager(); // Initialize the backup manager
            _view = new BackupView(); // Initialize the view for user interaction
        }

        public void Start(string[] args)
        {
            // Process command-line arguments
            if (args.Length > 0)
            {
                // If the argument is "execute" and indices are provided, execute the specified jobs
                if (args[0].ToLower() == "execute" && args.Length > 1)
                {
                    var indices = ParseArgs(args[1]); // Parse indices from arguments
                    _manager.ExecuteJobsByIndices(indices); // Execute jobs by indices
                    return;
                }
                // If the argument contains indices (numbers, dashes, or semicolons), execute the specified jobs
                else if (args[0].Contains("-") || args[0].Contains(";") || int.TryParse(args[0], out _))
                {
                    var indices = ParseArgs(args[0]); // Parse indices from arguments
                    _manager.ExecuteJobsByIndices(indices); // Execute jobs by indices
                    return;
                }
                // If the argument is "create" and sufficient arguments are provided, create a new backup job
                else if (args[0].ToLower() == "create" && args.Length >= 5)
                {
                    var newBackup = new Backup
                    {
                        Name = args[1], // Name of the backup
                        SourcePath = args[2], // Source directory
                        TargetPath = args[3], // Target directory
                        Type = args[4], // Type of backup (e.g., Full or Differential)
                        FileLength = 0 // Default file length
                    };
                    if (_manager.AddJob(newBackup)) // Add the new backup job
                        Console.WriteLine($"Backup '{args[1]}' created successfully.");
                    else
                        Console.WriteLine("Failed to create backup. Maximum number of jobs may have been reached.");
                    return;
                }
                // If the argument is "update" and sufficient arguments are provided, update an existing backup job
                else if (args[0].ToLower() == "update" && args.Length >= 5)
                {
                    var updatedBackup = new Backup
                    {
                        Name = args[1], // Name of the backup to update
                        SourcePath = args[2], // Updated source directory
                        TargetPath = args[3], // Updated target directory
                        Type = args[4], // Updated type of backup
                        FileLength = 0 // Default file length
                    };
                    if (_manager.UpdateJob(args[1], updatedBackup)) // Update the backup job
                        Console.WriteLine($"Backup '{args[1]}' updated successfully.");
                    else
                        Console.WriteLine($"Failed to update backup. Backup '{args[1]}' not found.");
                    return;
                }
                // If the argument is "delete" and sufficient arguments are provided, delete a backup job
                else if (args[0].ToLower() == "delete" && args.Length >= 2)
                {
                    if (_manager.RemoveJob(args[1])) // Remove the specified backup job
                        Console.WriteLine($"Backup '{args[1]}' deleted successfully.");
                    else
                        Console.WriteLine($"Failed to delete backup. Backup '{args[1]}' not found.");
                    return;
                }
                // If the argument is "list", display all backup jobs
                else if (args[0].ToLower() == "list")
                {
                    Console.WriteLine("=== Backup Jobs ===");
                    int i = 1;
                    foreach (var job in _manager.Jobs) // Iterate through all jobs
                    {
                        Console.WriteLine($"{i++}. {job}"); // Display each job
                    }
                    return;
                }
                // If the argument is "logs", display the logs
                else if (args[0].ToLower() == "logs")
                {
                    _manager.ShowLogs(); // Show logs
                    return;
                }
                // If the argument is "help", display the help menu
                else if (args[0].ToLower() == "help")
                {
                    PrintHelp(); // Print help information
                    return;
                }
            }

            // If no valid arguments are provided, use the interactive interface
            _language = _view.ChooseLanguage(); // Allow the user to choose a language
            while (true)
            {
                var opt = _view.DisplayMenu(_language); // Display the main menu
                if (opt == "7") break; // Exit the application if option 7 is selected
                HandleOption(opt); // Handle the selected menu option
            }
        }

        private void PrintHelp()
        {
            // Display the help message for command-line usage
            Console.WriteLine("EasySave Command Line Usage:");
            Console.WriteLine("  No arguments: Launch interactive interface");
            Console.WriteLine("  help: Show this help message");
            Console.WriteLine("  create <name> <source_path> <target_path> <type>: Create new backup");
            Console.WriteLine("  update <name> <source_path> <target_path> <type>: Update existing backup");
            Console.WriteLine("  delete <name>: Delete backup");
            Console.WriteLine("  list: List all backups");
            Console.WriteLine("  execute <indices>: Execute backups (e.g., 1-3 or 1;2;4)");
            Console.WriteLine("  <indices>: Execute backups (e.g., 1-3 or 1;2;4)");
            Console.WriteLine("  logs: Display logs");
        }

        private void HandleOption(string option)
        {
            // Handle the selected menu option
            switch (option)
            {
                case "1":
                    // Option 1: Create a new backup job
                    var jb = _view.AskNewBackupInfo(_language); // Get backup details from the user
                    if (!_manager.AddJob(jb)) // Try to add the job
                        _view.ShowMessage("max_jobs", _language); // Show error if max jobs reached
                    break;
                case "2":
                    // Option 2: Search and display a backup job
                    _view.DisplayBackupList(_manager.Jobs); // Display all jobs
                    if (_view.ConfirmSearch(_language)) // Confirm if the user wants to search
                    {
                        var backupName = _view.AskBackupName(_language); // Ask for the backup name
                        var b = _manager.GetJob(backupName); // Retrieve the job by name
                        if (b != null) _view.DisplayBackup(b); // Display the job if found
                        else _view.ShowMessage("not_found", _language); // Show error if not found
                    }
                    break;
                case "3":
                    // Option 3: Update an existing backup job
                    var oldName = _view.AskBackupName(_language); // Ask for the name of the job to update
                    var nb = _view.AskNewBackupInfo(_language, oldName); // Get updated details
                    if (!_manager.UpdateJob(oldName, nb)) // Try to update the job
                        _view.ShowMessage("not_found", _language); // Show error if not found
                    break;
                case "4":
                    // Option 4: Delete a backup job
                    var name = _view.AskBackupName(_language); // Ask for the name of the job to delete
                    if (!_manager.RemoveJob(name)) // Try to remove the job
                        _view.ShowMessage("not_found", _language); // Show error if not found
                    break;
                case "5":
                    // Option 5: Display logs
                    _manager.ShowLogs(); // Show logs
                    break;
                case "6":
                    // Option 6: Execute backup jobs
                    _view.DisplayBackupList(_manager.Jobs); // Display all jobs
                    var args = _view.AskBackupIndices(_language); // Ask for indices to execute
                    if (args.Length > 0)
                    {
                        foreach (var arg in args)
                        {
                            var indices = ParseArgs(arg); // Parse indices
                            _manager.ExecuteJobsByIndices(indices); // Execute jobs by indices
                        }
                        _view.ShowMessage("exec_success", _language); // Show success message
                    }
                    break;
                default:
                    // Invalid option
                    _view.ShowMessage("invalid", _language); // Show error for invalid option
                    break;
            }
        }

        private IEnumerable<int> ParseArgs(string arg)
        {
            // Parse a string of indices into a list of integers
            var list = new List<int>();
            if (arg.Contains("-"))
            {
                // If the argument contains a range (e.g., "1-3")
                var p = arg.Split('-'); // Split the range
                if (int.TryParse(p[0], out var a) && int.TryParse(p[1], out var b))
                    for (int i = a; i <= b; i++) list.Add(i); // Add all numbers in the range
            }
            else
            {
                // If the argument contains individual indices separated by semicolons (e.g., "1;2;4")
                foreach (var tok in arg.Split(';'))
                    if (int.TryParse(tok, out var x))
                        list.Add(x); // Add each parsed number to the list
            }
            return list; // Return the list of indices
        }
    }
}
