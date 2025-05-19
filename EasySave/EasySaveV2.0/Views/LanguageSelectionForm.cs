using EasySaveV2._0.Managers;
using System.Windows.Forms;

namespace EasySaveV2._0.Views
{
    public partial class LanguageSelectionForm : Form
    {
        private readonly LanguageManager _languageManager;
        private readonly ComboBox _languageComboBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;

        public LanguageSelectionForm()
        {
            _languageManager = LanguageManager.Instance;
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // LanguageSelectionForm
            // 
            ClientSize = new Size(284, 261);
            Name = "LanguageSelectionForm";
            Load += LanguageSelectionForm_Load;
            ResumeLayout(false);
        }

        private void InitializeUI()
        {
            this.Text = "EasySave - Language Selection";
            this.Size = new Size(300, 150);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label
            {
                Text = "Select your language / Choisissez votre langue",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(10)
            };

            var frenchButton = new Button
            {
                Text = "Français",
                Dock = DockStyle.Left,
                Width = 120,
                Margin = new Padding(10)
            };
            frenchButton.Click += (s, e) => SelectLanguage("fr");

            var englishButton = new Button
            {
                Text = "English",
                Dock = DockStyle.Right,
                Width = 120,
                Margin = new Padding(10)
            };
            englishButton.Click += (s, e) => SelectLanguage("en");

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };
            buttonPanel.Controls.Add(frenchButton);
            buttonPanel.Controls.Add(englishButton);

            this.Controls.Add(label);
            this.Controls.Add(buttonPanel);
        }

        private void SelectLanguage(string language)
        {
            _languageManager.SetLanguage(language);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
        }

        private void LanguageSelectionForm_Load(object sender, EventArgs e)
        {

        }
    }
} 