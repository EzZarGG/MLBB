using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;

namespace EasySaveV2._0.Views
{
    public partial class SettingsForm : Form
    {
        private readonly LanguageManager _languageManager;
        private readonly SettingsController _settingsController;
        private ComboBox _languageComboBox;
        private ComboBox _logFormatComboBox;

        public SettingsForm()
        {
            InitializeComponent();
            _settingsController = new SettingsController();
            _languageManager = LanguageManager.Instance;

            InitializeUI();
            SetupEventHandlers();
            LoadSettings();
            UpdateTranslations();
        }

        private void InitializeUI()
        {
            // Add language and log format controls to business software tab
            var languagePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2,
                Height = 80,
                Padding = new Padding(10)
            };

            // Language selection
            languagePanel.Controls.Add(new Label 
            { 
                Text = _languageManager.GetTranslation("settings.language"),
                Anchor = AnchorStyles.Right,
                AutoSize = true
            }, 0, 0);

            _languageComboBox = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _languageComboBox.Items.AddRange(new object[] { "Français", "English" });
            _languageComboBox.SelectedItem = _languageManager.CurrentLanguage == "fr" ? "Français" : "English";
            languagePanel.Controls.Add(_languageComboBox, 1, 0);

            // Log format selection
            languagePanel.Controls.Add(new Label 
            { 
                Text = _languageManager.GetTranslation("settings.logFormat"),
                Anchor = AnchorStyles.Right,
                AutoSize = true
            }, 0, 1);

            _logFormatComboBox = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _logFormatComboBox.Items.AddRange(new object[] { "JSON", "XML" });
            _logFormatComboBox.SelectedItem = _settingsController.GetCurrentLogFormat().ToString();
            languagePanel.Controls.Add(_logFormatComboBox, 1, 1);

            businessSoftwareTab.Controls.Add(languagePanel);
        }

        private void SetupEventHandlers()
        {
            _languageManager.LanguageChanged += OnLanguageChanged;
            _languageComboBox.SelectedIndexChanged += OnLanguageComboBoxChanged;
            _logFormatComboBox.SelectedIndexChanged += OnLogFormatComboBoxChanged;
            addBusinessSoftwareButton.Click += OnAddBusinessSoftwareClick;
            removeBusinessSoftwareButton.Click += OnRemoveBusinessSoftwareClick;
            addEncryptionButton.Click += OnAddEncryptionClick;
            removeEncryptionButton.Click += OnRemoveEncryptionClick;
            saveButton.Click += OnSaveClick;
            cancelButton.Click += OnCancelClick;
            this.Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            LoadBusinessSoftware();
            LoadEncryptionExtensions();
            UpdateTranslations();
        }

        private void OnLanguageChanged(object sender, string languageCode)
        {
            UpdateTranslations();
        }

        private void OnLanguageComboBoxChanged(object sender, EventArgs e)
        {
            var language = _languageComboBox.SelectedItem.ToString() == "Français" ? "fr" : "en";
            _languageManager.SetLanguage(language);
        }

        private void OnLogFormatComboBoxChanged(object sender, EventArgs e)
        {
            var logFormat = (LogFormat)Enum.Parse(typeof(LogFormat), _logFormatComboBox.SelectedItem.ToString());
            _settingsController.SetLogFormat(logFormat);
        }

        private void UpdateTranslations()
        {
            Text = _languageManager.GetTranslation("settings.title");
            businessSoftwareTab.Text = _languageManager.GetTranslation("settings.tab.businessSoftware");
            encryptionTab.Text = _languageManager.GetTranslation("settings.tab.encryption");
            businessSoftwareColumn.Text = _languageManager.GetTranslation("settings.businessSoftware.name");
            encryptionColumn.Text = _languageManager.GetTranslation("settings.encryption.extension");
            addBusinessSoftwareButton.Text = _languageManager.GetTranslation("settings.businessSoftware.add");
            removeBusinessSoftwareButton.Text = _languageManager.GetTranslation("settings.businessSoftware.remove");
            addEncryptionButton.Text = _languageManager.GetTranslation("settings.encryption.add");
            removeEncryptionButton.Text = _languageManager.GetTranslation("settings.encryption.remove");
            saveButton.Text = _languageManager.GetTranslation("button.save");
            cancelButton.Text = _languageManager.GetTranslation("button.cancel");
        }

        private void LoadBusinessSoftware()
        {
            businessSoftwareList.Items.Clear();
            foreach (var software in _settingsController.GetBusinessSoftware())
            {
                businessSoftwareList.Items.Add(software);
            }
        }

        private void LoadEncryptionExtensions()
        {
            encryptionList.Items.Clear();
            foreach (var extension in _settingsController.GetEncryptionExtensions())
            {
                encryptionList.Items.Add(extension);
            }
        }

        private void OnAddBusinessSoftwareClick(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog(_languageManager.GetTranslation("settings.businessSoftware.enter")))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var softwareName = dialog.InputText.Trim();
                    if (!string.IsNullOrEmpty(softwareName))
                    {
                        _settingsController.AddBusinessSoftware(softwareName);
                        LoadBusinessSoftware();
                    }
                }
            }
        }

        private void OnRemoveBusinessSoftwareClick(object sender, EventArgs e)
        {
            if (businessSoftwareList.SelectedItems.Count > 0)
            {
                var softwareName = businessSoftwareList.SelectedItems[0].Text;
                _settingsController.RemoveBusinessSoftware(softwareName);
                LoadBusinessSoftware();
            }
        }

        private void OnAddEncryptionClick(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog(_languageManager.GetTranslation("settings.encryption.enter")))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var extension = dialog.InputText.Trim();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        if (!extension.StartsWith("."))
                        {
                            extension = "." + extension;
                        }
                        _settingsController.AddEncryptionExtension(extension);
                        LoadEncryptionExtensions();
                    }
                }
            }
        }

        private void OnRemoveEncryptionClick(object sender, EventArgs e)
        {
            if (encryptionList.SelectedItems.Count > 0)
            {
                var extension = encryptionList.SelectedItems[0].Text;
                _settingsController.RemoveEncryptionExtension(extension);
                LoadEncryptionExtensions();
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }
    }

    public class InputDialog : Form
    {
        private TextBox _textBox;
        private Button _okButton;
        private Button _cancelButton;
        private Label _label;

        public string InputText => _textBox.Text;

        public InputDialog(string prompt)
        {
            InitializeComponents(prompt);
        }

        private void InitializeComponents(string prompt)
        {
            _label = new Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(12, 9),
                Text = prompt
            };

            _textBox = new TextBox
            {
                Location = new System.Drawing.Point(12, 29),
                Size = new System.Drawing.Size(260, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(116, 58),
                Size = new System.Drawing.Size(75, 23),
                Text = "OK",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            _cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(197, 58),
                Size = new System.Drawing.Size(75, 23),
                Text = "Cancel",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            Controls.AddRange(new Control[] { _label, _textBox, _okButton, _cancelButton });
            ClientSize = new System.Drawing.Size(284, 93);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Input";
        }
    }
}