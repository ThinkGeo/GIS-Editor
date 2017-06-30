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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BingMapDataRepositoryItem : DataRepositoryItem, IStorableSettings
    {
        private string bingMapsKey;
        private Layers.BingMapsMapType bingMapType;

        public BingMapDataRepositoryItem()
        {
            Name = GisEditor.LanguageManager.GetStringResource("BingMapsConfigWindowTitle");
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/BingMaps.PNG", UriKind.RelativeOrAbsolute));
            bingMapsKey = string.Empty;
            GisEditor.ProjectManager.Opened += ProjectManager_Opened;

            if (IsLoadable)
            {
                MenuItem propertMenuItem = new MenuItem();
                propertMenuItem.Header = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties");
                propertMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/properties.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
                propertMenuItem.Command = new RelayCommand(ShowProperties);

                ContextMenu.Items.Add(propertMenuItem);
            }
        }

        private void ShowProperties()
        {
            BingMapsConfigWindow configWindow = new BingMapsConfigWindow();
            configWindow.ShowDialog();
        }

        protected override bool IsLeafCore
        {
            get { return true; }
        }

        protected override bool IsLoadableCore
        {
            get { return true; }
        }

        internal string BingMapsKey
        {
            get { return bingMapsKey; }
            set { bingMapsKey = value; }
        }

        internal Layers.BingMapsMapType BingMapType
        {
            get { return bingMapType; }
            set { bingMapType = value; }
        }

        protected override void LoadCore()
        {
            BaseMapsHelper.AddBingMapsOverlay(GisEditor.ActiveMap);
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadCoreDescription));
        }

        protected override Collection<DataRepositoryItem> GetSearchResultCore(IEnumerable<string> keywords)
        {
            var result = new Collection<DataRepositoryItem>();
            if (keywords.Any(keyWord => Name.IndexOf(keyWord, StringComparison.OrdinalIgnoreCase) != -1))
            {
                var item = new BingMapDataRepositoryItem();
                item.Icon = null;
                result.Add(item);
            }
            return result;
        }

        public StorableSettings GetSettings()
        {
            var settings = new StorableSettings();
            settings.GlobalSettings["BingMapsKey"] = BingMapsKey;
            settings.GlobalSettings["BingMapType"] = BingMapType.ToString();
            return settings;
        }

        public void ApplySettings(StorableSettings settings)
        {
            if (settings.GlobalSettings.ContainsKey("BingMapType"))
            {
                bingMapType = (Layers.BingMapsMapType)Enum.Parse(typeof(Layers.BingMapsMapType), settings.GlobalSettings["BingMapType"]);
            }
        }

        public SettingUserControl GetSettingsUI()
        {
            return null;
        }

        private void ProjectManager_Opened(object sender, OpenedProjectManagerEventArgs e)
        {
            ProjectPluginManager projectManager = sender as ProjectPluginManager;
            if (projectManager != null)
            {
                var maps = projectManager.GetDeserializedMaps();
                var mapsWithBings = maps.Select(m => new { Map = m, BingOverlays = m.Overlays.OfType<BingMapsOverlay>().ToList() })
                    .Where(o => o.BingOverlays.Count > 0).ToList();

                var hasKey = !String.IsNullOrEmpty(BingMapsKey);
                var needAskForKey = false;
                foreach (var mapWithBings in mapsWithBings)
                {
                    foreach (var bing in mapWithBings.BingOverlays)
                    {
                        if (hasKey) bing.ApplicationId = BingMapsKey;
                        else if (mapWithBings.Map.Overlays.Contains(bing))
                        {
                            needAskForKey = true;
                            mapWithBings.Map.Overlays.Remove(bing);
                        }
                    }
                }

                if (needAskForKey && mapsWithBings.Count > 0)
                {
                    BingMapsConfigWindow configWindow = new BingMapsConfigWindow();
                    if (configWindow.ShowDialog().GetValueOrDefault())
                    {
                        foreach (var mapWithBings in mapsWithBings)
                        {
                            foreach (var bing in mapWithBings.BingOverlays)
                            {
                                if (!mapWithBings.Map.Overlays.Contains(bing))
                                {
                                    bing.ApplicationId = BingMapsKey;
                                    mapWithBings.Map.Overlays.Insert(0, bing);
                                }
                            }

                            if (mapWithBings.Map.ActualWidth != 0 || mapWithBings.Map.ActualHeight != 0)
                            {
                                mapWithBings.Map.Refresh();
                            }
                        }
                    }
                }
            }
        }
    }
}
