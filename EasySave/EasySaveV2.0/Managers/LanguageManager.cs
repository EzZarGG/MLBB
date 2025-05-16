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
        private Dictionary<string, Dictionary<string, string>> _translations;
        private string _currentLanguage;
        private readonly string _translationsFile;

        public event EventHandler LanguageChanged;
        public event EventHandler TranslationsReloaded;

        public string CurrentLanguage => _currentLanguage;

        private LanguageManager()
        {
            _translationsFile = Path.Combine(AppContext.BaseDirectory, "Ressources", "translation.json");
            _currentLanguage = "en"; // Default language
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
                if (!File.Exists(_translationsFile))
                {
                    // Create default translations if file doesn't exist
                    CreateDefaultTranslations();
                }

                var json = File.ReadAllText(_translationsFile);
                _translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

                // Validate translations
                ValidateTranslations();
            }
            catch (Exception ex)
            {
                // If there's an error loading translations, create a default English translation
                _translations = new Dictionary<string, Dictionary<string, string>>
                {
                    ["en"] = new Dictionary<string, string>
                    {
                        ["error.loadTranslations"] = "Error loading translations: " + ex.Message,
                        ["error.missingTranslation"] = "Missing translation for key: {0}",
                        ["error.invalidLanguage"] = "Invalid language: {0}"
                    }
                };
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
                    ["error.invalidLanguage"] = "Invalid language: {0}"
                },
                ["fr"] = new Dictionary<string, string>
                {
                    ["language.english"] = "Anglais",
                    ["language.french"] = "Français",
                    ["error.loadTranslations"] = "Erreur lors du chargement des traductions",
                    ["error.missingTranslation"] = "Traduction manquante pour la clé : {0}",
                    ["error.invalidLanguage"] = "Langue invalide : {0}"
                }
            };

            SaveTranslations();
        }

        private void ValidateTranslations()
        {
            if (_translations == null || !_translations.ContainsKey("en"))
            {
                throw new InvalidOperationException("English translations are required");
            }

            // Ensure all languages have the same keys as English
            var englishKeys = _translations["en"].Keys.ToList();
            foreach (var language in _translations.Keys.Where(k => k != "en"))
            {
                foreach (var key in englishKeys)
                {
                    if (!_translations[language].ContainsKey(key))
                    {
                        _translations[language][key] = _translations["en"][key];
                    }
                }
            }

            SaveTranslations();
        }

        private void SaveTranslations()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_translations, options);
            File.WriteAllText(_translationsFile, json);
        }

        public void SetLanguage(string language)
        {
            if (!_translations.ContainsKey(language))
            {
                throw new ArgumentException(GetTranslation("error.invalidLanguage", language));
            }

            if (_currentLanguage != language)
            {
                _currentLanguage = language;
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string GetTranslation(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_translations.TryGetValue(_currentLanguage, out var translations) &&
                translations.TryGetValue(key, out var translation))
            {
                return translation;
            }

            // Fallback to English if translation not found
            if (_currentLanguage != "en" &&
                _translations.TryGetValue("en", out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out var englishTranslation))
            {
                return englishTranslation;
            }

            // If still not found, return the key and log the missing translation
            Logger.GetInstance().LogAdminAction(
                "System",
                "WARNING",
                GetTranslation("error.missingTranslation", key)
            );

            return key;
        }

        public string GetTranslation(string key, params object[] args)
        {
            var translation = GetTranslation(key);
            return string.Format(translation, args);
        }

        public IEnumerable<string> GetAvailableLanguages()
        {
            return _translations.Keys;
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
        }

        public void RemoveTranslation(string language, string key)
        {
            if (_translations.ContainsKey(language) && _translations[language].ContainsKey(key))
            {
                _translations[language].Remove(key);
                SaveTranslations();
            }
        }

        public bool HasTranslation(string language, string key)
        {
            return _translations.ContainsKey(language) && _translations[language].ContainsKey(key);
        }
    }
}