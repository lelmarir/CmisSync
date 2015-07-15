//   CmisSync, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

#if (__MonoCS__)
#define MonoBug24381 // https://bugzilla.xamarin.com/show_bug.cgi?id=24381
#endif

using CmisSync.Lib;
using CmisSync.Lib.Cmis;
using log4net;
using System;
using System.ComponentModel;
using System.Timers;

namespace CmisSync
{
    /// <summary>
    /// State of the CmisSync status icon.
    /// </summary>
    public enum IconState
    {
        /// <summary>
        /// Sync is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// Sync is running.
        /// </summary>
        Syncing,
        /// <summary>
        /// Sync is in error state.
        /// </summary>
        Error
    }

    /// <summary>
    /// MVC controller for the CmisSync status icon.
    /// </summary>
    public class StatusIconController
    {
        /// <summary>
        /// Log.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StatusIconController));

        /// <summary>
        /// Update icon event.
        /// </summary>
        public event UpdateIconEventHandler UpdateIconEvent = delegate { };

        /// <summary>
        /// Update icon event.
        /// </summary>
        public delegate void UpdateIconEventHandler(int icon_frame);

        /// <summary>
        /// Update menu event.
        /// </summary>
        public event UpdateMenuEventHandler UpdateMenuEvent = delegate { };

        /// <summary>
        /// Update menu event.
        /// </summary>
        public delegate void UpdateMenuEventHandler(IconState state);

        /// <summary>
        /// Update status event.
        /// </summary>
        public event UpdateStatusItemEventHandler UpdateStatusItemEvent = delegate { };

        /// <summary>
        /// Update status event.
        /// </summary>
        public delegate void UpdateStatusItemEventHandler(string state_text);

        /// <summary>
        /// Update suspended sync folder event.
        /// </summary>
        public event UpdateSuspendSyncFolderEventHandler UpdateSuspendSyncFolderEvent = delegate { };

        /// <summary>
        /// Update suspended sync folder event.
        /// </summary>
        public delegate void UpdateSuspendSyncFolderEventHandler(Config.SyncConfig.LocalRepository repo);

        /// <summary>
        /// 
        /// </summary>
        public event UpdateTransmissionMenuEventHandler UpdateTransmissionMenuEvent = delegate { };

        /// <summary>
        /// 
        /// </summary>
        public delegate void UpdateTransmissionMenuEventHandler();

        /// <summary>
        /// Current state of the CmisSync tray icon.
        /// </summary>
        public IconState CurrentState = IconState.Idle;

        /// <summary>
        /// Short text shown at the top of the menu of the CmisSync tray icon.
        /// </summary>
        public string StateText = Properties_Resources.Welcome;

        /// <summary>
        /// Maximum number of remote folders in the menu before the overflow menu appears.
        /// </summary>
        public readonly int MenuOverflowThreshold = 9;

        /// <summary>
        /// Minimum number of remote folders to populate the overflow menu.
        /// </summary>
        public readonly int MinSubmenuOverflowCount = 3;

