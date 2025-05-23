using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using EasySaveV2._0;

namespace EasySaveV2._0.Managers
{
    /// <summary>
    /// Manages application translations and language settings.
    /// Implements a singleton pattern to ensure consistent language management across the application.
    /// </summary>
    public class LanguageManager : IDisposable
    {
        private static LanguageManager? _instance;
        private static readonly object _lock = new();
        private const string LANGUAGES_DIR = "Ressources";
        private const string DEFAULT_LANGUAGE = "en";
        private const int CACHE_SIZE = 1000; // Maximum number of cached translations
        private string _currentLanguage = DEFAULT_LANGUAGE;
        private Dictionary<string, Dictionary<string, string>> _translations = new();
        private readonly string _translationsFile;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, string> _translationCache;
        private bool _isDisposed;

        /// <summary>
        /// Event raised when the application language is changed.
        /// </summary>
        public event EventHandler<string>? LanguageChanged;

        /// <summary>
        /// Event raised when translations are reloaded.
        /// </summary>
        public event EventHandler? TranslationsReloaded;

        /// <summary>
        /// Event raised when an error occurs during language operations.
        /// </summary>
        public event EventHandler<Exception>? LanguageLoadError;

        /// <summary>
        /// Gets the current language code.
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Gets the singleton instance of LanguageManager.
        /// </summary>
        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LanguageManager();
                    }
                }
                return _instance;
            }
        }

        private LanguageManager()
        {
            try
            {
                _translationsFile = Path.Combine(AppContext.BaseDirectory, LANGUAGES_DIR, "translations.json");
                _jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                _translationCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Ensure the languages directory exists
                var languagesDir = Path.Combine(AppContext.BaseDirectory, LANGUAGES_DIR);
                if (!Directory.Exists(languagesDir))
                {
                    Directory.CreateDirectory(languagesDir);
                }

                LoadTranslations();
            }
            catch (Exception ex)
            {
                LanguageLoadError?.Invoke(this, ex);
                throw;
            }
        }

        private void LoadTranslations()
        {
            try
            {
                var directory = Path.GetDirectoryName(_translationsFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_translationsFile))
                {
                    var error = new FileNotFoundException("Translations file not found", _translationsFile);
                    LanguageLoadError?.Invoke(this, error);
                    throw error;
                }

                var json = File.ReadAllText(_translationsFile);
                var loadedTranslations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, _jsonOptions);
                
                if (loadedTranslations == null)
                {
                    var error = new InvalidOperationException("Failed to load translations");
                    LanguageLoadError?.Invoke(this, error);
                    throw error;
                }

                _translations = loadedTranslations;
                ValidateTranslations();
                ClearCache();
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not InvalidOperationException)
            {
                LanguageLoadError?.Invoke(this, ex);
                throw;
            }
        }

        private void ValidateTranslations()
        {
            if (_translations == null || !_translations.ContainsKey(DEFAULT_LANGUAGE))
            {
                var error = new InvalidOperationException("English translations are required");
                LanguageLoadError?.Invoke(this, error);
                throw error;
            }

            var englishKeys = _translations[DEFAULT_LANGUAGE].Keys.ToList();
            var missingKeys = new List<string>();
            var invalidKeys = new List<string>();

            // Validate all languages
            foreach (var language in _translations.Keys)
            {
                foreach (var key in englishKeys)
                {
                    if (!_translations[language].ContainsKey(key))
                    {
                        missingKeys.Add($"{language}:{key}");
                        _translations[language][key] = _translations[DEFAULT_LANGUAGE][key];
                    }
                    else if (string.IsNullOrWhiteSpace(_translations[language][key]))
                    {
                        invalidKeys.Add($"{language}:{key}");
                        _translations[language][key] = _translations[DEFAULT_LANGUAGE][key];
                    }
                }
            }

            if (missingKeys.Any() || invalidKeys.Any())
            {
                var error = new InvalidOperationException(
                    $"Translation validation issues: " +
                    $"{(missingKeys.Any() ? $"Missing keys: {string.Join(", ", missingKeys)}" : "")}" +
                    $"{(invalidKeys.Any() ? $"Invalid keys: {string.Join(", ", invalidKeys)}" : "")}"
                );
                LanguageLoadError?.Invoke(this, error);
            }

            SaveTranslations();
        }

        private void SaveTranslations()
        {
            try
            {
                var json = JsonSerializer.Serialize(_translations, _jsonOptions);
                File.WriteAllText(_translationsFile, json);
                ClearCache();
            }
            catch (Exception ex)
            {
                LanguageLoadError?.Invoke(this, ex);
                throw;
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                var error = new ArgumentException("Language code cannot be null or empty");
                LanguageLoadError?.Invoke(this, error);
                throw error;
            }

            if (!_translations.ContainsKey(languageCode))
            {
                var error = new ArgumentException($"Invalid language code: {languageCode}");
                LanguageLoadError?.Invoke(this, error);
                throw error;
            }

            if (_currentLanguage != languageCode)
            {
                _currentLanguage = languageCode;
                ClearCache();
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

            try
            {
                // Check cache first
                var cacheKey = $"{_currentLanguage}:{key}";
                if (_translationCache.TryGetValue(cacheKey, out var cachedTranslation))
                {
                    return args.Length > 0 ? string.Format(cachedTranslation, args) : cachedTranslation;
                }

                if (_translations == null)
                {
                    LoadTranslations();
                }

                string? translation = null;
                if (_translations.TryGetValue(_currentLanguage, out var translations) &&
                    translations.TryGetValue(key, out translation))
                {
                    CacheTranslation(cacheKey, translation);
                    return args.Length > 0 ? string.Format(translation, args) : translation;
                }

                // Fallback to English
                if (_currentLanguage != DEFAULT_LANGUAGE &&
                    _translations.TryGetValue(DEFAULT_LANGUAGE, out var englishTranslations) &&
                    englishTranslations.TryGetValue(key, out translation))
                {
                    CacheTranslation(cacheKey, translation);
                    return args.Length > 0 ? string.Format(translation, args) : translation;
                }

                return key;
            }
            catch (Exception ex)
            {
                LanguageLoadError?.Invoke(this, ex);
                return key;
            }
        }

        /// <summary>
        /// Caches a translation for faster retrieval.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="translation">The translation to cache.</param>
        private void CacheTranslation(string key, string translation)
        {
            if (_translationCache.Count >= CACHE_SIZE)
            {
                _translationCache.Clear();
            }
            _translationCache[key] = translation;
        }

        /// <summary>
        /// Clears the translation cache.
        /// </summary>
        private void ClearCache()
        {
            _translationCache.Clear();
        }

        /// <summary>
        /// Gets a list of available languages.
        /// </summary>
        /// <returns>A list of language codes.</returns>
        public List<string> GetAvailableLanguages()
        {
            return _translations.Keys.ToList();
        }

        /// <summary>
        /// Reloads translations from the translations file.
        /// </summary>
        public void ReloadTranslations()
        {
            try
            {
                LoadTranslations();
                TranslationsReloaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LanguageLoadError?.Invoke(this, ex);
                throw;
            }
        }

        /// <summary>
        /// Adds a new translation.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="key">The translation key.</param>
        /// <param name="value">The translation value.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        public void AddTranslation(string language, string key, string value)
        {
            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(key))
            {
                var error = new ArgumentException("Language and key cannot be null or empty");
                LanguageLoadError?.Invoke(this, error);
                throw error;
            }

            if (!_translations.ContainsKey(language))
            {
                _translations[language] = new Dictionary<string, string>();
            }

            _translations[language][key] = value;
            SaveTranslations();
            TranslationsReloaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Removes a translation.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="key">The translation key.</param>
        public void RemoveTranslation(string language, string key)
        {
            if (_translations.ContainsKey(language) && _translations[language].ContainsKey(key))
            {
                _translations[language].Remove(key);
                SaveTranslations();
                TranslationsReloaded?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Checks if a translation exists.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="key">The translation key.</param>
        /// <returns>True if the translation exists, false otherwise.</returns>
        public bool HasTranslation(string language, string key)
        {
            return _translations.ContainsKey(language) && _translations[language].ContainsKey(key);
        }

        /// <summary>
        /// Disposes of the LanguageManager resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the LanguageManager resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    ClearCache();
                    _translations.Clear();
                }
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer for LanguageManager.
        /// </summary>
        ~LanguageManager()
        {
            Dispose(false);
        }
    }
}