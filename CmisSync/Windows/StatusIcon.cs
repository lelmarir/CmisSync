﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows;
using System.Globalization;
using CmisSync.Lib;
using System.Reflection;
using System.Media;

namespace CmisSync
{
    /// <summary>
    /// CmisSync icon in the Windows status bar.
    /// </summary>
    public class StatusIcon : Form
    {
        /// <summary>
        /// MVC controller for the the status icon.
        /// </summary>
        public StatusIconController Controller = new StatusIconController();

        /// <summary>
        /// Context menu that appears when right-clicking on the CmisSync icon.
        /// </summary>
        private ContextMenuStrip traymenu = new ContextMenuStrip();

        /// <summary>
        /// Windows object for the status icon.
        /// </summary>
        private NotifyIcon trayicon = new NotifyIcon();

        /// <summary>
        /// Frames of the animation used when a download/upload is going on.
        /// The first frame is the static frame used when no activity is going on.
        /// </summary>
        private Icon[] animationFrames;

        /// <summary>
        /// Icon to show when a cmis error has occured.
        /// </summary>
        private Icon errorIcon;

        /// <summary>
        /// Menu item that shows the state of CmisSync (up-to-date, etc).
        /// </summary>
        private ToolStripMenuItem stateItem;

        /// <summary>
        /// Menu item that allows the user to exit CmisSync.
        /// </summary>
        private ToolStripMenuItem exitItem;


        /// <summary>
        /// Constructor.
        /// </summary>
        public StatusIcon()
        {
            // Create the menu.
            CreateIcons();
            CreateMenu();

            // Setup the status icon.
            this.trayicon.Icon = animationFrames[0];
            this.trayicon.Text = Properties_Resources.CmisSync;
            this.trayicon.ContextMenuStrip = this.traymenu;
            this.trayicon.Visible = true;
            this.trayicon.MouseClick += NotifyIcon1_MouseClick; //Open Root sync folder
            //this.trayicon.MouseClick += TrayIcon_MouseClick; //Open context menu
        }


        /// <summary>
        /// When form is loaded,
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Set up the controller to create menu elements on update.
            CreateInvokeMethods();

            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.
            base.OnLoad(e);
        }


