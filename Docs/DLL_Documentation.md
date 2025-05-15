# EasySaveLogging DLL Documentation

## English Version

### Overview

The EasySaveLogging DLL provides a robust logging solution for the EasySave backup application. It implements a thread-safe singleton logger that records backup operations and administrative actions to a file. The logger supports two file formats:

- **JSON format** (default format)
- **XML format**

If no format is explicitly selected, the logger will use JSON as the default format.

### Technical Specifications

- **Framework**: .NET 8.0
- **Language**: C#
- **Project Type**: Class Library
- **Dependency**: System.Text.Json (included in .NET 8.0)

### Core Components

#### Logger Class

The `Logger` class is the main component of the EasySaveLogging DLL. It is implemented as a thread-safe singleton to ensure consistent logging across multiple threads.

##### Key Features

- **Singleton Pattern**: Ensures a single logger instance throughout the application
- **Thread Safety**: Uses locking mechanism to prevent concurrent writes
- **Multiple Format Support**: Supports both JSON (default) and XML formats
- **Multiple Log Types**: Supports both operation logs and administrative logs

##### Public Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `GetInstance()` | None | Returns the singleton instance of the Logger |
| `SetLogFilePath(string path)` | path: The file path for the log file | Sets the location of the log file and creates necessary directories |
| `SetLogFormat(string format)` | format: The format for logs ("JSON" or "XML") | Sets the format for log files (JSON is default if not specified) |
| `CreateLog(...)` | See detailed parameters below | Records a file transfer operation |
| `LogAdminAction(...)` | See detailed parameters below | Records an administrative action |
| `DisplayLogs()` | None | Outputs the contents of the log file to the console |

###### CreateLog Method Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| backupName | string | Name of the backup job |
| transferTime | TimeSpan | Duration of the file transfer |
| fileSize | long | Size of the transferred file in bytes |
| date | DateTime | Timestamp of the operation |
| sourcePath | string | Source path of the file |
| targetPath | string | Destination path of the file |
| logType | string | Type of log (e.g., "INFO", "ERROR") |

###### LogAdminAction Method Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| backupName | string | Name of the backup job (can be empty) |
| actionType | string | Type of administrative action |
| message | string | Description of the action |

#### LogEntry Class

The `LogEntry` class is an internal class used to structure the log data before serialization to JSON.

##### Properties

| Property | Type | Description |
|----------|------|-------------|
| Timestamp | DateTime | When the log entry was created |
| BackupName | string | Name of the backup job |
| SourcePath | string | Source path of the file |
| TargetPath | string | Destination path of the file |
| FileSize | long | Size of the file in bytes |
| TransferTime | long | Duration of transfer in milliseconds |
| Message | string | Description or error message |
| LogType | string | Type of log entry (e.g., "INFO", "ERROR") |
| ActionType | string | Type of action (e.g., "FILE_TRANSFER") |

### Log File Format

The logger supports two file formats:

#### JSON Format (Default)

When using the default JSON format, the log file is stored as a JSON array of log entries. Each entry contains the fields described in the LogEntry class above.

Example log file content (JSON):
```json
[
  {
    "timestamp": "2025-05-15T10:30:45.123Z",
    "backupName": "Daily Backup",
    "sourcePath": "C:\\Documents",
    "targetPath": "D:\\Backups\\Documents",
    "fileSize": 1024,
    "transferTime": 150,
    "message": "File transferred",
    "logType": "INFO",
    "actionType": "FILE_TRANSFER"
  },
  {
    "timestamp": "2025-05-15T10:35:12.456Z",
    "backupName": "Daily Backup",
    "sourcePath": "",
    "targetPath": "",
    "fileSize": 0,
    "transferTime": 0,
    "message": "Backup completed successfully",
    "logType": "INFO",
    "actionType": "BACKUP_COMPLETE"
  }
]
```

#### XML Format

When using the XML format, the log file is stored as an XML document with a root `<Logs>` element containing multiple `<LogEntry>` elements.

