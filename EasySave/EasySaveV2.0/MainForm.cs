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
using System.IO;

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
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    MessageBox.Show("Unhandled exception: " + (e.ExceptionObject as Exception)?.ToString());
                };

                // Initialize logger and set log file path FIRST
                _logger = Logger.GetInstance();
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);
                _logger.SetLogFilePath(Path.Combine(logDir, "application.log"));
                _logger.LogAdminAction("System", "STARTUP", "Starting application initialization...");
                UpdateStatus("Initializing application...");

                // Initialize managers and controllers
                _languageManager = LanguageManager.Instance;
                _settingsController = new SettingsController();
                _backupController = new BackupController();

                // Initialize UI components
                InitializeComponent();

                // Initialize UI elements in correct order
                InitializeUI();
                InitializeMenuStrip();
                InitializeToolStrip();
                InitializeListView();
                InitializeStatusStrip();
                InitializeContextMenu();
                InitializeTimer();

                // Set up event handlers
                if (_languageManager != null)
                {
                    _languageManager.LanguageChanged += OnLanguageChanged;
                    _languageManager.TranslationsReloaded += OnTranslationsReloaded;
                }

                // Update UI with translations
                UpdateAllTexts();

                // Initialize application state
                InitializeApplication();

                _isInitialized = true;
                _logger.LogAdminAction("System", "INIT", "MainForm initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing MainForm: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show(
                    (_languageManager?.GetTranslation("message.error")?.Replace("{0}", ex.Message) ?? ex.Message) +
                    "\n\nStackTrace:\n" + ex.StackTrace,
                    _languageManager?.GetTranslation("menu.title") ?? "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
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

                UpdateStatus("Initializing UI...");
                InitializeUI();

                UpdateStatus("Loading backup list...");
                if (_backupController != null)
                {
                    RefreshBackupList();
                }

                _isInitialized = true;
                _logger.LogAdminAction("System", "INIT", "Application initialization completed successfully");
                UpdateStatus(_languageManager?.GetTranslation("status.ready") ?? "Ready");
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
                _updateTimer.Tick += OnUpdateTimerTick;
                _updateTimer.Start();
                _logger.LogAdminAction("System", "INIT", "Timer started successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing timer: {ex.Message}");
                throw;
            }
        }

        private void OnUpdateTimerTick(object? sender, EventArgs e)
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

        private void InitializeUI()
        {
            try
            {
                if (_backupListView == null)
                {
                    _logger?.LogAdminAction("System", "ERROR", "BackupListView is null during initialization");
                    throw new InvalidOperationException("BackupListView is null");
                }

                // Initialize ListView columns
                _backupListView.Columns.Clear();
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);
                _backupListView.Columns.Add(_languageManager.GetTranslation("backup.progress"), 100);

                _logger?.LogAdminAction("System", "INIT", "UI initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing UI: {ex.Message}");
                throw;
            }
        }

        private void InitializeMenuStrip()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing menu strip...");

                // File menu
                var fileMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file")) { Tag = "menu.file" };
                var exitItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file.exit")) { Tag = "menu.file.exit" };
                exitItem.Click += (s, e) => Application.Exit();
                fileMenu.DropDownItems.Add(exitItem);

                // Backup menu
                var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup")) { Tag = "menu.backup" };
                var createBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.create")) { Tag = "menu.backup.create" };
                createBackupItem.Click += (s, e) => AddJob();
                var editBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.edit")) { Tag = "menu.backup.edit" };
                editBackupItem.Click += (s, e) => UpdateJob();
                var deleteBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.delete")) { Tag = "menu.backup.delete" };
                deleteBackupItem.Click += (s, e) => RemoveJob();
                var runBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.run")) { Tag = "menu.backup.run" };
                runBackupItem.Click += (s, e) => ExecuteJob();

                backupMenu.DropDownItems.AddRange(new[] { createBackupItem, editBackupItem, deleteBackupItem, runBackupItem });

                // Settings menu
                var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings")) { Tag = "menu.settings" };
                var openSettingsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings.open")) { Tag = "menu.settings.open" };
                openSettingsItem.Click += (s, e) => OpenSettings();
                settingsMenu.DropDownItems.Add(openSettingsItem);

                // Language menu
                var languageMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.language")) { Tag = "menu.language" };
                var changeLanguageItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.language.change")) { Tag = "menu.language.change" };
                changeLanguageItem.Click += (s, e) => ShowLanguageSelection();
                languageMenu.DropDownItems.Add(changeLanguageItem);

                // View menu
                var viewMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view")) { Tag = "menu.view" };
                var viewLogsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view.logs")) { Tag = "menu.view.logs" };
                viewLogsItem.Click += (s, e) => ViewLogs();
                viewMenu.DropDownItems.Add(viewLogsItem);

                // Help menu
                var helpMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.help")) { Tag = "menu.help" };

                _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, backupMenu, settingsMenu, languageMenu, viewMenu, helpMenu });
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

                var newButton = new ToolStripButton(_languageManager.GetTranslation("menu.new")) { Tag = "menu.new" };
                newButton.Click += (s, e) => AddJob();

                var editButton = new ToolStripButton(_languageManager.GetTranslation("menu.edit")) { Tag = "menu.edit" };
                editButton.Click += (s, e) => UpdateJob();

                var deleteButton = new ToolStripButton(_languageManager.GetTranslation("menu.delete")) { Tag = "menu.delete" };
                deleteButton.Click += (s, e) => RemoveJob();

                var runButton = new ToolStripButton(_languageManager.GetTranslation("menu.run")) { Tag = "menu.run" };
                runButton.Click += (s, e) => ExecuteJob();

                var settingsButton = new ToolStripButton(_languageManager.GetTranslation("menu.settings")) { Tag = "menu.settings" };
                settingsButton.Click += (s, e) => OpenSettings();

                var languageButton = new ToolStripButton(_languageManager.GetTranslation("menu.language")) { Tag = "menu.language" };
                languageButton.Click += (s, e) => ShowLanguageSelection();

                _toolStrip.Items.AddRange(new ToolStripItem[] {
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

        private void InitializeListView()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing list view...");

                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.name"), Tag = "backup.name", Width = 150 });
                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.source"), Tag = "backup.source", Width = 200 });
                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.destination"), Tag = "backup.destination", Width = 200 });
                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.type"), Tag = "backup.type", Width = 100 });
                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.status"), Tag = "backup.status", Width = 100 });
                _backupListView.Columns.Add(new ColumnHeader { Text = _languageManager.GetTranslation("backup.progress"), Tag = "backup.progress", Width = 150 });

                _logger.LogAdminAction("System", "INIT", "List view initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing list view: {ex.Message}");
                throw;
            }
        }

        private void InitializeStatusStrip()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing status strip...");
                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
                _logger.LogAdminAction("System", "INIT", "Status strip initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing status strip: {ex.Message}");
                throw;
            }
        }

        private void InitializeContextMenu()
        {
            try
            {
                _logger.LogAdminAction("System", "INIT", "Initializing context menu...");
                var contextMenu = new ContextMenuStrip();

                var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit")) { Tag = "menu.edit" };
                editContextItem.Click += (s, e) => UpdateJob();

                var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete")) { Tag = "menu.delete" };
                deleteContextItem.Click += (s, e) => RemoveJob();

                var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run")) { Tag = "menu.run" };
                runContextItem.Click += (s, e) => ExecuteJob();

                contextMenu.Items.AddRange(new ToolStripItem[] { editContextItem, deleteContextItem, runContextItem });
                _backupListView.ContextMenuStrip = contextMenu;

                _logger.LogAdminAction("System", "INIT", "Context menu initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error initializing context menu: {ex.Message}");
                throw;
            }
        }

        private void OnLanguageChanged(object? sender, string languageCode)
        {
            if (!_isInitialized) return;

            _logger.LogAdminAction("System", "LANGUAGE", $"Language changed to: {languageCode}");
            
            // Update form title
            this.Text = _languageManager.GetTranslation("menu.title");

            // Update all UI elements
            UpdateAllTexts();
            RefreshBackupList();
        }

        private void OnTranslationsReloaded(object? sender, EventArgs e)
        {
            _logger.LogAdminAction("System", "LANGUAGE", "Translations reloaded");
            OnLanguageChanged(sender, _languageManager.CurrentLanguage);
        }

        private void UpdateAllTexts()
        {
            try
            {
                // Update menu items
                UpdateMenuItems(_menuStrip.Items);

                // Update toolbar items
                foreach (ToolStripItem item in _toolStrip.Items)
                {
                    if (item.Tag is string key)
                        item.Text = _languageManager.GetTranslation(key);
                }

                // Update list view columns
                foreach (ColumnHeader column in _backupListView.Columns)
                {
                    if (column.Tag is string key)
                        column.Text = _languageManager.GetTranslation(key);
                }

                // Update context menu
                if (_backupListView.ContextMenuStrip != null)
                {
                    foreach (ToolStripItem item in _backupListView.ContextMenuStrip.Items)
                    {
                        if (item.Tag is string key)
                            item.Text = _languageManager.GetTranslation(key);
                    }
                }

                // Update status label
                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error updating texts: {ex.Message}");
            }
        }

        private void UpdateMenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.Tag is string key)
                    item.Text = _languageManager.GetTranslation(key);

                if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0)
                    UpdateMenuItems(menuItem.DropDownItems);
            }
        }

        private void UpdateBackupStates()
        {
            try
            {
                foreach (ListViewItem item in _backupListView.Items)
                {
                    var backup = _backupController.GetBackup(item.Text);
                    if (backup != null)
                    {
                        var state = _backupController.GetBackupState(backup.Name);
                        if (state != null)
                        {
                            item.SubItems[4].Text = GetStatusText(state.Status);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error updating backup states: {ex.Message}");
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
            try
            {
                if (_backupListView == null)
                {
                    _logger?.LogAdminAction("System", "ERROR", "BackupListView is null");
                    return;
                }

                if (_backupController == null)
                {
                    _logger?.LogAdminAction("System", "ERROR", "BackupController is null");
                    return;
                }

                if (_languageManager == null)
                {
                    _logger?.LogAdminAction("System", "ERROR", "LanguageManager is null");
                    return;
                }

                _logger.LogAdminAction("System", "UI", "Refreshing backup list");
                _backupListView.Items.Clear();

                // Initialize columns if they don't exist
                if (_backupListView.Columns.Count == 0)
                {
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);
                    _backupListView.Columns.Add(_languageManager.GetTranslation("backup.progress"), 100);
                }

                var backups = _backupController.GetBackups();
                if (backups != null && backups.Any())
                {
                    foreach (Backup backup in backups)
                    {
                        if (backup == null) continue;

                        var item = new ListViewItem(backup.Name);
                        item.SubItems.Add(backup.SourcePath);
                        item.SubItems.Add(backup.TargetPath);
                        item.SubItems.Add(backup.Type);
                        var state = _backupController.GetBackupState(backup.Name);
                        string status = state != null ? GetStatusText(state.Status) : GetStatusText("pending");
                        item.SubItems.Add(status);
                        item.SubItems.Add("0%");
                        item.Tag = backup;

                        _backupListView.Items.Add(item);
                    }
                }
                else
                {
                    _logger.LogAdminAction("System", "UI", "No backups found");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error refreshing backup list: {ex.Message}");
                MessageBox.Show(
                    _languageManager?.GetTranslation("message.error")?.Replace("{0}", ex.Message) ?? ex.Message,
                    _languageManager?.GetTranslation("menu.title") ?? "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void AddJob()
        {
            try
            {
                _logger.LogAdminAction("System", "UI", "Opening add backup form");
                using (var form = new BackupForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        var backup = form.Backup;
                        _backupController.CreateBackup(backup.Name, backup.SourcePath, backup.TargetPath, backup.Type);
                        _logger.LogAdminAction("System", "BACKUP", $"Created new backup job: {backup.Name}");
                        RefreshBackupList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error adding backup job: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void UpdateJob()
        {
            try
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

                _logger.LogAdminAction("System", "UI", "Opening edit backup form");
                var selectedItem = _backupListView.SelectedItems[0];
                var backup = selectedItem.Tag as Backup;

                if (backup != null)
                {
                    using (var form = new BackupForm(backup))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            var updated = form.Backup;
                            _backupController.EditBackup(updated.Name, updated.SourcePath, updated.TargetPath, updated.Type);
                            _logger.LogAdminAction("System", "BACKUP", $"Updated backup job: {updated.Name}");
                            RefreshBackupList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error updating backup job: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void RemoveJob()
        {
            try
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
                        _logger.LogAdminAction("System", "BACKUP", $"Deleted backup job: {backup.Name}");
                        RefreshBackupList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error removing backup job: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private async void ExecuteJob()
        {
            try
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
                    _logger.LogAdminAction("System", "BACKUP", $"Starting backup job: {backup.Name}");
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                    _progressBar.Visible = true;

                    await _backupController.StartBackup(backup.Name);

                    _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                    _progressBar.Visible = false;
                    _logger.LogAdminAction("System", "BACKUP", $"Completed backup job: {backup.Name}");

                    MessageBox.Show(
                        _languageManager.GetTranslation("message.backupSuccess"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error executing backup job: {ex.Message}");
                _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                _progressBar.Visible = false;

                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                RefreshBackupList();
            }
        }

        private void OpenSettings()
        {
            try
            {
                _logger.LogAdminAction("System", "UI", "Opening settings form");
                using (var form = new SettingsForm(_languageManager, _settingsController))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _logger.LogAdminAction("System", "SETTINGS", "Settings saved successfully");
                        UpdateStatus(_languageManager.GetTranslation("status.settingsSaved"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error opening settings: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ViewLogs()
        {
            try
            {
                _logger.LogAdminAction("System", "UI", "Opening logs");
                _backupController.DisplayLogs();
            }
            catch (Exception ex)
            {
                _logger?.LogAdminAction("System", "ERROR", $"Error viewing logs: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _logger.LogAdminAction("System", "SHUTDOWN", "Form closing...");
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("message.confirmExit"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.No)
                {
                    _logger.LogAdminAction("System", "SHUTDOWN", "Form closing cancelled by user");
                    e.Cancel = true;
                    return;
                }

                _updateTimer.Stop();
                _logger.LogAdminAction("System", "SHUTDOWN", "Timer stopped");
                base.OnFormClosing(e);
                _logger.LogAdminAction("System", "SHUTDOWN", "Form closed successfully");
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