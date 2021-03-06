using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Labrune
{
    public partial class LabruneFind : Form
    {
        public String ValueToFind { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool AlsoSearchInHashesAndLabels { get; set; }

        public LabruneFind()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            ValueToFind = FindTextBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CheckCase_CheckedChanged(object sender, EventArgs e)
        {
            IsCaseSensitive = CheckCase.Checked;
        }

        private void CheckAlsoHash_CheckedChanged(object sender, EventArgs e)
        {
            AlsoSearchInHashesAndLabels = CheckAlsoHash.Checked;
        }
    }
}
