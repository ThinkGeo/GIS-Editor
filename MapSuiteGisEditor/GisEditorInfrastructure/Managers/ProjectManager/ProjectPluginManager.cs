/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(ProjectPluginManager))]
    public class ProjectPluginManager : PluginManager
    {
        private static readonly string settingsFileName;
        private static readonly string protectedElementName;
        private static object savingLocker;
        private static string projectSettingsPathFileName;
        private static ReaderWriterLockSlim deserializeLocker;
        private Uri projectUriCore;
        private bool isLoaded;
        private bool isUriChanged;
        private string projectID;
        private Version currentProjectVersion;

        private string viewPassword;
        private string savePassword;
        private ProjectReadWriteMode projectReadWriteState = ProjectReadWriteMode.ReadWrite;

        [NonSerialized]
        private DispatcherTimer autoBackupTimer;
        private Collection<GisEditorWpfMap> deserializedMaps;
        private Exception error = null;
        private ProjectPlugin backupProjectPlugin;

        private static readonly string tempString = "Temp";
        private static readonly string dataFilesPathFileName = "DataFiles.txt";
        private static readonly string tempFilesPath = GetTemporaryFolder();

        /// <summary>
        /// Occurs when [opened].
        /// </summary>
        public event EventHandler<OpenedProjectManagerEventArgs> Opened;

        /// <summary>
        /// Occurs when [opening].
        /// </summary>
        public event EventHandler<OpeningProjectManagerEventArgs> Opening;

        /// <summary>
        /// Occurs when [closed].
        /// </summary>
        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Occurs when [closing].
        /// </summary>
        public event EventHandler<EventArgs> Closing;

        /// <summary>
        /// Occurs when [project URI changed].
        /// </summary>
        public event EventHandler<UriChangedProjectPluginManagerEventArgs> ProjectUriChanged;

        /// <summary>
        /// Occurs when [loading preview image].
        /// </summary>
        public event EventHandler<LoadingPreviewImageProjectPluginManagerEventArgs> LoadingPreviewImage;

        /// <summary>
        /// Occurs when [auto backup interval changed].
        /// </summary>
        public event EventHandler<AutoBackupIntervalChangedProjectPluginManagerEventArgs> AutoBackupIntervalChanged;

        /// <summary>
        /// Occurs when [can auto backup changed].
        /// </summary>
        public event EventHandler<CanAutoBackupChangedProjectPluginManagerEventArgs> CanAutoBackupChanged;

        /// <summary>
        /// Occurs when [project state changed].
        /// </summary>
        public event EventHandler<StateChangedProjectPluginManagerEventArgs> ProjectStateChanged;


        private ProjectPlugin currentProjectPlugin;

        static ProjectPluginManager()
        {
            savingLocker = new object();
            deserializeLocker = new ReaderWriterLockSlim();
            settingsFileName = "ProjectSettings.settings";
            protectedElementName = "Protection";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectPluginManager" /> class.
        /// </summary>
        public ProjectPluginManager()
        {
            deserializedMaps = new Collection<GisEditorWpfMap>();
            autoBackupTimer = new DispatcherTimer(DispatcherPriority.Background);
            autoBackupTimer.Tick += new EventHandler(AutoBackupTimer_Tick);

        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is loaded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is loaded; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoaded
        {
            get { return isLoaded; }
            protected set { isLoaded = value; }
        }

        /// <summary>
        /// Gets or sets the project URI.
        /// </summary>
        /// <value>
        /// The project URI.
        /// </value>
        public Uri ProjectUri
        {
            get { return projectUriCore; }
            set
            {
                Uri oldProjectUri = projectUriCore;
                if (oldProjectUri != value)
                {
                    projectUriCore = value;
                    isUriChanged = true;

                    UriChangedProjectPluginManagerEventArgs eventArgs = new UriChangedProjectPluginManagerEventArgs(projectUriCore, oldProjectUri);
                    projectReadWriteState = ProjectReadWriteMode.ReadWrite;
                    eventArgs.ProjectReadWriteMode = projectReadWriteState;
                    OnProjectUriChanged(eventArgs);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can auto backup.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can auto backup; otherwise, <c>false</c>.
        /// </value>
        public bool CanAutoBackup
        {
            get { return autoBackupTimer.IsEnabled; }
            set
            {
                if (autoBackupTimer.IsEnabled != value)
                {
                    autoBackupTimer.IsEnabled = value;
                    OnCanAutoBackupChanged(new CanAutoBackupChangedProjectPluginManagerEventArgs(value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the auto backup interval.
        /// </summary>
        /// <value>
        /// The auto backup interval.
        /// </value>
        public TimeSpan AutoBackupInterval
        {
            get
            {
                return autoBackupTimer.Interval;
            }
            set
            {
                TimeSpan oldInterval = autoBackupTimer.Interval;
                if (oldInterval != value)
                {
                    autoBackupTimer.Interval = value;
                    OnAutoBackupIntervalChanged(new AutoBackupIntervalChangedProjectPluginManagerEventArgs(value, oldInterval));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current project plugin.
        /// </summary>
        /// <value>
        /// The current project plugin.
        /// </value>
        public ProjectPlugin CurrentProjectPlugin
        {
            get
            {
                return currentProjectPlugin;
            }
            set
            {
                if (currentProjectPlugin == null) backupProjectPlugin = value;
                currentProjectPlugin = value;
            }
        }

        internal static string ProjectSettingsPathFileName
        {
            get
            {
                if (string.IsNullOrEmpty(projectSettingsPathFileName))
                {
                    string tempPath = GisEditor.InfrastructureManager.TemporaryPath;
                    projectSettingsPathFileName = Path.Combine(tempPath, "TempProject", settingsFileName);
                }
                return projectSettingsPathFileName;
            }
        }

        public string Id
        {
            get { return projectID; }
        }


        public Version CurrentProjectVersion
        {
            get { return currentProjectVersion; }
        }

        /// <summary>
        /// Gets the settings core.
        /// </summary>
        /// <returns></returns>
        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();
            settings.GlobalSettings["AutoBackupInterval"] = AutoBackupInterval.Ticks.ToString();
            settings.ProjectSettings["ProjectID"] = projectID;

            XElement aliasElement = new XElement("Aliases");
            CleanUnusedAliases(GisEditor.GetMaps());
            foreach (var item in AliasExtension.Aliases)
            {
                aliasElement.Add(new XElement("Source"
                    , new XAttribute("Id", item.Key)
                    , item.Value.Select(i => new XElement("Alias"
                        , new XAttribute("Key", i.Key)
                        , new XAttribute("Value", i.Value))))
                    );
            }
            settings.ProjectSettings["Alias"] = aliasElement.ToString();
            return settings;
        }

        private static void CleanUnusedAliases(IEnumerable<GisEditorWpfMap> maps)
        {
            var featureSources = maps.SelectMany(m => m.GetFeatureLayers(false)).Select(f => f.FeatureSource).ToList();
            var aliasKeysInUsing = featureSources.Select(f => f.Id);
            var aliasKeysOutUsing = AliasExtension.Aliases.Keys.Where(k => !aliasKeysInUsing.Contains(k)).ToList();

            foreach (var unusedAlias in aliasKeysOutUsing)
            {
                AliasExtension.Aliases.Remove(unusedAlias);
            }
        }

        /// <summary>
        /// Applies the settings core.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("AutoBackupInterval"))
            {
                long oldAutoSaveInterval = 0;
                if (long.TryParse(settings.GlobalSettings["AutoBackupInterval"], out oldAutoSaveInterval))
                {
                    AutoBackupInterval = TimeSpan.FromTicks(oldAutoSaveInterval);
                }
            }

            if (settings.ProjectSettings.ContainsKey("ProjectID"))
            {
                projectID = settings.ProjectSettings["ProjectID"];
            }
            else
            {
                projectID = Guid.NewGuid().ToString();
            }

            AliasExtension.Aliases.Clear();
            if (settings.ProjectSettings.ContainsKey("Alias"))
            {
                XElement aliasElement = XElement.Parse(settings.ProjectSettings["Alias"]);
                foreach (var item in aliasElement.Elements("Source"))
                {
                    string id = item.Attribute("Id").Value;
                    Dictionary<string, string> aliases = new Dictionary<string, string>();
                    foreach (var subItem in item.Elements("Alias"))
                    {
                        aliases.Add(subItem.Attribute("Key").Value, subItem.Attribute("Value").Value);
                    }

                    AliasExtension.Aliases.Add(id, aliases);
                }
            }
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            return CollectPlugins<ProjectPlugin>();
        }

        public bool HasPassword()
        {
            return HasPasswordCore();
        }

        protected virtual bool HasPasswordCore()
        {
            string emptyPassword = StringProtector.Instance.Empty;
            return (!string.IsNullOrEmpty(viewPassword)
                || !string.IsNullOrEmpty(savePassword))
                && (emptyPassword != viewPassword
                || emptyPassword != savePassword);
        }

        public void PackageProjectFile()
        {
            PackageProjectFileCore();
        }

        protected virtual void PackageProjectFileCore()
        {
            Uri tempUri = GisEditor.ProjectManager.ProjectUri;

            try
            {
                string tempProjectFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), tempString, "MapSuiteGisEditor", string.Format("{0}.tgproj", DateTime.Now.ToString("yyyyMMddHHmmss")));
                if (File.Exists(tempProjectFilePath)) File.Delete(tempProjectFilePath);

                GisEditor.ProjectManager.ProjectUri = new Uri(tempProjectFilePath);
                ProjectSaveAsResult saveAsResult = CurrentProjectPlugin.GetPackageSaveAsUri();

                Uri tempLocalProjectUri = GisEditor.ProjectManager.ProjectUri;

                if (!saveAsResult.Canceled)
                {
                    SavePackageTempProject(tempLocalProjectUri);

                    string pathFileName = saveAsResult.Uri.LocalPath;
                    int currentProgress = 1;
                    string zipFilePath = Path.ChangeExtension(pathFileName, "tgproj");
                    string zipFileNameWithOutExtention = Path.GetFileNameWithoutExtension(pathFileName);
                    string path = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "PackingProject");
                    string tempDirectory = string.Empty;

                    PackProgressWindow packProgressWindow = new PackProgressWindow();
                    packProgressWindow.Show();

                    System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        using (var zipfileAdapter = ZipFileAdapterManager.CreateInstance(tempLocalProjectUri.LocalPath))
                        {
                            Collection<string> dataPathFileNames = GetDataFilePaths(zipfileAdapter);

                            MemoryStream resultDocumentStream = new MemoryStream();
                            Collection<MemoryStream> streams = new Collection<MemoryStream>();
                            try
                            {
                                string[] zipEntries = zipfileAdapter.GetEntryNames().Where(e => e.EndsWith(".tgmap", StringComparison.Ordinal) || e.EndsWith(".txt", StringComparison.Ordinal)).ToArray();
                                Dictionary<string, MemoryStream> newEntries = new Dictionary<string, MemoryStream>();

                                if (packProgressWindow.TotalTasks == 0)
                                {
                                    packProgressWindow.TotalTasks = zipEntries.Length + dataPathFileNames.Count;
                                }

                                foreach (var zipEntry in zipEntries)
                                {
                                    using (StreamReader reader = new StreamReader(zipfileAdapter.GetEntryStreamByName(zipEntry)))
                                    {
                                        string value = reader.ReadToEnd();
                                        foreach (var dataPathFileName in dataPathFileNames)
                                        {
                                            string fileName = Path.GetFileNameWithoutExtension(dataPathFileName).Replace("&", "&amp;");
                                            string oldValue = Path.Combine(Path.GetDirectoryName(dataPathFileName), fileName);
                                            string newValue = Path.Combine(".", zipFileNameWithOutExtention + "_Data", fileName);
                                            value = value.Replace(oldValue, newValue);
                                        }

                                        value = value.Replace(tempFilesPath, "..");

                                        MemoryStream memoStream = new MemoryStream(Encoding.UTF8.GetBytes(value));
                                        streams.Add(memoStream);

                                        newEntries.Add(zipEntry, memoStream);

                                        packProgressWindow.CurrentProgress = currentProgress;
                                        currentProgress++;

                                        reader.Close();
                                    }
                                }

                                zipfileAdapter.RemoveEntries(zipEntries);
                                foreach (var zipEntry in newEntries)
                                {
                                    zipfileAdapter.AddEntity(zipEntry.Key, zipEntry.Value);
                                }

                                string newProjName = Path.ChangeExtension(zipFileNameWithOutExtention, "proj");
                                string tempFileName = Path.ChangeExtension(Path.GetFileName(tempLocalProjectUri.LocalPath), "proj");

                                if (zipfileAdapter.GetEntryNames().Contains(tempFileName))
                                {
                                    zipfileAdapter.SetFileName(tempFileName, newProjName);

                                    if (!saveAsResult.KeepPasswords)
                                    {
                                        using (Stream tempStream = zipfileAdapter.GetEntryStreamByName(newProjName))
                                        {
                                            XDocument document = XDocument.Load(tempStream);
                                            XElement projectElement = document.Descendants("Project").FirstOrDefault();
                                            XElement protectElement = projectElement.Descendants(protectedElementName).FirstOrDefault();
                                            if (protectElement != null) protectElement.Remove();

                                            document.Save(resultDocumentStream);
                                            resultDocumentStream.Seek(0, SeekOrigin.Begin);

                                            zipfileAdapter.RemoveEntry(newProjName);
                                            zipfileAdapter.AddEntity(newProjName, resultDocumentStream);
                                        }
                                    }
                                }

                                if (tempLocalProjectUri.LocalPath.Equals(zipFilePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    zipFilePath = Path.Combine(Path.GetDirectoryName(pathFileName), tempString, Path.ChangeExtension(zipFileNameWithOutExtention, "tgproj"));
                                    tempDirectory = Path.GetDirectoryName(zipFilePath);

                                    if (!Directory.Exists(tempDirectory))
                                    {
                                        Directory.CreateDirectory(tempDirectory);
                                    }
                                }

                                zipfileAdapter.Save(zipFilePath);
                            }
                            finally
                            {
                                if (resultDocumentStream != null) resultDocumentStream.Dispose();

                                if (streams.Count > 1)
                                {
                                    foreach (var stream in streams)
                                    {
                                        stream.Dispose();
                                    }
                                }
                            }

                            currentProgress = MoveDataFiles(currentProgress, path, packProgressWindow, dataPathFileNames);
                        }

                        SavePackageResult(saveAsResult, zipFilePath, zipFileNameWithOutExtention, path);
                    });

                    task.ContinueWith(e =>
                    {
                        try
                        {
                            if (Directory.Exists(path)) Directory.Delete(path, true);
                            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
                            if (File.Exists(tempProjectFilePath)) File.Delete(tempProjectFilePath);
                        }
                        catch { }
                    });
                }
            }
            finally
            {
                GisEditor.ProjectManager.ProjectUri = tempUri;
            }
        }



        /// <summary>
        /// Projects the exists.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns></returns>
        public bool ProjectExists(Uri projectUri)
        {
            return ProjectExistsCore(projectUri);
        }

        /// <summary>
        /// Gets the project plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<ProjectPlugin> GetProjectPlugins()
        {
            return new Collection<ProjectPlugin>(GetPlugins().Cast<ProjectPlugin>().ToList());
        }

        public Collection<T> GetActiveProjectPlugins<T>() where T : ProjectPlugin
        {
            return new Collection<T>(GetActiveProjectPlugins().OfType<T>().ToList());
        }

        /// <summary>
        /// Gets the project plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<ProjectPlugin> GetActiveProjectPlugins()
        {
            var activePlugins = from p in GetProjectPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<ProjectPlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Projects the exists core.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns></returns>
        protected virtual bool ProjectExistsCore(Uri projectUri)
        {
            bool isExists = false;

            if (currentProjectPlugin != null)
            {
                isExists = currentProjectPlugin.ProjectExists(projectUri);
            }

            return isExists;
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        public void OpenProject()
        {
            OpenProject(ProjectUri);
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        public void OpenProject(Uri projectUri)
        {
            Validators.CheckIsProjectExist(projectUri, this);
            Validators.CheckAreSameProjectsOpened(projectUri, this);

            ProjectStreamInfo projectInfo;

            if (projectUri != null && projectUri.Scheme.ToLower().Contains("backup"))
            {
                string backupProjectFolder = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "BackupProject");

                switch (projectUri.Scheme)
                {
                    case "openbackup":
                        backupProjectFolder = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "OpenBackupProject");
                        break;
                    case "savebackup":
                        backupProjectFolder = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "SaveBackupProject");
                        break;
                }

                string path = projectUri.LocalPath;
                if (!File.Exists(projectUri.LocalPath))
                {
                    path = Directory.GetFiles(backupProjectFolder, "*.tgproj").FirstOrDefault();
                }


                projectInfo = new ProjectStreamInfo(new Uri("backup:" + path), null);
            }
            else
            {
                projectInfo = new ProjectStreamInfo(projectUri, null);
            }

            currentProjectPlugin.LoadProjectStream(projectInfo);

            try
            {
                if (projectUri != null && projectInfo.Stream != null)
                {
                    OpenProject(projectInfo);
                }
                else if (projectUri.AbsolutePath.Equals("blank", StringComparison.OrdinalIgnoreCase))
                {
                    IsLoaded = true;
                    ProjectUri = projectInfo == null ? null : projectInfo.Uri;

                    string tempProjectSettingPathFileName = projectSettingsPathFileName;
                    projectSettingsPathFileName = string.Empty;
                    GisEditor.InfrastructureManager.ApplySettings(GetStorableSettings());
                    projectSettingsPathFileName = tempProjectSettingPathFileName;
                }
            }
            catch (Exception innerException)
            {
                IsLoaded = false;
                string message = String.Format(CultureInfo.InvariantCulture, "Current project file [{0}] cannot be opened.", Path.GetFileName(projectUri.AbsolutePath));
                Exception ex = new InvalidDataException(message);

                ExceptionInfo error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
                LoggerMessage loggerMessage = new LoggerMessage(LoggerLevel.Error, message, error);
                GisEditor.LoggerManager.Log(loggerMessage);

                ExceptionInfo innerExcecptionInfo = new ExceptionInfo(innerException.Message, innerException.StackTrace, innerException.Source);
                LoggerMessage innerExcecptionMessage = new LoggerMessage(LoggerLevel.Debug, innerException.Message, innerExcecptionInfo);
                GisEditor.LoggerManager.Log(innerExcecptionMessage);
                throw ex;
            }

            if (projectUri.Scheme.Equals("backup", StringComparison.OrdinalIgnoreCase)) projectUriCore = projectUri;
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        public void OpenProject(ProjectStreamInfo projectStreamInfo)
        {
            try
            {
                if (projectStreamInfo != null)
                {
                    OpeningProjectManagerEventArgs args = new OpeningProjectManagerEventArgs(projectStreamInfo);
                    OnOpening(args);
                    if (args.IsCanceled) return;

                    error = null;

                    IsLoaded = true;
                    ProjectUri = projectStreamInfo == null ? null : projectStreamInfo.Uri;
                    OpenProjectCore(projectStreamInfo);

                    OpenedProjectManagerEventArgs e = new OpenedProjectManagerEventArgs(projectStreamInfo, error);
                    OnOpened(e);
                }
            }
            catch (Exception ex)
            {
                GisEditorMessageBox messageBox = new GisEditorMessageBox(System.Windows.MessageBoxButton.OK);
                messageBox.Message = String.Format(CultureInfo.InvariantCulture, "This project is last modified with version {0}, but it cannot be opened by this version. Please roll GIS Editior back to {0} and have another try.", currentProjectVersion);
                messageBox.Title = "Error";
                messageBox.ViewDetailHeader = "Call stack";
                messageBox.ErrorMessage = ex.StackTrace;
                messageBox.ShowDialog();
            }
            finally
            {
                Backup("OpenBackupProject");
            }
        }

        /// <summary>
        /// Opens the project core.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        protected virtual void OpenProjectCore(ProjectStreamInfo projectStreamInfo)
        {
            Stream projectStream = null;
            ZipFileAdapter zipFileAdapter = null;

            try
            {
                Collection<string> repairDatafiles = new Collection<string>();

                projectStream = projectStreamInfo.Stream;
                zipFileAdapter = ZipFileAdapterManager.CreateInstance(projectStream);
                //zipFile = ZipFile.Read(projectStream);
                string projectName = Path.GetFileNameWithoutExtension(currentProjectPlugin.GetProjectShortName(projectStreamInfo.Uri));
                //error = GetMissingDataFilesError(projectName, zipFileAdapter, repairDatafiles);

                LoadPasswords(zipFileAdapter, projectStreamInfo.Uri);

                //bool protectedPassed = LoadProjectProtectedConfigure(zipFileAdapter);
                //if (!protectedPassed) return;

                error = GetMissingDataFilesError(projectName, zipFileAdapter);

                LoadProjectSettings(projectName, zipFileAdapter);
                LoadDocumentWindows(projectStreamInfo.Uri, zipFileAdapter);
                GetDeserializedMaps();

                //FixMissingDataFileInMap(repairDatafiles);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (projectStream != null) projectStream.Dispose();
                if (zipFileAdapter != null) zipFileAdapter.Dispose();
            }
        }

        private static void FixMissingDataFileInMap(Collection<string> repairDatafiles)
        {
            if (repairDatafiles.Count > 0)
            {
                if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ProjectPluginManagerMissLayersText") + Environment.NewLine + GisEditor.LanguageManager.GetStringResource("ProjectPluginManagerRemapLocationText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                {
                    Collection<string> repairLayerPaths = new Collection<string>();
                    Collection<FeatureLayer> repairLayers = new Collection<FeatureLayer>();

                    var fileDialog = new OpenFileDialog();

                    foreach (var file in repairDatafiles)
                    {
                        var extension = Path.GetExtension(file);
                        var plugin = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>().FirstOrDefault(l => l.ExtensionFilter.Contains(extension));
                        if (plugin != null)
                        {
                            fileDialog.Title = GisEditor.LanguageManager.GetStringResource("ProjectPluginManagerMissingFileText") + " " + file;
                            fileDialog.Filter = plugin.ExtensionFilter;

                            var para = new GetLayersParameters();
                            if (fileDialog.ShowDialog().GetValueOrDefault())
                            {
                                para.LayerUris.Add(new Uri(fileDialog.FileName));
                            }

                            var newlayer = plugin.GetLayers(para).FirstOrDefault() as FeatureLayer;
                            repairLayers.Add(newlayer);
                        }
                    }

                    if (GisEditor.ActiveMap != null && repairLayers.Count > 0)
                    {
                        GisEditor.ActiveMap.AddLayersToActiveOverlay(repairLayers);
                        GisEditor.UIManager.RefreshPlugins();
                    }
                }
            }
        }

        /// <summary>
        /// Closes the project.
        /// </summary>
        public void CloseProject()
        {
            if (IsLoaded)
            {
                // FileGeoDatabaseFeatureSource.DisposeUnmanagedTables();

                OnClosing(null);
                IsLoaded = false;
                ClearCaches();
                CloseProjectCore();
                ProjectUri = null;
                OnClosed(null);
            }
        }

        /// <summary>
        /// Closes the project core.
        /// </summary>
        protected void CloseProjectCore()
        {
            ResetProjectState();
            CleanTempFiles();

            savePassword = string.Empty;
            viewPassword = string.Empty;

            projectReadWriteState = ProjectReadWriteMode.ReadWrite;
            OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        public void SaveProject()
        {
            if (IsLoaded)
            {
                SaveProject(ProjectUri);
            }
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        public void SaveProject(Uri projectUri)
        {
            //if (!CheckSavePassword(projectUri)) return;

            bool isAutoBackupTimerRunning = false;
            if (autoBackupTimer.IsEnabled)
            {
                isAutoBackupTimerRunning = true;
                autoBackupTimer.Stop();
            }

            ProjectStreamInfo projectStreamInfo = null;
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                if (CurrentProjectPlugin != null)
                {
                    bool keepPassword = true;

                    if (projectUri == null
                        || projectUri.AbsolutePath.Equals("blank", StringComparison.Ordinal)
                        || projectUri.AbsolutePath.Equals("Untitled", StringComparison.Ordinal)
                        || projectUri.LocalPath.Equals(GetBackupProjectFilePath(new Uri("giseditor:Untitled"), "BackupProject")))
                    {
                        var saveAsResult = CurrentProjectPlugin.GetProjectSaveAsUri();
                        if (!saveAsResult.Canceled)
                        {
                            projectStreamInfo = new ProjectStreamInfo(saveAsResult.Uri, memoryStream);
                            keepPassword = saveAsResult.KeepPasswords;
                        }
                    }
                    else
                    {
                        projectStreamInfo = new ProjectStreamInfo(projectUri, memoryStream);
                    }

                    if (projectStreamInfo != null)
                    {
                        SaveProject(projectStreamInfo);
                        if (projectStreamInfo.Stream.Length > 0)
                        {
                            projectStreamInfo.Stream.Seek(0, SeekOrigin.Begin);
                            if (!keepPassword) projectStreamInfo.Stream = GetCleanPasswordsStream(projectStreamInfo.Uri, projectStreamInfo.Stream);

                            CurrentProjectPlugin.SaveProjectStream(projectStreamInfo);
                            ProjectUri = projectStreamInfo.Uri;
                        }
                    }
                }

                if (isAutoBackupTimerRunning) autoBackupTimer.Start();
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (memoryStream != null) memoryStream.Dispose();

                Backup("SaveBackupProject");
            }
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void SaveProject(ProjectStreamInfo projectStreamInfo)
        {
            lock (savingLocker)
            {
                SaveProjectCore(projectStreamInfo);
            }

            if (projectStreamInfo.Stream.Length > 0)
            {
                var errors = ValidateProjectStream(projectStreamInfo.Stream);

                if (!string.IsNullOrEmpty(errors))
                {
                    throw new InvalidOperationException(errors);
                }
            }
        }

        /// <summary>
        /// Saves the project core.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        protected virtual void SaveProjectCore(ProjectStreamInfo projectStreamInfo)
        {
            if (projectStreamInfo != null)
            {
                var zipFileAdapter = ZipFileAdapterManager.CreateInstance();
                string tempProjectFolder = string.Empty;
                try
                {
                    string projectName = currentProjectPlugin.GetProjectShortName(projectStreamInfo.Uri);
                    XElement rootElement = new XElement("Project");
                    rootElement.Add(new XAttribute("Version", System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Windows.Application.ResourceAssembly.Location).FileVersion));
                    XElement documentsElement = new XElement("Documents");
                    documentsElement.Add(new XAttribute("ActivateIndex", GisEditor.DockWindowManager.ActiveDocumentIndex == -1 ? 0 : GisEditor.DockWindowManager.ActiveDocumentIndex));

                    string projectNameWithExtension = Path.GetFileName(projectName);
                    string projectNameWithoutExtension = Path.GetFileNameWithoutExtension(projectName);
                    tempProjectFolder = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "TempProject");

                    CreateTemprorayDirectory(tempProjectFolder);
                    SaveMapsState(projectStreamInfo, documentsElement, projectNameWithoutExtension, tempProjectFolder);
                    //SaveProjectProtectedConfigure(projectStreamInfo.ViewPassword, projectStreamInfo.SavePassword, tempProjectFolder);
                    SaveProjectInformation(Path.Combine(tempProjectFolder, projectNameWithExtension), rootElement, documentsElement);
                    CleanFiles(projectName, tempProjectFolder, GisEditor.DockWindowManager.DocumentWindows);
                    SaveProjectSettings();
                    SavePreviewImage(tempProjectFolder);

                    //zipFile = new ZipFile();
                    zipFileAdapter.AddDirectoryToZipFile(tempProjectFolder);
                    zipFileAdapter.Save(projectStreamInfo.Stream);

                    projectStreamInfo.Stream.Seek(0, SeekOrigin.Begin);
                    projectStreamInfo.Stream = GetProtectProjectStream(ZipFileAdapterManager.CreateInstance(projectStreamInfo.Stream), projectStreamInfo.Uri);

                    //zipFile.AddDirectory(tempProjectFolder);
                    //zipFile.Save(projectStreamInfo.Stream);
                    //if (zipFile != null) zipFile.Dispose();
                }
                catch (Exception ex)
                {
                    GisEditorMessageBox messageBox = new GisEditorMessageBox(System.Windows.MessageBoxButton.OK);
                    messageBox.Message = string.Concat("Save project failed. ", ex.Message);
                    messageBox.Title = "Error";
                    messageBox.ViewDetailHeader = "Call stack";
                    messageBox.ErrorMessage = ex.StackTrace;
                    messageBox.ShowDialog();

                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    //if (zipFile != null) zipFile.Dispose();
                    if (zipFileAdapter != null) zipFileAdapter.Dispose();
                    DeleteTemprorayDirectory(tempProjectFolder);
                }
            }
        }


        /// <summary>
        /// Raises the <see cref="E:ProjectUriChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UriChangedProjectPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnProjectUriChanged(UriChangedProjectPluginManagerEventArgs e)
        {
            EventHandler<UriChangedProjectPluginManagerEventArgs> handler = ProjectUriChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:ProjectUriChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UriChangedProjectPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnProjectStateChanged(StateChangedProjectPluginManagerEventArgs e)
        {
            EventHandler<StateChangedProjectPluginManagerEventArgs> handler = ProjectStateChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:AutoBackupIntervalChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="AutoBackupIntervalChangedProjectPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnAutoBackupIntervalChanged(AutoBackupIntervalChangedProjectPluginManagerEventArgs e)
        {
            EventHandler<AutoBackupIntervalChangedProjectPluginManagerEventArgs> handler = AutoBackupIntervalChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:CanAutoBackupChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="CanAutoBackupChangedProjectPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnCanAutoBackupChanged(CanAutoBackupChangedProjectPluginManagerEventArgs e)
        {
            EventHandler<CanAutoBackupChangedProjectPluginManagerEventArgs> handler = CanAutoBackupChanged;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:LoadingPreviewImage" /> event.
        /// </summary>
        /// <param name="e">The <see cref="LoadingPreviewImageProjectPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLoadingPreviewImage(LoadingPreviewImageProjectPluginManagerEventArgs e)
        {
            EventHandler<LoadingPreviewImageProjectPluginManagerEventArgs> handler = LoadingPreviewImage;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Closing" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected virtual void OnClosing(EventArgs e)
        {
            EventHandler<EventArgs> handler = Closing;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Closed" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected virtual void OnClosed(EventArgs e)
        {
            EventHandler<EventArgs> handler = Closed;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Opening" /> event.
        /// </summary>
        /// <param name="e">The <see cref="OpeningProjectManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnOpening(OpeningProjectManagerEventArgs e)
        {
            EventHandler<OpeningProjectManagerEventArgs> handler = Opening;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:Opened" /> event.
        /// </summary>
        /// <param name="e">The <see cref="OpenedProjectManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnOpened(OpenedProjectManagerEventArgs e)
        {
            EventHandler<OpenedProjectManagerEventArgs> handler = Opened;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<GettingDeserializedMapsEventArgs> GettingDeserializedMaps;

        protected virtual void OnGettingDeserializedMaps(GettingDeserializedMapsEventArgs e)
        {
            EventHandler<GettingDeserializedMapsEventArgs> handler = GettingDeserializedMaps;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<GottenDeserializedMapsEventArgs> GottenDeserializedMaps;

        protected virtual void OnGottenDeserializedMaps(GottenDeserializedMapsEventArgs e)
        {
            EventHandler<GottenDeserializedMapsEventArgs> handler = GottenDeserializedMaps;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Gets the deserialized maps.
        /// </summary>
        /// <returns></returns>
        public Collection<GisEditorWpfMap> GetDeserializedMaps()
        {
            try
            {
                GettingDeserializedMapsEventArgs e = new GettingDeserializedMapsEventArgs(ProjectUri);
                OnGettingDeserializedMaps(e);

                deserializeLocker.EnterWriteLock();
                if (isUriChanged || deserializedMaps == null)
                {
                    deserializedMaps = GetDeserializedMapsCore();
                    foreach (var featureLayer in deserializedMaps.SelectMany(m => m.GetFeatureLayers()))
                    {
                        featureLayer.FeatureIdsToExclude.Clear();
                    }
                }

                GottenDeserializedMapsEventArgs gottenEventArgs = new GottenDeserializedMapsEventArgs(deserializedMaps);
                OnGottenDeserializedMaps(gottenEventArgs);

                return deserializedMaps;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return new Collection<GisEditorWpfMap>();
            }
            finally
            {
                isUriChanged = false;
                deserializeLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the deserialized maps core.
        /// </summary>
        /// <returns></returns>
        protected virtual Collection<GisEditorWpfMap> GetDeserializedMapsCore()
        {
            deserializedMaps.Clear();

            if (!ProjectExists(ProjectUri))
            {
                return deserializedMaps;
            }

            var info = new ProjectStreamInfo(ProjectUri, null);
            currentProjectPlugin.LoadProjectStream(info);

            using (var zipFileAdapter = ZipFileAdapterManager.CreateInstance(info.Stream))
            {
                foreach (var document in GisEditor.DockWindowManager.DocumentWindows)
                {
                    GisEditorWpfMap wpfMap = null;

                    string projectName = Path.GetFileNameWithoutExtension(CurrentProjectPlugin.GetProjectShortName(ProjectUri));
                    string mapFileName = string.Format(CultureInfo.InvariantCulture, @"{0}.tgmap", document.Name);
                    string mapFileTitle = string.Format(CultureInfo.InvariantCulture, @"{0}.tgmap", document.Title);

                    if (!zipFileAdapter.GetEntryNames().Contains(mapFileName)) mapFileName = mapFileTitle;

                    if (zipFileAdapter.GetEntryNames().Contains(mapFileName))
                    {
                        //AppDomain currentDomain = AppDomain.CurrentDomain;
                        using (var mapStream = zipFileAdapter.GetEntryStreamByName(mapFileName))
                        {
                            var mapString = new StreamReader(mapStream).ReadToEnd();
                            mapString = GetAbsoluteTempFilePath(mapString);
                            mapString = GetAbsoluteDataFilePath(mapString);//mapString.Replace("./", Path.GetDirectoryName(ProjectUri.LocalPath) + "/");
                            mapString = GetFixedMapString(mapString);
                            mapString = GetFixedMissingFileMapString(zipFileAdapter, mapString);

                            //currentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                            using (var wpfMapMemory = new MemoryStream(Encoding.UTF8.GetBytes(mapString)))
                            {
                                wpfMap = (GisEditorWpfMap)GisEditor.Serializer.Deserialize(wpfMapMemory);

                                wpfMap.Overlays.OfType<LayerOverlay>().ForEach(o =>
                                {
                                    var shp = o.Layers.OfType<ShapeFileFeatureLayer>().ToList();
                                    foreach (var s in shp)
                                    {
                                        if (!File.Exists(s.ShapePathFilename))
                                        {
                                            //o.Layers.Remove(s);
                                            s.IsVisible = false;
                                        }
                                    }
                                });

                                wpfMap.Overlays.OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<ShapeFileFeatureLayer>()
                                    .Select(l => l.ZoomLevelSet).ForEach(zs =>
                                    {
                                        var zoomLevelsToRemove = zs.CustomZoomLevels.Where(z => z.ApplyUntilZoomLevel != ApplyUntilZoomLevel.None && z.CustomStyles.Count == 0).ToList();
                                        foreach (var z in zoomLevelsToRemove)
                                        {
                                            zs.CustomZoomLevels.Remove(z);
                                        }
                                    });

                                //currentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                                if (wpfMap != null) deserializedMaps.Add(wpfMap);
                            }
                        }
                    }
                }
            }

            return deserializedMaps;
        }

        /// <summary>
        /// Check can save project
        /// </summary>
        /// <param name="projectStreamInfo"></param>
        /// <returns></returns>
        public bool CanSaveProject(ProjectStreamInfo projectStreamInfo)
        {
            return CanSaveProjectCore(projectStreamInfo);
        }

        /// <summary>
        /// Check can save project core
        /// </summary>
        /// <param name="projectStreamInfo"></param>
        /// <returns></returns>
        protected virtual bool CanSaveProjectCore(ProjectStreamInfo projectStreamInfo)
        {
            bool canSaveProject = true;
            if (projectReadWriteState == ProjectReadWriteMode.ReadOnly
                && currentProjectPlugin.ProjectExists(projectStreamInfo.Uri)
                && StringProtector.Instance.Empty != savePassword)
            {
                EnterViewPasswordWindow window = new EnterViewPasswordWindow(GisEditor.LanguageManager.GetStringResource("PasswordToSave"));
                if (window.ShowDialog().GetValueOrDefault())
                {
                    string inputPassword = StringProtector.Instance.Encrypt(window.passwordBox.Password);
                    if (!inputPassword.Equals(savePassword, StringComparison.OrdinalIgnoreCase))
                    {
                        canSaveProject = false;
                        System.Windows.Forms.MessageBox.Show("The password is incorrect. GisEditor cannot save the changes.", "Password", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    else
                    {
                        projectReadWriteState = ProjectReadWriteMode.ReadWrite;
                        OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));
                    }
                }
                else
                {
                    canSaveProject = false;
                }
            }

            return canSaveProject;
        }

        /// <summary>
        /// Check can open project
        /// </summary>
        /// <param name="projectStreamInfo"></param>
        /// <returns></returns>
        public bool CanOpenProject(ProjectStreamInfo projectStreamInfo)
        {
            return CanOpenProjectCore(projectStreamInfo);
        }

        /// <summary>
        /// Check can open project core
        /// </summary>
        /// <param name="projectStreamInfo"></param>
        /// <returns></returns>
        protected virtual bool CanOpenProjectCore(ProjectStreamInfo projectStreamInfo)
        {
            bool canOpenProject = true;
            if (currentProjectPlugin != null)
            {
                currentProjectPlugin.LoadProjectStream(projectStreamInfo);

                if (projectStreamInfo.Stream != null)
                {
                    ZipFileAdapter zipFileAdapter = ZipFileAdapterManager.CreateInstance(projectStreamInfo.Stream);

                    string projectName = currentProjectPlugin.GetProjectShortName(projectStreamInfo.Uri);
                    string projectNameWithExtension = Path.GetFileName(Path.ChangeExtension(projectName, "proj"));

                    if (zipFileAdapter.GetEntryNames().Contains(projectNameWithExtension))
                    {
                        XDocument doucment = XDocument.Load(zipFileAdapter.GetEntryStreamByName(projectNameWithExtension));
                        XElement protectionElement = doucment.Descendants("Protection").FirstOrDefault();

                        string tempViewPassword = string.Empty;

                        if (protectionElement != null)
                        {
                            XElement viewElement = protectionElement.Descendants("View").FirstOrDefault();
                            if (viewElement != null)
                            {
                                tempViewPassword = protectionElement.Descendants("View").FirstOrDefault().Value;
                            }
                        }

                        if (!string.IsNullOrEmpty(tempViewPassword)
                            && StringProtector.Instance.Empty != tempViewPassword)
                        {
                            EnterViewPasswordWindow window = new EnterViewPasswordWindow(GisEditor.LanguageManager.GetStringResource("PasswordRequest"));
                            if (window.ShowDialog().GetValueOrDefault())
                            {
                                string inputPassword = StringProtector.Instance.Encrypt(window.passwordBox.Password);
                                if (!inputPassword.Equals(tempViewPassword, StringComparison.OrdinalIgnoreCase))
                                {
                                    canOpenProject = false;
                                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("PasswordNotCorrect"), "Password", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                                }
                                else
                                {
                                    projectReadWriteState = ProjectReadWriteMode.ReadOnly;
                                    OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));
                                }
                            }
                            else
                            {
                                canOpenProject = false;
                            }
                        }
                    }

                    projectStreamInfo.Stream.Seek(0, SeekOrigin.Begin);
                }
                // parse stream and restore passwords.
                // OpenDialog and check if the password is not empty.
                // canOpenProject = passwordMatches.
            }

            return canOpenProject;
        }

        /// <summary>
        /// Set password to project
        /// </summary>
        public void SetPassword()
        {
            SetPasswordCore();
        }

        /// <summary>
        /// Set password to project core
        /// </summary>
        protected virtual void SetPasswordCore()
        {
            string passwordToOpen = string.Empty;
            string passwordToSave = string.Empty;

            ProjectLockWindow projectLockWindow = new ProjectLockWindow("Set password.", false, "", "");
            projectLockWindow.Owner = System.Windows.Application.Current.MainWindow;

            if (!string.IsNullOrEmpty(savePassword))
                projectLockWindow.passwordBox2.Password = StringProtector.Instance.Decrypt(savePassword);
            if (!string.IsNullOrEmpty(viewPassword))
                projectLockWindow.passwordBox.Password = StringProtector.Instance.Decrypt(viewPassword);

            if (projectLockWindow.ShowDialog().GetValueOrDefault())
            {
                if (System.Windows.MessageBox.Show("The project will be protected by the specific password to open/save, do you want to continue?", "Info", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.Yes)
                {
                    passwordToOpen = projectLockWindow.passwordBox.Password;
                    passwordToSave = projectLockWindow.passwordBox2.Password;
                }
            }

            //if (string.IsNullOrEmpty(passwordToOpen) && string.IsNullOrEmpty(passwordToSave)) return;

            bool canSaveProject = true;

            if (projectReadWriteState == ProjectReadWriteMode.ReadOnly)
            {
                EnterViewPasswordWindow window = new EnterViewPasswordWindow(GisEditor.LanguageManager.GetStringResource("PasswordToSave"));
                if (window.ShowDialog().GetValueOrDefault())
                {
                    string inputPassword = StringProtector.Instance.Encrypt(window.passwordBox.Password);
                    if (!inputPassword.Equals(savePassword, StringComparison.OrdinalIgnoreCase))
                    {
                        canSaveProject = false;
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("PasswordNotCorrect"), "Password", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    else
                    {
                        projectReadWriteState = ProjectReadWriteMode.ReadWrite;
                        OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));
                    }
                }
                else
                {
                    canSaveProject = false;
                }
            }

            if (canSaveProject)
            {
                viewPassword = StringProtector.Instance.Encrypt(passwordToOpen);
                savePassword = StringProtector.Instance.Encrypt(passwordToSave);

                if (!(ProjectUri == null
                        || ProjectUri.AbsolutePath.Equals("blank", StringComparison.Ordinal)
                        || ProjectUri.AbsolutePath.Equals("Untitled", StringComparison.Ordinal)
                        || ProjectUri.LocalPath.Equals(GetBackupProjectFilePath(new Uri("giseditor:Untitled"), "BackupProject"))))
                {
                    ZipFileAdapter zipAdapter = ZipFileAdapterManager.CreateInstance(ProjectUri.LocalPath);
                    currentProjectPlugin.SaveProjectStream(new ProjectStreamInfo(ProjectUri, GetProtectProjectStream(zipAdapter, ProjectUri)));
                }
            }
        }

        private string GetFixedMissingFileMapString(ZipFileAdapter zipFileAdapter, string mapString)
        {
            var doc = XDocument.Parse(mapString);

            var elements = doc.Elements().Elements("overlays").Elements().Elements().Elements("layers").Elements();

            FixedMissingElements(elements, new Collection<string>());

            mapString = doc.ToString();

            return mapString;
        }

        private void FixedMissingElements(IEnumerable<XElement> elements, Collection<string> dataFiles)
        {
            Collection<XElement> removeElements = new Collection<XElement>();

            var elementsArray = elements.ToArray();

            for (int i = 0; i < elementsArray.Length; i++)
            {
                var value = elementsArray[i].Element("value");

                var typeValue = Type.GetType(value.Attribute("type").Value);
                if (typeValue == null) continue;

                var plugin = GisEditor.LayerManager.GetLayerPlugins(typeValue).FirstOrDefault();
                if (plugin != null)
                {
                    var extensions = plugin.ExtensionFilter.Replace("*", string.Empty).Split('|');
                    var layerContent = value.ToString();

                    foreach (var extension in extensions.Where(e => !string.IsNullOrEmpty(e)))
                    {
                        var endIndex = layerContent.ToUpper().IndexOf(extension.ToUpper() + "<");
                        if (endIndex >= 0)
                        {
                            endIndex += extension.Length;
                            var currentIndex = endIndex;
                            while (layerContent[currentIndex] != '>') currentIndex--;

                            currentIndex++;
                            var dataPath = GetAbsoluteDataFilePath(layerContent.Substring(currentIndex, endIndex - currentIndex));
                            dataPath = FixSpecialCharacters(dataPath);
                            if (!File.Exists(dataPath))
                            {
                                //elementsArray[i].Remove();
                                XElement isVisibleX = value.Element("isVisible");
                                if (isVisibleX != null)
                                {
                                    isVisibleX.Value = "false";
                                }
                                else
                                {
                                    value.Add(new XElement("isVisible", false));
                                }
                                if (!dataFiles.Contains(dataPath)) dataFiles.Add(dataPath);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private string FixSpecialCharacters(string path)
        {
            Dictionary<string, string> characters = new Dictionary<string, string>();
            characters.Add("&amp;amp;", "&");
            characters.Add("&amp;", "&");
            characters.Add("&lt;", "<");
            characters.Add("&gt;", ">");
            characters.Add("&apos;", "'");
            characters.Add("&quot;", "\"");

            foreach (var item in characters)
            {
                if (path.Contains(item.Key))
                {
                    path = path.Replace(item.Key, item.Value);
                }
            }

            return path;
        }

        internal void Backup(string schema)
        {
            if (projectReadWriteState != ProjectReadWriteMode.ReadOnly && IsLoaded && ProjectUri != null)
            {
                CleanAutoBackTempFiles();
                var tempProjectUri = new Uri(GetBackupProjectFilePath(ProjectUri, schema));

                var tmpCurrentProjectPlugin = currentProjectPlugin;
                currentProjectPlugin = backupProjectPlugin;

                //if (ProjectUri.LocalPath.Equals("blank", StringComparison.Ordinal))
                //{
                //    ProjectUri = new Uri("giseditor:Untitled");
                //}

                if (currentProjectPlugin != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        var projectStreamInfo = new ProjectStreamInfo(tempProjectUri, memoryStream);

                        SaveProject(projectStreamInfo);
                        projectStreamInfo.Stream.Seek(0, SeekOrigin.Begin);

                        var tempDirectory = Directory.GetParent(Path.GetDirectoryName(projectSettingsPathFileName)).FullName;
                        var backupDirectory = Path.Combine(tempDirectory, schema);

                        if (!Directory.Exists(backupDirectory))
                        {
                            Directory.CreateDirectory(backupDirectory);
                        }

                        currentProjectPlugin.SaveProjectStream(projectStreamInfo);

                        string[] contents = new string[2];
                        contents[0] = currentProjectPlugin.GetType().FullName;

                        if (ProjectUri != null)
                            contents[1] = ProjectUri.LocalPath;
                        else
                            contents[1] = currentProjectPlugin.GetProjectFullName(tempProjectUri);

                        //if (!string.IsNullOrEmpty(projectRealPath) && !projectRealPath.Equals("blank"))
                        //{
                        //    contents[1] = projectRealPath;
                        //}
                        //else if (!string.IsNullOrEmpty(tempProjectRealPath))
                        //{
                        //    contents[1] = tempProjectRealPath;
                        //}
                        //else
                        //{
                        //    contents[1] = currentProjectPlugin.GetProjectFullName(tempProjectUri);
                        //}

                        File.WriteAllLines(Path.ChangeExtension(tempProjectUri.LocalPath, ".tgproj.txt"), contents);
                        currentProjectPlugin = tmpCurrentProjectPlugin;
                    }
                }
            }
        }

        private string ValidateProjectStream(Stream stream)
        {
            var reslut = string.Empty;

            stream.Seek(0, SeekOrigin.Begin);

            //var zipFile = ZipFile.Read(stream);
            var zipFileAdapter = ZipFileAdapterManager.CreateInstance(stream);

            //var validateResult = ValidateProjectFiles(zipFile);
            //if (!string.IsNullOrEmpty(validateResult))
            //    reslut += validateResult;

            var validateResult = ValidateProjectDoucment(zipFileAdapter);
            if (!string.IsNullOrEmpty(validateResult))
                reslut += validateResult;

            return reslut;
        }

        private string ValidateProjectDoucment(ZipFileAdapter zipFileAdapter)
        {
            var result = string.Empty;

            var entities = zipFileAdapter.GetEntryNames().Where(e => { return e.EndsWith(".tgmap"); }).ToList();

            if (GisEditor.DockWindowManager.DocumentWindows.Count != entities.Count)
            {
                //foreach (var document in GisEditor.DockWindowManager.DocumentWindows)
                //{
                //    if (!entities.Contains(document.Title))
                //    {
                //        result += document.Title + ",";
                //    }
                //}

                //result = result.TrimEnd(',');

                result = "Map state validation failed." + System.Environment.NewLine;
            }

            return result;
        }

        private string GetFixedMapString(string mapString)
        {
            foreach (var item in GetKeyValue())
            {
                if (mapString.Contains(item.Item1))
                    mapString = mapString.Replace(item.Item1, item.Item2);
            }
            return mapString;
        }

        private IEnumerable<Tuple<string, string>> GetKeyValue()
        {
            yield return new Tuple<string, string>("type=\"ThinkGeo.MapSuite.GisEditor.Plugins.GisEditorLayerOverlay, GisEditorPluginCore"
                , "type=\"ThinkGeo.MapSuite.WpfDesktopEdition.Extension.GisEditorLayerOverlay, WpfDesktopEditionExtension");
            yield return new Tuple<string, string>("type=\"ThinkGeo.MapSuite.GisEditor.Plugins.GisEditorEditInteractiveOverlay, GisEditorPluginCore"
                , "type=\"ThinkGeo.MapSuite.WpfDesktopEdition.Extension.GisEditorEditInteractiveOverlay, WpfDesktopEditionExtension");
        }

        private void DeleteTemprorayDirectory(string tempProjectFolder)
        {
            if (Directory.Exists(tempProjectFolder))
            {
                Directory.Delete(tempProjectFolder, true);
            }
        }

        private static void CreateTemprorayDirectory(string mapFileFolder)
        {
            if (!Directory.Exists(mapFileFolder))
            {
                Directory.CreateDirectory(mapFileFolder);
            }
        }

        private Exception GetMissingDataFilesError(string projectName, ZipFileAdapter zipFileAdapter)
        {
            Exception error = null;

            var missingDataPathFileNames = GetMissingMapDataFilesByXml(projectName, zipFileAdapter);
            if (missingDataPathFileNames.Count > 0)
            {
                var newDataFiles = new Dictionary<string, string>();
                RemapMissingDataFiles(projectName, zipFileAdapter, missingDataPathFileNames, newDataFiles);

                var errorMessage = new StringBuilder();
                foreach (var missingFile in newDataFiles)
                {
                    if (string.IsNullOrEmpty(missingFile.Value))
                    {
                        errorMessage.AppendLine(missingFile.Key);
                    }
                }
                if (errorMessage.Length > 0)
                {
                    error = new FileNotFoundException("ShapeFile not found.", errorMessage.ToString());
                }
            }

            return error;
        }

        private Collection<string> GetMissingMapDataFilesByXml(string projectName, ZipFileAdapter zipFileAdapter)
        {
            Collection<string> dataFiles = new Collection<string>();

            IEnumerable<string> allMapNames = XDocument.Load(zipFileAdapter.GetEntryStreamByName(projectName + ".proj")).Descendants("Document").Select(d => d.Attribute("Name").Value + ".tgmap");
            string content = "";

            foreach (var map in allMapNames)
            {
                if (zipFileAdapter.GetEntryNames().Contains(map))
                {
                    using (StreamReader sr = new StreamReader(zipFileAdapter.GetEntryStreamByName(map)))
                    {
                        content = sr.ReadToEnd();
                        XDocument doc = XDocument.Parse(content);

                        IEnumerable<XElement> elements = doc.Elements().Elements("overlays").Elements().Elements().Elements("layers").Elements();
                        FixedMissingElements(elements, dataFiles);
                    }
                }
            }

            return dataFiles;
        }

        private void RemapMissingDataFiles(string projectName
            , ZipFileAdapter zipFileAdapter
            , IEnumerable<string> missingDataFiles
            , Dictionary<string, string> newDataFiles)
        {
            string projectFilePath = ProjectUri.LocalPath;
            if (!File.Exists(projectFilePath)) return;
            var fileDialog = new OpenFileDialog();

            string tmpProjectFolder = Path.GetDirectoryName(projectFilePath) + "\\" + Path.GetFileNameWithoutExtension(projectFilePath);
            string currentDirectory = Directory.GetCurrentDirectory();

            MissingDataWindow window = new MissingDataWindow();
            window.DataContext = missingDataFiles;

            if (window.ShowDialog().GetValueOrDefault())
            {
                foreach (var item in missingDataFiles)
                {
                    fileDialog.Title = GisEditor.LanguageManager.GetStringResource("ProjectPluginManagerMissingFileText") + " " + item;

                    var currentPlugin = GisEditor.LayerManager.GetPlugins().OfType<FeatureLayerPlugin>().Where(p => p.ExtensionFilter.ToLowerInvariant().Contains(Path.GetExtension(item).ToLowerInvariant())).FirstOrDefault();
                    if (currentPlugin != null)
                    {
                        fileDialog.Filter = currentPlugin.ExtensionFilter;
                    }

                    if (fileDialog.ShowDialog().GetValueOrDefault()) newDataFiles.Add(item, fileDialog.FileName);
                    else newDataFiles.Add(item, "");
                }
                zipFileAdapter.ExtractAll(tmpProjectFolder);

                Directory.SetCurrentDirectory(tmpProjectFolder);

                var allMapNames = XDocument.Load(zipFileAdapter.GetEntryStreamByName(projectName + ".proj")).Descendants("Document").Select(d => d.Attribute("Name").Value + ".tgmap");

                foreach (var map in allMapNames)
                {
                    if (zipFileAdapter.GetEntryNames().Contains(map))
                    {
                        var content = "";
                        using (StreamReader sr = new StreamReader(zipFileAdapter.GetEntryStreamByName(map)))
                        {
                            content = sr.ReadToEnd();
                        }

                        foreach (var pair in newDataFiles)
                        {
                            if (!string.IsNullOrEmpty(pair.Value))
                            {
                                var oldFileWithoutExtension = pair.Key.Remove(pair.Key.Length - 3, 3);
                                var newFileWithoutExtension = pair.Value.Remove(pair.Value.Length - 3, 3);

                                string oldRelativelyFileWithoutExtension = GetRelativelyDataFilePath(oldFileWithoutExtension);
                                if (content.Contains(oldFileWithoutExtension))
                                {
                                    content = content.Replace(oldFileWithoutExtension, newFileWithoutExtension);
                                }
                                else if (content.Contains(oldRelativelyFileWithoutExtension))
                                {
                                    content = content.Replace(oldRelativelyFileWithoutExtension, newFileWithoutExtension);
                                }

                                var oldFileTemp = oldFileWithoutExtension.Insert(oldFileWithoutExtension.Length - 1, "_tmp");
                                if (content.Contains(oldFileTemp)) content = content.Replace(oldFileTemp, newFileWithoutExtension);
                            }
                        }
                        var mapFilePath = Path.Combine(tmpProjectFolder, map);
                        using (StreamWriter sw = new StreamWriter(mapFilePath, false))
                        {
                            sw.Write(content);
                        }
                        zipFileAdapter.RemoveEntry(map);
                        zipFileAdapter.AddFileToZipFile(Path.GetFileName(mapFilePath));
                    }
                }

                Directory.SetCurrentDirectory(currentDirectory);
                zipFileAdapter.Save(projectFilePath);

                if (Directory.Exists(tmpProjectFolder))
                {
                    Directory.Delete(tmpProjectFolder, true);
                }
            }
        }

        private void LoadProjectSettings(string projectName, ZipFileAdapter zipFileAdapter)
        {
            if (zipFileAdapter.GetEntryNames().Contains(settingsFileName))
            {
                string tmpFilePath = projectSettingsPathFileName + ".tmp";
                if (File.Exists(tmpFilePath)) File.Delete(tmpFilePath);

                zipFileAdapter.ExtractEntryByName(settingsFileName, Path.GetDirectoryName(projectSettingsPathFileName));
                GisEditor.InfrastructureManager.ApplySettings(GetStorableSettings());
                if (File.Exists(projectSettingsPathFileName))
                {
                    File.Delete(projectSettingsPathFileName);
                }
            }
        }

        private static IEnumerable<IStorableSettings> GetStorableSettings()
        {
            var storableSettings = GisEditor.InfrastructureManager.GetManagers().Cast<IStorableSettings>()
            .Concat(GisEditor.InfrastructureManager.GetManagers().OfType<PluginManager>().SelectMany(m => m.GetPlugins()));
            return storableSettings;
        }

        private void LoadDocumentWindows(Uri projectUri, ZipFileAdapter zipFileAdapter)
        {
            string projFileName = Path.ChangeExtension(currentProjectPlugin.GetProjectShortName(projectUri), "proj");
            if (zipFileAdapter.GetEntryNames().Contains(projFileName))
            {
                var project = (from prj in XDocument.Load(zipFileAdapter.GetEntryStreamByName(projFileName)).Elements() //LoadXDocument(path).Elements()
                               select new
                               {
                                   //Name = prj.Attribute("Name").Value,
                                   Version = prj.Attribute("Version").Value,
                                   ActivateIndex = Int32.Parse(prj.Descendants("Documents").First().Attribute("ActivateIndex").Value),
                                   Documents = from document in prj.Descendants("Documents").First().Elements("Document")
                                               select new
                                               {
                                                   Title = document.Attribute("Title").Value,
                                                   Name = document.Attribute("Name").Value
                                               }
                               }).FirstOrDefault();

                if (project != null)
                {
                    currentProjectVersion = new Version(7, 0);
                    if (!string.IsNullOrEmpty(project.Version))
                    {
                        Version tempVersion = null;
                        if (Version.TryParse(project.Version, out tempVersion))
                        {
                            currentProjectVersion = tempVersion;
                        }
                    }

                    GisEditor.DockWindowManager.ActiveDocumentIndex = project.ActivateIndex;
                    foreach (var document in project.Documents)
                    {
                        DocumentWindow documentWindow = new DocumentWindow(null, document.Title, document.Name, true);
                        GisEditor.DockWindowManager.DocumentWindows.Add(documentWindow);
                    }
                }
            }
        }

        private void SaveMapsState(ProjectStreamInfo projectStreamInfo, XElement documentsElement, string fileName, string tempProjectFolder)
        {
            List<string> currentDataPathFileNames = new List<string>();

            foreach (var item in GisEditor.DockWindowManager.DocumentWindows)
            {
                GisEditorWpfMap wpfMap = item.Content as GisEditorWpfMap;
                if (wpfMap == null || !wpfMap.IsMapLoaded)
                {
                    wpfMap = deserializedMaps.FirstOrDefault(m => m.Name.Equals(item.Name));
                    item.Content = wpfMap;
                    wpfMap.Name = item.Title;
                }

                string mapFileName = Path.Combine(tempProjectFolder, String.Format(CultureInfo.InvariantCulture, @"{0}.tgmap", wpfMap.Name));
                SaveDocumentState(documentsElement, fileName, item);
                SaveMapState(item, mapFileName);
                CollectDataPathFileNames(currentDataPathFileNames, wpfMap);
            }

            if (currentDataPathFileNames.Count > 0)
            {
                string shapeFileConfigurePath = Path.Combine(tempProjectFolder, "DataFiles.txt");
                if (File.Exists(shapeFileConfigurePath))
                {
                    File.Delete(shapeFileConfigurePath);
                }

                File.WriteAllLines(shapeFileConfigurePath, currentDataPathFileNames);
            }
        }

        private void SavePreviewImage(string tempProjectFolder)
        {
            string imagePath = System.IO.Path.Combine(tempProjectFolder, "Preview.png");
            if (File.Exists(imagePath)) File.Delete(imagePath);

            LoadingPreviewImageProjectPluginManagerEventArgs args = new LoadingPreviewImageProjectPluginManagerEventArgs();
            OnLoadingPreviewImage(args);
            if (args.Image != null) args.Image.Save(imagePath);
        }

        private void AutoBackupTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                bool isBusy = GisEditor.GetMaps().Any(m => m.IsBusy);
                if (!isBusy)
                {
                    Backup("BackupProject");
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, invalidOperationException.Message, new ExceptionInfo(invalidOperationException));
            }
        }

        private void SaveProjectSettings()
        {
            var storableSettings = GisEditor.InfrastructureManager.GetManagers().Cast<IStorableSettings>()
                .Concat(GisEditor.InfrastructureManager.GetManagers()
                .OfType<PluginManager>().SelectMany(m => m.GetPlugins())
                .Where(p => p.IsActive));

            GisEditor.InfrastructureManager.SaveSettings(storableSettings);
        }

        private static void SaveDocumentState(XElement documentsElement, string fileName, DocumentWindow item)
        {
            XElement documentElement = new XElement("Document");
            documentElement.Add(new XAttribute("Title", item.Title));
            documentElement.Add(new XAttribute("Name", item.Name));
            documentsElement.Add(documentElement);
        }

        private static void SaveMapState(DocumentWindow item, string mapFileName)
        {
            WpfMap currentMap = item.Content as WpfMap;
            if (currentMap != null)
            {
                currentMap.Overlays.ForEach(o => o.Close());
                GisEditor.Serializer.Serialize(item.Content, mapFileName);
                currentMap.Overlays.ForEach(o => o.Open());
            }
        }

        /// <summary>
        /// Delete the extra files.
        /// Assume we have an project file that contains 3 docuemnts, we open it, delete one of the documents by
        /// using the document list, then we save the project file, and that is when this method will be called
        /// to deleted the extra files.
        /// </summary>
        private void CleanFiles(string projectFileName, string fileFolder, IEnumerable<DocumentWindow> documents)
        {
            DirectoryInfo directory = new DirectoryInfo(System.IO.Path.GetDirectoryName(fileFolder + @"\" + Path.GetFileNameWithoutExtension(projectFileName)));

            FileInfo[] files = directory.GetFiles("*.tgmap");

            foreach (FileInfo file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                if (!documents.Any(d => d.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                    || d.Title.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    file.Delete();
                }
            }
        }

        private void SaveProjectInformation(string path, XElement rootElement, XElement documentsElement)
        {
            rootElement.Add(documentsElement);
            rootElement.Save(Path.ChangeExtension(path, "proj"));
        }

        private static void CollectDataPathFileNames(List<string> layerFilePathList, WpfMap wpfMap)
        {
            var layers = wpfMap.Overlays.OfType<LayerOverlay>()
                .SelectMany(overlay => overlay.Layers.OfType<Layer>());

            foreach (var featureLayer in layers)
            {
                var plugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).Where(p => !string.IsNullOrEmpty(p.ExtensionFilter)).FirstOrDefault();

                if (plugin != null)
                {
                    var uri = plugin.GetUri(featureLayer);
                    if (uri != null && uri.Scheme.Equals("file") && (File.Exists(uri.OriginalString) || Directory.Exists(uri.OriginalString)))
                    {
                        if (!layerFilePathList.Contains(uri.OriginalString))
                        {
                            layerFilePathList.Add(uri.OriginalString);
                        }
                    }
                }
            }
        }

        private void CleanAutoBackTempFiles()
        {
            string backupDirectory = Directory.GetParent(GetBackupProjectFilePath(ProjectUri, "BackupProject")).FullName;
            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, true);
            }
        }

        private void CleanTempFiles()
        {
            CleanAutoBackTempFiles();

            string openBackupDirectory = Directory.GetParent(GetBackupProjectFilePath(ProjectUri, "OpenBackupProject")).FullName;
            if (Directory.Exists(openBackupDirectory))
            {
                Directory.Delete(openBackupDirectory, true);
            }

            string saveBackupDirectory = Directory.GetParent(GetBackupProjectFilePath(ProjectUri, "SaveBackupProject")).FullName;
            if (Directory.Exists(saveBackupDirectory))
            {
                Directory.Delete(saveBackupDirectory, true);
            }

            if (File.Exists(ProjectSettingsPathFileName))
            {
                File.Delete(ProjectSettingsPathFileName);
            }
        }

        private string GetBackupProjectFilePath(Uri projectUri, string schema)
        {
            string tempDirectory = Directory.GetParent(Path.GetDirectoryName(projectSettingsPathFileName)).FullName;
            string backupDirectory = Path.Combine(tempDirectory, schema);

            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            if (projectUri == null) return null;

            string fileName = Path.GetFileNameWithoutExtension(currentProjectPlugin.GetProjectShortName(projectUri));

            return Path.Combine(backupDirectory, fileName + ".tgproj");
        }

        private string GetAbsoluteDataFilePath(string path)
        {
            string result = path.Replace("./", Path.GetDirectoryName(ProjectUri.LocalPath) + "/");
            result = result.Replace(".\\", Path.GetDirectoryName(ProjectUri.LocalPath) + "\\");
            return result;
        }

        private string GetRelativelyDataFilePath(string path)
        {
            string result = path.Replace(Path.GetDirectoryName(ProjectUri.LocalPath) + "/", "./");
            result = result.Replace(Path.GetDirectoryName(ProjectUri.LocalPath) + "\\", ".\\");
            return result;
        }

        private string GetAbsoluteTempFilePath(string path)
        {
            return path.Replace("..\\", GetTemporaryFolder());
        }

        private static string GetTemporaryFolder()
        {
            string returnValue = string.Empty;
            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable(tempString);
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable("Tmp");
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = @"c:\MapSuiteTemp";
            }

            return returnValue + "\\";
        }

        private void ClearCaches()
        {
            try
            {
                var cachesFolder = GisEditor.InfrastructureManager.TemporaryPath + "\\TileCaches";

                if (Directory.Exists(cachesFolder))
                {
                    var directories = Directory.GetDirectories(cachesFolder);

                    foreach (var directory in directories)
                    {
                        if (directory.EndsWith("BingMap")
                            || directory.EndsWith("OpenStreetMap")
                            || directory.EndsWith("WorldMapKit"))
                            continue;

                        if (Directory.Exists(directory))
                        {
                            Directory.Delete(directory, true);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, exception.Message, new ExceptionInfo(exception));
            }
        }

        private void ResetProjectState()
        {
            var currentMaps = GisEditor.DockWindowManager.DocumentWindows.Select(w => w.Content).OfType<GisEditorWpfMap>().ToArray();
            GisEditor.UIManager.GetUIPlugins().ForEach(p =>
            {
                foreach (var currentMap in currentMaps)
                {
                    p.DetachMap(currentMap);
                }
            });

            if (GisEditor.ActiveMap != null)
            {
                foreach (var overlay in GisEditor.ActiveMap.Overlays)
                {
                    overlay.Close();
                }
                GisEditor.ActiveMap.ActiveLayer = null;
                GisEditor.ActiveMap.ActiveOverlay = null;
                GisEditor.ActiveMap = null;
            }
        }

        private void LoadPasswords(ZipFileAdapter zipFileAdapter, Uri projectUri)
        {
            string projectName = currentProjectPlugin.GetProjectShortName(projectUri);
            string projectNameWithExtension = Path.GetFileName(Path.ChangeExtension(projectName, "proj"));

            Stream stream = zipFileAdapter.GetEntryStreamByName(projectNameWithExtension);

            if (stream != null)
            {
                //TODO
                XDocument doucment = XDocument.Load(stream);
                XElement protectionElement = doucment.Descendants(protectedElementName).FirstOrDefault();

                if (protectionElement != null)
                {

                    XElement viewElement = protectionElement.Descendants("View").FirstOrDefault();
                    if (viewElement != null)
                    {
                        viewPassword = viewElement.Value;
                    }

                    XElement saveElement = protectionElement.Descendants("Save").FirstOrDefault();
                    if (saveElement != null)
                    {
                        savePassword = saveElement.Value;
                        if (!string.IsNullOrEmpty(savePassword) && savePassword != StringProtector.Instance.Empty)
                        {
                            projectReadWriteState = ProjectReadWriteMode.ReadOnly;
                        }
                    }
                }

                OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));
            }
        }

        private MemoryStream GetProtectProjectStream(ZipFileAdapter adapter, Uri projectUri)
        {
            MemoryStream stream = new MemoryStream();
            MemoryStream resultStream = new MemoryStream();

            try
            {
                string projectName = currentProjectPlugin.GetProjectShortName(projectUri);
                string projectNameWithExtension = Path.GetFileName(Path.ChangeExtension(projectName, "proj"));

                if (adapter.GetEntryNames().Contains(projectNameWithExtension))
                {
                    XDocument projDocument = XDocument.Load(adapter.GetEntryStreamByName(projectNameWithExtension));
                    XElement node = projDocument.Descendants("Project").FirstOrDefault();

                    XElement protectElement = node.Descendants(protectedElementName).FirstOrDefault();
                    if (protectElement == null)
                    {
                        XElement protectionElement = new XElement(protectedElementName);
                        protectionElement.Add(new XElement("View", viewPassword));
                        protectionElement.Add(new XElement("Save", savePassword));
                        node.Add(protectionElement);
                    }
                    else
                    {

                        string emptyPassword = StringProtector.Instance.Empty;

                        XElement viewElement = protectElement.Descendants("View").FirstOrDefault();
                        if (viewElement != null)
                        {
                            if (emptyPassword == viewPassword)
                            {
                                viewElement.Remove();
                            }
                            else
                            {
                                viewElement.Value = viewPassword;
                            }
                        }
                        else if (emptyPassword != viewPassword)
                        {
                            protectElement.Add(new XElement("View", viewPassword));
                        }

                        XElement saveElement = protectElement.Descendants("Save").FirstOrDefault();
                        if (saveElement != null)
                        {
                            if (emptyPassword == savePassword)
                            {
                                saveElement.Remove();
                            }
                            else
                            {
                                saveElement.Value = savePassword;
                            }
                        }
                        else if (emptyPassword != savePassword)
                        {
                            protectElement.Add(new XElement("Save", savePassword));
                        }
                    }

                    adapter.RemoveEntry(projectNameWithExtension);

                    projDocument.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    adapter.AddEntity(projectNameWithExtension, stream);
                    adapter.Save(resultStream);
                }

                resultStream.Seek(0, SeekOrigin.Begin);
                return resultStream;
            }
            finally
            {
                stream.Dispose();
                adapter.Dispose();
            }
        }

        private MemoryStream GetCleanPasswordsStream(Uri projectUri, Stream projectStream)
        {
            MemoryStream resultStream = new MemoryStream();
            MemoryStream resultDocumentStream = new MemoryStream();

            ZipFileAdapter adapter = ZipFileAdapterManager.CreateInstance(projectStream);
            string projectName = currentProjectPlugin.GetProjectShortName(projectUri);
            string projectNameWithExtension = Path.GetFileName(Path.ChangeExtension(projectName, "proj"));

            Stream stream = adapter.GetEntryStreamByName(projectNameWithExtension);
            XDocument document = XDocument.Load(stream);
            XElement projectElement = document.Descendants("Project").FirstOrDefault();
            XElement protectElement = projectElement.Descendants(protectedElementName).FirstOrDefault();
            if (protectElement != null) protectElement.Remove();
            document.Save(resultDocumentStream);
            resultDocumentStream.Seek(0, SeekOrigin.Begin);

            adapter.RemoveEntry(projectNameWithExtension);
            adapter.AddEntity(projectNameWithExtension, resultDocumentStream);
            adapter.Save(resultStream);

            string emptyPassword = StringProtector.Instance.Empty;

            savePassword = emptyPassword;
            viewPassword = emptyPassword;

            projectReadWriteState = ProjectReadWriteMode.ReadWrite;
            OnProjectStateChanged(new StateChangedProjectPluginManagerEventArgs(projectReadWriteState));

            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }

        private void SavePackageTempProject(Uri tempLocalProjectUri)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                var projectStreamInfo = new ProjectStreamInfo(tempLocalProjectUri, memoryStream);

                if (projectStreamInfo != null)
                {
                    SaveProject(projectStreamInfo);

                    if (projectStreamInfo.Stream.Length > 0)
                    {
                        projectStreamInfo.Stream.Seek(0, SeekOrigin.Begin);
                        CurrentProjectPlugin.SaveProjectStream(projectStreamInfo);
                    }
                }
            }
        }

        private static Collection<string> GetDataFilePaths(ZipFileAdapter zipfileAdapter)
        {
            Collection<string> dataPathFileNames = new Collection<string>();
            if (zipfileAdapter.GetEntryNames().Contains(dataFilesPathFileName))
            {
                Stream dataPathFileNamesStream = null;
                StreamReader reader = null;
                try
                {
                    dataPathFileNamesStream = zipfileAdapter.GetEntryStreamByName(dataFilesPathFileName);
                    reader = new StreamReader(dataPathFileNamesStream);
                    while (!reader.EndOfStream)
                    {
                        dataPathFileNames.Add(reader.ReadLine());
                    }
                }
                finally
                {
                    if (dataPathFileNamesStream != null)
                    {
                        dataPathFileNamesStream.Close();
                        dataPathFileNamesStream.Dispose();
                    }

                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
            }
            return dataPathFileNames;
        }

        private static int MoveDataFiles(int currentProgress, string path, PackProgressWindow packProgressWindow, Collection<string> dataPathFileNames)
        {
            foreach (var dataPathFileName in dataPathFileNames)
            {
                if (dataPathFileName.EndsWith(".gdb"))
                {
                    string[] files = Directory.GetFiles(dataPathFileName);

                    string targetPathDirName = Path.Combine(path, Path.GetFileName(dataPathFileName));

                    Directory.CreateDirectory(targetPathDirName);

                    foreach (var file in files)
                    {
                        var targetPathFileName = Path.Combine(targetPathDirName, Path.GetFileName(file));
                        File.Copy(file, targetPathFileName, true);
                        File.SetAttributes(targetPathFileName, FileAttributes.Normal);
                    }
                }
                else
                {
                    string[] files = Directory.GetFiles(Path.GetDirectoryName(dataPathFileName), Path.GetFileNameWithoutExtension(dataPathFileName) + ".*");

                    foreach (var file in files)
                    {
                        string targetPathFileName = Path.Combine(path, Path.GetFileName(file));
                        File.Copy(file, targetPathFileName, true);
                        File.SetAttributes(targetPathFileName, FileAttributes.Normal);
                    }
                }

                packProgressWindow.CurrentProgress = currentProgress;
                currentProgress++;
            }
            return currentProgress;
        }

        private void SavePackageResult(ProjectSaveAsResult saveAsResult, string zipFilePath, string zipFileNameWithOutExtention, string path)
        {
            using (var packZipFile = ZipFileAdapterManager.CreateInstance())
            {
                packZipFile.AddFileToZipFile(zipFilePath, string.Empty);
                packZipFile.AddDirectoryToZipFile(path, zipFileNameWithOutExtention + "_Data");

                MemoryStream resultStream = new MemoryStream();
                try
                {
                    packZipFile.Save(resultStream);
                    resultStream.Seek(0, SeekOrigin.Begin);

                    CurrentProjectPlugin.SaveProjectStream(new ProjectStreamInfo(saveAsResult.Uri, resultStream));
                }
                finally
                {
                    if (resultStream != null)
                    {
                        resultStream.Close();
                        resultStream.Dispose();
                    }
                }
            }
        }
    }
}