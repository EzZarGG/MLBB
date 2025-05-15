# EasySaveLogging - DLL Documentation

## English

### Description

`EasySaveLogging` is a reusable, versioned DLL written in C# targeting .NET Core.  
It is designed to manage structured and readable JSON log files for the **EasySave** backup application, and can be integrated into other C# applications needing consistent logging of actions and events.

This library implements a **singleton logger**, ensuring centralized and thread-safe logging.

---

### Features

- Singleton-based logger pattern
- Writes structured JSON log files
- Supports two log types:
  - **File transfer logs** (`CreateLog`)
  - **Administrative logs** (`LogAdminAction`)
- Human-readable JSON output (indented, camelCase)
- Thread-safe writing
- Dynamically creates log file and directory if missing

---

### Setup and Usage

#### 1. Reference the DLL in your project

- Add the compiled `EasySaveLogging.dll` to your Visual Studio project
- Or include the project `EasySaveLogging.csproj` in your solution and add a reference to it

#### 2. Initialize the Logger

```cSharp
using EasySaveLogging;

var logger = Logger.GetInstance();
logger.SetLogFilePath("C:\\Logs\\log-2025-05-14.json");
```
---

### Log a file transfer

```cSharp
logger.CreateLog(
	backupName: "MyBackup",
	transferTime: TimeSpan.FromMilliseconds(234),
	fileSize: 102400,
	date: DateTime.Now,
	sourcePath: @"D:\source\file.txt",
	targetPath: @"E:\backup\file.txt",
	logType: "INFO" // or "ERROR"
);
```

---

### Log an admin action

```cSharp
logger.LogAdminAction(
	backupName: "MyBackup",
	actionType: "START_BACKUP",
	message: "Backup operation initiated."
);
```

---

### Display logs in console

```cSharp
logger.DisplayLogs();
```

### Log File Format

- Format: JSON array ([{}, {}, ...])

- One file per day (recommended via filename)

Example entry:

```cSharp
{
  "timestamp": "2025-05-14T15:34:22",
  "backupName": "MyBackup",
  "sourcePath": "D:\\source\\file.txt",
  "targetPath": "E:\\backup\\file.txt",
  "fileSize": 102400,
  "transferTime": 234,
  "message": "File transferred",
  "logType": "INFO",
  "actionType": "FILE_TRANSFER"
}
```

---

### Class Overview

#### Logger (Singleton)

| Method                        | Description                                             |
| ----------------------------- | ------------------------------------------------------- |
| `GetInstance()`               | Returns the singleton instance                          |
| `SetLogFilePath(string path)` | Sets the path to the log file (creates file if missing) |
| `CreateLog(...)`              | Logs a file transfer operation                          |
| `LogAdminAction(...)`         | Logs an administrative action                           |
| `DisplayLogs()`               | Displays the current JSON log content in console        |

#### LogEntry (Internal)

| Property       | Description                                               |
| -------------- | --------------------------------------------------------- |
| `Timestamp`    | Date and time of the log                                  |
| `BackupName`   | Name of the backup job                                    |
| `SourcePath`   | File source path                                          |
| `TargetPath`   | File destination path                                     |
| `FileSize`     | Size of the file in bytes                                 |
| `TransferTime` | Duration in milliseconds                                  |
| `Message`      | Log message (e.g., "File transferred")                    |
| `LogType`      | Type of log (e.g., "INFO", "ERROR")                       |
| `ActionType`   | Action category (e.g., "FILE\_TRANSFER", "START\_BACKUP") |

---

### Thread safety

All log entries are appended within a lock to ensure thread-safe operations when multiple parts of the application write logs concurrently.

---

### Requirements

- .NET 8.0 or compatible runtime

- Works with Windows file system paths (UNC/local/external)

---

### Version

- Current Version: 1.0.0

- Changes:

	- Initial implementation of CreateLog and LogAdminAction

	- Structured log entry format

	- Thread-safe file writes

	- JSON formatting with indentation

---

---

### Support

If you have questions or issues integrating the DLL, open an issue in the EasySave main repository or contact the development team.

---

## Fran�ais

### Description

`EasySaveLogging` est une DLL r�utilisable et versionn�e �crite en C# ciblant .NET Core.  
Elle est con�ue pour g�rer des fichiers journaux JSON structur�s et lisibles pour l'application de sauvegarde **EasySave**, et peut �tre int�gr�e dans d'autres applications C# n�cessitant une journalisation coh�rente des actions et �v�nements.

Cette biblioth�que impl�mente un **logger singleton**, assurant une journalisation centralis�e et thread-safe.

---

### Fonctionnalit�s

