﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CmisSync.Lib;

namespace CmisSync
{
    public partial class MissingFolderDialog : Form
    {
        public enum Action { MOVE, REMOVE, RECREATE }

        public Action action;
        private Lib.Config.SyncConfig.LocalRepository repo;

        public MissingFolderDialog()
        {
            InitializeComponent();
        }

        public MissingFolderDialog(Lib.Config.SyncConfig.LocalRepository repo) : this()
        {
            this.repo = repo;
            this.lblLocalPath.Text = repo.LocalPath;
            this.lblRemotePath.Text = repo.RemotePath;
        }

        private void RepositoryAction_Load(object sender, EventArgs e)
        {

        }

        private void btnMoved_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.action = Action.MOVE;
            this.Close();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.action = Action.REMOVE;
            this.Close();
        }

        private void btnResync_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.action = Action.RECREATE;
            this.Close();
        }

        private void lblMessage_Click(object sender, EventArgs e)
        {

        }
    }
}
