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
using System.IO;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class Jpeg2000RasterLayerPlugin : RasterLayerPlugin
    {
        public Jpeg2000RasterLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("Jp2RasterFileLayerPluginJpeg2000RastersName");
            Description = GisEditor.LanguageManager.GetStringResource("Jp2RasterFileLayerPluginSelectJp2IamgeDescription");
            ExtensionFilterCore = "Jpeg2000 Raster File(s) *.jp2|*.jp2";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_jpeg2000.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.Jpeg2000RasterFileLayerPlugin;
            RequireWorldFile = true;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<Jpeg2000RasterLayer>(ExtensionFilter,
                l => l.PathFilename,
                (l, newPathFilename) => l.PathFilename = newPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(Jpeg2000RasterLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<Jpeg2000RasterLayer>().PathFilename);
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

        protected override RasterLayer GetRasterLayer(Uri uri)
        {
            RasterLayer layer = new Jpeg2000RasterLayer(Path.GetFullPath(uri.LocalPath));
            return layer;
        }
    }
}