using System;
using System.Windows.Forms;
using EasySaveV2._0.Managers;

namespace EasySaveV2._0.Views
{
    public partial class LanguageSelectionForm : Form
    {
        private readonly LanguageManager _languageManager;
        private bool _languageSelected;

        public LanguageSelectionForm()
        {
            InitializeComponent();
            _languageManager = LanguageManager.Instance;
            _languageManager.LanguageChanged += OnLanguageChanged;
            LoadLanguages();
        }

        private void OnLanguageChanged(object sender, string languageCode)
        {
            UpdateTranslations();
        }

        private void UpdateTranslations()
        {
            Text = _languageManager.GetTranslation("Select Language");
            languageLabel.Text = _languageManager.GetTranslation("Choose your language:");
            okButton.Text = _languageManager.GetTranslation("OK");
            cancelButton.Text = _languageManager.GetTranslation("Cancel");
        }

        private void LoadLanguages()
        {
            languageComboBox.Items.Clear();
            foreach (var language in _languageManager.GetAvailableLanguages())
            {
                languageComboBox.Items.Add(language);
            }

            var currentLanguage = _languageManager.GetCurrentLanguage();
            languageComboBox.SelectedItem = currentLanguage;
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (languageComboBox.SelectedItem != null)
            {
                var selectedLanguage = languageComboBox.SelectedItem.ToString();
                _languageManager.SetLanguage(selectedLanguage);
                _languageSelected = true;
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("Please select a language"),
                    _languageManager.GetTranslation("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            if (!_languageSelected)
            {
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("No language selected. Do you want to exit?"),
                    _languageManager.GetTranslation("Exit"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    Application.Exit();
                }
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
} 