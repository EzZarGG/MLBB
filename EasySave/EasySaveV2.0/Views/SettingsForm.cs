using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;
using EasySaveV2._0.Models;
using EasySaveLogging;

namespace EasySaveV2._0.Views
{
    public partial class SettingsForm : Form
    {
        private readonly LanguageManager _languageManager;
        private readonly SettingsController _settingsController;
        private TabControl _tabControl = new();
        private TabPage _businessSoftwareTab = new();
        private TabPage _encryptionTab = new();
        private TabPage _logFormatTab = new();
        private ListView _businessSoftwareList = new();
        private ListView _encryptionList = new();
        private Button _addBusinessSoftwareButton = new();
        private Button _removeBusinessSoftwareButton = new();
        private Button _addEncryptionButton = new();
        private Button _removeEncryptionButton = new();
        private Button _saveButton = new();
        private Button _cancelButton = new();
        private ColumnHeader _businessSoftwareColumn = new();
        private ColumnHeader _encryptionColumn = new();
        private ComboBox _logFormatComboBox = new();

        public SettingsForm(LanguageManager languageManager, SettingsController settingsController)
        {
            _languageManager = languageManager;
            _settingsController = settingsController;

            InitializeComponent();
            _languageManager.ReloadTranslations();
            InitializeUI();
            SetupEventHandlers();
            LoadSettings();
            UpdateFormTexts();
        }

        private void SetupEventHandlers()
        {
            _addBusinessSoftwareButton.Click += OnAddBusinessSoftwareClick;
            _removeBusinessSoftwareButton.Click += OnRemoveBusinessSoftwareClick;
            _addEncryptionButton.Click += OnAddEncryptionClick;
            _removeEncryptionButton.Click += OnRemoveEncryptionClick;
            _saveButton.Click += OnSaveClick;
            _cancelButton.Click += OnCancelClick;
        }

        private void LoadSettings()
        {
            LoadBusinessSoftware();
            LoadEncryptionExtensions();
        }

        private void InitializeUI()
        {
            // Initialize tab control
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 10, 10, 10)
            };
            _businessSoftwareTab = new TabPage(_languageManager.GetTranslation("settings.tab.businessSoftware"));
            _encryptionTab = new TabPage(_languageManager.GetTranslation("settings.tab.encryption"));
            _logFormatTab = new TabPage(_languageManager.GetTranslation("settings.tab.logFormat"));

            // Initialize business software list
            _businessSoftwareList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            _businessSoftwareColumn = new ColumnHeader
            {
                Text = _languageManager.GetTranslation("settings.businessSoftware.name"),
                Width = 200
            };
            _businessSoftwareList.Columns.Add(_businessSoftwareColumn);

            // Initialize encryption list
            _encryptionList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            _encryptionColumn = new ColumnHeader
            {
                Text = _languageManager.GetTranslation("settings.encryption.extension"),
                Width = 200
            };
            _encryptionList.Columns.Add(_encryptionColumn);

            // Initialize log format combo box
            _logFormatComboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };
            _logFormatComboBox.Items.AddRange(new object[] { LogFormat.JSON, LogFormat.XML });

            // Add label for log format
            var logFormatLabel = new Label
            {
                Text = _languageManager.GetTranslation("settings.logFormat.title"),
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 5)
            };

            // Add controls to log format tab
            _logFormatTab.Controls.Add(logFormatLabel);
            _logFormatTab.Controls.Add(_logFormatComboBox);

            // Initialize buttons
            _addBusinessSoftwareButton = new Button
            {
                Text = _languageManager.GetTranslation("settings.businessSoftware.add"),
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _removeBusinessSoftwareButton = new Button
            {
                Text = _languageManager.GetTranslation("settings.businessSoftware.remove"),
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _addEncryptionButton = new Button
            {
                Text = _languageManager.GetTranslation("settings.encryption.add"),
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _removeEncryptionButton = new Button
            {
                Text = _languageManager.GetTranslation("settings.encryption.remove"),
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _saveButton = new Button
            {
                Text = _languageManager.GetTranslation("button.save"),
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _cancelButton = new Button
            {
                Text = _languageManager.GetTranslation("button.cancel"),
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Bottom,
                Height = 30
            };

            // Create button panels
            var businessSoftwareButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };
            businessSoftwareButtonPanel.Controls.Add(_addBusinessSoftwareButton);
            businessSoftwareButtonPanel.Controls.Add(_removeBusinessSoftwareButton);

            var encryptionButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };
            encryptionButtonPanel.Controls.Add(_addEncryptionButton);
            encryptionButtonPanel.Controls.Add(_removeEncryptionButton);

            var mainButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft
            };
            mainButtonPanel.Controls.Add(_cancelButton);
            mainButtonPanel.Controls.Add(_saveButton);

            // Add controls to tabs
            _businessSoftwareTab.Controls.Add(_businessSoftwareList);
            _businessSoftwareTab.Controls.Add(businessSoftwareButtonPanel);

            _encryptionTab.Controls.Add(_encryptionList);
            _encryptionTab.Controls.Add(encryptionButtonPanel);

            _tabControl.TabPages.Add(_businessSoftwareTab);
            _tabControl.TabPages.Add(_encryptionTab);
            _tabControl.TabPages.Add(_logFormatTab);

            // Add controls to form
            Controls.Clear();
            Controls.Add(_tabControl);
            Controls.Add(mainButtonPanel);
        }

        private void LoadBusinessSoftware()
        {
            _businessSoftwareList.Items.Clear();
            foreach (var software in _settingsController.GetBusinessSoftware())
            {
                _businessSoftwareList.Items.Add(software);
            }
        }

        private void LoadEncryptionExtensions()
        {
            _encryptionList.Items.Clear();
            foreach (var extension in _settingsController.GetEncryptionExtensions())
            {
                _encryptionList.Items.Add(extension);
            }
        }

        private void OnAddBusinessSoftwareClick(object? sender, EventArgs e)
        {
            using (var dialog = new InputDialog(_languageManager.GetTranslation("settings.businessSoftware.add")))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _settingsController.AddBusinessSoftware(dialog.InputText);
                    LoadBusinessSoftware();
                }
            }
        }

        private void OnRemoveBusinessSoftwareClick(object? sender, EventArgs e)
        {
            if (_businessSoftwareList.SelectedItems.Count > 0)
            {
                _settingsController.RemoveBusinessSoftware(_businessSoftwareList.SelectedItems[0].Text);
                LoadBusinessSoftware();
            }
        }

        private void OnAddEncryptionClick(object? sender, EventArgs e)
        {
            using (var dialog = new InputDialog(_languageManager.GetTranslation("settings.encryption.add")))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _settingsController.AddEncryptionExtension(dialog.InputText);
                    LoadEncryptionExtensions();
                }
            }
        }

        private void OnRemoveEncryptionClick(object? sender, EventArgs e)
        {
            if (_encryptionList.SelectedItems.Count > 0)
            {
                _settingsController.RemoveEncryptionExtension(_encryptionList.SelectedItems[0].Text);
                LoadEncryptionExtensions();
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void UpdateControlTexts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag is string key)
                    control.Text = _languageManager.GetTranslation(key);
                if (control.HasChildren)
                    UpdateControlTexts(control.Controls);
            }
        }

        private void UpdateFormTexts()
        {
            // Update form title
            this.Text = _languageManager.GetTranslation("settings.title");
            // Update all controls with tags
            UpdateControlTexts(this.Controls);
        }
    }

    public class InputDialog : Form
    {
        private TextBox _textBox = new();
        private Button _okButton = new();
        private Button _cancelButton = new();
        private Label _label = new();

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