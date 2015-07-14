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


using CmisSync.Lib;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;

using CmisSync.Lib;
using CmisSync.Lib.Cmis;
using CmisSync.Lib.Events;
using CmisSync.Auth;

using System.Windows.Forms;

#if __COCOA__
// using Edit = CmisSync.EditWizardController;
#endif

namespace CmisSync
{
    /// <summary>
    /// Platform-independant part of the main CmisSync controller.
    /// </summary>
    public abstract class ControllerBase : IActivityListener
    {
        /// <summary>
        /// Log.
        /// </summary>
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(ControllerBase));

        /// <summary>
        /// Whether it is the first time that CmisSync is being run.
        /// </summary>
        private bool firstRun;

        /// <summary>
        /// All the info about the CmisSync synchronized folder being created.
        /// </summary>
        private Config.SyncConfig.LocalRepository repoInfo;

        /// <summary>
        /// Whether the repositories have finished loading.
        /// </summary>
        public bool RepositoriesLoaded { get; private set; }

        /// <summary>
        /// List of the CmisSync synchronized folders.
        /// </summary>
        private List<RepoBase> repositories = new List<RepoBase>();

        /// <summary>
        /// Path where the CmisSync synchronized folders are by default.
        /// </summary>
        public string FoldersPath { get; private set; }

        /// <summary>
        /// Show setup window event.
        /// </summary>
        public event ShowSetupWindowEventHandler ShowSetupWindowEvent = delegate { };

        /// <summary>
        /// Show setup window event.
        /// </summary>
        public delegate void ShowSetupWindowEventHandler(PageType page_type);

        /// <summary>
        /// Show about window event.
        /// </summary>
        public event Action ShowAboutWindowEvent = delegate { };

        /// <summary>
        /// Folder list changed.
        /// </summary>
        public event Action FolderListChanged = delegate { };

        /// <summary>
        /// Called with status changes to idle.
        /// </summary>
        public event Action OnIdle = delegate { };

        /// <summary>
        /// Called with status changes to syncing.
        /// </summary>
        public event Action OnSyncing = delegate { };

        /// <summary>
        /// Called with status changes to error.
        /// </summary>
        public event Action<Config.SyncConfig.LocalRepository, Exception> OnError = delegate { };

        /// <summary>
        /// Called with status changes to error resolved.
        /// </summary>
        public event Action OnErrorResolved = delegate { };

        /// <summary>
        /// Alert notification.
        /// </summary>
        public event AlertNotificationRaisedEventHandler AlertNotificationRaised = delegate { };

        /// <summary>
        /// Alert notification.
        /// </summary>
        public delegate void AlertNotificationRaisedEventHandler(string title, string message);

        /// <summary>
        /// Get the repositories configured in CmisSync.
        /// </summary>
        public RepoBase[] Repositories
        {
            get
            {
                lock (this.repo_lock)
                    return this.repositories.GetRange(0, this.repositories.Count).ToArray();
            }
        }

        /// <summary>
        /// Whether it is the first time that CmisSync is being run.
        /// </summary>
        public bool FirstRun
        {
            get
            {
                return firstRun;
            }
        }

        /// <summary>
        /// The list of synchronized folders.
        /// </summary>
        public List<Config.SyncConfig.LocalRepository> LocalRepositories
        {
            get
            {
                List<Config.SyncConfig.LocalRepository> localRepositories = new List<Config.SyncConfig.LocalRepository>();
                foreach (Config.SyncConfig.LocalRepository rep in ConfigManager.CurrentConfig.LocalRepositories)
                {
                    localRepositories.Add(rep);
                }
                localRepositories.OrderBy(r => r.DisplayName).ToList();

                return localRepositories;
            }
        }

        /// <summary>
        /// Add CmisSync to the list of programs to be started up when the user logs into Windows.
        /// </summary>
        public abstract void CreateStartupItem();

        /// <summary>
        /// Add CmisSync to the user's Windows Explorer bookmarks.
        /// </summary>
        public abstract void AddToBookmarks();

        /// <summary>
        /// Creates the CmisSync folder in the user's home folder.
        /// </summary>
        public abstract bool CreateCmisSyncFolder();

        /// <summary>
        /// Keeps track of whether a download or upload is going on, for display of the task bar animation.
        /// </summary>
        private IActivityListener activityListenerAggregator;


