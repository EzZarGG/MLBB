using EasySaveV3._0.Controllers;
namespace EasySaveV3._0
{
    partial class MainForm
    {
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ListView _backupListView;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _statusLabel;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Timer _updateTimer;
        private System.Windows.Forms.Button _pauseButton;
        private System.Windows.Forms.Button _resumeButton;
        private System.Windows.Forms.Button _stopButton;
        private System.Windows.Forms.Panel _controlPanel;

        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._toolStrip = new System.Windows.Forms.ToolStrip();
            this._backupListView = new System.Windows.Forms.ListView();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._updateTimer = new System.Windows.Forms.Timer(this.components);
            this._controlPanel = new System.Windows.Forms.Panel();
            this._pauseButton = new System.Windows.Forms.Button();
            this._resumeButton = new System.Windows.Forms.Button();
            this._stopButton = new System.Windows.Forms.Button();
            this._statusStrip.SuspendLayout();
            this._controlPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _menuStrip
            // 
            this._menuStrip.Dock = System.Windows.Forms.DockStyle.Top;
            this._menuStrip.Location = new System.Drawing.Point(0, 0);
            this._menuStrip.Name = "_menuStrip";
            this._menuStrip.Size = new System.Drawing.Size(1280, 24);
            this._menuStrip.TabIndex = 0;
            // 
            // _toolStrip
            // 
            this._toolStrip.Dock = System.Windows.Forms.DockStyle.Top;
            this._toolStrip.Location = new System.Drawing.Point(0, 24);
            this._toolStrip.Name = "_toolStrip";
            this._toolStrip.Size = new System.Drawing.Size(1280, 25);
            this._toolStrip.TabIndex = 1;
            // 
            // _backupListView
            // 
            this._backupListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._backupListView.FullRowSelect = true;
            this._backupListView.GridLines = true;
            this._backupListView.Location = new System.Drawing.Point(0, 24);
            this._backupListView.MultiSelect = false;
            this._backupListView.Name = "_backupListView";
            this._backupListView.Size = new System.Drawing.Size(1280, 706);
            this._backupListView.TabIndex = 2;
            this._backupListView.UseCompatibleStateImageBehavior = false;
            this._backupListView.View = System.Windows.Forms.View.Details;
            // 
            // _statusStrip
            // 
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this._statusLabel
            });
            this._statusStrip.Location = new System.Drawing.Point(0, 780);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(1280, 20);
            this._statusStrip.TabIndex = 3;
            // 
            // _statusLabel
            // 
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(0, 15);
            this._statusLabel.Spring = true;
            this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _progressBar
            // 
            this._progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._progressBar.Location = new System.Drawing.Point(0, 760);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(1280, 20);
            this._progressBar.TabIndex = 4;
            this._progressBar.Visible = false;
            // 
            // _updateTimer
            // 
            this._updateTimer.Interval = 1000;
            this._updateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // _controlPanel
            // 
            this._controlPanel.Controls.Add(this._pauseButton);
            this._controlPanel.Controls.Add(this._resumeButton);
            this._controlPanel.Controls.Add(this._stopButton);
            this._controlPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._controlPanel.Location = new System.Drawing.Point(0, 730);
            this._controlPanel.Name = "_controlPanel";
            this._controlPanel.Size = new System.Drawing.Size(1280, 30);
            this._controlPanel.TabIndex = 5;
            // 
            // _pauseButton
            // 
            this._pauseButton.Location = new System.Drawing.Point(10, 5);
            this._pauseButton.Name = "_pauseButton";
            this._pauseButton.Size = new System.Drawing.Size(100, 23);
            this._pauseButton.TabIndex = 0;
            this._pauseButton.Text = "Pause";
            this._pauseButton.UseVisualStyleBackColor = true;
            this._pauseButton.Click += new System.EventHandler(this.PauseButton_Click);
            // 
            // _resumeButton
            // 
            this._resumeButton.Location = new System.Drawing.Point(120, 5);
            this._resumeButton.Name = "_resumeButton";
            this._resumeButton.Size = new System.Drawing.Size(100, 23);
            this._resumeButton.TabIndex = 1;
            this._resumeButton.Text = "Resume";
            this._resumeButton.UseVisualStyleBackColor = true;
            this._resumeButton.Click += new System.EventHandler(this.ResumeButton_Click);
            // 
            // _stopButton
            // 
            this._stopButton.Location = new System.Drawing.Point(230, 5);
            this._stopButton.Name = "_stopButton";
            this._stopButton.Size = new System.Drawing.Size(100, 23);
            this._stopButton.TabIndex = 2;
            this._stopButton.Text = "Stop";
            this._stopButton.UseVisualStyleBackColor = true;
            this._stopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.MinimumSize = new System.Drawing.Size(1024, 600);
            this.Controls.Add(this._backupListView);
            this.Controls.Add(this._controlPanel);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._statusStrip);
            this.Controls.Add(this._menuStrip);
            this.Icon = System.Drawing.SystemIcons.Application;
            this.MainMenuStrip = this._menuStrip;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EasySave";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            this._controlPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

       

        #endregion
    }
}