- Mod�le de logger bas� sur le singleton
- �crit des fichiers journaux JSON structur�s
- Prend en charge deux types de journaux :
  - **Journaux de transfert de fichiers** (`CreateLog`)
  - **Journaux administratifs** (`LogAdminAction`)
- Sortie JSON lisible par l'humain (indent�, camelCase)
- �criture thread-safe
- Cr�e dynamiquement le fichier journal et le r�pertoire s'ils sont manquants

---

### Configuration et utilisation

#### 1. R�f�rencer la DLL dans votre projet

- Ajoutez la DLL compil�e `EasySaveLogging.dll` � votre projet Visual Studio
- Ou incluez le projet `EasySaveLogging.csproj` dans votre solution et ajoutez-y une r�f�rence

#### 2. Initialiser le Logger

```cSharp
using EasySaveLogging;

var logger = Logger.GetInstance();
logger.SetLogFilePath("C:\\Logs\\log-2025-05-14.json");
```
---

### Journaliser un transfert de fichier

```cSharp
logger.CreateLog(
	backupName: "MaSauvegarde",
	transferTime: TimeSpan.FromMilliseconds(234),
	fileSize: 102400,
	date: DateTime.Now,
	sourcePath: @"D:\source\fichier.txt",
	targetPath: @"E:\backup\fichier.txt",
	logType: "INFO" // ou "ERROR"
);
```

---

### Journaliser une action administrative

```cSharp
logger.LogAdminAction(
	backupName: "MaSauvegarde",
	actionType: "START_BACKUP",
	message: "Op�ration de sauvegarde initi�e."
);
```

---

### Afficher les journaux dans la console

```cSharp
logger.DisplayLogs();
```

---

### Format du fichier journal

- Format : tableau JSON ([{}, {}, ...])

- Un fichier par jour (recommand� via le nom de fichier)

Exemple d'entr�e :

```cSharp
{
  "timestamp": "2025-05-14T15:34:22",
  "backupName": "MaSauvegarde",
  "sourcePath": "D:\\source\\fichier.txt",
  "targetPath": "E:\\backup\\fichier.txt",
  "fileSize": 102400,
  "transferTime": 234,
  "message": "Fichier transf�r�",
  "logType": "INFO",
  "actionType": "FILE_TRANSFER"
}
```

---

### Aper�u des classes

#### Logger (Singleton)

| M�thode                       | Description                                                     |
| ----------------------------- | --------------------------------------------------------------- |
| `GetInstance()`               | Renvoie l'instance singleton                                    |
| `SetLogFilePath(string path)` | D�finit le chemin du fichier journal (cr�e le fichier si absent)|
| `CreateLog(...)`              | Journalise une op�ration de transfert de fichier                |
| `LogAdminAction(...)`         | Journalise une action administrative                            |
| `DisplayLogs()`               | Affiche le contenu actuel du journal JSON dans la console       |

#### LogEntry (Interne)

| Propri�t�      | Description                                                   |
| -------------- | ------------------------------------------------------------- |
| `Timestamp`    | Date et heure du journal                                      |
| `BackupName`   | Nom de la t�che de sauvegarde                                 |
| `SourcePath`   | Chemin source du fichier                                      |
| `TargetPath`   | Chemin de destination du fichier                              |
| `FileSize`     | Taille du fichier en octets                                   |
| `TransferTime` | Dur�e en millisecondes                                        |
| `Message`      | Message du journal (ex. "Fichier transf�r�")                  |
| `LogType`      | Type de journal (ex. "INFO", "ERROR")                         |
| `ActionType`   | Cat�gorie d'action (ex. "FILE\_TRANSFER", "START\_BACKUP")    |

---

### S�curit� des threads

Toutes les entr�es de journal sont ajout�es dans un verrou pour garantir des op�rations thread-safe lorsque plusieurs parties de l'application �crivent des journaux simultan�ment.

---

### Pr�requis

- .NET 8.0 ou runtime compatible

- Fonctionne avec les chemins de fichiers Windows (UNC/local/externe)

---

### Version

- Version actuelle : 1.0.0

- Changements :

	- Impl�mentation initiale de CreateLog et LogAdminAction

	- Format d'entr�e de journal structur�

	- �critures de fichiers thread-safe

	- Formatage JSON avec indentation

---

### Licence/Utilisation

Cette DLL est destin�e � un usage interne avec l'application EasySave. Pour une utilisation externe, veuillez vous r�f�rer au d�p�t et � la licence EasySave.

---

### Support

Si vous avez des questions ou des probl�mes d'int�gration de la DLL, ouvrez une issue dans le d�p�t principal d'EasySave ou contactez l'�quipe de d�veloppement.