        /// <summary>
        /// Set up the controller to create menu elements on update.
        /// </summary>
        private void CreateInvokeMethods()
        {
            // Icon.
            Controller.UpdateIconEvent += delegate(int icon_frame)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke((Action)delegate
                    {
                        if (icon_frame > -1)
                            this.trayicon.Icon = animationFrames[icon_frame];
                        else
                            this.trayicon.Icon = errorIcon;
                    });
                }
            };

            // Status item.
            Controller.UpdateStatusItemEvent += delegate(string state_text)
            {
                if (IsHandleCreated)
                {

                    BeginInvoke((Action)delegate
                    {
                        this.stateItem.Text = state_text;
                        this.trayicon.Text = Utils.Ellipsis(Properties_Resources.CmisSync + "\n" + state_text, 63);
                    });
                }
            };

            // Menu.
            Controller.UpdateMenuEvent += delegate(IconState state)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke((Action)delegate
                    {
                        CreateMenu();
                    });
                }
            };

            // Repo Submenu.
            Controller.UpdateSuspendSyncFolderEvent += delegate(Config.SyncConfig.LocalRepository repo)
            {
                if (IsHandleCreated)
                {
                    BeginInvoke((Action)delegate
                    {
                        ToolStripMenuItem repoitem = (ToolStripMenuItem)this.traymenu.Items["tsmi" + repo];
                        ToolStripMenuItem pauseitem = (ToolStripMenuItem)repoitem.DropDownItems.Find("pause", false)[0];
                        ToolStripMenuItem syncitem = (ToolStripMenuItem)repoitem.DropDownItems.Find("sync", false)[0];

                        foreach (RepoBase aRepo in Program.Controller.Repositories)
                        {
                            if (aRepo.RepoInfo == repo)
                            {
                                setSyncItemState(pauseitem, syncitem, aRepo.Status);
                                break;
                            }
                        }
                    });
                }
            };

            Program.Controller.AlertNotificationRaised += delegate(string title, string message)
            {
                if (ConfigManager.CurrentConfig.Notifications)
                {
                    //Only show balloon tips when notifications are on

                    SystemSounds.Exclamation.Play();

                    trayicon.ShowBalloonTip(25000, title, message, ToolTipIcon.Error);

                    //System.Windows.Forms.MessageBox.Show(message, title,
                    //    System.Windows.Forms.MessageBoxButtons.OK,
                    //    System.Windows.Forms.MessageBoxIcon.Error);
                }
            };
        }


        private void setSyncItemState(ToolStripMenuItem pauseItem, ToolStripMenuItem syncItem, SyncStatus status)
        {
            switch (status)
            {
                case SyncStatus.Idle:
                    pauseItem.Text = CmisSync.Properties_Resources.PauseSync;
                    pauseItem.Image = UIHelpers.GetBitmap("media_playback_pause");
                    syncItem.Enabled = true;
                    break;
                case SyncStatus.Suspend:
                    pauseItem.Text = CmisSync.Properties_Resources.ResumeSync;
                    pauseItem.Image = UIHelpers.GetBitmap("media_playback_start");
                    syncItem.Enabled = false;
                    break;
            }
        }

        /// <summary>
        /// Dispose of the status icon GUI elements.
        /// </summary>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                this.trayicon.Dispose();
            }

            base.Dispose(isDisposing);
        }


        /// <summary>
        /// Create the GUI elements of the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Reset existing items.
            this.traymenu.Items.Clear();

            // Create the state menu item.
            this.stateItem = new ToolStripMenuItem()
            {
                Text = Utils.Ellipsis(Controller.StateText, 32),
                Enabled = false
            };

            if (Controller.StateText.Length > 32)
            {
                this.stateItem.ToolTipText = Utils.WordWrap(Controller.StateText, 63);
            }

            this.traymenu.Items.Add(stateItem);
            this.trayicon.Text = Utils.Ellipsis(Properties_Resources.CmisSync + "\n" + Controller.StateText, 63);

            // Create a menu item per synchronized folder.
            if (Controller.Folders.Length > 0)
            {
                foreach (Config.SyncConfig.LocalRepository localRepo in Controller.Folders)
                {
                    // Main item.
                    ToolStripMenuItem subfolderItem = new ToolStripMenuItem()
                    {
                        Text = localRepo.DisplayName,
                        Name = "tsmi" + localRepo,
                        Image = UIHelpers.GetBitmap("folder"),
                    };

                    // Sub-item: open locally.
                    ToolStripMenuItem openLocalFolderItem = new ToolStripMenuItem()
                    {
                        Text = CmisSync.Properties_Resources.OpenLocalFolder,
                        Name = "openLocal",
                        Image = UIHelpers.GetBitmap("folder"),
                    };
                    openLocalFolderItem.Click += OpenLocalFolderDelegate(localRepo);

                    // Sub-item: open remotely.
                    ToolStripMenuItem openRemoteFolderItem = new ToolStripMenuItem()
                    {
                        Text = CmisSync.Properties_Resources.BrowseRemoteFolder,
                        Name = "openRemote",
                        Image = UIHelpers.GetBitmap("classic_folder_web"),
                    };
                    openRemoteFolderItem.Click += OpenRemoteFolderDelegate(localRepo);

                    // Sub-item: suspend sync.
                    ToolStripMenuItem suspendFolderItem = new ToolStripMenuItem()
                    {
                        Name = "pause",
                    };
                    suspendFolderItem.Click += SuspendSyncFolderDelegate(localRepo);

                    // Sub-item: remove folder from sync
                    ToolStripMenuItem removeFolderFromSyncItem = new ToolStripMenuItem()
                    {
                        Text = Properties_Resources.RemoveFolderFromSync,
                        Name = "remove",
                        Image = UIHelpers.GetBitmap("disconnect"),
                        Tag = "remove",
                    };
                    removeFolderFromSyncItem.Click += RemoveFolderFromSyncDelegate(localRepo);

                    // Sub-item: manual sync
                    ToolStripMenuItem manualSyncItem = new ToolStripMenuItem()
                    {
                        Text = Properties_Resources.ManualSync,
                        Name = "sync",
                        Image = UIHelpers.GetBitmap("media_playback_refresh"),
                    };
                    manualSyncItem.Click += ManualSyncDelegate(localRepo);

                    // Sub-item: settings dialog
                    ToolStripMenuItem settingsItem = new ToolStripMenuItem()
                    {
                        Text = Properties_Resources.Settings,
                        Name = "settings",
                    };
                    settingsItem.Click += SettingsDelegate(localRepo);


                    setSyncItemState(suspendFolderItem, manualSyncItem, SyncStatus.Idle);
                    foreach (RepoBase aRepo in Program.Controller.Repositories)
                    {
                        if (aRepo.RepoInfo.Equals(localRepo))
                        {
                            setSyncItemState(suspendFolderItem, manualSyncItem, aRepo.Status);
                            break;
                        }
                    }

                    // Add the sub-items.
                    subfolderItem.DropDownItems.Add(openLocalFolderItem);
                    subfolderItem.DropDownItems.Add(openRemoteFolderItem);
                    subfolderItem.DropDownItems.Add(new ToolStripSeparator());
                    subfolderItem.DropDownItems.Add(suspendFolderItem);
                    subfolderItem.DropDownItems.Add(manualSyncItem);

                    // Add the configuration modification sub-items, if configuration is not frozen.
                    if ( ! ConfigManager.CurrentConfig.FrozenConfiguration)
                    {
                        subfolderItem.DropDownItems.Add(new ToolStripSeparator());
                        subfolderItem.DropDownItems.Add(removeFolderFromSyncItem);
                        subfolderItem.DropDownItems.Add(new ToolStripSeparator());
                        subfolderItem.DropDownItems.Add(settingsItem);
                    }

                    // Add the main item.
                    this.traymenu.Items.Add(subfolderItem);
                }
            }
            this.traymenu.Items.Add(new ToolStripSeparator());
            
            // Create the menu item that lets the user add a new synchronized folder.
            // Depending on configuration, disable or skip some elements.
            if (!ConfigManager.CurrentConfig.FrozenConfiguration)
            {
                ToolStripMenuItem addFolderItem = new ToolStripMenuItem()
                {
                    Text = CmisSync.Properties_Resources.AddARemoteFolder,
                    Name = "add",
                    Image = UIHelpers.GetBitmap("connect")
                };
                addFolderItem.Click += delegate
                {
                    Controller.AddRemoteFolderClicked();
                };
                this.traymenu.Items.Add(addFolderItem);
                this.traymenu.Items.Add(new ToolStripSeparator());

                if (ConfigManager.CurrentConfig.SingleRepository && ConfigManager.CurrentConfig.LocalRepositories.Count > 0)
                {
                    //Configured for single repository and repository count 1 or more so disable menu item.
                    addFolderItem.Enabled = false;
                }
            }

            // Create the menu item that lets the user view the log.
            ToolStripMenuItem log_item = new ToolStripMenuItem()
            {
                Text = CmisSync.Properties_Resources.ViewLog,
                Name = "log"
            };
            log_item.Click += delegate
            {
                Controller.LogClicked();
            };
            this.traymenu.Items.Add(log_item);

            // Create the About menu.
            ToolStripMenuItem about_item = new ToolStripMenuItem()
            {
                Text = CmisSync.Properties_Resources.About,
                Name = "about"
            };
            about_item.Click += delegate
            {
                Controller.AboutClicked();
            };
            this.traymenu.Items.Add(about_item);

            // Create the exit menu.
            this.exitItem = new ToolStripMenuItem()
            {
                Text = CmisSync.Properties_Resources.Exit,
                Name = "exit"
            };
            this.exitItem.Click += delegate
            {
                this.trayicon.Dispose();
                Controller.QuitClicked();
            };
            this.traymenu.Items.Add(this.exitItem);
        }


        /// <summary>
        /// Create the animation frames from image files.
        /// </summary>
        private void CreateIcons()
        {
            this.animationFrames = new Icon[] {
	            UIHelpers.GetIcon ("process-syncing-i"),
	            UIHelpers.GetIcon ("process-syncing-ii"),
	            UIHelpers.GetIcon ("process-syncing-iii"),
	            UIHelpers.GetIcon ("process-syncing-iiii"),
	            UIHelpers.GetIcon ("process-syncing-iiiii")
			};
            this.errorIcon = UIHelpers.GetIcon("process-syncing-error");
        }


        /// <summary>
        /// Delegate for opening the local folder.
        /// </summary>
        private EventHandler OpenLocalFolderDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                Controller.LocalFolderClicked(repo);
            };
        }

        /// <summary>
        /// MouseEventListener function for opening the local folder.
        /// </summary>
        private void NotifyIcon1_MouseClick(Object sender, MouseEventArgs e)
        {
            //FIXME: make this work
            //if(e.Button == MouseButtons.Left)
            //    Controller.LocalFolderClicked();
        }

        /// <summary>
        /// MouseEventListener function for popping up context menu.
        /// </summary>
        private void TrayIcon_MouseClick(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(trayicon, null);
            }
        }


        /// <summary>
        /// Delegate for opening the remote folder.
        /// </summary>
        private EventHandler OpenRemoteFolderDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                Controller.RemoteFolderClicked(repo);
            };
        }


        /// <summary>
        /// Delegate for suspending sync.
        /// </summary>
        private EventHandler SuspendSyncFolderDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                Controller.SuspendSyncClicked(repo);
            };
        }

        private EventHandler RemoveFolderFromSyncDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                if (System.Windows.MessageBox.Show(
                    CmisSync.Properties_Resources.RemoveSyncQuestion,
                    CmisSync.Properties_Resources.RemoveSyncTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No
                    ) == MessageBoxResult.Yes)
                {
                    Controller.RemoveFolderFromSyncClicked(repo);
                }
            };
        }

        private EventHandler ManualSyncDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                Controller.ManualSyncClicked(repo);
            };
        }

        private EventHandler SettingsDelegate(Config.SyncConfig.LocalRepository repo)
        {
            return delegate
            {
                Controller.SettingsClicked(repo);
            };
        }
    }
}
