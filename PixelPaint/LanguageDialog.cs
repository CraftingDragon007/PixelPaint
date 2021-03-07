using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelPaint
{
    public partial class LanguageDialog : Form
    {
        public LanguageDialog()
        {
            LanguageIndex = CultureInfo.CreateSpecificCulture("en");
            InitializeComponent();
        }

        private void LanguageDialog_Load(object sender, EventArgs e)
        {
            
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            switch (LanguageComboBox.SelectedIndex)
            {
                case 0:
                    LanguageIndex = CultureInfo.CreateSpecificCulture("en");
                    break;
                case 1:
                    LanguageIndex = CultureInfo.CreateSpecificCulture("de");
                    break;
                case 2:
                    LanguageIndex = CultureInfo.CreateSpecificCulture("fr");
                    break;
                default:
                    return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public CultureInfo LanguageIndex { get; set; }        
    }
}
