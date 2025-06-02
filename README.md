# EasySave

## English

### What is EasySave?
EasySave is a backup software application designed to provide efficient and user-friendly backup solutions. This application allows users to create, manage, and execute backup jobs with features such as real-time backup, differential backup, and complete backup.

### Technologies & Architecture
- **Language**: C#
- **Framework**: .NET Core 8.0
- **Architecture**: Model-View-Controller (MVC)
- **Logging**: Custom JSON and XML logging system

### Purpose
The purpose of EasySave is to offer a reliable and straightforward backup solution that enables users to safeguard their important data through various backup methods. It provides both command-line and interactive interfaces to manage backup operations, making it versatile for different use cases.

### System Requirements
- Windows operating system
- .NET Core 6/7/8

### Installation
1. Clone the repository: `git clone https://github.com/EzZarGG/MLBB.git`
2. Open the solution in Visual Studio
3. Build the solution
4. Run the application from the build directory

### Version-Specific Details

#### v1.0.0
- **Multiple Backup Types**: Create complete or differential backups
- **Job Management**: Create, edit, and delete backup jobs (up to 5 jobs)
- **Real-time Monitoring**: Track backup progress and status
- **Logging System**: Detailed JSON logs of backup operations 
- **User-friendly Interface**: Simple console interface with language options (English/French)
- **Command-line Support**: Execute operations directly from the command line
- **State Management**: Real-time state tracking of backup jobs

#### v1.1.0
- **All features from v1.0.0**
- **Enhanced Logging Options**: Choose between JSON and XML formats for log files
- **Improved Configuration**: Added format selection for logging preferences

#### v2.0.0
- **All features from v1.1.0**
- **Graphical User Interface**: Modern Windows Forms interface
- **Real-time Progress**: Visual progress tracking for backup operations
- **Advanced Encryption**: Integration with CryptoSoft for file encryption
- **Business Software Protection**: Automatic pause during business software usage
- **Enhanced Configuration**: JSON-based configuration with encryption settings
- **Improved Error Handling**: Detailed error reporting and logging
- **Multi-threaded Operations**: Parallel backup execution support

#### v3.0.0

* All features from v2.0.0
* Pause/Resume Support  
  - The user can now pause or resume one or multiple backup tasks at any time (pause takes effect immediately after the current file transfer finishes; resume restarts where it left off)
* Business-Software Detection  
  - If a “business” application is launched, EasySave will immediately stop all ongoing transfers. All backups automatically resume as soon as that business application is closed.
* Priority-File Management  
  - No non-priority file will be backed up as long as there is at least one priority-extension file waiting. (Extensions are declared by the user in a pre-configured list.)
* Large-File Transfer Restriction  
  - To avoid saturating the network, EasySave will not transfer two files whose size is greater than a configurable threshold n KB at the same time. (n KB is adjustable in the configuration.)
* Remote Console (via Sockets)  
  - A remote, graphical monitoring console has been added. A user can now attach to a distant machine and both watch progress in real time and issue Play/Pause/Stop commands over the network.
* CryptoSoft Mono-Instance Support  
  - CryptoSoft is now strictly mono-instance (cannot run two copies on the same PC). EasySave has been updated to detect and handle any errors or collisions caused by that restriction.
* Network-Load Monitoring (OPTIONAL)  
  - If network usage exceeds a configurable threshold, EasySave will automatically reduce its parallel tasks to avoid saturating the link.


### Getting Started
Refer to the User Guide for detailed instructions on how to use 
EasySave v1.0.
Refer to the User Guide V2 for detailed instructions on how to use EasySave v2.0.

---

## Français

### Qu'est-ce qu'EasySave?
EasySave est une application de sauvegarde conçue pour offrir des solutions de sauvegarde efficaces et conviviales. Cette application permet aux utilisateurs de créer, gérer et exécuter des travaux de sauvegarde avec des fonctionnalités telles que la sauvegarde en temps réel, la sauvegarde différentielle et la sauvegarde complète.

### Technologies et Architecture
- **Langage**: C#
- **Framework**: .NET Core 8.0
- **Architecture**: Modèle-Vue-Contrôleur (MVC)
- **Journalisation**: Système de journalisation personnalisé en JSON et XML

