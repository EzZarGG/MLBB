using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;
using EasySaveLogging;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace EasySaveV2._0.Views
{
    public partial class SettingsForm : Form
    {
        private readonly LanguageManager _languageManager;
        private readonly SettingsController _settingsController;
        private ComboBox _languageComboBox;
        private ComboBox _logFormatComboBox;
        private ListBox _businessSoftwareListBox;
        private ListBox _encryptionExtensionsListBox;
        private TextBox _newBusinessSoftwareTextBox;
        private TextBox _newExtensionTextBox;
        private Button _addBusinessSoftwareButton;
        private Button _removeBusinessSoftwareButton;
        private Button _addExtensionButton;
        private Button _removeExtensionButton;
        private Button _saveButton;
        private Button _cancelButton;

        public SettingsForm()
        {
            _languageManager = LanguageManager.Instance;
            _settingsController = new SettingsController();
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(10)
            };

            // Language selection
            layout.Controls.Add(new Label { Text = _languageManager.GetTranslation("settings.language"), Anchor = AnchorStyles.Right }, 0, 0);
            _languageComboBox = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _languageComboBox.Items.AddRange(new object[] { "Français", "English" });
            _languageComboBox.SelectedItem = _languageManager.CurrentLanguage == "fr" ? "Français" : "English";
            layout.Controls.Add(_languageComboBox, 1, 0);

            // Log format selection
            layout.Controls.Add(new Label { Text = _languageManager.GetTranslation("settings.logFormat"), Anchor = AnchorStyles.Right }, 0, 1);
            _logFormatComboBox = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _logFormatComboBox.Items.AddRange(new object[] { "JSON", "XML" });
            _logFormatComboBox.SelectedItem = _settingsController.GetCurrentLogFormat().ToString();
            layout.Controls.Add(_logFormatComboBox, 1, 1);

            // Business software list
            layout.Controls.Add(new Label { Text = _languageManager.GetTranslation("settings.businessSoftware"), Anchor = AnchorStyles.Right }, 0, 2);
            _businessSoftwareListBox = new ListBox { Width = 200, Height = 100 };
            _businessSoftwareListBox.Items.AddRange(_settingsController.GetBusinessSoftware().ToArray());
            layout.Controls.Add(_businessSoftwareListBox, 1, 2);

            var businessSoftwarePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Width = 100
            };
            _newBusinessSoftwareTextBox = new TextBox { Width = 100 };
            _addBusinessSoftwareButton = new Button { Text = _languageManager.GetTranslation("settings.add"), Width = 100 };
            _addBusinessSoftwareButton.Click += (s, e) => AddBusinessSoftware();
            _removeBusinessSoftwareButton = new Button { Text = _languageManager.GetTranslation("settings.remove"), Width = 100 };
            _removeBusinessSoftwareButton.Click += (s, e) => RemoveBusinessSoftware();
            businessSoftwarePanel.Controls.Add(_newBusinessSoftwareTextBox);
            businessSoftwarePanel.Controls.Add(_addBusinessSoftwareButton);
            businessSoftwarePanel.Controls.Add(_removeBusinessSoftwareButton);
            layout.Controls.Add(businessSoftwarePanel, 2, 2);

            // Encryption extensions list
            layout.Controls.Add(new Label { Text = _languageManager.GetTranslation("settings.encryptionExtensions"), Anchor = AnchorStyles.Right }, 0, 3);
            _encryptionExtensionsListBox = new ListBox { Width = 200, Height = 100 };
            _encryptionExtensionsListBox.Items.AddRange(_settingsController.GetEncryptionExtensions().ToArray());
            layout.Controls.Add(_encryptionExtensionsListBox, 1, 3);

            var extensionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Width = 100
            };
            _newExtensionTextBox = new TextBox { Width = 100 };
            _addExtensionButton = new Button { Text = _languageManager.GetTranslation("settings.add"), Width = 100 };
            _addExtensionButton.Click += (s, e) => AddExtension();
            _removeExtensionButton = new Button { Text = _languageManager.GetTranslation("settings.remove"), Width = 100 };
            _removeExtensionButton.Click += (s, e) => RemoveExtension();
            extensionsPanel.Controls.Add(_newExtensionTextBox);
            extensionsPanel.Controls.Add(_addExtensionButton);
            extensionsPanel.Controls.Add(_removeExtensionButton);
            layout.Controls.Add(extensionsPanel, 2, 3);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            _saveButton = new Button { Text = _languageManager.GetTranslation("settings.save"), Width = 80 };
            _saveButton.Click += (s, e) => SaveSettings();
            _cancelButton = new Button { Text = _languageManager.GetTranslation("settings.cancel"), Width = 80 };
            _cancelButton.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            this.Controls.Add(layout);
            this.Controls.Add(buttonPanel);
        }

        private void AddBusinessSoftware()
        {
            var software = _newBusinessSoftwareTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(software) && !_businessSoftwareListBox.Items.Contains(software))
            {
                _businessSoftwareListBox.Items.Add(software);
                _newBusinessSoftwareTextBox.Clear();
            }
        }

        private void RemoveBusinessSoftware()
        {
            if (_businessSoftwareListBox.SelectedItem != null)
            {
                _businessSoftwareListBox.Items.Remove(_businessSoftwareListBox.SelectedItem);
            }
        }

        private void AddExtension()
        {
            var extension = _newExtensionTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(extension) && !_encryptionExtensionsListBox.Items.Contains(extension))
            {
                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }
                _encryptionExtensionsListBox.Items.Add(extension);
                _newExtensionTextBox.Clear();
            }
        }

        private void RemoveExtension()
        {
            if (_encryptionExtensionsListBox.SelectedItem != null)
            {
                _encryptionExtensionsListBox.Items.Remove(_encryptionExtensionsListBox.SelectedItem);
            }
        }

        private void SaveSettings()
        {
            // Save language
            var language = _languageComboBox.SelectedItem.ToString() == "Français" ? "fr" : "en";
            _languageManager.SetLanguage(language);

            // Save log format
            var logFormat = (LogFormat)Enum.Parse(typeof(LogFormat), _logFormatComboBox.SelectedItem.ToString());
            _settingsController.SetLogFormat(logFormat);

            // Save business software list
            var businessSoftware = new List<string>();
            foreach (var item in _businessSoftwareListBox.Items)
            {
                businessSoftware.Add(item.ToString());
            }
            _settingsController.SetBusinessSoftware(businessSoftware);

            // Save encryption extensions list
            var extensions = new List<string>();
            foreach (var item in _encryptionExtensionsListBox.Items)
            {
                extensions.Add(item.ToString());
            }
            _settingsController.SetEncryptionExtensions(extensions);

            this.DialogResult = DialogResult.OK;
            this.Close();
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