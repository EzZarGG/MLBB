using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using EasySaveV2._0.Managers;
using EasySaveLogging;

namespace EasySaveV2._0.Controllers
{
    /// <summary>
    /// Controller responsible for managing application settings.
    /// Implements a singleton pattern to ensure consistent settings management across the application.
    /// </summary>
    public class SettingsController : IDisposable
    {
        private static SettingsController? _instance;
        private static readonly object _lock = new();
        private readonly string _settingsFile;
        private readonly JsonSerializerOptions _jsonOptions;
        private Settings _settings;
        private readonly LanguageManager _languageManager;
        private bool _isDisposed;
        private readonly Dictionary<string, Process[]> _processCache;
        private DateTime _lastProcessCheck;
        private const int PROCESS_CACHE_DURATION_SECONDS = 5;


        /// <summary>
        /// Gets the singleton instance of SettingsController.
        /// </summary>
        public static SettingsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SettingsController();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SettingsController class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when settings initialization fails.</exception>
        private SettingsController()
        {
            try
            {
                _settingsFile = Path.Combine(AppContext.BaseDirectory, "settings.json");
                _jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                _languageManager = LanguageManager.Instance;
                _processCache = new Dictionary<string, Process[]>(StringComparer.OrdinalIgnoreCase);
                _lastProcessCheck = DateTime.MinValue;

                LoadSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.settingsInitFailed"), ex);
            }
        }

        /// <summary>
        /// Loads settings from the settings file.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when settings cannot be loaded.</exception>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    var loadedSettings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions)
                                 ?? throw new InvalidOperationException("error.settingsDeserializationFailed");

                    if (loadedSettings == null)
                    {
                        throw new InvalidOperationException(_languageManager.GetTranslation("error.settingsDeserializationFailed"));
                    }