        /// <summary>
        /// A folder lock for the base directory.
        /// </summary>
        private FolderLock folderLock;

        /// <summary>
        /// Concurrency locks.
        /// </summary>
        private Object repo_lock = new Object();

        /// <summary>
        /// Constructor.
        /// </summary>
        public ControllerBase()
        {
            activityListenerAggregator = new ActivityListenerAggregator(this);
        }

        /// <summary>
        /// Initialize the controller.
        /// </summary>
        /// <param name="firstRun">Whether it is the first time that CmisSync is being run.</param>
        public virtual void Initialize(Boolean firstRun)
        {
            this.firstRun = firstRun;

            FoldersPath = ConfigManager.CurrentConfig.DefaultRepositoryRootFolderPath;

            // Create the CmisSync folder and add it to the bookmarks
            bool syncFolderCreated = CreateCmisSyncFolder();

            if (syncFolderCreated)
            {
                AddToBookmarks();
            }

            if (firstRun)
            {
                ConfigManager.CurrentConfig.Notifications = true;
            }

            folderLock = new FolderLock(FoldersPath);
        }

        /// <summary>
        /// Once the GUI has loaded, show setup window if it is the first run, or check the repositories.
        /// </summary>
        public void UIHasLoaded()
        {
            if (firstRun)
            {
                ShowSetupWindow(PageType.Setup);
            }

            Thread t = new Thread(() =>
            {
                CheckRepositories();
                RepositoriesLoaded = true;
                // Update GUI.
                FolderListChanged();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        /// <summary>
        /// Initialize (in the GUI and syncing mechanism) an existing CmisSync synchronized folder.
        /// </summary>
        /// <param name="repositoryInfo">Synchronized folder path</param>
        private void AddRepository(Config.SyncConfig.LocalRepository repositoryInfo)
        {
            RepoBase repo = null;
            repo = new CmisSync.Lib.Sync.CmisRepo(repositoryInfo, activityListenerAggregator);
            this.repositories.Add(repo);
            repo.Initialize();
        }

        /// <summary>
        /// Update settings for repository.
        /// </summary>
        public void UpdateRepositorySettings(string repoName, string password, int pollInterval, bool syncAtStartup)
        {
            foreach (RepoBase repoBase in this.repositories)
            {
                if (repoBase.RepoInfo.DisplayName == repoName)
                {
                    repoBase.UpdateSettings(password, pollInterval, syncAtStartup);
                    OnErrorResolved();
                    FolderListChanged();
                }
            }
        }

        /// <summary>
        /// Remove repository from sync.
        /// </summary>
        public void RemoveRepositoryFromSync(Config.SyncConfig.LocalRepository repo)
        {
            if (repo != null)
            {
                RemoveRepository(repo);
                ConfigManager.CurrentConfig.RemoveFolder(repo);
                FolderListChanged();
            }
            else
            {
                Logger.Warn("Reponame \"" + repo + "\" could not be found: Removing Repository failed");
            }
        }

        /// <summary>
        /// Run a sync manually.
        /// </summary>
        public void ManualSync(Config.SyncConfig.LocalRepository repo)
        {
            foreach (RepoBase aRepo in this.repositories)
            {
                if (aRepo.RepoInfo == repo && aRepo.Status == SyncStatus.Idle)
                {

                    aRepo.ManualSync();
                    Logger.Debug("Requested to manually sync " + aRepo.RepoInfo.DisplayName);
                }
            }
        }

        /// <summary>
        /// Remove a synchronized folder from the CmisSync configuration.
        /// This happens after the user removes the folder.
        /// </summary>
        /// <param name="folder">The synchronized folder to remove</param>
        private void RemoveRepository(Config.SyncConfig.LocalRepository folder)
        {
            foreach (RepoBase repo in this.repositories)
            {
                if (repo.RepoInfo.LocalPath.Equals(folder.LocalPath))
                {
                    repo.CancelSync();
                    repo.Dispose();
                    this.repositories.Remove(repo);
                    Logger.Info("Removed Repository: " + repo.RepoInfo);
                    break;
                }
            }

            // Remove Cmis Database File
            string dbfilename = folder.DisplayName;
            dbfilename = dbfilename.Replace("\\", "_");
            dbfilename = dbfilename.Replace("/", "_");
            RemoveDatabase(dbfilename);
        }

        /// <summary>
        /// Remove the local database associated with a CmisSync synchronized folder.
        /// </summary>
        /// <param name="folder_path">The synchronized folder whose database is to be removed</param>
        private void RemoveDatabase(string folder_path)
        {
            string databasefile = Path.Combine(ConfigManager.CurrentConfig.ConfigPath, Path.GetFileName(folder_path) + ".cmissync");
            if (File.Exists(databasefile))
            {
                File.Delete(databasefile);
                Logger.Info("Removed database: " + databasefile);
            }
        }

        /// <summary>
        /// Pause or un-pause synchronization for a particular folder.
        /// </summary>
        /// <param name="repo">the folder to pause/unpause</param>
        public void SuspendOrResumeRepositorySynchronization(Config.SyncConfig.LocalRepository repo)
        {
            lock (this.repo_lock)
            {
                //FIXME: why are we sospendig all repositories instead of the one passed?
                foreach (RepoBase aRepo in this.repositories)
                {
                    if (aRepo.Status != SyncStatus.Suspend)
                    {
                        SuspendRepositorySynchronization(repo);
                    }
                    else
                    {
                        ResumeRepositorySynchronization(repo);
                    }
                }
            }
        }

        /// <summary>
        /// Pause synchronization for a particular folder.
        /// </summary>
        /// <param name="repo">the folder to pause</param>
        public void SuspendRepositorySynchronization(Config.SyncConfig.LocalRepository repo)
        {
            lock (this.repo_lock)
            {
                //FIXME: why are we sospendig all repositories instead of the one passed?
                foreach (RepoBase aRepo in this.repositories)
                {
                    if (aRepo.Status != SyncStatus.Suspend)
                    {
                        aRepo.Suspend();
                        Logger.Debug("Requested to suspend sync of repo " + aRepo.RepoInfo.DisplayName);
                    }
                }
            }
        }

        /// <summary>
        /// Un-pause synchronization for a particular folder.
        /// </summary>
        /// <param name="repo">the folder to unpause</param>
        public void ResumeRepositorySynchronization(Config.SyncConfig.LocalRepository repo)
        {
            lock (this.repo_lock)
            {
                //FIXME: why are we sospendig all repositories instead of the one passed?
                foreach (RepoBase aRepo in this.repositories)
                {
                    if (aRepo.Status == SyncStatus.Suspend)
                    {
                        aRepo.Resume();
                        Logger.Debug("Requested to resume sync of repo " + aRepo.RepoInfo.DisplayName);
                    }
                }
            }
        }

        /// <summary>
        /// Check the configured CmisSync synchronized folders.
        /// </summary>
        private void CheckRepositories()
        {
            lock (this.repo_lock)
            {
                Queue<Config.SyncConfig.LocalRepository> missingFolders = new Queue<Config.SyncConfig.LocalRepository>();
                foreach (Config.SyncConfig.LocalRepository repo in ConfigManager.CurrentConfig.LocalRepositories)
                {
                    string folder_path = repo.LocalPath;

                    if (!Directory.Exists(folder_path))
                    {
                        // If folder has been deleted, ask the user what to do.
                        Logger.Info("ControllerBase | Found missing folder '" + repo.DisplayName + "'");
                        missingFolders.Enqueue(repo);
                    }
                    else
                    {
                        AddRepository(repo);
                    }
                }

                while (missingFolders.Count != 0)
                {
                    handleMissingSyncFolder(missingFolders.Dequeue());
                }

                ConfigManager.CurrentConfig.Save();
            }

            // Update GUI.
            FolderListChanged();
        }

        private void handleMissingSyncFolder(Config.SyncConfig.LocalRepository repo)
        {
            bool handled = false;

            while (handled == false)
            {
                MissingFolderDialog dialog = new MissingFolderDialog(repo);
                dialog.ShowDialog();
                if (dialog.action == MissingFolderDialog.Action.MOVE)
                {
                    String startPath = Directory.GetParent(repo.LocalPath).FullName;
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    fbd.SelectedPath = startPath;
                    fbd.Description = "Select the folder you have moved or renamed";
                    fbd.ShowNewFolderButton = false;
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && fbd.SelectedPath.Length > 0)
                    {
                        if (!Directory.Exists(fbd.SelectedPath))
                        {
                            throw new InvalidDataException();
                        }
                        Logger.Info("ControllerBase | Folder '" + repo.DisplayName + "' ('" + repo.LocalPath + "') moved to '" + fbd.SelectedPath + "'");
                        repo.LocalPath = fbd.SelectedPath;

                        AddRepository(repo);
                        handled = true;
                    }
                }
                else if (dialog.action == MissingFolderDialog.Action.REMOVE)
                {
                    RemoveRepository(repo);
                    ConfigManager.CurrentConfig.LocalRepositories.Remove(repo);

                    Logger.Info("ControllerBase | Removed folder '" + repo.DisplayName + "' from config");
                    handled = true;
                }
                else if (dialog.action == MissingFolderDialog.Action.RECREATE)
                {
                    RemoveRepository(repo);
                    ConfigManager.CurrentConfig.LocalRepositories.Remove(repo);
                    CreateRepository(repo);
                    Logger.Info("ControllerBase | Folder '" + repo.DisplayName + "' recreated");
                    handled = true;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            //handled == true
            //now resume the sincronization (if ever was suspended)
            //FIXME: the problem is that if the user suspended this repo it will get resumed anyway (ignoring the user setting)
            Program.Controller.ResumeRepositorySynchronization(repo);
        }

        /// <summary>
        /// Fix the file attributes of a folder, recursively.
        /// </summary>
        /// <param name="path">Folder to fix</param>
        private void ClearFolderAttributes(string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] folders = Directory.GetDirectories(path);

            foreach (string folder in folders)
                ClearFolderAttributes(folder);

            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
                if (!CmisSync.Lib.Utils.IsSymlink(file))
                    File.SetAttributes(file, FileAttributes.Normal);
        }

        /// <summary>
        /// Create a new CmisSync synchronized folder.
        /// </summary>
        public void CreateRepository(Config.SyncConfig.LocalRepository repoInfo)
        {
            //TODO: reset stats or others transient states

            // Add folder to XML config file.
            ConfigManager.CurrentConfig.AddLocalRepository(repoInfo);

            // Initialize in the GUI.
            AddRepository(repoInfo);
            FolderListChanged();
        }

        /// <summary>
        /// Show first-time wizard.
        /// </summary>
        public void ShowSetupWindow(PageType page_type)
        {
            ShowSetupWindowEvent(page_type);
        }

        /// <summary>
        /// Show info about CmisSync
        /// </summary>
        public void ShowAboutWindow()
        {
            ShowAboutWindowEvent();
        }

        /// <summary>
        /// Show an alert to the user.
        /// </summary>
        public void ShowAlert(string title, string message)
        {
            AlertNotificationRaised(Properties_Resources.CmisSync + " " + title, message);
        }

        /// <summary>
        /// Quit CmisSync.
        /// </summary>
        public virtual void Quit()
        {
            foreach (RepoBase repo in Repositories)
                repo.Dispose();

            folderLock.Dispose();

            Logger.Info("Exiting.");
            Environment.Exit(0);
        }

        /// <summary>
        /// A download or upload has started, so run task icon animation.
        /// </summary>
        public void ActivityStarted()
        {
            OnSyncing();
        }

        /// <summary>
        /// No download nor upload, so no task icon animation.
        /// </summary>
        public void ActivityStopped()
        {
            OnIdle();
        }

        /// <summary>
        /// Error occured.
        /// </summary>
        public void ActivityError(Config.SyncConfig.LocalRepository repo, Exception error)
        {
            if (error is MissingSyncFolderException)
            {
                //Suspend sync... (should be resumed after the user has handled the error)
                Program.Controller.SuspendRepositorySynchronization(repo);
                //FIXME: should update the suspended menu item, but i can't from here
                //UpdateSuspendSyncFolderEvent(reponame);
                
                //handle in a new thread, becouse this is the syncronization one and can be killed if the user decide to remove the repo or resync it
                Thread t = new Thread(() =>
                {
                    handleMissingSyncFolder(repo);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();

                //dont resume here, the handler thread will if needed (or kill this thread)
                //Program.Controller.ResumeRepositorySynchronization(reponame);

                //handled, no need to do anything else
                return;
            }

            OnError(repo, error);
        }
    }
}