Example log file content (XML):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Logs>
  <LogEntry>
    <Timestamp>2025-05-15T10:30:45.123Z</Timestamp>
    <BackupName>Daily Backup</BackupName>
    <SourcePath>C:\Documents</SourcePath>
    <TargetPath>D:\Backups\Documents</TargetPath>
    <FileSize>1024</FileSize>
    <TransferTime>150</TransferTime>
    <Message>File transferred</Message>
    <LogType>INFO</LogType>
    <ActionType>FILE_TRANSFER</ActionType>
  </LogEntry>
  <LogEntry>
    <Timestamp>2025-05-15T10:35:12.456Z</Timestamp>
    <BackupName>Daily Backup</BackupName>
    <SourcePath></SourcePath>
    <TargetPath></TargetPath>
    <FileSize>0</FileSize>
    <TransferTime>0</TransferTime>
    <Message>Backup completed successfully</Message>
    <LogType>INFO</LogType>
    <ActionType>BACKUP_COMPLETE</ActionType>
  </LogEntry>
</Logs>
```

### Usage Examples

#### Initializing the Logger

```csharp
// Get the logger instance
var logger = EasySaveLogging.Logger.GetInstance();

// Set the log file path
logger.SetLogFilePath("C:\\Logs\\EasySave\\backup_log.json");

// Optional: Set the log format (JSON is default if not specified)
// For JSON format:
logger.SetLogFormat("JSON");
// OR for XML format:
// logger.SetLogFormat("XML");
```

#### Logging a File Transfer

```csharp
// Log a successful file transfer
logger.CreateLog(
    backupName: "Daily Backup",
    transferTime: TimeSpan.FromMilliseconds(150),
    fileSize: 1024,
    date: DateTime.Now,
    sourcePath: "C:\\Documents\\file.txt",
    targetPath: "D:\\Backups\\Documents\\file.txt",
    logType: "INFO"
);

// Log a failed file transfer
logger.CreateLog(
    backupName: "Daily Backup",
    transferTime: TimeSpan.FromMilliseconds(50),
    fileSize: 1024,
    date: DateTime.Now,
    sourcePath: "C:\\Documents\\error.txt",
    targetPath: "D:\\Backups\\Documents\\error.txt",
    logType: "ERROR"
);
```

#### Logging Administrative Actions

```csharp
// Log the start of a backup job
logger.LogAdminAction(
    backupName: "Daily Backup",
    actionType: "BACKUP_START",
    message: "Starting daily backup operation"
);

