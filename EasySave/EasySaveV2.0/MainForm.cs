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
        private ListView _backupListView;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ProgressBar _progressBar;
        private System.Windows.Forms.Timer _updateTimer;
        private MenuStrip _menuStrip;
        private ToolStrip _toolStrip;

        public MainForm()
        {
            InitializeComponent();
            _backupController = new BackupController();
            _settingsController = new SettingsController();
            _languageManager = LanguageManager.Instance;
            InitializeTimer();
            InitializeUI();

            // Subscribe to language events
            _languageManager.LanguageChanged += OnLanguageChanged;
            _languageManager.TranslationsReloaded += OnTranslationsReloaded;

            // Check if it's the first run, show language selection
            if (string.IsNullOrEmpty(_languageManager.CurrentLanguage))
            {
                using (var languageForm = new LanguageSelectionForm())
                {
                    languageForm.ShowDialog();
                }
            }

            // Load backup list
            RefreshBackupList();
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000; // Update every second
            _updateTimer.Tick += (sender, e) => UpdateBackupStates();
            _updateTimer.Start();
        }

        private void InitializeUI()
        {
            // Set form properties
            this.Size = new Size(1024, 768);
            this.Text = _languageManager.GetTranslation("menu.title");
            this.Icon = SystemIcons.Application;

            // Create menu strip
            _menuStrip = new MenuStrip();
            this.MainMenuStrip = _menuStrip;
            _menuStrip.Dock = DockStyle.Top;

            // File menu
            var fileMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file"));
            var exitItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file.exit"));
            exitItem.Click += (sender, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitItem);

            // Backup menu
            var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup"));
            var createBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.create"));
            createBackupItem.Click += (sender, e) => AddJob();

            var editBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.edit"));
            editBackupItem.Click += (sender, e) => UpdateJob();

            var deleteBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.delete"));
            deleteBackupItem.Click += (sender, e) => RemoveJob();

            var runBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.run"));
            runBackupItem.Click += (sender, e) => ExecuteJob();

            backupMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                createBackupItem, editBackupItem, deleteBackupItem, runBackupItem
            });

            // Settings menu
            var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings"));
            var openSettingsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings.open"));
            openSettingsItem.Click += (sender, e) => OpenSettings();
            settingsMenu.DropDownItems.Add(openSettingsItem);

            // View menu
            var viewMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view"));
            var viewLogsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view.logs"));
            viewLogsItem.Click += (sender, e) => ViewLogs();
            viewMenu.DropDownItems.Add(viewLogsItem);

            // Help menu
            var helpMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.help"));

            // Add all menus to menu strip
            _menuStrip.Items.AddRange(new ToolStripItem[]
            {
                fileMenu, backupMenu, settingsMenu, viewMenu, helpMenu
            });

            // Create toolbar
            _toolStrip = new ToolStrip();
            _toolStrip.Dock = DockStyle.Top;

            var newButton = new ToolStripButton(_languageManager.GetTranslation("menu.new"));
            newButton.Click += (sender, e) => AddJob();

            var editButton = new ToolStripButton(_languageManager.GetTranslation("menu.edit"));
            editButton.Click += (sender, e) => UpdateJob();

            var deleteButton = new ToolStripButton(_languageManager.GetTranslation("menu.delete"));
            deleteButton.Click += (sender, e) => RemoveJob();

            var runButton = new ToolStripButton(_languageManager.GetTranslation("menu.run"));
            runButton.Click += (sender, e) => ExecuteJob();

            var settingsButton = new ToolStripButton(_languageManager.GetTranslation("menu.settings"));
            settingsButton.Click += (sender, e) => OpenSettings();

            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                newButton, editButton, deleteButton, runButton, new ToolStripSeparator(), settingsButton
            });

            // Create backup list view
            _backupListView = new ListView();
            _backupListView.Dock = DockStyle.Fill;
            _backupListView.View = View.Details;
            _backupListView.FullRowSelect = true;
            _backupListView.GridLines = true;
            _backupListView.MultiSelect = false;

            // Add columns
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.progress"), 150);

            // Create status strip
            _statusStrip = new StatusStrip();
            _statusStrip.Dock = DockStyle.Bottom;

            _statusLabel = new ToolStripStatusLabel(_languageManager.GetTranslation("status.ready"));
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            _progressBar = new ProgressBar();
            _progressBar.Dock = DockStyle.Bottom;
            _progressBar.Height = 20;
            _progressBar.Visible = false;

            _statusStrip.Items.Add(_statusLabel);

            // Add all controls to form
            this.Controls.Add(_progressBar);
            this.Controls.Add(_backupListView);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_menuStrip);

            // Context menu for backup list
            var contextMenu = new ContextMenuStrip();
            var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit"));
            editContextItem.Click += (sender, e) => UpdateJob();

            var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete"));
            deleteContextItem.Click += (sender, e) => RemoveJob();

            var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run"));
            runContextItem.Click += (sender, e) => ExecuteJob();

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                editContextItem, deleteContextItem, runContextItem
            });

            _backupListView.ContextMenuStrip = contextMenu;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Update text for all UI elements
            this.Text = _languageManager.GetTranslation("menu.title");

            // Update menu items
            if (_menuStrip.Items.Count >= 5)
            {
                _menuStrip.Items[0].Text = _languageManager.GetTranslation("menu.file");
                if (_menuStrip.Items[0] is ToolStripMenuItem fileMenu && fileMenu.DropDownItems.Count > 0)
                {
                    fileMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.file.exit");
                }

                _menuStrip.Items[1].Text = _languageManager.GetTranslation("menu.backup");
                if (_menuStrip.Items[1] is ToolStripMenuItem backupMenu && backupMenu.DropDownItems.Count >= 4)
                {
                    backupMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.backup.create");
                    backupMenu.DropDownItems[1].Text = _languageManager.GetTranslation("menu.backup.edit");
                    backupMenu.DropDownItems[2].Text = _languageManager.GetTranslation("menu.backup.delete");
                    backupMenu.DropDownItems[3].Text = _languageManager.GetTranslation("menu.backup.run");
                }

                _menuStrip.Items[2].Text = _languageManager.GetTranslation("menu.settings");
                if (_menuStrip.Items[2] is ToolStripMenuItem settingsMenu && settingsMenu.DropDownItems.Count > 0)
                {
                    settingsMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.settings.open");
                }

                _menuStrip.Items[3].Text = _languageManager.GetTranslation("menu.view");
                if (_menuStrip.Items[3] is ToolStripMenuItem viewMenu && viewMenu.DropDownItems.Count > 0)
                {
                    viewMenu.DropDownItems[0].Text = _languageManager.GetTranslation("menu.view.logs");
                }

                _menuStrip.Items[4].Text = _languageManager.GetTranslation("menu.help");
            }

            // Update toolbar
            if (_toolStrip.Items.Count >= 5)
            {
                _toolStrip.Items[0].Text = _languageManager.GetTranslation("menu.new");
                _toolStrip.Items[1].Text = _languageManager.GetTranslation("menu.edit");
                _toolStrip.Items[2].Text = _languageManager.GetTranslation("menu.delete");
                _toolStrip.Items[3].Text = _languageManager.GetTranslation("menu.run");
                _toolStrip.Items[5].Text = _languageManager.GetTranslation("menu.settings");
            }

            // Update list view columns
            if (_backupListView.Columns.Count >= 6)
            {
                _backupListView.Columns[0].Text = _languageManager.GetTranslation("backup.name");
                _backupListView.Columns[1].Text = _languageManager.GetTranslation("backup.source");
                _backupListView.Columns[2].Text = _languageManager.GetTranslation("backup.destination");
                _backupListView.Columns[3].Text = _languageManager.GetTranslation("backup.type");
                _backupListView.Columns[4].Text = _languageManager.GetTranslation("backup.status");
                _backupListView.Columns[5].Text = _languageManager.GetTranslation("backup.progress");
            }

            // Update context menu
            if (_backupListView.ContextMenuStrip != null && _backupListView.ContextMenuStrip.Items.Count >= 3)
            {
                _backupListView.ContextMenuStrip.Items[0].Text = _languageManager.GetTranslation("menu.edit");
                _backupListView.ContextMenuStrip.Items[1].Text = _languageManager.GetTranslation("menu.delete");
                _backupListView.ContextMenuStrip.Items[2].Text = _languageManager.GetTranslation("menu.run");
            }

            // Update status label if in ready state
            if (_statusLabel.Text == "Ready" || _statusLabel.Text == "Prêt")
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.ready");
            }
        }

        private void OnTranslationsReloaded(object sender, EventArgs e)
        {
            // Same as language changed
            OnLanguageChanged(sender, e);
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
            using (var form = new SettingsForm(_settingsController))
            {
                form.ShowDialog();
            }
        }

        private void ViewLogs()
        {
            _backupController.DisplayLogs();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var result = MessageBox.Show(
                _languageManager.GetTranslation("message.confirmExit"),
                _languageManager.GetTranslation("menu.title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            // Clean up and stop timer
            _updateTimer.Stop();

            base.OnFormClosing(e);
        }
    }
}