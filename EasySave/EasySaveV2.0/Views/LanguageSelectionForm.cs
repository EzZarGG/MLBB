using System;
using System.Windows.Forms;
using EasySaveV2._0.Managers;

namespace EasySaveV2._0.Views
{
    public partial class LanguageSelectionForm : Form
    {
        private readonly LanguageManager _languageManager;
        private bool _languageSelected;
        private readonly bool _isInitialLaunch;
        private Label _languageLabel = new();
        private ComboBox _languageComboBox = new();
        private Button _okButton = new();
        private Button _cancelButton = new();

        public LanguageSelectionForm(bool isInitialLaunch = false)
        {
            _languageManager = LanguageManager.Instance;
            _isInitialLaunch = isInitialLaunch;
            InitializeComponent();
            _languageSelected = false;

            InitializeUI();
            SetupEventHandlers();
            LoadLanguages();
            UpdateFormTexts();
        }

        private void InitializeUI()
        {
            // Form properties
            this.Text = _languageManager.GetTranslation("language.selection.title");
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Layout panel
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20),
                RowStyles = {
                    new RowStyle(SizeType.Percent, 40),  // Title
                    new RowStyle(SizeType.Percent, 30),  // ComboBox
                    new RowStyle(SizeType.Percent, 30)   // Buttons
                }
            };

            // Title label
            var titleLabel = new Label
            {
                Tag = "language.selection.title",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = false
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Language combo box
            _languageComboBox = new ComboBox
            {
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.None,
                Font = new Font(this.Font.FontFamily, 12)
            };
            var comboBoxPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            comboBoxPanel.Controls.Add(_languageComboBox);
            _languageComboBox.Location = new Point(
                (comboBoxPanel.Width - _languageComboBox.Width) / 2,
                (comboBoxPanel.Height - _languageComboBox.Height) / 2
            );
            layout.Controls.Add(comboBoxPanel, 0, 1);

            // Button panel
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = false,
                Height = 40,
                ColumnStyles = {
                    new ColumnStyle(SizeType.Percent, 50),
                    new ColumnStyle(SizeType.Percent, 50)
                }
            };

            _okButton = new Button
            {
                Tag = "button.ok",
                Width = 100,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.None
            };
            _cancelButton = new Button
            {
                Tag = "button.cancel",
                Width = 100,
                Height = 35,
                Font = new Font(this.Font.FontFamily, 10),
                Anchor = AnchorStyles.None
            };

            buttonPanel.Controls.Add(_okButton, 0, 0);
            buttonPanel.Controls.Add(_cancelButton, 1, 0);
            layout.Controls.Add(buttonPanel, 0, 2);

            // Cleanup: don't add controls twice
            this.Controls.Clear();
            this.Controls.Add(layout);
        }

        private void SetupEventHandlers()
        {
            _languageManager.LanguageChanged += OnLanguageChanged;
            _okButton.Click += OnOkClick;
            _cancelButton.Click += OnCancelClick;
        }

        private void OnLanguageChanged(object? sender, string languageCode)
        {
            UpdateFormTexts();
        }

        private void UpdateFormTexts()
        {
            // Update form title
            this.Text = _languageManager.GetTranslation("language.selection.title");

            // Update all controls with tags
            UpdateControlTexts(this.Controls);

            // Update combo box items
            LoadLanguages();
        }

        private void UpdateControlTexts(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag is string key)
                {
                    control.Text = _languageManager.GetTranslation(key);
                }
                if (control.HasChildren)
                {
                    UpdateControlTexts(control.Controls);
                }
            }
        }

        private void LoadLanguages()
        {
            _languageComboBox.Items.Clear();
            foreach (var language in _languageManager.GetAvailableLanguages())
            {
                var languageName = _languageManager.GetTranslation($"language.{language.ToLower()}");
                _languageComboBox.Items.Add(new LanguageItem(language, languageName));
            }

            var currentLanguage = _languageManager.CurrentLanguage;
            var currentItem = _languageComboBox.Items.Cast<LanguageItem>()
                .FirstOrDefault(item => item.Code == currentLanguage);
            if (currentItem != null)
            {
                _languageComboBox.SelectedItem = currentItem;
            }
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            if (_languageComboBox.SelectedItem is LanguageItem selectedLanguage)
            {
                _languageManager.SetLanguage(selectedLanguage.Code);
                _languageSelected = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("language.selection.required"),
                    _languageManager.GetTranslation("language.selection.error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            if (_isInitialLaunch && !_languageSelected)
            {
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("language.selection.exitConfirm"),
                    _languageManager.GetTranslation("language.selection.exitTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    Application.Exit();
                    return;
                }
            }
            
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _languageManager.LanguageChanged -= OnLanguageChanged;
        }

        private class LanguageItem
        {
            public string Code { get; }
            public string Name { get; }

            public LanguageItem(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}