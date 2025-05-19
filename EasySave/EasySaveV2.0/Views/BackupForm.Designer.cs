namespace EasySaveV2._0.Views
{
    partial class BackupForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.nameLabel = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.btnSourceBrowse = new System.Windows.Forms.Button();
            this.targetLabel = new System.Windows.Forms.Label();
            this.txtTarget = new System.Windows.Forms.TextBox();
            this.btnTargetBrowse = new System.Windows.Forms.Button();
            this.typeLabel = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.mainPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainPanel
            // 
            this.mainPanel.ColumnCount = 3;
            this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.mainPanel.Controls.Add(this.nameLabel, 0, 0);
            this.mainPanel.Controls.Add(this.txtName, 1, 0);
            this.mainPanel.Controls.Add(this.sourceLabel, 0, 1);
            this.mainPanel.Controls.Add(this.txtSource, 1, 1);
            this.mainPanel.Controls.Add(this.btnSourceBrowse, 2, 1);
            this.mainPanel.Controls.Add(this.targetLabel, 0, 2);
            this.mainPanel.Controls.Add(this.txtTarget, 1, 2);
            this.mainPanel.Controls.Add(this.btnTargetBrowse, 2, 2);
            this.mainPanel.Controls.Add(this.typeLabel, 0, 3);
            this.mainPanel.Controls.Add(this.cmbType, 1, 3);
            this.mainPanel.Controls.Add(this.buttonPanel, 1, 4);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Padding = new System.Windows.Forms.Padding(20);
            this.mainPanel.RowCount = 5;
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainPanel.Size = new System.Drawing.Size(500, 300);
            this.mainPanel.TabIndex = 0;
            // 
            // nameLabel
            // 
            this.nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nameLabel.AutoSize = true;
            this.nameLabel.Location = new System.Drawing.Point(23, 20);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(82, 15);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Backup Name:";
            // 
            // txtName
            // 
            this.txtName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtName.Location = new System.Drawing.Point(148, 17);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(280, 23);
            this.txtName.TabIndex = 1;
            // 
            // sourceLabel
            // 
            this.sourceLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Location = new System.Drawing.Point(23, 80);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(75, 15);
            this.sourceLabel.TabIndex = 2;
            this.sourceLabel.Text = "Source Path:";
            // 
            // txtSource
            // 
            this.txtSource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtSource.Location = new System.Drawing.Point(148, 77);
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(280, 23);
            this.txtSource.TabIndex = 3;
            // 
            // btnSourceBrowse
            // 
            this.btnSourceBrowse.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSourceBrowse.Location = new System.Drawing.Point(434, 77);
            this.btnSourceBrowse.Name = "btnSourceBrowse";
            this.btnSourceBrowse.Size = new System.Drawing.Size(60, 23);
            this.btnSourceBrowse.TabIndex = 4;
            this.btnSourceBrowse.Text = "Browse";
            this.btnSourceBrowse.UseVisualStyleBackColor = true;
            this.btnSourceBrowse.Click += new System.EventHandler(this.OnSourceBrowseClick);
            // 
            // targetLabel
            // 
            this.targetLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.targetLabel.AutoSize = true;
            this.targetLabel.Location = new System.Drawing.Point(23, 140);
            this.targetLabel.Name = "targetLabel";
            this.targetLabel.Size = new System.Drawing.Size(73, 15);
            this.targetLabel.TabIndex = 5;
            this.targetLabel.Text = "Target Path:";
            // 
            // txtTarget
            // 
            this.txtTarget.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtTarget.Location = new System.Drawing.Point(148, 137);
            this.txtTarget.Name = "txtTarget";
            this.txtTarget.Size = new System.Drawing.Size(280, 23);
            this.txtTarget.TabIndex = 6;
            // 
            // btnTargetBrowse
            // 
            this.btnTargetBrowse.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnTargetBrowse.Location = new System.Drawing.Point(434, 137);
            this.btnTargetBrowse.Name = "btnTargetBrowse";
            this.btnTargetBrowse.Size = new System.Drawing.Size(60, 23);
            this.btnTargetBrowse.TabIndex = 7;
            this.btnTargetBrowse.Text = "Browse";
            this.btnTargetBrowse.UseVisualStyleBackColor = true;
            this.btnTargetBrowse.Click += new System.EventHandler(this.OnTargetBrowseClick);
            // 
            // typeLabel
            // 
            this.typeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.typeLabel.AutoSize = true;
            this.typeLabel.Location = new System.Drawing.Point(23, 200);
            this.typeLabel.Name = "typeLabel";
            this.typeLabel.Size = new System.Drawing.Size(73, 15);
            this.typeLabel.TabIndex = 8;
            this.typeLabel.Text = "Backup Type:";
            // 
            // cmbType
            // 
            this.cmbType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Items.AddRange(new object[] {
            "Full",
            "Differential"});
            this.cmbType.Location = new System.Drawing.Point(148, 197);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(280, 23);
            this.cmbType.TabIndex = 9;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.Controls.Add(this.btnSave);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Location = new System.Drawing.Point(148, 260);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(280, 40);
            this.buttonPanel.TabIndex = 10;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(3, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 34);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.OnSaveClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(177, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 34);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            // 
            // BackupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 300);
            this.Controls.Add(this.mainPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BackupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Backup Job";
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainPanel;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.TextBox txtSource;
        private System.Windows.Forms.Button btnSourceBrowse;
        private System.Windows.Forms.Label targetLabel;
        private System.Windows.Forms.TextBox txtTarget;
        private System.Windows.Forms.Button btnTargetBrowse;
        private System.Windows.Forms.Label typeLabel;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
} 