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
    }
}
