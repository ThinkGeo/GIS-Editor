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
    public class GdiPlusRasterLayerPlugin : RasterLayerPlugin
    {
        public GdiPlusRasterLayerPlugin()
            : base()
        {
            ExtensionFilterCore = "Image Raster File(s) *.bmp;*.emf;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.wmf|*.bmp;*.emf;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.wmf|"
            + "Bmp(s) *.bmp|*.bmp|"
            + "Emf(s) *.emf|*.emf|"
            + "Gif(s) *.gif|*.gif|"
            + "Jpeg(s) *.jpg;*.jpeg|*.jpg;*.jpeg|"
            + "Png(s) *.png|*.png|"
            + "Tiff(s) *.tif;*.tiff|*.tif;*.tiff|"
            + "Wmf(s) *.wmf|*.wmf";
            Name = GisEditor.LanguageManager.GetStringResource("GdiPlusRasterFileLayerPluginImageRasterFilesName");
            Description = GisEditor.LanguageManager.GetStringResource("GdiPlusRasterFileLayerPluginSelectImageDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_gdiplus.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.GdiPlusRasterFileLayerPlugin;
            RequireWorldFile = true;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<NativeImageRasterLayer>(ExtensionFilter,
                l => l.ImagePathFilename,
                (l, newPathFilename) => l.ImagePathFilename = newPathFilename);

        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(NativeImageRasterLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<NativeImageRasterLayer>().ImagePathFilename);
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
            RasterLayer layer = null;
            layer = new NativeImageRasterLayer(uri.LocalPath);

            return layer;
        }
    }
}