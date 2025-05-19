using System;
using System.Windows.Forms;
using EasySaveV2._0.Managers;

namespace EasySaveV2._0.Views
{
    public partial class LanguageSelectionForm : Form
    {
        private readonly LanguageManager _languageManager;
        private bool _languageSelected;
        private Label _languageLabel = new();
        private ComboBox _languageComboBox = new();
        private Button _okButton = new();
        private Button _cancelButton = new();

        public LanguageSelectionForm()
        {
            _languageManager = LanguageManager.Instance;
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
            this.Size = new System.Drawing.Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Layout panel
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10)
            };

            // Language label
            _languageLabel = new Label
            {
                Tag = "language.selection.choose",
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize = true
            };
            layout.Controls.Add(_languageLabel, 0, 0);

            // Language combo box
            _languageComboBox = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left
            };
            layout.Controls.Add(_languageComboBox, 1, 0);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5)
            };

            _okButton = new Button
            {
                Tag = "button.ok",
                Width = 80
            };
            _cancelButton = new Button
            {
                Tag = "button.cancel",
                Width = 80
            };

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_okButton);

            this.Controls.Add(layout);
            this.Controls.Add(buttonPanel);
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
            if (!_languageSelected)
            {
                var result = MessageBox.Show(
                    _languageManager.GetTranslation("language.selection.exit.confirm"),
                    _languageManager.GetTranslation("language.selection.exit.title"),
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
                Close();
            }
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