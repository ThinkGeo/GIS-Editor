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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GpxFeatureLayerPlugin : FeatureLayerPlugin
    {
        public GpxFeatureLayerPlugin()
            : base()
        {
            Author = "ThinkGeo";
            Description = "GPS eXchange Files Plugin";
            Name = "GPX (GPS EXchange Format)";

            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_gpx.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_gpx.png", UriKind.RelativeOrAbsolute));
            ExtensionFilterCore = "GPX File(s) *.gpx|*.gpx";

            DataSourceResolveToolCore = new FileDataSourceResolveTool<GpxFeatureLayer>(ExtensionFilter,
                l => l.GpxPathFilename,
                (l, newPathFilename) => l.GpxPathFilename = newPathFilename);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<GpxFeatureLayer>().GpxPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(GpxFeatureLayer);
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
                    var gpxFeatureLayer = new GpxFeatureLayer(fileName);
                    gpxFeatureLayer.Name = Path.GetFileNameWithoutExtension(fileName);
                    gpxFeatureLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;

                    resultLayers.Add(gpxFeatureLayer);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FileLayerPluginUsedByAnotherProcessText"), GisEditor.LanguageManager.GetStringResource("FileLayerPluginFilebeingusedCaption"));
                }
            }

            return resultLayers;
        }
    }
}