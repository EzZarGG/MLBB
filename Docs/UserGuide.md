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

## Introduction {#introduction}

EasySave is a cross-platform backup solution. This guide covers both V1 (the original release) and V1.1 (the incremental update), detailing installation, core workflows, troubleshooting, and new improvements.

---

## Installation {#installation}

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

## Getting Started (V1) {#getting-started-v1}

When launching for the first time, the main interactive menu presents:

* Create a new backup job
* View existing jobs
* Execute a job
* Modify a job
* Delete a job
* Exit

Use numeric choices to navigate.

---

## Creating and Managing Backup Jobs (V1) {#jobs-v1}

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

## Executing Backups (V1) {#executing-v1}

1. Select **Execute a backup job**
2. Choose one or multiple jobs (e.g., `1-3` or `1;2;4`)
3. View live progress output
4. On completion, see summary:

   * Files scanned
   * Files copied
   * Duration

---

## Logs and Troubleshooting (V1) {#logs-v1}

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

## Command-Line Usage (V1) {#cli-v1}

Run with dotnet or exe arguments.

### Help

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

### List

```bash
dotnet run -- list
```

### Execute

```bash
dotnet run -- execute "1-3"
```

### Logs

```bash
dotnet run -- logs
```

Notes:

* Running without args enters interactive mode
* Max 5 backup jobs
* Override log directory via `EASYSAVE_LOG_DIR`
* Override state directory via `EASYSAVE_STATE_DIR`

---

## What's New in V1.1 {#whats-new-v11}

### 8.1 New Features {#new-features-v11}

* **Collaboration Mode**: share and synchronize jobs in real-time
* **WebSocket Notifications**: live alerts on job status
* **Encrypted Archives**: optional AES-256 encryption for backups

### 8.2 Enhancements and Fixes {#enhancements-v11}

* Updated dependencies for security
* Resolved pagination bug in job lists
* Optimized API queries and file enumeration
* Improved error messages and logging detail

---

## Getting Started (V1.1) {#getting-started-v11}

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

## Support & Contribution {#support}

For help or to contribute:

