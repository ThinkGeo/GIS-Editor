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
    public class MrSidRasterLayerPlugin : RasterLayerPlugin
    {
        public MrSidRasterLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("MrSidRasterLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("MrSidRasterLayerPluginDescription");
            ExtensionFilterCore = "MrSid Raster Image (*.sid) |*.sid";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_fileicon_raster.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataformats_mrsid.png", UriKind.RelativeOrAbsolute));
            Index = LayerPluginOrder.MrSidRasterFileLayerPlugin;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<MrSidRasterLayer>(ExtensionFilter,
                l => l.PathFilename,
                (l, newPathFilename) => l.PathFilename = newPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(MrSidRasterLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<MrSidRasterLayer>().PathFilename);
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
            RasterLayer layer = new MrSidRasterLayer(uri.LocalPath);

            //layer.SafeProcess(() =>
            //{
            //    if (layer.HasProjectionText)
            //    {
            //        string proj = layer.GetProjectionText();
            //        if (!proj.Equals(GisEditor.ActiveMap.DisplayProjectionParameters, StringComparison.OrdinalIgnoreCase))
            //        {
            //            Proj4Projection projection = new Proj4Projection();
            //            projection.InternalProjectionParametersString = proj;
            //            projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            //            layer.ImageSource.Projection = projection;
            //        }
            //    }
            //});

            return layer;
        }

        //protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        //{
        //    Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);

        //    foreach (Uri uri in getLayersParameters.LayerUris)
        //    {
        //        var fileName = uri.LocalPath;
        //        MrSidRasterLayer layer = null;
        //        string worldFilePath = Path.ChangeExtension(fileName, "sdw");

        //        if (File.Exists(worldFilePath))
        //        {
        //            layer = new MrSidRasterLayer(Path.GetFullPath(fileName), worldFilePath);
        //        }
        //        else
        //        {
        //            layer = new MrSidRasterLayer(Path.GetFullPath(fileName));
        //        }

        //        layer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
        //        layer.UpperThreshold = double.MaxValue;
        //        layer.LowerThreshold = 0d;
        //        layer.Name = Path.GetFileNameWithoutExtension(fileName);
        //        layer.SafeProcess(() =>
        //        {
        //            if (layer.HasProjectionText)
        //            {
        //                Proj4Projection projection = new Proj4Projection();
        //                projection.InternalProjectionParametersString = layer.GetProjectionText();
        //                projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
        //                layer.ImageSource.Projection = projection;
        //                layer.ImageSource.Projection.Open();
        //            }
        //        });
        //        resultLayers.Add(layer);
        //    }

        //    IEnumerable<RasterLayerInfo> rasterLayerInfos = resultLayers.Select(layer
        //        => new RasterLayerInfo((MrSidRasterLayer)layer, Path.ChangeExtension(((MrSidRasterLayer)layer).PathFilename, "prj")));
        //    LayerPluginHelper.SetInternalProjectionForRasterLayers(rasterLayerInfos);

        //    resultLayers.OfType<MrSidRasterLayer>().Where(m => m.ImageSource.Projection != null).Select(l => l.ImageSource.Projection).ForEach(p => p.Open());

        //    return resultLayers;
        //}
    }
}