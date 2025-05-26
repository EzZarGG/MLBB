using System;
using System.Windows.Forms;
using EasySaveV3._0.Managers;
using EasySaveV3._0.Models;
using EasySaveV3._0.Controllers;
using System.IO;
using System.Linq;
using System.Drawing;

namespace EasySaveV3._0.Views
{
    public partial class BackupForm : Form
    {
        private readonly Backup _backup;
        private readonly bool _isEditMode;
        private readonly BackupController _backupController;
        private readonly LanguageManager _languageManager;

        // UI Controls
        private TextBox _nameTextBox = new();
        private TextBox _sourceTextBox = new();
        private TextBox _targetTextBox = new();
        private ComboBox _typeComboBox = new();
        private Button _sourceBrowseButton = new();
        private Button _targetBrowseButton = new();
        private Button _saveButton = new();
        private Button _cancelButton = new();

        public Backup Backup => _backup;

        public BackupForm(Backup? backup = null)
        {
            _backup = backup ?? new Backup();
            _isEditMode = backup != null;
            _backupController = new BackupController();
            _languageManager = LanguageManager.Instance;

            InitializeComponent();
            _languageManager.ReloadTranslations();
            InitializeUI();
            SetupEventHandlers();

            if (_isEditMode)
            {
                LoadBackupToUI();
            }
            else
            {
                _typeComboBox.SelectedIndex = 0;
            }

            UpdateFormTexts();
        }

