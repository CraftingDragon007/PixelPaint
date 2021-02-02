using System.Windows.Forms;

namespace PixelPaint
{
    partial class Main
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
        internal Label PixelLabel;
        internal RadioButton Green;
        internal RadioButton Gelb;
        internal RadioButton Rot;
        internal RadioButton Blau;
        private Panel ImagePanel;
        private Label PixelSizeLabel;
        private RadioButton White;
        private RadioButton Black;
        private RadioButton Other;
        private Button BrowseColorsButton;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem FileToolStripMenuItem;
        private ToolStripMenuItem OpenToolStripMenuItem;
        private ToolStripMenuItem SaveToolStripMenuItem;
        private ToolStripMenuItem SaveUnderToolStripMenuItem;
        private ToolStripMenuItem ResetToolStripMenuItem;
        private ToolStripMenuItem PixelSizeToolStripMenuItem;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.PixelLabel = new System.Windows.Forms.Label();
            this.Green = new System.Windows.Forms.RadioButton();
            this.Gelb = new System.Windows.Forms.RadioButton();
            this.Rot = new System.Windows.Forms.RadioButton();
            this.Blau = new System.Windows.Forms.RadioButton();
            this.ImagePanel = new System.Windows.Forms.Panel();
            this.PixelSizeLabel = new System.Windows.Forms.Label();
            this.White = new System.Windows.Forms.RadioButton();
            this.Black = new System.Windows.Forms.RadioButton();
            this.Other = new System.Windows.Forms.RadioButton();
            this.BrowseColorsButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveUnderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ResetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PixelSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.EditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UndoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RedoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // PixelLabel
            // 
            this.PixelLabel.AutoSize = true;
            this.PixelLabel.Location = new System.Drawing.Point(11, 210);
            this.PixelLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.PixelLabel.Name = "PixelLabel";
            this.PixelLabel.Size = new System.Drawing.Size(32, 13);
            this.PixelLabel.TabIndex = 40;
            this.PixelLabel.Text = "Pixel:";
            // 
            // Green
            // 
            this.Green.AutoSize = true;
            this.Green.BackColor = System.Drawing.Color.Lime;
            this.Green.Location = new System.Drawing.Point(12, 76);
            this.Green.Margin = new System.Windows.Forms.Padding(2);
            this.Green.Name = "Green";
            this.Green.Size = new System.Drawing.Size(48, 17);
            this.Green.TabIndex = 36;
            this.Green.TabStop = true;
            this.Green.Text = "Grün";
            this.Green.UseVisualStyleBackColor = false;
            this.Green.CheckedChanged += new System.EventHandler(this.Green_CheckedChanged);
            // 
            // Gelb
            // 
            this.Gelb.AutoSize = true;
            this.Gelb.BackColor = System.Drawing.Color.Yellow;
            this.Gelb.Location = new System.Drawing.Point(12, 97);
            this.Gelb.Margin = new System.Windows.Forms.Padding(2);
            this.Gelb.Name = "Gelb";
            this.Gelb.Size = new System.Drawing.Size(47, 17);
            this.Gelb.TabIndex = 37;
            this.Gelb.TabStop = true;
            this.Gelb.Text = "Gelb";
            this.Gelb.UseVisualStyleBackColor = false;
            this.Gelb.CheckedChanged += new System.EventHandler(this.Gelb_CheckedChanged);
            // 
            // Rot
            // 
            this.Rot.AutoSize = true;
            this.Rot.BackColor = System.Drawing.Color.Red;
            this.Rot.Location = new System.Drawing.Point(12, 141);
            this.Rot.Margin = new System.Windows.Forms.Padding(2);
            this.Rot.Name = "Rot";
            this.Rot.Size = new System.Drawing.Size(42, 17);
            this.Rot.TabIndex = 39;
            this.Rot.TabStop = true;
            this.Rot.Text = "Rot";
            this.Rot.UseVisualStyleBackColor = false;
            this.Rot.CheckedChanged += new System.EventHandler(this.Rot_CheckedChanged);
            // 
            // Blau
            // 
            this.Blau.AutoSize = true;
            this.Blau.BackColor = System.Drawing.Color.Blue;
            this.Blau.Location = new System.Drawing.Point(12, 119);
            this.Blau.Margin = new System.Windows.Forms.Padding(2);
            this.Blau.Name = "Blau";
            this.Blau.Size = new System.Drawing.Size(46, 17);
            this.Blau.TabIndex = 38;
            this.Blau.TabStop = true;
            this.Blau.Text = "Blau";
            this.Blau.UseVisualStyleBackColor = false;
            this.Blau.CheckedChanged += new System.EventHandler(this.Blau_CheckedChanged);
            // 
            // ImagePanel
            // 
            this.ImagePanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ImagePanel.Location = new System.Drawing.Point(99, 31);
            this.ImagePanel.Name = "ImagePanel";
            this.ImagePanel.Size = new System.Drawing.Size(653, 319);
            this.ImagePanel.TabIndex = 43;
            // 
            // PixelSizeLabel
            // 
            this.PixelSizeLabel.AutoSize = true;
            this.PixelSizeLabel.Location = new System.Drawing.Point(45, 210);
            this.PixelSizeLabel.Name = "PixelSizeLabel";
            this.PixelSizeLabel.Size = new System.Drawing.Size(13, 13);
            this.PixelSizeLabel.TabIndex = 44;
            this.PixelSizeLabel.Text = "0";
            // 
            // White
            // 
            this.White.AutoSize = true;
            this.White.BackColor = System.Drawing.Color.White;
            this.White.Location = new System.Drawing.Point(12, 54);
            this.White.Name = "White";
            this.White.Size = new System.Drawing.Size(54, 17);
            this.White.TabIndex = 45;
            this.White.TabStop = true;
            this.White.Text = "Weiss";
            this.White.UseVisualStyleBackColor = false;
            this.White.CheckedChanged += new System.EventHandler(this.White_CheckedChanged);
            // 
            // Black
            // 
            this.Black.AutoSize = true;
            this.Black.BackColor = System.Drawing.Color.Black;
            this.Black.ForeColor = System.Drawing.Color.White;
            this.Black.Location = new System.Drawing.Point(12, 31);
            this.Black.Name = "Black";
            this.Black.Size = new System.Drawing.Size(66, 17);
            this.Black.TabIndex = 46;
            this.Black.TabStop = true;
            this.Black.Text = "Schwarz";
            this.Black.UseVisualStyleBackColor = false;
            this.Black.CheckedChanged += new System.EventHandler(this.Black_CheckedChanged);
            // 
            // Other
            // 
            this.Other.AutoSize = true;
            this.Other.Location = new System.Drawing.Point(12, 164);
            this.Other.Name = "Other";
            this.Other.Size = new System.Drawing.Size(58, 17);
            this.Other.TabIndex = 47;
            this.Other.TabStop = true;
            this.Other.Text = "andere";
            this.Other.UseVisualStyleBackColor = true;
            this.Other.CheckedChanged += new System.EventHandler(this.Other_CheckedChanged);
            // 
            // BrowseColorsButton
            // 
            this.BrowseColorsButton.Location = new System.Drawing.Point(67, 161);
            this.BrowseColorsButton.Name = "BrowseColorsButton";
            this.BrowseColorsButton.Size = new System.Drawing.Size(24, 23);
            this.BrowseColorsButton.TabIndex = 48;
            this.BrowseColorsButton.Text = "...";
            this.BrowseColorsButton.UseVisualStyleBackColor = true;
            this.BrowseColorsButton.Click += new System.EventHandler(this.BrowseColorsButton_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileToolStripMenuItem,
            this.EditToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 52;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenToolStripMenuItem,
            this.SaveToolStripMenuItem,
            this.SaveUnderToolStripMenuItem,
            this.ResetToolStripMenuItem,
            this.PixelSizeToolStripMenuItem,
            this.ExportToolStripMenuItem});
            this.FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            this.FileToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.FileToolStripMenuItem.Text = "Datei";
            // 
            // OpenToolStripMenuItem
            // 
            this.OpenToolStripMenuItem.Name = "OpenToolStripMenuItem";
            this.OpenToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.OpenToolStripMenuItem.Text = "Öffnen";
            this.OpenToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // SaveToolStripMenuItem
            // 
            this.SaveToolStripMenuItem.Name = "SaveToolStripMenuItem";
            this.SaveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.SaveToolStripMenuItem.Text = "Speichern";
            this.SaveToolStripMenuItem.Click += new System.EventHandler(this.SaveEvent);
            // 
            // SaveUnderToolStripMenuItem
            // 
            this.SaveUnderToolStripMenuItem.Name = "SaveUnderToolStripMenuItem";
            this.SaveUnderToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.SaveUnderToolStripMenuItem.Text = "Speichern unter...";
            this.SaveUnderToolStripMenuItem.Click += new System.EventHandler(this.SaveUnder);
            // 
            // ResetToolStripMenuItem
            // 
            this.ResetToolStripMenuItem.Name = "ResetToolStripMenuItem";
            this.ResetToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ResetToolStripMenuItem.Text = "Reset";
            this.ResetToolStripMenuItem.Click += new System.EventHandler(this.ResetPixels);
            // 
            // PixelSizeToolStripMenuItem
            // 
            this.PixelSizeToolStripMenuItem.Name = "PixelSizeToolStripMenuItem";
            this.PixelSizeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.PixelSizeToolStripMenuItem.Text = "Pixel Grösse..";
            this.PixelSizeToolStripMenuItem.Click += new System.EventHandler(this.ChangePixelSize);
            // 
            // ExportToolStripMenuItem
            // 
            this.ExportToolStripMenuItem.Name = "ExportToolStripMenuItem";
            this.ExportToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.ExportToolStripMenuItem.Text = "Exportieren...";
            this.ExportToolStripMenuItem.Click += new System.EventHandler(this.exportierenToolStripMenuItem_Click);
            // 
            // EditToolStripMenuItem
            // 
            this.EditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UndoToolStripMenuItem,
            this.RedoToolStripMenuItem});
            this.EditToolStripMenuItem.Name = "EditToolStripMenuItem";
            this.EditToolStripMenuItem.Size = new System.Drawing.Size(75, 20);
            this.EditToolStripMenuItem.Text = "Bearbeiten";
            // 
            // UndoToolStripMenuItem
            // 
            this.UndoToolStripMenuItem.Name = "UndoToolStripMenuItem";
            this.UndoToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.UndoToolStripMenuItem.Text = "Rückgängig";
            this.UndoToolStripMenuItem.Click += new System.EventHandler(this.rückgängigToolStripMenuItem_Click);
            // 
            // RedoToolStripMenuItem
            // 
            this.RedoToolStripMenuItem.Name = "RedoToolStripMenuItem";
            this.RedoToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.RedoToolStripMenuItem.Text = "Wiederholen";
            this.RedoToolStripMenuItem.Click += new System.EventHandler(this.wiederholenToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 499);
            this.Controls.Add(this.BrowseColorsButton);
            this.Controls.Add(this.Other);
            this.Controls.Add(this.Black);
            this.Controls.Add(this.White);
            this.Controls.Add(this.PixelSizeLabel);
            this.Controls.Add(this.ImagePanel);
            this.Controls.Add(this.Green);
            this.Controls.Add(this.PixelLabel);
            this.Controls.Add(this.Gelb);
            this.Controls.Add(this.Rot);
            this.Controls.Add(this.Blau);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Main";
            this.ShowIcon = false;
            this.Text = "Starting...";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResizeBegin += new System.EventHandler(this.Main_ResizeBegin);
            this.ResizeEnd += new System.EventHandler(this.Main_ResizeEnd);
            this.Resize += new System.EventHandler(this.Main_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ToolStripMenuItem EditToolStripMenuItem;
        private ToolStripMenuItem UndoToolStripMenuItem;
        private ToolStripMenuItem RedoToolStripMenuItem;        
    }
}