        /// <summary>
        /// The list of remote folders to show in the CmisSync tray menu.
        /// </summary>
        public Config.SyncConfig.LocalRepository[] Folders
        {
            get
            {
                int overflow_count = (Program.Controller.LocalRepositories.Count - MenuOverflowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.LocalRepositories.GetRange(0, MenuOverflowThreshold).ToArray();
                else
                    return Program.Controller.LocalRepositories.ToArray();
            }
        }

        /// <summary>
        /// The list of remote folders to show in the CmisSync tray's overflow menu.
        /// </summary>
        public Config.SyncConfig.LocalRepository[] OverflowFolders
        {
            get
            {
                int overflow_count = (Program.Controller.LocalRepositories.Count - MenuOverflowThreshold);

                if (overflow_count >= MinSubmenuOverflowCount)
                    return Program.Controller.LocalRepositories.GetRange(MenuOverflowThreshold, overflow_count).ToArray();
                else
                    return new Config.SyncConfig.LocalRepository[0];
            }
        }

        /// <summary>
        /// Total disk space taken by the sum of the remote folders.
        /// </summary>
        public string FolderSize
        {
            get
            {
                double size = 0;

                foreach (RepoBase repo in Program.Controller.Repositories)
                    size += repo.Size;

                if (size == 0)
                    return "";
                else
                    return "— " + CmisSync.Lib.Utils.FormatSize(size);
            }
        }

        /// <summary>
        /// Timer for the animation that appears when downloading/uploading a file.
        /// </summary>
        private Timer animation;

        /// <summary>
        /// Current frame of the animation being shown.
        /// First frame is the still icon.
        /// </summary>
        private int animation_frame_number;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StatusIconController()
        {
            InitAnimation();

            // A remote folder has been added.
            Program.Controller.FolderListChanged += delegate
            {
                if (CurrentState != IconState.Error)
                {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.LocalRepositories.Count == 0)
                        StateText = Properties_Resources.Welcome;
                    else
                        StateText = Properties_Resources.FilesUpToDate;
                }

                UpdateStatusItemEvent(StateText);
                UpdateIconEvent(CurrentState == IconState.Error ? -1 : 0);
                UpdateMenuEvent(CurrentState);
            };

            // No more download/upload.
            Program.Controller.OnIdle += delegate
            {
                if (CurrentState != IconState.Error)
                {
                    CurrentState = IconState.Idle;

                    if (Program.Controller.LocalRepositories.Count == 0)
                        StateText = Properties_Resources.Welcome;
                    else
                        StateText = Properties_Resources.FilesUpToDate;
                }

                UpdateStatusItemEvent(StateText);

#if MonoBug24381
                //this.animation_frame_number = 0; // Idle status icon
                //UpdateIconEvent(this.animation_frame_number);
#else
                this.animation.Stop();
                UpdateIconEvent(CurrentState == IconState.Error ? -1 : 0);
#endif

                UpdateMenuEvent(CurrentState);
            };

            // Syncing.
            Program.Controller.OnSyncing += delegate
            {
                Logger.Debug("StatusIconController OnSyncing");
                CurrentState = IconState.Syncing;
                StateText = Properties_Resources.SyncingChanges;

                UpdateStatusItemEvent(StateText);

#if MonoBug24381
                //this.animation_frame_number = 3; // Syncing icon
                //UpdateIconEvent(this.animation_frame_number);
#else
                this.animation.Start();
#endif
            };

            // Error.
            Program.Controller.OnError += delegate(Config.SyncConfig.LocalRepository repo, Exception error)
            {
                Logger.Error(String.Format("Error syncing '{0}': {1}", repo, error.Message), error);

                string message = String.Format(Properties_Resources.SyncError, repo, error.Message);

                IconState PreviousState = CurrentState;
                CurrentState = IconState.Error;
                StateText = message;

                UpdateStatusItemEvent(StateText);

#if ! MonoBug24381
                this.animation.Stop();

                UpdateIconEvent(-1);
                UpdateMenuEvent(CurrentState);
#endif

                if (error is PermissionDeniedException)
                {
                    //FIXME: why it get suspended? Instead i should ask the user if the password has changed and he want to enter a new one
                    //Suspend sync...
                    SuspendSyncClicked(repo);
                }

                if (PreviousState != IconState.Error)
                {
                    Program.Controller.ShowAlert(Properties_Resources.Error, message);
                }
            };

            Program.Controller.OnErrorResolved += delegate
            {
                CurrentState = IconState.Idle;
            };
        }

        /// <summary>
        /// With the local file explorer, open the folder where the local synchronized folders are.
        /// </summary>
        public void LocalFolderClicked(Config.SyncConfig.LocalRepository repo)
        {
            Program.Controller.OpenCmisSyncFolder(repo);
        }

