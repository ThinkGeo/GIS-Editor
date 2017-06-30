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
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TobinBasFeatureLayerPlugin : FeatureLayerPlugin
    {
        public TobinBasFeatureLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("TobinBasFeatureLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("TobinBasFeatureLayerPluginName");
            ExtensionFilterCore = "Bas file(s) *.bas|*.bas";

            DataSourceResolveToolCore = new FileDataSourceResolveTool<TobinBasFeatureLayer>(ExtensionFilter,
                l => l.TobinBasFilePathName,
                (l, newPathFilename) => l.TobinBasFilePathName = newPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(TobinBasFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<TobinBasFeatureLayer>().TobinBasFilePathName);
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            return SimpleShapeType.Unknown;
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.IsDataSourceAvailable(Layer) instead.")]
        protected override bool IsDataSourceAvailableCore(Layer layer)
        {
            return DataSourceResolveTool.IsDataSourceAvailable(layer);
        }

        [Obsolete("This method is obsoleted, please call DataSourceResolver.ResolveDataSource(Layer) instead.")]
        protected override void ResolveDataSourceCore(Layer layer)
        {
            DataSourceResolveTool.ResolveDataSource(layer);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);
            Collection<Uri> uris = getLayersParameters.LayerUris;

            foreach (Uri uri in uris)
            {
                var fileName = uri.LocalPath;
                try
                {
                    TobinBasFeatureLayer tobinBasFeatureLayer = new TobinBasFeatureLayer(fileName);
                    tobinBasFeatureLayer.Name = Path.GetFileNameWithoutExtension(fileName);
                    tobinBasFeatureLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                    ((TobinBasFeatureSource)tobinBasFeatureLayer.FeatureSource).RequireIndex = false;
                    resultLayers.Add(tobinBasFeatureLayer);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FileLayerPluginUsedByAnotherProcessText"), GisEditor.LanguageManager.GetStringResource("FileLayerPluginFilebeingusedCaption"));
                }
            }

            return resultLayers;
        }

        protected override void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            base.OnGottenLayers(e);
            foreach (var tobinBasFeatureLayer in e.Layers.OfType<TobinBasFeatureLayer>())
            {
                CompositeStyle compositeStyle = new CompositeStyle();
                compositeStyle.Name = tobinBasFeatureLayer.Name;
                AreaStyle areaStyle = AreaStyles.CreateSimpleAreaStyle(new GeoColor(0, GeoColor.SimpleColors.Black), new GeoColor(250, GeoColor.SimpleColors.Black), 1);
                areaStyle.Name = "Area Style";
                LineStyle lineStyle = LineStyles.CreateSimpleLineStyle(GeoColor.SimpleColors.Black, 0.5f, false);
                lineStyle.Name = "Line Style";
                PointStyle pointStyle = PointStyles.CreateSimplePointStyle(PointSymbolType.Circle, GeoColor.SimpleColors.Green, 3);
                pointStyle.Name = "Point Style";

                compositeStyle.Styles.Add(areaStyle);
                compositeStyle.Styles.Add(lineStyle);
                compositeStyle.Styles.Add(pointStyle);

                foreach (var zoomLevel in tobinBasFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                {
                    zoomLevel.CustomStyles.Clear();
                    zoomLevel.CustomStyles.Add(compositeStyle);
                }
            }

            BuildIndexAdapter adapter = new TobinBasBuildIndexAdapter(this);
            adapter.BuildIndex(e.Layers.OfType<FeatureLayer>());
        }
    }
}