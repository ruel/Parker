using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Parker
{
    public partial class frmPrompt : Form
    {
        public string toRet = "";

        public frmPrompt(string lbl, string title)
        {
            InitializeComponent();
            this.Text = title;
            lblTxt.Text = lbl;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            toRet = txtIn.Text;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
