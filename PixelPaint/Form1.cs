using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class Form1 : Form
    {

        public static List<Thread> threads = new List<Thread>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            Screen myScreen = Screen.FromControl(this);
            Rectangle area = myScreen.WorkingArea;
            this.Location = new Point(0, 0);
            this.Size = area.Size;
            */
            this.WindowState = FormWindowState.Normal;
        }

        private void neuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(CreateNew);
            Form1.threads.Add(thread);
            thread.Start();
        }

        private void CreateNew()
        {
            Main main = new Main();
            main.TopLevel = false;
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { main.Show(); }));
            main.NewProjekt();
        }

        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PixelPaintDateien(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                Thread thread = new Thread(() => { Open(openFileDialog.FileName); });
                Form1.threads.Add(thread);
                thread.Start();
            }
        }

        private void Open(string fileName)
        {
            Main main = new Main();
            main.TopLevel = false;
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { panel1.Controls.Add(main); }));
            this.Invoke(new Action(() => { main.Show(); }));
            main.Open(fileName);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }

            Application.Exit();
        }
    }
}
