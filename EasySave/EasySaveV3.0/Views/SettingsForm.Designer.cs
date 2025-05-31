namespace EasySaveV3._0.Views
{
    partial class SettingsForm : System.Windows.Forms.Form
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
        private System.Windows.Forms.TabPage transfersTab;
        private System.Windows.Forms.Label maxLargeFileLabel;
        private System.Windows.Forms.NumericUpDown maxLargeFileNumeric;

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
            this.transfersTab = new System.Windows.Forms.TabPage();
            this.maxLargeFileLabel = new System.Windows.Forms.Label();
            this.maxLargeFileNumeric = new System.Windows.Forms.NumericUpDown();

            ((System.ComponentModel.ISupportInitialize)(this.maxLargeFileNumeric)).BeginInit();

            // Form properties
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 500);
            this.MinimumSize = new System.Drawing.Size(600, 500);
            this.Name = "SettingsForm";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Settings";

            // TabControl
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(10, 10);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(564, 400);
            this.tabControl.TabIndex = 0;

            // Business Software Tab
            this.businessSoftwareTab.Tag = "settings.tab.businessSoftware";
            this.businessSoftwareTab.UseVisualStyleBackColor = true;
            this.businessSoftwareTab.Padding = new System.Windows.Forms.Padding(10);

            // Encryption Tab
            this.encryptionTab.Tag = "settings.tab.encryption";
            this.encryptionTab.UseVisualStyleBackColor = true;
            this.encryptionTab.Padding = new System.Windows.Forms.Padding(10);

            // Business Software List
            this.businessSoftwareList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.businessSoftwareList.FullRowSelect = true;
            this.businessSoftwareList.GridLines = true;
            this.businessSoftwareList.Location = new System.Drawing.Point(10, 10);
            this.businessSoftwareList.Name = "businessSoftwareList";
            this.businessSoftwareList.Size = new System.Drawing.Size(536, 300);
            this.businessSoftwareList.TabIndex = 0;
            this.businessSoftwareList.UseCompatibleStateImageBehavior = false;
            this.businessSoftwareList.View = System.Windows.Forms.View.Details;
            this.businessSoftwareList.Columns.Add(this.businessSoftwareColumn);
            this.businessSoftwareColumn.Tag = "settings.businessSoftware.name";
            this.businessSoftwareColumn.Width = 500;

            // Encryption List
            this.encryptionList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.encryptionList.FullRowSelect = true;
            this.encryptionList.GridLines = true;
            this.encryptionList.Location = new System.Drawing.Point(10, 10);
            this.encryptionList.Name = "encryptionList";
            this.encryptionList.Size = new System.Drawing.Size(536, 300);
            this.encryptionList.TabIndex = 0;
            this.encryptionList.UseCompatibleStateImageBehavior = false;
            this.encryptionList.View = System.Windows.Forms.View.Details;
            this.encryptionList.Columns.Add(this.encryptionColumn);
            this.encryptionColumn.Tag = "settings.encryption.extension";
            this.encryptionColumn.Width = 500;

            // Add Business Software Button
            this.addBusinessSoftwareButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addBusinessSoftwareButton.Location = new System.Drawing.Point(10, 320);
            this.addBusinessSoftwareButton.Name = "addBusinessSoftwareButton";
            this.addBusinessSoftwareButton.Size = new System.Drawing.Size(120, 30);
            this.addBusinessSoftwareButton.TabIndex = 1;
            this.addBusinessSoftwareButton.Tag = "settings.businessSoftware.add";
            this.addBusinessSoftwareButton.UseVisualStyleBackColor = true;

            // Remove Business Software Button
            this.removeBusinessSoftwareButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.removeBusinessSoftwareButton.Location = new System.Drawing.Point(140, 320);
            this.removeBusinessSoftwareButton.Name = "removeBusinessSoftwareButton";
            this.removeBusinessSoftwareButton.Size = new System.Drawing.Size(120, 30);
            this.removeBusinessSoftwareButton.TabIndex = 2;
            this.removeBusinessSoftwareButton.Tag = "settings.businessSoftware.remove";
            this.removeBusinessSoftwareButton.UseVisualStyleBackColor = true;

            // Add Encryption Button
            this.addEncryptionButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addEncryptionButton.Location = new System.Drawing.Point(10, 320);
            this.addEncryptionButton.Name = "addEncryptionButton";
            this.addEncryptionButton.Size = new System.Drawing.Size(120, 30);
            this.addEncryptionButton.TabIndex = 1;
            this.addEncryptionButton.Tag = "settings.encryption.add";
            this.addEncryptionButton.UseVisualStyleBackColor = true;

            // Remove Encryption Button
            this.removeEncryptionButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.removeEncryptionButton.Location = new System.Drawing.Point(140, 320);
            this.removeEncryptionButton.Name = "removeEncryptionButton";
            this.removeEncryptionButton.Size = new System.Drawing.Size(120, 30);
            this.removeEncryptionButton.TabIndex = 2;
            this.removeEncryptionButton.Tag = "settings.encryption.remove";
            this.removeEncryptionButton.UseVisualStyleBackColor = true;

            // Save Button
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(374, 450);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 30);
            this.saveButton.TabIndex = 3;
            this.saveButton.Tag = "button.save";
            this.saveButton.UseVisualStyleBackColor = true;

            // Cancel Button
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(474, 450);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 30);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Tag = "button.cancel";
            this.cancelButton.UseVisualStyleBackColor = true;

            // Add controls to tabs
            this.businessSoftwareTab.Controls.Add(this.businessSoftwareList);
            this.businessSoftwareTab.Controls.Add(this.addBusinessSoftwareButton);
            this.businessSoftwareTab.Controls.Add(this.removeBusinessSoftwareButton);

            this.encryptionTab.Controls.Add(this.encryptionList);
            this.encryptionTab.Controls.Add(this.addEncryptionButton);
            this.encryptionTab.Controls.Add(this.removeEncryptionButton);

            this.tabControl.TabPages.Add(this.businessSoftwareTab);
            this.tabControl.TabPages.Add(this.encryptionTab);

            // Add controls to form
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.cancelButton);

            this.tabControl.ResumeLayout(false);
            this.businessSoftwareTab.ResumeLayout(false);
            this.encryptionTab.ResumeLayout(false);
            this.ResumeLayout(false);

            this.transfersTab.Tag = "settings.tab.transfers";
            this.transfersTab.Text = "Transferts";
            this.transfersTab.UseVisualStyleBackColor = true;
            this.transfersTab.Padding = new System.Windows.Forms.Padding(10);

            // *** Configuration de l'onglet Transferts ***
            this.transfersTab.Tag = "settings.tab.transfers";
            this.transfersTab.Text = "Transferts";
            this.transfersTab.UseVisualStyleBackColor = true;
            this.transfersTab.Padding = new System.Windows.Forms.Padding(10);

            // Label
            this.maxLargeFileLabel.AutoSize = true;
            this.maxLargeFileLabel.Location = new System.Drawing.Point(10, 15);
            this.maxLargeFileLabel.Name = "maxLargeFileLabel";
            this.maxLargeFileLabel.Size = new System.Drawing.Size(180, 23);
            this.maxLargeFileLabel.TabIndex = 0;
            this.maxLargeFileLabel.Text = "Seuil « gros fichiers » (Ko) :";

            // NumericUpDown
            this.maxLargeFileNumeric.Location = new System.Drawing.Point(200, 12);
            this.maxLargeFileNumeric.Name = "maxLargeFileNumeric";
            this.maxLargeFileNumeric.Size = new System.Drawing.Size(120, 23);
            this.maxLargeFileNumeric.TabIndex = 1;
            this.maxLargeFileNumeric.DecimalPlaces = 0;
            this.maxLargeFileNumeric.Minimum = 1;
            this.maxLargeFileNumeric.Maximum = 1024 * 1024;  // jusqu'à 1 048 576 Ko
            this.maxLargeFileNumeric.Increment = 10;

            // Ajout des contrôles dans l'onglet Transferts
            this.transfersTab.Controls.Add(this.maxLargeFileLabel);
            this.transfersTab.Controls.Add(this.maxLargeFileNumeric);

            // Ajout de l'onglet Transferts au TabControl
            this.tabControl.TabPages.Add(this.transfersTab);

            // *** EndInit pour le NumericUpDown ***
            ((System.ComponentModel.ISupportInitialize)(this.maxLargeFileNumeric)).EndInit();

            // — Vos appels existants à ResumeLayout (tabControl, form, etc.) —
            this.tabControl.ResumeLayout(false);
            this.businessSoftwareTab.ResumeLayout(false);
            this.encryptionTab.ResumeLayout(false);
            this.ResumeLayout(false);


        }

    #endregion
}
}