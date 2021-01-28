using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class Form1 : Form
    {
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
            this.WindowState = FormWindowState.Maximized;
        }

        private void neuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Main main = new Main();
            main.TopLevel = false;
            panel1.Controls.Add(main);
            main.Show();
        }

        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PixelPaintDateien(*.pxp)|*.pxp";

            if (openFileDialog.ShowDialog().Equals(DialogResult.OK))
            {
                Main main = new Main();
                main.TopLevel = false;
                panel1.Controls.Add(main);
                main.Show();
                main.Open(openFileDialog.FileName);
            }
        }
    }
}
