using PixelPaint.Properties;
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
        //private bool isLoaded;
        private int s;

        private List<Thread> threads = new List<Thread>();

        public Main()
        {
            InitializeComponent();
        }

        public void changeTitle(String text)
        {
            this.Invoke(new System.Action(() => { this.Text = /*"PixelPaint | " +*/ text; }));   
            return;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.minimumSize = this.Size;
            this.Text = "Menü";
            s = Settings.Default.PixelSize;
            //newProjekt();
            //Form form = new Form();
            //form.TopLevel = false;
            //ImagePanel.Controls.Add((Control)form);
            //form.Show();
            //MenuPanel.Visible = false;
        }

        public void NewProjekt()
        {
            Thread thread = new Thread(() => {
                //isLoaded = false;
                //changeTitle("Unbenannt*");
                this.Invoke(new System.Action(() => { this.Text = /*"PixelPaint | " +*/ "Unbenannt*"; }));
                //this.name = "Unbenannt";
                ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));              
                px = 0;
                //MenuPanel.Visible = false;
                //EditorPanel.Visible = true;            
                int x = 0;
                int y = 0;
                int s = this.s;
                //MessageBox.Show(ImagePanel.Height.ToString());
                while (y + s <= ImagePanel.Height)
                {
                    //this.changeTitle(y.ToString());
                    while (x + s <= ImagePanel.Width)
                    {
                        //MessageBox.Show("");
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
                        label2.Invoke(new System.Action(() => { label2.Text = px.ToString(); })); 
                        //MessageBox.Show(box.Name);
                    }
                    x = 0;
                    y += s;
                }

                //isLoaded = true;
                //MessageBox.Show("Finished!");
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
                 this.Invoke(new System.Action(() => { this.Text = fileName.Substring(fileName.LastIndexOf("\\")+1).Replace(".pxp", ""); }));
                StreamReader reader = new StreamReader(File.OpenRead(fileName));
                String content = reader.ReadToEnd();
                string[] result = content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                Properties.Settings.Default.PixelSize = int.Parse(result[0].Split('=')[1]);
                Properties.Settings.Default.Save();

                    //changeTitle("Unbenannt*");
                    this.Invoke(new System.Action(() => { this.Text = /*"PixelPaint | " +*/ "Unbenannt*"; }));
                    //this.name = "Unbenannt";
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    //MenuPanel.Visible = false;
                    //EditorPanel.Visible = true;            
                    int x = 0;
                    int y = 0;
                    int s = int.Parse(result[0].Split('=')[1]);
                    //MessageBox.Show(ImagePanel.Height.ToString());
                    while (y + s <= ImagePanel.Height)
                    {
                        //this.changeTitle(y.ToString());
                        while (x + s <= ImagePanel.Width)
                        {
                            //MessageBox.Show("");
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
                            label2.Invoke(new System.Action(() => { label2.Text = px.ToString(); }));
                            //MessageBox.Show(box.Name);
                        }
                        x = 0;
                        y += s;
                    }

                    this.fileName = fileName;
                this.name = fileName.Substring(fileName.LastIndexOf("\\")+1).Replace(".pxp", "");
                result = result.Where(val => val != result[0]).ToArray();
                int i = 0;
                //while (!isLoaded) { }
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
                //isLoaded = true;
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
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
                if(int.Parse(value) < 5)
                {
                    MessageBox.Show("Die minimale Pixel Grösse ist 5!", "Warnung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Properties.Settings.Default.PixelSize = int.Parse(value);
                Properties.Settings.Default.Save();

                this.s = int.Parse(value);

                Thread thread = new Thread(() => {
                    //isLoaded = false;
                    //changeTitle("Unbenannt*");
                    this.Invoke(new System.Action(() => { this.Text = /*"PixelPaint | " +*/ "Unbenannt*"; }));
                    //this.name = "Unbenannt";
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    //MenuPanel.Visible = false;
                    //EditorPanel.Visible = true;            
                    int x = 0;
                    int y = 0;
                    int s = this.s;
                    //MessageBox.Show(ImagePanel.Height.ToString());
                    while (y + s <= ImagePanel.Height)
                    {
                        //this.changeTitle(y.ToString());
                        while (x + s <= ImagePanel.Width)
                        {
                            //MessageBox.Show("");
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
                            label2.Invoke(new System.Action(() => { label2.Text = px.ToString(); }));
                            //MessageBox.Show(box.Name);
                        }
                        x = 0;
                        y += s;
                    }

                    //isLoaded = true;
                    //MessageBox.Show("Finished!");
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
                Thread thread = new Thread(() => {
                    //isLoaded = false;
                    //changeTitle("Unbenannt*");
                    this.Invoke(new System.Action(() => { this.Text = /*"PixelPaint | " +*/ "Unbenannt*"; }));
                    //this.name = "Unbenannt";
                    ImagePanel.Invoke(new System.Action(() => { ImagePanel.Controls.Clear(); }));
                    px = 0;
                    //MenuPanel.Visible = false;
                    //EditorPanel.Visible = true;            
                    int x = 0;
                    int y = 0;
                    int s = this.s;
                    //MessageBox.Show(ImagePanel.Height.ToString());
                    while (y + s <= ImagePanel.Height)
                    {
                        //this.changeTitle(y.ToString());
                        while (x + s <= ImagePanel.Width)
                        {
                            //MessageBox.Show("");
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
                            label2.Invoke(new System.Action(() => { label2.Text = px.ToString(); }));
                            //MessageBox.Show(box.Name);
                        }
                        x = 0;
                        y += s;
                    }

                    //isLoaded = true;
                    //MessageBox.Show("Finished!");
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

        private void Save_Under(object sender, EventArgs e)
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

        private void OpenEvent(object sender, EventArgs e)
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

        internal Label Label1;
        internal RadioButton Green;
        internal RadioButton Gelb;
        internal RadioButton Rot;
        internal RadioButton Blau;
        private Panel ImagePanel;
        private Label label2;
        private RadioButton White;
        private RadioButton Black;
        private RadioButton Other;
        private Button button1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem dateiToolStripMenuItem;
        private ToolStripMenuItem öffnenToolStripMenuItem;
        private ToolStripMenuItem speichernToolStripMenuItem;
        private ToolStripMenuItem speichernUnterToolStripMenuItem;
        private ToolStripMenuItem resetToolStripMenuItem;
        private ToolStripMenuItem pixelGrösseToolStripMenuItem;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.Label1 = new System.Windows.Forms.Label();
            this.Green = new System.Windows.Forms.RadioButton();
            this.Gelb = new System.Windows.Forms.RadioButton();
            this.Rot = new System.Windows.Forms.RadioButton();
            this.Blau = new System.Windows.Forms.RadioButton();
            this.ImagePanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.White = new System.Windows.Forms.RadioButton();
            this.Black = new System.Windows.Forms.RadioButton();
            this.Other = new System.Windows.Forms.RadioButton();
            this.button1 = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.dateiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.öffnenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speichernToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speichernUnterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pixelGrösseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportierenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bearbeitenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rückgängigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wiederholenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(11, 210);
            this.Label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(32, 13);
            this.Label1.TabIndex = 40;
            this.Label1.Text = "Pixel:";
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 210);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(13, 13);
            this.label2.TabIndex = 44;
            this.label2.Text = "0";
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
            this.White.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
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
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(67, 161);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(24, 23);
            this.button1.TabIndex = 48;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dateiToolStripMenuItem,
            this.bearbeitenToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 52;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // dateiToolStripMenuItem
            // 
            this.dateiToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.öffnenToolStripMenuItem,
            this.speichernToolStripMenuItem,
            this.speichernUnterToolStripMenuItem,
            this.resetToolStripMenuItem,
            this.pixelGrösseToolStripMenuItem,
            this.exportierenToolStripMenuItem});
            this.dateiToolStripMenuItem.Name = "dateiToolStripMenuItem";
            this.dateiToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.dateiToolStripMenuItem.Text = "Datei";
            // 
            // öffnenToolStripMenuItem
            // 
            this.öffnenToolStripMenuItem.Name = "öffnenToolStripMenuItem";
            this.öffnenToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.öffnenToolStripMenuItem.Text = "Öffnen";
            this.öffnenToolStripMenuItem.Click += new System.EventHandler(this.OpenEvent);
            // 
            // speichernToolStripMenuItem
            // 
            this.speichernToolStripMenuItem.Name = "speichernToolStripMenuItem";
            this.speichernToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.speichernToolStripMenuItem.Text = "Speichern";
            this.speichernToolStripMenuItem.Click += new System.EventHandler(this.SaveEvent);
            // 
            // speichernUnterToolStripMenuItem
            // 
            this.speichernUnterToolStripMenuItem.Name = "speichernUnterToolStripMenuItem";
            this.speichernUnterToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.speichernUnterToolStripMenuItem.Text = "Speichern unter...";
            this.speichernUnterToolStripMenuItem.Click += new System.EventHandler(this.Save_Under);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.resetToolStripMenuItem.Text = "Reset";
            this.resetToolStripMenuItem.Click += new System.EventHandler(this.ResetPixels);
            // 
            // pixelGrösseToolStripMenuItem
            // 
            this.pixelGrösseToolStripMenuItem.Name = "pixelGrösseToolStripMenuItem";
            this.pixelGrösseToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pixelGrösseToolStripMenuItem.Text = "Pixel Grösse..";
            this.pixelGrösseToolStripMenuItem.Click += new System.EventHandler(this.ChangePixelSize);
            // 
            // exportierenToolStripMenuItem
            // 
            this.exportierenToolStripMenuItem.Name = "exportierenToolStripMenuItem";
            this.exportierenToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportierenToolStripMenuItem.Text = "Exportieren...";
            this.exportierenToolStripMenuItem.Click += new System.EventHandler(this.exportierenToolStripMenuItem_Click);
            // 
            // bearbeitenToolStripMenuItem
            // 
            this.bearbeitenToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rückgängigToolStripMenuItem,
            this.wiederholenToolStripMenuItem});
            this.bearbeitenToolStripMenuItem.Name = "bearbeitenToolStripMenuItem";
            this.bearbeitenToolStripMenuItem.Size = new System.Drawing.Size(75, 20);
            this.bearbeitenToolStripMenuItem.Text = "Bearbeiten";
            // 
            // rückgängigToolStripMenuItem
            // 
            this.rückgängigToolStripMenuItem.Name = "rückgängigToolStripMenuItem";
            this.rückgängigToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.rückgängigToolStripMenuItem.Text = "Rückgängig";
            this.rückgängigToolStripMenuItem.Click += new System.EventHandler(this.rückgängigToolStripMenuItem_Click);
            // 
            // wiederholenToolStripMenuItem
            // 
            this.wiederholenToolStripMenuItem.Name = "wiederholenToolStripMenuItem";
            this.wiederholenToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.wiederholenToolStripMenuItem.Text = "Wiederholen";
            this.wiederholenToolStripMenuItem.Click += new System.EventHandler(this.wiederholenToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 499);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Other);
            this.Controls.Add(this.Black);
            this.Controls.Add(this.White);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ImagePanel);
            this.Controls.Add(this.Green);
            this.Controls.Add(this.Label1);
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

        private ToolStripMenuItem bearbeitenToolStripMenuItem;
        private ToolStripMenuItem rückgängigToolStripMenuItem;
        private ToolStripMenuItem wiederholenToolStripMenuItem;

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

        private ToolStripMenuItem exportierenToolStripMenuItem;

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
