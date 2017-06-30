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
    public abstract class EcwRasterLayerPlugin : RasterLayerPlugin
    {
        public EcwRasterLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("EcwRasterLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("EcwRasterLayerPluginDescription");
            ExtensionFilterCore = "ECW Raster File(s) *.ecw|*.ecw";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_ecw.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.EcwRasterFileLayerPlugin;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<EcwRasterLayer>(ExtensionFilter, 
                l => l.PathFilename, 
                (l, newPathFilename) => l.PathFilename = newPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(EcwRasterLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<EcwRasterLayer>().PathFilename);
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

        protected override RasterLayer GetRasterLayer(Uri uri, WorldFileInfo wInfo)
        {
            EcwRasterLayer layer = null;
            if (File.Exists(wInfo.WorldFilePath))
            {
                layer = new EcwRasterLayer(uri.LocalPath, wInfo.WorldFilePath);
            }
            else
            {
                layer = new EcwRasterLayer(uri.LocalPath);
            }

            return layer;
        }

        //protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        //{
        //    Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);

        //    var layers = getLayersParameters.LayerUris.Select(uri =>
        //    {
        //        var fileName = uri.LocalPath;
        //        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        //        string dirName = Path.GetDirectoryName(fileName);
        //        string worldFileName = Path.Combine(dirName, fileNameWithoutExtension + ".eww");

        //        EcwRasterLayer layer = null;
        //        if (File.Exists(worldFileName))
        //        {
        //            layer = new EcwRasterLayer(fileName, worldFileName);
        //        }
        //        else
        //        {
        //            layer = new EcwRasterLayer(fileName);
        //        }

        //        layer.UpperThreshold = double.MaxValue;
        //        layer.LowerThreshold = 0d;
        //        layer.Name = Path.GetFileNameWithoutExtension(fileName);

        //        return layer;
        //    }).ToArray();

        //    foreach (var layer in layers)
        //    {
        //        resultLayers.Add(layer);
        //    }

        //    IEnumerable<RasterLayerInfo> rasterLayerInfos = resultLayers.Select(layer
        //        => new RasterLayerInfo((EcwRasterLayer)layer, Path.ChangeExtension(((EcwRasterLayer)layer).PathFilename, ".prj")));

        //    LayerPluginHelper.SetInternalProjectionForRasterLayers(rasterLayerInfos);
        //    return resultLayers;
        //}
    }
}