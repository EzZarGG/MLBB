namespace EasySaveV2._0.Views
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage businessSoftwareTab;
        private System.Windows.Forms.TabPage encryptionTab;
        private System.Windows.Forms.ListView businessSoftwareList;
        private System.Windows.Forms.ListView encryptionList;
        private System.Windows.Forms.Button addBusinessSoftwareButton;
        private System.Windows.Forms.Button removeBusinessSoftwareButton;
        private System.Windows.Forms.Button addEncryptionButton;
        private System.Windows.Forms.Button removeEncryptionButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ColumnHeader businessSoftwareColumn;
        private System.Windows.Forms.ColumnHeader encryptionColumn;

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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.businessSoftwareTab = new System.Windows.Forms.TabPage();
            this.encryptionTab = new System.Windows.Forms.TabPage();
            this.businessSoftwareList = new System.Windows.Forms.ListView();
            this.businessSoftwareColumn = new System.Windows.Forms.ColumnHeader();
            this.encryptionList = new System.Windows.Forms.ListView();
            this.encryptionColumn = new System.Windows.Forms.ColumnHeader();
            this.addBusinessSoftwareButton = new System.Windows.Forms.Button();
            this.removeBusinessSoftwareButton = new System.Windows.Forms.Button();
            this.addEncryptionButton = new System.Windows.Forms.Button();
            this.removeEncryptionButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();

            // TabControl
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(10, 10);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(564, 300);
            this.tabControl.TabIndex = 0;

            // Business Software Tab
            this.businessSoftwareTab.Text = "Business Software";
            this.businessSoftwareTab.UseVisualStyleBackColor = true;
            this.businessSoftwareTab.Padding = new System.Windows.Forms.Padding(10);

            // Encryption Tab
            this.encryptionTab.Text = "Encryption";
            this.encryptionTab.UseVisualStyleBackColor = true;
            this.encryptionTab.Padding = new System.Windows.Forms.Padding(10);

            // Business Software List
            this.businessSoftwareList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.businessSoftwareList.FullRowSelect = true;
            this.businessSoftwareList.GridLines = true;
            this.businessSoftwareList.Location = new System.Drawing.Point(10, 10);
            this.businessSoftwareList.Name = "businessSoftwareList";
            this.businessSoftwareList.Size = new System.Drawing.Size(536, 200);
            this.businessSoftwareList.TabIndex = 0;
            this.businessSoftwareList.UseCompatibleStateImageBehavior = false;
            this.businessSoftwareList.View = System.Windows.Forms.View.Details;
            this.businessSoftwareList.Columns.Add(this.businessSoftwareColumn);
            this.businessSoftwareColumn.Text = "Software Name";
            this.businessSoftwareColumn.Width = 500;

            // Encryption List
            this.encryptionList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.encryptionList.FullRowSelect = true;
            this.encryptionList.GridLines = true;
            this.encryptionList.Location = new System.Drawing.Point(10, 10);
            this.encryptionList.Name = "encryptionList";
            this.encryptionList.Size = new System.Drawing.Size(536, 200);
            this.encryptionList.TabIndex = 0;
            this.encryptionList.UseCompatibleStateImageBehavior = false;
            this.encryptionList.View = System.Windows.Forms.View.Details;
            this.encryptionList.Columns.Add(this.encryptionColumn);
            this.encryptionColumn.Text = "Extension";
            this.encryptionColumn.Width = 500;

            // Add Business Software Button
            this.addBusinessSoftwareButton.Location = new System.Drawing.Point(10, 220);
            this.addBusinessSoftwareButton.Name = "addBusinessSoftwareButton";
            this.addBusinessSoftwareButton.Size = new System.Drawing.Size(120, 30);
            this.addBusinessSoftwareButton.TabIndex = 1;
            this.addBusinessSoftwareButton.Text = "Add Software";
            this.addBusinessSoftwareButton.UseVisualStyleBackColor = true;
            this.addBusinessSoftwareButton.Click += new System.EventHandler(this.OnAddBusinessSoftwareClick);

            // Remove Business Software Button
            this.removeBusinessSoftwareButton.Location = new System.Drawing.Point(140, 220);
            this.removeBusinessSoftwareButton.Name = "removeBusinessSoftwareButton";
            this.removeBusinessSoftwareButton.Size = new System.Drawing.Size(120, 30);
            this.removeBusinessSoftwareButton.TabIndex = 2;
            this.removeBusinessSoftwareButton.Text = "Remove Software";
            this.removeBusinessSoftwareButton.UseVisualStyleBackColor = true;
            this.removeBusinessSoftwareButton.Click += new System.EventHandler(this.OnRemoveBusinessSoftwareClick);

            // Add Encryption Button
            this.addEncryptionButton.Location = new System.Drawing.Point(10, 220);
            this.addEncryptionButton.Name = "addEncryptionButton";
            this.addEncryptionButton.Size = new System.Drawing.Size(120, 30);
            this.addEncryptionButton.TabIndex = 1;
            this.addEncryptionButton.Text = "Add Extension";
            this.addEncryptionButton.UseVisualStyleBackColor = true;
            this.addEncryptionButton.Click += new System.EventHandler(this.OnAddEncryptionClick);

            // Remove Encryption Button
            this.removeEncryptionButton.Location = new System.Drawing.Point(140, 220);
            this.removeEncryptionButton.Name = "removeEncryptionButton";
            this.removeEncryptionButton.Size = new System.Drawing.Size(120, 30);
            this.removeEncryptionButton.TabIndex = 2;
            this.removeEncryptionButton.Text = "Remove Extension";
            this.removeEncryptionButton.UseVisualStyleBackColor = true;
            this.removeEncryptionButton.Click += new System.EventHandler(this.OnRemoveEncryptionClick);

            // Save Button
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(374, 320);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 30);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.OnSaveClick);

            // Cancel Button
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(474, 320);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 30);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.OnCancelClick);

            // SettingsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.cancelButton);
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "SettingsForm";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.OnLoad);

            this.tabControl.ResumeLayout(false);
            this.businessSoftwareTab.ResumeLayout(false);
            this.encryptionTab.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
} 