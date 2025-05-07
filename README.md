# MLBB
# EasySave - Version 1.0

## 📦 Description

**EasySave v1.0** is a console-based application developed with .NET Core, designed to manage backup tasks for both English and French users. It allows the creation, execution, and monitoring of full or differential backups while maintaining detailed logs and real-time status reports in JSON format.

---

## ⚙️ Features

- Command Line Interface (CLI)
- Supports up to **5 backup tasks**
- Backup types:
  - Full backup
  - Differential backup
- Multilingual support: **English** and **French**
- Source and destination directories can be:
  - Local drives
  - External drives
  - Network drives
- Execute one or multiple backup tasks via CLI:
  - Example `1-3` → executes backups 1, 2, and 3
  - Example `1;3` → executes backups 1 and 3

---

## 📁 Backup Task Structure

Each backup task is defined by:
- Backup name
- Source directory
- Destination directory
- Backup type (Full or Differential)

---

## 📝 Daily Log File

The application writes a **daily JSON log file** in real time, containing the following details for each processed file:
- ⏱ Timestamp
- 📝 Backup task name
- 📂 Full UNC path of the source file
- 📁 Full UNC path of the destination file
- 📐 File size
- 🕐 Transfer time in milliseconds (negative if an error occurs)

The JSON log should have line breaks between elements for better readability.

Example:
```json
{
  "Date": "2020-12-17T14:25:43",
  "Name": "MyBackup1",
  "Source": "\\\\server\\folder\\file.txt",
  "Destination": "\\\\backup\\folder\\file.txt",
  "Size": 991,
  "TransferTime": 142
}

