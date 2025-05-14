# EasySaveV1 User Guide

## English

### Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Creating a Backup Job](#creating-a-backup-job)
5. [Managing Backup Jobs](#managing-backup-jobs)
6. [Executing Backups](#executing-backups)
7. [Understanding Logs](#understanding-logs)
8. [Troubleshooting](#troubleshooting)
9. [Command Line Usage](#command-line-usage)

### Introduction
EasySaveV1 is a backup software solution designed to provide reliable and efficient backup capabilities. This guide will walk you through all the features and functionality of the application to help you secure your data effectively.

### Installation
1. Ensure your system meets the minimum requirements:
   - Windows operating system
   - .NET Core 6/7/8

2. Installation steps:
   - Download or clone the repository from GitHub: https://github.com/EzZarGG/MLBB.git
   - Open the solution file in Visual Studio
   - Build the solution (Ctrl+Shift+B)
   - Navigate to the output directory and run the executable file

### Getting Started
When you first launch EasySaveV1, you'll be presented with the main menu showing the following options:
- Create a new backup job
- View existing backup jobs
- Execute a backup job
- Modify a backup job
- Delete a backup job
- Exit the application

Navigate through the menu by typing the corresponding number and pressing Enter.

### Creating a Backup Job
To create a new backup job:

1. Select "Create a new backup job" from the main menu
2. Enter a unique name for your backup job
3. Enter the source directory path (the folder you want to back up)
4. Enter the target directory path (where you want the backup to be stored)
5. Select the backup type:
   - Complete backup: Copies all files from source to target
   - Differential backup: Copies only files that have changed since the last backup

6. Confirm your selection to create the job

### Managing Backup Jobs
You can view, modify, or delete existing backup jobs from the main menu:

- **View Jobs**: Lists all created backup jobs with their settings
- **Modify Jobs**: Allows you to update the source path, target path, or backup type
- **Delete Jobs**: Removes a backup job from the system

### Executing Backups
To execute a backup job:

1. Select "Execute a backup job" from the main menu
2. Choose the job you want to execute from the list
3. The backup process will begin, and you'll see progress information displayed
4. Upon completion, a summary will be shown with statistics about the operation

### Understanding Logs
EasySaveV1 maintains two types of logs:

1. **Status Logs**: Real-time information about ongoing backup jobs
   - Location: [AppDirectory]/Logs/status
   - Format: JSON files with timestamp information

2. **Activity Logs**: Detailed history of all backup operations
   - Location: [AppDirectory]/Logs/activity
   - Format: JSON and/or XML files with comprehensive details

These logs include information such as:
- Job name
- Source and target path
- File size and count
- Transfer time
- Encryption status

### Troubleshooting
Common issues and solutions:

- **Access Denied Errors**: Ensure the application has proper permissions to access the source and target directories
- **Job Not Found**: Verify that the job name exists in the system
- **Path Not Found**: Confirm that the specified paths are valid and accessible
- **Backup Failed**: Check logs for specific error messages and ensure adequate disk space is available

For additional support, please submit issues on the GitHub repository.

### Command Line Usage
EasySaveV1 can be used either in interactive mode or through command line. Here's how to use the command line functionality:

#### Prerequisites
- .NET 8.0 or higher installed
- EasySaveV1 source code extracted

#### Available Commands

##### Display Help
```bash
dotnet run -- help
```

##### Create a Backup
```bash
dotnet run -- create "BackupName" "C:\Source\Path" "D:\Destination\Path" "Full"
```

Arguments:
1. `create`: Creation command
2. Backup name (must be unique)
3. Source path (folder to backup)
4. Destination path (where files will be copied)
5. Backup type ("Full" or "Differential")

##### Update a Backup
```bash
dotnet run -- update "BackupName" "C:\New\Source\Path" "D:\New\Destination\Path" "Differential"
```

Arguments:
1. `update`: Update command
2. Name of the existing backup to modify
3. New source path (or the same to keep it)
4. New destination path (or the same to keep it)
5. New backup type (or the same to keep it)

##### Delete a Backup
```bash
dotnet run -- delete "BackupName"
```

Arguments:
1. `delete`: Delete command
2. Name of the backup to delete

##### List Existing Backups
```bash
dotnet run -- list
```

##### Execute Backups
```bash
dotnet run -- execute "1-3"   # Executes backups 1 to 3
dotnet run -- execute "1;2;4" # Executes backups 1, 2 and 4
```

Or simply:
```bash
dotnet run -- "1-3"   # The "execute" command is implicit
dotnet run -- "1;2;4"
```

##### Display Logs
```bash
dotnet run -- logs
```

#### Usage Examples

```bash
# Create three backups
dotnet run -- create "Documents" "C:\Users\User\Documents" "D:\Backup\Documents" "Full"
dotnet run -- create "Images" "C:\Users\User\Pictures" "D:\Backup\Pictures" "Full"
dotnet run -- create "Projects" "C:\Users\User\Projects" "D:\Backup\Projects" "Differential"

# List backups
dotnet run -- list

# Update a backup
dotnet run -- update "Documents" "C:\Users\User\Documents" "E:\Backup\Documents" "Full"

# Execute multiple backups
dotnet run -- "1-2"

# Display logs
dotnet run -- logs

# Delete a backup
dotnet run -- delete "Images"
```

#### Notes
- If you run the application without arguments, interactive mode will be launched
- The maximum number of backups is limited to 5
- Logs are saved in the "Logs" folder at the application root, unless the `EASYSAVE_LOG_DIR` environment variable is defined
- Backup states are saved in the "State" folder at the application root, unless the `EASYSAVE_STATE_DIR` environment variable is defined

---

# Guide d'Utilisateur EasySaveV1

## Fran�ais

### Table des Mati�res
1. [Introduction](#introduction-1)
2. [Installation](#installation-1)
3. [Premiers Pas](#premiers-pas)
4. [Cr�ation d'un Travail de Sauvegarde](#cr�ation-dun-travail-de-sauvegarde)
5. [Gestion des Travaux de Sauvegarde](#gestion-des-travaux-de-sauvegarde)
6. [Ex�cution des Sauvegardes](#ex�cution-des-sauvegardes)
7. [Comprendre les Journaux](#comprendre-les-journaux)
8. [D�pannage](#d�pannage)
9. [Utilisation en Ligne de Commande](#utilisation-en-ligne-de-commande)

### Introduction
EasySaveV1 est une solution de sauvegarde con�ue pour offrir des capacit�s de sauvegarde fiables et efficaces. Ce guide vous accompagnera � travers toutes les fonctionnalit�s de l'application pour vous aider � s�curiser vos donn�es efficacement.

### Installation
1. Assurez-vous que votre syst�me r�pond aux exigences minimales :
   - Syst�me d'exploitation Windows
   - .NET Core 6/7/8

2. �tapes d'installation :
   - T�l�chargez ou clonez le d�p�t depuis GitHub : https://github.com/EzZarGG/MLBB.git
   - Ouvrez le fichier de solution dans Visual Studio
   - Compilez la solution (Ctrl+Shift+B)
   - Naviguez jusqu'au r�pertoire de sortie et ex�cutez le fichier ex�cutable

### Premiers Pas
Lorsque vous lancez EasySaveV1 pour la premi�re fois, vous verrez le menu principal affichant les options suivantes :
- Cr�er un nouveau travail de sauvegarde
- Afficher les travaux de sauvegarde existants
- Ex�cuter un travail de sauvegarde
- Modifier un travail de sauvegarde
- Supprimer un travail de sauvegarde
- Quitter l'application

Naviguez dans le menu en tapant le num�ro correspondant et en appuyant sur Entr�e.

### Cr�ation d'un Travail de Sauvegarde
Pour cr�er un nouveau travail de sauvegarde :

1. S�lectionnez "Cr�er un nouveau travail de sauvegarde" dans le menu principal
2. Entrez un nom unique pour votre travail de sauvegarde
3. Entrez le chemin du r�pertoire source (le dossier que vous souhaitez sauvegarder)
4. Entrez le chemin du r�pertoire cible (o� vous souhaitez stocker la sauvegarde)
5. S�lectionnez le type de sauvegarde :
   - Sauvegarde compl�te : Copie tous les fichiers de la source vers la cible
   - Sauvegarde diff�rentielle : Copie uniquement les fichiers qui ont �t� modifi�s depuis la derni�re sauvegarde

6. Confirmez votre s�lection pour cr�er le travail

### Gestion des Travaux de Sauvegarde
Vous pouvez afficher, modifier ou supprimer des travaux de sauvegarde existants depuis le menu principal :

- **Afficher les Travaux** : Liste tous les travaux de sauvegarde cr��s avec leurs param�tres
- **Modifier les Travaux** : Vous permet de mettre � jour le chemin source, le chemin cible ou le type de sauvegarde
- **Supprimer les Travaux** : Supprime un travail de sauvegarde du syst�me

### Ex�cution des Sauvegardes
Pour ex�cuter un travail de sauvegarde :

1. S�lectionnez "Ex�cuter un travail de sauvegarde" dans le menu principal
2. Choisissez le travail que vous souhaitez ex�cuter dans la liste
3. Le processus de sauvegarde commencera, et vous verrez les informations de progression affich�es
4. � la fin, un r�sum� sera affich� avec des statistiques sur l'op�ration

### Comprendre les Journaux
EasySaveV1 maintient deux types de journaux :

1. **Journaux d'�tat** : Informations en temps r�el sur les travaux de sauvegarde en cours
   - Emplacement : [R�pertoireApplication]/Logs/status
   - Format : Fichiers JSON avec informations d'horodatage

2. **Journaux d'Activit�** : Historique d�taill� de toutes les op�rations de sauvegarde
   - Emplacement : [R�pertoireApplication]/Logs/activity
   - Format : Fichiers JSON et/ou XML avec des d�tails complets

Ces journaux incluent des informations telles que :
- Nom du travail
- Chemin source et cible
- Taille et nombre de fichiers
- Temps de transfert
- �tat de chiffrement

### D�pannage
Probl�mes courants et solutions :

- **Erreurs d'Acc�s Refus�** : Assurez-vous que l'application dispose des autorisations appropri�es pour acc�der aux r�pertoires source et cible
- **Travail Non Trouv�** : V�rifiez que le nom du travail existe dans le syst�me
- **Chemin Non Trouv�** : Confirmez que les chemins sp�cifi�s sont valides et accessibles
- **�chec de Sauvegarde** : Consultez les journaux pour des messages d'erreur sp�cifiques et assurez-vous que l'espace disque disponible est suffisant

Pour un support suppl�mentaire, veuillez soumettre des probl�mes sur le d�p�t GitHub.

### Utilisation en Ligne de Commande
EasySaveV1 peut �tre utilis� soit en mode interactif, soit en ligne de commande. Voici comment utiliser les fonctionnalit�s en ligne de commande :

#### Pr�requis
- .NET 8.0 ou sup�rieur install�
- Code source d'EasySaveV1 extrait

#### Commandes disponibles

##### Afficher l'aide
```bash
dotnet run -- help
```

##### Cr�er une sauvegarde
```bash
dotnet run -- create "NomSauvegarde" "C:\Chemin\Source" "D:\Chemin\Destination" "Full"
```

Arguments:
1. `create` : Commande de cr�ation
2. Nom de la sauvegarde (doit �tre unique)
3. Chemin source (dossier � sauvegarder)
4. Chemin destination (o� les fichiers seront copi�s)
5. Type de sauvegarde ("Full" ou "Differential")

##### Mettre � jour une sauvegarde
```bash
dotnet run -- update "NomSauvegarde" "C:\Nouveau\Chemin\Source" "D:\Nouveau\Chemin\Destination" "Differential"
```

Arguments:
1. `update` : Commande de mise � jour
2. Nom de la sauvegarde existante � modifier
3. Nouveau chemin source (ou le m�me pour le conserver)
4. Nouveau chemin destination (ou le m�me pour le conserver)
5. Nouveau type de sauvegarde (ou le m�me pour le conserver)

##### Supprimer une sauvegarde
```bash
dotnet run -- delete "NomSauvegarde"
```

Arguments:
1. `delete` : Commande de suppression
2. Nom de la sauvegarde � supprimer

##### Lister les sauvegardes existantes
```bash
dotnet run -- list
```

##### Ex�cuter des sauvegardes
```bash
dotnet run -- execute "1-3"   # Ex�cute les sauvegardes 1 � 3
dotnet run -- execute "1;2;4" # Ex�cute les sauvegardes 1, 2 et 4
```

Ou simplement:
```bash
dotnet run -- "1-3"   # La commande "execute" est implicite
dotnet run -- "1;2;4"
```

##### Afficher les logs
```bash
dotnet run -- logs
```

#### Exemples d'utilisation

```bash
# Cr�er trois sauvegardes
dotnet run -- create "Documents" "C:\Users\User\Documents" "D:\Backup\Documents" "Full"
dotnet run -- create "Images" "C:\Users\User\Pictures" "D:\Backup\Pictures" "Full"
dotnet run -- create "Projets" "C:\Users\User\Projects" "D:\Backup\Projects" "Differential"

# Lister les sauvegardes
dotnet run -- list

# Mettre � jour une sauvegarde
dotnet run -- update "Documents" "C:\Users\User\Documents" "E:\Backup\Documents" "Full"

# Ex�cuter plusieurs sauvegardes
dotnet run -- "1-2"

# Afficher les logs
dotnet run -- logs

# Supprimer une sauvegarde
dotnet run -- delete "Images"
```

#### Notes
- Si vous ex�cutez l'application sans arguments, le mode interactif sera lanc�
- Le nombre maximum de sauvegardes est limit� � 5
- Les logs sont enregistr�s dans le dossier "Logs" � la racine de l'application, sauf si la variable d'environnement `EASYSAVE_LOG_DIR` est d�finie
- Les �tats de sauvegarde sont enregistr�s dans le dossier "State" � la racine de l'application, sauf si la variable d'environnement `EASYSAVE_STATE_DIR` est d�finie