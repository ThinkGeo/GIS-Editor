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


using AvalonDock;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Windows.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for GisEditorUserControl.xaml
    /// </summary>
    public partial class GisEditorUserControl : UserControl
    {
        private static readonly int dpi = 96;
        private static readonly object lockLayoutInstance = new object();
        private static readonly string mapNamePattern = @"^[a-zA-Z_]\w*$";
        private static readonly string loggerFileFormat = "MapSuiteGisEditor{0}.log";

        private static string mapNameWarningMessage;

        private int documentsCount;
        private bool isCloseSilently;
        private bool doesLaunched;
        private string startupProjectPath;
        private ObservableCollection<DocumentContent> documentSource;
        private ObservableCollection<DockableContent> dockableContents;
        private Dictionary<UIPlugin, IEnumerable<DockableContentInfo>> dockableContentsInPlugins;
        private ObservableCollection<RecentProjectModel> recentProjectFiles;
        private DocumentContent previousDocumentContent;
        private Dictionary<DocumentContent, DocumentState> documentState;
        private DispatcherTimer projectBackupMessageUpdateTimer;
        private SolidColorBrush mapBackgroundBrush;
        private PointShape lastClickedPoint;
        private Point currentMouseScreenCoordinate;
        private StackPanel autoSavingStackPanel;
        private DockingStateManager dockingStateManager;
        private GeneralManager generalManager;
        private List<object> defaultMenuItems;
        private bool isClosingAllButThis;
        private Window window;
        private TextBlock timeTextBlock;
        private TextBlock autoSavingTextBlock;
        private Uri currentProjectUrl;
        private ProjectReadWriteMode currentProjectReadWriteMode;

        static GisEditorUserControl()
        {
            ThreadPool.SetMinThreads(50, 50);
            ThreadPool.SetMaxThreads(100, 100);

            transparentBrush = new SolidColorBrush(Colors.Transparent);
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 183, 0));

            highlightBrush = new LinearGradientBrush();
            highlightBrush.StartPoint = new Point(0, 0);
            highlightBrush.EndPoint = new Point(0, 1);
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(254, 251, 244), 0));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 231, 206), 0.19));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 222, 184), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 206, 107), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 222, 154), 0.79));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 235, 170), 1));

            // BaseShape.GeometryLibrary = GeometryLibrary.Managed;
            // Feature.GeometryLibrary = GeometryLibrary.Managed;
        }

        public GisEditorUserControl()
            : this(string.Empty)
        {
        }

        public GisEditorUserControl(string startupProjectPath)
        {
            StartupProjectPath = startupProjectPath;
            InitializeComponent();
            currentProjectReadWriteMode = ProjectReadWriteMode.ReadWrite;
            mapNameWarningMessage = GisEditor.LanguageManager.GetStringResource("MapNameRequirementsText") + Environment.NewLine
            + "   " + GisEditor.LanguageManager.GetStringResource("MapNameRequirements1StartWithText") + Environment.NewLine
            + "   " + GisEditor.LanguageManager.GetStringResource("MapNameRequirements2DuplicateText") + Environment.NewLine
            + "   " + GisEditor.LanguageManager.GetStringResource("MapNameRequirements3TwoConsecutiveText");
        }

        public string StartupProjectPath
        {
            get { return startupProjectPath; }
            set { startupProjectPath = value; }
        }

        private MouseCoordinateType MouseCoordinateType
        {
            get
            {
                if (generalManager == null)
                {
                    generalManager = GisEditorHelper.GetManagers().OfType<GeneralManager>().FirstOrDefault();
                }

                if (generalManager != null) return generalManager.MouseCoordinateType;
                else return MouseCoordinateType.LongitudeLatitude;
            }
        }

        [Obfuscation]
        internal void ApplicationMenu_DropDownOpened(object sender, EventArgs e)
        {
            InitializeApplicationMenuItems();
        }

        public void AddDocument()
        {
            documentsCount++;

            string documentTitle = CreateDocumentTitle();
            GisEditorWpfMap map = CreateNewMap(documentTitle);

            DocumentWindow newDocumentWindow = new DocumentWindow(map, documentTitle, documentTitle.Replace(" ", string.Empty).Trim(), true);
            GisEditor.DockWindowManager.DocumentWindows.Add(newDocumentWindow);

            if (GisEditor.ActiveMap != null)
            {
                GisEditor.ActiveMap.ActiveOverlay = null;
                GisEditor.ActiveMap.ActiveLayer = null;
            }

            GisEditor.ActiveMap = newDocumentWindow.Content as GisEditorWpfMap;
            GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => p.AttachMap(map));
            GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.AddDocumentDescription));

            var newDocument = documentSource.FirstOrDefault(d => d.Content == newDocumentWindow.Content);
            if (newDocument != null)
            {
                previousDocumentContent = newDocument;
                newDocument.Focus();
            }
        }

        public void OpenProject()
        {
            OpenProject(null);
        }

        public void OpenProject(Uri projectUri)
        {
            ProjectStreamInfo tempInfo = new ProjectStreamInfo();
            tempInfo.Uri = projectUri;
            bool canOpenProject = GisEditor.ProjectManager.CanOpenProject(tempInfo);

            if (!canOpenProject) return;

            if (projectUri != null)
            {
                GisEditor.ProjectManager.OpenProject(projectUri);
            }
            else
            {
                //var projectInfo = new ProjectStreamInfo();
                //if (GisEditor.ProjectManager.CurrentProjectPlugin != null) GisEditor.ProjectManager.CurrentProjectPlugin.LoadProjectStream(projectInfo);

                if (tempInfo.Uri == null) return;
                if (!CheckCurrentProject(tempInfo.Uri)) return;

                if (GisEditor.ProjectManager.IsLoaded
                    && GisEditor.ProjectManager.ProjectUri != null)
                {
                    //ResetProjectState();
                    GisEditor.ProjectManager.CloseProject();
                }

                GisEditor.ProjectManager.OpenProject(tempInfo);
            }

            RefreshRencentList();
        }

        public void CreateNewProject()
        {
            if (GisEditor.ProjectManager.IsLoaded)
            {
                if (RemindUserToSave())
                {
                    return;
                }
                GisEditor.ProjectManager.CloseProject();
                DocumentsListBox.ItemsSource = null;
            }

            //ResetProjectState();
            GisEditor.ProjectManager.OpenProject(new Uri("giseditor:blank"));
            AddDocument();
            DocumentsListBox.ItemsSource = documentSource;

            ResetMapsState();

            foreach (var file in recentProjectFiles)
            {
                file.IsEnabled = true;
            }

#if Evaluation
            switch (status)
            {
                case InstallerStatus.EvaluationWithKeyRun:
                case InstallerStatus.EvaluationWithKeyDebug:
                    this.Title = String.Format(CultureInfo.InvariantCulture, "ThinkGeo Map Suite GIS Editor Evaluation - Day {0} of 60", 60 - InstallerChecker.GetEvalDays());
                    break;

                default:
                    this.Title = "ThinkGeo Map Suite GIS Editor";
                    break;
            }
#endif
        }

        public void RenameDocument(MenuItem menuItem)
        {
            RenameMap(menuItem.DataContext as DocumentContent);
        }

        public void SaveProject()
        {
            SaveProject(GisEditor.ProjectManager.ProjectUri);
        }

        public void SaveProject(Uri projectUri)
        {
            try
            {
                GisEditor.DockWindowManager.ActiveDocumentIndex = DockManager.MainDocumentPane.SelectedIndex;
                SaveProjectFile(projectUri);
                GisEditor.InfrastructureManager.SaveSettings(dockingStateManager);
                ResetMapsState();
            }
            catch (InvalidOperationException ex)
            {
                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                messageBox.Title = GisEditor.LanguageManager.GetStringResource("PrintErrorWarningCaption");
                messageBox.Message = GisEditor.LanguageManager.GetStringResource("SavingProjectFailedText");
                messageBox.ErrorMessage = ex.Message;
                messageBox.ShowDialog();
            }
        }

        public void OpenPluginManagerDialog()
        {
            GeoCollection<UIPlugin> previousPlugins = new GeoCollection<UIPlugin>();
            GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => previousPlugins.Add(p.Id, p));
            var pluginManagerWindow = new PluginManagerWindow();
            if (pluginManagerWindow.ShowDialog().GetValueOrDefault())
            {
                var newPlugins = new GeoCollection<UIPlugin>();
                GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => newPlugins.Add(p.Id, p));
                ApplyEnabledPlugins(newPlugins, previousPlugins);

                if (previousPlugins.Count != newPlugins.Count) GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.OpenPluginManagerDialogDescription));
            }
            NewPluginsDetected();
        }

        public void DeleteSelectDocument()
        {
            if (DocumentsListBox.SelectedItem != null)
            {
                var result = PromptRemovingDocument();
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    var removingDocumentContent = (DocumentContent)DocumentsListBox.SelectedItem;
                    var removingDocumentWindow = GisEditor.DockWindowManager.DocumentWindows.FirstOrDefault(d => d.Content == removingDocumentContent.Content);
                    if (removingDocumentWindow != null)
                    {
                        GisEditor.DockWindowManager.DocumentWindows.Remove(removingDocumentWindow);
                    }

                    if (documentSource.Contains(removingDocumentContent))
                    {
                        documentSource.Remove(removingDocumentContent);
                    }
                }
            }
        }

        public void ProcessMapContextMenu(string name)
        {
            if (name == "PreviousExtent")
            {
                GisEditor.ActiveMap.ZoomToPreviousExtent();
            }
            else if (name == "NextExtent")
            {
                GisEditor.ActiveMap.ZoomToNextExtent();
            }
            else if (name == "ZoomIn")
            {
                GisEditor.ActiveMap.ZoomIn();
            }
            else if (name == "ZoomOut")
            {
                GisEditor.ActiveMap.ZoomOut();
            }
            else if (name == "CenterAt" && lastClickedPoint != null)
            {
                GisEditor.ActiveMap.CenterAt(lastClickedPoint);
            }
            else if (name == "AddDocument")
            {
                AddDocument();
            }
            else if (name == "RenameDocument")
            {
                RenameMap((DocumentContent)GisEditor.ActiveMap.Parent);
            }
        }

        public void OpenRecentProjectFile(Uri projectUri)
        {
            ProjectStreamInfo tempInfo = new ProjectStreamInfo();
            tempInfo.Uri = projectUri;
            bool canOpenProject = GisEditor.ProjectManager.CanOpenProject(tempInfo);

            if (!canOpenProject) return;

            if (projectUri != null)
            {
                if (ExitIfProjectIsOpened(projectUri)) return;

                var foundRecords = recentProjectFiles.FirstOrDefault(r => r.FullPath.AbsolutePath.Equals(projectUri.AbsolutePath, StringComparison.Ordinal));
                var currentPlugin = GisEditor.ProjectManager.GetPlugins().OfType<ProjectPlugin>().FirstOrDefault(p => p.GetType().Name.Equals(foundRecords.ProjectPluginType, StringComparison.Ordinal));
                if (currentPlugin != null)
                {
                    if (ExitIfProjectNotExist(projectUri))
                    {
                        return;
                    }

                    GisEditor.ProjectManager.CurrentProjectPlugin = currentPlugin;
                }

                if (!CheckCurrentProject(projectUri)) return;

                if (GisEditor.ProjectManager.IsLoaded
                    && GisEditor.ProjectManager.ProjectUri != null)
                {
                    //ResetProjectState();
                    GisEditor.ProjectManager.CloseProject();
                }

                GisEditor.ProjectManager.OpenProject(projectUri);

                RefreshRencentList();
            }
        }

        public void Load()
        {
            LoadCore();
        }

        protected virtual void LoadCore()
        {
            Window window = GetOwnerWindow();
            window.Closed += new EventHandler(Window_Closed);
            window.Closing += new CancelEventHandler(Window_Closing);

            InitializeLayoutBackupTimer();
            InitializeDefaultVariables();
            GisEditor.InfrastructureManager.ApplySettings(GisEditorHelper.GetManagers());
            TileOverlayExtension.TemporaryPath = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "TileCaches");
            InitializeRecentFileList();
            dockableContents.Add(DocumentsList);
            GisEditor.DockWindowManager.DockWindows.Add(new DockWindow(DocumentsList.Content as Control));

            DockManager.DocumentsSource = documentSource;
            DocumentsListBox.ItemsSource = documentSource;
            ltb.ItemsSource = dockableContents.Select(d => new DockWindowViewModel(d));
            //OpenDockWindowButton.ItemsSource = dockableContents;
            DockManager.DocumentPaneAdded += (s, arg) => { HookDocumentPaneEvents(arg); };
            DockManager.DocumentClosing += new EventHandler<CancelEventArgs>(DockManager_DocumentClosing);
            DocumentPane.ClosingAllButThis = OnClosingAllButThis;
            DocumentPane.ClosedAllButThis = OnClosedAllButThis;

            //licenseEntity = new LicenseEntity();
            //MaskPanel.DataContext = licenseEntity;

            InitializeProjectPlugins();
            InitializeUIPlugins();
            OpenLatestFailedProjectIfExists();

            //AppHelper.SplashScreen.Close(TimeSpan.FromMilliseconds(500));
            generalManager = GisEditorHelper.GetManagers().OfType<GeneralManager>().FirstOrDefault();

            GisEditorCommands.GisEditorUserControl = this;
        }

        private void RenameMap(DocumentContent documentContent)
        {
            RenameMapWindow renameWindow = new RenameMapWindow(documentContent.Title);

            if (renameWindow.ShowDialog().GetValueOrDefault())
            {
                string newTitle = renameWindow.NewMapName;

                if (newTitle != null)
                {
                    documentContent.Title = newTitle;
                    var map = documentContent.Content as WpfMap;
                    if (map != null)
                    {
                        map.Name = newTitle;
                    }

                    DocumentWindow docWin = GisEditor.DockWindowManager.DocumentWindows.FirstOrDefault(d => d.Content == documentContent.Content);
                    if (docWin != null)
                    {
                        docWin.Title = newTitle;
                        docWin.Name = newTitle;
                    }
                }
            }
        }

        private bool ExitIfProjectNotExist(Uri projectUri)
        {
            if (projectUri != null && !GisEditor.ProjectManager.ProjectExists(projectUri) && !projectUri.AbsolutePath.Equals("blank", StringComparison.OrdinalIgnoreCase))
            {
                var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlDoesntExistsMessage"), GisEditor.LanguageManager.GetStringResource("MapSuiteGisEditorCaption"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Asterisk);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    var obsoletedProjectFile = recentProjectFiles.FirstOrDefault(r => r.FullPath.AbsolutePath.Equals(projectUri.AbsolutePath, StringComparison.OrdinalIgnoreCase));
                    if (obsoletedProjectFile != null)
                    {
                        recentProjectFiles.Remove(obsoletedProjectFile);
                        ReorderRecentProjectIndexes();
                    }
                    GisEditorHelper.SaveRecentFiles(recentProjectFiles);
                }

                return true;
            }
            else return false;
        }

        private bool ExitIfProjectIsOpened(Uri projectUri)
        {
            if (GisEditor.ProjectManager.IsLoaded
                && GisEditor.ProjectManager.ProjectUri != null
                && !GisEditor.ProjectManager.ProjectUri.LocalPath.Equals("blank", StringComparison.OrdinalIgnoreCase)
                && projectUri.AbsolutePath.Equals(GisEditor.ProjectManager.ProjectUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlAlreadyOpenMessage"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return true;
            }
            else return false;
        }

        private bool ExitIfCancelToSavePrj(Uri projectUri)
        {
            if (GisEditor.ProjectManager.IsLoaded
                && GisEditor.ProjectManager.ProjectUri != null
                && !projectUri.AbsolutePath.Equals(GisEditor.ProjectManager.ProjectUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            {
                if (RemindUserToSave())
                {
                    return true;
                }
            }

            return false;
        }

        private void InitializeApplicationMenuItems()
        {
            IEnumerable<RibbonApplicationMenuItem> menuItems = GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>()
                .SelectMany(p => p.ApplicationMenuItems);

            bool startRemoving = false;
            int insertPosition = 0;
            for (int i = ribbonContainer.ApplicationMenu.Items.Count - 1; i >= 0; i--)
            {
                var currentItem = ribbonContainer.ApplicationMenu.Items.OfType<FrameworkElement>().ElementAt(i);

                if (currentItem.Tag != null && currentItem.Tag.Equals("1"))
                {
                    startRemoving = true;
                }
                else if (currentItem.Tag != null && currentItem.Tag.Equals("0"))
                {
                    startRemoving = false;
                    insertPosition = ribbonContainer.ApplicationMenu.Items.IndexOf(currentItem) + 1;
                }
                else if (startRemoving)
                {
                    ribbonContainer.ApplicationMenu.Items.Remove(currentItem);
                }
            }

            menuItems.ForEach((menuItem) =>
            {
                menuItem.SetValue(RibbonExtension.IsAppliedUnderscoreProperty, true);
                ribbonContainer.ApplicationMenu.Items.Insert(insertPosition, menuItem);
            });
        }

        private GisEditorWpfMap CreateNewMap(string name = null)
        {
            GisEditorWpfMap map = new GisEditorWpfMap(name == null ? string.Empty : name);
            if (generalManager != null && generalManager.Scales.Count > 0)
            {
                map.ZoomLevelSet.CustomZoomLevels.Clear();
                foreach (var scale in generalManager.Scales)
                {
                    map.ZoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(scale));
                }
            }
            InitializeMapProperties(map);
            InitializeMapEvents(map);
            InitializePanZoomBar(map, GetShowPanZoomBar());
            return map;
        }

        private string CreateDocumentTitle()
        {
            int index = 1;
            string documentName = String.Format(CultureInfo.InvariantCulture, "Map_{0}", index);
            while (GisEditor.DockWindowManager.DocumentWindows.Any(d => d.Title == documentName))
            {
                documentName = String.Format(CultureInfo.InvariantCulture, "Map_{0}", ++index);
            }
            return documentName;
        }

        private void NewPluginsDetected()
        {
            string directoryPath = GetAssemblyLocation() + "\\Plugins\\Upgrade\\";
            if (Directory.Exists(directoryPath) && Directory.GetFiles(directoryPath, "*.tmp").Length > 0)
            {
                if (MessageBoxResult.Yes == MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NewPluginsDetectedMessageBoxContent"), "MapSuite GisEditor restart", MessageBoxButton.YesNo))
                {
                    Process.Start("RestartTool.exe");
                    isCloseSilently = true;

                    GetOwnerWindow().Close();
                }
            }
        }

        internal void ApplyEnabledPlugins(GeoCollection<UIPlugin> newPlugins, GeoCollection<UIPlugin> previousPlugins)
        {
            Collection<string> previousPluginKeys = previousPlugins.GetKeys();
            foreach (string privousPluginKey in previousPluginKeys)
            {
                if (!newPlugins.Contains(privousPluginKey))
                {
                    UIPlugin deletedPlugin = previousPlugins[privousPluginKey];
                    DestroyRibbonGroups(deletedPlugin.RibbonEntries);
                    DestroyDockableContents(CollectDockableContentsInPlugin(deletedPlugin));
                    DestroyStatusBarItems(deletedPlugin.StatusBarItems);
                    GisEditor.GetMaps().ForEach(m => deletedPlugin.DetachMap(m));
                    deletedPlugin.Unload();
                }
            }

            Collection<string> currentPluginKeys = newPlugins.GetKeys();
            Collection<UIPlugin> pluginsToBuild = new Collection<UIPlugin>();
            foreach (string currentPluginKey in currentPluginKeys)
            {
                if (!previousPluginKeys.Contains(currentPluginKey))
                {
                    UIPlugin newPlugin = newPlugins[currentPluginKey];
                    newPlugin.Load();
                    pluginsToBuild.Add(newPlugin);
                    GisEditor.GetMaps().ForEach(m => newPlugin.AttachMap(m));
                    InitializeDockableContent(CollectDockableContentsInPlugin(newPlugin));
                    InitializeStatusBarItems(newPlugin.StatusBarItems);
                }
            }

            GisEditor.UIManager.BuildRibbonBar(ribbonContainer, pluginsToBuild);
        }

        private Collection<DockableContentInfo> CollectDockableContentsInPlugin(UIPlugin plugin)
        {
            if (!dockableContentsInPlugins.ContainsKey(plugin))
            {
                var pluginContents = (from content in plugin.DockWindows
                                      select new DockableContentInfo
                                      {
                                          StartupMode = content.StartupMode,
                                          DockWindow = content
                                      }).ToList();

                dockableContentsInPlugins.Add(plugin, pluginContents);
            }

            Collection<DockableContentInfo> resultPluginContents = new Collection<DockableContentInfo>();
            foreach (var pluginContent in dockableContentsInPlugins[plugin])
            {
                resultPluginContents.Add(pluginContent);
            }

            return resultPluginContents;
        }

        private DockableContent DockWindowToDockableContent(DockWindow dockWindow)
        {
            DockableContent dockableContent = new DockableContent();
            if (!dockWindow.Name.Equals("--"))
            {
                dockableContent.Name = dockWindow.Name;
            }
            SetDockableContentTitle(dockableContent, dockWindow.Title);

            DockableContent obsoletedParent = dockWindow.Content.Parent as DockableContent;
            if (obsoletedParent != null) obsoletedParent.Content = null;

            dockableContent.Content = dockWindow.Content;
            dockableContent.DockPosition = ConvertToDockPosition(dockWindow.Position);
            return dockableContent;
        }

        private void InitializeDefaultVariables()
        {
            defaultMenuItems = GetMenuItems().ToList();
            dockableContents = new ObservableCollection<DockableContent>();
            documentSource = new ObservableCollection<DocumentContent>();
            autoSavingStackPanel = new StackPanel();
            autoSavingStackPanel.Orientation = Orientation.Horizontal;
            dockingStateManager = new DockingStateManager(DockManager, this);
            mapBackgroundBrush = new SolidColorBrush(Colors.White);
            documentState = new Dictionary<DocumentContent, DocumentState>();
            dockableContentsInPlugins = new Dictionary<UIPlugin, IEnumerable<DockableContentInfo>>();

            documentSource.CollectionChanged += new NotifyCollectionChangedEventHandler(DocumentSource_CollectionChanged);
            ribbonContainer.ApplicationMenu.DropDownOpened += new EventHandler(ApplicationMenu_DropDownOpened);
            GisEditor.DockWindowManager.DockWindowOpened += new EventHandler<DockWindowOpenedDockWindowManagerEventArgs>(LayoutManager_OpenedDockWindow);
            GisEditor.DockWindowManager.ThemeChanged += new EventHandler<ThemeChangedDockWindowManagerEventArgs>(LayoutManager_ThemeChanged);
            GisEditor.ProjectManager.ProjectUriChanged += new EventHandler<UriChangedProjectPluginManagerEventArgs>(ProjectFileManager_UriChanged);
            GisEditor.ProjectManager.ProjectStateChanged += new EventHandler<StateChangedProjectPluginManagerEventArgs>(ProjectManager_ProjectStateChanged);
            GisEditor.ProjectManager.LoadingPreviewImage += new EventHandler<LoadingPreviewImageProjectPluginManagerEventArgs>(ProjectFileManager_PreviewImageFetch);
            GisEditor.ProjectManager.Opened += new EventHandler<OpenedProjectManagerEventArgs>(ProjectFileManager_Opened);
            GisEditor.ProjectManager.Closed += new EventHandler<EventArgs>(ProjectManager_Closed);
            GisEditor.ProjectManager.Closing += new EventHandler<EventArgs>(ProjectManager_Closing);
            GisEditor.ProjectManager.AutoBackupIntervalChanged += new EventHandler<AutoBackupIntervalChangedProjectPluginManagerEventArgs>(ProjectManager_AutoBackupIntervalChanged);
            GisEditor.ProjectManager.CanAutoBackupChanged += new EventHandler<CanAutoBackupChangedProjectPluginManagerEventArgs>(ProjectManager_CanAutoBackupChanged);
            GisEditor.ProjectManager.Opening += new EventHandler<OpeningProjectManagerEventArgs>(ProjectManager_Opening);
            GisEditor.DockWindowManager.DockWindows.CollectionChanged += new NotifyCollectionChangedEventHandler(DockWindows_CollectionChanged);
            GisEditor.DockWindowManager.DocumentWindows.CollectionChanged += new NotifyCollectionChangedEventHandler(DocumentWindows_CollectionChanged);
        }

        [Obfuscation]
        private void DocumentSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (documentSource.Count == 0)
            {
                if (GisEditor.ActiveMap != null)
                {
                    var emptyMap = new GisEditorWpfMap();
                    GisEditor.ActiveMap.ActiveLayer = null;
                    GisEditor.ActiveMap.ActiveOverlay = null;
                    GisEditor.ActiveMap = emptyMap;

                    GisEditor.InfrastructureManager.ApplySettings(GisEditor.UIManager.GetPlugins());
                    GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => p.Refresh(null));
                    GisEditor.ActiveMap = null;
                }
            }
        }

        [Obfuscation]
        private void DocumentWindows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<DocumentWindow>())
                    {
                        if (item.Content == null || documentSource.All(tmpDocument => tmpDocument.Content != item.Content))
                        {
                            DocumentContent docContent = new DocumentContent()
                            {
                                Title = item.Title,
                                IsFloatingAllowed = item.CanFloat,
                                Name = item.Name,

                                //Tag = item.Tag,
                                Content = item.Content
                            };

                            ResetLayoutIfInvalid();
                            documentSource.Add(docContent);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.OfType<DocumentWindow>())
                    {
                        var deletingDocumentContent = documentSource.FirstOrDefault(tmpDocument => tmpDocument.Content == item.Content);
                        if (deletingDocumentContent != null)
                        {
                            documentSource.Remove(deletingDocumentContent);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    for (int i = documentSource.Count - 1; i >= 0; i--)
                    {
                        documentSource.RemoveAt(i);
                    }
                    break;
            }
        }

        private void ResetLayoutIfInvalid()
        {
            if (defaultPane.GetManager() == null)
            {
                GisEditor.InfrastructureManager.ApplySettings(dockingStateManager);
                defaultPane = DockManager.MainDocumentPane;
            }
        }

        [Obfuscation]
        private void DockWindows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DockableContent currentDockableContent = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<DockWindow>())
                    {
                        if (dockableContents.All(tmpDockableContent => item.Content != tmpDockableContent.Content))
                        {
                            currentDockableContent = ConvertToDockableContent(item);
                            dockableContents.Add(currentDockableContent);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.OfType<DockWindow>())
                    {
                        if (dockableContents.Any(tmpDockableContent => item.Content == tmpDockableContent.Content))
                        {
                            currentDockableContent = ConvertToDockableContent(item);
                            dockableContents.Remove(currentDockableContent);
                            currentDockableContent.Close();
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    dockableContents.Clear();
                    break;

                default:
                    break;
            }

            var sortedDockWindows = GisEditor.DockWindowManager.GetSortedDockWindows();

            ObservableCollection<DockableContent> dockableContentsForDataBinding = new ObservableCollection<DockableContent>();
            foreach (var dockWindow in sortedDockWindows)
            {
                var resultDockableContent = dockableContents.FirstOrDefault(d => d.Content == dockWindow.Content);
                if (resultDockableContent != null)
                {
                    dockableContentsForDataBinding.Add(resultDockableContent);
                }
                else
                {
                    dockableContentsForDataBinding.Add(ConvertToDockableContent(dockWindow));
                }
            }
            ltb.ItemsSource = dockableContentsForDataBinding.Select(d => new DockWindowViewModel(d)).ToList();
        }

        [Obfuscation]
        private void LayoutManager_OpenedDockWindow(object sender, DockWindowOpenedDockWindowManagerEventArgs e)
        {
            if (GisEditor.DockWindowManager.DockWindows.All(dc => dc.Content != e.DockWindow.Content))
            {
                GisEditor.DockWindowManager.DockWindows.Add(e.DockWindow);
            }

            DockableContent dockableContent = ConvertToDockableContent(e.DockWindow);
            dockableContent.StateChanged -= DockableContentState_Changed;
            dockableContent.StateChanged += DockableContentState_Changed;
            switch (e.DockWindowPosition)
            {
                case DockWindowPosition.Default:
                    if (dockableContent.State == DockableContentState.AutoHide)
                    {
                        dockableContent.Activate();
                    }
                    else if (dockableContent.State == DockableContentState.Hidden || dockableContent.State == DockableContentState.None)
                    {
                        dockableContent.ResetSavedStateAndPosition();
                        dockableContent.Show(DockManager);
                    }
                    dockableContent.Focus();
                    break;

                case DockWindowPosition.Left:
                    dockableContent.Show(DockManager, AnchorStyle.Left);
                    break;

                case DockWindowPosition.Bottom:
                    dockableContent.Show(DockManager, AnchorStyle.Bottom);
                    break;

                case DockWindowPosition.Right:
                    dockableContent.Show(DockManager, AnchorStyle.Right);
                    break;

                case DockWindowPosition.Floating:
                    dockableContent.FloatingStartupLocation = WindowStartupLocation.CenterOwner;
                    dockableContent.Show(DockManager);
                    dockableContent.ShowAsFloatingWindow(DockManager, true);
                    break;

                default:
                    dockableContent.Show(DockManager, AnchorStyle.Right);
                    break;
            }
        }

        [Obfuscation]
        private void LayoutManager_ThemeChanged(object sender, ThemeChangedDockWindowManagerEventArgs e)
        {
            ApplyTheme(e.NewTheme);
        }

        [Obfuscation]
        private void ExplorerShell_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }

        private void ApplyTheme(Theme newTheme)
        {
            ribbonContainer.Resources.MergedDictionaries.Clear();
            switch (newTheme)
            {
                case Theme.Dark:
                    ThemeFactory.ChangeTheme(new Uri("/AvalonDock.Themes;component/themes/ExpressionDark.xaml", UriKind.RelativeOrAbsolute));
                    break;

                case Theme.Blue:
                    ThemeFactory.ChangeTheme("aero.normalcolor");
                    break;

                case Theme.Silver:
                    ThemeFactory.ChangeTheme(new Uri("/AvalonDock.Themes;component/themes/dev2010.xaml", UriKind.RelativeOrAbsolute));
                    break;

                case Theme.Win7:
                    ThemeFactory.ChangeTheme("generic");
                    break;

                default:
                    break;
            }
        }

        private DockableContent ConvertToDockableContent(DockWindow dockWindow)
        {
            IEnumerable<DockableContent> resultDockableContent = dockableContents;
            if (dockableContentsInPlugins != null)
            {
                var dockableContentsFromPlugins = dockableContentsInPlugins.Values.SelectMany(v => v).Select(d => d.DockWindow);
                resultDockableContent = resultDockableContent.Concat(dockableContentsFromPlugins.Select(d => DockableContentInfo.ToDockableContent(d)));
            }

            foreach (var item in resultDockableContent)
            {
                if (item.Content == dockWindow.Content)
                {
                    if (!dockWindow.Name.Equals("--"))
                    {
                        item.Name = dockWindow.Name;
                    }
                    SetDockableContentTitle(item, dockWindow.Title);
                    item.DockPosition = ConvertToDockPosition(dockWindow.Position);
                    return item;
                }
            }

            DockableContent newContent = DockWindowToDockableContent(dockWindow);
            newContent.FloatingWindowSize = dockWindow.FloatingSize;
            newContent.FloatingStartupLocation = dockWindow.FloatingLocation;
            return newContent;
        }

        private static void SetDockableContentTitle(DockableContent item, string titleKey)
        {
            string title = GisEditor.LanguageManager.GetStringResource(titleKey);
            if (!string.IsNullOrEmpty(title))
            {
                item.SetResourceReference(DockableContent.TitleProperty, titleKey);
            }
            else
            {
                item.Title = titleKey;
            }
        }

        private void InitializeLayoutBackupTimer()
        {
            projectBackupMessageUpdateTimer = new DispatcherTimer(DispatcherPriority.Background);
            projectBackupMessageUpdateTimer.Interval = new TimeSpan(0, 10, 0);
            projectBackupMessageUpdateTimer.Tick += new EventHandler(ProjectBackupMessageUpdateTimer_Tick);
            StartBackupProjectTimer(StartBackupMode.Start);
        }

        private void InitializeRecentFileList()
        {
            recentProjectFiles = GisEditorHelper.GetRecentFileList();
            Dispatcher.BeginInvoke(() => ribbonContainer.ApplicationMenu.DataContext = recentProjectFiles);
        }

        private void InitializeUIPlugins()
        {
            var uiPluginManager = GisEditor.UIManager;
            var settingsManager = GisEditor.InfrastructureManager;

            settingsManager.ApplySettings(uiPluginManager);
            uiPluginManager.GetPlugins().ForEach(p => p.Load());

            var uiPlugins = uiPluginManager.GetActiveUIPlugins();
            Collection<UIPlugin> pluginsToBuild = new Collection<UIPlugin>();
            foreach (UIPlugin plugin in uiPlugins)
            {
                pluginsToBuild.Add(plugin);
                InitializeDockableContent(CollectDockableContentsInPlugin(plugin));
                InitializeStatusBarItems(plugin.StatusBarItems);
            }

            GisEditor.UIManager.BuildRibbonBar(ribbonContainer, pluginsToBuild);
            InitializeApplicationMenuItems();
        }

        private RibbonApplicationMenuItem GetApplicationMenuItem(string uid)
        {
            return ribbonContainer.ApplicationMenu.Items.OfType<RibbonApplicationMenuItem>().Where(i =>
                        i.Uid.Equals(uid, StringComparison.Ordinal)).First();
        }

        private RibbonApplicationMenuItem GetApplicationMenuItemByPlugin(ProjectPlugin projectPlugin)
        {
            return new RibbonApplicationMenuItem()
            {
                Header = projectPlugin.Name,
                ImageSource = projectPlugin.GetOpenProjectIcon(),
                ToolTipDescription = projectPlugin.Description,
                ToolTipTitle = projectPlugin.Description,
                Tag = projectPlugin
            };
        }

        private void InitializeProjectPlugins()
        {
            var plugins = GisEditor.ProjectManager.GetPlugins();
            var openMenuItem = GetApplicationMenuItem("OpenProject");
            var saveMenuItem = GetApplicationMenuItem("SaveProject");

            if (plugins.Count < 1)
            {
                openMenuItem.IsEnabled = false;
                saveMenuItem.IsEnabled = false;
            }
            else if (plugins.Count > 1)
            {
                List<RibbonApplicationMenuItem> openMenuItems = new List<RibbonApplicationMenuItem>();
                List<RibbonApplicationMenuItem> saveMenuItems = new List<RibbonApplicationMenuItem>();

                plugins.Cast<ProjectPlugin>().ForEach(plugin =>
                {
                    RibbonApplicationMenuItem subOpenMenu = GetApplicationMenuItemByPlugin(plugin);
                    subOpenMenu.Command = new RelayCommand(() =>
                    {
                        if (subOpenMenu.Tag != null)
                        {
                            var currentPlugin = subOpenMenu.Tag as ProjectPlugin;
                            GisEditor.ProjectManager.CurrentProjectPlugin = currentPlugin;
                            OpenProject(null);
                        }
                    });

                    RibbonApplicationMenuItem subSaveMenu = GetApplicationMenuItemByPlugin(plugin);
                    subSaveMenu.ImageSource = plugin.GetSaveProjectIcon();
                    subSaveMenu.Command = new RelayCommand(() =>
                    {
                        if (subSaveMenu.Tag != null)
                        {
                            var currentPlugin = subSaveMenu.Tag as ProjectPlugin;
                            GisEditor.ProjectManager.CurrentProjectPlugin = currentPlugin;
                            SaveProject(null);
                        }
                    });

                    openMenuItems.Add(subOpenMenu);
                    saveMenuItems.Add(subSaveMenu);
                });

                openMenuItem.ItemsSource = openMenuItems;
                saveMenuItem.ItemsSource = saveMenuItems;
            }
        }

        private void InitializeStatusBarItems(IEnumerable<object> statusBarItems)
        {
            foreach (object statusBarItem in statusBarItems)
            {
                UIElement statusBarItemElement = statusBarItem as UIElement;
                if (statusBarItemElement != null)
                {
                    CustomStatusBarItemPanel.Children.Add(statusBarItemElement);
                }
            }
        }

        private void InitializeDockableContent(IEnumerable<DockableContentInfo> contents)
        {
            foreach (var dockableContent in contents)
            {
                DockableContent content = DockableContentInfo.ToDockableContent(dockableContent.DockWindow);
                switch (content.DockPosition)
                {
                    case DockPosition.Left:
                        content.Show(DockManager, AnchorStyle.Left);
                        break;

                    case DockPosition.Right:
                        content.Show(DockManager, AnchorStyle.Right);
                        break;

                    case DockPosition.Bottom:
                        content.Show(DockManager, AnchorStyle.Bottom);
                        break;

                    default:
                        content.Show(DockManager, AnchorStyle.Left);
                        break;
                }

                switch (dockableContent.StartupMode)
                {
                    case DockWindowStartupMode.AutoHide:
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (content.State != DockableContentState.AutoHide)
                            {
                                content.ToggleAutoHide();
                            }
                        }, DispatcherPriority.Background);
                        content.Tag = false;
                        break;

                    case DockWindowStartupMode.Hide:
                        content.Hide();
                        content.Tag = false;
                        break;

                    case DockWindowStartupMode.Display:
                    case DockWindowStartupMode.Default:
                        content.Tag = true;//the tag will be used in a binding expression.
                        break;

                    default:
                        content.Tag = false;
                        break;
                }

                content.StateChanged += new RoutedEventHandler(DockableContentState_Changed);
                DockWindow dockWindow = dockableContent.DockWindow;
                GisEditor.DockWindowManager.DockWindows.Add(dockWindow);
            }
        }

        private void InitializeRibbonContextualTabGroups(IEnumerable<RibbonContextualTabGroup> contextualTabGroups)
        {
            foreach (RibbonContextualTabGroup tabGroup in contextualTabGroups)
            {
                ribbonContainer.ContextualTabGroups.Add(tabGroup);
            }
        }

        private void DestroyStatusBarItems(IEnumerable<object> statusBarItems)
        {
            foreach (object statusBarItem in statusBarItems)
            {
                UIElement statusBarItemElement = statusBarItem as UIElement;
                if (statusBarItemElement != null && CustomStatusBarItemPanel.Children.Contains(statusBarItemElement))
                {
                    CustomStatusBarItemPanel.Children.Remove(statusBarItemElement);
                }
            }
        }

        //private void DestroyRibbonGroups(IEnumerable<RibbonGroup> ribbonGroups)
        private void DestroyRibbonGroups(IEnumerable<RibbonEntry> ribbonEntries)
        {
            foreach (RibbonEntry item in ribbonEntries.Where(r => r.RibbonGroup != null))
            {
                RibbonTab ribbonTab = item.RibbonGroup.Parent as RibbonTab;
                if (ribbonTab != null)
                {
                    RibbonGroup ribbonGroup = item.RibbonGroup;
                    ribbonTab.Items.Remove(ribbonGroup);
                    if (ribbonTab.Items.Count == 0)
                    {
                        ribbonContainer.Items.Remove(ribbonTab);
                    }
                }
            }
        }

        private void DestroyRibbonContextualTabGroups(IEnumerable<RibbonContextualTabGroup> contextualTabGroups)
        {
            foreach (RibbonContextualTabGroup tabGroup in contextualTabGroups)
            {
                if (ribbonContainer.ContextualTabGroups.Contains(tabGroup))
                {
                    ribbonContainer.ContextualTabGroups.Remove(tabGroup);
                }
            }
        }

        private void DestroyDockableContents(IEnumerable<DockableContentInfo> contents)
        {
            foreach (var contentInfo in contents)
            {
                if (GisEditor.DockWindowManager.DockWindows.Contains(contentInfo.DockWindow))
                {
                    GisEditor.DockWindowManager.DockWindows.Remove(contentInfo.DockWindow);
                }

                DockableContent dockableContent = DockManager.DockableContents.FirstOrDefault(d => d.Name.Equals(contentInfo.DockWindow.Name, StringComparison.Ordinal));
                if (dockableContent != null) dockableContent.Close();
            }
        }

        internal void RemoveOrAddAutoSaveStackPanel(bool isAdd)
        {
            if (isAdd)
            {
                if (!CustomStatusBarItemPanel.Children.Contains(autoSavingStackPanel))
                {
                    CustomStatusBarItemPanel.Children.Add(autoSavingStackPanel);
                }
            }
            else
            {
                if (CustomStatusBarItemPanel.Children.Contains(autoSavingStackPanel))
                {
                    CustomStatusBarItemPanel.Children.Remove(autoSavingStackPanel);
                }
            }
        }

        [Obfuscation]
        private void ProjectBackupMessageUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (GisEditor.ProjectManager.IsLoaded && generalManager.IsDisplayAutoSave && GisEditor.ProjectManager.ProjectUri != null)
            {
                if (!CustomStatusBarItemPanel.Children.Contains(autoSavingStackPanel))
                {
                    CustomStatusBarItemPanel.Children.Add(autoSavingStackPanel);
                }

                if (autoSavingTextBlock == null)
                {
                    autoSavingTextBlock = new TextBlock();
                    autoSavingTextBlock.SetResourceReference(TextBlock.TextProperty, "AutoRecoveryLastSavedText");
                }

                if (timeTextBlock == null) timeTextBlock = new TextBlock();
                timeTextBlock.Text = String.Format(CultureInfo.InvariantCulture, " {0}", DateTime.Now.ToShortTimeString());

                if (!autoSavingStackPanel.Children.Contains(autoSavingTextBlock))
                {
                    autoSavingStackPanel.Children.Add(autoSavingTextBlock);
                }
                if (!autoSavingStackPanel.Children.Contains(timeTextBlock))
                {
                    autoSavingStackPanel.Children.Add(timeTextBlock);
                }
            }
        }

        [Obfuscation]
        private void DockManager_ActiveDocumentChanged(object sender, EventArgs e)
        {
            DocumentContent currentDocumentContent = DockManager.ActiveDocument as DocumentContent;
            DockableContent currentDockableContent = DockManager.ActiveDocument as DockableContent;

            if (currentDocumentContent != null)
            {
                if (documentSource.Contains(currentDocumentContent))
                {
                    if (currentDocumentContent != null && currentDocumentContent.Content == null)
                    {
                        GisEditorWpfMap map = CreateNewMap(currentDocumentContent.Title);
                        map.Loaded += new RoutedEventHandler(Map_Loaded);
                        currentDocumentContent.Content = map;
                        var activeDocumentWindow = GisEditor.DockWindowManager.DocumentWindows.FirstOrDefault(d => d.Title.Equals(currentDocumentContent.Title));
                        if (activeDocumentWindow != null) activeDocumentWindow.Content = map;
                        GisEditor.ActiveMap = map;
                        GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => p.AttachMap(map));
                    }

                    if (previousDocumentContent != null && currentDocumentContent != previousDocumentContent)
                    {
                        var oldWpfMap = previousDocumentContent.Content as GisEditorWpfMap;
                        var newWpfMap = currentDocumentContent.Content as GisEditorWpfMap;

                        SaveDocumentState(previousDocumentContent);
                        SetGlobalsValues(currentDocumentContent);
                        GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.DockManagerActiveDocumentChangedDescription));
                        previousDocumentContent = currentDocumentContent;
                        mapStatus.Content = newWpfMap.GetMapBasicInformation();
                    }
                }
            }
            else if (currentDockableContent == null)
            {
                lock (lockLayoutInstance)
                {
                    var activeMap = GisEditor.ActiveMap;
                    GisEditor.ActiveMap = CreateNewMap();
                    GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => p.Refresh(null));
                    GisEditor.ActiveMap = activeMap;
                }
            }
        }

        [Obfuscation]
        private void DocumentPane_CanClickPlusButton(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GisEditor.ProjectManager.IsLoaded;
        }

        private void Map_OverlaysDrawn(object sender, OverlaysDrawnWpfMapEventArgs e)
        {
            GisEditorWpfMap map = sender as GisEditorWpfMap;
            mapStatus.Content = map.GetMapBasicInformation();
        }

        private async void Map_MouseMove(object sender, MouseEventArgs e)
        {
            GisEditorWpfMap map = sender as GisEditorWpfMap;
            if (map != null)
            {
                currentMouseScreenCoordinate = e.GetPosition(map);

                mouseCoordinateStatus.Content = await Task.Run(() => map.GetFormattedWorldCoordinate(new ScreenPointF((float)currentMouseScreenCoordinate.X, (float)currentMouseScreenCoordinate.Y), MouseCoordinateType));
            }
        }

        [Obfuscation]
        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                GisEditorWpfMap map = sender as GisEditorWpfMap;
                DocumentContent parentDocuemnt = map.Parent as DocumentContent;
                if (parentDocuemnt.IsActiveDocument)
                {
                    //GisEditor.ProjectManager.LoadDocumentState(parentDocuemnt);
                    var mapsInProject = GisEditor.ProjectManager.GetDeserializedMaps();
                    parentDocuemnt.Content = mapsInProject.FirstOrDefault(m => m.Name.Equals(parentDocuemnt.Title));
                    var containerWindow = GisEditor.DockWindowManager.DocumentWindows.FirstOrDefault(w => w.Title.Equals(parentDocuemnt.Title, StringComparison.OrdinalIgnoreCase));
                    if (containerWindow != null) containerWindow.Content = parentDocuemnt.Content as Control;

                    //this is because we substitute the map when we do deserialization.
                    map = parentDocuemnt.Content as GisEditorWpfMap;
                    if (map != null)
                    {
                        if (map.Parent == DockManager.ActiveDocument)
                        {
                            GisEditor.ActiveMap = map;
                            map.IsMapLoaded = true;
                            foreach (var item in GetMenuItems())
                            {
                                map.ContextMenu.Items.Add(item);
                            }
                            InitializeMapEvents(map);
                            InitializePanZoomBar(map, GetShowPanZoomBar());
                            GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().ForEach(p => p.AttachMap(map));
                            GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.MapLoadedDescription));

                            //Dispatcher.BeginInvoke(() => GisEditor.ActiveMap.Refresh(), DispatcherPriority.Background);
                            GisEditor.ActiveMap.Width = 1;
                            GisEditor.ActiveMap.Width = double.NaN;
                        }

                        map.Loaded -= new RoutedEventHandler(Map_Loaded);
                        map.IsMapStateChanged = false;
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditorMessageBox messageBox = new GisEditorMessageBox(System.Windows.MessageBoxButton.OK);
                messageBox.Message = String.Format(CultureInfo.InvariantCulture, "This project is last modified with version {0}, but it cannot be opened by this version. Please roll GIS Editior back to {0} and have another try.", GisEditor.ProjectManager.CurrentProjectVersion);
                messageBox.Title = "Error";
                messageBox.ViewDetailHeader = "Call stack";
                messageBox.ErrorMessage = ex.StackTrace;
                messageBox.ShowDialog();
            }
        }

        private bool GetShowPanZoomBar()
        {
            Dictionary<string, string> options = Tag as Dictionary<string, string>;
            if (options != null && options.ContainsKey("IsShowPanZoomBar"))
            {
                return Boolean.TrueString.Equals(options["IsShowPanZoomBar"], StringComparison.OrdinalIgnoreCase);
            }
            else return true;
        }

        [Obfuscation]
        private void DocumentPane_PlusButtonClicked(object sender, EventArgs e)
        {
            DocumentPane documentPane = sender as DocumentPane;
            if (documentPane != null)
            {
                AddDocument();
            }
        }

        [Obfuscation]
        private void DocumentPane_CanClickPlusButton(DependencyObject sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GisEditor.ProjectManager.IsLoaded;
        }

        [Obfuscation]
        private void DocumentsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DocumentsListBox.SelectedItem != null)
            {
                DocumentContent content = DocumentsListBox.SelectedItem as DocumentContent;
                if (!documentSource.Contains(content))
                {
                    documentSource.Add(content);
                }
                DockManager.ActiveDocument = content;
                GisEditor.ActiveMap = content.Content as GisEditorWpfMap;
                GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.DocumentsListMouseDoubleClickDescription));
            }
        }

        private IEnumerable<IStorableSettings> GetSettingsToSave()
        {
            yield return dockingStateManager;

            var storableSettings = GisEditorHelper.GetManagers().Cast<IStorableSettings>().Concat(
                    GisEditor.InfrastructureManager.GetManagers().OfType<PluginManager>()
                    .SelectMany(m => m.GetPlugins()));

            foreach (var item in storableSettings)
            {
                yield return item;
            }
        }

        private void CloseProject(CancelEventArgs e)
        {
            if (GisEditor.ProjectManager.IsLoaded)
            {
                if (RemindUserToSave())
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    //If user chooses "No" and the project file does not exist, that means the user
                    //wants to abandon the unsaved project, so we should delete all png files
                    //that got cached during the proccess of this project being used.
                    if (!GisEditor.ProjectManager.ProjectExists(GisEditor.ProjectManager.ProjectUri))
                    {
                        foreach (var map in documentSource.Select(d => d.Content).OfType<GisEditorWpfMap>())
                        {
                            var tileOverlays = from overlay in map.Overlays.OfType<TileOverlay>()
                                               where !overlay.IsBase
                                                     && overlay.TileCache != null
                                               select overlay;

                            tileOverlays.Select(o => o.TileCache).Distinct().Where(c => c != null).AsParallel().ForEach(c =>
                            {
                                lock (c)
                                {
                                    try { c.ClearCache(); }
                                    catch { }
                                }
                            });
                        }
                    }
                }
                GisEditor.ProjectManager.CloseProject();
            }
        }

        [Obfuscation]
        private void DockableContentState_Changed(object sender, RoutedEventArgs e)
        {
            DockableContent currentDock = (DockableContent)sender;
            currentDock.Tag = currentDock.State != DockableContentState.Hidden;
        }

        [Obfuscation]
        private void DockManager_Loaded(object sender, RoutedEventArgs e)
        {
            CreateNewProject();
            GisEditor.InfrastructureManager.ApplySettings(dockingStateManager);
            defaultPane = DockManager.MainDocumentPane;
            ApplyTheme(GisEditor.DockWindowManager.Theme);
        }

        [Obfuscation]
        private void DocumentsList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!GisEditor.ProjectManager.IsLoaded)
            {
                e.Handled = true;
            }
        }

        [Obfuscation]
        private void RenameControl_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            if (!e.NewText.Equals(e.OldText))
            {
                if (ValidateMapName(e.NewText))
                {
                    (DocumentsListBox.SelectedItem as DocumentContent).Title = e.NewText;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(mapNameWarningMessage, "Info", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    e.IsCancelled = true;
                }
            }
            else
                e.IsCancelled = true;
        }

        private void OpenMapNormalContextMenu(ContextMenuEventArgs e)
        {
            ContextMenu contextMenu = GisEditor.ActiveMap.ContextMenu;

            foreach (var menuItem in defaultMenuItems.OfType<MenuItem>())
            {
                if (menuItem.Parent != null)
                {
                    (menuItem.Parent as ContextMenu).Items.Remove(menuItem);
                }
                contextMenu.Items.Add(menuItem);
            }
        }

        private IEnumerable<object> GetMenuItems()
        {
            yield return DockManager.FindResource("ZoomInMenuItem");
            yield return DockManager.FindResource("ZoomOutMenuItem");
            yield return DockManager.FindResource("PreviousExtentMenuItem");
            yield return DockManager.FindResource("NextExtentMenuItem");
            yield return DockManager.FindResource("CenterAtMenuItem");
            yield return new Separator();
            yield return DockManager.FindResource("RefreshMapMenuItem");
            yield return DockManager.FindResource("RenameDocumentMenuItem");
        }

        private void UpdateRecentProjectFileSource(Uri newProjectFileName)
        {
            if (newProjectFileName == null
                || newProjectFileName.AbsolutePath.Equals("blank", StringComparison.OrdinalIgnoreCase)
                || newProjectFileName.Scheme.ToLower().Contains("backup")
                || newProjectFileName.AbsolutePath.Equals("untitled", StringComparison.OrdinalIgnoreCase))
                return;

            var foundRecords = recentProjectFiles.Where(r => r.FullPath.AbsolutePath.Equals(newProjectFileName.AbsolutePath, StringComparison.OrdinalIgnoreCase)).ToList();
            if (foundRecords.Count > 0)
            {
                foreach (var foundRecord in foundRecords)
                {
                    recentProjectFiles.Remove(foundRecord);
                }
                foundRecords.First().ProjectPluginType = GisEditor.ProjectManager.CurrentProjectPlugin.GetType().Name;
                recentProjectFiles.Insert(0, foundRecords.First());
            }
            else
            {
                RecentProjectModel viewModel = new RecentProjectModel(newProjectFileName, GisEditor.ProjectManager.CurrentProjectPlugin.GetType().Name);
                recentProjectFiles.Insert(0, viewModel);
            }

            while (recentProjectFiles.Count > 20)
            {
                recentProjectFiles.RemoveAt(recentProjectFiles.Count - 1);
            }

            ReorderRecentProjectIndexes();

            foreach (var foundRecord in foundRecords)
            {
                foundRecord.IsEnabled = GisEditorCommands.OpenRecentProjectFileCommand.CanExecute(foundRecord.FullPath);
            }
        }

        private void ReorderRecentProjectIndexes()
        {
            for (int i = 0; i < recentProjectFiles.Count; i++)
            {
                recentProjectFiles[i].IsEnabled = true;
                recentProjectFiles[i].Index = i + 1;
            }
        }

        private void HookDocumentPaneEvents(DocumentPaneAddedEventArgs arg)
        {
            arg.AddedDocumentPane.PlusButtonClicked -= new EventHandler(DocumentPane_PlusButtonClicked);
            arg.AddedDocumentPane.PlusButtonClicked += new EventHandler(DocumentPane_PlusButtonClicked);
            arg.AddedDocumentPane.CanClickPlusButton -= new CanExecuteRoutedEventHandler(DocumentPane_CanClickPlusButton);
            arg.AddedDocumentPane.CanClickPlusButton += new CanExecuteRoutedEventHandler(DocumentPane_CanClickPlusButton);
        }

        private void OpenLatestFailedProjectIfExists()
        {
            Dispatcher.BeginInvoke(() =>
            {
                RestoreBackupWindow window = new RestoreBackupWindow();
                if (window.NeedOpen)
                {
                    if (window.ShowDialog().GetValueOrDefault())
                    {
                        var currentPlugin = GisEditor.ProjectManager.GetPlugins().OfType<ProjectPlugin>().FirstOrDefault(p => p.GetType().FullName.Equals(window.PluginType, StringComparison.Ordinal));
                        if (currentPlugin != null)
                        {
                            GisEditor.ProjectManager.CurrentProjectPlugin = currentPlugin;

                            ProjectManager_Closed(this, null);

                            ProjectStreamInfo tempInfo = new ProjectStreamInfo();
                            tempInfo.Uri = new Uri(window.BackupProjectFilePath);
                            bool canOpenProject = GisEditor.ProjectManager.CanOpenProject(tempInfo);

                            if (!canOpenProject) return;

                            string schema = "backup:";

                            if (window.rbtnSave.IsChecked.Value)
                            {
                                schema = "savebackup:";
                            }
                            else if (window.rbtnOpen.IsChecked.Value)
                            {
                                schema = "openbackup:";
                            }

                            GisEditor.ProjectManager.OpenProject(new Uri(schema + window.BackupProjectFilePath));
                        }
                    }
                    else
                    {
                        ClearBackupProjects();
                    }
                }
            });

            //string backupProjectFolder = GisEditorHelper.GetBackupProjectFolder();

            //if (Directory.Exists(backupProjectFolder))
            //{
            //    string[] files = Directory.GetFiles(backupProjectFolder, "*.tgproj.txt");

            //    string pluginType = string.Empty;
            //    string backupProjectFilePath = string.Empty;

            //    if (files.Length > 0)
            //    {
            //        string[] contents = File.ReadAllLines(files[0]);

            //        if (contents.Length == 2)
            //        {
            //            pluginType = contents[0];
            //            string projectFileName = contents[1];

            //            string directory = Path.GetDirectoryName(files[0]);
            //            backupProjectFilePath = Path.Combine(directory, projectFileName + ".tgproj");
            //        }
            //    }

            //    var currentPlugin = GisEditor.ProjectManager.GetPlugins().OfType<ProjectPlugin>().FirstOrDefault(p => p.GetType().FullName.Equals(pluginType, StringComparison.Ordinal));
            //    if (currentPlugin != null)
            //    {
            //        GisEditor.ProjectManager.CurrentProjectPlugin = currentPlugin;
            //        if (File.Exists(backupProjectFilePath))
            //        {
            //            Dispatcher.BeginInvoke(() =>
            //            {
            //                RestoreBackupWindow window = new RestoreBackupWindow();
            //                if (window.ShowDialog().GetValueOrDefault())
            //                {
            //                    if (File.Exists(window.BackProjectPath))
            //                    {
            //                        string[] files = Directory.GetFiles(window.BackProjectPath, "*.tgproj.txt");

            //                        string pluginType = string.Empty;
            //                        string backupProjectFilePath = string.Empty;

            //                        if (files.Length > 0)
            //                        {
            //                            string[] contents = File.ReadAllLines(files[0]);

            //                            if (contents.Length == 2)
            //                            {
            //                                pluginType = contents[0];
            //                                string projectFileName = contents[1];

            //                                string directory = Path.GetDirectoryName(files[0]);
            //                                backupProjectFilePath = Path.Combine(directory, projectFileName + ".tgproj");

            //                                ProjectManager_Closed(this, null);

            //                                ProjectStreamInfo tempInfo = new ProjectStreamInfo();
            //                                tempInfo.Uri = new Uri(backupProjectFilePath);
            //                                bool canOpenProject = GisEditor.ProjectManager.CanOpenProject(tempInfo);

            //                                if (!canOpenProject) return;

            //                                GisEditor.ProjectManager.OpenProject(new Uri("backup:" + backupProjectFilePath));
            //                            }
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    Directory.Delete(backupProjectFolder, true);
            //                }

            //            }, DispatcherPriority.Background);
            //        }
            //    }
            //}
        }

        private void ClearBackupProjects()
        {
            try
            {
                string autoBackupFolder = GisEditorHelper.GetBackupProjectFolder();
                if (Directory.Exists(autoBackupFolder))
                {
                    Directory.Delete(autoBackupFolder, true);
                }

                string openBackupFolder = GisEditorHelper.GetLastOpenBackupProjectFolder();
                if (Directory.Exists(openBackupFolder))
                {
                    Directory.Delete(openBackupFolder, true);
                }

                string saveBackupFolder = GisEditorHelper.GetLastSavedBackupProjectFolder();
                if (Directory.Exists(saveBackupFolder))
                {
                    Directory.Delete(saveBackupFolder, true);
                }
            }
            catch (IOException)
            {
            }
        }

        private void InitializeMapProperties(GisEditorWpfMap map)
        {
            map.AllowDrop = true;
            map.IsMapStateChanged = false;
            map.Background = mapBackgroundBrush;
            map.BackgroundOverlay = new BackgroundOverlay();
            map.BackgroundOverlay.BackgroundBrush = new GeoSolidBrush(GeoColor.StandardColors.White);
        }

        private void InitializeMapEvents(GisEditorWpfMap map)
        {
            map.MapClick -= Map_MapClick;
            map.MapClick += Map_MapClick;
            map.MouseMove -= Map_MouseMove;
            map.MouseMove += Map_MouseMove;
            map.OverlaysDrawn -= Map_OverlaysDrawn;
            map.OverlaysDrawn += Map_OverlaysDrawn;
            map.ZoomLevelSetChanged -= Map_ZoomLevelSetChanged;
            map.ZoomLevelSetChanged += Map_ZoomLevelSetChanged;
            map.ContextMenuOpening -= Map_ContextMenuOpening;
            map.ContextMenuOpening += Map_ContextMenuOpening;
            map.ContextMenu.Opened -= ContextMenu_Opened;
            map.ContextMenu.Opened += ContextMenu_Opened;

            map.DisplayProjectionParametersChanged -= Map_DisplayProjectionParametersChanged;
            map.DisplayProjectionParametersChanged += Map_DisplayProjectionParametersChanged;
        }

        private void Map_DisplayProjectionParametersChanged(object sender, DisplayProjectionParametersChangedGisEditorWpfMapEventArgs e)
        {
            GisEditorWpfMap map = (GisEditorWpfMap)sender;
            mapStatus.Content = map.GetMapBasicInformation();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.ContextMenu != null && GisEditor.ActiveMap.ContextMenu.Tag != null)
            {
                Point touchDownPoint = ((Point)GisEditor.ActiveMap.ContextMenu.Tag);
                CollectContextMenu(GisEditor.ActiveMap, new PointShape(touchDownPoint.X, touchDownPoint.X));
            }
            e.Handled = true;
        }

        private void Map_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            WpfMap currentMap = (WpfMap)sender;
            PointShape screenPoint = new PointShape(e.CursorLeft, e.CursorTop);
            CollectContextMenu(currentMap, screenPoint);
            e.Handled = true;
        }

        private void CollectContextMenu(WpfMap currentMap, PointShape screenPoint)
        {
            PointShape worldPoint = currentMap.ToWorldCoordinate(screenPoint);
            GetMapContextMenuParameters contextMenuArgs = new GetMapContextMenuParameters(screenPoint
                , worldPoint
                , currentMap.CurrentScale
                , currentMap.CurrentResolution);

            ContextMenu contextMenu = new ContextMenu();
            foreach (var item in GetMenuItems().Reverse())
            {
                contextMenu.Items.Insert(0, item);
            }

            foreach (var menuItem in GisEditor.UIManager.GetMapContextMenuItems(contextMenuArgs))
            {
                object menuItemToAdd = menuItem;
                if (menuItem.Header.ToString() == "--")
                {
                    menuItemToAdd = new Separator();
                }
                contextMenu.Items.Insert(0, menuItemToAdd);
            }

            contextMenu.IsOpen = true;
        }

        private void Map_ZoomLevelSetChanged(object sender, ZoomLevelSetChangedWpfMapEventArgs e)
        {
            if (generalManager != null && e.NewZoomLevelSet != null)
            {
                if (e.NewZoomLevelSet.CustomZoomLevels.Count == 0)
                {
                    if (generalManager.Scales.Count > 0)
                    {
                        foreach (var item in generalManager.Scales)
                        {
                            e.NewZoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(item));
                        }
                    }
                    else
                    {
                        foreach (var item in e.NewZoomLevelSet.GetZoomLevels())
                        {
                            item.Scale = Math.Round(item.Scale, 6);
                            e.NewZoomLevelSet.CustomZoomLevels.Add(item);
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            var scale = e.NewZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale * 0.5;
                            var zoomLevel = new ZoomLevel(Math.Round(scale, 6));
                            e.NewZoomLevelSet.CustomZoomLevels.Add(zoomLevel);
                        }
                    }
                    (sender as WpfMap).MinimumScale = e.NewZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale;
                }
                else
                {
                    generalManager.Scales.Clear();
                    foreach (var item in e.NewZoomLevelSet.CustomZoomLevels.Where(z => !(z is PreciseZoomLevel)))
                    {
                        generalManager.Scales.Add(item.Scale);
                    }
                }
            }
        }

        private static void InitializePanZoomBar(GisEditorWpfMap map, bool showPanZoomBar)
        {
            var switcherPanZoomBarMapTool = GisEditor.ControlManager.GetUI<SwitcherPanZoomBarMapTool>();
            if (switcherPanZoomBarMapTool != null && map.MapTools.All(mapTool => !(mapTool is SwitcherPanZoomBarMapTool)))
            {
                map.MapTools.Add(switcherPanZoomBarMapTool);
                switcherPanZoomBarMapTool.IsEnabled = showPanZoomBar;
            }
        }

        [Obfuscation]
        private void Map_MapClick(object sender, MapClickWpfMapEventArgs e)
        {
            lastClickedPoint = e.WorldLocation;
        }

        private void SaveDocumentState(DocumentContent documentContent)
        {
            if (GisEditor.ActiveMap == null) return;

            if (!documentState.ContainsKey(documentContent))
            {
                documentState.Add(documentContent,
                      new DocumentState
                      {
                          ActiveLayer = GisEditor.ActiveMap.ActiveLayer,
                          ActiveOverlay = GisEditor.ActiveMap.ActiveOverlay
                      });
            }
            else
            {
                documentState[documentContent].ActiveLayer = GisEditor.ActiveMap.ActiveLayer;
                documentState[documentContent].ActiveOverlay = GisEditor.ActiveMap.ActiveOverlay;
            }
        }

        private void SetGlobalsValues(DocumentContent documentContent)
        {
            if (!documentState.ContainsKey(documentContent))
            {
                documentState.Add(documentContent, new DocumentState());
            }

            GisEditor.ActiveMap = documentContent.Content as GisEditorWpfMap;
            GisEditor.ActiveMap.ActiveLayer = documentState[documentContent].ActiveLayer;
            GisEditor.ActiveMap.ActiveOverlay = documentState[documentContent].ActiveOverlay;
        }

        private void ResetMapsState()
        {
            foreach (GisEditorWpfMap map in GisEditor.DockWindowManager.DocumentWindows
                .Select(item => item.Content)
                .OfType<GisEditorWpfMap>())
            {
                map.IsMapStateChanged = false;
            }
        }

        private bool RemindUserToSave()
        {
            bool isMapStateMatched = !GisEditor.DockWindowManager.DocumentWindows
                .Select(item => item.Content)
                .OfType<GisEditorWpfMap>()
                .Any(map => map.IsMapStateChanged);

            Collection<DocumentWindow> documentWindows = GisEditor.DockWindowManager.DocumentWindows;
            bool isOnlyOneWorldMapKitOverlay = documentWindows.Count == 1
                && documentWindows[0].Content != null
                && (documentWindows[0].Content as GisEditorWpfMap).Overlays.Count == 1
                && ((documentWindows[0].Content as GisEditorWpfMap).Overlays[0] is WorldMapKitMapOverlay
                    || (documentWindows[0].Content as GisEditorWpfMap).Overlays[0] is BingMapsOverlay
                    || (documentWindows[0].Content as GisEditorWpfMap).Overlays[0] is OpenStreetMapOverlay);

            if (isMapStateMatched || isCloseSilently || isOnlyOneWorldMapKitOverlay)
            {
                return false;
            }
            System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlSaveCurrentText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question);
            switch (result)
            {
                case System.Windows.Forms.DialogResult.Cancel:
                    return true;

                case System.Windows.Forms.DialogResult.Yes:
                    if (GisEditor.ProjectManager.IsLoaded && GisEditor.ProjectManager.ProjectUri != null)
                    {
                        SaveProject();
                    }
                    else
                    {
                        SaveProject(null);
                    }
                    SaveRecentFiles();
                    break;

                default:
                    break;
            }
            return false;
        }

        private void SetActiveDocument()
        {
            int selectedIndex = GisEditor.DockWindowManager.ActiveDocumentIndex;
            if (selectedIndex != -1 && selectedIndex < DockManager.MainDocumentPane.Items.Count)
            {
                DockManager.ActiveDocument = (ManagedContent)DockManager.MainDocumentPane.Items[selectedIndex];
            }
            else
            {
                DockManager.ActiveDocument = (ManagedContent)DockManager.MainDocumentPane.Items[0];
            }

            GisEditor.ActiveMap = DockManager.ActiveDocument.Content as GisEditorWpfMap;
            GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.SetActiveDocumentDescription));
        }

        private bool ValidateMapName(string mapName)
        {
            var allMapNames = GisEditor.DockWindowManager.DocumentWindows.Select(document => document.Title);
            return Regex.IsMatch(mapName, mapNamePattern) && !allMapNames.Contains(mapName) && !mapName.Contains("__");
        }

        private void Launch()
        {
            if (!doesLaunched)
            {
                doesLaunched = true;
                Dispatcher.BeginInvoke(() =>
                {
                    //WarningMessageText.Visibility = Visibility.Hidden;
                    //imgLoading.Visibility = Visibility.Hidden;
                    //MaskPanel.Visibility = Visibility.Collapsed;
                    //if (gifTimer != null) gifTimer.Stop();

                    if (!String.IsNullOrEmpty(StartupProjectPath))
                    {
                        ProjectManager_Closed(this, null);
                        OpenProject(new Uri(StartupProjectPath));
                    }
                });
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                {
                    //WarningMessageText.Visibility = Visibility.Hidden;
                    //imgLoading.Visibility = Visibility.Hidden;
                    //MaskPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        [Obfuscation]
        internal void SaveRecentFiles()
        {
            GisEditorHelper.SaveRecentFiles(recentProjectFiles);
        }

        [Obfuscation]
        internal void OnWindowClosing(CancelEventArgs e)
        {
            CloseProject(e);
            ResetLayoutIfInvalid();
        }

        private void SaveProjectFile(Uri projectUri)
        {
            GisEditor.ProjectManager.SaveProject(projectUri);
            if (projectUri == null || (projectUri != null && Path.GetDirectoryName(projectUri.LocalPath) != Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "MapSuiteGisEditor")))
                UpdateRecentProjectFileSource(GisEditor.ProjectManager.ProjectUri);
        }

        private static string GetAssemblyLocation()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        private static System.Drawing.Image GenerateImage(FrameworkElement element)
        {
            Size size = RetrieveDesiredSize(element);

            Rect rect = new Rect(0, 0, size.Width, size.Height);

            if (size.Width < 1 || size.Height < 1)
            {
                return System.Drawing.Image.FromStream(Application.GetResourceStream(new Uri("/MapSuiteGisEditor;component/Images/DefaultPreview.png", UriKind.RelativeOrAbsolute)).Stream);
            }
            else
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, dpi, dpi, PixelFormats.Pbgra32);

                element.Arrange(rect); //Let the control arrange itself inside your Rectangle
                rtb.Render(element); //Render the control on the RenderTargetBitmap

                //Now encode and convert to a gdi+ Image object
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                using (MemoryStream stream = new MemoryStream())
                {
                    png.Save(stream);
                    return System.Drawing.Image.FromStream(stream);
                }
            }
        }

        private static Size RetrieveDesiredSize(FrameworkElement element)
        {
            if (Equals(element.ActualWidth, double.NaN) || Equals(element.ActualHeight, double.NaN))
            {
                element.Measure(new System.Windows.Size(double.MaxValue, double.MaxValue));
                return element.DesiredSize;
            }

            return new Size(element.ActualWidth, element.ActualHeight);
        }

        [Obfuscation]
        private void ProjectFileManager_PreviewImageFetch(object sender, LoadingPreviewImageProjectPluginManagerEventArgs e)
        {
            e.Image = GenerateImage((FrameworkElement)Content);
        }

        [Obfuscation]
        private void ProjectFileManager_UriChanged(object sender, UriChangedProjectPluginManagerEventArgs e)
        {
            currentProjectUrl = e.NewUri;
            currentProjectReadWriteMode = e.ProjectReadWriteMode;
            UpdateMainTitle();
        }

        private void UpdateMainTitle()
        {
            string projectName = "Untitled";

            if (GisEditor.ProjectManager.CurrentProjectPlugin != null)
            {
                string shortName = GisEditor.ProjectManager.CurrentProjectPlugin.GetProjectShortName(currentProjectUrl);
                projectName = shortName.Equals("blank", StringComparison.Ordinal) ? "Untitled" : shortName;
            }

            string editionName = GisEditor.InfrastructureManager.EditionName;
            if (!string.IsNullOrEmpty(editionName)) editionName = " (" + editionName + ")";

            string state = string.Empty;
            if (currentProjectReadWriteMode != ProjectReadWriteMode.ReadWrite)
            {
                state = string.Format("({0})", currentProjectReadWriteMode.ToString());
            }

            GetOwnerWindow().Title = string.Format("Map Suite GIS Editor{0} - {1} - {2} {3}", editionName, AboutWindow.GetVersionInfo(), projectName, state);
        }

        [Obfuscation]
        private void ProjectManager_ProjectStateChanged(object sender, StateChangedProjectPluginManagerEventArgs e)
        {
            string title = GetOwnerWindow().Title;

            title = title.TrimEnd(" (ReadOnly)".ToArray());

            if (e.State == ProjectReadWriteMode.ReadOnly)
            {
                title = string.Format("{0} {1}", title, "(ReadOnly)");
            }

            GetOwnerWindow().Title = title;
        }

        [Obfuscation]
        private void ProjectManager_Closed(object sender, EventArgs e)
        {
            documentState.Clear();
            documentsCount = 0;
            GisEditor.DockWindowManager.DocumentWindows.Clear();
            autoSavingStackPanel.Children.Clear();
        }

        [Obfuscation]
        private void ProjectManager_Closing(object sender, EventArgs e)
        {
            GisEditor.InfrastructureManager.SaveSettings(GetSettingsToSave());
            var dockWindowsNeedToRemove = new Collection<DockWindow>();
            var allDockWindows = GisEditor.UIManager.GetUIPlugins().SelectMany(plugin => plugin.DockWindows).ToList();
            foreach (var dockWindow in GisEditor.DockWindowManager.DockWindows)
            {
                if (allDockWindows.FirstOrDefault(d => d.Content == dockWindow.Content) == null
                    && dockWindow.Content != DocumentsList.Content)
                    dockWindowsNeedToRemove.Add(dockWindow);
            }
            foreach (var item in dockWindowsNeedToRemove)
            {
                GisEditor.DockWindowManager.DockWindows.Remove(item);
            }
        }

        [Obfuscation]
        private void ProjectFileManager_Opened(object sender, OpenedProjectManagerEventArgs e)
        {
            if (e.Error != null)
            {
                System.Windows.Forms.MessageBox.Show(e.Error.Message, "Shapefile Missing Warning", System.Windows.Forms.MessageBoxButtons.OK);
            }

            UpdateRecentProjectFileSource(e.ProjectStreamInfo.Uri);
            documentsCount = GisEditor.DockWindowManager.DocumentWindows.Count;

            if (GisEditor.DockWindowManager.ActiveDocumentIndex != 0)
            {
                documentSource.Where(d => d.Content != null && d.Content is GisEditorWpfMap).ForEach(d => d.Content = null);
                GisEditor.DockWindowManager.DocumentWindows.Where(d => d.Content != null && d.Content is GisEditorWpfMap)
                    .ForEach(d => d.Content = null);
            }

            if (DockManager.MainDocumentPane.Items.Count > 0)
            {
                SetActiveDocument();
            }

            ResetMapsState();
        }

        [Obfuscation]
        private void ProjectManager_Opening(object sender, OpeningProjectManagerEventArgs e)
        {
        }

        [Obfuscation]
        private void ProjectManager_AutoBackupIntervalChanged(object sender, AutoBackupIntervalChangedProjectPluginManagerEventArgs e)
        {
            SyncAutoBackupSettings();
        }

        [Obfuscation]
        private void ProjectManager_CanAutoBackupChanged(object sender, CanAutoBackupChangedProjectPluginManagerEventArgs e)
        {
            SyncAutoBackupSettings();
        }

        private void SplitLoggerFile()
        {
            string loggerFolderPath = Path.Combine(GisEditor.InfrastructureManager.SettingsPath, "Logging");
            string loggerFilePath = Path.Combine(loggerFolderPath, "MapSuiteGisEditor.log");
            string tempFilePath = loggerFilePath + ".temp";
            if (File.Exists(loggerFilePath))
            {
                File.Copy(loggerFilePath, tempFilePath, true);
                StreamReader streamReader = null;
                StreamWriter streamWriter = null;
                try
                {
                    streamReader = new StreamReader(tempFilePath);
                    string text = null;
                    while ((text = streamReader.ReadLine()) != null)
                    {
                        string[] array = text.Split(' ');
                        if (array.Length > 0)
                        {
                            DateTime dateTime;
                            if (DateTime.TryParse(array[0], out dateTime))
                            {
                                string targetFilePath = Path.Combine(loggerFolderPath, string.Format(CultureInfo.InvariantCulture, loggerFileFormat, array[0]));
                                if (streamWriter != null) streamWriter.Dispose();
                                streamWriter = new StreamWriter(targetFilePath, true);
                                streamWriter.WriteLine(text);
                            }
                            else
                            {
                                streamWriter.WriteLine(text);
                            }
                        }
                    }
                }
                finally
                {
                    if (streamReader != null) streamReader.Dispose();
                    if (streamWriter != null) streamWriter.Dispose();
                    File.Delete(tempFilePath);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveRecentFiles();
            Window mainWindow = GetOwnerWindow();
            GisEditorHelper.SaveWindowLocation(mainWindow);

            string location = string.Format("{0},{1},{2},{3}", mainWindow.Left, mainWindow.Top, mainWindow.Width, mainWindow.Height);

            string targetFilePath = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "location.txt");

            File.WriteAllText(targetFilePath, location);

            OnWindowClosing(e);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            GisEditor.UIManager.GetPlugins().ForEach(p => p.Unload());
            SplitLoggerFile();
        }

        private bool CheckCurrentProject(Uri ProjectUri)
        {
            bool isCanceled = false;

            if (ExitIfProjectNotExist(ProjectUri)) isCanceled = true;
            else if (ExitIfProjectIsOpened(ProjectUri)) isCanceled = true;
            else if (ExitIfCancelToSavePrj(ProjectUri)) isCanceled = true;

            return !isCanceled;
        }

        private void SyncAutoBackupSettings()
        {
            StartBackupProjectTimer(GisEditor.ProjectManager.CanAutoBackup);
            projectBackupMessageUpdateTimer.Interval = GisEditor.ProjectManager.AutoBackupInterval;
        }

        private static DockWindowPosition ConvertToDockWindowPosition(DockPosition dockPosition)
        {
            switch (dockPosition)
            {
                case DockPosition.Bottom:
                    return DockWindowPosition.Bottom;

                case DockPosition.Left:
                    return DockWindowPosition.Left;

                case DockPosition.Right:
                    return DockWindowPosition.Right;
            }

            return DockWindowPosition.Right;
        }

        private static DockPosition ConvertToDockPosition(DockWindowPosition dockWindowPosition)
        {
            switch (dockWindowPosition)
            {
                case DockWindowPosition.Left:
                    return DockPosition.Left;

                case DockWindowPosition.Bottom:
                    return DockPosition.Bottom;

                case DockWindowPosition.Default:
                case DockWindowPosition.Right:
                case DockWindowPosition.Floating:
                default:
                    return DockPosition.Right;
            }
        }

        private bool OnClosingAllButThis()
        {
            var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlRemoveAllMessage"), GisEditor.LanguageManager.GetStringResource("RemoveMapsMessageCaption"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            var prompt = result == System.Windows.Forms.DialogResult.Yes;
            if (prompt) isClosingAllButThis = true;
            return prompt;
        }

        private void OnClosedAllButThis()
        {
            isClosingAllButThis = false;

            var removingDocks = GisEditor.DockWindowManager.DocumentWindows.Where(d => !DockManager.Documents.Any(concreteD => concreteD.Content == d.Content)).ToList();
            foreach (var removingDock in removingDocks)
            {
                GisEditor.DockWindowManager.DocumentWindows.Remove(removingDock);
            }
        }

        private void DockManager_DocumentClosing(object sender, CancelEventArgs e)
        {
            if (!isClosingAllButThis)
            {
                var result = PromptRemovingDocument();
                if (result != System.Windows.Forms.DialogResult.Yes)
                {
                    e.Cancel = true;
                }
                else
                {
                    var removingDock = GisEditor.DockWindowManager.DocumentWindows.FirstOrDefault(d => DockManager.ActiveDocument.Content == d.Content);
                    if (removingDock != null) GisEditor.DockWindowManager.DocumentWindows.Remove(removingDock);
                }
            }
        }

        private static System.Windows.Forms.DialogResult PromptRemovingDocument()
        {
            return System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlRemovefromMessage"), GisEditor.LanguageManager.GetStringResource("RemoveMapMessageCaption"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
        }

        private void RefreshRencentList()
        {
            foreach (var file in recentProjectFiles)
            {
                file.IsEnabled = GisEditorCommands.OpenRecentProjectFileCommand.CanExecute(file.FullPath);
            }
        }

        private Window GetOwnerWindow()
        {
            if (window == null) window = Window.GetWindow(this);
            if (window == null) throw new ArgumentNullException("Window", "Owner window can't be null, please call this method after attaching this usercontrol to a window.");
            return window;
        }

        private void StartBackupProjectTimer(bool isEnabled)
        {
            StartBackupProjectTimer(isEnabled ? StartBackupMode.Enable : StartBackupMode.Disable);
        }

        private void StartBackupProjectTimer(StartBackupMode startBackupMode)
        {
            switch (startBackupMode)
            {
                case StartBackupMode.Enable:
                    projectBackupMessageUpdateTimer.IsEnabled = true;
                    break;

                case StartBackupMode.Disable:
                    projectBackupMessageUpdateTimer.IsEnabled = false;
                    break;

                case StartBackupMode.Start:
                default:
                    projectBackupMessageUpdateTimer.Start();
                    break;
            }
        }

        private enum StartBackupMode
        {
            Start = 0,
            Enable = 1,
            Disable = 2
        }

        [Serializable]
        public class DockingStateManager : Manager
        {
            private DockingManager dockingManager;
            private GisEditorUserControl gisEditorUserControl;

            public DockingStateManager(DockingManager dockingManager, GisEditorUserControl gisEditorUserControl)
            {
                this.dockingManager = dockingManager;
                this.gisEditorUserControl = gisEditorUserControl;
            }

            protected override void ApplySettingsCore(StorableSettings settings)
            {
                base.ApplySettingsCore(settings);
                bool applied = false;
                if (settings.GlobalSettings.ContainsKey("Layout"))
                {
                    try
                    {
                        ApplySettingsInternal(XElement.Parse(settings.GlobalSettings["Layout"]));
                        applied = true;
                    }
                    catch { }
                }

                if (!applied)
                {
                    ApplySettingsInternal(null);
                }
            }

            protected override StorableSettings GetSettingsCore()
            {
                var settings = base.GetSettingsCore();
                settings.GlobalSettings["Layout"] = GetSettingsInternal().ToString();
                return settings;
            }

            private XElement GetSettingsInternal()
            {
                if (dockingManager.IsLoaded)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        dockingManager.SaveLayout(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        XElement xElement = XElement.Load(ms);
                        return xElement;
                    }
                }
                else return null;
            }

            private void ApplySettingsInternal(XElement stateXml)
            {
                Stream ms = new MemoryStream();
                if (stateXml == null)
                {
                    var streamInfo = Application.GetResourceStream(new Uri("/MapSuiteGisEditor;component/Resources/Layout.xml", UriKind.RelativeOrAbsolute));
                    ms = streamInfo.Stream;
                }
                else
                {
                    stateXml.Save(ms);
                }

                ms.Seek(0, SeekOrigin.Begin);
                var dockableContentXmls = XElement.Load(ms).Descendants("DockableContent").ToArray();
                var unsavedDockableContents = new Collection<DockableContent>();
                foreach (var content in dockingManager.DockableContents)
                {
                    var inLayout = dockableContentXmls.Any(d =>
                    {
                        var nameAttribute = d.Attribute("Name");
                        return nameAttribute != null && nameAttribute.Value.Equals(content.Name);
                    });

                    if (!inLayout && content.Tag != null)
                    {
                        var isVisible = false;
                        if (bool.TryParse(content.Tag.ToString(), out isVisible) && isVisible)
                        {
                            unsavedDockableContents.Add(content);
                        }
                    }
                }

                RestoreDockWindowLayout(ms);

                IEnumerable<DockWindowViewModel> contents = gisEditorUserControl.ltb.ItemsSource as IEnumerable<DockWindowViewModel>;
                foreach (var dockableContent in dockingManager.DockableContents.Reverse())
                {
                    DockWindowViewModel content = contents.FirstOrDefault(c => c.CurrentDockableContent == dockableContent);
                    if (content != null)
                    {
                        if (dockableContent.State == DockableContentState.Hidden || dockableContent.State == DockableContentState.None)
                        {
                            dockableContent.Tag = false;
                            content.HookVisibility = Visibility.Collapsed;
                        }
                        else
                        {
                            dockableContent.Tag = true;
                            content.HookVisibility = Visibility.Visible;
                        }
                    }
                }

                foreach (var content in unsavedDockableContents)
                {
                    content.Show();
                    content.Tag = true;
                }
            }

            private void RestoreDockWindowLayout(Stream ms)
            {
                // TODO: Make restore layerout works correctly, now it may be a bug that we have to restore layout twice, if not the layout cannot restore.
                ms.Seek(0, SeekOrigin.Begin);
                dockingManager.RestoreLayout(ms);

                ms.Seek(0, SeekOrigin.Begin);
                dockingManager.RestoreLayout(ms);
            }
        }

        [Serializable]
        public class DocumentState
        {
            public Overlay ActiveOverlay { get; set; }

            public Layer ActiveLayer { get; set; }

            public InteractiveOverlay ActiveInteractiveOverlay { get; set; }
        }

        private static LinearGradientBrush highlightBrush;
        private static SolidColorBrush transparentBrush;
        private static SolidColorBrush borderBrush;

        [Obfuscation]
        private void ListBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DockWindowViewModel dockWindowViewModel = ((FrameworkElement)sender).DataContext as DockWindowViewModel;
            if (dockWindowViewModel != null && !(dockWindowViewModel.CurrentDockableContent.Content is Separator))
            {
                dockWindowViewModel.HookVisibility = dockWindowViewModel.HookVisibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
            }
        }

        [Obfuscation]
        private void DockWindowToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var sortedDockWindows = GisEditor.DockWindowManager.GetSortedDockWindows();
            ObservableCollection<DockableContent> dockableContentsForDataBinding = new ObservableCollection<DockableContent>();
            foreach (var dockWindow in sortedDockWindows)
            {
                var resultDockableContent = dockableContents.FirstOrDefault(d => d.Content == dockWindow.Content);
                if (resultDockableContent != null)
                {
                    dockableContentsForDataBinding.Add(resultDockableContent);
                }
                else
                {
                    dockableContentsForDataBinding.Add(ConvertToDockableContent(dockWindow));
                }
            }
            ltb.ItemsSource = dockableContentsForDataBinding.Select(d => new DockWindowViewModel(d)).ToList();
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Border border = (Border)sender;
            border.BorderBrush = transparentBrush;
            border.Background = transparentBrush;
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = (Border)sender;
            DockWindowViewModel dockWindowViewModel = border.DataContext as DockWindowViewModel;
            if (dockWindowViewModel == null || !(dockWindowViewModel.CurrentDockableContent.Content is Separator))
            {
                border.BorderBrush = borderBrush;
                border.Background = highlightBrush;
            }
        }
    }

    [Obfuscation]
    internal class DockWindowViewModel : ViewModelBase
    {
        private bool isChecked;
        private Visibility hookVisibility;
        private DockableContent currentDockableContent;

        public DockWindowViewModel(DockWindow dockWindow)
        {
        }

        public DockWindowViewModel(DockableContent dockableContent)
        {
            if (dockableContent != null)
            {
                this.currentDockableContent = dockableContent;
                if (currentDockableContent.Tag != null && (bool)currentDockableContent.Tag)
                {
                    this.hookVisibility = Visibility.Visible;
                }
                else
                {
                    this.hookVisibility = Visibility.Hidden;
                }
            }
        }

        public DockableContent CurrentDockableContent
        {
            get { return currentDockableContent; }
        }

        public string Name
        {
            get { return currentDockableContent.Title; }
        }

        public Visibility HookVisibility
        {
            get { return hookVisibility; }
            set
            {
                hookVisibility = value;
                RaisePropertyChanged("HookVisibility");
                if (currentDockableContent != null)
                {
                    if (hookVisibility == Visibility.Visible)
                    {
                        FloatingDockablePane floatingPane = currentDockableContent.ContainerPane as FloatingDockablePane;
                        if (floatingPane == null && currentDockableContent.ContainerPane == null)
                        {
                            currentDockableContent.Show();
                        }

                        currentDockableContent.Tag = true;
                    }
                    else
                    {
                        currentDockableContent.Hide();
                        currentDockableContent.Tag = false;
                    }
                }
            }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                RaisePropertyChanged("IsChecked");
            }
        }
    }
}