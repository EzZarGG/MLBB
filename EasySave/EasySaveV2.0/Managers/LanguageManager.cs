using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using EasySaveLogging;

namespace EasySaveV2._0.Managers
{
    public class LanguageManager
    {
        private static LanguageManager _instance;
        private const string LANGUAGES_DIR = "Ressources";
        private const string DEFAULT_LANGUAGE = "en";
        private string _currentLanguage;
        private Dictionary<string, Dictionary<string, string>> _translations;
        private readonly string _translationsFile;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler<string> LanguageChanged;
        public event EventHandler TranslationsReloaded;
        public event EventHandler<Exception> LanguageLoadError;

        public string CurrentLanguage => _currentLanguage;

        private LanguageManager()
        {
            _translationsFile = Path.Combine(AppContext.BaseDirectory, LANGUAGES_DIR, "translations.json");
            _currentLanguage = DEFAULT_LANGUAGE;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            LoadTranslations();
        }

        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LanguageManager();
                }
                return _instance;
            }
        }

        private void LoadTranslations()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_translationsFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_translationsFile));
                }

                if (!File.Exists(_translationsFile))
                {
                    CreateDefaultTranslations();
                }
                else
                {
                    var json = File.ReadAllText(_translationsFile);
                    _translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, _jsonOptions);
                    ValidateTranslations();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().LogAdminAction("System", "ERROR", $"Error loading translations: {ex.Message}");
                CreateDefaultTranslations();
                LanguageLoadError?.Invoke(this, ex);
            }
        }

        private void CreateDefaultTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["language.english"] = "English",
                    ["language.french"] = "French",
                    ["error.loadTranslations"] = "Error loading translations",
                    ["error.missingTranslation"] = "Missing translation for key: {0}",
                    ["error.invalidLanguage"] = "Invalid language: {0}",
                    ["menu.title"] = "EasySave Backup",
                    ["menu.file"] = "File",
                    ["menu.file.exit"] = "Exit",
                    ["menu.backup"] = "Backup",
                    ["menu.backup.create"] = "Create Backup",
                    ["menu.backup.edit"] = "Edit Backup",
                    ["menu.backup.delete"] = "Delete Backup",
                    ["menu.backup.run"] = "Run Backup",
                    ["menu.settings"] = "Settings",
                    ["menu.settings.open"] = "Open Settings",
                    ["menu.language"] = "Language",
                    ["menu.language.change"] = "Change Language",
                    ["menu.view"] = "View",
                    ["menu.view.logs"] = "View Logs",
                    ["menu.help"] = "Help",
                    ["status.ready"] = "Ready",
                    ["status.backupInProgress"] = "Backup in progress...",
                    ["status.backupComplete"] = "Backup completed",
                    ["status.backupError"] = "Backup failed",
                    ["status.settingsSaved"] = "Settings saved",
                    ["message.backupExists"] = "A backup with this name already exists",
                    ["message.backupNotFound"] = "Backup not found",
                    ["message.businessSoftwareRunning"] = "Business software is running. Backup cannot be started.",
                    ["message.confirmDelete"] = "Are you sure you want to delete this backup?",
                    ["message.confirmDeleteTitle"] = "Confirm Delete",
                    ["message.confirmExit"] = "Are you sure you want to exit?",
                    ["message.error"] = "An error occurred: {0}",
                    ["message.notFound"] = "No backup selected",
                    ["message.backupSuccess"] = "Backup completed successfully"
                },
                ["fr"] = new Dictionary<string, string>
                {
                    ["language.english"] = "Anglais",
                    ["language.french"] = "Français",
                    ["error.loadTranslations"] = "Erreur lors du chargement des traductions",
                    ["error.missingTranslation"] = "Traduction manquante pour la clé : {0}",
                    ["error.invalidLanguage"] = "Langue invalide : {0}",
                    ["menu.title"] = "EasySave Sauvegarde",
                    ["menu.file"] = "Fichier",
                    ["menu.file.exit"] = "Quitter",
                    ["menu.backup"] = "Sauvegarde",
                    ["menu.backup.create"] = "Créer une sauvegarde",
                    ["menu.backup.edit"] = "Modifier la sauvegarde",
                    ["menu.backup.delete"] = "Supprimer la sauvegarde",
                    ["menu.backup.run"] = "Exécuter la sauvegarde",
                    ["menu.settings"] = "Paramètres",
                    ["menu.settings.open"] = "Ouvrir les paramètres",
                    ["menu.language"] = "Langue",
                    ["menu.language.change"] = "Changer la langue",
                    ["menu.view"] = "Affichage",
                    ["menu.view.logs"] = "Voir les logs",
                    ["menu.help"] = "Aide",
                    ["status.ready"] = "Prêt",
                    ["status.backupInProgress"] = "Sauvegarde en cours...",
                    ["status.backupComplete"] = "Sauvegarde terminée",
                    ["status.backupError"] = "Échec de la sauvegarde",
                    ["status.settingsSaved"] = "Paramètres enregistrés",
                    ["message.backupExists"] = "Une sauvegarde avec ce nom existe déjà",
                    ["message.backupNotFound"] = "Sauvegarde non trouvée",
                    ["message.businessSoftwareRunning"] = "Un logiciel métier est en cours d'exécution. La sauvegarde ne peut pas être démarrée.",
                    ["message.confirmDelete"] = "Êtes-vous sûr de vouloir supprimer cette sauvegarde ?",
                    ["message.confirmDeleteTitle"] = "Confirmer la suppression",
                    ["message.confirmExit"] = "Êtes-vous sûr de vouloir quitter ?",
                    ["message.error"] = "Une erreur est survenue : {0}",
                    ["message.notFound"] = "Aucune sauvegarde sélectionnée",
                    ["message.backupSuccess"] = "Sauvegarde terminée avec succès"
                }
            };

            SaveTranslations();
        }

        private void ValidateTranslations()
        {
            if (_translations == null || !_translations.ContainsKey(DEFAULT_LANGUAGE))
            {
                throw new InvalidOperationException("English translations are required");
            }

            // Ensure all languages have the same keys as English
            var englishKeys = _translations[DEFAULT_LANGUAGE].Keys.ToList();
            foreach (var language in _translations.Keys.Where(k => k != DEFAULT_LANGUAGE))
            {
                foreach (var key in englishKeys)
                {
                    if (!_translations[language].ContainsKey(key))
                    {
                        _translations[language][key] = _translations[DEFAULT_LANGUAGE][key];
                    }
                }
            }

            SaveTranslations();
        }

        private void SaveTranslations()
        {
            try
            {
                var json = JsonSerializer.Serialize(_translations, _jsonOptions);
                File.WriteAllText(_translationsFile, json);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().LogAdminAction("System", "ERROR", $"Error saving translations: {ex.Message}");
                LanguageLoadError?.Invoke(this, ex);
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return;
            }

            if (!_translations.ContainsKey(languageCode))
            {
                var error = new ArgumentException(GetTranslation("error.invalidLanguage", languageCode));
                LanguageLoadError?.Invoke(this, error);
                return;
            }

            if (_currentLanguage != languageCode)
            {
                _currentLanguage = languageCode;
                LanguageChanged?.Invoke(this, languageCode);
                TranslationsReloaded?.Invoke(this, EventArgs.Empty);
            }
        }

        public string GetTranslation(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_translations.TryGetValue(_currentLanguage, out var translations) &&
                translations.TryGetValue(key, out var translation))
            {
                return args.Length > 0 ? string.Format(translation, args) : translation;
            }

            // Fallback to English if translation not found
            if (_currentLanguage != DEFAULT_LANGUAGE &&
                _translations.TryGetValue(DEFAULT_LANGUAGE, out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out var englishTranslation))
            {
                return args.Length > 0 ? string.Format(englishTranslation, args) : englishTranslation;
            }

            // If still not found, log the missing translation and return the key
            Logger.GetInstance().LogAdminAction(
                "System",
                "WARNING",
                GetTranslation("error.missingTranslation", key)
            );

            return key;
        }

        public List<string> GetAvailableLanguages()
        {
            return _translations.Keys.ToList();
        }

        public void ReloadTranslations()
        {
            LoadTranslations();
            TranslationsReloaded?.Invoke(this, EventArgs.Empty);
        }

        public void AddTranslation(string language, string key, string value)
        {
            if (!_translations.ContainsKey(language))
            {
                _translations[language] = new Dictionary<string, string>();
            }

            _translations[language][key] = value;
            SaveTranslations();
            TranslationsReloaded?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveTranslation(string language, string key)
        {
            if (_translations.ContainsKey(language) && _translations[language].ContainsKey(key))
            {
                _translations[language].Remove(key);
                SaveTranslations();
                TranslationsReloaded?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasTranslation(string language, string key)
        {
            return _translations.ContainsKey(language) && _translations[language].ContainsKey(key);
        }
    }
}