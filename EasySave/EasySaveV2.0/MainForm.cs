using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using EasySaveV2._0.Views;
using EasySaveV2._0.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EasySaveLogging;

namespace EasySaveV2._0
{
    public partial class MainForm : Form
    {
        private readonly BackupController _backupController;
        private readonly SettingsController _settingsController;
        private readonly LanguageManager _languageManager;
        private readonly Logger _logger;
        private bool _isInitialized = false;

        public MainForm()
        {
            try
            {
                _logger = Logger.GetInstance();
                _logger.SetLogFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "application.log"));
                _logger.LogAdminAction("System", "STARTUP", "Starting application initialization...");
                UpdateStatus("Initializing application...");

                InitializeComponent();
                _logger.LogAdminAction("System", "INIT", "Components initialized");

                _backupController = new BackupController();
                _settingsController = new SettingsController();
                _languageManager = LanguageManager.Instance;
                _logger.LogAdminAction("System", "INIT", "Controllers and managers initialized");

                // Subscribe to language events
                _languageManager.LanguageChanged += OnLanguageChanged;
                _languageManager.TranslationsReloaded += OnTranslationsReloaded;
                _logger.LogAdminAction("System", "INIT", "Language events subscribed");

                // Show language selection first if no language is set
                if (string.IsNullOrEmpty(_languageManager.CurrentLanguage))
                {
                    _logger.LogAdminAction("System", "LANGUAGE", "No language set, showing language selection");
                    UpdateStatus("Please select a language...");
                    ShowLanguageSelection();
                }
                else
                {
                    _logger.LogAdminAction("System", "LANGUAGE", $"Language already set to: {_languageManager.CurrentLanguage}");
                    UpdateStatus("Initializing interface...");
                    InitializeApplication();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error during initialization: {ex.Message}");
                MessageBox.Show(
                    $"Error during initialization: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Application.Exit();
            }
        }

