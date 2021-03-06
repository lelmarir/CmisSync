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


using log4net;
using System;
using System.IO;
using Timers = System.Timers;
using CmisSync.Lib.Auth;
using DotCMIS.Exceptions;
using CmisSync.Lib.Cmis;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using CmisSync.Lib.Sync;
using System.Windows;
using System.Windows.Threading;

namespace CmisSync.Lib.Sync
{

    public enum SyncMode
    {
        FULL, PARTIAL
    }

    /// <summary>
    /// Current status of the synchronization.
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// Still not started nor configured
        /// </summary>
        Init = -1,
        /// <summary>
        /// Normal operation.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Going to Syncing but still blocked by something (probably by a signal)
        /// </summary>
        Starting = 1,

        /// <summary>
        /// All going, currently syncing
        /// </summary>
        Syncing = 2,

        /// <summary>
        /// Cancellation requested, waiting for the nest check point
        /// </summary>
        Cancelling = 3,

        /// <summary>
        /// Synchronization is suspended.
        /// </summary>
        Suspended = 5
    }

    /// <summary>
    /// Synchronizes a remote folder.
    /// This class contains the loop that synchronizes every X seconds.
    /// </summary>
    public abstract class SyncFolderSyncronizerBase : ModelBase, IDisposable
    {
        /// <summary>
        /// Log.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SyncFolderSyncronizerBase));

        /// <summary>
        /// Background worker for sync.
        /// </summary>
        private BackgroundWorker syncWorker;

        /// <summary>
        /// Current status of the synchronization (paused or not).
        /// </summary>
        private SyncStatus _status = SyncStatus.Init;
        public SyncStatus Status
        {
            get { return _status; }
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }

            }
        }
        
        /// <summary>
        /// Return the synchronized folder's information.
        /// </summary>
        public Config.SyncConfig.SyncFolder SyncFolderInfo { get; set; }

        /// <summary>
        /// Watches the local filesystem for changes.
        /// </summary>
        protected Watcher watcher { get; private set; }

        /// <summary>
        /// Timer for watching the local and remote filesystems.
        /// </summary>
        private Timers.Timer remote_timer = new Timers.Timer();

        /// <summary>
        /// Event to notify that the sync has completed.
        /// </summary>
        private AutoResetEvent syncAutoResetEvent = new AutoResetEvent(true);
        
        /// <summary>
        /// When the last full sync completed.
        /// </summary>
        private DateTime last_sync;

        /// <summary>
        /// When the last partial sync completed.
        /// </summary>
        private DateTime last_partial_sync;

        /// <summary>
        /// Track whether <c>Dispose</c> has been called.
        /// </summary>
        private bool disposed = false;

        //public EventsObservableCollection Events { get; private set; }

        public delegate void SyncronizerEventHandler(SyncronizerEvent e);
        public event SyncronizerEventHandler Event;

        protected virtual void OnEvent(SyncronizerEvent e)
        {
            SyncronizerEventHandler handler = Event;
            if (handler != null)
            {
                handler(e);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SyncFolderSyncronizerBase(Config.SyncConfig.SyncFolder syncFolderInfo)
        {
            if (null == syncFolderInfo)
            {
                throw new ArgumentNullException("syncFolderInfo");
            }

            this.SyncFolderInfo = syncFolderInfo;
            configure();
            this.SyncFolderInfo.PropertyChanged += syncFolderInfo_propertyChanged;

            // Folder lock.
            // Disabled for now. Can be an interesting feature, but should be made opt-in, as
            // most users would be surprised to see this file appear.
            // folderLock = new FolderLock(LocalPath);


            // Main loop syncing every X seconds.
            remote_timer.Elapsed += delegate
            {
                // Synchronize.
                SyncInBackground();
            };
            remote_timer.AutoReset = true;

            syncWorker = new BackgroundWorker();
            syncWorker.WorkerSupportsCancellation = true;
            syncWorker.DoWork += new DoWorkEventHandler(
                delegate(Object o, DoWorkEventArgs args)
                {
                    SyncMode syncMode = (SyncMode)args.Argument;
                    Sync(syncMode);
                }
            );
        }

        protected virtual void configure()
        {
            if (this.Status != SyncStatus.Init && this.Status != SyncStatus.Suspended)
            {
                throw new InvalidOperationException();
            }

            this.remote_timer.Interval = SyncFolderInfo.PollInterval;

            if (watcher != null)
            {
                watcher.Dispose();
            }
            watcher = new Watcher(SyncFolderInfo.LocalPath);
            watcher.ChangeEvent += OnFileActivity;

            Logger.Info("Repo " + SyncFolderInfo.DisplayName + " - Set poll interval to " + SyncFolderInfo.PollInterval + "ms");
            remote_timer.Interval = SyncFolderInfo.PollInterval;

            if (this.Status == SyncStatus.Init)
            {
                if (this.SyncFolderInfo.IsSuspended)
                {
                    Status = SyncStatus.Suspended;
                }
                else
                {
                    Status = SyncStatus.Idle;
                }
            }
        }

        private void syncFolderInfo_propertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                CancelSyncronization(false);
                Suspend(false);
                configure();
                Resume();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();            
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~SyncFolderSyncronizerBase()
        {
            Dispose(false);
        }


        /// <summary>
        /// Implement IDisposable interface. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Dispose pattern implementation.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.remote_timer.Stop();
                    this.remote_timer.Dispose();
                    this.watcher.Dispose();
                    //this.folderLock.Dispose();
                }
                this.disposed = true;
            }
        }

        public void Initialize()
        {
            // Sync up everything that changed
            // since we've been offline
            if (SyncFolderInfo.SyncAtStartup)
            {
                SyncInBackground();
                Logger.Info(String.Format("Repo {0} - sync launch at startup", SyncFolderInfo.DisplayName));
            }
            else
            {
                Logger.Info(String.Format("Repo {0} - sync not launch at startup", SyncFolderInfo.DisplayName));
                // if LastSuccessSync + pollInterval >= DateTime.Now => Sync
                DateTime tm = SyncFolderInfo.LastSuccessedSync.AddMilliseconds(SyncFolderInfo.PollInterval);
                // http://msdn.microsoft.com/fr-fr/library/system.datetime.compare(v=vs.110).aspx
                if (DateTime.Compare(DateTime.Now, tm) >= 0)
                {
                    SyncInBackground();
                    Logger.Info(String.Format("Repo {0} - sync launch based on last success time sync + poll interval", SyncFolderInfo.DisplayName));
                }
                else
                {
                    Logger.Info(String.Format("Repo {0} - sync not launch based on last success time sync + poll interval - Next sync at {1}", SyncFolderInfo.DisplayName, tm));
                }
            }
        }

        /// <summary>
        /// Synchronize between CMIS folder and local folder.
        /// </summary>
        public void Sync()
        {
            Sync(SyncMode.FULL);
        }

        /// <summary>
        /// Synchronize between CMIS folder and local folder.
        /// </summary>
        public void Sync(SyncMode syncMode)
        {
            if (Status == SyncStatus.Syncing)
            {
                Logger.Debug(SyncFolderInfo.DisplayName + ": sync in progress, cannot start again");
                return;
            }

            Logger.Debug(SyncFolderInfo.DisplayName + ": syncWorker.DoWork(syncMode=" + syncMode + ")");
            if (this.Status == SyncStatus.Idle)
            {
                Status = SyncStatus.Syncing;
                syncAutoResetEvent.Reset();
                Logger.Info((syncMode == SyncMode.FULL ? "Full" : "Partial") + " Sync Started: " + SyncFolderInfo.LocalPath);
                if (syncMode == SyncMode.FULL)
                {
                    remote_timer.Stop();
                }
                watcher.Enable = false;//Disable events while syncing...

                OnEvent(new SyncronizationStarted(this));

                try
                {
                    doSync(syncMode);
                }
                catch (OperationCanceledException e)
                {
                    Logger.Info("OperationCanceled: " + e.Message);
                    if(Status != SyncStatus.Suspended){
                        Status = SyncStatus.Idle;
                    }                     
                }
                catch (CmisPermissionDeniedException e)
                {
                    NotifySyncException(EventLevel.ERROR, e);
                }
                catch (MissingRootSyncFolderException e)
                {
                    NotifySyncException(EventLevel.ERROR, e);
                }
                catch (CmisConnectionException e)
                {
                    if (e.Message.StartsWith("Cannot access"))
                    {
                        NotifySyncException(EventLevel.ERROR, new NetworkException(e));
                        //probably a network error (or the network cable disconnected)
                    }
                    else
                    {
                        NotifySyncException(EventLevel.ERROR, e);
                    }
                }
                catch (UnhandledException e)
                {
                    NotifySyncException(EventLevel.ERROR, e);
                }
                catch (Exception e)
                {
                    NotifySyncException(EventLevel.ERROR, new UnhandledException(e));
                }
                finally
                {
                    if (syncMode == SyncMode.FULL)
                    {
                        remote_timer.Start();
                        last_sync = DateTime.Now;
                    }
                    else
                    {
                        last_partial_sync = DateTime.Now;
                    }
                    if (watcher.GetChangeCount() > 0)
                    {
                        //Watcher was stopped (due to error) so clear and restart sync
                        watcher.Clear();
                    }

                    watcher.Enable = true;
                    Logger.Info((syncMode == SyncMode.FULL ? "Full" : "Partial") + " Sync Completed: " + SyncFolderInfo.LocalPath);

                    // Save last sync
                    SyncFolderInfo.LastSuccessedSync = DateTime.Now;
                    ConfigManager.CurrentConfig.Save();

                    syncAutoResetEvent.Set();
                    if (Status != SyncStatus.Suspended) {
                        Status = SyncStatus.Idle;
                    }

                    //TODO: signal if the syncronization has ended normally or has been interrupted
                    OnEvent(new SyncronizationComleted(this));
                }
            }
            else
            {
                Logger.Info(String.Format("Repo {0} - Sync skipped.Status={1}", this.SyncFolderInfo.DisplayName, this.Status));
            }
        }

        protected abstract void doSync(SyncMode syncMode);

        /// <summary>
        /// Tell if the sync is in progress (even if the Status is Suspended the the sync thread may sill run)
        /// </summary>
        public bool IsSyncingInProgress()
        {
            return syncWorker.IsBusy;
        }

        /// <summary>
        /// Synchronize.
        /// The synchronization is performed in the background, so that the UI stays usable.
        /// </summary>
        public void SyncInBackground()
        {
            SyncInBackground(SyncMode.FULL);
        }

        /// <summary>
        /// Sync in the background.
        /// </summary>
        public void SyncInBackground(SyncMode syncMode)
        {
            if (this.Status == SyncStatus.Idle)
            {
                if (IsSyncingInProgress())
                {
                    Logger.Debug("Sync already running in background: " + SyncFolderInfo.LocalPath);
                    return;
                }

                syncWorker.RunWorkerAsync(syncMode);
            }
            else
            {
                Logger.Info(String.Format("Repo {0} - Sync skipped. Status={1}", this.SyncFolderInfo.DisplayName, this.Status));
            }
        }

        /// <summary>
        /// Will send message the currently running sync thread (if one exists) to stop syncing as soon as the next
        /// blockign operation completes.
        /// </summary>
        public void CancelSyncronization() { CancelSyncronization(true); }

        /// <summary>
        /// Will send message the currently running sync thread (if one exists) to stop syncing as soon as the next
        /// blockign operation completes.
        /// <param name="async">if the method should return immediatly or wait for the sync thread to stop</param>
        /// </summary>
        public void CancelSyncronization(bool async)
        {
            if (IsSyncingInProgress())
            {
                Logger.Info("Cancel Sync Requested...");
                Status = SyncStatus.Cancelling;
                syncWorker.CancelAsync();
                if (async == false)
                {
                    Logger.Debug("Wait for thread to complete...");
                    syncAutoResetEvent.WaitOne();
                    Logger.Debug("...cancel completed.");
                }
            }
        }

        public void Suspend()
        {
            Suspend(true);
        }

        /// <summary>
        /// Stop syncing and avoid any further sync until resumed.
        /// </summary>
        public void Suspend(bool persist)
        {
            if (Status != SyncStatus.Suspended)
            {
                this.remote_timer.Stop();
                watcher.Enable = false;
                CancelSyncronization();
                if (persist)
                {
                    //Get configuration
                    SyncFolderInfo.IsSuspended = true;
                    ConfigManager.CurrentConfig.Save();
                }
                Status = SyncStatus.Suspended;
            }
        }

        /// <summary>
        /// Restart syncing.
        /// </summary>
        public virtual void Resume()
        {
            if (Status == SyncStatus.Suspended)
            {
                this.remote_timer.Start();
                watcher.Enable = true;  
                if (!IsSyncingInProgress())
                {                                      
                    Status = SyncStatus.Idle;
                    //sync now
                    SyncInBackground();
                }
                else
                {
                    //the sync is still in progress (yet not reached any cancellation check point)
                    Status = SyncStatus.Syncing;
                    //it should reach the check point, find all clear and go on
                }
            }

            SyncFolderInfo.IsSuspended = false;
            ConfigManager.CurrentConfig.Save();
        }

        /// <summary>
        /// Sleep while suspended.
        /// </summary>
        protected void CheckPendingCancelation()
        {
            if (syncWorker.CancellationPending)
            {
                //Sync was cancelled...
                throw new OperationCanceledException("Sync was cancelled by user.");
            }
        }

        /// <summary>
        /// Some file activity has been detected, sync changes.
        /// </summary>
        protected void OnFileActivity(object sender, EventArgs args)
        {
            // Run partial sync.
            SyncInBackground(SyncMode.PARTIAL);
        }

        /// <summary>
        /// Called when sync encounters a critical error (the syncronization has been stopped).
        /// </summary>
        protected void NotifySyncException(EventLevel level, CmisBaseException exception)
        {
            if (level == EventLevel.ERROR)
            {
                Logger.Error("Sync event (" + level + "): " + exception.GetType() + ", " + exception.Message, exception);
            }
            else
            {
                Logger.Info("Sync event (" + level + "): " + exception.GetType() + ", " + exception.Message);
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                OnEvent(new SyncronizationException(this, exception, level));
            }));
        }
    }
}
