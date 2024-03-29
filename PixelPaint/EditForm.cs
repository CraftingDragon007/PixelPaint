﻿using PixelPaint.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static PixelPaint.MainForm;

namespace PixelPaint
{
    public partial class EditForm : Form
    {
        private int px = 0;
        private Color color = Color.White;
        private Color customColor = Color.White;
        private String name = null;
        private String fileName = null;
        private Action lastAction = Action.nothing;
        private Size minimumSize = Size.Empty;
        private int s;
        private bool mouseDown = false;

        private readonly List<Thread> threads = new List<Thread>();

        public EditForm()
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
            this.Text = "Menu";
            s = Settings.Default.PixelSize;
            FileToolStripMenuItem.Text = GetLang("File_Menu");
            OpenToolStripMenuItem.Text = GetLang("Open_Menu_Item");
            SaveToolStripMenuItem.Text = GetLang("Save_Menu_Item");
            SaveAsToolStripMenuItem.Text = GetLang("Save_As_Menu_Item");
            ResetToolStripMenuItem.Text = GetLang("Reset_Menu_Item");
            PixelSizeToolStripMenuItem.Text = GetLang("Pixel_Size_Menu_Item");
            ExportToolStripMenuItem.Text = GetLang("Export_Menu_Item");
            EditToolStripMenuItem.Text = GetLang("Edit_Menu");
            UndoToolStripMenuItem.Text = GetLang("Undo_Menu_Item");
            RedoToolStripMenuItem.Text = GetLang("Redo_Menu_Item");
            Black.Text = GetLang("Black_Label");
            White.Text = GetLang("White_Label");
            Green.Text = GetLang("Green_Label");
            Yellow.Text = GetLang("Yellow_Label");
            Blue.Text = GetLang("Blue_Label");
            Red.Text = GetLang("Red_Label");
            Other.Text = GetLang("Others_Label");
            PixelLabel.Text = GetLang("Pixels_Label");
        }

