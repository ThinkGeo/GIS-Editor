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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class KmlFeatureLayerPlugin : FeatureLayerPlugin
    {
        public KmlFeatureLayerPlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("KmlFeatureLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("KmlFeatureLayerPluginDescription");
            ExtensionFilterCore = "Kml File(s) *.kml|*.kml";
            Index = LayerPluginOrder.KMLFeatureLayerPlugin;

            DataSourceResolveToolCore = new FileDataSourceResolveTool<KmlFeatureLayer>(ExtensionFilter,
                l => l.KmlPathFilename,
                (l, newPathFilename) => l.KmlPathFilename = newPathFilename);

        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri(layer.Cast<KmlFeatureLayer>().KmlPathFilename);
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(KmlFeatureLayer);
        }

        protected override void OnGottenLayers(GottenLayersLayerPluginEventArgs e)
        {
            BuildIndexAdapter adapter = new KmlBuildIndexAdapter(this);
            adapter.BuildIndex(e.Layers.OfType<FeatureLayer>());
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
            foreach (var fileName in getLayersParameters.LayerUris.Select(u => u.LocalPath))
            {
                KmlFeatureLayer layer = new KmlFeatureLayer(fileName);
                layer.Name = Path.GetFileNameWithoutExtension(fileName);
                resultLayers.Add(layer);
            }
            return resultLayers;
        }
    }
}
