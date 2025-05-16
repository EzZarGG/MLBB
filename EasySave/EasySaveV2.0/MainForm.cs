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

            _languageManager.LanguageChanged += OnLanguageChanged;
            _languageManager.TranslationsReloaded += OnTranslationsReloaded;

            RefreshBackupList();
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000;
            _updateTimer.Tick += (sender, e) => UpdateBackupStates();
            _updateTimer.Start();
        }

        private void InitializeUI()
        {
            this.Size = new Size(1024, 768);
            this.Text = _languageManager.GetTranslation("menu.title");
            this.Icon = SystemIcons.Application;

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
            backupMenu.DropDownItems.AddRange(new ToolStripItem[] { createBackupItem, editBackupItem, deleteBackupItem, runBackupItem });

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

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, backupMenu, settingsMenu, viewMenu, helpMenu });

            // Toolbar
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
            _toolStrip.Items.AddRange(new ToolStripItem[] { newButton, editButton, deleteButton, runButton, new ToolStripSeparator(), settingsButton });

            // ListView
            _backupListView = new ListView();
            _backupListView.Dock = DockStyle.Fill;
            _backupListView.View = View.Details;
            _backupListView.FullRowSelect = true;
            _backupListView.GridLines = true;
            _backupListView.MultiSelect = false;
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.progress"), 150);

            // Status strip
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

            // Add controls
            this.Controls.Add(_progressBar);
            this.Controls.Add(_backupListView);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_menuStrip);

            // Context menu
            var contextMenu = new ContextMenuStrip();
            var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit"));
            editContextItem.Click += (sender, e) => UpdateJob();
            var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete"));
            deleteContextItem.Click += (sender, e) => RemoveJob();
            var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run"));
            runContextItem.Click += (sender, e) => ExecuteJob();
            contextMenu.Items.AddRange(new ToolStripItem[] { editContextItem, deleteContextItem, runContextItem });
            _backupListView.ContextMenuStrip = contextMenu;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Update all UI texts
            this.Text = _languageManager.GetTranslation("menu.title");
            _menuStrip.Items[0].Text = _languageManager.GetTranslation("menu.file");
            _menuStrip.Items[1].Text = _languageManager.GetTranslation("menu.backup");
            _menuStrip.Items[2].Text = _languageManager.GetTranslation("menu.settings");
            _menuStrip.Items[3].Text = _languageManager.GetTranslation("menu.view");
            RefreshBackupList();
        }

        private void OnTranslationsReloaded(object sender, EventArgs e)
        {
            OnLanguageChanged(sender, e);
        }

        private void UpdateBackupStates()
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

        private string GetStatusText(string status)
        {
            switch (status?.ToLower() ?? "pending")
            {
                case "active":
                    return _languageManager.GetTranslation("backup.state.running");
                case "inactive":
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
            foreach (Backup backup in _backupController.GetBackups())
            {
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

        private void AddJob()
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

        private void UpdateJob()
        {
            if (_backupListView.SelectedItems.Count == 0) return;
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

        private void RemoveJob()
        {
            if (_backupListView.SelectedItems.Count == 0) return;
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
            if (_backupListView.SelectedItems.Count == 0) return;
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
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                    _progressBar.Visible = true;
                    await _backupController.StartBackup(backup.Name);
                    _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                    _progressBar.Visible = false;
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.backupSuccess"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
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
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm())
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
            _updateTimer.Stop();
            base.OnFormClosing(e);
        }
    }
}
