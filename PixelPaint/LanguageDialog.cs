using PixelPaint.Properties;
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
        public LanguageDialog(CultureInfo defaultLanguage = null)
        {
            InitializeComponent();
            foreach (var lang in MainForm.supportedLanguages)
                LanguageComboBox.Items.Add(CultureInfo.GetCultureInfo(lang).NativeName);                
            
            if (LanguageIndex == null)
                LanguageIndex = Settings.Default.Language;

            if (defaultLanguage != null)
                LanguageIndex = defaultLanguage;

            // Set the selected language and text of the combobox
            LanguageComboBox.Text = LanguageIndex.NativeName;
            LanguageComboBox.SelectedIndex = Array.IndexOf(MainForm.supportedLanguages, LanguageIndex.Name);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            LanguageIndex = CultureInfo.GetCultureInfo(MainForm.supportedLanguages[LanguageComboBox.SelectedIndex]);

            DialogResult = DialogResult.OK;
            Close();
        }

        public CultureInfo LanguageIndex { get; set; }        
    }
}
