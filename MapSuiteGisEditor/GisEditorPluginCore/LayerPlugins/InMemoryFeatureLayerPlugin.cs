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
using System.Linq;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class InMemoryFeatureLayerPlugin : FeatureLayerPlugin
    {
        public InMemoryFeatureLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("MemoryLayersName");
            //CanConvertToShapeFileCore = true;
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get
            {
                return true;
            }
            set
            {
                base.CanPageFeaturesEfficientlyCore = value;
            }
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(InMemoryFeatureLayer);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> layers = new Collection<Layer>();
            foreach (var uri in getLayersParameters.LayerUris.Where(u => u != null))
            {
                InMemoryFeatureLayer layer = new InMemoryFeatureLayer();
                layer.Name = uri.LocalPath;
                layers.Add(layer);
            }

            return layers;
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            return SimpleShapeType.Unknown;
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri("mem://" + layer.Name);
        }
    }
}