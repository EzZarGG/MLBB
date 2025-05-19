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
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            Logger.GetInstance().SetLogFilePath(Path.Combine(logDir, "application.log"));

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Configure unhandled exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
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
    }
}