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
using Svg;

namespace PixelPaint
{
    public partial class EditForm : Form
    {
        private int px = 0;
        private Color color = Color.White;
        private Color customColor = Color.White;
        private string projectName = null;
        private string fileName = null;
        private Stack<Action> undoStack = new Stack<Action>();
        private Stack<Action> redoStack = new Stack<Action>();
        private Size minimumSize = Size.Empty;
        private int PixelSize;
        private bool mouseDown = false;

        private bool _savedInternal;

        // trigger UpdateCurrentFileDisplayName every time Saved changes
        private bool Saved { get => _savedInternal; set { _savedInternal = value; UpdateCurrentFileDisplayName(); } }

        private readonly List<Thread> threads = new List<Thread>();

        public EditForm()
        {
            InitializeComponent();
        }

        public void UpdateCurrentFileDisplayName(string name = null)
        {
            if (name == null && projectName == null) return;
            if (name == null) name = projectName;
            else projectName = name;
            string title;
            if (Saved) title = name;
            else title = name + "*";
            Invoke(new System.Action(() => { Text = title; }));
        }

        public void UpdateSavedState(bool state, string name = null)
        {
            if (name != null) projectName = name;
            Saved = state;
        }

        private void EditForm_Load(object sender, EventArgs e)
        {
            minimumSize = Size;
            PixelSize = Settings.Default.PixelSize;
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

        private PictureBox CreatePictureBox(int x, int y, int index, Color color)
        {
            var box = new PictureBox
            {
                Name = "Box" + index,
                Size = new Size(PixelSize, PixelSize),
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(x, y)
            };
            box.Click += new EventHandler(Paint);
            box.MouseEnter += new EventHandler(PaintDown);
            box.MouseDown += new MouseEventHandler(Down);
            box.MouseUp += new MouseEventHandler(Up);
            box.MouseMove += new MouseEventHandler(EditForm_MouseMove);
            return box;
        }

        public void NewProject()
        {
            var thread = new Thread(() =>
            {
                UpdateSavedState(false, GetLang("Unnamed_Title"));
                ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                px = 0;
                var x = 0;
                var y = 0;
                var s = PixelSize;
                while (y + s <= ImagePanel.Height)
                {
                    while (x + s <= ImagePanel.Width)
                    {
                        var box = CreatePictureBox(x, y, px, Color.White);
                        ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));

                        x += s;
                        px += 1;
                        PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                    }
                    x = 0;
                    y += s;
                }
            });
            threads.Add(thread);
            Threads.Add(thread);
            thread.Start();
        }

        private void SaveOld(string fileName)
        {
            var writer = new StreamWriter(File.OpenWrite(fileName));
            writer.WriteLine("Size=" + Settings.Default.PixelSize);
            foreach (Control control in ImagePanel.Controls)
            {
                var box = (PictureBox)control;
                writer.WriteLine(box.BackColor.R + "|" + box.BackColor.G + "|" + box.BackColor.B);
            }
            writer.Flush();
            writer.Close();

            UpdateSavedState(true, fileName.Substring(fileName.LastIndexOf("\\") + 1));
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
                var box = (PictureBox)control;
                var color = box.BackColor;
                stream.WriteByte(color.R);
                stream.WriteByte(color.G);
                stream.WriteByte(color.B);
            }
            stream.Flush();
            stream.Close();