        private void UpdateStatus(string message)
        {
            try
            {
                if (_statusLabel != null)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => _statusLabel.Text = message));
                    }
                    else
                    {
                        _statusLabel.Text = message;
                    }
                }
                _logger?.LogAdminAction("System", "STATUS", message);
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error updating status: {ex.Message}");
            }
        }

        private void ShowLanguageSelection()
        {
            try
            {
                UpdateStatus("Opening language selection...");
                using (var languageForm = new LanguageSelectionForm())
                {
                    if (languageForm.ShowDialog() == DialogResult.OK)
                    {
                        _logger.LogAdminAction("System", "LANGUAGE", "Language selected successfully");
                        UpdateStatus("Initializing interface...");
                        InitializeApplication();
                    }
                    else
                    {
                        _logger.LogAdminAction("System", "LANGUAGE", "Language selection cancelled by user");
                        UpdateStatus("Language selection cancelled");
                        Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error in language selection: {ex.Message}");
                MessageBox.Show(
                    $"Error during language selection: {ex.Message}",
                    "Language Selection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Application.Exit();
            }
        }

        private void InitializeApplication()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogAdminAction("System", "INIT", "Application already initialized, skipping initialization");
                    return;
                }

                _logger.LogAdminAction("System", "INIT", "Starting application initialization...");
                UpdateStatus("Initializing timer...");
                InitializeTimer();

                UpdateStatus("Initializing menu...");
                InitializeMenuStrip();

                UpdateStatus("Initializing toolbar...");
                InitializeToolStrip();

                UpdateStatus("Initializing list view...");
                InitializeListViewColumns();

                UpdateStatus("Initializing context menu...");
                InitializeContextMenu();

                UpdateStatus("Loading backup list...");
                RefreshBackupList();

                _isInitialized = true;
                _logger.LogAdminAction("System", "INIT", "Application initialization completed successfully");
                UpdateStatus(_languageManager.GetTranslation("status.ready"));
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error during application initialization: {ex.Message}");
                MessageBox.Show(
                    $"Error during application initialization: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Application.Exit();
            }
        }

        private void InitializeTimer()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing timer...");
                _updateTimer.Interval = 1000;
                _updateTimer.Start();
                _logger.LogAdminAction("System", "INIT", "Timer started successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing timer: {ex.Message}");
                throw;
            }
        }

        private void OnUpdateTimerTick(object sender, EventArgs e)
        {
            try
            {
                UpdateBackupStates();
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error updating backup states: {ex.Message}");
            }
        }

        private void InitializeMenuStrip()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing menu strip...");
                // File menu
                var fileMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file"));
                var exitItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file.exit"));
                exitItem.Click += (s, e) => Application.Exit();
                fileMenu.DropDownItems.Add(exitItem);

                // Backup menu
                var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup"));
                var createBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.create"));
                createBackupItem.Click += (s, e) => AddJob();

                var editBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.edit"));
                editBackupItem.Click += (s, e) => UpdateJob();

                var deleteBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.delete"));
                deleteBackupItem.Click += (s, e) => RemoveJob();

                var runBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.run"));
                runBackupItem.Click += (s, e) => ExecuteJob();

                backupMenu.DropDownItems.AddRange(new ToolStripItem[]
                {
                    createBackupItem, editBackupItem, deleteBackupItem, runBackupItem
                });

                // Settings menu
                var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings"));
                var openSettingsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings.open"));
                openSettingsItem.Click += (s, e) => OpenSettings();
                settingsMenu.DropDownItems.Add(openSettingsItem);

                // Language menu
                var languageMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.language"));
                var changeLanguageItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.language.change"));
                changeLanguageItem.Click += (s, e) => ShowLanguageSelection();
                languageMenu.DropDownItems.Add(changeLanguageItem);

                // View menu
                var viewMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view"));
                var viewLogsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view.logs"));
                viewLogsItem.Click += (s, e) => ViewLogs();
                viewMenu.DropDownItems.Add(viewLogsItem);

                // Help menu
                var helpMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.help"));

                // Add all menus to menu strip
                _menuStrip.Items.AddRange(new ToolStripItem[]
                {
                    fileMenu, backupMenu, settingsMenu, languageMenu, viewMenu, helpMenu
                });
                _logger.LogAdminAction("System", "INIT", "Menu strip initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing menu strip: {ex.Message}");
                throw;
            }
        }

        private void InitializeToolStrip()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing tool strip...");
                var newButton = new ToolStripButton(_languageManager.GetTranslation("menu.new"));
                newButton.Click += (s, e) => AddJob();

                var editButton = new ToolStripButton(_languageManager.GetTranslation("menu.edit"));
                editButton.Click += (s, e) => UpdateJob();

                var deleteButton = new ToolStripButton(_languageManager.GetTranslation("menu.delete"));
                deleteButton.Click += (s, e) => RemoveJob();

                var runButton = new ToolStripButton(_languageManager.GetTranslation("menu.run"));
                runButton.Click += (s, e) => ExecuteJob();

                var settingsButton = new ToolStripButton(_languageManager.GetTranslation("menu.settings"));
                settingsButton.Click += (s, e) => OpenSettings();

                var languageButton = new ToolStripButton(_languageManager.GetTranslation("menu.language"));
                languageButton.Click += (s, e) => ShowLanguageSelection();

                _toolStrip.Items.AddRange(new ToolStripItem[]
                {
                    newButton, editButton, deleteButton, runButton, 
                    new ToolStripSeparator(), 
                    settingsButton, languageButton
                });
                _logger.LogAdminAction("System", "INIT", "Tool strip initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing tool strip: {ex.Message}");
                throw;
            }
        }

        private void InitializeListViewColumns()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing list view columns...");
                if (_backupListView == null)
                {
                    throw new InvalidOperationException("BackupListView is null");
                }

                // Add columns
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.progress"), 150);

                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
                _logger.LogAdminAction("System", "INIT", "List view columns initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing list view columns: {ex.Message}");
                throw;
            }
        }

        private void InitializeContextMenu()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing context menu...");
                if (_backupListView == null)
                {
                    throw new InvalidOperationException("BackupListView is null");
                }

                var contextMenu = new ContextMenuStrip();
                var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit"));
                editContextItem.Click += (s, e) => UpdateJob();

                var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete"));
                deleteContextItem.Click += (s, e) => RemoveJob();

                var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run"));
                runContextItem.Click += (s, e) => ExecuteJob();

                contextMenu.Items.AddRange(new ToolStripItem[]
                {
                    editContextItem, deleteContextItem, runContextItem
                });

                _backupListView.ContextMenuStrip = contextMenu;
                _logger.LogAdminAction("System", "INIT", "Context menu initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing context menu: {ex.Message}");
                throw;
            }
        }

        private void OnLanguageChanged(object sender, string languageCode)
        {
            if (!_isInitialized) return;

            // Update form title
            this.Text = _languageManager.GetTranslation("menu.title");

            // Update menu items
            UpdateMenuItems();
            UpdateToolStripItems();
            UpdateListViewColumns();
            UpdateContextMenu();
            UpdateStatusLabel();

            // Refresh the backup list to update translations
            RefreshBackupList();
        }

        private void UpdateMenuItems()
        {
            if (_menuStrip.Items.Count >= 6)
            {
                // File menu
                _menuStrip.Items[0].Text = _languageManager.GetTranslation("menu.file");
                if (_menuStrip.Items[0] is ToolStripMenuItem fileMenu && fileMenu.DropDownItems.Count > 0)
                {
                    fileMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.file.exit");
                }

                // Backup menu
                _menuStrip.Items[1].Text = _languageManager.GetTranslation("menu.backup");
                if (_menuStrip.Items[1] is ToolStripMenuItem backupMenu && backupMenu.DropDownItems.Count >= 4)
                {
                    backupMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.backup.create");
                    backupMenu.DropDownItems[1].Text = _languageManager.GetTranslation("menu.backup.edit");
                    backupMenu.DropDownItems[2].Text = _languageManager.GetTranslation("menu.backup.delete");
                    backupMenu.DropDownItems[3].Text = _languageManager.GetTranslation("menu.backup.run");
                }

                // Settings menu
                _menuStrip.Items[2].Text = _languageManager.GetTranslation("menu.settings");
                if (_menuStrip.Items[2] is ToolStripMenuItem settingsMenu && settingsMenu.DropDownItems.Count > 0)
                {
                    settingsMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.settings.open");
                }

                // Language menu
                _menuStrip.Items[3].Text = _languageManager.GetTranslation("menu.language");
                if (_menuStrip.Items[3] is ToolStripMenuItem languageMenu && languageMenu.DropDownItems.Count > 0)
                {
                    languageMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.language.change");
                }

                // View menu
                _menuStrip.Items[4].Text = _languageManager.GetTranslation("menu.view");
                if (_menuStrip.Items[4] is ToolStripMenuItem viewMenu && viewMenu.DropDownItems.Count > 0)
                {
                    viewMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.view.logs");
                }

                // Help menu
                _menuStrip.Items[5].Text = _languageManager.GetTranslation("menu.help");
            }
        }

        private void UpdateToolStripItems()
        {
            if (_toolStrip.Items.Count >= 7)
            {
                _toolStrip.Items[0].Text = _languageManager.GetTranslation("menu.new");
                _toolStrip.Items[1].Text = _languageManager.GetTranslation("menu.edit");
                _toolStrip.Items[2].Text = _languageManager.GetTranslation("menu.delete");
                _toolStrip.Items[3].Text = _languageManager.GetTranslation("menu.run");
                _toolStrip.Items[5].Text = _languageManager.GetTranslation("menu.settings");
                _toolStrip.Items[6].Text = _languageManager.GetTranslation("menu.language");
            }
        }

        private void UpdateListViewColumns()
        {
            if (_backupListView.Columns.Count >= 6)
            {
                _backupListView.Columns[0].Text = _languageManager.GetTranslation("backup.name");
                _backupListView.Columns[1].Text = _languageManager.GetTranslation("backup.source");
                _backupListView.Columns[2].Text = _languageManager.GetTranslation("backup.destination");
                _backupListView.Columns[3].Text = _languageManager.GetTranslation("backup.type");
                _backupListView.Columns[4].Text = _languageManager.GetTranslation("backup.status");
                _backupListView.Columns[5].Text = _languageManager.GetTranslation("backup.progress");
            }
        }

        private void UpdateContextMenu()
        {
            if (_backupListView.ContextMenuStrip != null && _backupListView.ContextMenuStrip.Items.Count >= 3)
            {
                _backupListView.ContextMenuStrip.Items[0].Text = _languageManager.GetTranslation("menu.edit");
                _backupListView.ContextMenuStrip.Items[1].Text = _languageManager.GetTranslation("menu.delete");
                _backupListView.ContextMenuStrip.Items[2].Text = _languageManager.GetTranslation("menu.run");
            }
        }

        private void UpdateStatusLabel()
        {
            if (_statusLabel.Text == "Ready" || _statusLabel.Text == "Prêt")
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
            }
        }

        private void OnTranslationsReloaded(object sender, EventArgs e)
        {
            // Call OnLanguageChanged with the current language
            OnLanguageChanged(sender, _languageManager.GetCurrentLanguage());
        }

        private void UpdateBackupStates()
        {
            // Update backup states in the list view
            List<Backup> backups = _backupController.GetBackups();

            foreach (Backup backup in backups)
            {
                // Find corresponding list view item
                foreach (ListViewItem item in _backupListView.Items)
                {
                    if (item.Text == backup.Name)
                    {
                        // Update status and progress
                        var state = _backupController.GetBackup(backup.Name);
                        if (state != null)
                        {
                            item.SubItems[4].Text = GetStatusText(state.Status);
                            // Further update progress if needed
                        }
                        break;
                    }
                }
            }
        }

        private string GetStatusText(string status)
        {
            switch (status?.ToLower() ?? "pending")
            {
                case "active":
                    return _languageManager.GetTranslation("backup.state.running");
                case "completed":
                    return _languageManager.GetTranslation("backup.state.completed");
                case "error":
                    return _languageManager.GetTranslation("backup.state.error");
                case "pending":
                default:
                    return _languageManager.GetTranslation("backup.state.waiting");
            }
        }

        private void RefreshBackupList()
        {
            _backupListView.Items.Clear();

            // Get all backups
            List<Backup> backups = _backupController.GetBackups();

            // Add each backup to the list view
            foreach (Backup backup in backups)
            {
                var item = new ListViewItem(backup.Name);
                item.SubItems.Add(backup.SourcePath);
                item.SubItems.Add(backup.TargetPath);
                item.SubItems.Add(backup.Type);
                item.SubItems.Add(GetStatusText("pending")); // Default status
                item.SubItems.Add("0%"); // Default progress
                item.Tag = backup; // Store backup object for reference

                _backupListView.Items.Add(item);
            }
        }

        private void AddJob()
        {
            using (var form = new BackupForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshBackupList();
                }
            }
        }

        private void UpdateJob()
        {
            if (_backupListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.notFound"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var selectedItem = _backupListView.SelectedItems[0];
            var backup = selectedItem.Tag as Backup;

            if (backup != null)
            {
                using (var form = new BackupForm(backup))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        RefreshBackupList();
                    }
                }
            }
        }

        private void RemoveJob()
        {
            if (_backupListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.notFound"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var selectedItem = _backupListView.SelectedItems[0];
            var backup = selectedItem.Tag as Backup;

            if (backup != null)
            {
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("message.confirmDelete"),
                    _languageManager.GetTranslation("message.confirmDeleteTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    _backupController.DeleteBackup(backup.Name);
                    RefreshBackupList();
                }
            }
        }

        private async void ExecuteJob()
        {
            if (_backupListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.notFound"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Check if a business software is running
            if (_settingsController.IsBusinessSoftwareRunning())
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.businessSoftwareRunning"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            var selectedItem = _backupListView.SelectedItems[0];
            var backup = selectedItem.Tag as Backup;

            if (backup != null)
            {
                try
                {
                    // Update UI to show job is running
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                    _progressBar.Visible = true;

                    // Start the backup
                    await _backupController.StartBackup(backup.Name);

                    // Update UI when done
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                    _progressBar.Visible = false;

                    // Show success message
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.backupSuccess"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    // Update UI for error state
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                    _progressBar.Visible = false;

                    // Show error message
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                finally
                {
                    // Refresh the backup list to update statuses
                    RefreshBackupList();
                }
            }
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Settings were saved
                    UpdateStatus(_languageManager.GetTranslation("Settings saved"));
                }
            }
        }

        private void ViewLogs()
        {
            _backupController.DisplayLogs();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Form closing...");
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("message.confirmExit"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.No)
                {
                    _logger.LogAdminAction("System", "INIT", "Form closing cancelled by user");
                    e.Cancel = true;
                    return;
                }

                // Clean up and stop timer
                _updateTimer.Stop();
                _logger.LogAdminAction("System", "INIT", "Timer stopped");

                base.OnFormClosing(e);
                _logger.LogAdminAction("System", "INIT", "Form closed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error during form closing: {ex.Message}");
                MessageBox.Show(
                    $"Error during form closing: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}