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


using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AvalonDock;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ContentUIPlugin : UIPlugin
    {
        private const int maxBufferedSize = 512;
        private Collection<GisEditorWpfMap> initializedMaps;
        private ContentSetting option;
        private RibbonEntry contentEntry;

        [NonSerialized]
        private ContentRibbonGroup contentGroup;

        [NonSerialized]
        private ContentsOptionUserControl optionUI;

        public event EventHandler<LayerPluginDropDownOpenedContentPluginEventArgs> LayerPluginDropDownOpened;

        public event EventHandler<LayerPluginDropDownOpeningContentPluginEventArgs> LayerPluginDropDownOpening;

        public ContentUIPlugin()
        {
            Description = GisEditor.LanguageManager.GetStringResource("ContentUIPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/NewContent.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/NewContent.png", UriKind.RelativeOrAbsolute));
            Index = UIPluginOrder.ContentPlugin;
            initializedMaps = new Collection<GisEditorWpfMap>();

            //IndexAdapterManager.Instance.GetPlugins();
            //ShapeAdapterManager.Instance.GetPlugins();
            InteractiveOverlayPluginManager.Instance.GetPlugins();

            contentGroup = new ContentRibbonGroup();
            contentEntry = new RibbonEntry(contentGroup, RibbonTabOrder.Home, "HomeRibbonTabHeader");
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new ContentsOptionUserControl();
                optionUI.DataContext = new ContentsOptionViewModel(Singleton<ContentSetting>.Instance);
            }
            return optionUI;
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            base.RefreshCore(currentMap, refreshArgs);
            var dockManager = GetDockingManager();
            if (dockManager != null)
            {
                var dataControls = dockManager.DockableContents.Select(d => d.Content).OfType<DataViewerUserControl>().ToArray();
                foreach (var dataControl in dataControls)
                {
                    if (!dockManager.Documents.Any(d => d.Content == dataControl.Tag))
                    {
                        var removingDataControl = GisEditor.DockWindowManager.DockWindows.FirstOrDefault(d => d.Content == dataControl);
                        if (removingDataControl != null)
                        {
                            GisEditor.DockWindowManager.DockWindows.Remove(removingDataControl);
                        }
                    }
                }
            }
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            option = Singleton<ContentSetting>.Instance;

            if (!RibbonEntries.Contains(contentEntry)) RibbonEntries.Add(contentEntry);
            if (!StatusBarItems.Contains(StatusBar.GetInstance())) StatusBarItems.Add(StatusBar.GetInstance());
            GisEditor.DockWindowManager.DocumentWindows.CollectionChanged -= DocumentWindows_CollectionChanged;
            GisEditor.DockWindowManager.DocumentWindows.CollectionChanged += DocumentWindows_CollectionChanged;
        }

        private DockingManager GetDockingManager()
        {
            DockingManager dockManager = null;
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                dockManager = Application.Current.MainWindow.FindName("DockManager") as DockingManager;
            }
            return dockManager;
        }

        private void DocumentWindows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && Singleton<ContentSetting>.Instance.DefaultBaseMapOption != DefaultBaseMap.None)
            {
                GisEditorWpfMap gisEditorWpfMap = e.NewItems.OfType<DocumentWindow>().Select(d => d.Content).FirstOrDefault() as GisEditorWpfMap;
                if (gisEditorWpfMap != null)
                {
                    gisEditorWpfMap.Loaded -= GisEditorWpfMap_Loaded;
                    gisEditorWpfMap.Loaded += GisEditorWpfMap_Loaded;
                }
            }
        }

        private void GisEditorWpfMap_Loaded(object sender, RoutedEventArgs e)
        {
            GisEditorWpfMap currentMap = (GisEditorWpfMap)sender;
            if (CheckForInternetAvailability())
            {
                DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
                timer.Interval = TimeSpan.FromMilliseconds(200);
                timer.Tick += (s, e1) =>
                {
                    timer.Stop();
                    switch (Singleton<ContentSetting>.Instance.DefaultBaseMapOption)
                    {
                        case DefaultBaseMap.WorldMapKit:
                            BaseMapsHelper.AddWorldMapKitOverlay(currentMap);
                            GisEditor.UIManager.BeginRefreshPlugins();
                            break;

                        case DefaultBaseMap.OpenStreetMaps:
                            BaseMapsHelper.AddOpenStreetMapOverlay(currentMap);
                            GisEditor.UIManager.BeginRefreshPlugins();
                            break;

                        case DefaultBaseMap.BingMaps:
                            BaseMapsHelper.AddBingMapsOverlay(currentMap);
                            GisEditor.UIManager.BeginRefreshPlugins();
                            break;

                        case DefaultBaseMap.None:
                        default:
                            break;
                    }
                };
                timer.Start();
            }
            currentMap.Loaded -= GisEditorWpfMap_Loaded;
        }

        protected override void UnloadCore()
        {
            contentGroup = null;
            RibbonEntries.Clear();
            StatusBarItems.Clear();
            GisEditor.DockWindowManager.DocumentWindows.CollectionChanged -= DocumentWindows_CollectionChanged;
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);
            InitializeMap(wpfMap);
            FixBaseMapsCacheIssue(wpfMap);
            option.SetPanZoomBarVisiable(wpfMap);

            this.LayerPluginDropDownOpened += UIPlugin_LayerPluginDropDownOpened;
        }

        private void UIPlugin_LayerPluginDropDownOpened(object sender, LayerPluginDropDownOpenedContentPluginEventArgs e)
        {
            InMemoryFeatureLayerPlugin memoryLayerPlugin = e.AvailableLayerPlugins.OfType<InMemoryFeatureLayerPlugin>().FirstOrDefault();
            if (memoryLayerPlugin != null)
            {
                e.AvailableLayerPlugins.Remove(memoryLayerPlugin);
            }
        }

        private void FixBaseMapsCacheIssue(GisEditorWpfMap wpfMap)
        {
            Type[] baseOverlayTypes = new Type[]
            {
                typeof(WorldMapKitMapOverlay),
                typeof(BingMapsOverlay),
                typeof(OpenStreetMapOverlay)
            };

            wpfMap.Overlays.ForEach(o =>
            {
                if (o is WorldMapKitMapOverlay)
                {
                    ((WorldMapKitMapOverlay)o).RefreshCache();
                }
                else if (o is BingMapsOverlay)
                {
                    ((BingMapsOverlay)o).RefreshCache();
                }
                else if (o is OpenStreetMapOverlay)
                {
                    ((OpenStreetMapOverlay)o).RefreshCache();
                }
            });
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            if (initializedMaps.Contains(wpfMap))
            {
                wpfMap.Drop -= Map_Drop;
                initializedMaps.Remove(wpfMap);
            }

            this.LayerPluginDropDownOpened -= UIPlugin_LayerPluginDropDownOpened;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in Singleton<ContentSetting>.Instance.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            Singleton<ContentSetting>.Instance.LoadState(settings.GlobalSettings);
        }

        private void InitializeMap(GisEditorWpfMap currentMap)
        {
            currentMap.AllowDrop = true;
            currentMap.Drop -= Map_Drop;
            currentMap.Drop += Map_Drop;
            currentMap.AddingLayersToActiveOverlay -= CurrentMap_AddingLayersToActiveOverlay;
            currentMap.AddingLayersToActiveOverlay += CurrentMap_AddingLayersToActiveOverlay;
            foreach (var worldMapKitOverlay in currentMap.Overlays.OfType<WorldMapKitMapOverlay>())
            {
                worldMapKitOverlay.ClientId = BaseMapsHelper.WmkClientId;
                worldMapKitOverlay.PrivateKey = BaseMapsHelper.WmkPrivateKey;
            }
            initializedMaps.Add(currentMap);
        }

        private void CurrentMap_AddingLayersToActiveOverlay(object sender, AddingLayersToActiveOverlayEventArgs e)
        {
            e.AddLayersParameters.IsMaxRecordsToDrawEnabled = Singleton<ContentSetting>.Instance.IsLimitDrawgingFeaturesCount;
            e.AddLayersParameters.MaxRecordsToDraw = Singleton<ContentSetting>.Instance.MaxRecordsToDraw;
            e.AddLayersParameters.TileSize = Singleton<ContentSetting>.Instance.TileSize;
            e.AddLayersParameters.IsCacheEnabled = Singleton<ContentSetting>.Instance.UseCache;
            e.AddLayersParameters.ZoomToExtentOfFirstAutomatically = Singleton<ContentSetting>.Instance.IsZoomToExtentOfOnlyFirstLayer;
            e.AddLayersParameters.ZoomToExtentAutomatically = Singleton<ContentSetting>.Instance.IsZoomToExtentOfOnlyFirstLayer ? false : Singleton<ContentSetting>.Instance.IsZoomToExtentOfNewLayer;
            e.AddLayersParameters.DrawingQuality = Singleton<ContentSetting>.Instance.HighQuality ? DrawingQuality.HighQuality : DrawingQuality.HighSpeed;
        }

        private void Map_Drop(object sender, DragEventArgs e)
        {
            LayerListHelper.AddDropFilesToActiveMap(e);
        }

        internal void OnLayerPluginDropDownOpened(ObservableCollection<LayerPlugin> availableLayerPlugins)
        {
            EventHandler<LayerPluginDropDownOpenedContentPluginEventArgs> handler = LayerPluginDropDownOpened;
            if (handler != null)
            {
                handler(null, new LayerPluginDropDownOpenedContentPluginEventArgs(availableLayerPlugins));
            }
        }

        internal void OnLayerPluginDropDownOpening(ObservableCollection<LayerPlugin> availableLayerPlugins)
        {
            EventHandler<LayerPluginDropDownOpeningContentPluginEventArgs> handler = LayerPluginDropDownOpening;
            if (handler != null)
            {
                handler(null, new LayerPluginDropDownOpeningContentPluginEventArgs(availableLayerPlugins));
            }
        }

        private static bool CheckForInternetAvailability()
        {
            //try
            //{
            //    using (var client = new WebClient())
            //    {
            //        using (var stream = client.OpenRead("http://www.google.com"))
            //        {
            //            return true;
            //        }
            //    }
            //}
            //catch
            //{
            //    return false;
            //}

            ObjectQuery objectQuery = new ObjectQuery("select * from Win32_NetworkAdapter where NetConnectionStatus=2");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(objectQuery);
            return (searcher.Get().Count > 0) ? true : false;
        }
    }
}