using System;
using System.Windows.Forms;
using EasySaveV2._0.Controllers;
using EasySaveV2._0.Managers;

namespace EasySaveV2._0.Views
{
    public partial class SettingsForm : Form
    {
        private readonly SettingsController _settingsController;
        private readonly LanguageManager _languageManager;

        public SettingsForm()
        {
            InitializeComponent();
            _settingsController = new SettingsController();
            _languageManager = LanguageManager.Instance;

            // Initialize tabs
            tabControl.TabPages.Add(businessSoftwareTab);
            tabControl.TabPages.Add(encryptionTab);

            // Add controls to tabs
            businessSoftwareTab.Controls.Add(businessSoftwareList);
            businessSoftwareTab.Controls.Add(addBusinessSoftwareButton);
            businessSoftwareTab.Controls.Add(removeBusinessSoftwareButton);

            encryptionTab.Controls.Add(encryptionList);
            encryptionTab.Controls.Add(addEncryptionButton);
            encryptionTab.Controls.Add(removeEncryptionButton);

            // Subscribe to language changes
            _languageManager.LanguageChanged += OnLanguageChanged;
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

        private void UpdateTranslations()
        {
            Text = _languageManager.GetTranslation("Settings");
            businessSoftwareTab.Text = _languageManager.GetTranslation("Business Software");
            encryptionTab.Text = _languageManager.GetTranslation("Encryption");
            addBusinessSoftwareButton.Text = _languageManager.GetTranslation("Add Software");
            removeBusinessSoftwareButton.Text = _languageManager.GetTranslation("Remove Software");
            addEncryptionButton.Text = _languageManager.GetTranslation("Add Extension");
            removeEncryptionButton.Text = _languageManager.GetTranslation("Remove Extension");
            saveButton.Text = _languageManager.GetTranslation("Save");
            cancelButton.Text = _languageManager.GetTranslation("Cancel");
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
            using (var dialog = new InputDialog(_languageManager.GetTranslation("Enter Software Name")))
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
            using (var dialog = new InputDialog(_languageManager.GetTranslation("Enter File Extension")))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var extension = dialog.InputText.Trim();
                    if (!string.IsNullOrEmpty(extension))
                    {
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