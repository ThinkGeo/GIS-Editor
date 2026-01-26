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
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BingMapDataRepositoryItem : DataRepositoryItem, IStorableSettings
    {
        private string clientId;
        private string clientSecret;
        private ThinkGeoCloudRasterMapsMapType mapType;

        public BingMapDataRepositoryItem()
        {
            Name = GisEditor.LanguageManager.GetStringResource("BingMapsConfigWindowTitle");
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/BingMaps.PNG", UriKind.RelativeOrAbsolute));
            clientId = BaseMapsHelper.ThinkGeoCloudClientId;
            clientSecret = BaseMapsHelper.ThinkGeoCloudClientSecret;
            mapType = ThinkGeoCloudRasterMapsMapType.Light;
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
            get { return clientId; }
            set { clientId = value; }
        }

        internal string ThinkGeoCloudClientSecret
        {
            get { return clientSecret; }
            set { clientSecret = value; }
        }

        internal ThinkGeoCloudRasterMapsMapType ThinkGeoCloudMapType
        {
            get { return mapType; }
            set { mapType = value; }
        }

        protected override void LoadCore()
        {
            BaseMapsHelper.AddThinkGeoCloudRasterMapsOverlay(GisEditor.ActiveMap);
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
            settings.GlobalSettings["ThinkGeoCloudClientId"] = BingMapsKey;
            settings.GlobalSettings["ThinkGeoCloudClientSecret"] = ThinkGeoCloudClientSecret;
            settings.GlobalSettings["ThinkGeoCloudRasterMapType"] = ThinkGeoCloudMapType.ToString();
            return settings;
        }

        public void ApplySettings(StorableSettings settings)
        {
            if (settings.GlobalSettings.ContainsKey("ThinkGeoCloudClientId"))
            {
                clientId = settings.GlobalSettings["ThinkGeoCloudClientId"];
            }
            else if (settings.GlobalSettings.ContainsKey("BingMapsKey"))
            {
                clientId = settings.GlobalSettings["BingMapsKey"];
            }

            if (settings.GlobalSettings.ContainsKey("ThinkGeoCloudClientSecret"))
            {
                clientSecret = settings.GlobalSettings["ThinkGeoCloudClientSecret"];
            }

            if (settings.GlobalSettings.ContainsKey("ThinkGeoCloudRasterMapType"))
            {
                mapType = (ThinkGeoCloudRasterMapsMapType)Enum.Parse(typeof(ThinkGeoCloudRasterMapsMapType), settings.GlobalSettings["ThinkGeoCloudRasterMapType"]);
            }
            else if (settings.GlobalSettings.ContainsKey("BingMapType"))
            {
                mapType = ConvertLegacyBingMapType(settings.GlobalSettings["BingMapType"]);
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
                var mapsWithRasters = maps.Select(m => new { Map = m, RasterOverlays = m.Overlays.OfType<ThinkGeoCloudRasterMapsOverlay>().ToList() })
                    .Where(o => o.RasterOverlays.Count > 0).ToList();

                var hasCredentials = !String.IsNullOrEmpty(BingMapsKey) && !String.IsNullOrEmpty(ThinkGeoCloudClientSecret);
                var needAskForKey = false;
                foreach (var mapWithRasters in mapsWithRasters)
                {
                    foreach (var rasterOverlay in mapWithRasters.RasterOverlays)
                    {
                        if (hasCredentials)
                        {
                            rasterOverlay.ClientId = BingMapsKey;
                            rasterOverlay.ClientSecret = ThinkGeoCloudClientSecret;
                        }
                        else if (mapWithRasters.Map.Overlays.Contains(rasterOverlay))
                        {
                            needAskForKey = true;
                            mapWithRasters.Map.Overlays.Remove(rasterOverlay);
                        }
                    }
                }

                if (needAskForKey && mapsWithRasters.Count > 0)
                {
                    BingMapsConfigWindow configWindow = new BingMapsConfigWindow();
                    if (configWindow.ShowDialog().GetValueOrDefault())
                    {
                        foreach (var mapWithRasters in mapsWithRasters)
                        {
                            foreach (var rasterOverlay in mapWithRasters.RasterOverlays)
                            {
                                if (!mapWithRasters.Map.Overlays.Contains(rasterOverlay))
                                {
                                    rasterOverlay.ClientId = configWindow.BingMapsKey;
                                    rasterOverlay.ClientSecret = configWindow.ClientSecret;
                                    mapWithRasters.Map.Overlays.Insert(0, rasterOverlay);
                                }
                            }

                            if (mapWithRasters.Map.ActualWidth != 0 || mapWithRasters.Map.ActualHeight != 0)
                            {
                                mapWithRasters.Map.RefreshAsync();
                            }
                        }
                    }
                }
            }
        }

        private static ThinkGeoCloudRasterMapsMapType ConvertLegacyBingMapType(string legacyType)
        {
            if (String.IsNullOrWhiteSpace(legacyType)) return ThinkGeoCloudRasterMapsMapType.Default;

            if (Enum.TryParse(legacyType, true, out BingMapsMapType bingType))
            {
                switch (bingType)
                {
                    case BingMapsMapType.Aerial:
                        return ThinkGeoCloudRasterMapsMapType.Aerial;
                    case BingMapsMapType.AerialWithLabels:
                        return ThinkGeoCloudRasterMapsMapType.Hybrid;
                    case BingMapsMapType.CanvasDark:
                        return ThinkGeoCloudRasterMapsMapType.Dark;
                    case BingMapsMapType.Road:
                    default:
                        return ThinkGeoCloudRasterMapsMapType.Light;
                }
            }

            return ThinkGeoCloudRasterMapsMapType.Default;
        }
    }
}

