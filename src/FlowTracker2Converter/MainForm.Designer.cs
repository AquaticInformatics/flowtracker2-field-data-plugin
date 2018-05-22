namespace FlowTracker2Converter
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBoxAqTs = new System.Windows.Forms.PictureBox();
            this.convertButton = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.clearButton = new System.Windows.Forms.Button();
            this.viewLicenseButton = new System.Windows.Forms.Button();
            this.licenseCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAqTs)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxAqTs
            // 
            this.pictureBoxAqTs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxAqTs.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxAqTs.Image")));
            this.pictureBoxAqTs.Location = new System.Drawing.Point(287, 12);
            this.pictureBoxAqTs.Name = "pictureBoxAqTs";
            this.pictureBoxAqTs.Padding = new System.Windows.Forms.Padding(2);
            this.pictureBoxAqTs.Size = new System.Drawing.Size(145, 52);
            this.pictureBoxAqTs.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxAqTs.TabIndex = 27;
            this.pictureBoxAqTs.TabStop = false;
            // 
            // convertButton
            // 
            this.convertButton.Location = new System.Drawing.Point(13, 27);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new System.Drawing.Size(75, 23);
            this.convertButton.TabIndex = 28;
            this.convertButton.Text = "Convert ...";
            this.convertButton.UseVisualStyleBackColor = true;
            this.convertButton.Click += new System.EventHandler(this.OnConvertClicked);
            // 
            // outputTextBox
            // 
            this.outputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outputTextBox.Location = new System.Drawing.Point(4, 115);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.ReadOnly = true;
            this.outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputTextBox.Size = new System.Drawing.Size(437, 76);
            this.outputTextBox.TabIndex = 29;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(426, 13);
            this.label1.TabIndex = 30;
            this.label1.Text = "Convert a FlowTracker2 *.ft file to a FlowTracker1 *.dis for importing into AQUAR" +
    "IUS 3.X.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(279, 13);
            this.label2.TabIndex = 31;
            this.label2.Text = "Drag and drop a *.ft file here, or use the \"Convert\" button.";
            // 
            // clearButton
            // 
            this.clearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearButton.Location = new System.Drawing.Point(4, 196);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(75, 23);
            this.clearButton.TabIndex = 32;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            // 
            // viewLicenseButton
            // 
            this.viewLicenseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.viewLicenseButton.Location = new System.Drawing.Point(358, 196);
            this.viewLicenseButton.Name = "viewLicenseButton";
            this.viewLicenseButton.Size = new System.Drawing.Size(80, 23);
            this.viewLicenseButton.TabIndex = 33;
            this.viewLicenseButton.Text = "View License";
            this.viewLicenseButton.UseVisualStyleBackColor = true;
            this.viewLicenseButton.Click += new System.EventHandler(this.viewLicenseButton_Click);
            // 
            // licenseCheckBox
            // 
            this.licenseCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.licenseCheckBox.AutoSize = true;
            this.licenseCheckBox.Location = new System.Drawing.Point(205, 201);
            this.licenseCheckBox.Name = "licenseCheckBox";
            this.licenseCheckBox.Size = new System.Drawing.Size(147, 17);
            this.licenseCheckBox.TabIndex = 34;
            this.licenseCheckBox.Text = "I accept the license terms";
            this.licenseCheckBox.UseVisualStyleBackColor = true;
            this.licenseCheckBox.CheckedChanged += new System.EventHandler(this.licenseCheckBox_CheckedChanged);
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 220);
            this.Controls.Add(this.licenseCheckBox);
            this.Controls.Add(this.viewLicenseButton);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.convertButton);
            this.Controls.Add(this.pictureBoxAqTs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(460, 254);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.OnDragOver);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAqTs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxAqTs;
        private System.Windows.Forms.Button convertButton;
        private System.Windows.Forms.TextBox outputTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button viewLicenseButton;
        private System.Windows.Forms.CheckBox licenseCheckBox;
    }
}