// Log configuration changes
logger.LogAdminAction(
    backupName: null,
    actionType: "CONFIG_CHANGE",
    message: "Changed target directory to D:\\NewBackups"
);
```

#### Displaying Logs

```csharp
// Output the logs to console
logger.DisplayLogs();
```

### Thread Safety

The logger implements thread safety through the use of a lock mechanism on the singleton instance. This ensures that multiple threads can safely write to the log file without data corruption.

### Best Practices

1. **Initialize Early**: Set the log file path at application startup
2. **Regular Monitoring**: Implement a log rotation strategy for long-running applications
3. **Error Handling**: Implement try-catch blocks around logging operations
4. **Performance**: Consider logging only essential information during high-throughput operations

### Implementation Notes

- The logger creates the log directory and initializes an empty log structure if the file doesn't exist
- All log entries are appended to the existing log file
- JSON is used as the default format if no format is specified
- When using JSON format, the output is formatted with indentation for better readability
- When switching formats, any existing log file will be converted to the new format

---

## Version Française

### Aperçu

La DLL EasySaveLogging fournit une solution robuste de journalisation pour l'application de sauvegarde EasySave. Elle implémente un logger singleton thread-safe qui enregistre les opérations de sauvegarde et les actions administratives dans un fichier. Le logger prend en charge deux formats de fichier :

- **Format JSON** (format par défaut)
- **Format XML**

Si aucun format n'est explicitement sélectionné, le logger utilisera JSON comme format par défaut.

### Spécifications Techniques

- **Framework**: .NET 8.0
- **Langage**: C#
- **Type de Projet**: Bibliothèque de Classes
- **Dépendance**: System.Text.Json (inclus dans .NET 8.0)

### Composants Principaux

#### Classe Logger

La classe `Logger` est le composant principal de la DLL EasySaveLogging. Elle est implémentée comme un singleton thread-safe pour assurer une journalisation cohérente à travers plusieurs threads.

##### Fonctionnalités Clés

- **Patron Singleton**: Garantit une seule instance du logger dans toute l'application
- **Sécurité Thread**: Utilise un mécanisme de verrouillage pour empêcher les écritures concurrentes
- **Support de Formats Multiples**: Prend en charge les formats JSON (par défaut) et XML
- **Types de Journaux Multiples**: Prend en charge à la fois les journaux d'opération et les journaux administratifs

##### Méthodes Publiques

| Méthode | Paramètres | Description |
|---------|------------|-------------|
| `GetInstance()` | Aucun | Renvoie l'instance singleton du Logger |
| `SetLogFilePath(string path)` | path: Le chemin du fichier journal | Définit l'emplacement du fichier journal et crée les répertoires nécessaires |
| `SetLogFormat(string format)` | format: Le format pour les journaux ("JSON" ou "XML") | Définit le format pour les fichiers journaux (JSON est le défaut si non spécifié) |
| `CreateLog(...)` | Voir les paramètres détaillés ci-dessous | Enregistre une opération de transfert de fichier |
| `LogAdminAction(...)` | Voir les paramètres détaillés ci-dessous | Enregistre une action administrative |
| `DisplayLogs()` | Aucun | Affiche le contenu du fichier journal dans la console |

###### Paramètres de la Méthode CreateLog

| Paramètre | Type | Description |
|-----------|------|-------------|
| backupName | string | Nom de la tâche de sauvegarde |
| transferTime | TimeSpan | Durée du transfert de fichier |
| fileSize | long | Taille du fichier transféré en octets |
| date | DateTime | Horodatage de l'opération |
| sourcePath | string | Chemin source du fichier |
| targetPath | string | Chemin de destination du fichier |
| logType | string | Type de journal (ex: "INFO", "ERROR") |

###### Paramètres de la Méthode LogAdminAction

| Paramètre | Type | Description |
|-----------|------|-------------|
| backupName | string | Nom de la tâche de sauvegarde (peut être vide) |
| actionType | string | Type d'action administrative |
| message | string | Description de l'action |

#### Classe LogEntry

La classe `LogEntry` est une classe interne utilisée pour structurer les données de journal avant leur sérialisation en JSON.

##### Propriétés

| Propriété | Type | Description |
|-----------|------|-------------|
| Timestamp | DateTime | Quand l'entrée de journal a été créée |
| BackupName | string | Nom de la tâche de sauvegarde |
| SourcePath | string | Chemin source du fichier |
| TargetPath | string | Chemin de destination du fichier |
| FileSize | long | Taille du fichier en octets |
| TransferTime | long | Durée du transfert en millisecondes |
| Message | string | Description ou message d'erreur |
| LogType | string | Type d'entrée de journal (ex: "INFO", "ERROR") |
| ActionType | string | Type d'action (ex: "FILE_TRANSFER") |

### Format du Fichier Journal

Le logger prend en charge deux formats de fichier :

#### Format JSON (Par Défaut)

Lorsque vous utilisez le format JSON par défaut, le fichier journal est stocké sous forme d'un tableau JSON d'entrées de journal. Chaque entrée contient les champs décrits dans la classe LogEntry ci-dessus.

Exemple de contenu de fichier journal (JSON) :
```json
[
  {
    "timestamp": "2025-05-15T10:30:45.123Z",
    "backupName": "Sauvegarde Quotidienne",
    "sourcePath": "C:\\Documents",
    "targetPath": "D:\\Backups\\Documents",
    "fileSize": 1024,
    "transferTime": 150,
    "message": "Fichier transféré",
    "logType": "INFO",
    "actionType": "FILE_TRANSFER"
  },
  {
    "timestamp": "2025-05-15T10:35:12.456Z",
    "backupName": "Sauvegarde Quotidienne",
    "sourcePath": "",
    "targetPath": "",
    "fileSize": 0,
    "transferTime": 0,
    "message": "Sauvegarde terminée avec succès",
    "logType": "INFO",
    "actionType": "BACKUP_COMPLETE"
  }
]
```

#### Format XML

Lorsque vous utilisez le format XML, le fichier journal est stocké sous forme d'un document XML avec un élément racine `<Logs>` contenant plusieurs éléments `<LogEntry>`.

Exemple de contenu de fichier journal (XML) :
```xml
<?xml version="1.0" encoding="utf-8"?>
<Logs>
  <LogEntry>
    <Timestamp>2025-05-15T10:30:45.123Z</Timestamp>
    <BackupName>Sauvegarde Quotidienne</BackupName>
    <SourcePath>C:\Documents</SourcePath>
    <TargetPath>D:\Backups\Documents</TargetPath>
    <FileSize>1024</FileSize>
    <TransferTime>150</TransferTime>
    <Message>Fichier transféré</Message>
    <LogType>INFO</LogType>
    <ActionType>FILE_TRANSFER</ActionType>
  </LogEntry>
  <LogEntry>
    <Timestamp>2025-05-15T10:35:12.456Z</Timestamp>
    <BackupName>Sauvegarde Quotidienne</BackupName>
    <SourcePath></SourcePath>
    <TargetPath></TargetPath>
    <FileSize>0</FileSize>
    <TransferTime>0</TransferTime>
    <Message>Sauvegarde terminée avec succès</Message>
    <LogType>INFO</LogType>
    <ActionType>BACKUP_COMPLETE</ActionType>
  </LogEntry>