### Objectif
L'objectif d'EasySave est d'offrir une solution de sauvegarde fiable et simple qui permet aux utilisateurs de protéger leurs données importantes grâce à diverses méthodes de sauvegarde. Il fournit des interfaces en ligne de commande et interactives pour gérer les opérations de sauvegarde, ce qui le rend polyvalent pour différents cas d'utilisation.

### Configuration requise
- Système d'exploitation Windows
- .NET Core 6/7/8

### Installation
1. Clonez le dépôt : `git clone https://github.com/EzZarGG/MLBB.git`
2. Ouvrez la solution dans Visual Studio
3. Compilez la solution
4. Exécutez l'application à partir du répertoire de compilation

### Détails spécifiques aux versions

#### v1.0.0
- **Types de sauvegarde multiples** : Créez des sauvegardes complètes ou différentielles
- **Gestion des travaux** : Créez, modifiez et supprimez des travaux de sauvegarde (jusqu'à 5 travaux)
- **Suivi en temps réel** : Suivez la progression et l'état des sauvegardes
- **Système de journalisation** : Journaux JSON détaillés des opérations de sauvegarde
- **Interface conviviale** : Interface console simple avec options de langue (anglais/français)
- **Support en ligne de commande** : Exécutez des opérations directement depuis la ligne de commande
- **Gestion d'état** : Suivi en temps réel de l'état des travaux de sauvegarde

#### v1.1.0
- **Toutes les fonctionnalités de v1.0.0**
- **Options de journalisation améliorées** : Choisissez entre les formats JSON et XML pour les fichiers journaux
- **Configuration améliorée** : Ajout de la sélection du format pour les préférences de journalisation

#### v2.0.0
- **Toutes les fonctionnalités de v1.1.0**
- **Interface Graphique** : Interface moderne Windows Forms
- **Progression en Temps Réel** : Suivi visuel des opérations de sauvegarde
- **Chiffrement Avancé** : Intégration avec CryptoSoft pour le chiffrement des fichiers
- **Protection des Logiciels Métier** : Pause automatique pendant l'utilisation des logiciels métier
- **Configuration Améliorée** : Configuration basée sur JSON avec paramètres de chiffrement
- **Gestion des Erreurs Améliorée** : Rapports d'erreurs et journalisation détaillés
- **Opérations Multi-threads** : Support de l'exécution parallèle des sauvegardes

#### v3.0.0

* Toutes les fonctionnalités de la v2.0.0
* Mise en pause/Reprise  
  - L’utilisateur peut désormais mettre en pause ou reprendre un ou plusieurs travaux de sauvegarde à tout moment (la pause s’effectue dès la fin du transfert en cours ; la reprise repart d’où elle s’était arrêtée)
* Détection de Logiciel Métier  
  - Si un logiciel métier est lancé, EasySave interrompt immédiatement tous les transferts en cours. Les sauvegardes redémarrent automatiquement dès que le logiciel métier est fermé.
* Gestion des Fichiers Prioritaires  
  - Aucune sauvegarde de fichier non prioritaire ne sera lancée tant qu’une extension prioritaire est en attente. (Les extensions sont déclarées par l’utilisateur dans une liste prédéfinie.)
* Restriction de Sauvegardes de Gros Fichiers  
  - Pour ne pas saturer la bande passante, EasySave n’autorise pas le transfert simultané de deux fichiers dont la taille est supérieure à une valeur n Ko paramétrable. (n Ko est configurable dans les paramètres.)
* Console à Distance (via Sockets)  
  - Une console graphique distante a été ajoutée. Un utilisateur peut maintenant se connecter à un poste éloigné pour suivre en temps réel la progression et envoyer des commandes Play/Pause/Stop par réseau.
* Support Mono-Instance pour CryptoSoft  
  - CryptoSoft est désormais strictement mono-instance (ne peut pas s’exécuter deux fois sur une même machine). EasySave gère les éventuels conflits ou erreurs liées à cette contrainte.
* Surveillance de la Charge Réseau (OPTIONNEL)  
  - Si la charge réseau dépasse un seuil paramétrable, EasySave réduit automatiquement le nombre de tâches parallèles pour éviter de saturer la connexion.


### Démarrage
Consultez le Guide d'Utilisateur pour des instructions détaillées sur l'utilisation d'EasySaveV2.
