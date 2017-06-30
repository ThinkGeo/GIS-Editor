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


using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetRemoveLayerMenuItem()
        {
            var command = new ObservedCommand(Remove, () => true);
            return GetMenuItem("Remove", "/GisEditorInfrastructure;component/Images/unload.png", command);
        }

        private static void Remove()
        {
            bool needRefresh = false;
            if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                foreach (var mapElementEntity in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    Layer layer = mapElementEntity.ConcreteObject as Layer;
                    if (layer != null)
                    {
                        RemoveOneLayer(layer, ref needRefresh);
                    }
                }
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Layer)
            {
                var removingLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Layer;
                if (removingLayer != null)
                {
                    RemoveOneLayer(removingLayer, ref needRefresh);
                }
            }

            if (needRefresh)
            {
                GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItems, RefreshArgsDescriptions.RemoveDescription));
            }
        }

        private static void RemoveOneLayer(Layer layer, ref bool needRefresh)
        {
            if (layer is FeatureLayer)
            {
                GisEditor.SelectionManager.ClearSelectedFeatures((FeatureLayer)layer);
            }

            LayerOverlay tileOverlay = GisEditor.ActiveMap.GetOverlaysContaining(layer).FirstOrDefault();
            if (tileOverlay != null)
            {
                lock (tileOverlay.Layers)
                {
                    tileOverlay.Layers.Remove(layer);
                }
                tileOverlay.Invalidate();
                if (tileOverlay.Layers.Count == 0)
                {
                    GisEditor.ActiveMap.Overlays.Remove(tileOverlay);
                    if (GisEditor.ActiveMap.ActiveOverlay == tileOverlay)
                    {
                        GisEditor.ActiveMap.ActiveOverlay = null;
                    }
                }
                needRefresh = true;

                lock (layer) layer.Close();
                if (GisEditor.ActiveMap != null)
                {
                    GisEditor.ActiveMap.ActiveLayer = null;
                }
            }
        }
    }
}