</Logs>
```

### Exemples d'Utilisation

#### Initialisation du Logger

```csharp
// Obtenir l'instance du logger
var logger = EasySaveLogging.Logger.GetInstance();

// Définir le chemin du fichier journal
logger.SetLogFilePath("C:\\Logs\\EasySave\\backup_log.json");

// Optionnel : Définir le format du journal (JSON est le format par défaut si non spécifié)
// Pour le format JSON :
logger.SetLogFormat("JSON");
// OU pour le format XML :
// logger.SetLogFormat("XML");
```

#### Journalisation d'un Transfert de Fichier

```csharp
// Journaliser un transfert de fichier réussi
logger.CreateLog(
    backupName: "Sauvegarde Quotidienne",
    transferTime: TimeSpan.FromMilliseconds(150),
    fileSize: 1024,
    date: DateTime.Now,
    sourcePath: "C:\\Documents\\fichier.txt",
    targetPath: "D:\\Backups\\Documents\\fichier.txt",
    logType: "INFO"
);

// Journaliser un transfert de fichier échoué
logger.CreateLog(
    backupName: "Sauvegarde Quotidienne",
    transferTime: TimeSpan.FromMilliseconds(50),
    fileSize: 1024,
    date: DateTime.Now,
    sourcePath: "C:\\Documents\\erreur.txt",
    targetPath: "D:\\Backups\\Documents\\erreur.txt",
    logType: "ERROR"
);
```

#### Journalisation d'Actions Administratives

```csharp
// Journaliser le début d'une tâche de sauvegarde
logger.LogAdminAction(
    backupName: "Sauvegarde Quotidienne",
    actionType: "BACKUP_START",
    message: "Démarrage de l'opération de sauvegarde quotidienne"
);

// Journaliser les changements de configuration
logger.LogAdminAction(
    backupName: null,
    actionType: "CONFIG_CHANGE",
    message: "Changement du répertoire cible vers D:\\NouvellesSauvegardes"
);
```

#### Affichage des Journaux

```csharp
// Afficher les journaux dans la console
logger.DisplayLogs();
```

### Sécurité Thread

Le logger implémente la sécurité thread grâce à l'utilisation d'un mécanisme de verrouillage sur l'instance singleton. Cela garantit que plusieurs threads peuvent écrire en toute sécurité dans le fichier journal sans corruption de données.

### Bonnes Pratiques

1. **Initialisation Précoce**: Définir le chemin du fichier journal au démarrage de l'application
2. **Surveillance Régulière**: Implémenter une stratégie de rotation des journaux pour les applications à longue durée d'exécution
3. **Gestion des Erreurs**: Implémenter des blocs try-catch autour des opérations de journalisation
4. **Performance**: Envisager de journaliser uniquement les informations essentielles lors d'opérations à haut débit

### Notes d'Implémentation

- Le logger crée le répertoire du journal et initialise une structure de journal vide si le fichier n'existe pas
- Toutes les entrées de journal sont ajoutées au fichier journal existant
- JSON est utilisé comme format par défaut si aucun format n'est spécifié
- Lors de l'utilisation du format JSON, la sortie est formatée avec indentation pour une meilleure lisibilité
- Lors du changement de format, tout fichier journal existant sera converti au nouveau format