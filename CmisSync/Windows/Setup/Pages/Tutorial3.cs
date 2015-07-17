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
    public partial class Tutorial3 : Page
    {
        public Tutorial3()
        {
            InitializeComponent();
            
            this.FooterButtons = new Button[] { this.continueButton };

            continueButton.Click += delegate
            {
                NavigateTo(new Tutorial4());
            };
        }

        private void Tutorial3_Load(object sender, EventArgs e)
        {
            
        }
    }
}
