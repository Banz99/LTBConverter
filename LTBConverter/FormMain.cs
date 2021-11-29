using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;


namespace LTBConverter
{
    public partial class FormMain : Form
    {

        public FormMain()
        {
            InitializeComponent();
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            ArrayList Encodings = new ArrayList();
            Encodings.Add(Encoding.UTF8);
            Encodings.Add(Encoding.GetEncoding(1252));
            cmbEnconding.DataSource = Encodings;

            cmbEnconding.DisplayMember = "EncodingName";
            cmbEnconding.ValueMember = "CodePage";
        }

        private void cmbEnconding_SelectedIndexChanged(object sender, EventArgs e)
        {
            LTBManagement.EncodingCodePage = (int)cmbEnconding.SelectedValue;
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdin = new OpenFileDialog();
            fdin.Title = "Select the input .ltb file";
            fdin.Filter = "WF Engine text files (*.ltb)| *.ltb";

            if (fdin.ShowDialog() == DialogResult.OK)
            {
                SaveFileDialog fdout = new SaveFileDialog();
                fdout.Title = "Select the output .xml file";
                fdout.Filter = "Extensible Markup Language files (*.xml)| *.xml";
                fdout.FileName = Path.GetDirectoryName(fdin.FileName) + "\\" + Path.GetFileNameWithoutExtension(fdin.FileName) + ".xml";

                if (fdout.ShowDialog() == DialogResult.OK) {
                    try
                    {
                        LTBManagement.LTBExtract(fdin.FileName, fdout.FileName);
                        MessageBox.Show("File extracted successfully");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void btnRepack_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdin = new OpenFileDialog();
            fdin.Title = "Select the previously extracted .xml file";
            fdin.Filter = "Extensible Markup Language files (*.xml)| *.xml";

            if (fdin.ShowDialog() == DialogResult.OK)
            {
                SaveFileDialog fdout = new SaveFileDialog();
                fdout.Title = "Select the output .ltb file";
                fdout.Filter = "WF Engine text files (*.ltb)| *.ltb";
                fdout.FileName = Path.GetDirectoryName(fdin.FileName) + "\\" + Path.GetFileNameWithoutExtension(fdin.FileName) + "_rep.ltb";

                if (fdout.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LTBManagement.LTBRepack(fdin.FileName, fdout.FileName);
                        MessageBox.Show("File repacked successfully");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
