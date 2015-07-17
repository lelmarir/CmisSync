using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CmisSync.Setup
{
    public partial class SetupWindow2 : Form
    {
        private Pages.Page _page;
        public Pages.Page Page
        {
            get
            { return this._page; }
            set
            {

                this.title.Text = value.Title;
                this.description.Text = value.Description;
                this.footerButtonsLayout.Controls.Clear();
                this.footerButtonsLayout.Controls.AddRange(value.FooterButtons);
                Control previousControl = this.tableLayoutPanel1.GetControlFromPosition(0, 1);
                if (previousControl != null) 
                {
                    this.tableLayoutPanel1.Controls.Remove(previousControl);
                }
                this.tableLayoutPanel1.Controls.Add(value, 0, 1);
                value.Dock = DockStyle.Fill;

                value.Parent = this;
                this._page = value;
            }
        }

        public SetupWindow2()
        {
            InitializeComponent();
        }

        private void SetupWindow2_Load(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
