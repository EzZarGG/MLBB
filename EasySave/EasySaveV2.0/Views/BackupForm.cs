using EasySaveV2._0.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

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

        public Backup Backup { get; private set; }

        public BackupForm(Backup backup = null)
        {
            InitializeComponent();
            _backup = backup;
            _isEditMode = backup != null;
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Edit Backup" : "New Backup";
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(10)
            };

            // Name
            layout.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Right }, 0, 0);
            _nameTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            layout.Controls.Add(_nameTextBox, 1, 0);

            // Source
            layout.Controls.Add(new Label { Text = "Source:", Anchor = AnchorStyles.Right }, 0, 1);
            _sourceTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            layout.Controls.Add(_sourceTextBox, 1, 1);
            _sourceBrowseButton = new Button { Text = "Browse...", Width = 80 };
            _sourceBrowseButton.Click += OnSourceBrowseClick;
            layout.Controls.Add(_sourceBrowseButton, 2, 1);

            // Target
            layout.Controls.Add(new Label { Text = "Target:", Anchor = AnchorStyles.Right }, 0, 2);
            _targetTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left };
            layout.Controls.Add(_targetTextBox, 1, 2);
            _targetBrowseButton = new Button { Text = "Browse...", Width = 80 };
            _targetBrowseButton.Click += OnTargetBrowseClick;
            layout.Controls.Add(_targetBrowseButton, 2, 2);

            // Type
            layout.Controls.Add(new Label { Text = "Type:", Anchor = AnchorStyles.Right }, 0, 3);
            _typeComboBox = new ComboBox { Width = 300, Anchor = AnchorStyles.Left, DropDownStyle = ComboBoxStyle.DropDownList };
            _typeComboBox.Items.AddRange(new object[] { "Full", "Differential" });
            layout.Controls.Add(_typeComboBox, 1, 3);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            _saveButton = new Button { Text = "Save", Width = 80 };
            _saveButton.Click += OnSaveClick;
            _cancelButton = new Button { Text = "Cancel", Width = 80 };
            _cancelButton.Click += OnCancelClick;

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            this.Controls.Add(layout);
            this.Controls.Add(buttonPanel);

            if (_isEditMode)
            {
                LoadBackupToUI();
            }
        }

        private void OnSourceBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select source folder";
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
                dialog.Description = "Select target folder";
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
                MessageBox.Show("Please enter a name for the backup.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_sourceTextBox.Text))
            {
                MessageBox.Show("Please select a source folder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_targetTextBox.Text))
            {
                MessageBox.Show("Please select a target folder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (_typeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a backup type.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Directory.Exists(_sourceTextBox.Text))
            {
                MessageBox.Show("Source folder does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _typeComboBox.SelectedItem = _backup.Type;
            }
        }
    }
} 