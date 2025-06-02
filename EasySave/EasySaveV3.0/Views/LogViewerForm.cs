using System;
using System.Drawing;
using System.Windows.Forms;
using EasySaveLogging;
using System.Linq;
using EasySaveV3._0.Managers;
using System.IO;

namespace EasySaveV3._0.Views
{
    public class LogViewerForm : Form
    {
        private readonly ListView _logListView;
        private readonly ComboBox _severityFilter;
        private readonly Button _refreshButton;
        private readonly Button _openLogFileButton;
        private readonly Logger _logger;
        private readonly LanguageManager _languageManager;

        private static readonly string[] SeverityLevels = { "All", "DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL" };

        public LogViewerForm(Logger logger, LanguageManager languageManager)
        {
            _logger = logger;
            _languageManager = languageManager;

            // Configure form
            this.Text = _languageManager.GetTranslation("logs.title");
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1700, 700);
            this.WindowState = FormWindowState.Maximized;

            // Create controls
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(20, 20, 20, 10)
            };

            // Severity filter
            var severityLabel = new Label
            {
                Location = new Point(20, 25),
                Text = _languageManager.GetTranslation("logs.severity"),
                AutoSize = true,
                Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold)
            };

            _severityFilter = new ComboBox
            {
                Location = new Point(150, 22),
                Width = 250,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(this.Font.FontFamily, 11)
            };
            _severityFilter.Items.AddRange(SeverityLevels);
            _severityFilter.SelectedIndex = 0;

            // Buttons
            _refreshButton = new Button
            {
                Location = new Point(420, 22),
                Width = 180,
                Height = 40,
                Text = _languageManager.GetTranslation("logs.refresh"),
                Font = new Font(this.Font.FontFamily, 10),
                Image = SystemIcons.Application.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0),
                FlatStyle = FlatStyle.System
            };
            _refreshButton.Click += RefreshButton_Click;

            _openLogFileButton = new Button
            {
                Location = new Point(610, 22),
                Width = 180,
                Height = 40,
                Text = _languageManager.GetTranslation("logs.openFile"),
                Font = new Font(this.Font.FontFamily, 10),
                Image = SystemIcons.Application.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(8, 0, 8, 0),
                FlatStyle = FlatStyle.System
            };
            _openLogFileButton.Click += OpenLogFileButton_Click;

            filterPanel.Controls.AddRange(new Control[] { 
                severityLabel, _severityFilter,
                _refreshButton, _openLogFileButton 
            });

            _logListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font(this.Font.FontFamily, 11),
                BackColor = Color.White
            };

            // Add columns with adjusted widths
            _logListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.timestamp"), Width = 200 },
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.severity"), Width = 120 },
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.backupName"), Width = 200 },
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.backupType"), Width = 150 },
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.action"), Width = 200 },
                new ColumnHeader { Text = _languageManager.GetTranslation("logs.message"), Width = 800 }
            });

            // Add controls to form
            this.Controls.Add(_logListView);
            this.Controls.Add(filterPanel);

            // Load initial data
            LoadLogs();
            LoadBackupNames();
        }

        private void LoadBackupNames()
        {
            var entries = _logger.ReadAllEntries();
            var backupNames = entries.Select(e => e.BackupName).Distinct().OrderBy(n => n).ToList();
        }

        private void LoadLogs()
        {
            try
            {
                _logListView.Items.Clear();
                var entries = _logger.ReadAllEntries();

                // Apply severity filter only
                var filteredEntries = entries.Where(e =>
                {
                    if (_severityFilter.SelectedIndex > 0 && e.LogType != _severityFilter.SelectedItem.ToString())
                        return false;
                    return true;
                });

                foreach (var entry in filteredEntries.OrderByDescending(e => e.Timestamp))
                {
                    var item = new ListViewItem(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.AddRange(new[]
                    {
                        entry.LogType,
                        entry.BackupName,
                        entry.BackupType ?? "Unknown",
                        entry.ActionType,
                        entry.Message
                    });

                    // Set color based on severity
                    item.BackColor = GetColorForSeverity(entry.LogType);
                    _logListView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("logs.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("logs.errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private Color GetColorForSeverity(string severity)
        {
            return severity switch
            {
                "DEBUG" => Color.LightGray,
                "INFO" => Color.White,
                "WARNING" => Color.LightYellow,
                "ERROR" => Color.LightPink,
                "CRITICAL" => Color.Red,
                _ => Color.White
            };
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadLogs();
        }

        private void OpenLogFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                var logPath = _logger.GetLogFilePath();
                if (File.Exists(logPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        _languageManager.GetTranslation("logs.fileNotFound"),
                        _languageManager.GetTranslation("logs.errorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    _languageManager.GetTranslation("logs.error").Replace("{0}", ex.Message),
                    _languageManager.GetTranslation("logs.errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
} 