namespace CmisSync.Setup
{
    partial class SetupWindow2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupWindow2));
            this.buttonsSeparationLine = new System.Windows.Forms.Label();
            this.sideSplash = new System.Windows.Forms.Panel();
            this.footerButtonsLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.continueButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.backButton = new System.Windows.Forms.Button();
            this.headerLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.title = new System.Windows.Forms.Label();
            this.description = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.content = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.footerButtonsLayout.SuspendLayout();
            this.headerLayout.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.content.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonsSeparationLine
            // 
            this.buttonsSeparationLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.buttonsSeparationLine, "buttonsSeparationLine");
            this.buttonsSeparationLine.Name = "buttonsSeparationLine";
            // 
            // sideSplash
            // 
            resources.ApplyResources(this.sideSplash, "sideSplash");
            this.sideSplash.Name = "sideSplash";
            // 
            // footerButtonsLayout
            // 
            this.footerButtonsLayout.Controls.Add(this.continueButton);
            this.footerButtonsLayout.Controls.Add(this.cancelButton);
            this.footerButtonsLayout.Controls.Add(this.backButton);
            resources.ApplyResources(this.footerButtonsLayout, "footerButtonsLayout");
            this.footerButtonsLayout.Name = "footerButtonsLayout";
            // 
            // continueButton
            // 
            resources.ApplyResources(this.continueButton, "continueButton");
            this.continueButton.Name = "continueButton";
            this.continueButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // backButton
            // 
            resources.ApplyResources(this.backButton, "backButton");
            this.backButton.Name = "backButton";
            this.backButton.UseVisualStyleBackColor = true;
            // 
            // headerLayout
            // 
            resources.ApplyResources(this.headerLayout, "headerLayout");
            this.headerLayout.Controls.Add(this.title);
            this.headerLayout.Controls.Add(this.description);
            this.headerLayout.Name = "headerLayout";
            // 
            // title
            // 
            resources.ApplyResources(this.title, "title");
            this.title.ForeColor = System.Drawing.SystemColors.Highlight;
            this.title.Name = "title";
            // 
            // description
            // 
            resources.ApplyResources(this.description, "description");
            this.description.ForeColor = System.Drawing.SystemColors.GrayText;
            this.description.Name = "description";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.headerLayout, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.content, 0, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // content
            // 
            resources.ApplyResources(this.content, "content");
            this.content.Controls.Add(this.label1);
            this.content.Name = "content";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // SetupWindow2
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.footerButtonsLayout);
            this.Controls.Add(this.sideSplash);
            this.Controls.Add(this.buttonsSeparationLine);
            this.Name = "SetupWindow2";
            this.Load += new System.EventHandler(this.SetupWindow2_Load);
            this.footerButtonsLayout.ResumeLayout(false);
            this.headerLayout.ResumeLayout(false);
            this.headerLayout.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.content.ResumeLayout(false);
            this.content.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label buttonsSeparationLine;
        private System.Windows.Forms.Panel sideSplash;
        private System.Windows.Forms.FlowLayoutPanel footerButtonsLayout;
        private System.Windows.Forms.Button continueButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button backButton;
        private System.Windows.Forms.FlowLayoutPanel headerLayout;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.Label description;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel content;
        private System.Windows.Forms.Label label1;
    }
}