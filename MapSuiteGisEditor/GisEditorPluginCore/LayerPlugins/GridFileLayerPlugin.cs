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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GridLayerPlugin : FeatureLayerPlugin
    {
        public GridLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("GridFileLayerPluginGridFileName");
            Description = GisEditor.LanguageManager.GetStringResource("GridFileLayerPluginSelectGridDescription");
            ExtensionFilterCore = "Grid File(s) *.grd|*.grd";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_grid.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_grd.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.GeoTiffRasterFileLayerPlugin;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<GridFeatureLayer>(ExtensionFilter,
                l => l.PathFilename,
                (l, newPathFilename) => l.PathFilename = newPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(GridFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<GridFeatureLayer>().PathFilename);
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

            Func<string, bool> checkGRIIsExist = fileName => File.Exists(Path.ChangeExtension(fileName, ".gri"));
            if (getLayersParameters.LayerUris.Select(u => u.LocalPath).Any(checkGRIIsExist))
            {
                MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GridFileLayerPluginFormatNotSupportedText"));
            }

            foreach (var gridFile in getLayersParameters.LayerUris.Select(u => u.LocalPath).Where(fileName => !checkGRIIsExist(fileName)))
            {
                GridFeatureLayer gridFeatureLayer = new GridFeatureLayer(gridFile);
                gridFeatureLayer.Name = Path.GetFileNameWithoutExtension(gridFile);
                resultLayers.Add(gridFeatureLayer);
            }
            return resultLayers;
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            return SimpleShapeType.Area;
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            UserControl propertiesUserControl = base.GetPropertiesUICore(layer);

            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, propertiesUserControl);

            return propertiesUserControl;
        }
    }
}