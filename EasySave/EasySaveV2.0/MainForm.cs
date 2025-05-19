using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using EasySaveV2._0.Views;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Notifications;
using System;
using System.Threading.Tasks;
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
        private readonly BackupManager _backupManager;
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
            
            
            _languageManager = LanguageManager.Instance;
            INotifier notifier = new MessageBoxNotifier();
            _backupController = new BackupController(notifier);
            _backupManager = new BackupManager();
            _settingsController = new SettingsController();

            InitializeTimer();
            InitializeUI();

            _languageManager.LanguageChanged += OnLanguageChanged;
            _languageManager.TranslationsReloaded += OnTranslationsReloaded;

            UpdateAllTexts();
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
            var fileMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file"))
            {
                Tag = "menu.file"
            };
            var exitItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file.exit"))
            {
                Tag = "menu.file.exit"
            };
            exitItem.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitItem);

            // Backup menu
            var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup"))
            {
                Tag = "menu.backup"
            };
            var createBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.create"))
            {
                Tag = "menu.backup.create"
            };
            createBackupItem.Click += (s, e) => AddJob();
            var editBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.edit"))
            {
                Tag = "menu.backup.edit"
            };
            editBackupItem.Click += (s, e) => UpdateJob();
            var deleteBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.delete"))
            {
                Tag = "menu.backup.delete"
            };
            deleteBackupItem.Click += (s, e) => RemoveJob();
            var runBackupItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup.run"))
            {
                Tag = "menu.backup.run"
            };
            runBackupItem.Click += (s, e) => ExecuteJob();

            backupMenu.DropDownItems.AddRange(new[]
            {
        createBackupItem,
        editBackupItem,
        deleteBackupItem,
        runBackupItem
    });
            // Settings menu
            var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings"))
            {
                Tag = "menu.settings"
            };
            var openSettingsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings.open"))
            {
                Tag = "menu.settings.open"
            };
            openSettingsItem.Click += (s, e) => OpenSettings();
            settingsMenu.DropDownItems.Add(openSettingsItem);

            // View menu
            var viewMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view"))
            {
                Tag = "menu.view"
            };
            var viewLogsItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view.logs"))
            {
                Tag = "menu.view.logs"
            };
            viewLogsItem.Click += (s, e) => ViewLogs();
            viewMenu.DropDownItems.Add(viewLogsItem);
            // Help menu
            var helpMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.help"))
            {
                Tag = "menu.help"
            };

            _menuStrip.Items.AddRange(new ToolStripItem[]
            {
        fileMenu,
        backupMenu,
        settingsMenu,
        viewMenu,
        helpMenu
            });
            // Toolbar
            _toolStrip = new ToolStrip { Dock = DockStyle.Top };
            var newButton = new ToolStripButton(_languageManager.GetTranslation("menu.new"))
            {
                Tag = "menu.new"
            }; newButton.Click += (s, e) => AddJob();
            var editButton = new ToolStripButton(_languageManager.GetTranslation("menu.edit"))
            {
                Tag = "menu.edit"
            }; editButton.Click += (s, e) => UpdateJob();
            var deleteButton = new ToolStripButton(_languageManager.GetTranslation("menu.delete"))
            {
                Tag = "menu.delete"
            }; deleteButton.Click += (s, e) => RemoveJob();
            var runButton = new ToolStripButton(_languageManager.GetTranslation("menu.run"))
            {
                Tag = "menu.run"
            }; runButton.Click += (s, e) => ExecuteJob();
            var settingsButton = new ToolStripButton(_languageManager.GetTranslation("menu.settings"))
            {
                Tag = "menu.settings"
            }; settingsButton.Click += (s, e) => OpenSettings();

            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
        newButton,
        editButton,
        deleteButton,
        runButton,
        new ToolStripSeparator(),
        settingsButton
            });
            // ListView
            _backupListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.name"),
                Tag = "backup.name",
                Width = 150
            });
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.source"),
                Tag = "backup.source",
                Width = 200
            });
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.destination"),
                Tag = "backup.destination",
                Width = 200
            });
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.type"),
                Tag = "backup.type",
                Width = 100
            });
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.status"),
                Tag = "backup.status",
                Width = 100
            });
            _backupListView.Columns.Add(new ColumnHeader
            {
                Text = _languageManager.GetTranslation("backup.progress"),
                Tag = "backup.progress",
                Width = 150
            });

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
            var editContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.edit"))
            {
                Tag = "menu.edit"
            };
            editContextItem.Click += (s, e) => UpdateJob();
            var deleteContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.delete"))
            {
                Tag = "menu.delete"
            };
            deleteContextItem.Click += (s, e) => RemoveJob();
            var runContextItem = new ToolStripMenuItem(_languageManager.GetTranslation("menu.run"))
            {
                Tag = "menu.run"
            };
            runContextItem.Click += (s, e) => ExecuteJob();
            contextMenu.Items.AddRange(new ToolStripItem[]
            {
        editContextItem,
        deleteContextItem,
        runContextItem
            });
            _backupListView.ContextMenuStrip = contextMenu;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // On se contente d'appeler la mise à jour récursive + le rafraîchissement de liste
            UpdateAllTexts();
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
        private void UpdateAllTexts()
        {
            // 1) Menus (récursif sur tous les sous-menus)
            UpdateMenuItems(_menuStrip.Items);

            // 2) Toolbar
            foreach (ToolStripItem btn in _toolStrip.Items)
            {
                if (btn.Tag is string key)
                    btn.Text = _languageManager.GetTranslation(key);
            }

            // 3) Colonnes ListView
            foreach (ColumnHeader col in _backupListView.Columns)
            {
                if (col.Tag is string key)
                    col.Text = _languageManager.GetTranslation(key);
            }

            // 4) Titre de la fenêtre (optionnel)
            if (this.Tag is string formKey)
                this.Text = _languageManager.GetTranslation(formKey);

            // 5) … éventuels autres contrôles taggés
        }


        private void UpdateMenuItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.Tag is string key)
                    item.Text = _languageManager.GetTranslation(key);

                if (item is ToolStripMenuItem mi && mi.DropDownItems.Count > 0)
                    UpdateMenuItems(mi.DropDownItems);
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

            var backup = _backupListView.SelectedItems[0].Tag as Backup;
            if (backup == null) return;

            try
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
                _progressBar.Visible = true;

                // Le contrôleur gère la détection et les notifications
                await _backupController.RunBackupAsync(
                    new BackupJob
                    {
                        Name = backup.Name,
                        FilesToSave = _backupManager.CollectFiles(backup),
                        TargetDirectory = backup.TargetPath
                    },
                    CancellationToken.None
                );

                _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                _progressBar.Visible = false;
                RefreshBackupList();
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
