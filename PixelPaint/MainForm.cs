using PixelPaint.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class MainForm : Form
    {
        public static List<Thread> threads = new List<Thread>();

        public static CultureInfo CultureInfo = Settings.Default.Language;
        public static ResourceManager langManager;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            langManager = new ResourceManager("PixelPaint.Languages.Res", typeof(MainForm).Assembly);
            FileToolStripMenuItem.Text = GetLang("File_Menu");
            NewToolStripMenuItem.Text = GetLang("New_Menu_Item");
            OpenToolStripMenuItem.Text = GetLang("Open_Menu_Item");
            LanguageToolStripMenuItem.Text = GetLang("Language");
        }

        public static string GetLang(string key)
        {
            return langManager.GetString(key, CultureInfo);
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(CreateNew);
            MainForm.threads.Add(thread);
            thread.Start();
        }

        private void CreateNew()
        {
            EditForm main = new EditForm();
            main.TopLevel = false;
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { main.Show(); }));
            main.NewProjekt();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                var newFormat = openFileDialog.FilterIndex == 1;
                Thread thread = new Thread(() => { Open(openFileDialog.FileName, newFormat); });
                MainForm.threads.Add(thread);
                thread.Start();
            }
        }

        private void Open(string fileName, bool newFormat)
        {
            EditForm main = new EditForm();
            main.TopLevel = false;
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { main.Show(); }));
            if (newFormat) main.NewOpen(fileName);
            else
            main.Open(fileName);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }
            Application.Exit();
        }

        private void LanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LanguageDialog dialog = new LanguageDialog();
            dialog.LanguageIndex = CultureInfo;
            if (dialog.ShowDialog().Equals(DialogResult.Cancel)){
                return;
            }
            CultureInfo = dialog.LanguageIndex;
            Settings.Default.Language = CultureInfo;
            Settings.Default.Save();
            if(MessageBox.Show("To Change the Language the Application needs to restart.\nDo You want to Restart now?", "Restart Needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Equals(DialogResult.Yes))
            {
                Application.Restart();
            }
        }
    }
}
