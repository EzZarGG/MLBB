# EasySaveLogging DLL Documentation

## English Version

### Overview

The EasySaveLogging DLL provides a robust logging solution for the EasySave V2.0 backup application. It implements a thread-safe singleton logger that records backup operations, encryption activities, and business software protection events to files in either JSON or XML format.

### Technical Specifications

- **Framework**: .NET 8.0
- **Language**: C#
- **Project Type**: Class Library
- **Dependencies**: 
  - System.Text.Json (included in .NET 8.0)
  - System.Xml (included in .NET 8.0)

### Core Components

#### Logger Class

The `Logger` class is the main component of the EasySaveLogging DLL. It is implemented as a thread-safe singleton to ensure consistent logging across multiple threads.

##### Key Features

- **Singleton Pattern**: Ensures a single logger instance throughout the application
- **Thread Safety**: Uses locking mechanism to prevent concurrent writes
- **Multiple Format Support**: Supports both JSON (default) and XML formats
- **Multiple Log Types**: Supports operation logs, encryption logs, and business software protection logs

##### Public Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `GetInstance()` | None | Returns the singleton instance of the Logger |
| `SetLogFilePath(string path)` | path: The file path for the log file | Sets the location of the log file and creates necessary directories |
| `SetLogFormat(LogFormat format)` | format: The format for logs (JSON or XML) | Sets the format for log files (JSON is default if not specified) |
| `LogBackupOperation(BackupOperation operation)` | operation: The backup operation details | Records a backup operation |
| `LogEncryptionOperation(EncryptionOperation operation)` | operation: The encryption operation details | Records an encryption operation |
| `LogBusinessSoftwareEvent(BusinessSoftwareEvent event)` | event: The business software event details | Records a business software protection event |
| `LogError(string message, Exception ex)` | message: Error message, ex: Exception | Records an error with stack trace |
| `GetLogs(DateTime startDate, DateTime endDate)` | startDate, endDate: Date range | Retrieves logs within the specified date range |

##### LogEntry Class

The `LogEntry` class structures the log data before serialization.

| Property | Type | Description |
|----------|------|-------------|
| Timestamp | DateTime | When the log entry was created |
| JobName | string | Name of the backup job |
| Operation | string | Type of operation performed |
| Message | string | Description or error message |
| Level | LogLevel | Log level (Info, Warning, Error) |
| Details | Dictionary<string, object> | Additional operation-specific details |

### Log File Format

#### JSON Format (Default)

```json
{
  "logs": [
    {
      "timestamp": "2024-03-15T10:30:45.123Z",
      "jobName": "Daily Backup",
      "operation": "BACKUP_START",
      "message": "Starting backup operation",
      "level": "Info",
      "details": {
        "sourcePath": "C:\\Documents",
        "targetPath": "D:\\Backup",
        "type": "Full"
      }
    },
    {
      "timestamp": "2024-03-15T10:35:12.456Z",
      "jobName": "Daily Backup",
      "operation": "ENCRYPTION",
      "message": "File encrypted successfully",
      "level": "Info",
      "details": {
        "filePath": "D:\\Backup\\document.docx",
        "algorithm": "AES-256"
      }
    }
  ]
}
```

#### XML Format

```xml
<?xml version="1.0" encoding="utf-8"?>
<Logs>
  <LogEntry>
    <Timestamp>2024-03-15T10:30:45.123Z</Timestamp>
    <JobName>Daily Backup</JobName>
    <Operation>BACKUP_START</Operation>
    <Message>Starting backup operation</Message>
    <Level>Info</Level>
    <Details>
      <SourcePath>C:\Documents</SourcePath>
      <TargetPath>D:\Backup</TargetPath>
      <Type>Full</Type>
    </Details>
  </LogEntry>
  <LogEntry>
    <Timestamp>2024-03-15T10:35:12.456Z</Timestamp>
    <JobName>Daily Backup</JobName>
    <Operation>ENCRYPTION</Operation>
    <Message>File encrypted successfully</Message>
    <Level>Info</Level>
    <Details>
      <FilePath>D:\Backup\document.docx</FilePath>
      <Algorithm>AES-256</Algorithm>
    </Details>
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

// Set the log format (JSON is default)
logger.SetLogFormat(LogFormat.JSON);
```

#### Logging Backup Operations

```csharp
// Log a backup start
logger.LogBackupOperation(new BackupOperation
{
    JobName = "Daily Backup",
    Operation = "BACKUP_START",
    Message = "Starting backup operation",
    Details = new Dictionary<string, object>
    {
        { "sourcePath", "C:\\Documents" },
        { "targetPath", "D:\\Backup" },
        { "type", "Full" }
    }
});

// Log a backup completion
logger.LogBackupOperation(new BackupOperation
{
    JobName = "Daily Backup",
    Operation = "BACKUP_COMPLETE",
    Message = "Backup completed successfully",
    Details = new Dictionary<string, object>
    {
        { "filesProcessed", 150 },
        { "totalSize", 1024000 }
    }
});
```