                    ValidateSettings(loadedSettings);
                    _settings = loadedSettings;
                    BusinessSoftwareManager.Initialize(_settings.BusinessSoftware);
                }
                else
                {
                    _settings = new Settings();
                    SaveSettings();
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.settingsLoadFailed"), ex);
            }
        }

        /// <summary>
        /// Validates settings to ensure they are properly configured.
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when settings validation fails.</exception>
        private void ValidateSettings(Settings settings)
        {
            if (settings == null)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.settingsNull"));
            }

            // Validate business software list
            if (settings.BusinessSoftware == null)
            {
                settings.BusinessSoftware = new List<string>();
            }
            else
            {
                settings.BusinessSoftware = settings.BusinessSoftware
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct()
                    .ToList();
            }

            // Validate encryption extensions
            if (settings.EncryptionExtensions == null)
            {
                settings.EncryptionExtensions = new List<string>();
            }
            else
            {
                settings.EncryptionExtensions = settings.EncryptionExtensions
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.Trim().ToLower())
                    .Select(e => e.StartsWith(".") ? e : "." + e)
                    .Distinct()
                    .ToList();
            }
        }

        /// <summary>
        /// Saves settings to the settings file.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when settings cannot be saved.</exception>
        private void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                BusinessSoftwareManager.Initialize(_settings.BusinessSoftware);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_languageManager.GetTranslation("error.settingsSaveFailed"), ex);
            }
        }

        /// <summary>
        /// Gets the current log format.
        /// </summary>
        /// <returns>Current log format</returns>
        public LogFormat GetCurrentLogFormat()
        {
            return _settings.LogFormat;
        }

        /// <summary>
        /// Sets the log format.
        /// </summary>
        /// <param name="format">Desired log format</param>
        /// <exception cref="ArgumentException">Thrown when format is invalid.</exception>
        public void SetLogFormat(LogFormat format)
        {
            if (!Enum.IsDefined(typeof(LogFormat), format))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.invalidLogFormat"));
            }

            if (_settings.LogFormat != format)
            {
                _settings.LogFormat = format;
                SaveSettings();
                
                // Update the logger format and file path
                var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                var logPath = Path.Combine(logDir, $"log{(format == LogFormat.JSON ? ".json" : ".xml")}");
                
                var logger = Logger.GetInstance();
                logger.SetLogFormat(format);
                logger.SetLogFilePath(logPath);
            }
        }

        /// <summary>
        /// Gets the list of business software.
        /// </summary>
        /// <returns>List of business software names</returns>
        public List<string> GetBusinessSoftware()
        {
            return new List<string>(_settings.BusinessSoftware);
        }

        /// <summary>
        /// Adds a business software to the list.
        /// </summary>
        /// <param name="softwareName">Name of the software to add</param>
        /// <exception cref="ArgumentException">Thrown when software name is invalid.</exception>
        public void AddBusinessSoftware(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.softwareNameEmpty"));
            }

            softwareName = softwareName.Trim();
            if (!_settings.BusinessSoftware.Contains(softwareName))
            {
                _settings.BusinessSoftware.Add(softwareName);
                SaveSettings();
                ClearProcessCache();
            }
        }

        /// <summary>
        /// Removes a business software from the list.
        /// </summary>
        /// <param name="softwareName">Name of the software to remove</param>
        /// <exception cref="ArgumentException">Thrown when software name is invalid.</exception>
        public void RemoveBusinessSoftware(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.softwareNameEmpty"));
            }

            softwareName = softwareName.Trim();
            if (_settings.BusinessSoftware.Remove(softwareName))
            {
                SaveSettings();
                ClearProcessCache();
            }
        }

        /// <summary>
        /// Sets the list of business software.
        /// </summary>
        /// <param name="software">List of software names</param>
        /// <exception cref="ArgumentNullException">Thrown when software list is null.</exception>
        public void SetBusinessSoftware(List<string> software)
        {
            if (software == null)
            {
                throw new ArgumentNullException(nameof(software), _languageManager.GetTranslation("error.softwareListNull"));
            }

            _settings.BusinessSoftware = software
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
            SaveSettings();
            ClearProcessCache();
        }

        /// <summary>
        /// Gets the list of encryption extensions.
        /// </summary>
        /// <returns>List of file extensions to encrypt</returns>
        public List<string> GetEncryptionExtensions()
        {
            return new List<string>(_settings.EncryptionExtensions);
        }

        /// <summary>
        /// Adds an encryption extension to the list.
        /// </summary>
        /// <param name="extension">File extension to add</param>
        /// <exception cref="ArgumentException">Thrown when extension is invalid.</exception>
        public void AddEncryptionExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.extensionEmpty"));
            }

            extension = extension.Trim().ToLower();
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            if (!_settings.EncryptionExtensions.Contains(extension))
            {
                _settings.EncryptionExtensions.Add(extension);
                SaveSettings();
            }
        }

        /// <summary>
        /// Removes an encryption extension from the list.
        /// </summary>
        /// <param name="extension">File extension to remove</param>
        /// <exception cref="ArgumentException">Thrown when extension is invalid.</exception>
        public void RemoveEncryptionExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.extensionEmpty"));
            }

            extension = extension.Trim().ToLower();
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            if (_settings.EncryptionExtensions.Remove(extension))
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// Sets the list of encryption extensions.
        /// </summary>
        /// <param name="extensions">List of file extensions</param>
        /// <exception cref="ArgumentNullException">Thrown when extensions list is null.</exception>
        public void SetEncryptionExtensions(List<string> extensions)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions), _languageManager.GetTranslation("error.extensionsListNull"));
            }

            _settings.EncryptionExtensions = extensions
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLower())
                .Select(e => e.StartsWith(".") ? e : "." + e)
                .Distinct()
                .ToList();
            SaveSettings();
        }

        /// <summary>
        /// Checks if any business software is currently running.
        /// </summary>
        /// <returns>True if any business software is running, false otherwise</returns>
        public bool IsBusinessSoftwareRunning()
        {
            UpdateProcessCache();
            return _settings.BusinessSoftware.Any(software =>
                _processCache.TryGetValue(software, out var processes) &&
                processes.Length > 0);
        }


        /// <summary>
        /// Gets the name of the currently running business software.
        /// </summary>
        /// <returns>Name of the running software, or empty string if none</returns>
        public string GetRunningBusinessSoftware()
        {
            UpdateProcessCache();
            return _settings.BusinessSoftware.FirstOrDefault(software => 
                _processCache.TryGetValue(software, out var processes) && 
                processes.Length > 0) ?? string.Empty;
        }

        /// <summary>
        /// Updates the process cache with current running processes.
        /// </summary>
        private void UpdateProcessCache()
        {
            if ((DateTime.Now - _lastProcessCheck).TotalSeconds >= PROCESS_CACHE_DURATION_SECONDS)
            {
                ClearProcessCache();
                foreach (var software in _settings.BusinessSoftware)
                {
                    try
                    {
                        _processCache[software] = Process.GetProcessesByName(software);
                    }
                    catch
                    {
                        _processCache[software] = Array.Empty<Process>();
                    }
                }
                _lastProcessCheck = DateTime.Now;
            }
        }

        /// <summary>
        /// Clears the process cache.
        /// </summary>
        private void ClearProcessCache()
        {
            _processCache.Clear();
            _lastProcessCheck = DateTime.MinValue;
        }

        /// <summary>
        /// Checks if a file should be encrypted based on its extension.
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file should be encrypted, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when file path is invalid.</exception>
        public bool ShouldEncryptFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(_languageManager.GetTranslation("error.filePathEmpty"));
            }

            var extension = Path.GetExtension(filePath).ToLower();
            return _settings.EncryptionExtensions.Contains(extension);
        }

        /// <summary>
        /// Disposes of the SettingsController resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the SettingsController resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    ClearProcessCache();
                    _settings.BusinessSoftware.Clear();
                    _settings.EncryptionExtensions.Clear();
                }
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer for SettingsController.
        /// </summary>
        ~SettingsController()
        {
            Dispose(false);
        }

        private class Settings
        {
            public LogFormat LogFormat { get; set; } = LogFormat.JSON;
            public List<string> BusinessSoftware { get; set; } = new List<string>();
            public List<string> EncryptionExtensions { get; set; } = new List<string>();
        }
    }
}