        private void InitializeUI()
        {
            // Form properties
            this.Text = _languageManager.GetTranslation(_isEditMode ? "backup.edit.title" : "backup.create.title");
            this.Size = new System.Drawing.Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.White;

            // Layout panel
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(20),
                RowStyles = {
                    new RowStyle(SizeType.Percent, 15),  // Title
                    new RowStyle(SizeType.Percent, 17),  // Name
                    new RowStyle(SizeType.Percent, 17),  // Source
                    new RowStyle(SizeType.Percent, 17),  // Target
                    new RowStyle(SizeType.Percent, 17),  // Type
                    new RowStyle(SizeType.Percent, 17)   // Buttons
                },
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 20),  // Labels
                    new ColumnStyle(SizeType.Percent, 60),  // TextBoxes
                    new ColumnStyle(SizeType.Percent, 20)   // Buttons
                }
            };

            // Title label
            var titleLabel = new Label
            {
                Tag = _isEditMode ? "backup.edit.title" : "backup.create.title",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 16, FontStyle.Bold),
                AutoSize = false,
                ForeColor = Color.FromArgb(0, 120, 215) // Windows blue
            };
            layout.Controls.Add(titleLabel, 0, 0);
            layout.SetColumnSpan(titleLabel, 3);

            // Name field
            var nameLabel = new Label
            {
                Tag = "backup.name",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                AutoSize = false,
                Padding = new Padding(0, 0, 10, 0)
            };
            _nameTextBox = new TextBox
            {
                Width = 400,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            layout.Controls.Add(nameLabel, 0, 1);
            layout.Controls.Add(_nameTextBox, 1, 1);

            // Source field with browse button
            var sourceLabel = new Label
            {
                Tag = "backup.source",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                AutoSize = false,
                Padding = new Padding(0, 0, 10, 0)
            };
            _sourceTextBox = new TextBox
            {
                Width = 400,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            _sourceBrowseButton = new Button
            {
                Tag = "backup.browse",
                Width = 100,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 9),
                FlatStyle = FlatStyle.System,
                Image = SystemIcons.WinLogo.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(5, 0, 5, 0)
            };
            layout.Controls.Add(sourceLabel, 0, 2);
            layout.Controls.Add(_sourceTextBox, 1, 2);
            layout.Controls.Add(_sourceBrowseButton, 2, 2);

            // Target field with browse button
            var targetLabel = new Label
            {
                Tag = "backup.target",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                AutoSize = false,
                Padding = new Padding(0, 0, 10, 0)
            };
            _targetTextBox = new TextBox
            {
                Width = 400,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            _targetBrowseButton = new Button
            {
                Tag = "backup.browse",
                Width = 100,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 9),
                FlatStyle = FlatStyle.System,
                Image = SystemIcons.WinLogo.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(5, 0, 5, 0)
            };
            layout.Controls.Add(targetLabel, 0, 3);
            layout.Controls.Add(_targetTextBox, 1, 3);
            layout.Controls.Add(_targetBrowseButton, 2, 3);

            // Type combo box
            var typeLabel = new Label
            {
                Tag = "backup.type",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                AutoSize = false,
                Padding = new Padding(0, 0, 10, 0)
            };
            _typeComboBox = new ComboBox
            {
                Width = 400,
                Height = 30,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.System
            };
            layout.Controls.Add(typeLabel, 0, 4);
            layout.Controls.Add(_typeComboBox, 1, 4);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = false,
                Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

            _saveButton = new Button
            {
                Tag = "button.save",
                Width = 120,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10),
                FlatStyle = FlatStyle.System,
                Image = SystemIcons.Shield.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(5, 0, 5, 0)
            };
            _cancelButton = new Button
            {
                Tag = "button.cancel",
                Width = 120,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10),
                FlatStyle = FlatStyle.System,
                Image = SystemIcons.Question.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(5, 0, 5, 0)
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);
            layout.Controls.Add(buttonPanel, 0, 5);
            layout.SetColumnSpan(buttonPanel, 3);

            // Cleanup: don't add controls twice
            this.Controls.Clear();
            this.Controls.Add(layout);

            // Populate type combo
            PopulateTypeCombo();
        }

        private void SetupEventHandlers()
        {
            _languageManager.LanguageChanged += OnLanguageChanged;
            _sourceBrowseButton.Click += OnSourceBrowseClick;
            _targetBrowseButton.Click += OnTargetBrowseClick;
            _saveButton.Click += OnSaveClick;
            _cancelButton.Click += OnCancelClick;
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            UpdateFormTexts();
        }

        private void UpdateFormTexts()
        {
            // Update form title
            this.Text = _languageManager.GetTranslation(_isEditMode ? "backup.edit.title" : "backup.create.title");

            // Update all controls with tags
            UpdateControlTexts(this.Controls);

            // Update combo box items
            PopulateTypeCombo();

            // Reselect the correct type if editing
            if (_isEditMode)
                LoadBackupToUI();
        }

        private void UpdateControlTexts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag is string key)
                {
                    control.Text = _languageManager.GetTranslation(key);
                }
                if (control.HasChildren)
                {
                    UpdateControlTexts(control.Controls);
                }
            }
        }

        private class ComboItem
        {
            public string Key { get; }
            public string Display { get; }
            public ComboItem(string key, string display)
            {
                Key = key;
                Display = display;
            }
            public override string ToString() => Display;
        }

        private void PopulateTypeCombo()
        {
            _typeComboBox.Items.Clear();
            _typeComboBox.Items.Add(new ComboItem("Full", _languageManager.GetTranslation("backup.type.full")));
            _typeComboBox.Items.Add(new ComboItem("Differential", _languageManager.GetTranslation("backup.type.differential")));
            if (_typeComboBox.SelectedIndex == -1 && _typeComboBox.Items.Count > 0)
                _typeComboBox.SelectedIndex = 0;
        }

        private void OnSourceBrowseClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = _languageManager.GetTranslation("backup.source.browse")
            };

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
            {
                _sourceTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnTargetBrowseClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = _languageManager.GetTranslation("backup.target.browse")
            };

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
            {
                _targetTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            UpdateBackupFromUI();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                ShowError("backup.error.name.required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_sourceTextBox.Text))
            {
                ShowError("backup.error.source.required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_targetTextBox.Text))
            {
                ShowError("backup.error.target.required");
                return false;
            }

            if (!Directory.Exists(_sourceTextBox.Text))
            {
                ShowError("backup.error.source.not.exists");
                return false;
            }

            if (!Directory.Exists(_targetTextBox.Text))
            {
                ShowError("backup.error.target.not.exists");
                return false;
            }

            if (_sourceTextBox.Text.Equals(_targetTextBox.Text, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("backup.error.same.paths");
                return false;
            }

            return true;
        }

        private void ShowError(string messageKey)
        {
            MessageBox.Show(
                _languageManager.GetTranslation(messageKey),
                _languageManager.GetTranslation("error.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private void UpdateBackupFromUI()
        {
            _backup.Name = _nameTextBox.Text;
            _backup.SourcePath = _sourceTextBox.Text;
            _backup.TargetPath = _targetTextBox.Text;
            _backup.Type = (_typeComboBox.SelectedItem as ComboItem)?.Key ?? "Full";
        }

        private void LoadBackupToUI()
        {
            _nameTextBox.Text = _backup.Name;
            _sourceTextBox.Text = _backup.SourcePath;
            _targetTextBox.Text = _backup.TargetPath;
            foreach (ComboItem item in _typeComboBox.Items)
            {
                if (item.Key.Equals(_backup.Type, StringComparison.OrdinalIgnoreCase))
                {
                    _typeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}