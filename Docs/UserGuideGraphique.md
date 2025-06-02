# EasySave V2.0 & V3.0 User Guide

## English

### Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Main Interface](#main-interface)
5. [Creating Backup Jobs](#creating-backup-jobs)
6. [Managing Backup Jobs](#managing-backup-jobs)
7. [Executing Backups](#executing-backups)
8. [Encryption Settings](#encryption-settings)
9. [Business Software Protection](#business-software-protection)
10. [Logs and Monitoring](#logs-and-monitoring)
11. [Troubleshooting](#troubleshooting)

### Introduction
EasySave V2.0 is a modern backup solution featuring a graphical user interface. This guide will help you understand and use all the features of the application effectively.

### Installation
1. **Prerequisites**:
   - Windows operating system
   - .NET 8.0 or higher
   - Visual Studio 2022 (for development)

2. **Installation Steps**:
   ```bash
   git clone https://github.com/EzZarGG/MLBB.git
   cd MLBB
   dotnet build
   ```

### Getting Started
1. Launch EasySaveV2.0.exe
2. The main window will appear with the following sections:
   - Backup Jobs List
   - Job Details
   - Progress Monitor
   - Log Viewer

### Main Interface
The main window is divided into several sections:

1. **Menu Bar**:
   - File: New, Open, Save, Exit
   - Edit: Create, Modify, Delete jobs
   - View: Show/Hide panels
   - Tools: Settings, Logs
   - Help: Documentation, About

2. **Toolbar**:
   - Quick access to common operations
   - Start/Stop backup buttons
   - Settings access

3. **Main Panel**:
   - Left: Backup jobs list
   - Center: Job details and progress
   - Right: Log viewer

### Creating Backup Jobs
1. Click "Create" in the toolbar or use File > New
2. Fill in the job details:
   - Name: Unique identifier
   - Source: Source directory path
   - Target: Destination directory path
   - Type: Full or Differential
   - Priority: High, Medium, Low
3. Click "Save" to create the job

### Managing Backup Jobs
1. **View Jobs**:
   - All jobs are listed in the main panel
   - Click a job to view details

2. **Modify Jobs**:
   - Select a job
   - Click "Modify" or double-click
   - Update settings
   - Save changes

3. **Delete Jobs**:
   - Select a job
   - Click "Delete" or press Delete key
   - Confirm deletion

### Executing Backups
1. **Single Job**:
   - Select a job
   - Click "Start" or use the play button
   - Monitor progress in real-time

2. **Multiple Jobs**:
   - Select multiple jobs (Ctrl+Click)
   - Click "Start All"
   - Monitor progress for each job

3. **Pause/Resume**:
   - Click "Pause" during execution
   - Click "Resume" to continue

### Encryption Settings
1. **Configure Encryption**:
   - Open Settings (Tools > Settings)
   - Navigate to Encryption tab
   - Set encryption key
   - Add file extensions to encrypt

2. **CryptoSoft Integration**:
   - Set CryptoSoft path in settings
   - Configure encryption options
   - Test encryption

### Business Software Protection
1. **Configure Protected Software**:
   - Open Settings
   - Navigate to Business Software tab
   - Add software to protect
   - Set protection rules

2. **Protection Behavior**:
   - Automatic pause when protected software is running
   - Resume when software is closed
   - Configurable delay before resume

### Logs and Monitoring
1. **Real-time Monitoring**:
   - Progress bars for each job
   - File transfer statistics
   - Error notifications

2. **Log Viewer**:
   - Access through Tools > Logs
   - Filter by job, date, type
   - Export logs in JSON/XML

### Troubleshooting
Common issues and solutions:

1. **Access Denied**:
   - Check folder permissions
   - Run as administrator if needed

2. **Encryption Errors**:
   - Verify CryptoSoft path
   - Check encryption key
   - Validate file permissions

3. **Performance Issues**:
   - Check disk space
   - Verify network connection
   - Adjust buffer size in settings

   #### v3.0.0

##### What's New (EN)

- **Pause / Resume Support**  
  • The user can now pause or resume one or multiple backup tasks at any time.  
  • “Pause” takes effect immediately after the current file transfer finishes; “Resume” restarts that task where it left off.  

- **Business-Application Detection**  
  • If EasySave detects that a “business” (métier) application is running, it will immediately stop all ongoing backups.  
  • All paused backups automatically resume as soon as that application is closed.  

- **Priority-File Management**  
  • No non-priority file will be backed up as long as at least one priority-extension file is waiting.  
  • Extensions are declared by the user in a predefined list (see “Configuration → Priority Extensions”).  

- **Large-File Transfer Restriction**  
  • To avoid saturating the network, EasySave will not transfer simultaneously two files whose size is greater than a configurable threshold (n KB).  
  • “n KB” is adjustable under “Configuration → Network / File Settings.”  

- **Remote Console (via Sockets)**  
  • A new graphical console allows a user to connect from a remote machine and monitor all backups in real time.  
  • From that remote console, the user can issue Play / Pause / Stop commands over the network.  

- **CryptoSoft Mono-Instance Support**  
  • CryptoSoft must now run as a single instance (cannot execute two copies on the same PC).  
  • EasySave detects any conflict (e.g., CryptoSoft already running) and displays an error before attempting encryption.  

- **Network-Load Monitoring (OPTIONAL)**  
  • If network usage exceeds a configurable threshold, EasySave will automatically reduce its parallel tasks.  
  • This feature is off by default—enable under “Configuration → Performance → Network Load.”


---

## Français

### Table des Matières
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Premiers Pas](#premiers-pas)
4. [Interface Principale](#interface-principale)
5. [Création de Travaux de Sauvegarde](#creation-travaux)
6. [Gestion des Travaux](#gestion-travaux)
7. [Exécution des Sauvegardes](#execution-sauvegardes)
8. [Paramètres de Chiffrement](#parametres-chiffrement)
9. [Protection des Logiciels Métier](#protection-logiciels)
10. [Journaux et Surveillance](#journaux-surveillance)
11. [Dépannage](#depannage)

### Introduction
EasySave V2.0 est une solution de sauvegarde moderne dotée d'une interface graphique. Ce guide vous aidera à comprendre et utiliser efficacement toutes les fonctionnalités de l'application.

### Installation
1. **Prérequis** :
   - Système d'exploitation Windows
   - .NET 8.0 ou supérieur
   - Visual Studio 2022 (pour le développement)

2. **Étapes d'installation** :
   ```bash
   git clone https://github.com/EzZarGG/MLBB.git
   cd MLBB
   dotnet build
   ```

### Premiers Pas
1. Lancez EasySaveV2.0.exe
2. La fenêtre principale apparaîtra avec les sections suivantes :
   - Liste des travaux de sauvegarde
   - Détails du travail
   - Moniteur de progression
   - Visualiseur de journaux

### Interface Principale
La fenêtre principale est divisée en plusieurs sections :

1. **Barre de Menu** :
   - Fichier : Nouveau, Ouvrir, Enregistrer, Quitter
   - Édition : Créer, Modifier, Supprimer des travaux
   - Affichage : Afficher/Masquer les panneaux
   - Outils : Paramètres, Journaux
   - Aide : Documentation, À propos

2. **Barre d'Outils** :
   - Accès rapide aux opérations courantes
   - Boutons Démarrer/Arrêter
   - Accès aux paramètres

3. **Panneau Principal** :
   - Gauche : Liste des travaux
   - Centre : Détails et progression
   - Droite : Visualiseur de journaux

### Création de Travaux de Sauvegarde
1. Cliquez sur "Créer" dans la barre d'outils ou utilisez Fichier > Nouveau
2. Remplissez les détails du travail :
   - Nom : Identifiant unique
   - Source : Chemin du répertoire source
   - Cible : Chemin du répertoire de destination
   - Type : Complète ou Différentielle
   - Priorité : Haute, Moyenne, Basse
3. Cliquez sur "Enregistrer" pour créer le travail

### Gestion des Travaux
1. **Afficher les Travaux** :
   - Tous les travaux sont listés dans le panneau principal
   - Cliquez sur un travail pour voir les détails

2. **Modifier les Travaux** :
   - Sélectionnez un travail
   - Cliquez sur "Modifier" ou double-cliquez
   - Mettez à jour les paramètres
   - Enregistrez les modifications

3. **Supprimer les Travaux** :
   - Sélectionnez un travail
   - Cliquez sur "Supprimer" ou appuyez sur la touche Suppr
   - Confirmez la suppression

### Exécution des Sauvegardes
1. **Travail Unique** :
   - Sélectionnez un travail
   - Cliquez sur "Démarrer" ou utilisez le bouton lecture
   - Surveillez la progression en temps réel

2. **Travaux Multiples** :
   - Sélectionnez plusieurs travaux (Ctrl+Clic)
   - Cliquez sur "Tout Démarrer"
   - Surveillez la progression de chaque travail

3. **Pause/Reprise** :
   - Cliquez sur "Pause" pendant l'exécution
   - Cliquez sur "Reprendre" pour continuer

### Paramètres de Chiffrement
1. **Configurer le Chiffrement** :
   - Ouvrez les Paramètres (Outils > Paramètres)
   - Accédez à l'onglet Chiffrement
   - Définissez la clé de chiffrement
   - Ajoutez les extensions de fichiers à chiffrer

2. **Intégration CryptoSoft** :
   - Définissez le chemin de CryptoSoft dans les paramètres
   - Configurez les options de chiffrement
   - Testez le chiffrement

### Protection des Logiciels Métier
1. **Configurer les Logiciels Protégés** :
   - Ouvrez les Paramètres
   - Accédez à l'onglet Logiciels Métier
   - Ajoutez les logiciels à protéger
   - Définissez les règles de protection

2. **Comportement de Protection** :
   - Pause automatique lors de l'exécution des logiciels protégés
   - Reprise à la fermeture du logiciel
   - Délai configurable avant la reprise

### Journaux et Surveillance
1. **Surveillance en Temps Réel** :
   - Barres de progression pour chaque travail
   - Statistiques de transfert de fichiers
   - Notifications d'erreurs

2. **Visualiseur de Journaux** :
   - Accès via Outils > Journaux
   - Filtrage par travail, date, type
   - Export des journaux en JSON/XML

### Dépannage
Problèmes courants et solutions :

1. **Accès Refusé** :
   - Vérifiez les permissions des dossiers
   - Exécutez en tant qu'administrateur si nécessaire

2. **Erreurs de Chiffrement** :
   - Vérifiez le chemin de CryptoSoft
   - Vérifiez la clé de chiffrement
   - Validez les permissions des fichiers

3. **Problèmes de Performance** :
   - Vérifiez l'espace disque
   - Vérifiez la connexion réseau
   - Ajustez la taille du tampon dans les paramètres


   ##### Nouveautés (FR)

- **Mise en pause / Reprise**  
  • L’utilisateur peut désormais mettre en pause ou reprendre un ou plusieurs travaux de sauvegarde à tout moment.  
  • La “Pause” s’effectue immédiatement après la fin du transfert de fichier en cours ; la “Reprise” repart d’où elle s’était arrêtée.  

- **Détection de logiciel métier**  
  • Si EasySave détecte qu’un logiciel métier est en cours d’exécution, il interrompt immédiatement tous les transferts en cours.  
  • Toutes les sauvegardes en pause redémarrent automatiquement dès que ce logiciel métier est fermé.  

- **Gestion des fichiers prioritaires**  
  • Aucune sauvegarde d’un fichier non prioritaire ne peut commencer tant qu’au moins un fichier d’extension prioritaire est en attente.  
  • Les extensions prioritaires sont déclarées par l’utilisateur dans une liste prédéfinie (voir « Configuration → Extensions prioritaires »).  

- **Restriction de transfert de gros fichiers**  
  • Pour ne pas saturer la bande passante, EasySave n’autorise pas le transfert simultané de deux fichiers dont la taille est supérieure à un seuil paramétrable (n Ko).  
  • “n Ko” est configurable dans « Configuration → Paramètres Réseau / Fichiers ».  

- **Console à distance (via Sockets)**  
  • Une nouvelle console graphique permet à l’utilisateur de se connecter depuis un poste distant et de suivre toutes les sauvegardes en temps réel.  
  • Depuis cette console, l’utilisateur peut envoyer des commandes Play / Pause / Stop par réseau.  

- **Support Mono-Instance pour CryptoSoft**  
  • CryptoSoft doit désormais s’exécuter en mono-instance (ne peut pas tourner deux fois sur la même machine).  
  • EasySave détecte tout conflit (par exemple, CryptoSoft déjà ouvert) et affiche un message d’erreur avant de lancer le chiffrement.  

- **Surveillance de la charge réseau (OPTIONNEL)**  
  • Si l’utilisation réseau dépasse un seuil paramétrable, EasySave réduit automatiquement le nombre de tâches parallèles.  
  • Cette option est désactivée par défaut : pour l’activer, aller dans « Configuration → Performance → Charge Réseau ».