        /// <summary>
        /// With the default web browser, open the remote folder of a CmisSync synchronized folder.
        /// </summary>
        public void RemoteFolderClicked(Config.SyncConfig.LocalRepository repo)
        {
            Program.Controller.OpenRemoteFolder(repo);
        }

        /// <summary>
        /// With the default web browser, open the remote folder of a CmisSync synchronized folder.
        /// </summary>
        public void SettingsClicked(Config.SyncConfig.LocalRepository repo)
        {
            Program.UI.Setup.Controller.saved_repository = repo.DisplayName;
            if (repo != null)
            {
                Program.UI.Setup.Controller.saved_user = repo.Account.Credentials.UserName;
                Program.UI.Setup.Controller.saved_remote_path = repo.RemotePath;
                Program.UI.Setup.Controller.saved_address = repo.Account.RemoteUrl;
                Program.UI.Setup.Controller.saved_sync_interval = repo.PollInterval;
                Program.UI.Setup.Controller.saved_syncatstartup = repo.SyncAtStartup;
            }
            Program.Controller.ShowSetupWindow(PageType.Settings);
        }

        /// <summary>
        /// Open the remote folder addition wizard.
        /// </summary>
        public void AddRemoteFolderClicked()
        {
            Program.Controller.ShowSetupWindow(PageType.Add1);
        }

        /// <summary>
        /// Open the CmisSync log with a text file viewer.
        /// </summary>
        public void LogClicked()
        {
            Program.Controller.ShowLog(ConfigManager.CurrentConfig.GetLogFilePath());
        }

        /// <summary>
        /// Show the About dialog.
        /// </summary>
        public void AboutClicked()
        {
            Program.Controller.ShowAboutWindow();
        }

        /// <summary>
        /// Quit CmisSync.
        /// </summary>
        public void QuitClicked()
        {
            Program.Controller.Quit();
        }

        /// <summary>
        /// Suspend synchronization for a particular folder.
        /// </summary>
        public void SuspendSyncClicked(Config.SyncConfig.LocalRepository repo)
        {
            Program.Controller.SuspendOrResumeRepositorySynchronization(repo);
            //TODO: the StatusIcon should listen the controlleo or the repository for Suspended changes instead of call UpdateSuspendSyncFolderEvent
            UpdateSuspendSyncFolderEvent(repo);
        }

        /// <summary>
        /// Tries to remove a given repo from sync
        /// </summary>
        public void RemoveFolderFromSyncClicked(Config.SyncConfig.LocalRepository repo)
        {
            #if __COCOA__
            Program.Controller.RemoveRepositoryFromSync(reponame);
            #else
            System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                Properties_Resources.RemoveFolderFromSyncConfirm,
                Properties_Resources.RemoveFolderFromSync,
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question);

            // If the yes button was pressed ...
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                //Run this in background so as not to free the GUI...
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(
                    delegate(Object o, DoWorkEventArgs args)
                    {
                        Program.Controller.RemoveRepositoryFromSync((Config.SyncConfig.LocalRepository)args.Argument);
                    }
                );
                worker.RunWorkerAsync(repo);
            }
            #endif
        }

        /// <summary>
        /// Manual sync clicked.
        /// </summary>
        public void ManualSyncClicked(Config.SyncConfig.LocalRepository repo)
        {
            Program.Controller.ManualSync(repo);
        }

        /// <summary>
        /// Start the tray icon animation.
        /// </summary>
        private void InitAnimation()
        {
#if MonoBug24381

            //this.animation_frame_number = 3; // Syncing icon
            //UpdateIconEvent(this.animation_frame_number);
#else
            this.animation_frame_number = 0;

            this.animation = new Timer()
            {
                Interval = 200
            };

            this.animation.Elapsed += delegate
            {
                //Logger.Debug("StatusIconController Animation Elapsed " + this.animation_frame_number);
                if (this.animation_frame_number < 4)
                    this.animation_frame_number++;
                else
                    this.animation_frame_number = 0;

                UpdateIconEvent(this.animation_frame_number);
            };
#endif
        }
    }
}
