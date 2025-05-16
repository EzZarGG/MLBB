using EasySaveV2._0.Controllers;
using EasySaveV2._0.Models;
using EasySaveV2._0.Views;
using EasySaveV2._0.Managers;
using System.Windows.Forms;

namespace EasySaveV2._0
{
    public partial class MainForm : Form
    {
        private BackupController _backupController;
        private SettingsController _settingsController;
        private LanguageManager _languageManager;
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
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000; // Update every second
            _updateTimer.Tick += (s, e) => UpdateBackupStates();
            _updateTimer.Start();
        }

        private void InitializeUI()
        {
            this.Text = "EasySave";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Menu Strip
            _menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.file"));
            fileMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.newBackup"), null, (s, e) => AddJob());
            fileMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.exit"), null, (s, e) => this.Close());
            _menuStrip.Items.Add(fileMenu);

            var backupMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.backup"));
            backupMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.editBackup"), null, (s, e) => UpdateJob());
            backupMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.deleteBackup"), null, (s, e) => RemoveJob());
            backupMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.runBackup"), null, (s, e) => ExecuteJob());
            _menuStrip.Items.Add(backupMenu);

            var settingsMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.settings"));
            settingsMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.settings.open"), null, (s, e) => OpenSettings());
            _menuStrip.Items.Add(settingsMenu);

            var viewMenu = new ToolStripMenuItem(_languageManager.GetTranslation("menu.view"));
            viewMenu.DropDownItems.Add(_languageManager.GetTranslation("menu.view.logs"), null, (s, e) => ViewLogs());
            _menuStrip.Items.Add(viewMenu);

            // Tool Strip
            _toolStrip = new ToolStrip();
            _toolStrip.Items.Add(new ToolStripButton(_languageManager.GetTranslation("menu.new"), null, (s, e) => AddJob()));
            _toolStrip.Items.Add(new ToolStripButton(_languageManager.GetTranslation("menu.edit"), null, (s, e) => UpdateJob()));
            _toolStrip.Items.Add(new ToolStripButton(_languageManager.GetTranslation("menu.delete"), null, (s, e) => RemoveJob()));
            _toolStrip.Items.Add(new ToolStripButton(_languageManager.GetTranslation("menu.run"), null, (s, e) => ExecuteJob()));

            // List View
            _backupListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.name"), 150);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.source"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.destination"), 200);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.type"), 100);
            _backupListView.Columns.Add(_languageManager.GetTranslation("backup.status"), 100);

            // Status Strip
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel(_languageManager.GetTranslation("status.ready"));
            _progressBar = new ProgressBar { Width = 200 };
            _statusStrip.Items.Add(_statusLabel);
            _statusStrip.Items.Add(new ToolStripStatusLabel(""));
            _statusStrip.Items.Add(new ToolStripControlHost(_progressBar));

            // Add controls to form
            this.Controls.Add(_menuStrip);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_backupListView);
            this.Controls.Add(_statusStrip);

            // Refresh backup list
            RefreshBackupList();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Update UI text based on current language
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
                        item.SubItems[4].Text = state.Status;
                    }
                }
            }
        }

        private void RefreshBackupList()
        {
            _backupListView.Items.Clear();
            foreach (var backup in _backupController.GetBackups())
            {
                var item = new ListViewItem(backup.Name);
                item.SubItems.Add(backup.SourcePath);
                item.SubItems.Add(backup.TargetPath);
                item.SubItems.Add(backup.Type);
                item.SubItems.Add("Pending");
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

            var selectedBackup = _backupController.GetBackup(_backupListView.SelectedItems[0].Text);
            if (selectedBackup == null) return;

            using (var form = new BackupForm(selectedBackup))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var backup = form.Backup;
                    _backupController.EditBackup(backup.Name, backup.SourcePath, backup.TargetPath, backup.Type);
                    RefreshBackupList();
                }
            }
        }

        private void RemoveJob()
        {
            if (_backupListView.SelectedItems.Count == 0) return;

            var selectedBackup = _backupListView.SelectedItems[0].Text;
            if (MessageBox.Show(
                _languageManager.GetTranslation("message.confirmDelete"),
                _languageManager.GetTranslation("message.confirmDeleteTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _backupController.DeleteBackup(selectedBackup);
                RefreshBackupList();
            }
        }

        private async void ExecuteJob()
        {
            if (_backupListView.SelectedItems.Count == 0) return;

            var selectedBackup = _backupListView.SelectedItems[0].Text;
            _statusLabel.Text = _languageManager.GetTranslation("status.backupInProgress");
            _progressBar.Value = 0;

            try
            {
                await _backupController.StartBackup(selectedBackup);
                _statusLabel.Text = _languageManager.GetTranslation("status.backupComplete");
                _progressBar.Value = 100;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = _languageManager.GetTranslation("status.backupError");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (MessageBox.Show(
                _languageManager.GetTranslation("message.confirmExit"),
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            _updateTimer.Stop();
            base.OnFormClosing(e);
        }
    }
}
