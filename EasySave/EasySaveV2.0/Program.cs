using System;
using System.IO;
using System.Windows.Forms;
using EasySaveLogging;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Views;

namespace EasySaveV2._0
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Get configured log format
                var logFormat = Config.GetLogFormat();
                DebugLog($"Main - Initial log format from config: {logFormat}");
                
                // Ensure log directory exists with correct case
                var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    DebugLog("Main - Creating log directory");
                    Directory.CreateDirectory(logDir);
                }

                // Initialize logger with correct format
                var logger = Logger.GetInstance();
                DebugLog($"Main - Setting initial format to: {logFormat}");
                logger.SetLogFormat(logFormat);
                
                // Set log file path with correct name and extension
                string logFileName = "log" + (logFormat == LogFormat.JSON ? ".json" : ".xml");
                string logPath = Path.Combine(logDir, logFileName);
                DebugLog($"Main - Setting log path to: {logPath}");
                logger.SetLogFilePath(logPath);

                // Delete any old application.json file if it exists
                string oldLogPath = Path.Combine(logDir, "application.json");
                if (File.Exists(oldLogPath))
                {
                    DebugLog($"Main - Deleting old log file: {oldLogPath}");
                    try
                    {
                        File.Delete(oldLogPath);
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Main - Error deleting old log file: {ex.Message}");
                    }
                }

                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Configure unhandled exception handling
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // Ensure required directories exist
                EnsureDirectoriesExist();

                // Initialize language selection
                using (var languageForm = new LanguageSelectionForm())
                {
                    if (languageForm.ShowDialog() == DialogResult.OK)
                    {
                        // Start the main application
                        Application.Run(new MainForm());
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void EnsureDirectoriesExist()
        {
            var directories = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Logs"),
                Path.Combine(AppContext.BaseDirectory, "State"),
                Path.Combine(AppContext.BaseDirectory, "Ressources")
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        private static void Application_ThreadException(object? sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Une erreur inattendue s'est produite : {e.Exception.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Une erreur critique s'est produite : {ex.Message}", "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void HandleException(Exception ex)
        {
            try
            {
                // Log the exception
                Logger.GetInstance().LogAdminAction(
                    "System",
                    "ERROR",
                    $"Unhandled exception: {ex.Message}\nStack trace: {ex.StackTrace}"
                );

                // Show error message to user
                MessageBox.Show(
                    $"An error occurred: {ex.Message}\n\nPlease check the logs for more details.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch
            {
                // If logging fails, at least show a basic error message
                MessageBox.Show(
                    "A critical error occurred. The application will now exit.",
                    "Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            // Exit the application
            Application.Exit();
        }

        private static void DebugLog(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug.log"), logMessage);
            }
            catch
            {
                // Ignore debug logging errors
            }
        }
    }
}