            UpdateSavedState(true, fileName.Substring(fileName.LastIndexOf("\\") + 1));
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
                    PixelSize = Settings.Default.PixelSize;
                    stream.ReadByte();
                    var fileSizeLength = stream.ReadByte();
                    stream.ReadByte();
                    var fileSize = new byte[fileSizeLength];
                    stream.Read(fileSize, 0, fileSize.Length);
                    var fileSizeAsInt = BitConverter.ToInt32(fileSize, 0);
                    stream.ReadByte();
                    var pixelCount = fileSizeAsInt / 3;
                    if (pixelCount <= 0)
                    {
                        throw new FormatException();
                    }
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    var x = 0; var y = 0;
                    for (var i = 0; i < pixelCount; i++)
                    {
                        var r = stream.ReadByte();
                        var g = stream.ReadByte();
                        var b = stream.ReadByte();
                        var color = Color.FromArgb(r, g, b);
                        if (x + PixelSize >= ImagePanel.Width)
                        {
                            x = 0;
                            y += PixelSize;
                        }
                        var box = CreatePictureBox(x, y, i, color);
                        ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));
                        x += PixelSize;
                    }
                    stream.Close();
                    px = pixelCount;
                    PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));

                    this.fileName = fileName;
                    UpdateSavedState(true, name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    MessageBox.Show(GetLang("Error_Invalid_File"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            threads.Add(thread);
            Threads.Add(thread);
            thread.Start();
        }

        public void Open(string fileName)
        {
            var thread = new Thread(() =>
            {

                if (File.Exists(fileName))
                {
                    var content = File.ReadAllText(fileName);
                    var result = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    Settings.Default.PixelSize = int.Parse(result[0].Split('=')[1]);
                    Settings.Default.Save();
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    var x = 0;
                    var y = 0;
                    var s = int.Parse(result[0].Split('=')[1]);
                    while (y + s <= ImagePanel.Height)
                    {
                        while (x + s <= ImagePanel.Width)
                        {
                            var box = CreatePictureBox(x, y, px, Color.White);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }

                    this.fileName = fileName;
                    UpdateSavedState(true, fileName.Substring(fileName.LastIndexOf("\\") + 1));

                    result = result.Where(val => val != result[0]).ToArray();
                    var i = 0;
                    while (i < result.Length)
                    {
                        var box = (PictureBox)ImagePanel.Controls[i];
                        string[] vs = result[i].Split('|');
                        Color color = Color.FromArgb(int.Parse(vs[0]), int.Parse(vs[1]), int.Parse(vs[2]));
                        Invoke(new System.Action(() => { box.BackColor = color; }));
                        i++;
                    }
                }
                else MessageBox.Show(GetLang("Path_Not_Exists"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
            threads.Add(thread);
            Threads.Add(thread);
            thread.Start();
        }

        private new void Paint(object sender, EventArgs e)
        {
            var box = (PictureBox)sender;
            var action = new Action(box, box.BackColor, color);
            if (undoStack.Count == 0 || !undoStack.Peek().Equals(action))
            {
                action.Execute();
                undoStack.Push(action);
                redoStack.Clear();
                UpdateSavedState(false);
            }
        }

        private void PaintDown(object sender, EventArgs e)
        {
            var box = (PictureBox)sender;
            if (!mouseDown) return;
            var action = new Action(box, box.BackColor, color);
            if (undoStack.Count == 0 || !undoStack.Peek().Equals(action))
            {
                action.Execute();
                undoStack.Push(action);
                redoStack.Clear();
                UpdateSavedState(false);
            }
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                var action = undoStack.Pop();
                action.Undo();
                redoStack.Push(action); 
                UpdateSavedState(false);
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                var action = redoStack.Pop();
                action.Redo();
                undoStack.Push(action);
                UpdateSavedState(false);
            }
        }

        private void Green_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.Lime;
        }

        private void Yellow_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.Yellow;
        }

        private void Blue_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.Blue;
        }

        private void Red_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.Red;
        }

        private void White_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.White;
        }

        private void Black_CheckedChanged(object sender, EventArgs e)
        {
            color = Color.Black;
        }

        private void Other_CheckedChanged(object sender, EventArgs e)
        {
            color = customColor;
        }

        private void BrowseColorsButton_Click(object sender, EventArgs e)
        {
            var dialog = new ColorDialog();
            dialog.Color = Color.Black;
            if (dialog.ShowDialog().Equals(DialogResult.OK))
            {
                customColor = dialog.Color;
                Other.BackColor = dialog.Color;
                var newCustomColor = dialog.Color;
                var invertedCustomColor = Color.FromArgb(255 - newCustomColor.R, 255 -
                  newCustomColor.G, 255 - newCustomColor.B);
                Other.ForeColor = invertedCustomColor;
                if (Other.Checked)
                {
                    color = dialog.Color;
                }
            }
        }

        private void ChangePixelSize(object sender, EventArgs e)
        {
            var value = Settings.Default.PixelSize.ToString();
            if (InputBox("PixelPaint | " + GetLang("Pixel_Size_Menu_Item"), GetLang("Image_Reset_Warning"), ref value).Equals(DialogResult.OK))
            {
                if (int.Parse(value) < 5)
                {
                    MessageBox.Show(GetLang("Min_Pixel_Size_Error"), GetLang("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                Settings.Default.PixelSize = int.Parse(value);
                Settings.Default.Save();

                PixelSize = int.Parse(value);

                var thread = new Thread(() =>
                {
                    Invoke(new System.Action(() => { Text = GetLang("Unnamed_Title"); }));
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    var x = 0;
                    var y = 0;
                    var s = PixelSize;
                    while (y + s <= ImagePanel.Height)
                    {
                        while (x + s <= ImagePanel.Width)
                        {
                            var box = CreatePictureBox(x, y, px, Color.White);
                            ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Add(box); }));

                            x += s;
                            px += 1;
                            PixelSizeLabel.Invoke(new System.Action(() => { PixelSizeLabel.Text = px.ToString(); }));
                        }
                        x = 0;
                        y += s;
                    }
                });
                threads.Add(thread);
                Threads.Add(thread);
                thread.Start();

                fileName = "";
            }
        }

        private void ResetPixels(object sender, EventArgs e)
        {
            if (MessageBox.Show(GetLang("Image_Reset_Warning"), "PixelPaint | " + GetLang("Reset_Confirmation"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning).Equals(DialogResult.OK))
            {
                var thread = new Thread(() =>
                 {
                     UpdateSavedState(false, GetLang("Unnamed_Title"));
                     ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                     px = 0;
                     var x = 0;
                     var y = 0;
                     var s = PixelSize;
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
                             box.MouseMove += new MouseEventHandler(EditForm_MouseMove);
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
                threads.Add(thread);
                Threads.Add(thread);
                thread.Start();
                fileName = "";                
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
            saveFileDialog.Title = "PixelPaint | " + GetLang("Picture") + " " + GetLang("Save_Menu_Item");
            saveFileDialog.FileName = "";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                if (saveFileDialog.FilterIndex == 1)
                    NewSave(saveFileDialog.FileName);
                else
                    SaveOld(saveFileDialog.FileName);
            }
        }

        private void OpenFileEvent(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                if (openFileDialog.FilterIndex == 1)
                    NewOpen(openFileDialog.FileName);
                else
                    Open(openFileDialog.FileName);
            }
        }

        class Action
        {
            private readonly PictureBox box;
            private readonly Color before;
            private readonly Color after;
            private readonly bool isNothing;
            public static Action nothing = new Action();

            public Action(PictureBox box, Color before, Color after)
            {
                this.box = box;
                this.before = before;
                this.after = after;
                isNothing = false;
            }

            private Action()
            {
                isNothing = true;
                box = null;
                before = Color.White;
                after = Color.White;
            }

            public void Execute()
            {
                if (!isNothing)
                {
                    box.BackColor = after;
                }
            }

            public void Undo()
            {
                if (!isNothing)
                {
                    box.BackColor = before;
                }
            }

            public void Redo()
            {
                if (!isNothing)
                {
                    box.BackColor = after;
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is Action other)
                {
                    return box == other.box && before == other.before && after == other.after;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return box.GetHashCode() ^ before.GetHashCode() ^ after.GetHashCode();
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

        public void ExportAsSvg(string filename, bool pixelsWithBorders)
        {
            int width = PixelSize * (ImagePanel.Width / PixelSize);
            int height = PixelSize * (ImagePanel.Height / PixelSize);

            var svg = GenerateSvg(pixelsWithBorders, width, height);

            var xml = svg.GetXML();
            File.WriteAllText(filename, xml);
        }

        public SvgDocument GenerateSvg(bool pixelsWithBorders, int width, int height)
        {

            var svgDocument = new SvgDocument
            {
                Width = new SvgUnit(width),
                Height = new SvgUnit(height),
                ViewBox = new SvgViewBox(0, 0, width, height)
            };

            foreach (Control control in ImagePanel.Controls)
            {
                var box = (PictureBox)control;
                var x = box.Location.X;
                var y = box.Location.Y;
                var color = box.BackColor;

                var rect = new SvgRectangle
                {
                    X = new SvgUnit(x),
                    Y = new SvgUnit(y),
                    Width = new SvgUnit(PixelSize),
                    Height = new SvgUnit(PixelSize),
                    Fill = new SvgColourServer(color)
                };

                if (pixelsWithBorders)
                {
                    rect.Stroke = new SvgColourServer(Color.Black);
                    rect.StrokeWidth = 1;
                }

                svgDocument.Children.Add(rect);
            }

            return svgDocument;
        }

        public void ExportPngFromSvg(string fileName, bool pixelsWithBorders)
        {
            int width = PixelSize * (ImagePanel.Width / PixelSize);
            int height = PixelSize * (ImagePanel.Height / PixelSize);
            var svg = GenerateSvg(pixelsWithBorders, width, height);

            // Create a bitmap to render the SVG
            var bitmap = new Bitmap(width, height);
            svg.Draw(bitmap);

            // Save the bitmap as a PNG file
            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ExportToFileEvent(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG " + GetLang("Picture") + "(*.png)|*.png|SVG " + GetLang("Picture") + "(*.svg)|*.svg";
            saveFileDialog.Title = "PixelPaint | " + GetLang("Picture") + " " + GetLang("Save_Menu_Item");
            saveFileDialog.FileName = "";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (saveFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                // ask the user with a popup if they want to have pixel borders
                var result = MessageBox.Show(GetLang("Export_Border_Msg"), GetLang("Export_Border_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                var bordered = result == DialogResult.Yes;

                if (saveFileDialog.FilterIndex == 1)
                {
                    ExportPngFromSvg(saveFileDialog.FileName, bordered);
                }
                else if (saveFileDialog.FilterIndex == 2)
                {
                    ExportAsSvg(saveFileDialog.FileName, bordered);
                }
            }
        }

        private void EditForm_Resize(object sender, EventArgs e)
        {
            if (Size.Width * Size.Height < minimumSize.Width * minimumSize.Height)
            {
                Size = minimumSize;
            }
        }

        private void EditForm_ResizeBegin(object sender, EventArgs e)
        {
            if (Size.Width * Size.Height < minimumSize.Width * minimumSize.Height)
            {
                FormBorderStyle = FormBorderStyle.FixedSingle;
            }
        }

        private void EditForm_ResizeEnd(object sender, EventArgs e)
        {
            if (Size.Width * Size.Height < minimumSize.Width * minimumSize.Height)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private void EditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
            {
                return;
            }
            if (ProcessFormClosing(sender, e))
            RemoveChild(this);
        }

        public bool ProcessFormClosing(object sender, FormClosingEventArgs e)
        {
            // check for unsaved changes 
            if (!Saved)
            {
                var result = MessageBox.Show(GetLang("Unsaved_Changes_Msg"), GetLang("Unsaved_Changes_Title"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    NewSaveEvent(sender, e);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return false;
                }
            }

            foreach (Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }

            return true;
        }

        private void Up(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void Down(object sender, MouseEventArgs e)
        {
            mouseDown = true;
        }

        private void EditForm_MouseMove(object sender, MouseEventArgs e)
        {
            BringToFront();
            if (e.Button == MouseButtons.Left)
            {
                foreach (Control control in ImagePanel.Controls)
                {
                    if (control != null)
                    {
                        if (control is PictureBox box)
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                Undo();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Y))
            {
                Redo();
                return true;
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                NewSaveEvent(this, new EventArgs());
                return true;
            }
            if (keyData == (Keys.Control | Keys.O))
            {
                OpenFileEvent(this, new EventArgs());
                return true;
            }
            if (keyData == (Keys.Control | Keys.E))
            {
                ExportToFileEvent(this, new EventArgs());
                return true;
            }
            if (keyData == (Keys.Control | Keys.S | Keys.Shift))
            {
                SaveAs(this, new EventArgs());
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

