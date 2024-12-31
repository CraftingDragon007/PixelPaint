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

        public static readonly string[] supportedLanguages = { "en", "de", "fr" };
        public static CultureInfo CultureInfo = Settings.Default.Language;
        public static ResourceManager langManager;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Settings.Default.FirstRun)
            {
                Settings.Default.FirstRun = false;
                Settings.Default.Save();
                LanguageDialog dialog;
                var userLang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                if (Array.IndexOf(supportedLanguages, userLang) != -1)
                {
                    dialog = new LanguageDialog(CultureInfo.GetCultureInfo(userLang));
                }
                else
                {
                    dialog = new LanguageDialog(CultureInfo);
                }

                if (dialog.ShowDialog().Equals(DialogResult.Cancel))
                {
                    return;
                }

                CultureInfo = dialog.LanguageIndex;
                Settings.Default.Language = CultureInfo;
                Settings.Default.Save();
            }
            WindowState = FormWindowState.Maximized;
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
            var thread = new Thread(CreateNew);
            threads.Add(thread);
            thread.Start();
        }

        private void CreateNew()
        {
            var main = new EditForm();
            main.TopLevel = false;
            Invoke(new Action(() => { panel1.Controls.Add(main); }));
            Invoke(new Action(() => { main.Show(); }));
            main.NewProject();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                var newFormat = openFileDialog.FilterIndex == 1;
                var thread = new Thread(() => { Open(openFileDialog.FileName, newFormat); });
                threads.Add(thread);
                thread.Start();
            }
        }

        private void Open(string fileName, bool newFormat)
        {
            var main = new EditForm();
            main.TopLevel = false;
            Invoke(new Action(() => { panel1.Controls.Add(main); }));
            Invoke(new Action(() => { panel1.Controls.Add(main); }));
            Invoke(new Action(() => { main.Show(); }));
            if (newFormat) main.NewOpen(fileName);
            else
            main.Open(fileName);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var thread in threads)
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
            var dialog = new LanguageDialog(CultureInfo);
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
