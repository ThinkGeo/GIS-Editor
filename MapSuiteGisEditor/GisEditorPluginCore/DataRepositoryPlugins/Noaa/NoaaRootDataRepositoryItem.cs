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
using System.Linq;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class NoaaRootDataRepositoryItem : DataRepositoryItem
    {
        public NoaaRootDataRepositoryItem()
            : base()
        {
            DataRepositoryItem weatherRadarItem = new NoaaDataRepositoryItem();
            weatherRadarItem.Name = "Weather Radar";
            weatherRadarItem.Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_radar_ui_icon.png", UriKind.RelativeOrAbsolute));
            weatherRadarItem.Loaded += WeatherRadarItem_Loaded;
            Children.Add(weatherRadarItem);

            DataRepositoryItem weatherStationItem = new NoaaDataRepositoryItem();
            weatherStationItem.Name = "Weather Stations";
            weatherStationItem.Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_stations_ui_icon.png", UriKind.RelativeOrAbsolute));
            weatherStationItem.Loaded += WeatherStationItem_Loaded;
            Children.Add(weatherStationItem);

            DataRepositoryItem weatherWarningItem = new NoaaDataRepositoryItem();
            weatherWarningItem.Name = "Weather Warnings";
            weatherWarningItem.Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/weather_warnings.png", UriKind.RelativeOrAbsolute));
            weatherWarningItem.Loaded += WeatherWarningsItem_Loaded;
            Children.Add(weatherWarningItem);
        }

        protected override bool IsLoadableCore
        {
            get { return true; }
        }

        protected override void LoadCore()
        {
            LoadNoaaLayer<NoaaRadarRasterLayer>("NoaaRadarRasterLayer:None", false);
            LoadNoaaLayer<NoaaWeatherStationFeatureLayer>("NoaaWeatherStationFeatureLayer:None");
            LoadNoaaLayer<NoaaWeatherWarningsFeatureLayer>("NoaaWeatherWarningsFeatureLayer:None");
        }

        private void WeatherStationItem_Loaded(object sender, LoadedDataRepositoryItemEventArgs e)
        {
            LoadNoaaLayer<NoaaWeatherStationFeatureLayer>("NoaaWeatherStationFeatureLayer:None");
        }

        private void WeatherWarningsItem_Loaded(object sender, LoadedDataRepositoryItemEventArgs e)
        {
            LoadNoaaLayer<NoaaWeatherWarningsFeatureLayer>("NoaaWeatherWarningsFeatureLayer:None");
        }

        private void WeatherRadarItem_Loaded(object sender, LoadedDataRepositoryItemEventArgs e)
        {
            LoadNoaaLayer<NoaaRadarRasterLayer>("NoaaRadarRasterLayer:None");
        }

        private static void LoadNoaaLayer<T>(string uri, bool refresh = true) where T : Layer
        {
            NoaaWeatherOverlay weatherOverlay = GisEditor.ActiveMap.Overlays.OfType<NoaaWeatherOverlay>().FirstOrDefault();
            if (weatherOverlay == null)
            {
                weatherOverlay = new NoaaWeatherOverlay();
                GisEditor.ActiveMap.Overlays.Add(weatherOverlay);
                GisEditor.ActiveMap.Refresh(weatherOverlay);
            }

            T layer = weatherOverlay.Layers.OfType<T>().FirstOrDefault();
            if (layer == null)
            {
                GetLayersParameters parameters = new GetLayersParameters();
                parameters.LayerUris.Add(new Uri(uri));
                layer = GisEditor.LayerManager.GetLayers<T>(parameters).FirstOrDefault();
                weatherOverlay.Layers.Add(layer);
            }

            layer.IsVisible = true;

            if (refresh)
            {
                weatherOverlay.Refresh();
                GisEditor.UIManager.RefreshPlugins();
            }
        }
    }
}