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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NoaaWeatherWarningsLayerPlugin : FeatureLayerPlugin
    {
        public NoaaWeatherWarningsLayerPlugin()
            : base()
        {
            Author = "ThinkGeo";
            Description = "NOAA Weather Warnings Plugin.";
            Name = "NOAA Weather Warning";

            //TODO; Replace these icons with ones for Weather Warnings
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/weather_warnings.png", UriKind.RelativeOrAbsolute));
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/weather_warnings.png", UriKind.RelativeOrAbsolute));

            //TODO: Turn this on if the initial load time is too long.  This can delay the loading of the GIS Editor though.
            //NoaaWeatherWarningsMonitor.StartMonitoring();
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(NoaaWeatherWarningsFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri("NoaaWeatherWarningsFeatureLayer:None");
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            NoaaWeatherWarningsFeatureLayer noaaWeatherWarningsFeatureLayer = new NoaaWeatherWarningsFeatureLayer();
            GisEditor.LayerManager.FeatureIdColumnNames[noaaWeatherWarningsFeatureLayer.FeatureSource.Id] = "RECID";

            CompositeStyle compositeStyle = new CompositeStyle(new NoaaWeatherWarningsStyle());
            compositeStyle.Name = "Noaa Weather Warnings";

            foreach (ZoomLevel zoomLevel in GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels())
            {
                ZoomLevel newZoomLevel = new ZoomLevel(zoomLevel.Scale);
                newZoomLevel.CustomStyles.Add(compositeStyle);
                noaaWeatherWarningsFeatureLayer.ZoomLevelSet.CustomZoomLevels.Add(newZoomLevel);
            }

            return new Collection<Layer> { noaaWeatherWarningsFeatureLayer };
        }

        protected override string GetInternalProj4ProjectionParametersCore(FeatureLayer featureLayer)
        {
            return Proj4Projection.GetDecimalDegreesParametersString();
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            return SimpleShapeType.Area;
        }
    }
}