        public void NewProjekt()
        {
            Thread thread = new Thread(() =>
            {
                this.Invoke(new System.Action(() => { this.Text = MainForm.GetLang("Unnamed_Title"); }));
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
                        box.Text = box.Name;
                        box.Click += new EventHandler(Paint);
                        box.MouseEnter += new EventHandler(PaintDown);
                        box.MouseDown += new MouseEventHandler(Down);
                        box.MouseUp += new MouseEventHandler(Up);
                        box.MouseMove += new MouseEventHandler(PaintMove);
                        box.Size = new Size(s, s);
                        box.BackColor = Color.White;
                        box.BorderStyle = BorderStyle.FixedSingle;
                        box.Location = new Point(x, y);
                        ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));

                        x += s;
                        px += 1;
                        PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                    }
                    x = 0;
                    y += s;
                }
            });
            this.threads.Add(thread);
            MainForm.threads.Add(thread);
            thread.Start();
        }

        private void SaveOld(String fileName)
        {
            StreamWriter writer = new StreamWriter(File.OpenWrite(fileName));
            writer.WriteLine("Size=" + Settings.Default.PixelSize);
            foreach (Control control in ImagePanel.Controls)
            {
                PictureBox box = (PictureBox)control;
                writer.WriteLine(box.BackColor.R + "|" + box.BackColor.G + "|" + box.BackColor.B);
            }
            writer.Flush();
            writer.Close();
        }

        private void NewSave(string fileName)
        {
            var stream = File.OpenWrite(fileName);
            stream.Position = 0;
            var header = Encoding.ASCII.GetBytes("PixelPaint");
            stream.Write(header, 0, header.Length);
            var size = BitConverter.GetBytes(Settings.Default.PixelSize);
            stream.WriteByte((byte)size.Length);
            stream.Write(size, 0, size.Length);
            stream.WriteByte(77);
            var fileSize = BitConverter.GetBytes(ImagePanel.Controls.Count * 3);
            var fSizeLength = (byte)fileSize.Length;
            stream.WriteByte(fSizeLength);
            stream.WriteByte(77);
            stream.Write(fileSize, 0, fileSize.Length);
            stream.WriteByte(77);
            stream.Flush();
            
            foreach (var control in ImagePanel.Controls)
            {
                PictureBox box = (PictureBox)control;
                var color = box.BackColor;
                stream.WriteByte(color.R);
                stream.WriteByte(color.G);
                stream.WriteByte(color.B);
            }
            stream.Flush();
            stream.Close();
        }

        public void NewOpen(string fileName)
        {
            if (!File.Exists(fileName))
            {
                MessageBox.Show(GetLang("Path_Not_Exists"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    var name = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    Invoke(new System.Action(() => { Text = name; }));
                    var stream = File.OpenRead(fileName);
                    var header = Encoding.ASCII.GetBytes("PixelPaint");
                    var headerBuffer = new byte[header.Length];
                    stream.Read(headerBuffer, 0, headerBuffer.Length);
                    if (!headerBuffer.SequenceEqual(header))
                    {
                        stream.Close();
                        throw new FormatException();
                    }
                    var pixelSizeLength = stream.ReadByte();
                    var pixelSize = new byte[pixelSizeLength];
                    stream.Read(pixelSize, 0, pixelSize.Length);
                    Settings.Default.PixelSize = BitConverter.ToInt32(pixelSize, 0);
                    Settings.Default.Save();
                    s = Settings.Default.PixelSize;
                    stream.ReadByte();
                    var fileSizeLength = stream.ReadByte();
                    stream.ReadByte();
                    var fileSize = new byte[fileSizeLength];
                    stream.Read(fileSize, 0, fileSize.Length);
                    var fileSizeAsInt = BitConverter.ToInt32(fileSize, 0);
                    stream.ReadByte();
                    var pixelCount = fileSizeAsInt / 3;
                    if(pixelCount  <= 0)
                    {
                        throw new FormatException();
                    }
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    int x = 0; int y = 0;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        var r = stream.ReadByte();
                        var g = stream.ReadByte();
                        var b = stream.ReadByte();
                        var color = Color.FromArgb(r, g, b);
                        if (x + s >= ImagePanel.Width)
                        {
                            x = 0;
                            y += s;
                        }
                        var box = new PictureBox
                        {
                            Name = "Box" + i
                        };
                        box.Click += new EventHandler(Paint);
                        box.MouseEnter += new EventHandler(PaintDown);
                        box.MouseDown += new MouseEventHandler(Down);
                        box.MouseUp += new MouseEventHandler(Up);
                        box.MouseMove += new MouseEventHandler(PaintMove);
                        box.Size = new Size(s, s);
                        box.BackColor = color;
                        box.BorderStyle = BorderStyle.FixedSingle;
                        box.Location = new Point(x, y);
                        ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));
                        x += s;
                    }
                    stream.Close();
                    px = pixelCount;
                    PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    MessageBox.Show(GetLang("Error_Invalid_File"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            threads.Add(thread);
            MainForm.threads.Add(thread);
            thread.Start();
        }

        public void Open(String fileName)
        {
            Thread thread = new Thread(() =>
            {

                if (File.Exists(fileName))
                {
                    Invoke(new System.Action(() => { Text = fileName.Substring(fileName.LastIndexOf("\\") + 1); }));
                    String content = File.ReadAllText(fileName);
                    string[] result = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    Settings.Default.PixelSize = int.Parse(result[0].Split('=')[1]);
                    Settings.Default.Save();
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
                            box.MouseEnter += new EventHandler(PaintDown);
                            box.MouseDown += new MouseEventHandler(Down);
                            box.MouseUp += new MouseEventHandler(Up);
                            box.MouseMove += new MouseEventHandler(PaintMove);
                            box.Size = new Size(s, s);
                            box.BackColor = Color.White;
                            box.BorderStyle = BorderStyle.FixedSingle;
                            box.Location = new Point(x, y);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }

                    this.fileName = fileName;
                    name = fileName.Substring(fileName.LastIndexOf("\\") + 1);
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
                else MessageBox.Show(GetLang("Path_Not_Exists"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
            this.threads.Add(thread);
            MainForm.threads.Add(thread);
            thread.Start();
        }

        private new void Paint(object sender, EventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            lastAction = new Action(box, box.BackColor, color);
            box.BackColor = color;
        }

        private void PaintDown(object sender, EventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            if (!mouseDown) return;
            lastAction = new Action(box, box.BackColor, color);
            box.BackColor = color;
        }

        private void Undo()
        {
            lastAction.Undo();
        }

        private void Redo()
        {
            lastAction.Redo();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            NewProjekt();
        }

        private void Green_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Lime;
        }

        private void Yellow_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Yellow;
        }

        private void Blue_CheckedChanged(object sender, EventArgs e)
        {
            this.color = Color.Blue;
        }

        private void Red_CheckedChanged(object sender, EventArgs e)
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
            String value = Settings.Default.PixelSize.ToString();
            if (InputBox("PixelPaint | " + GetLang("Pixel_Size_Menu_Item"), GetLang("Image_Reset_Warning"), ref value).Equals(DialogResult.OK))
            {
                if (int.Parse(value) < 5)
                {
                    MessageBox.Show(GetLang("Min_Pixel_Size_Error"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                Settings.Default.PixelSize = int.Parse(value);
                Settings.Default.Save();

                this.s = int.Parse(value);

                Thread thread = new Thread(() =>
                {
                    this.Invoke(new System.Action(() => { this.Text = MainForm.GetLang("Unnamed_Title"); }));
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
                            box.MouseEnter += new EventHandler(PaintDown);
                            box.MouseDown += new MouseEventHandler(Down);
                            box.MouseUp += new MouseEventHandler(Up);
                            box.MouseMove += new MouseEventHandler(PaintMove);
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
                MainForm.threads.Add(thread);
                thread.Start();

                this.fileName = "";
            }
        }

        private void ResetPixels(object sender, EventArgs e)
        {
            if (MessageBox.Show(GetLang("Image_Reset_Warning"), "PixelPaint | " + GetLang("Reset_Confirmation"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning).Equals(DialogResult.OK))
            {
                Thread thread = new Thread(() =>
                {
                    this.Invoke(new System.Action(() => { this.Text = MainForm.GetLang("Unnamed_Title"); }));
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
                            box.MouseEnter += new EventHandler(PaintDown);
                            box.MouseDown += new MouseEventHandler(Down);
                            box.MouseUp += new MouseEventHandler(Up);
                            box.MouseMove += new MouseEventHandler(PaintMove);
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
                MainForm.threads.Add(thread);
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
            buttonCancel.Text = "Cancel";
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

        private void NewSaveEvent(object sender, EventArgs e)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                    var extension = Path.GetExtension(fileName);
                    if (extension == ".bxp")
                    {
                        NewSave(fileName);
                    }
                    else SaveOld(fileName);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                SaveAs(sender, e);
            }
        }

        private void SaveAs(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";
            saveFileDialog.Title = "PixelPaint | " + MainForm.GetLang("Picture") + " " + MainForm.GetLang("Save_Menu_Item");
            saveFileDialog.FileName = "";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                fileName = saveFileDialog.FileName;
                name = saveFileDialog.FileName.Substring(saveFileDialog.FileName.LastIndexOf("\\") + 1).Replace(saveFileDialog.FileName.Split('.').Last(), "");
                Text = fileName;
                if (saveFileDialog.FilterIndex == 1)
                    NewSave(fileName);
                else
                    SaveOld(fileName);
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                if(openFileDialog.FilterIndex == 1)
                    NewOpen(openFileDialog.FileName);
                else
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

            public Color Undo()
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

            public Color Redo()
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



        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void Export(String filename)
        {
            //TODO: Make this working!

            //Point position = this.Location;
            Point point = ImagePanel.Location;
            //this.FormBorderStyle = FormBorderStyle.None;
            //ImagePanel.Location = new Point(0, 0);
            //this.Location = new Point(0, 0);
            Rectangle rectangle = ImagePanel.Bounds;
            using (Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(PointToScreen(point), Point.Empty, rectangle.Size);
                }
                //this.Location = position;
                //ImagePanel.Location = point;
                //this.FormBorderStyle = FormBorderStyle.Sizable;
                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void ExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG " + MainForm.GetLang("Picture") + "(*.png)|*.png";
            saveFileDialog.Title = "PixelPaint | " + MainForm.GetLang("Picture") + " " + MainForm.GetLang("Save_Menu_Item");
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

        private void Up(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void Down(object sender, MouseEventArgs e)
        {
            mouseDown = true;
        }

        private void PaintMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                foreach(Control control in ImagePanel.Controls)
                {
                    if (control != null)
                    {
                        if(control is PictureBox box)
                        {
                            Point pt = box.PointToClient(Cursor.Position);
                            Rectangle rc = box.ClientRectangle;
                            if (rc.Contains(pt))
                            {
                                PaintDown(box, e);
                            }
                        }
                    }
                }
            }
        }

        private void EditForm_Activated(object sender, EventArgs e)
        {
            BringToFront();
        }
    }
}
