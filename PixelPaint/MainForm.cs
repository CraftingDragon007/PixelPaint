using PixelPaint.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class MainForm : Form
    {
        public static List<Thread> Threads = new List<Thread>();

        public static readonly string[] SupportedLanguages = { "en", "de", "fr" };
        public static CultureInfo CultureInfo = Settings.Default.Language;
        public static ResourceManager LangManager;

        private static List<EditForm> Editors = new List<EditForm>();

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
                if (Array.IndexOf(SupportedLanguages, userLang) != -1)
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
            LangManager = new ResourceManager("PixelPaint.Languages.Res", typeof(MainForm).Assembly);
            FileToolStripMenuItem.Text = GetLang("File_Menu");
            NewToolStripMenuItem.Text = GetLang("New_Menu_Item");
            OpenToolStripMenuItem.Text = GetLang("Open_Menu_Item");
            LanguageToolStripMenuItem.Text = GetLang("Language");
        }

        public static string GetLang(string key)
        {
            return LangManager.GetString(key, CultureInfo);
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var thread = new Thread(CreateNew);
            Threads.Add(thread);
            thread.Start();
        }

        private void CreateNew()
        {
            var editor = new EditForm();
            editor.TopLevel = false;
            Invoke(new Action(() =>
            {
                panel1.Controls.Add(editor);
                editor.Show();
                editor.BringToFront();
            }));
            editor.NewProject();
            Editors.Add(editor);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "BetterPixelPaintFile(*.bxp)|*.bxp|PixelPaintFile(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                var newFormat = openFileDialog.FilterIndex == 1;
                var thread = new Thread(() => { Open(openFileDialog.FileName, newFormat); });
                Threads.Add(thread);
                thread.Start();
            }
        }

        private void Open(string fileName, bool newFormat)
        {
            var editor = new EditForm();
            editor.TopLevel = false;
            Invoke(new Action(() =>
            {
                panel1.Controls.Add(editor);
                editor.Show();
                editor.BringToFront();
            }));
            if (newFormat) editor.NewOpen(fileName);
            else
                editor.Open(fileName);
            Editors.Add(editor);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
                return;
            }
            foreach (var child in Editors.ToList())
            {
                if (!child.ProcessFormClosing(sender, new FormClosingEventArgs(CloseReason.FormOwnerClosing, false)))
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    panel1.Controls.Remove(child);
                    Editors.Remove(child);
                }
            }

            foreach (var thread in Threads)
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
            if (dialog.ShowDialog().Equals(DialogResult.Cancel))
            {
                return;
            }
            CultureInfo = dialog.LanguageIndex;
            Settings.Default.Language = CultureInfo;
            Settings.Default.Save();
            if (MessageBox.Show("To Change the Language the Application needs to restart.\nDo You want to Restart now?", "Restart Needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Equals(DialogResult.Yes))
            {
                Application.Restart();
            }
        }

        internal static void RemoveChild(EditForm editForm)
        {
            Editors.Remove(editForm);
        }
    }
}
