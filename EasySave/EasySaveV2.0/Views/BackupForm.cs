using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using EasySaveV2._0.Managers;

namespace EasySaveV2._0.Views
{
    public partial class BackupForm : Form
    {
        private readonly Backup _backup;
        private readonly bool _isEditMode;
        private TextBox _nameTextBox;
        private TextBox _sourceTextBox;
        private TextBox _targetTextBox;
        private ComboBox _typeComboBox;
        private Button _sourceBrowseButton;
        private Button _targetBrowseButton;
        private Button _saveButton;
        private Button _cancelButton;
        private readonly LanguageManager _languageManager = LanguageManager.Instance;

        public Backup Backup { get; private set; }

        public BackupForm(Backup backup = null)
        {
            InitializeComponent();
            _backup = backup;
            _isEditMode = backup != null;

            InitializeUI();

            // Subscribe to language changes
            _languageManager.LanguageChanged += (s, e) => UpdateFormTexts();

            // Set initial texts
            UpdateFormTexts();

            if (_isEditMode)
                LoadBackupToUI();
        }

        private void InitializeUI()
        {
            // Title and Tag for the form
            var formKey = _isEditMode ? "menu.editBackup" : "menu.newBackup";
            this.Text = _languageManager.GetTranslation(formKey);
            this.Tag = formKey;

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

            // — Name
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

            // — Source + Browse
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
                Tag = "button.browse",
                Width = 80
            };
            _sourceBrowseButton.Click += OnSourceBrowseClick;
            layout.Controls.Add(_sourceTextBox, 1, 1);
            layout.Controls.Add(_sourceBrowseButton, 2, 1);

            // — Target + Browse
            layout.Controls.Add(new Label
            {
                Tag = "backup.destination",
                Anchor = AnchorStyles.Right
            }, 0, 2);
            _targetTextBox = new TextBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left
            };
            _targetBrowseButton = new Button
            {
                Tag = "button.browse",
                Width = 80
            };
            _targetBrowseButton.Click += OnTargetBrowseClick;
            layout.Controls.Add(_targetTextBox, 1, 2);
            layout.Controls.Add(_targetBrowseButton, 2, 2);

            // — Backup type
            layout.Controls.Add(new Label
            {
                Tag = "backup.type",
                Anchor = AnchorStyles.Right
            }, 0, 3);
            _typeComboBox = new ComboBox
            {
                Width = 300,
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Tag = "backup.type"
            };
            layout.Controls.Add(_typeComboBox, 1, 3);

            // — Save / Cancel buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };
            _saveButton = new Button
            {
                Tag = "settings.save",
                Width = 80
            };
            _saveButton.Click += OnSaveClick;
            _cancelButton = new Button
            {
                Tag = "settings.cancel",
                Width = 80
            };
            _cancelButton.Click += OnCancelClick;

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            this.Controls.Add(layout);
            this.Controls.Add(buttonPanel);
        }

        /// <summary>
        /// Updates all texts (form title, labels, buttons, combo items) when language changes.
        /// </summary>
        private void UpdateFormTexts()
        {
            // 1) Form title
            if (this.Tag is string formKey)
                this.Text = _languageManager.GetTranslation(formKey);

            // 2) Update tagged controls recursively
            UpdateControlTexts(this.Controls);

            // 3) Update items in the type combo
            PopulateTypeCombo();
        }

        /// <summary>
        /// Recursively updates Control.Text for any control with a Tag.
        /// </summary>
        private void UpdateControlTexts(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c.Tag is string key)
                    c.Text = _languageManager.GetTranslation(key);
                if (c.HasChildren)
                    UpdateControlTexts(c.Controls);
            }
        }

        private void PopulateTypeCombo()
        {
            _typeComboBox.Items.Clear();
            _typeComboBox.Items.Add(_languageManager.GetTranslation("backup.type.full"));
            _typeComboBox.Items.Add(_languageManager.GetTranslation("backup.type.differential"));
            if (_isEditMode && _backup != null)
            {
                var key = _backup.Type.Equals("Full", StringComparison.OrdinalIgnoreCase)
                    ? "backup.type.full"
                    : "backup.type.differential";
                _typeComboBox.SelectedItem = _languageManager.GetTranslation(key);
            }
        }

        private void OnSourceBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = _languageManager.GetTranslation("backup.source");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _sourceTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnTargetBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = _languageManager.GetTranslation("backup.destination");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _targetTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                UpdateBackupFromUI();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("backup.askName"),
                    _languageManager.GetTranslation("message.invalid"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_sourceTextBox.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("backup.source"),
                    _languageManager.GetTranslation("message.invalid"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_targetTextBox.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("backup.destination"),
                    _languageManager.GetTranslation("message.invalid"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (_typeComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("backup.type"),
                    _languageManager.GetTranslation("message.invalid"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            if (!Directory.Exists(_sourceTextBox.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.error").Replace("{0}", _languageManager.GetTranslation("backup.source")),
                    _languageManager.GetTranslation("message.error").Replace("{0}", ""),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void UpdateBackupFromUI()
        {
            Backup = new Backup
            {
                Name = _nameTextBox.Text,
                SourcePath = _sourceTextBox.Text,
                TargetPath = _targetTextBox.Text,
                Type = _typeComboBox.SelectedItem.ToString(),
                FileLength = 0
            };
        }

        private void LoadBackupToUI()
        {
            if (_backup != null)
            {
                _nameTextBox.Text = _backup.Name;
                _sourceTextBox.Text = _backup.SourcePath;
                _targetTextBox.Text = _backup.TargetPath;
                // Map internal type to translated item
                var key = _backup.Type.Equals("Full", StringComparison.OrdinalIgnoreCase)
                    ? "backup.type.full"
                    : "backup.type.differential";
                _typeComboBox.SelectedItem = _languageManager.GetTranslation(key);
            }
        }

        private void BackupForm_Load(object sender, EventArgs e)
        {
            // No additional logic
        }
    }
}
