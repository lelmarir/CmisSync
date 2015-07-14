﻿using System;
using System.IO;
using CmisSync.Lib.Cmis;


namespace CmisSync.Lib
{
    /// <summary></summary>
    abstract public class SyncItem
    {
        /// <summary>local Repository root folder</summary>
        protected string localRoot;

        /// <summary>remote Repository root folder</summary>
        protected string remoteRoot;

        /// <summary>local relative path</summary>
        protected string localPath;

        /// <summary>remote relative path</summary>
        protected string remotePath;

        /// <summary></summary>
        protected SyncItem()
        {
        }

        /// <summary></summary>
        abstract public string LocalRelativePath
        {
            get;
        }

        /// <summary></summary>
        abstract public string RemoteRelativePath
        {
            get;
        }

        /// <summary></summary>
        abstract public string LocalPath
        {
            get;
        }

        /// <summary></summary>
        abstract public string RemotePath
        {
            get;
        }

        /// <summary></summary>
        abstract public string LocalFileName
        {
            get;
        }

        /// <summary></summary>
        abstract public string RemoteFileName
        {
            get;
        }

        /// <summary></summary>
        /// <returns></returns>
        virtual public bool ExistsLocal()
        {
            return File.Exists(LocalPath);
        }
    }

    /// <summary></summary>
    public static class SyncItemFactory
    {
        /// <summary></summary>
        /// <param name="path"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromLocalPath(string path, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new LocalPathSyncItem(path, repoInfo);
        }

