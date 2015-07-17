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
    public partial class Tutorial2 : Page
    {
        public Tutorial2()
        {
            InitializeComponent();
            
            this.FooterButtons = new Button[] { this.continueButton };

            continueButton.Click += delegate
            {
                NavigateTo(new Tutorial3());
            };
        }

        private void Tutorial2_Load(object sender, EventArgs e)
        {
            
        }
    }
}
