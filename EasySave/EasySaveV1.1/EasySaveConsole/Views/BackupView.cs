using System;
using System.Collections.Generic;
using EasySaveLogging;
using EasySaveV1._1.EasySaveConsole.Models;

namespace EasySaveV1._1.EasySaveConsole.Views
{
    public class BackupView
    {
        public string ChooseLanguage()
        {
            while (true)
            {
                Console.WriteLine("1. Français");
                Console.WriteLine("2. English");
                Console.Write("> ");
                var input = Console.ReadLine();

                if (input == "1") return "fr";
                if (input == "2") return "en";

                Console.WriteLine("Entrée invalide. Veuillez entrer 1 ou 2.");
            }
        }

        public string DisplayMenu(string lang)
        {
            if (lang == "fr")
            {
                Console.WriteLine("=== Menu EasySave ===");
                Console.WriteLine("1. Créer une sauvegarde");
                Console.WriteLine("2. Afficher les sauvegardes");
                Console.WriteLine("3. Modifier une sauvegarde");
                Console.WriteLine("4. Supprimer une sauvegarde");
                Console.WriteLine("5. Afficher les logs");
                Console.WriteLine("6. Exécuter des sauvegardes");
                Console.WriteLine("7. Configurer le format des logs");
                Console.WriteLine("8. Quitter");
            }
            else
            {
                Console.WriteLine("=== EasySave Menu ===");
                Console.WriteLine("1. Create backup");
                Console.WriteLine("2. Display backups");
                Console.WriteLine("3. Update backup");
                Console.WriteLine("4. Delete backup");
                Console.WriteLine("5. Show logs");
                Console.WriteLine("6. Execute backups");
                Console.WriteLine("7. Configure log format");
                Console.WriteLine("8. Exit");
            }
            Console.Write("> ");
            return Console.ReadLine();
        }

        public Backup AskNewBackupInfo(string lang, string existingName = null)
        {
            Console.Write((lang == "fr" ? "Nom" : "Name")
                          + (existingName != null ? $" ({existingName})" : "") + ": ");
            var name = existingName ?? Console.ReadLine();
            Console.Write((lang == "fr" ? "Chemin source" : "Source path") + ": ");
            var src = Console.ReadLine();
            Console.Write((lang == "fr" ? "Chemin cible" : "Target path") + ": ");
            var dst = Console.ReadLine();
            Console.Write((lang == "fr" ? "Type (Full/Differential)" : "Type (Full/Differential)") + ": ");
            var typ = Console.ReadLine();
            return new Backup
            {
                Name = name,
                SourcePath = src,
                TargetPath = dst,
                Type = typ,
                FileLength = 0
            };
        }

        public void DisplayBackupList(IEnumerable<Backup> list)
        {
            Console.WriteLine("=== Liste des sauvegardes ===");
            int i = 1;
            foreach (var b in list)
                Console.WriteLine($"{i++}. {b}");
        }

        public bool ConfirmSearch(string lang)
        {
            Console.Write(lang == "fr"
                ? "Voulez-vous chercher par nom ? (o/n): "
                : "Search by name? (y/n): ");
            var c = Console.ReadLine()?.ToLower();
            return c == "o" || c == "y";
        }

        public string AskBackupName(string lang)
        {
            Console.Write(lang == "fr" ? "Nom de la sauvegarde: " : "Backup name: ");
            return Console.ReadLine();
        }

        public void DisplayBackup(Backup b)
        {
            Console.WriteLine(b);
        }

        public string[] AskBackupIndices(string lang)
        {
            Console.WriteLine(lang == "fr"
                ? "Entrez les numéros des sauvegardes à exécuter (ex: 1;2;3 ou 1-3):"
                : "Enter backup numbers to execute (ex: 1;2;3 or 1-3):");
            Console.Write("> ");
            string input = Console.ReadLine();
            return input?.Split(';') ?? Array.Empty<string>();
        }

        public void ShowMessage(string code, string lang)
        {
            var msg = code switch
            {
                "max_jobs" => lang == "fr"
                    ? "Nombre maximum de jobs atteint."
                    : "Maximum number of jobs reached.",
                "not_found" => lang == "fr"
                    ? "Sauvegarde introuvable."
                    : "Backup not found.",
                "invalid" => lang == "fr"
                    ? "Option invalide."
                    : "Invalid option.",
                "exec_success" => lang == "fr"
                    ? "Sauvegardes exécutées avec succès."
                    : "Backups executed successfully.",
                "format_changed" => lang == "fr"
                    ? "Format de log modifié avec succès."
                    : "Log format changed successfully.",
                _ => ""
            };
            Console.WriteLine(msg);
        }
      
        public LogFormat DisplayLogFormatMenu(string lang, LogFormat currentFormat)
        {
            Console.WriteLine(lang == "fr"
                ? "=== Configuration du format des logs ==="
                : "=== Log Format Configuration ===");

            Console.WriteLine(lang == "fr"
                ? $"Format actuel: {currentFormat}"
                : $"Current format: {currentFormat}");

            Console.WriteLine("1. JSON");
            Console.WriteLine("2. XML");

            while (true)
            {
                Console.Write("> ");
                var choice = Console.ReadLine();

                if (choice == "1") return LogFormat.JSON;
                if (choice == "2") return LogFormat.XML;

                Console.WriteLine(lang == "fr"
                    ? "Choix invalide. Entrez 1 ou 2."
                    : "Invalid choice. Enter 1 or 2.");
            }
        }
    }
}