using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;
using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EasySaveV2._0.Views
{
    public partial class SettingsForm : Form
    {
        private readonly SettingsController _settingsController;
        private readonly LanguageManager _languageManager;
        private readonly BackupController _backupController;
        private TabControl _tabControl;
        private TabPage _businessSoftwareTab;
        private TabPage _encryptionExtensionsTab;
        private TabPage _languageTab;
        private TabPage _logFormatTab;
        private ComboBox _languageComboBox;
        private ComboBox _logFormatComboBox;

        public int SelectedTab
        {
            get => _tabControl.SelectedIndex;
            set => _tabControl.SelectedIndex = value;
        }

        public SettingsForm(SettingsController settingsController)
        {
            _settingsController = settingsController;
            _languageManager = LanguageManager.Instance;
            _backupController = new BackupController();
            InitializeComponent();
            InitializeUI();
            _languageManager.LanguageChanged += OnLanguageChanged;
        }

        private void InitializeUI()
        {
        }

        private void RefreshBusinessSoftwareList(ListBox listBox)
        {
        }

        private void RefreshEncryptionExtensionsList(ListBox listBox)
        {
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
        }
    }

    public class InputDialog : Form
    {
        private readonly TextBox _textBox;
        public string Input => _textBox.Text;

        public InputDialog(string prompt)
        {
        }
    }
} 