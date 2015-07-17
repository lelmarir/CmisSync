using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CmisSync.Setup.Pages
{
    public partial class Tutorial4 : Page
    {
        public Tutorial4()
        {
            InitializeComponent();
            
            this.FooterButtons = new Button[] { this.finishButton };

            finishButton.Click += delegate
            {
                Close();
            };
        }

        private void Tutorial4_Load(object sender, EventArgs e)
        {
            
        }
    }
}
