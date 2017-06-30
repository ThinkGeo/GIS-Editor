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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ShapeFileBuildIndexAdapter : BuildIndexAdapter
    {
        private Collection<FeatureLayer> layersInBuildingIndex;

        public ShapeFileBuildIndexAdapter(FeatureLayerPlugin layerPlugin)
            : base(layerPlugin)
        {
            layersInBuildingIndex = new Collection<FeatureLayer>();
        }

        protected override void BuildIndexCore(FeatureLayer featureLayer)
        {
            lock (layersInBuildingIndex)
            {
                if (!layersInBuildingIndex.Contains(featureLayer))
                {
                    layersInBuildingIndex.Add(featureLayer);
                }
            }

            if (layersInBuildingIndex.Count > 0)
            {
                ShapeFileFeatureSource.BuildingIndex -= ShapeFileFeatureSource_BuildingIndex;
                ShapeFileFeatureSource.BuildingIndex += ShapeFileFeatureSource_BuildingIndex;
            }
            var uri = LayerPlugin.GetUri(featureLayer);
            if (uri != null)
            {
                ShapeFileFeatureLayer.BuildIndexFile(uri.LocalPath, BuildIndexMode.DoNotRebuild);

                lock (layersInBuildingIndex)
                {
                    if (layersInBuildingIndex.Contains(featureLayer))
                    {
                        layersInBuildingIndex.Remove(featureLayer);
                    }

                    if (layersInBuildingIndex.Count == 0)
                    {
                        ShapeFileFeatureSource.BuildingIndex -= new EventHandler<BuildingIndexShapeFileFeatureSourceEventArgs>(ShapeFileFeatureSource_BuildingIndex);
                    }
                }
            }
        }

        protected override void SetRequireIndexCore(FeatureLayer featureLayer, bool requireIndex)
        {
            ((ShapeFileFeatureLayer)featureLayer).RequireIndex = requireIndex;
        }

        private void ShapeFileFeatureSource_BuildingIndex(object sender, BuildingIndexShapeFileFeatureSourceEventArgs e)
        {
            BuildingIndexEventArgs args = new BuildingIndexEventArgs(e.RecordCount, e.CurrentRecordIndex, e.CurrentFeature, e.StartProcessTime, e.Cancel);
            OnBuildingIndex(args);
            e.Cancel = args.Cancel;
        }
    }
}