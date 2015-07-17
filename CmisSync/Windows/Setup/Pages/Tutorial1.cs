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
    public partial class Tutorial1 : Page
    {
        public Tutorial1()
        {
            InitializeComponent();

            this.FooterButtons = new Button[] { this.continueButton, this.skipTutorialButton };

            skipTutorialButton.Click += delegate
            {
                Close();
            };

            continueButton.Click += delegate
            {
                NavigateTo(new Tutorial2());
            };
        }

        private void Tutorial1_Load(object sender, EventArgs e)
        {
            
        }
    }
}
