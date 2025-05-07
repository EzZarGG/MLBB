# MLBB
# EasySave - Version 1.0

## 📦 Description

**EasySave v1.0** est une application console développée en .NET Core, destinée à la gestion de travaux de sauvegarde pour utilisateurs francophones et anglophones. Elle permet la création, l’exécution et le suivi de sauvegardes complètes ou différentielles, tout en assurant une traçabilité rigoureuse via des fichiers de logs et d’état au format JSON.

---

## ⚙️ Fonctionnalités

- Interface en ligne de commande (CLI)
- Jusqu’à **5 travaux de sauvegarde** définissables
- Types de sauvegarde :
  - Sauvegarde complète
  - Sauvegarde différentielle
- Multilingue : **Français** et **Anglais**
- Support de différents types de répertoires :
  - Disques locaux
  - Disques externes
  - Lecteurs réseaux
- Exécution d’un ou plusieurs travaux :
  - Exemple `1-3` → exécute les sauvegardes 1, 2 et 3
  - Exemple `1;3` → exécute les sauvegardes 1 et 3

---

## 📁 Structure d’un travail de sauvegarde

Chaque travail est défini par :
- Nom de la sauvegarde
- Répertoire source
- Répertoire cible
- Type de sauvegarde (complète ou différentielle)

---

## 📝 Fichier Log Journalier

Le logiciel écrit en temps réel un **fichier log journalier JSON**, contenant pour chaque fichier traité :
- ⏱ Horodatage
- 📝 Nom du travail de sauvegarde
- 📂 Chemin complet du fichier source (format UNC)
- 📁 Chemin complet du fichier de destination (format UNC)
- 📐 Taille du fichier
- 🕐 Temps de transfert (en ms, négatif si erreur)

Le fichier est structuré en JSON, avec un retour à la ligne entre chaque élément pour faciliter la lecture.

Exemple :
```json
{
  "Date": "2020-12-17T14:25:43",
  "Name": "MyBackup1",
  "Source": "\\\\server\\folder\\file.txt",
  "Destination": "\\\\backup\\folder\\file.txt",
  "Size": 991,
  "TransferTime": 142
}
