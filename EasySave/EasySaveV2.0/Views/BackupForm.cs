using EasySaveV2._0.Models;
using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace EasySaveV2._0.Views
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
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Layout panel
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(10)
            };

            // Name field
            layout.Controls.Add(new Label
            {
                Tag = "backup.name",
                Anchor = AnchorStyles.Right
            }, 0, 0);
            _nameTextBox = new TextBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left
            };
            layout.Controls.Add(_nameTextBox, 1, 0);

            // Source field with browse button
            layout.Controls.Add(new Label
            {
                Tag = "backup.source",
                Anchor = AnchorStyles.Right
            }, 0, 1);
            _sourceTextBox = new TextBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left
            };
            _sourceBrowseButton = new Button
            {
                Tag = "backup.browse",
                Width = 80
            };
            layout.Controls.Add(_sourceTextBox, 1, 1);
            layout.Controls.Add(_sourceBrowseButton, 2, 1);

            // Target field with browse button
            layout.Controls.Add(new Label
            {
                Tag = "backup.target",
                Anchor = AnchorStyles.Right
            }, 0, 2);
            _targetTextBox = new TextBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left
            };
            _targetBrowseButton = new Button
            {
                Tag = "backup.browse",
                Width = 80
            };
            layout.Controls.Add(_targetTextBox, 1, 2);
            layout.Controls.Add(_targetBrowseButton, 2, 2);

            // Type combo box
            layout.Controls.Add(new Label
            {
                Tag = "backup.type",
                Anchor = AnchorStyles.Right
            }, 0, 3);
            _typeComboBox = new ComboBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            PopulateTypeCombo();
            layout.Controls.Add(_typeComboBox, 1, 3);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            _saveButton = new Button
            {
                Tag = "button.save",
                Width = 80
            };
            _cancelButton = new Button
            {
                Tag = "button.cancel",
                Width = 80
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            this.Controls.Add(layout);
            this.Controls.Add(buttonPanel);
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

        private void PopulateTypeCombo()
        {
            _typeComboBox.Items.Clear();
            _typeComboBox.Items.Add(_languageManager.GetTranslation("backup.type.complete"));
            _typeComboBox.Items.Add(_languageManager.GetTranslation("backup.type.differential"));
            if (_typeComboBox.SelectedIndex == -1 && _typeComboBox.Items.Count > 0)
            {
                _typeComboBox.SelectedIndex = 0;
            }
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
            _backup.Type = _typeComboBox.SelectedIndex == 0 ? "Full" : "Differential";
        }

        private void LoadBackupToUI()
        {
            _nameTextBox.Text = _backup.Name;
            _sourceTextBox.Text = _backup.SourcePath;
            _targetTextBox.Text = _backup.TargetPath;
            _typeComboBox.SelectedIndex = _backup.Type.Equals("Full", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}