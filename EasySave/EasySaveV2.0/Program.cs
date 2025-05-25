using System;
using System.IO;
using System.Windows.Forms;
using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Views;
using EasySaveV2._0.Models;

namespace EasySaveV2._0
{
    /// <summary>
    /// Main entry point for the EasySave application.
    /// Handles application initialization, configuration, and error management.
    /// </summary>
    internal static class Program
    {
        private static readonly LanguageManager _languageManager = LanguageManager.Instance;
        private static Logger _logger;

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Application configuration
                ConfigureApplication();

                // Critical components initialization
                InitializeComponents();

                // User interface startup
                StartUserInterface();
            }
            catch (Exception ex)
            {
                HandleCriticalError(ex);
            }
        }

        /// <summary>
        /// Configures the basic application settings.
        /// </summary>
        private static void ConfigureApplication()
        {
            // User interface configuration
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Exception handling configuration
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// Initializes critical application components.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when initialization fails.</exception>
        private static void InitializeComponents()
        {
            try
            {
                // Create required directories
                EnsureDirectoriesExist();

                // Logger configuration
                ConfigureLogger();

                // Critical components validation
                ValidateCriticalComponents();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    _languageManager.GetTranslation("error.initFailed"), ex);
            }
        }

        /// <summary>
        /// Configures the logging system.
        /// Sets up the logger with the appropriate format and file path.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when logger configuration fails.</exception>
        private static void ConfigureLogger()
        {
            try
            {
                var logFormat = Config.GetLogFormat();
                
                var logDir = Config.GetLogDirectory();
                Directory.CreateDirectory(logDir);  // Ensure log directory exists
                
                var logPath = Path.Combine(logDir, $"log{(logFormat == LogFormat.JSON ? ".json" : ".xml")}");
                
                _logger = Logger.GetInstance();
                _logger.SetLogFormat(logFormat);  // Set the logging format
                _logger.SetLogFilePath(logPath);  // Set the log file path
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to configure logger", ex);
            }
        }

        /// <summary>
        /// Validates that all critical components are properly initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a critical component is not properly initialized.</exception>
        private static void ValidateCriticalComponents()
        {
            if (_languageManager == null)
                throw new InvalidOperationException(_languageManager.GetTranslation("error.languageManagerInitFailed"));
            
            if (_logger == null)
                throw new InvalidOperationException(_languageManager.GetTranslation("error.loggerInitFailed"));
        }

        /// <summary>
        /// Starts the application's user interface.
        /// Displays the language selection form and then the main form.
        /// </summary>
        private static void StartUserInterface()
        {
            using (var languageForm = new LanguageSelectionForm())
            {
                if (languageForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new MainForm());
                }
            }
        }

        /// <summary>
        /// Ensures all required application directories exist.
        /// Creates any missing directories and logs their creation.
        /// </summary>
        private static void EnsureDirectoriesExist()
        {
            var directories = new[]
            {
                Config.GetLogDirectory(),
                Path.Combine(AppContext.BaseDirectory, "State"),
                Path.Combine(AppContext.BaseDirectory, "Resources")
            };
        }

        /// <summary>
        /// Handles unhandled exceptions from the main thread.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data containing the exception</param>
        private static void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, false);
        }

        /// <summary>
        /// Handles unhandled exceptions from the application domain.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">Event data containing the exception</param>
        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex, e.IsTerminating);
            }
        }

        /// <summary>
        /// Handles an exception appropriately based on its type.
        /// Logs the error and displays a user-friendly message.
        /// </summary>
        /// <param name="ex">The exception to handle</param>
        /// <param name="isTerminating">Indicates whether the exception is fatal and will terminate the application</param>
        private static void HandleException(Exception ex, bool isTerminating)
        {
            try
            {
                var message = isTerminating
                    ? _languageManager.GetTranslation("error.critical")
                    : _languageManager.GetTranslation("error.unhandled");

                MessageBox.Show(
                    message.Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("error.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch
            {
                // If logging fails, display a basic error message
                MessageBox.Show(
                    "A critical error occurred. The application will now exit.",
                    "Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            if (isTerminating)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Handles a critical error that prevents application startup.
        /// Displays an error message and terminates the application.
        /// </summary>
        /// <param name="ex">The critical exception that occurred during startup</param>
        private static void HandleCriticalError(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("error.startupFailed").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("error.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch
            {
                MessageBox.Show(
                    "Failed to start the application. Please check the logs for details.",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                Application.Exit();
            }
        }
    }
}