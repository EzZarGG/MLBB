﻿using EasySaveV3._0.Controllers;
using EasySaveV3._0.Models;
using EasySaveV3._0.Views;
using EasySaveV3._0.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EasySaveLogging;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace EasySaveV3._0
{
    public partial class MainForm : Form
    {
        private readonly BackupController _backupController;
        private readonly SettingsController _settingsController;
        private readonly LanguageManager _languageManager;
        private readonly Logger _logger;
        private readonly SocketServerManager _socketServerManager;
        private bool _isInitialized = false;
        private ToolStripMenuItem? _editBackupMenuItem;
        private ToolStripMenuItem? _deleteBackupMenuItem;
        private ToolStripMenuItem? _runBackupMenuItem;
        private readonly Dictionary<string, ListViewItem> _backupItems;
        private const int UPDATE_INTERVAL_MS = 1000;
        private bool _isUpdating = false;

        public MainForm()
        {
            try
            {
                // Initialize collections
                _backupItems = new Dictionary<string, ListViewItem>(StringComparer.OrdinalIgnoreCase);

                // Set up global exception handling
                Application.ThreadException += (s, e) => HandleException(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleException(e.ExceptionObject as Exception);

                // Initialize managers and controllers using singleton pattern
                _languageManager = LanguageManager.Instance;
                _settingsController = SettingsController.Instance;
                _backupController = new BackupController();
                _logger = Logger.GetInstance();
                _socketServerManager = new SocketServerManager(_backupController);
             

               
                _backupController.BusinessSoftwareDetected += OnBusinessSoftwareDetected;
                _backupController.BusinessSoftwareResumed += OnBusinessSoftwareResumed;


               
                

                // Subscribe to business software events
                _backupController.BusinessSoftwareDetected += OnBusinessSoftwareDetected;
                _backupController.BusinessSoftwareResumed += OnBusinessSoftwareResumed;

                // Subscribe to events
                _backupController.FileProgressChanged += OnFileProgressChanged;
                _backupController.EncryptionProgressChanged += OnEncryptionProgressChanged;
                _languageManager.LanguageChanged += OnLanguageChanged;
                _languageManager.TranslationsReloaded += OnTranslationsReloaded;

                // Initialize UI
                InitializeComponent();
                InitializeUI();
                InitializeTimer();
                _updateTimer.Start();
                // Start the socket server
                _ = StartSocketServer();

                // Load initial data
                LoadInitialData();

                _isInitialized = true;
                UpdateStatus(_languageManager.GetTranslation("status.ready"));
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void DebugLog(string message)
        {
            Debug.WriteLine($"[EasySave Debug] {message}");
        }

        private async Task StartSocketServer()
        {
            try
            {
                DebugLog("Attempting to start socket server...");
                await _socketServerManager.StartServer();
                DebugLog("Socket server started successfully");
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to start socket server: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("error.socketServerStartFailed").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("error.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void StopSocketServer()
        {
            try
            {
                DebugLog("Attempting to stop socket server...");
                _socketServerManager.StopServer();
                DebugLog("Socket server stopped successfully");
            }
            catch (Exception ex)
            {
                DebugLog($"Error stopping socket server: {ex.Message}");
                MessageBox.Show(
                    _languageManager.GetTranslation("error.socketServerStopFailed").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("error.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void HandleException(Exception? ex)
        {
            if (ex == null) return;

            MessageBox.Show(
                _languageManager.GetTranslation("error.unhandled").Replace("{0}", ex.Message),
                _languageManager.GetTranslation("error.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private void LoadInitialData()
        {
            try
            {
                UpdateStatus(_languageManager.GetTranslation("status.loading"));
                RefreshBackupList();
                UpdateMenuItemsState();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void InitializeTimer()
        {
            _updateTimer.Interval = UPDATE_INTERVAL_MS;
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isUpdating)
            {
                _ = UpdateBackupStatesAsync();
            }
        }

        private async Task UpdateBackupStatesAsync()
        {
            
            if (_isUpdating)
                return;

            _isUpdating = true;
            try
            {
                var backups = _backupController.GetBackups();
                if (backups == null || backups.Count == 0)
                    return;

                foreach (var job in backups)
                {
                    StateModel? state = _backupController.GetBackupState(job.Name);
                    if (state == null)
                        continue;

                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            switch (state.Status)
                            {
                                case JobStatus.Active:
                                    _progressBar.Visible = true;
                                    _progressBar.Value = state.ProgressPercentage;

                                    _statusLabel.Text = _languageManager.GetTranslation(
                                        "message.jobProgress", job.Name, state.ProgressPercentage);
                                    break;

                                case JobStatus.Paused:
                                    _progressBar.Visible = false;
                                    _statusLabel.Text = _languageManager.GetTranslation(
                                        "message.backupPaused", job.Name);
                                    break;

                                case JobStatus.Completed:
                                    _progressBar.Visible = false;
                                    _statusLabel.Text = _languageManager.GetTranslation(
                                        "message.backupComplete", job.Name);
                                    break;

                                case JobStatus.Error:
                                    _progressBar.Visible = false;
                                    _statusLabel.Text = _languageManager.GetTranslation(
                                        "message.backupError", job.Name);
                                    break;
                            }
                        }));
                    }
                    else
                    {
                        switch (state.Status)
                        {
                            case JobStatus.Active:
                                _progressBar.Visible = true;
                                _progressBar.Value = state.ProgressPercentage;
                                _statusLabel.Text = _languageManager.GetTranslation(
                                    "message.jobProgress", job.Name, state.ProgressPercentage);
                                break;

                            case JobStatus.Paused:
                                _progressBar.Visible = false;
                                _statusLabel.Text = _languageManager.GetTranslation(
                                    "message.backupPaused", job.Name);
                                break;

                            case JobStatus.Completed:
                                _progressBar.Visible = false;
                                _statusLabel.Text = _languageManager.GetTranslation(
                                    "message.backupComplete", job.Name);
                                break;

                            case JobStatus.Error:
                                _progressBar.Visible = false;
                                _statusLabel.Text = _languageManager.GetTranslation(
                                    "message.backupError", job.Name);
                                break;
                        }

                    }
                }

                foreach (var item in _backupItems.Values)
                {
                    if (item.Tag is Backup backup)
                    {
                        StateModel? state = _backupController.GetBackupState(backup.Name);
                        if (state == null)
                            continue;

                        if (state.Status == JobStatus.Completed || state.Status == JobStatus.Error)
                            continue;

                        UpdateBackupItem(item, state);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                _isUpdating = false;
            }
        }


        private void UpdateBackupItem(ListViewItem item, StateModel state)
        {
            if (item.SubItems.Count < 6) return;

            item.SubItems[4].Text = state.Status;
            item.SubItems[5].Text = $"{state.ProgressPercentage}%";

            item.BackColor = state.Status switch
            {
                JobStatus.Active => Color.LightGreen,
                JobStatus.Paused => Color.LightYellow,
                JobStatus.Error => Color.LightPink,
                JobStatus.Completed => Color.LightBlue,
                JobStatus.Stopped => Color.LightGray,
                JobStatus.Cancelled => Color.LightGray,
                _ => SystemColors.Window
            };

            if (state.Status == JobStatus.Active || state.Status == JobStatus.Completed)
            {
                _progressBar.Value = Math.Min(Math.Max(state.ProgressPercentage, 0), 100);
                _progressBar.Visible = true;
            }
            else if (state.Status == JobStatus.Paused)
            {
                _progressBar.Visible = true; // Keep progress bar visible when paused
            }
            else
            {
                _progressBar.Visible = false;
            }

            // Update status label based on job status
            if (_statusLabel != null)
            {
                _statusLabel.Text = state.Status switch
                {
                    JobStatus.Ready => _languageManager.GetTranslation("status.ready"),
                    JobStatus.Active => _languageManager.GetTranslation("status.backupInProgress"),
                    JobStatus.Completed => _languageManager.GetTranslation("status.backupComplete"),
                    JobStatus.Error => _languageManager.GetTranslation("status.backupError"),
                    JobStatus.Paused => _languageManager.GetTranslation("status.backupPaused"),
                    JobStatus.Stopped => _languageManager.GetTranslation("status.backupStopped"),
                    JobStatus.Cancelled => _languageManager.GetTranslation("status.backupStopped"), // Use stopped translation for cancelled
                    _ => _languageManager.GetTranslation("status.ready")
                };
            }
        }

        private void RefreshBackupList()
        {
            try
            {
                _backupListView.BeginUpdate();
                _backupListView.Items.Clear();
                _backupItems.Clear();

                var backups = _backupController.GetBackups();
                foreach (var backup in backups)
                {
                    var item = CreateBackupListItem(backup);
                    _backupListView.Items.Add(item);
                    _backupItems[backup.Name] = item;
                }
            }
            finally
            {
                _backupListView.EndUpdate();
            }
        }

        private ListViewItem CreateBackupListItem(Backup backup)
        {
            var item = new ListViewItem(backup.Name) { Tag = backup };
            var state = _backupController.GetBackupState(backup.Name);
            item.SubItems.AddRange(new[]
            {
                backup.SourcePath,
                backup.TargetPath,
                backup.Type,
                state?.Status ?? "Ready",
                $"{state?.ProgressPercentage ?? 0}%"
            });
            return item;
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
            }
            catch (Exception)
            {
                // Ignore status update errors
            }
        }

        private void ShowLanguageSelection()
        {
            try
            {
                UpdateStatus("Opening language selection...");
                using (var languageForm = new LanguageSelectionForm(false))
                {
                    if (languageForm.ShowDialog() == DialogResult.OK)
                    {
                        UpdateStatus("Initializing interface...");
                        InitializeApplication();
                    }
                    else
                    {
                        UpdateStatus("Language selection cancelled");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during language selection: {ex.Message}",
                    "Language Selection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void InitializeApplication()
        {
            try
            {
                if (_isInitialized)
                {
                    return;
                }

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
                UpdateStatus(_languageManager?.GetTranslation("status.ready") ?? "Ready");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during application initialization: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Application.Exit();
            }
        }

        private void InitializeUI()
        {
            try
            {
                if (_backupListView == null)
                {
                    throw new InvalidOperationException("BackupListView is null");
                }

                // Initialize all UI components
                InitializeMenuStrip();
                InitializeListView();
                InitializeStatusStrip();
                InitializeContextMenu();

                // Set form properties
                this.Text = _languageManager.GetTranslation("menu.title");
                this.Icon = SystemIcons.Application;
                this.StartPosition = FormStartPosition.CenterScreen;

                // Remove the ToolStrip from the form
                if (_toolStrip != null)
                {
                    this.Controls.Remove(_toolStrip);
                    _toolStrip.Dispose();
                    _toolStrip = null;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize UI: " + ex.Message, ex);
            }
        }

        private void InitializeMenuStrip()
        {
            try
            {
                // Backup menu
                var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup")) 
                { 
                    Tag = "menu.backup",
                    Image = SystemIcons.Shield.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit
                };
                
                // Create backup item (always enabled)
                var createBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.create")) 
                { 
                    Tag = "menu.backup.create",
                    Image = SystemIcons.Information.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    ShortcutKeys = Keys.Control | Keys.N
                };
                createBackupItem.Click += (s, e) => AddJob();

                // Run all backups item (enabled if there are backups)
                var runAllBackupsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.runAll")) 
                { 
                    Tag = "menu.backup.runAll",
                    Image = SystemIcons.Application.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Enabled = _backupListView.Items.Count > 0,
                    ShortcutKeys = Keys.Control | Keys.R
                };
                runAllBackupsItem.Click += (s, e) => ExecuteAllJobs();

                // Run backup item (disabled by default)
                var runBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.run")) 
                { 
                    Tag = "menu.backup.run",
                    Image = SystemIcons.WinLogo.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Enabled = false,
                    ShortcutKeys = Keys.F5
                };
                runBackupItem.Click += (s, e) => ExecuteJob();

                // Edit backup item (disabled by default)
                var editBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.edit")) 
                { 
                    Tag = "menu.backup.edit",
                    Image = SystemIcons.Exclamation.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Enabled = false,
                    ShortcutKeys = Keys.F2
                };
                editBackupItem.Click += (s, e) => UpdateJob();

                // Delete backup item (disabled by default)
                var deleteBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.delete")) 
                { 
                    Tag = "menu.backup.delete",
                    Image = SystemIcons.Error.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Enabled = false,
                    ShortcutKeys = Keys.Delete
                };
                deleteBackupItem.Click += (s, e) => RemoveJob();

                // Add items to menu with separators
                backupMenu.DropDownItems.AddRange(new ToolStripItem[] { 
                    createBackupItem,
                    new ToolStripSeparator(),
                    runBackupItem,
                    runAllBackupsItem,
                    new ToolStripSeparator(),
                    editBackupItem,
                    deleteBackupItem
                });

                // Settings menu
                var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings")) 
                { 
                    Tag = "menu.settings",
                    Image = SystemIcons.Application.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    ShortcutKeys = Keys.F4
                };
                settingsMenu.Click += (s, e) => OpenSettings();

                // Language menu
                var languageMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.language")) 
                { 
                    Tag = "menu.language",
                    Image = SystemIcons.Question.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    ShortcutKeys = Keys.F3
                };
                languageMenu.Click += (s, e) => ShowLanguageSelection();

                // Logs menu
                var logsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view.logs")) 
                { 
                    Tag = "menu.view.logs",
                    Image = SystemIcons.Warning.ToBitmap(),
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    ShortcutKeys = Keys.F6
                };
                logsMenu.Click += (s, e) => ViewLogs();

                // Cleanup menu
                _menuStrip.Items.Clear();
                _menuStrip.Items.AddRange(new ToolStripItem[] { 
                    backupMenu, 
                    new ToolStripSeparator(),
                    settingsMenu, 
                    languageMenu, 
                    logsMenu 
                });

                // Store menu item references for later updates
                _editBackupMenuItem = editBackupItem;
                _deleteBackupMenuItem = deleteBackupItem;
                _runBackupMenuItem = runBackupItem;

                // Configure menu appearance
                _menuStrip.Dock = DockStyle.Top;
                _menuStrip.ShowItemToolTips = true;
                _menuStrip.RenderMode = ToolStripRenderMode.Professional;
                _menuStrip.BackColor = SystemColors.Control;
                _menuStrip.ForeColor = SystemColors.ControlText;
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void OnBusinessSoftwareDetected(object? sender, string jobName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBusinessSoftwareDetected(sender, jobName)));
                return;
            }

            _progressBar.Visible = false;
            _statusLabel.Text = _languageManager.GetTranslation("message.backupPaused", jobName);

            MessageBox.Show(
                _languageManager.GetTranslation("message.backupPaused", jobName),
                _languageManager.GetTranslation("error.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private void OnBusinessSoftwareResumed(object? sender, string jobName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBusinessSoftwareResumed(sender, jobName)));
                return;
            }

            _progressBar.Visible = true;
            _statusLabel.Text = _languageManager.GetTranslation("message.backupResumed", jobName);
        }


        private void InitializeListView()
        {
            try
            {
                // Cleanup: avoid duplicate columns
                _backupListView.Columns.Clear();
                _backupListView.Columns.AddRange(new[]
                {
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.name"), Tag = "backup.name", Width = 150 },
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.source"), Tag = "backup.source", Width = 200 },
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.target"), Tag = "backup.target", Width = 200 },
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.type"), Tag = "backup.type", Width = 100 },
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.status"), Tag = "backup.status", Width = 100 },
                    new ColumnHeader { Text = _languageManager.GetTranslation("backup.progress"), Tag = "backup.progress", Width = 150 }
                });

                // Enable multi-select
                _backupListView.MultiSelect = true;
                _backupListView.SelectedIndexChanged += (s, e) => UpdateMenuItemsState();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void InitializeStatusStrip()
        {
            try
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void InitializeContextMenu()
        {
            try
            {
                var contextMenu = new ContextMenuStrip();

                var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit")) { Tag = "menu.edit" };
                editContextItem.Click += (s, e) => UpdateJob();

                var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete")) { Tag = "menu.delete" };
                deleteContextItem.Click += (s, e) => RemoveJob();

                var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run")) { Tag = "menu.run" };
                runContextItem.Click += (s, e) => ExecuteJob();

                contextMenu.Items.AddRange(new ToolStripItem[] { editContextItem, deleteContextItem, runContextItem });
                _backupListView.ContextMenuStrip = contextMenu;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void OnLanguageChanged(object? sender, string languageCode)
        {
            if (!_isInitialized) return;
            
            // Update form title
            this.Text = _languageManager.GetTranslation("menu.title");

            // Update all UI elements
            UpdateAllTexts();
            RefreshBackupList();
        }

        private void OnTranslationsReloaded(object? sender, EventArgs e)
        {
            OnLanguageChanged(sender, _languageManager.CurrentLanguage);
        }

        private void UpdateAllTexts()
        {
            try
            {
                // Update menu items
                UpdateMenuItems(_menuStrip.Items);

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
            catch (Exception)
            {
                // Ignore text update errors
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

        private void UpdateMenuItemsState()
        {
            bool hasBackups = _backupListView.Items.Count > 0;
            bool hasSelection = _backupListView.SelectedItems.Count > 0;
            bool isActive = false;
            bool isPaused = false;

            if (hasSelection)
            {
                var selectedItem = _backupListView.SelectedItems[0];
                var state = _backupController.GetBackupState(selectedItem.Text);
                if (state != null)
                {
                    isActive = state.Status == "Active";
                    isPaused = state.Status == "Paused";
                }
            }

            // Update menu items
            if (_editBackupMenuItem != null)
                _editBackupMenuItem.Enabled = hasSelection && _backupListView.SelectedItems.Count == 1;
            if (_deleteBackupMenuItem != null)
                _deleteBackupMenuItem.Enabled = hasSelection;
            if (_runBackupMenuItem != null)
                _runBackupMenuItem.Enabled = hasSelection;

            // Update control buttons
            _pauseButton.Enabled = hasSelection && isActive;
            _resumeButton.Enabled = hasSelection && isPaused;
            _stopButton.Enabled = hasSelection && (isActive || isPaused);

            // Update "Run All" menu item
            foreach (ToolStripItem item in _menuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Tag?.ToString() == "menu.backup")
                {
                    foreach (ToolStripItem subItem in menuItem.DropDownItems)
                    {
                        if (subItem.Tag?.ToString() == "menu.backup.runAll")
                        {
                            subItem.Enabled = hasBackups;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private string GetStatusText(string status)
        {
            return status.ToLower() switch
            {
                "ready" => _languageManager.GetTranslation("status.ready"),
                "active" => _languageManager.GetTranslation("status.active"),
                "completed" => _languageManager.GetTranslation("status.completed"),
                "error" => _languageManager.GetTranslation("status.error"),
                "paused" => _languageManager.GetTranslation("status.paused"),
                _ => _languageManager.GetTranslation("status.ready")
            };
        }

        private void AddJob()
        {
            try
            {
                using (var form = new BackupForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        var backup = form.Backup;
                        _backupController.CreateBackup(backup.Name, backup.SourcePath, backup.TargetPath, backup.Type);
                        RefreshBackupList();
                    }
                }
            }
            catch (Exception ex)
            {
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
                            RefreshBackupList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                        RefreshBackupList();
                    }
                }
            }
            catch (Exception ex)
            {
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
                DebugLog("Starting backup job execution...");

                if (_backupController.IsCryptoSoftRunning())
                {
                    DebugLog("Cannot start backup: CryptoSoft is already running");
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.cryptoSoftAlreadyRunning"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning
                    );
                    return;
                }

                var selectedItems = _backupListView.SelectedItems;
                if (selectedItems.Count == 0)
                {
                    DebugLog("No backups selected for execution");
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.noBackupsSelected"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                var result = MessageBox.Show(
                    _languageManager.GetTranslation("message.confirmRunSelected").Replace("{0}", selectedItems.Count.ToString()),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    DebugLog($"Starting {selectedItems.Count} selected backups");
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                    _progressBar.Visible = true;

                    var selectedBackups = selectedItems.Cast<ListViewItem>()
                        .Select(item => item.Text)
                        .ToList();

                    // Update state to Active immediately for all selected backups
                    foreach (var backupName in selectedBackups)
                    {
                        if (_backupItems.TryGetValue(backupName, out var item))
                        {
                             item.SubItems[4].Text = JobStatus.Active; // Use constant
                             item.BackColor = Color.LightGreen;
                        }
                    }

                    await _backupController.StartSelectedBackups(selectedBackups);

                    DebugLog("Selected backups execution finished.");
                    
                    // Check the final status of the first selected backup to determine the message
                    var firstBackupName = selectedBackups.FirstOrDefault();
                    var finalState = firstBackupName != null ? _backupController.GetBackupState(firstBackupName) : null;

                    if (finalState?.Status == JobStatus.Stopped || finalState?.Status == JobStatus.Cancelled)
                    {
                         _statusLabel.Text = _languageManager.GetTranslation("status.backupStopped");
                         _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.backupStoppedSuccess"),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else if (finalState?.Status == JobStatus.Completed)
                    {
                        _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                        _progressBar.Visible = false;
                        MessageBox.Show(
                            _languageManager.GetTranslation("message.selectedBackupsSuccess"),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else if (finalState?.Status == JobStatus.Error)
                    {
                         _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                         _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.error").Replace("{0}", _languageManager.GetTranslation("message.backupError")),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    else // Handle other potential final states or no selection
                    {
                        _statusLabel.Text = _languageManager.GetTranslation("status.ready");
                        _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.error").Replace("{0}", "Backup did not complete as expected."),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error during backup execution: {ex.Message}");
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

        private async void ExecuteAllJobs()
        {
            try
            {
                DebugLog("Starting all backup jobs...");

                if (_backupController.IsCryptoSoftRunning())
                {
                    DebugLog("Cannot start backups: CryptoSoft is already running");
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.cryptoSoftAlreadyRunning"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning
                    );
                    return;
                }

                if (_backupListView.Items.Count == 0)
                {
                    DebugLog("No backups available to execute");
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.noBackups"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                var result = MessageBox.Show(
                    _languageManager.GetTranslation("message.confirmRunAll"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    DebugLog("Starting all backups");
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                    _progressBar.Visible = true;

                    // Update state to Active immediately for all backups
                    foreach (ListViewItem item in _backupListView.Items)
                    {
                        item.SubItems[4].Text = JobStatus.Active; // Use constant
                        item.BackColor = Color.LightGreen;
                    }

                    await _backupController.StartAllBackups();

                    DebugLog("All backups execution finished.");

                     // Check the final status of the first backup to determine the message
                    var firstBackupName = _backupListView.Items.Cast<ListViewItem>().FirstOrDefault()?.Text;
                    var finalState = firstBackupName != null ? _backupController.GetBackupState(firstBackupName) : null;

                     if (finalState?.Status == JobStatus.Stopped || finalState?.Status == JobStatus.Cancelled)
                    {
                         _statusLabel.Text = _languageManager.GetTranslation("status.backupStopped");
                         _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.backupStoppedSuccess"),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else if (finalState?.Status == JobStatus.Completed)
                    {
                        _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                        _progressBar.Visible = false;
                        MessageBox.Show(
                            _languageManager.GetTranslation("message.allBackupsSuccess"),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else if (finalState?.Status == JobStatus.Error)
                    {
                         _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                         _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.error").Replace("{0}", _languageManager.GetTranslation("message.backupError")),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                     else // Handle other potential final states or no backups
                    {
                        _statusLabel.Text = _languageManager.GetTranslation("status.ready");
                        _progressBar.Visible = false;
                         MessageBox.Show(
                            _languageManager.GetTranslation("message.error").Replace("{0}", "All backups did not complete as expected."),
                            _languageManager.GetTranslation("menu.title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Error during backup execution: {ex.Message}");
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
                using (var form = new SettingsForm(_languageManager, _settingsController))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        UpdateStatus(_languageManager.GetTranslation("status.settingsSaved"));
                    }
                }
            }
            catch (Exception ex)
            {
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
                _backupController.DisplayLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OnFileProgressChanged(object? sender, FileProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnFileProgressChanged(sender, e)));
                return;
            }

            if (_backupItems.TryGetValue(e.BackupName, out var item))
            {
                // Update backup state
                var state = new StateModel
                {
                    Status = e.ProgressPercentage == 100 ? "Completed" : "Active",
                    ProgressPercentage = e.ProgressPercentage
                };

                // If state is Completed, disable timer for this backup
                if (state.Status == "Completed")
                {
                    _updateTimer.Stop();
                }

                UpdateBackupItem(item, state);

                // Update progress bar
                _progressBar.Value = Math.Min(Math.Max(e.ProgressPercentage, 0), 100);
                _progressBar.Visible = state.Status == "Active";
            }
        }

        private void OnEncryptionProgressChanged(object? sender, EncryptionProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnEncryptionProgressChanged(sender, e)));
                return;
            }

            if (_backupItems.TryGetValue(e.BackupName, out var item))
            {
                UpdateBackupItem(item, new StateModel 
                { 
                    Status = "Active",
                    ProgressPercentage = e.ProgressPercentage 
                });
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DebugLog("Form is closing, cleaning up resources...");
            try
            {
                StopSocketServer();
                DebugLog("Socket server cleanup completed");

                if (IsBackupRunning())
                {
                    var result = MessageBox.Show(
                        _languageManager.GetTranslation("message.confirmExitWithBackup"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                Cleanup();
                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                DebugLog($"Error during cleanup: {ex.Message}");
            }
        }

        private bool IsBackupRunning()
        {
            return _backupItems.Values.Any(item => 
                item.SubItems.Count > 4 && 
                item.SubItems[4].Text == "Active");
        }

        private void Cleanup()
        {
            try
            {
                _updateTimer.Stop();
                _backupController.FileProgressChanged -= OnFileProgressChanged;
                _backupController.EncryptionProgressChanged -= OnEncryptionProgressChanged;
                _languageManager.LanguageChanged -= OnLanguageChanged;
                _languageManager.TranslationsReloaded -= OnTranslationsReloaded;

                // Clear priority extensions
                _settingsController.SetPriorityExtensions(new List<string>());

                if (_backupController is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void UpdateControlTexts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag is string translationKey)
                {
                    control.Text = _languageManager.GetTranslation(translationKey);
                }
                UpdateControlTexts(control.Controls);
            }
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_backupListView.SelectedItems.Count > 0)
                {
                    var selectedItem = _backupListView.SelectedItems[0];
                    var backupName = selectedItem.Text;

                    // Update UI state immediately
                    selectedItem.SubItems[4].Text = JobStatus.Paused; // Use constant
                    selectedItem.BackColor = Color.LightYellow; // Use color for paused
                    
                    _backupController.PauseBackup(backupName);
                    UpdateStatus(_languageManager.GetTranslation("status.backupPaused"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ResumeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_backupListView.SelectedItems.Count > 0)
                {
                    var selectedItem = _backupListView.SelectedItems[0];
                    _backupController.ResumeBackup(selectedItem.Text);
                    UpdateStatus(_languageManager.GetTranslation("status.backupInProgress"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (_backupListView.SelectedItems.Count > 0)
                {
                    var selectedItem = _backupListView.SelectedItems[0];
                    _backupController.StopBackup(selectedItem.Text);
                    UpdateStatus(_languageManager.GetTranslation("status.backupStopped"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}