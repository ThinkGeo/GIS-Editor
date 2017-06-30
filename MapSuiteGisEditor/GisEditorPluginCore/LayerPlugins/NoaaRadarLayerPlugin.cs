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
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NoaaRadarLayerPlugin : LayerPlugin
    {
        public NoaaRadarLayerPlugin()
            : base()
        {
            Author = "ThinkGeo";
            Description = "NOAA Weather Radar";
            Name = "NOAA Weather Radar";
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_radar_ui_icon.png", UriKind.RelativeOrAbsolute));
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_radar_ui_icon.png", UriKind.RelativeOrAbsolute));

            //TODO: Turn this on if the initial load time is too long.  This can delay the loading of the GIS Editor though.
            //NoaaRadarMonitor.StartMonitoring();
        }

        protected override void UnloadCore()
        {
            NoaaRadarMonitor.StopMonitoring();
            base.UnloadCore();
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(NoaaRadarRasterLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri("NoaaRadarRasterLayer:None");
        }

        protected override LayerListItem GetLayerListItemCore(Layer layer)
        {
            LayerListItem layerListItem = base.GetLayerListItemCore(layer);
            if (layerListItem != null)
            {
                Image image = new Image();
                image.Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_radar_ui_icon.png", UriKind.Relative));
                image.Width = 16;
                image.Height = 16;
                layerListItem.PreviewImage = image;
            }

            return layerListItem;
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            NoaaRadarRasterLayer noaaRadarRasterLayer = new NoaaRadarRasterLayer();
            noaaRadarRasterLayer.Transparency = 50 * 2.55f;
            string wgs84Parameters = Proj4Projection.GetWgs84ParametersString();
            noaaRadarRasterLayer.InitializeProj4Projection(wgs84Parameters);
            return new Collection<Layer> { noaaRadarRasterLayer };
        }
    }
}