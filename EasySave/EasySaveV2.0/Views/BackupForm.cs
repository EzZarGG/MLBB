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

        public BackupForm(Backup backup = null)
        {
            InitializeComponent();
            _backup = backup ?? new Backup();
            _isEditMode = backup != null;
            _backupController = new BackupController();
            _languageManager = LanguageManager.Instance;
            
            if (_isEditMode)
            {
                LoadBackupToUI();
            }
            else
            {
                cmbType.SelectedIndex = 0;
            }

            // Update UI translations
            UpdateTranslations();
            _languageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, string language)
        {
            UpdateTranslations();
        }

        private void UpdateTranslations()
        {
            this.Text = _languageManager.GetTranslation(_isEditMode ? "backup.edit.title" : "backup.create.title");
            nameLabel.Text = _languageManager.GetTranslation("backup.name");
            sourceLabel.Text = _languageManager.GetTranslation("backup.source");
            targetLabel.Text = _languageManager.GetTranslation("backup.target");
            typeLabel.Text = _languageManager.GetTranslation("backup.type");
            btnSourceBrowse.Text = _languageManager.GetTranslation("backup.browse");
            btnTargetBrowse.Text = _languageManager.GetTranslation("backup.browse");
            btnSave.Text = _languageManager.GetTranslation("button.save");
            btnCancel.Text = _languageManager.GetTranslation("button.cancel");
        }

        private void OnSourceBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = _languageManager.GetTranslation("backup.selectSource");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnTargetBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = _languageManager.GetTranslation("backup.selectTarget");
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtTarget.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            UpdateBackupFromUI();

            try
            {
                if (_isEditMode)
                {
                    _backupController.EditBackup(_backup.Name, _backup.SourcePath, _backup.TargetPath, _backup.Type);
                }
                else
                {
                    _backupController.CreateBackup(_backup.Name, _backup.SourcePath, _backup.TargetPath, _backup.Type);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
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

        private void OnCancelClick(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.enterName"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSource.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.selectSource"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtTarget.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.selectTarget"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            if (!Directory.Exists(txtSource.Text))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.sourceNotExist"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            if (!Directory.Exists(txtTarget.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtTarget.Text);
                }
                catch
                {
                    MessageBox.Show(
                        _languageManager.GetTranslation("message.cannotCreateTarget"),
                        _languageManager.GetTranslation("menu.title"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
            }

            // Check for duplicate names
            var backups = _backupController.GetBackups();
            if (backups.Any(b => b.Name == txtName.Text && (!_isEditMode || b.Name != _backup.Name)))
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("message.backupExists"),
                    _languageManager.GetTranslation("menu.title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            return true;
        }

        private void UpdateBackupFromUI()
        {
            _backup.Name = txtName.Text;
            _backup.SourcePath = txtSource.Text;
            _backup.TargetPath = txtTarget.Text;
            _backup.Type = cmbType.SelectedItem.ToString();
        }

        private void LoadBackupToUI()
        {
            txtName.Text = _backup.Name;
            txtSource.Text = _backup.SourcePath;
            txtTarget.Text = _backup.TargetPath;
            cmbType.SelectedItem = _backup.Type;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
} 