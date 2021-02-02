﻿using PixelPaint.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class Main : Form
    {
        private int px = 0;
        private Color color = Color.White;
        private Color customColor = Color.White;
        private String name = null;
        private String fileName = null;
        private Action lastAction = Action.nothing;
        private Size minimumSize = Size.Empty;
        private int s;

        private readonly List<Thread> threads = new List<Thread>();

        public Main()
        {
            InitializeComponent();
        }

        public void ChangeTitle(String text)
        {
            this.Invoke(new System.Action(() => { this.Text = text; }));
            return;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.minimumSize = this.Size;
            this.Text = "Menü";
            s = Settings.Default.PixelSize;
        }

        public void NewProjekt()
        {
            Thread thread = new Thread(() =>
            {
                this.Invoke(new System.Action(() => { this.Text = "Unbenannt*"; }));
                ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                px = 0;
                int x = 0;
                int y = 0;
                int s = this.s;
                while (y + s <= ImagePanel.Height)
                {
                    while (x + s <= ImagePanel.Width)
                    {
                        PictureBox box = new PictureBox();
                        box.Name = "Box" + px;
                        box.Click += new EventHandler(Paint);
                        box.Size = new Size(s, s);
                        box.BackColor = Color.White;
                        box.BorderStyle = BorderStyle.FixedSingle;
                        box.Location = new Point(x, y);
                        ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add((Control)box); }));

                        x += s;
                        px += 1;
                        PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                    }
                    x = 0;
                    y += s;
                }
            });
            this.threads.Add(thread);
            Form1.threads.Add(thread);
            thread.Start();
        }

        private void Save(String fileName)
        {
            StreamWriter writer = new StreamWriter(File.OpenWrite(fileName));
            writer.WriteLine("Size=" + Properties.Settings.Default.PixelSize);
            foreach (Control control in ImagePanel.Controls)
            {
                PictureBox box = (PictureBox)control;
                writer.WriteLine(box.BackColor.R + "|" + box.BackColor.G + "|" + box.BackColor.B);
            }
            writer.Flush();
            writer.Close();
        }

        public void Open(String fileName)
        {
            Thread thread = new Thread(() =>
            {

                if (File.Exists(fileName))
                {
                    this.Invoke(new System.Action(() => { this.Text = fileName.Substring(fileName.LastIndexOf("\\") + 1).Replace(".pxp", ""); }));
                    StreamReader reader = new StreamReader(File.OpenRead(fileName));
                    String content = reader.ReadToEnd();
                    string[] result = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    Properties.Settings.Default.PixelSize = int.Parse(result[0].Split('=')[1]);
                    Properties.Settings.Default.Save();
                    this.Invoke(new System.Action(() => { this.Text = "Unbenannt*"; }));
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    int x = 0;
                    int y = 0;
                    int s = int.Parse(result[0].Split('=')[1]);
                    while (y + s <= ImagePanel.Height)
                    {
                        while (x + s <= ImagePanel.Width)
                        {
                            PictureBox box = new PictureBox();
                            box.Name = "Box" + px;
                            box.Click += new EventHandler(Paint);
                            box.Size = new Size(s, s);
                            box.BackColor = Color.White;
                            box.BorderStyle = BorderStyle.FixedSingle;
                            box.Location = new Point(x, y);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add((Control)box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }

                    this.fileName = fileName;
                    this.name = fileName.Substring(fileName.LastIndexOf("\\") + 1).Replace(".pxp", "");
                    result = result.Where(val => val != result[0]).ToArray();
                    int i = 0;
                    while (i < result.Length)
                    {
                        PictureBox box = (PictureBox)ImagePanel.Controls[i];
                        string[] vs = result[i].Split('|');
                        Color color = Color.FromArgb(int.Parse(vs[0]), int.Parse(vs[1]), int.Parse(vs[2]));
                        this.Invoke(new System.Action(() => { box.BackColor = color; }));
                        i++;
                    }
                }
                else MessageBox.Show("Der Pfad Existiert nicht", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
            this.threads.Add(thread);
            Form1.threads.Add(thread);
            thread.Start();
        }

        private new void Paint(object sender, EventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            lastAction = new Action(box, box.BackColor, color);
            box.BackColor = color;
        }

        private void Undo()
        {
            lastAction.undo();
        }

        private void Redo()
        {
            lastAction.redo();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            NewProjekt();
        }

        private void Green_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Lime;
        }

        private void Gelb_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Yellow;
        }

        private void Blau_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Blue;
        }

        private void Rot_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Red;
        }

        private void White_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.White;
        }

        private void Black_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Black;
        }

        private void Other_CheckedChanged(object sender, EventArgs e)
        {
            color = customColor;
        }

        private void BrowseColorsButton_Click(object sender, EventArgs e)
        {
            ColorDialog dialog = new ColorDialog();
            dialog.Color = Color.Black;
            if (dialog.ShowDialog().Equals(DialogResult.OK))
            {
                customColor = dialog.Color;
                Other.BackColor = dialog.Color;
                Color d = dialog.Color;
                Color c = Color.FromArgb(255 - d.R, 255 -
                  d.G, 255 - d.B);
                Other.ForeColor = c;
                if (Other.Checked)
                {
                    color = dialog.Color;
                }
            }
        }

        private void ChangePixelSize(object sender, EventArgs e)
        {
            String value = Properties.Settings.Default.PixelSize.ToString();
            if (InputBox("PixelPaint | Grösse der Pixel", "Warnung! Wenn du fortfährst wird das Bild gelöscht!", ref value).Equals(DialogResult.OK))
            {
                if (int.Parse(value) < 5)
                {
                    MessageBox.Show("Die minimale Pixel Grösse ist 5!", "Warnung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Properties.Settings.Default.PixelSize = int.Parse(value);
                Properties.Settings.Default.Save();

                this.s = int.Parse(value);

                Thread thread = new Thread(() =>
                {
                    this.Invoke(new System.Action(() => { this.Text = "Unbenannt*"; }));
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    int x = 0;
                    int y = 0;
                    int s = this.s;
                    while (y + s <= ImagePanel.Height)
                    {
                        while (x + s <= ImagePanel.Width)
                        {
                            PictureBox box = new PictureBox();
                            box.Name = "Box" + px;
                            box.Click += new EventHandler(Paint);
                            box.Size = new Size(s, s);
                            box.BackColor = Color.White;
                            box.BorderStyle = BorderStyle.FixedSingle;
                            box.Location = new Point(x, y);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add((Control)box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }
                });
                this.threads.Add(thread);
                Form1.threads.Add(thread);
                thread.Start();

                this.fileName = "";
            }
        }

        private void ResetPixels(object sender, EventArgs e)
        {
            if (MessageBox.Show("Wenn Sie fortfahren wird das Bild gelöscht!", "PixelPaint | Reset Bestätigung", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning).Equals(DialogResult.OK))
            {
                Thread thread = new Thread(() =>
                {
                    this.Invoke(new System.Action(() => { this.Text = "Unbenannt*"; }));
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    int x = 0;
                    int y = 0;
                    int s = this.s;
                    while (y + s <= ImagePanel.Height)
                    {
                        while (x + s <= ImagePanel.Width)
                        {
                            PictureBox box = new PictureBox();
                            box.Name = "Box" + px;
                            box.Click += new EventHandler(Paint);
                            box.Size = new Size(s, s);
                            box.BackColor = Color.White;
                            box.BorderStyle = BorderStyle.FixedSingle;
                            box.Location = new Point(x, y);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add((Control)box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }
                });
                this.threads.Add(thread);
                Form1.threads.Add(thread);
                thread.Start();
                this.fileName = "";
            }
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Abbrechen";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void SaveEvent(object sender, EventArgs e)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                    Save(fileName);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PixelPaintDateien(*.pxp)|*.pxp";
                saveFileDialog.Title = "PixelPaint | Bild speichern";
                saveFileDialog.FileName = "";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
                {
                    fileName = saveFileDialog.FileName;
                    name = saveFileDialog.FileName.Substring(saveFileDialog.FileName.LastIndexOf("\\") + 1).Replace(".pxp", "");
                    Save(saveFileDialog.FileName);
                }
            }
        }

        private void SaveUnder(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PixelPaintDateien(*.pxp)|*.pxp";
            saveFileDialog.Title = "PixelPaint | Bild speichern";
            saveFileDialog.FileName = "";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                fileName = saveFileDialog.FileName;
                name = saveFileDialog.FileName.Substring(saveFileDialog.FileName.LastIndexOf("\\") + 1).Replace(".pxp", "");
                Save(saveFileDialog.FileName);
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PixelPaintDateien(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                Open(openFileDialog.FileName);
            }
        }

        class Action
        {
            private readonly PictureBox box;
            private readonly Color bevore;
            private readonly Color after;
            private readonly bool isNothing;
            public static Action nothing = new Action();
            public Action(PictureBox box, Color bevore, Color after)
            {
                this.box = box;
                this.bevore = bevore;
                this.after = after;
                this.isNothing = false;
            }

            private Action()
            {
                this.isNothing = true;
                this.box = null;
                this.bevore = Color.White;
                this.after = Color.White;
            }

            public Color undo()
            {
                if (!isNothing)
                {
                    return box.BackColor = bevore;
                }
                else
                {
                    return box.BackColor;
                }
            }

            public Color redo()
            {
                if (!isNothing)
                {
                    return box.BackColor = after;
                }
                else
                {
                    return box.BackColor;
                }
            }
        }



        private void rückgängigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void wiederholenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void Export(String filename)
        {
            Point position = this.Location;
            Point point = ImagePanel.Location;
            this.FormBorderStyle = FormBorderStyle.None;
            ImagePanel.Location = new Point(0, 0);
            this.Location = new Point(0, 0);
            //Rectangle bounds = this.Bounds;
            Rectangle rectangle = ImagePanel.Bounds;
            using (Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point(rectangle.Left, rectangle.Top), Point.Empty, rectangle.Size);
                }
                this.Location = position;
                ImagePanel.Location = point;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private ToolStripMenuItem ExportToolStripMenuItem;

        private void exportierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Bild(*.png)|*.png";
            saveFileDialog.Title = "PixelPaint | Bild speichern";
            saveFileDialog.FileName = "";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                Export(saveFileDialog.FileName);
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.Size.Width * this.Size.Height < this.minimumSize.Width * this.minimumSize.Height)
            {
                this.Size = this.minimumSize;
            }
        }

        private void Main_ResizeBegin(object sender, EventArgs e)
        {
            if (this.Size.Width * this.Size.Height < this.minimumSize.Width * this.minimumSize.Height)
            {
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
            }
        }

        private void Main_ResizeEnd(object sender, EventArgs e)
        {
            if (this.Size.Width * this.Size.Height < this.minimumSize.Width * this.minimumSize.Height)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }
        }
    }
}