#### Logging Encryption Operations

```csharp
// Log an encryption operation
logger.LogEncryptionOperation(new EncryptionOperation
{
    JobName = "Daily Backup",
    Operation = "ENCRYPTION",
    Message = "File encrypted successfully",
    Details = new Dictionary<string, object>
    {
        { "filePath", "D:\\Backup\\document.docx" },
        { "algorithm", "AES-256" }
    }
});
```

#### Logging Business Software Events

```csharp
// Log a business software event
logger.LogBusinessSoftwareEvent(new BusinessSoftwareEvent
{
    JobName = "Daily Backup",
    Operation = "SOFTWARE_DETECTED",
    Message = "Protected software detected",
    Details = new Dictionary<string, object>
    {
        { "softwareName", "Microsoft Word" },
        { "action", "Pause" }
    }
});
```

#### Error Handling

```csharp
try
{
    // Perform operation
}
catch (Exception ex)
{
    logger.LogError("Operation failed", ex);
}
```

### Best Practices

1. **Initialize Early**: Set the log file path at application startup
2. **Regular Monitoring**: Implement a log rotation strategy
3. **Error Handling**: Always wrap logging operations in try-catch blocks
4. **Performance**: Log only essential information during high-throughput operations
5. **Format Selection**: Choose JSON for better performance, XML for better readability

---

## Version Française

### Aperçu

La DLL EasySaveLogging fournit une solution robuste de journalisation pour l'application de sauvegarde EasySave V2.0. Elle implémente un logger singleton thread-safe qui enregistre les opérations de sauvegarde, les activités de chiffrement et les événements de protection des logiciels métier dans des fichiers au format JSON ou XML.

### Spécifications Techniques

- **Framework**: .NET 8.0
- **Langage**: C#
- **Type de Projet**: Bibliothèque de Classes
- **Dépendances**: 
  - System.Text.Json (inclus dans .NET 8.0)
  - System.Xml (inclus dans .NET 8.0)

### Composants Principaux

#### Classe Logger

La classe `Logger` est le composant principal de la DLL EasySaveLogging. Elle est implémentée comme un singleton thread-safe pour assurer une journalisation cohérente à travers plusieurs threads.

##### Fonctionnalités Clés

- **Patron Singleton**: Garantit une seule instance du logger dans toute l'application
- **Sécurité Thread**: Utilise un mécanisme de verrouillage pour empêcher les écritures concurrentes
- **Support de Formats Multiples**: Prend en charge les formats JSON (par défaut) et XML
- **Types de Journaux Multiples**: Prend en charge les journaux d'opération, de chiffrement et de protection des logiciels métier

##### Méthodes Publiques

| Méthode | Paramètres | Description |
|---------|------------|-------------|
| `GetInstance()` | Aucun | Renvoie l'instance singleton du Logger |
| `SetLogFilePath(string path)` | path: Le chemin du fichier journal | Définit l'emplacement du fichier journal et crée les répertoires nécessaires |
| `SetLogFormat(LogFormat format)` | format: Le format pour les journaux (JSON ou XML) | Définit le format pour les fichiers journaux (JSON est le défaut si non spécifié) |
| `LogBackupOperation(BackupOperation operation)` | operation: Les détails de l'opération de sauvegarde | Enregistre une opération de sauvegarde |
| `LogEncryptionOperation(EncryptionOperation operation)` | operation: Les détails de l'opération de chiffrement | Enregistre une opération de chiffrement |
| `LogBusinessSoftwareEvent(BusinessSoftwareEvent event)` | event: Les détails de l'événement logiciel métier | Enregistre un événement de protection des logiciels métier |
| `LogError(string message, Exception ex)` | message: Message d'erreur, ex: Exception | Enregistre une erreur avec la pile d'appels |
| `GetLogs(DateTime startDate, DateTime endDate)` | startDate, endDate: Plage de dates | Récupère les journaux dans la plage de dates spécifiée |

##### Classe LogEntry

La classe `LogEntry` structure les données de journal avant leur sérialisation.

| Propriété | Type | Description |
|-----------|------|-------------|
| Timestamp | DateTime | Quand l'entrée de journal a été créée |
| JobName | string | Nom de la tâche de sauvegarde |
| Operation | string | Type d'opération effectuée |
| Message | string | Description ou message d'erreur |
| Level | LogLevel | Niveau de journal (Info, Warning, Error) |
| Details | Dictionary<string, object> | Détails supplémentaires spécifiques à l'opération |

