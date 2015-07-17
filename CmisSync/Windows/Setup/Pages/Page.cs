using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Resources;
using System.Drawing.Design;

namespace CmisSync.Setup.Pages
{
    public partial class Page : UserControl
    {
        [Browsable(true)]
        [Localizable(true)]
        [SettingsBindable(true)]
        [Description("The page title"), Category("Appearance")] 
        public string Title { get; set; }

        [Browsable(true)]
        [Localizable(true)]
        [SettingsBindable(true)]
        [Description("The page description"), Category("Appearance")]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public string Description { get; set; }

        [Browsable(false)]
        public Button[] FooterButtons { get; set; }

        //TODO: genealize
        internal SetupWindow2 Parent; 

        public Page()
        {
            InitializeComponent();
            FooterButtons = new Button[] {backButton, cancelButton, continueButton};
        }

        private void Page_Load(object sender, EventArgs e)
        {

        }

        protected void NavigateTo(Page page) 
        {
            Parent.Page = page;
        }

        protected void Close()
        {
            Parent.Close();
        }
    }
}
