## EasySave User Guide V1 & V1.1

### Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started (V1)](#getting-started-v1)
4. [Creating and Managing Backup Jobs (V1)](#jobs-v1)
5. [Executing Backups (V1)](#executing-v1)
6. [Logs and Troubleshooting (V1)](#logs-v1)
7. [Command-Line Usage (V1)](#cli-v1)
8. [What's New in V1.1](#whats-new-v11)

   * 8.1 [New Features](#new-features-v11)
   * 8.2 [Enhancements and Fixes](#enhancements-v11)
9. [Getting Started (V1.1)](#getting-started-v11)
10. [Support & Contribution](#support)

---

## Introduction

EasySave is a cross-platform backup solution. This guide covers both V1 (the original release) and V1.1 (the incremental update), detailing installation, core workflows, troubleshooting, and new improvements.

---

## Installation

1. **Prerequisites**:

   * Windows OS
   * [.NET Core 6, 7 or 8](https://dotnet.microsoft.com/) installed
2. **Clone & Build**:

   ```bash
   git clone -b dev https://github.com/EzZarGG/MLBB.git
   cd MLBB/Docs
   ```
3. **Open & Compile**:

   * Open `EasySave.sln` in Visual Studio
   * Build (Ctrl+Shift+B)
4. **Run**:

   * Navigate to `bin/Debug/net8.0/` (or relevant folder)
   * Launch `EasySave.exe`

---

## Getting Started (V1)

When launching for the first time, the main interactive menu presents:

* Create a new backup job
* View existing jobs
* Execute a job
* Modify a job
* Delete a job
* Exit

Use numeric choices to navigate.

---

## Creating and Managing Backup Jobs (V1)

### Create a Job

1. Select **Create a new backup job**
2. Enter a unique name
3. Provide **Source Directory** path
4. Provide **Destination Directory** path
5. Choose backup type:

   * **Full**: copies all files each time
   * **Differential**: copies only changed files since last run
6. Confirm to save the job

### View Jobs

Displays a numbered list of saved jobs with settings (name, source, target, type).

### Modify a Job

1. Choose **Modify a backup job**
2. Select existing job by its number
3. Update source, target or type
4. Save changes

### Delete a Job

1. Choose **Delete a backup job**
2. Select job number to remove permanently

---

## Executing Backups (V1)

1. Select **Execute a backup job**
2. Choose one or multiple jobs (e.g., `1-3` or `1;2;4`)
3. View live progress output
4. On completion, see summary:

   * Files scanned
   * Files copied
   * Duration

---

## Logs and Troubleshooting (V1)

### Log Types

* **Status Logs**: Real-time JSON at `Logs/status`
* **Activity Logs**: JSON/XML history at `Logs/activity`

Logs record job name, paths, file counts, sizes, timestamps, and encryption status.

### Common Issues

* **Access Denied**: Check folder permissions
* **Job Not Found**: Verify name in system
* **Path Not Found**: Confirm paths exist
* **Backup Failed**: Inspect logs for details and free up space

---

## Command-Line Usage (V1)

Run with dotnet or exe arguments.

### Help

=======
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
### Create

```bash
dotnet run -- create "Name" "C:\Src" "D:\Dst" Full
```

### Update

```bash
dotnet run -- update "Name" "C:\NewSrc" "D:\NewDst" Differential
```

### Delete

```bash
dotnet run -- delete "Name"
```

### List Existing Backups
```bash
dotnet run -- list
```
### Execute Backups

```bash
dotnet run -- execute "1-3"   # Executes backups 1 to 3
dotnet run -- execute "1;2;4" # Executes backups 1, 2 and 4
```

Or simply:
```bash
dotnet run -- "1-3"   # The "execute" command is implicit
dotnet run -- "1;2;4"
```

### Display Logs

```bash
dotnet run -- logs
```
Notes:

* Running without args enters interactive mode
* Max 5 backup jobs
* Override log directory via `EASYSAVE_LOG_DIR`
* Override state directory via `EASYSAVE_STATE_DIR`

---

## What's New in V1.1

### 8.1 New Features

* **Collaboration Mode**: share and synchronize jobs in real-time
* **WebSocket Notifications**: live alerts on job status
* **Encrypted Archives**: optional AES-256 encryption for backups

### 8.2 Enhancements and Fixes

* Updated dependencies for security
* Resolved pagination bug in job lists
* Optimized API queries and file enumeration
* Improved error messages and logging detail

---

## Getting Started (V1.1)

1. **Update the app**:

   ```bash
   git pull && dotnet build
   ```
2. **Launch V1.1**:

   ```bash
   dotnet run -- start:v1.1
   ```
3. **Migration** (if upgrading):

   ```bash
   dotnet run -- migrate:v11
   ```
4. **Use new options**:

   * Enable collaboration in Settings
   * Toggle encryption per job
   * Subscribe to notifications

---

## Support & Contribution

For help or to contribute:

* Open an issue or PR on [GitHub](https://github.com/EzZarGG/MLBB)
* Join our discussion board

---
---
---

## Guide de lutilisateur EasySave V1 & V1.1

### Table des matières

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Premiers pas (V1)](#premiers-pas-v1)
4. [Création et gestion des travaux de sauvegarde (V1)](#jobs-v1)
5. [Exécution des sauvegardes (V1)](#executing-v1)
6. [Journaux et dépannage (V1)](#logs-v1)
7. [Utilisation en ligne de commande (V1)](#cli-v1)
8. [Nouveautés de la V1.1](#whats-new-v11)

   * 8.1 [Nouvelles fonctionnalités](#new-features-v11)
   * 8.2 [Améliorations et corrections](#enhancements-v11)
9. [Premiers pas (V1.1)](#getting-started-v11)
10. [Support et contribution](#support)

---

## Introduction

EasySave est une solution de sauvegarde multiplateforme. Ce guide couvre la version initiale V1 et la mise à jour incrémentale V1.1, détaillant linstallation, les flux de travail principaux, le dépannage et les améliorations apportées.

---

## Installation

1. **Prérequis** :

   * Système dexploitation Windows
   * [.NET Core 6, 7 ou 8](https://dotnet.microsoft.com/) installé
2. **Clonage et build** :

   ```bash
   git clone -b dev https://github.com/EzZarGG/MLBB.git
   cd MLBB/Docs
   ```
3. **Ouverture et compilation** :

   * Ouvrez `EasySave.sln` dans Visual Studio
   * Compilez (Ctrl+Shift+B)
4. **Exécution** :

   * Allez dans `bin/Debug/net8.0/` (ou le dossier correspondant)
   * Lancez `EasySave.exe`

---

## Premiers pas (V1)

Au premier lancement, le menu interactif principal propose :

* Créer un nouveau travail de sauvegarde
* Afficher les travaux existants
* Exécuter un travail
* Modifier un travail
* Supprimer un travail
* Quitter

Naviguez à laide des numéros correspondants.

---

## Création et gestion des travaux de sauvegarde (V1)

### Créer un travail

1. Choisissez **Créer un nouveau travail de sauvegarde**
2. Saisissez un nom unique
3. Indiquez le **répertoire source**
4. Indiquez le **répertoire de destination**
5. Sélectionnez le type de sauvegarde :

   * **Complète** : copie tous les fichiers à chaque exécution
   * **Différentielle** : copie seulement les fichiers modifiés depuis la dernière sauvegarde
6. Confirmez pour enregistrer le travail

### Afficher les travaux

Affiche la liste numérotée des travaux enregistrés (nom, source, destination, type).

### Modifier un travail

1. Sélectionnez **Modifier un travail de sauvegarde**
2. Choisissez le numéro du travail
3. Mettez à jour le source, la destination ou le type
4. Enregistrez les modifications

### Supprimer un travail

1. Sélectionnez **Supprimer un travail de sauvegarde**
2. Choisissez le numéro du travail à supprimer

---

## Exécution des sauvegardes (V1)

1. Sélectionnez **Exécuter un travail de sauvegarde**
2. Choisissez un ou plusieurs travaux (ex. `1-3` ou `1;2;4`)
3. Suivez la progression en temps réel
4. À la fin, un récapitulatif indique :

   * Nombre de fichiers scannés
   * Nombre de fichiers copiés
   * Durée de lopération

---

## Journaux et dépannage (V1)

### Types de journaux

* **Journaux détat** : JSON en temps réel dans `Logs/status`
* **Journaux dactivité** : historique en JSON/XML dans `Logs/activity`

Les journaux contiennent : nom du travail, chemins, nombre et taille de fichiers, horodatages, statut de chiffrement.

### Problèmes courants

* **Accès refusé** : vérifiez les permissions des dossiers
* **Travail introuvable** : confirmez le nom du travail
* **Chemin introuvable** : vérifiez lexistence des chemins
* **Échec de la sauvegarde** : consultez les journaux et libérez de lespace disque

---

## Utilisation en ligne de commande (V1)

Lancez lapplication avec des arguments dotnet ou exe.

### Afficher l'aide
```bash
dotnet run -- help
```
### Créer

```bash
dotnet run -- create "Nom" "C:\Src" "D:\Dst" Full
```

### Mettre à jour

```bash
dotnet run -- update "Nom" "C:\NewSrc" "D:\NewDst" Differential
```

### Supprimer

```bash
dotnet run -- delete "Nom"
```

### Lister
```bash
dotnet run -- list
```
### Exécuter

```bash
dotnet run -- execute "1-3"   # Exécute les sauvegardes 1 à 3
dotnet run -- execute "1;2;4" # Exécute les sauvegardes 1, 2 et 4
```

Ou simplement:

```bash
dotnet run -- "1-3"   # La commande "execute" est implicite
dotnet run -- "1;2;4"
```

### Journaux

```bash
dotnet run -- logs
```

**Notes** :

* Sans arguments, le mode interactif se lance
* Maximum 5 travaux de sauvegarde
* Personnalisez le dossier des journaux via `EASYSAVE_LOG_DIR`
* Personnalisez le dossier détat via `EASYSAVE_STATE_DIR`

---

## Nouveautés de la V1.1

### 8.1 Nouvelles fonctionnalités

* **Mode collaboration** : partage et synchronisation des travaux en temps réel
* **Notifications WebSocket** : alertes en direct sur létat des travaux
* **Archives chiffrées** : chiffrement AES-256 optionnel des sauvegardes

### 8.2 Améliorations et corrections

* Mise à jour des dépendances pour la sécurité
* Correction du bug de pagination dans la liste des travaux
* Optimisation des requêtes API et de lénumération des fichiers
* Messages derreur et journaux plus détaillés

---

## Premiers pas (V1.1)

1. **Mettez à jour lapplication** :

   ```bash
   git pull && dotnet build
   ```
2. **Lancez la V1.1** :

   ```bash
   dotnet run -- start:v1.1
   ```
3. **Migration (mise à jour)** :

   ```bash
   dotnet run -- migrate:v11
   ```
   
---

#### Exemples d'utilisation

```bash
# Créer trois sauvegardes
dotnet run -- create "Documents" "C:\Users\User\Documents" "D:\Backup\Documents" "Full"
dotnet run -- create "Images" "C:\Users\User\Pictures" "D:\Backup\Pictures" "Full"
dotnet run -- create "Projets" "C:\Users\User\Projects" "D:\Backup\Projects" "Differential"

# Lister les sauvegardes
dotnet run -- list

# Mettre à jour une sauvegarde
dotnet run -- update "Documents" "C:\Users\User\Documents" "E:\Backup\Documents" "Full"

# Exécuter plusieurs sauvegardes
dotnet run -- "1-2"

# Afficher les logs
dotnet run -- logs

# Supprimer une sauvegarde
dotnet run -- delete "Images"
```

#### Notes
- Si vous exécutez l'application sans arguments, le mode interactif sera lancé
- Le nombre maximum de sauvegardes est limité à 5
- Les logs sont enregistrés dans le dossier "Logs" à la racine de l'application, sauf si la variable d'environnement `EASYSAVE_LOG_DIR` est définie
- Les états de sauvegarde sont enregistrés dans le dossier "State" à la racine de l'application, sauf si la variable d'environnement `EASYSAVE_STATE_DIR` est définie

---

## Support et contribution

Pour toute aide ou contribution :

* Ouvrez une issue ou proposez une PR sur [GitHub](https://github.com/EzZarGG/MLBB)
* Rejoignez notre forum de discussion

*Merci davoir choisi EasySave !*