### Format du Fichier Journal

#### Format JSON (Par Défaut)

```json
{
  "logs": [
    {
      "timestamp": "2024-03-15T10:30:45.123Z",
      "jobName": "Sauvegarde Quotidienne",
      "operation": "BACKUP_START",
      "message": "Démarrage de l'opération de sauvegarde",
      "level": "Info",
      "details": {
        "sourcePath": "C:\\Documents",
        "targetPath": "D:\\Backup",
        "type": "Full"
      }
    },
    {
      "timestamp": "2024-03-15T10:35:12.456Z",
      "jobName": "Sauvegarde Quotidienne",
      "operation": "ENCRYPTION",
      "message": "Fichier chiffré avec succès",
      "level": "Info",
      "details": {
        "filePath": "D:\\Backup\\document.docx",
        "algorithm": "AES-256"
      }
    }
  ]
}
```

#### Format XML

```xml
<?xml version="1.0" encoding="utf-8"?>
<Logs>
  <LogEntry>
    <Timestamp>2024-03-15T10:30:45.123Z</Timestamp>
    <JobName>Sauvegarde Quotidienne</JobName>
    <Operation>BACKUP_START</Operation>
    <Message>Démarrage de l'opération de sauvegarde</Message>
    <Level>Info</Level>
    <Details>
      <SourcePath>C:\Documents</SourcePath>
      <TargetPath>D:\Backup</TargetPath>
      <Type>Full</Type>
    </Details>
  </LogEntry>
  <LogEntry>
    <Timestamp>2024-03-15T10:35:12.456Z</Timestamp>
    <JobName>Sauvegarde Quotidienne</JobName>
    <Operation>ENCRYPTION</Operation>
    <Message>Fichier chiffré avec succès</Message>
    <Level>Info</Level>
    <Details>
      <FilePath>D:\Backup\document.docx</FilePath>
      <Algorithm>AES-256</Algorithm>
    </Details>
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

// Définir le format du journal (JSON est le format par défaut)
logger.SetLogFormat(LogFormat.JSON);
```

#### Journalisation des Opérations de Sauvegarde

```csharp
// Journaliser le début d'une sauvegarde
logger.LogBackupOperation(new BackupOperation
{
    JobName = "Sauvegarde Quotidienne",
    Operation = "BACKUP_START",
    Message = "Démarrage de l'opération de sauvegarde",
    Details = new Dictionary<string, object>
    {
        { "sourcePath", "C:\\Documents" },
        { "targetPath", "D:\\Backup" },
        { "type", "Full" }
    }
});

// Journaliser la fin d'une sauvegarde
logger.LogBackupOperation(new BackupOperation
{
    JobName = "Sauvegarde Quotidienne",
    Operation = "BACKUP_COMPLETE",
    Message = "Sauvegarde terminée avec succès",
    Details = new Dictionary<string, object>
    {
        { "filesProcessed", 150 },
        { "totalSize", 1024000 }
    }
});
```

#### Journalisation des Opérations de Chiffrement

```csharp
// Journaliser une opération de chiffrement
logger.LogEncryptionOperation(new EncryptionOperation
{
    JobName = "Sauvegarde Quotidienne",
    Operation = "ENCRYPTION",
    Message = "Fichier chiffré avec succès",
    Details = new Dictionary<string, object>
    {
        { "filePath", "D:\\Backup\\document.docx" },
        { "algorithm", "AES-256" }
    }
});
```

#### Journalisation des Événements Logiciels Métier

```csharp
// Journaliser un événement logiciel métier
logger.LogBusinessSoftwareEvent(new BusinessSoftwareEvent
{
    JobName = "Sauvegarde Quotidienne",
    Operation = "SOFTWARE_DETECTED",
    Message = "Logiciel protégé détecté",
    Details = new Dictionary<string, object>
    {
        { "softwareName", "Microsoft Word" },
        { "action", "Pause" }
    }
});
```

#### Gestion des Erreurs

```csharp
try
{
    // Effectuer l'opération
}
catch (Exception ex)
{
    logger.LogError("L'opération a échoué", ex);
}
```

### Bonnes Pratiques

1. **Initialisation Précoce**: Définir le chemin du fichier journal au démarrage de l'application
2. **Surveillance Régulière**: Implémenter une stratégie de rotation des journaux
3. **Gestion des Erreurs**: Toujours envelopper les opérations de journalisation dans des blocs try-catch
4. **Performance**: Journaliser uniquement les informations essentielles lors d'opérations à haut débit
5. **Sélection du Format**: Choisir JSON pour de meilleures performances, XML pour une meilleure lisibilité