        /// <summary></summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromLocalPath(string folder, string fileName, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new LocalPathSyncItem(Path.Combine(folder, fileName), repoInfo);
        }

        /// <summary></summary>
        /// <param name="path"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromRemotePath(string path, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new RemotePathSyncItem(path, repoInfo);
        }

        /// <summary></summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromRemotePath(string folder, string fileName, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new RemotePathSyncItem(Path.Combine(folder, fileName), repoInfo);
        }

        /// <summary></summary>
        /// <param name="localFolder"></param>
        /// <param name="remoteFileName"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromLocalFolderAndRemoteName(string localFolder, string remoteFileName, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new LocalPathSyncItem(localFolder, remoteFileName, repoInfo);
        }

        /// <summary></summary>
        /// <param name="remoteFolder"></param>
        /// <param name="LocalFileName"></param>
        /// <param name="repoInfo"></param>
        /// <returns></returns>
        public static SyncItem CreateFromRemoteFolderAndLocalName(string remoteFolder, string LocalFileName, Config.SyncConfig.LocalRepository repoInfo)
        {
            return new RemotePathSyncItem(remoteFolder, LocalFileName, repoInfo);
        }

        /// <summary></summary>
        /// <param name="localPathPrefix"></param>
        /// <param name="localPath"></param>
        /// <param name="remotePathPrefix"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public static SyncItem CreateFromPaths(string localPathPrefix, string localPath, string remotePathPrefix, string remotePath)
        {
            return new LocalPathSyncItem(localPathPrefix, localPath, remotePathPrefix, remotePath);
        }
    }
               
    /// <summary></summary>
    public class LocalPathSyncItem : SyncItem
    {
        /// <summary></summary>
        /// <param name="localPath"></param>
        /// <param name="repoInfo"></param>
        public LocalPathSyncItem(string localPath, Config.SyncConfig.LocalRepository repoInfo)
        {
            this.localRoot = repoInfo.LocalPath;
            this.remoteRoot = repoInfo.RemotePath;

            this.localPath = localPath;
            if (localPath.StartsWith(this.localRoot))
            {
                this.localPath = localPath.Substring(localRoot.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            this.remotePath = PathRepresentationConverter.LocalToRemote(this.localPath);
        }

        /// <summary></summary>
        /// <param name="localFolder"></param>
        /// <param name="remoteRelativePath"></param>
        /// <param name="repoInfo"></param>
        public LocalPathSyncItem(string localFolder, string remoteRelativePath, Config.SyncConfig.LocalRepository repoInfo)
        {
            this.localRoot = repoInfo.LocalPath;
            this.remoteRoot = repoInfo.RemotePath;

            this.localPath = Path.Combine(localFolder, PathRepresentationConverter.RemoteToLocal(remoteRelativePath));
            if (localPath.StartsWith(this.localRoot))
            {
                this.localPath = localPath.Substring(localRoot.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            string localRootRelative = localFolder;
            if (localFolder.StartsWith(this.localRoot))
            {
                localRootRelative = localFolder.Substring(localRoot.Length).TrimStart(CmisPath.CMIS_FILE_SEPARATOR);
            }
            this.remotePath = CmisPath.Combine(PathRepresentationConverter.LocalToRemote(localRootRelative), remoteRelativePath);
        }

        /// <summary></summary>
        /// <param name="localPrefix"></param>
        /// <param name="localPath"></param>
        /// <param name="remotePrefix"></param>
        /// <param name="remotePath"></param>
        public LocalPathSyncItem(string localPrefix, string localPath, string remotePrefix, string remotePath)
        {
            this.localRoot = localPrefix;
            this.remoteRoot = remotePrefix;
            this.localPath = localPath;
            this.remotePath = remotePath;
        }
            
        /// <summary></summary>
        public override string LocalRelativePath
        {
            get
            {
                return localPath;
            }
        }

        /// <summary></summary>
        public override string RemoteRelativePath
        {
            get
            {
                return remotePath;
            }
        }

        /// <summary></summary>
        public override string LocalPath
        {
            get
            {
                return Path.Combine(this.localRoot, this.localPath);
            }
        }

        /// <summary></summary>
        public override string RemotePath
        {
            get
            {
                return Path.Combine(this.remoteRoot, this.remotePath);
            }
        }

        /// <summary></summary>
        public override string LocalFileName
        {
            get
            {
                return Path.GetFileName(this.localPath);
            }
        }

        /// <summary></summary>
        public override string RemoteFileName
        {
            get
            {
                return Path.GetFileName(this.remotePath);
            }
        }
    }

    /// <summary></summary>
    public class RemotePathSyncItem : SyncItem
    {
        /// <summary></summary>
        /// <param name="remotePath"></param>
        /// <param name="repoInfo"></param>
        public RemotePathSyncItem(string remotePath, Config.SyncConfig.LocalRepository repoInfo)
        {
            this.localRoot = PathRepresentationConverter.RemoteToLocal(repoInfo.LocalPath);
            this.remoteRoot = PathRepresentationConverter.LocalToRemote(repoInfo.RemotePath);

            this.remotePath = remotePath;
            if (remotePath.StartsWith(this.remoteRoot))
            {
                this.remotePath = remotePath.Substring(this.remoteRoot.Length).TrimStart(CmisPath.CMIS_FILE_SEPARATOR);
            }
            this.localPath = PathRepresentationConverter.RemoteToLocal(this.remotePath);
        }

        /// <summary></summary>
        /// <param name="remoteFolder"></param>
        /// <param name="localRelativePath"></param>
        /// <param name="repoInfo"></param>
        public RemotePathSyncItem(string remoteFolder, string localRelativePath, Config.SyncConfig.LocalRepository repoInfo)
        {
            this.localRoot = repoInfo.LocalPath;
            this.remoteRoot = repoInfo.RemotePath;

            this.remotePath = Path.Combine(remoteFolder, PathRepresentationConverter.LocalToRemote(localRelativePath));
            if (this.remotePath.StartsWith(this.remoteRoot))
            {
                this.remotePath = this.remotePath.Substring(this.localRoot.Length).TrimStart(CmisPath.CMIS_FILE_SEPARATOR);
            }
            string remoteRootRelative = remoteFolder;
            if (remoteFolder.StartsWith(this.remoteRoot))
            {
                remoteRootRelative = remoteFolder.Substring(localRoot.Length).TrimStart(CmisPath.CMIS_FILE_SEPARATOR);
            }
            this.localPath = Path.Combine(PathRepresentationConverter.RemoteToLocal(remoteRootRelative), localRelativePath);
        }

        /// <summary></summary>
        public override string LocalRelativePath
        {
            get
            {
                return localPath;
            }
        }

        /// <summary></summary>
        public override string RemoteRelativePath
        {
            get
            {
                return remotePath;
            }
        }
        
        /// <summary></summary>
        public override string LocalPath
        {
            get
            {
                return Path.Combine(localRoot, localPath);
            }
        }

        /// <summary></summary>
        public override string RemotePath
        {
            get
            {
                return Path.Combine(remoteRoot, remotePath);
            }
        }

        /// <summary></summary>
        public override string LocalFileName
        {
            get
            {
                return Path.GetFileName(localPath);
            }
        }

        /// <summary></summary>
        public override string RemoteFileName
        {
            get
            {
                return Path.GetFileName(remotePath);
            }
        }
    }

    /// <summary>Path representation converter.</summary>
    public interface IPathRepresentationConverter
    {
        /// <summary></summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        string LocalToRemote(string localPath);

        /// <summary></summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        string RemoteToLocal(string remotePath);
    }

    /// <summary></summary>
    public class DefaultPathRepresentationConverter : IPathRepresentationConverter
    {
        /// <summary></summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public string LocalToRemote(string localPath)
        {
            return localPath;
        }

        /// <summary></summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        public string RemoteToLocal(string remotePath)
        {
            return remotePath;
        }
    }

    /// <summary>Path representation converter.</summary>
    public static class PathRepresentationConverter
    {
        private static IPathRepresentationConverter PathConverter = new DefaultPathRepresentationConverter();

        /// <summary></summary>
        /// <param name="converter"></param>
        static public void SetConverter(IPathRepresentationConverter converter)
        {
            PathConverter = converter;
        }

        /// <summary></summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        static public string LocalToRemote(string localPath)
        {
            return PathConverter.LocalToRemote(localPath);
        }

        /// <summary></summary>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        static public string RemoteToLocal(string remotePath)
        {
            return PathConverter.RemoteToLocal(remotePath);
        }
    }
}

