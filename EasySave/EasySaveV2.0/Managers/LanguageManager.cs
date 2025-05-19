using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace EasySaveV2._0.Managers
{
    public class LanguageManager
    {
        private static LanguageManager _instance;
        private const string LANGUAGES_DIR = "Ressources";
        private const string DEFAULT_LANGUAGE = "en";
        private string _currentLanguage;
        private Dictionary<string, string> _translations;
        private string _languageFile;
        private readonly string _fallbackLanguageFile;

        public event EventHandler<string> LanguageChanged;
        public event EventHandler TranslationsReloaded;
        public event EventHandler<Exception> LanguageLoadError;

        public static LanguageManager Instance
        {
            get
            {
                _instance ??= new LanguageManager();
                return _instance;
            }
        }

        public string CurrentLanguage => _currentLanguage;

        private LanguageManager()
        {
            _currentLanguage = DEFAULT_LANGUAGE;
            _translations = new Dictionary<string, string>();
            _languageFile = Path.Combine(LANGUAGES_DIR, "translation.json");
            _fallbackLanguageFile = Path.Combine(LANGUAGES_DIR, "translation.json");
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            try
            {
                if (File.Exists(_languageFile))
                {
                    var json = File.ReadAllText(_languageFile);
                    var translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                    if (translations != null && translations.TryGetValue(_currentLanguage, out var languageTranslations))
                    {
                        _translations = FlattenTranslations(languageTranslations);
                    }
                    else
                    {
                        _translations = new Dictionary<string, string>();
                        LanguageLoadError?.Invoke(this, new Exception($"Language {_currentLanguage} not found in translation file"));
                    }
                }
                else
                {
                    _translations = new Dictionary<string, string>();
                    LanguageLoadError?.Invoke(this, new FileNotFoundException($"Translation file not found: {_languageFile}"));
                }
            }
            catch (Exception ex)
            {
                _translations = new Dictionary<string, string>();
                LanguageLoadError?.Invoke(this, ex);
            }
        }

        private Dictionary<string, string> FlattenTranslations(Dictionary<string, object> translations, string prefix = "")
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in translations)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                        if (nestedDict != null)
                        {
                            foreach (var nestedKvp in FlattenTranslations(nestedDict, key))
                            {
                                result[nestedKvp.Key] = nestedKvp.Value;
                            }
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.String)
                    {
                        result[key] = element.GetString() ?? key;
                    }
                }
            }
            return result;
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return;

            var newLanguageFile = Path.Combine(LANGUAGES_DIR, $"{languageCode}.json");
            if (!File.Exists(newLanguageFile))
            {
                LanguageLoadError?.Invoke(this, new FileNotFoundException($"Language file not found: {newLanguageFile}"));
                return;
            }

            _currentLanguage = languageCode;
            _languageFile = newLanguageFile;
            LoadLanguage();
            LanguageChanged?.Invoke(this, languageCode);
            TranslationsReloaded?.Invoke(this, EventArgs.Empty);
        }

        public string GetTranslation(string key)
        {
            if (_translations.TryGetValue(key, out var translation))
                return translation;

            return key;
        }

        public List<string> GetAvailableLanguages()
        {
            try
            {
                if (!Directory.Exists(LANGUAGES_DIR))
                    return new List<string> { DEFAULT_LANGUAGE };

                return Directory.GetFiles(LANGUAGES_DIR, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();
            }
            catch
            {
                return new List<string> { DEFAULT_LANGUAGE };
            }
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }
    }
}