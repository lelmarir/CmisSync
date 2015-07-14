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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using log4net;
using CmisSync.Auth;

namespace CmisSync.Lib
{
    /// <summary>
    /// Configuration of a CmisSync synchronized folder.
    /// It can be found in the XML configuration file.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The current config schema version.
        /// </summary>
        public const int SchemaVersion = 1;
        
        /// <summary>
        /// Chunk size for chunked transfers (not implemented yet)
        /// </summary>
        private const long DEFAULT_CHUNK_SIZE = 1024 * 1024;
        
        /// <summary>
        /// Default poll interval.
        /// It is used for any newly created synchronized folder.
        /// In milliseconds.
        /// </summary>
        public static readonly int DEFAULT_POLL_INTERVAL = 5 * 1000; // 5 seconds.
        
        /// <summary>
        /// Log.
        /// </summary>
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(Config));


        /// <summary>
        /// data structure storing the configuration.
        /// </summary>
        private SyncConfig configXml;
        
        /// <summary>
        /// Full path to the XML configuration file.
        /// </summary>
        public string ConfigurationFileFullPath { get; private set; }
        
        /// <summary>
        /// Path of the folder where configuration files are.
        /// These files are in particular the XML configuration file, the database files, and the log file.
        /// </summary>
        public string ConfigurationDirectoryPath { get; private set; }

        // XML elements.

        /// <summary>
        /// Config schema version.
        /// </summary>
        public int ConfigSchemaVersion { get { return configXml.ConfigSchemaVersion; } set { configXml.ConfigSchemaVersion = value; } }

        /// <summary>
        /// Notifications.
        /// </summary>
        public bool Notifications { get { return configXml.Notifications; } set { configXml.Notifications = value; } }

        /// <summary>
        /// Single Repository Only.
        /// </summary>
        public bool SingleRepository { get { return configXml.SingleRepository; } set { configXml.SingleRepository = value; } }

        /// <summary>
        /// Frozen configuration: The configuration can not be modified from the UI
        /// </summary>
        public bool FrozenConfiguration { get { return configXml.FrozenConfiguration; } set { configXml.FrozenConfiguration = value; } }

        public List<SyncConfig.LocalRepository> LocalRepositories { get { return configXml.LocalRepositories; } }

        /// <summary>
        /// Path to the user's home folder.
        /// </summary>
        public string UserHomePath
        {
            get
            {
                if (Backend.Platform == PlatformID.Win32NT)
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                else
                    return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }
        }


        /// <summary>
        /// Path where the synchronized folders are stored by default.
        /// </summary>
        public string DEFAULT_REPOSITORY_ROOT_FOLDER_PATH
        {
            get
            {
                return Path.Combine(UserHomePath, "CmisSync");
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public Config(string fullPath)
        {
            ConfigurationFileFullPath = fullPath;
            ConfigurationDirectoryPath = Path.GetDirectoryName(ConfigurationFileFullPath);
            Console.WriteLine("FullPath:" + ConfigurationFileFullPath);

            // Create configuration folder if it does not exist yet.
            if (!Directory.Exists(ConfigurationDirectoryPath))
                Directory.CreateDirectory(ConfigurationDirectoryPath);

            // Create an empty XML configuration file if none is present yet.
            if (!File.Exists(ConfigurationFileFullPath))
            {
                CreateInitialConfigFile();
            }

            // Load the XML configuration.
            try
            {
                Load();
            }
            catch (TypeInitializationException)
            {
                CreateInitialConfigFile();
            }
            catch (FileNotFoundException)
            {
                CreateInitialConfigFile();
            }
            catch (XmlException)
            {
                FileInfo file = new FileInfo(ConfigurationFileFullPath);

                // If the XML configuration file exists but with file size zero, then recreate it.
                if (file.Length == 0)
                {
                    File.Delete(ConfigurationFileFullPath);
                    CreateInitialConfigFile();
                }
                else
                {
                    throw new XmlException(ConfigurationFileFullPath + " does not contain a valid config XML structure.");
                }

            }

            //try again
            try
            {
                Load();
            }
            catch (Exception e) {
                throw;
            }
        }

        /// <summary>
        /// Create the initial XML configuration file.
        /// </summary>
        private void CreateInitialConfigFile()
        {
            CreateConfigFromScratch();

            // Save it as an XML file.
            Save();
        }


        /// <summary>
        /// Create an initial XML configuration with default settings and zero remote folders.
        /// </summary>
        private void CreateConfigFromScratch()
        {
            // Get the user name.
            string userName = "Unknown";
            if (Backend.Platform == PlatformID.Unix ||
                Backend.Platform == PlatformID.MacOSX)
            {
                userName = Environment.UserName;
                if (string.IsNullOrEmpty(userName))
                {
                    userName = String.Empty;
                }
                else
                {
                    userName = userName.TrimEnd(",".ToCharArray());
                }
            }
            else
            {
                userName = Environment.UserName;
            }

            if (string.IsNullOrEmpty(userName))
            {
                userName = "Unknown";
            }
            // Define the default XML configuration file.
            configXml = new SyncConfig()
            {
                ConfigSchemaVersion = Config.SchemaVersion,
                Notifications = true,
                SingleRepository = false, // Multiple repository for CmisSync, but some CmisSync-derived products have different defaults.
                FrozenConfiguration = false,
                Log4Net = createDefaultLog4NetElement(),
                LocalRepositories = new List<SyncConfig.LocalRepository>(),
                User = new User()
                {
                    EMail = "Unknown",
                    Name = userName
                }
            };
        }


        /// <summary>
        /// Log4net configuration, as an XML tree readily usable by Log4net.
        /// </summary>
        /// <returns></returns>
        public XmlElement GetLog4NetConfig()
        {
            return configXml.Log4Net as XmlElement;
        }

        /// <summary>
        /// Sets a new XmlNode as Log4NetConfig. Is useful for config migration
        /// </summary>
        /// <param name="node"></param>
        public void SetLog4NetConfig(XmlNode node)
        {
            this.configXml.Log4Net = node;
        }


        /// <summary>
        /// Add a synchronized folder to the configuration.
        /// </summary>
        public void AddLocalRepository(SyncConfig.LocalRepository repoInfo)
        {
            if (null == repoInfo)
            {
                return;
            }

            // Check that the CmisSync root folder exists.
            if (!Directory.Exists(ConfigManager.CurrentConfig.DEFAULT_REPOSITORY_ROOT_FOLDER_PATH))
            {
                Logger.Fatal(String.Format("Fetcher | ERROR - Cmis Default Folder {0} does not exist", ConfigManager.CurrentConfig.DEFAULT_REPOSITORY_ROOT_FOLDER_PATH));
                throw new DirectoryNotFoundException("Root folder don't exist !");
            }

            // Check that the folder is writable.
            if (!CmisSync.Lib.Utils.HasWritePermissionOnDir(ConfigManager.CurrentConfig.DEFAULT_REPOSITORY_ROOT_FOLDER_PATH))
            {
                Logger.Fatal(String.Format("Fetcher | ERROR - Cmis Default Folder {0} is not writable", ConfigManager.CurrentConfig.DEFAULT_REPOSITORY_ROOT_FOLDER_PATH));
                throw new UnauthorizedAccessException("Root folder is not writable!");
            }

            // Check that the folder exists.
            if (Directory.Exists(repoInfo.LocalPath))
            {
                Logger.Fatal(String.Format("Fetcher | ERROR - Cmis Repository Folder {0} already exist", repoInfo.LocalPath));
                throw new UnauthorizedAccessException("Repository folder already exists!");
            }

            // Create the local folder.
            Directory.CreateDirectory(repoInfo.LocalPath);

            this.configXml.LocalRepositories.Add(repoInfo);

            Save();
        }


        /// <summary>
        /// Remove a synchronized folder from the configuration.
        /// </summary>
        public void RemoveFolder(SyncConfig.LocalRepository repo)
        {
            this.configXml.LocalRepositories.Remove(repo);
            Logger.Info("Removed sync config: " + repo);
            Save();
        }


        /// <summary>
        /// Get the configured path to the log file.
        /// </summary>
        public string GetLogFilePath()
        {
            return Path.Combine(ConfigurationDirectoryPath, "debug_log.txt");
        }

        private string GetLogLevel()
        {
#if (DEBUG)
            return "DEBUG";
#else
            return "INFO";
#endif
        }


        /// <summary>
        /// Save the currently loaded (in memory) configuration back to the XML file.
        /// </summary>
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SyncConfig));
            using (TextWriter textWriter = new StreamWriter(ConfigurationFileFullPath))
            {
                serializer.Serialize(textWriter, this.configXml);
            }
        }


        private void Load()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(SyncConfig));
            using (TextReader textReader = new StreamReader(ConfigurationFileFullPath))
            {
                this.configXml = (SyncConfig)deserializer.Deserialize(textReader);
            }
        }

        private XmlElement createDefaultLog4NetElement()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(XmlElement));
            using (TextReader textReader = new StringReader(@"
  <log4net>
    <appender name=""CmisSyncFileAppender"" type=""log4net.Appender.RollingFileAppender"">
      <file value=""" + GetLogFilePath() + @""" />
      <appendToFile value=""true"" />
      <rollingStyle value=""Size"" />
      <maxSizeRollBackups value=""5"" />
      <maximumFileSize value=""1MB"" />
      <staticLogFileName value=""true"" />
      <layout type=""log4net.Layout.PatternLayout"">
        <conversionPattern value=""%date [%thread] %-5level %logger [%property{NDC}] - %message%newline"" />
      </layout>
    </appender>
    <root>
      <level value=""" + GetLogLevel() + @""" />
      <appender-ref ref=""CmisSyncFileAppender"" />
    </root>
  </log4net>"))
            {
                XmlElement result = (XmlElement)deserializer.Deserialize(textReader);
                return result;
            }
        }

        /// <summary>
        /// Sync configuration.
        /// </summary>
        [XmlRoot("CmisSync", Namespace = null)]
        public class SyncConfig {

            /// <summary>
            /// Config schema version.
            /// </summary>
            [XmlElement("configSchemaVersion")]
            public Int32 ConfigSchemaVersion { get; set; }
            
            /// <summary>
            /// Notifications.
            /// </summary>
            [XmlElement("notifications")]
            public Boolean Notifications { get; set; }
            
            /// <summary>
            /// Single repository.
            /// </summary>
            [XmlElement("singleRepository")]
            public Boolean SingleRepository { get; set; }
            
            /// <summary>
            /// Frozen configuration: The configuration can not be modified from the UI.
            /// </summary>
            [XmlElement("frozenConfiguration")]
            public Boolean FrozenConfiguration { get; set; }
            
            /// <summary>
            /// Logging config.
            /// </summary>
            [XmlAnyElement("log4net")]
            public XmlNode Log4Net { get; set; }
            
            /// <summary>
            /// List of the CmisSync synchronized folders.
            /// </summary>
            [XmlArray("localRepositories")]
            [XmlArrayItem("localRepository")]
            public List<SyncConfig.LocalRepository> LocalRepositories { get; set; }
            
            /// <summary>
            /// User.
            /// </summary>
            [XmlElement("user", typeof(User))]
            public User User { get; set; }

            /// <summary>
            /// Folder definition.
            /// </summary>
            public class LocalRepository
            {

                private readonly int MAX_FILE_NAME_TRY = 30;

                public LocalRepository() { 
                    IgnoredFolders = new List<IgnoredFolder>();
                }

                /// <summary>
                /// Name.
                /// </summary>
                [XmlElement("name")]
                public string DisplayName { get; set; }

                /// <summary>
                /// Path.
                /// </summary>
                [XmlElement("path")]
                public string LocalPath { get; set; }

                /// <summary>
                /// URL.
                /// </summary>
                [XmlElement("url")]
                public XmlUri RemoteUrl { get; set; }                

                /// <summary>
                /// Repository ID.
                /// </summary>
                [XmlElement("repository")]
                public string RepositoryId { get; set; }

                /// <summary>
                /// Remote path.
                /// </summary>
                [XmlElement("remoteFolder")]
                public string RemotePath { get; set; }

                /// <summary>
                /// Username.
                /// </summary>
                [XmlElement("user")]
                public string UserName { get; set; }

                /// <summary>
                /// CMIS password, hashed.
                /// For instance: AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAAtiSvUCYn...
                /// </summary>
                [XmlElement("password")]
                public Password Password { get; set; }

                /// <summary>
                /// IsSuspended
                /// </summary>
                [XmlElement("issuspended")]
                public bool IsSuspended { get; set; }

                /// <summary></summary>
                [XmlElement("syncatstartup")]
                public bool SyncAtStartup { get; set; }

                private double pollInterval = DEFAULT_POLL_INTERVAL;

                /// <summary>
                /// Poll interval.
                /// </summary>
                [XmlElement("pollinterval")]
                public double PollInterval {
                    get { return pollInterval; }
                    set {
                        if (value <= 0)
                        {
                            Logger.Warn("Poll interval value is invalid, "
                                + "using default poll interval: " + DEFAULT_POLL_INTERVAL);
                            pollInterval = DEFAULT_POLL_INTERVAL;
                        }
                        else
                        {
                            pollInterval = value;
                        }
                    }
                }

                [XmlIgnore]
                public int DEFAULT_MAX_UPLOAD_RETRIES = 2;
                [XmlElement("maxUploadRetries")]
                public int? _uploadRetries;
                /// <summary></summary>
                [XmlIgnore]
                public int? UploadRetries
                {
                    get {
                        if (_uploadRetries == null) {
                            return DEFAULT_MAX_UPLOAD_RETRIES;
                        }
                        return _uploadRetries; }
                    set {
                        if (value < 0)
                        {
                            throw new System.ArgumentException("UploadRetries value is not valid");
                        }
                        _uploadRetries = value;
                    }
                }

                [XmlIgnore]
                public int DEFAULT_MAX_DOWNLOAD_RETRIES = 2;
                [XmlElement("maxDownloadRetries", IsNullable = true)]
                public int? _downloadRetries;
                /// <summary></summary>
                [XmlIgnore]
                public int? DownloadRetries
                {
                    get
                    {
                        if (_downloadRetries == null)
                        {
                            return DEFAULT_MAX_DOWNLOAD_RETRIES;
                        }
                        return _downloadRetries;
                    }
                    set
                    {
                        if (value < 0)
                        {
                            throw new System.ArgumentException("DownloadRetries value is not valid");
                        }
                        _downloadRetries = value;
                    }
                }

                [XmlIgnore]
                public int DEFAULT_MAX_DELETION_RETRIES = 2;
                [XmlElement("maxDeletionRetries", IsNullable = true)]
                public int? _deletionRetries;
                /// <summary></summary>
                [XmlIgnore]
                public int? DeletionRetries
                {
                    get
                    {
                        if (_deletionRetries == null)
                        {
                            return DEFAULT_MAX_DELETION_RETRIES;
                        }
                        return _deletionRetries;
                    }
                    set
                    {
                        if (value < 0)
                        {
                            throw new System.ArgumentException("DeletionRetries value is not valid");
                        }
                        _deletionRetries = value;
                    }
                }

                /// <summary></summary>
                [XmlElement("features", IsNullable=true)]
                public Feature SupportedFeatures { get; set;}

                /// <summary>
                /// Ignored folders.
                /// </summary>
                [XmlElement("ignoreFolder", IsNullable = true)]
                public List<IgnoredFolder> IgnoredFolders { get; set; }

                private long chunkSize = DEFAULT_CHUNK_SIZE;

                /// <summary></summary>
                [XmlElement("chunkSize"), System.ComponentModel.DefaultValue(DEFAULT_CHUNK_SIZE)]
                public long ChunkSize
                {
                    get { return chunkSize; }
                    set
                    {
                        if (value < 0)
                        {
                            chunkSize = 0;
                        }
                        else
                        {
                            chunkSize = value;
                        }
                    }
                }

                private string _cmisDatabasePath;
                /// <summary>
                /// Full path to the local database.
                /// For instance: C:\\Users\\win7pro32bit\\AppData\\Roaming\\cmissync\\User Homes.cmissync
                /// </summary>
                [XmlElement("cmisDatabasePath")]
                public string CmisDatabasePath { 
                    get 
                    {
                        if (_cmisDatabasePath == null) {
                            _cmisDatabasePath = getNewCmisDatabasePath();
                        }
                        return _cmisDatabasePath;
                    } 
                    set 
                    {
                        _cmisDatabasePath = value;
                    } 
                }

                /// <summary>
                /// Get a new DatabasePath.
                /// The path will allways point to a non existing file.
                /// </summary>
                /// <returns></returns>
                public string getNewCmisDatabasePath() {
                    string name = DisplayName.Replace(" ", "_");
                    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                    {
                        name = name.Replace(c.ToString(), "");
                    }
                    string path;
                    int serial = 0;
                    do
                    {
                        path = Path.Combine(ConfigManager.CurrentConfig.ConfigurationDirectoryPath, name + ((serial != 0) ? "_" + serial : "") + ".cmissync");
                        serial++;
                        if (serial > MAX_FILE_NAME_TRY) {
                            throw new System.InvalidOperationException("Unable to find a name for a new database");
                        }
                    } while (File.Exists(path));
                    
                    return path;
                }

                /// <summary>
                /// Define the last successed sync time
                /// </summary>
                [XmlIgnore]
                public DateTime LastSuccessedSync { get; set; }

                /// <summary>
                /// If the given path should be ignored, TRUE will be returned,
                /// otherwise FALSE.
                /// </summary>
                /// <param name="path"></param>
                /// <returns></returns>
                public bool isPathIgnored(string path)
                {
                    if(Utils.IsInvalidFolderName(path.Replace("/", "").Replace("\"","")))
                        return true;
                    foreach(IgnoredFolder ignoredFolder in IgnoredFolders) 
                    {
                        if(ignoredFolder.Match(path))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public override string ToString() {
                    return this.DisplayName + "(" + this.LocalPath + "<->" + this.RemoteUrl + "/" + this.RemotePath + ")";
                }

            }
        }

        /// <summary>
        /// Ignored folder.
        /// </summary>
        public class IgnoredFolder
        {
            /// <summary>
            /// Folder path.
            /// </summary>
            [XmlAttribute("path")]
            public string Path { get; set; }
        
            public bool Match(string path)
            {
 	            if(String.IsNullOrEmpty(path)) {
                    return false;
                }
                //FIXME: use regex
                return path.StartsWith(Path);
            }
        }

        /// <summary>
        /// User details.
        /// </summary>
        public class User
        {
            /// <summary>
            /// Name.
            /// </summary>
            [XmlElement("name")]
            public string Name { get; set; }
            /// <summary>
            /// Email.
            /// </summary>
            [XmlElement("email")]
            public string EMail { get; set; }
        }

        /// <summary></summary>
        public class Feature {
            /// <summary></summary>
            [XmlElement("getFolderTree", IsNullable=true)]
            public bool? GetFolderTreeSupport {get; set;}
            /// <summary></summary>
            [XmlElement("getDescendants", IsNullable=true)]
            public bool? GetDescendantsSupport {get; set;}
            /// <summary></summary>
            [XmlElement("getContentChanges", IsNullable=true)]
            public bool? GetContentChangesSupport {get; set;}
            /// <summary></summary>
            [XmlElement("fileSystemWatcher", IsNullable=true)]
            public bool? FileSystemWatcherSupport {get; set;}
            /// <summary></summary>
            [XmlElement("maxContentChanges", IsNullable=true)]
            public int? MaxNumberOfContentChanges {get; set;}
            /// <summary></summary>
            [XmlElement("chunkedSupport", IsNullable=true)]
            public bool? ChunkedSupport {get;set;}
            /// <summary></summary>
            [XmlElement("chunkedDownloadSupport", IsNullable=true)]
            public bool? ChunkedDownloadSupport {get;set;}
        }
    }
}