* Open an issue or PR on [GitHub](https://github.com/EzZarGG/MLBB)
* Join our discussion board

---

## Guide de l�utilisateur EasySave V1 & V1.1

### Table des mati�res

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Premiers pas (V1)](#premiers-pas-v1)
4. [Cr�ation et gestion des travaux de sauvegarde (V1)](#jobs-v1)
5. [Ex�cution des sauvegardes (V1)](#executing-v1)
6. [Journaux et d�pannage (V1)](#logs-v1)
7. [Utilisation en ligne de commande (V1)](#cli-v1)
8. [Nouveaut�s de la V1.1](#whats-new-v11)

   * 8.1 [Nouvelles fonctionnalit�s](#new-features-v11)
   * 8.2 [Am�liorations et corrections](#enhancements-v11)
9. [Premiers pas (V1.1)](#getting-started-v11)
10. [Support et contribution](#support)

---

## Introduction {#introduction}

EasySave est une solution de sauvegarde multiplateforme. Ce guide couvre la version initiale V1 et la mise � jour incr�mentale V1.1, d�taillant l�installation, les flux de travail principaux, le d�pannage et les am�liorations apport�es.

---

## Installation {#installation}

1. **Pr�requis** :

   * Syst�me d�exploitation Windows
   * [.NET Core 6, 7 ou 8](https://dotnet.microsoft.com/) install�
2. **Clonage et build** :

   ```bash
   git clone -b dev https://github.com/EzZarGG/MLBB.git
   cd MLBB/Docs
   ```
3. **Ouverture et compilation** :

   * Ouvrez `EasySave.sln` dans Visual Studio
   * Compilez (Ctrl+Shift+B)
4. **Ex�cution** :

   * Allez dans `bin/Debug/net8.0/` (ou le dossier correspondant)
   * Lancez `EasySave.exe`

---

## Premiers pas (V1) {#premiers-pas-v1}

Au premier lancement, le menu interactif principal propose :

* Cr�er un nouveau travail de sauvegarde
* Afficher les travaux existants
* Ex�cuter un travail
* Modifier un travail
* Supprimer un travail
* Quitter

Naviguez � l�aide des num�ros correspondants.

---

## Cr�ation et gestion des travaux de sauvegarde (V1) {#jobs-v1}

### Cr�er un travail

1. Choisissez **Cr�er un nouveau travail de sauvegarde**
2. Saisissez un nom unique
3. Indiquez le **r�pertoire source**
4. Indiquez le **r�pertoire de destination**
5. S�lectionnez le type de sauvegarde :

   * **Compl�te** : copie tous les fichiers � chaque ex�cution
   * **Diff�rentielle** : copie seulement les fichiers modifi�s depuis la derni�re sauvegarde
6. Confirmez pour enregistrer le travail

### Afficher les travaux

Affiche la liste num�rot�e des travaux enregistr�s (nom, source, destination, type).

### Modifier un travail

1. S�lectionnez **Modifier un travail de sauvegarde**
2. Choisissez le num�ro du travail
3. Mettez � jour le source, la destination ou le type
4. Enregistrez les modifications

### Supprimer un travail

1. S�lectionnez **Supprimer un travail de sauvegarde**
2. Choisissez le num�ro du travail � supprimer

---

## Ex�cution des sauvegardes (V1) {#executing-v1}

1. S�lectionnez **Ex�cuter un travail de sauvegarde**
2. Choisissez un ou plusieurs travaux (ex. `1-3` ou `1;2;4`)
3. Suivez la progression en temps r�el
4. � la fin, un r�capitulatif indique :

   * Nombre de fichiers scann�s
   * Nombre de fichiers copi�s
   * Dur�e de l�op�ration

---

## Journaux et d�pannage (V1) {#logs-v1}

### Types de journaux

* **Journaux d��tat** : JSON en temps r�el dans `Logs/status`
* **Journaux d�activit�** : historique en JSON/XML dans `Logs/activity`

Les journaux contiennent : nom du travail, chemins, nombre et taille de fichiers, horodatages, statut de chiffrement.

### Probl�mes courants

* **Acc�s refus�** : v�rifiez les permissions des dossiers
* **Travail introuvable** : confirmez le nom du travail
* **Chemin introuvable** : v�rifiez l�existence des chemins
* **�chec de la sauvegarde** : consultez les journaux et lib�rez de l�espace disque

---

## Utilisation en ligne de commande (V1) {#cli-v1}

Lancez l�application avec des arguments dotnet ou exe.

### Aide

```bash
dotnet run -- help
```

### Cr�er

```bash
dotnet run -- create "Nom" "C:\Src" "D:\Dst" Full
```

### Mettre � jour

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

### Ex�cuter

```bash
dotnet run -- execute "1-3"
```

### Journaux

```bash
dotnet run -- logs
```

**Notes** :

* Sans arguments, le mode interactif se lance
* Maximum 5 travaux de sauvegarde
* Personnalisez le dossier des journaux via `EASYSAVE_LOG_DIR`
* Personnalisez le dossier d��tat via `EASYSAVE_STATE_DIR`

---

## Nouveaut�s de la V1.1 {#whats-new-v11}

### 8.1 Nouvelles fonctionnalit�s {#new-features-v11}

* **Mode collaboration** : partage et synchronisation des travaux en temps r�el
* **Notifications WebSocket** : alertes en direct sur l��tat des travaux
* **Archives chiffr�es** : chiffrement AES-256 optionnel des sauvegardes

### 8.2 Am�liorations et corrections {#enhancements-v11}

* Mise � jour des d�pendances pour la s�curit�
* Correction du bug de pagination dans la liste des travaux
* Optimisation des requ�tes API et de l��num�ration des fichiers
* Messages d�erreur et journaux plus d�taill�s

---

## Premiers pas (V1.1) {#getting-started-v11}

1. **Mettez � jour l�application** :

   ```bash
   git pull && dotnet build
   ```
2. **Lancez la V1.1** :

   ```bash
   dotnet run -- start:v1.1
   ```
3. **Migration (mise � jour)** :

   ```bash
   dotnet run -- migrate:v11
   ```
4. **Utilisez les nouvelles options** :

   * Activez la collaboration dans les param�tres
   * Basculez le chiffrement pour chaque travail
   * Abonnez-vous aux notifications

---

## Support et contribution {#support}

Pour toute aide ou contribution :

* Ouvrez une issue ou proposez une PR sur [GitHub](https://github.com/EzZarGG/MLBB)
* Rejoignez notre forum de discussion

*Merci d�avoir choisi EasySave !*

