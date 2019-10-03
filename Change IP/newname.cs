using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Change_IP
{
    public partial class newname : Form
    {
        public newname()
        {
            InitializeComponent();
        }
        public string profileName = "";
        private void button1_Click(object sender, EventArgs e)
        {
            profileName = textBox1.Text;
            if (profileName.Trim() == "")
            {
                MessageBox.Show("Please enter profile name.");
            }
            else
            {
                this.Close();
            }
        }
    }
}
