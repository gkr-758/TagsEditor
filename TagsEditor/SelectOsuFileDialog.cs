using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TagsEditor
{
    public partial class SelectOsuFileDialog : Form
    {
        public SelectOsuFileDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectOsuFileDialog));
            this.SuspendLayout();
            // 
            // SelectOsuFileDialog
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectOsuFileDialog";
            this.Load += new System.EventHandler(this.SelectOsuFileDialog_Load);
            this.ResumeLayout(false);

        }

        private void SelectOsuFileDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
