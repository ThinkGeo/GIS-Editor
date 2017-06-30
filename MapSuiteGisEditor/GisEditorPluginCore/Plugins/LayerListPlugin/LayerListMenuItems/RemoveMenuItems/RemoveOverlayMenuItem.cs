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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetRemoveOverlayMenuItem()
        {
            var command = new ObservedCommand(RemoveOverlay, () => true);
            return GetMenuItem("Remove", "/GisEditorPluginCore;component/Images/unload.png", command);
        }

        private static void RemoveOverlay()
        {
            bool needRefresh = false;
            if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                foreach (var overlayEntity in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    var overlay = overlayEntity.ConcreteObject as Overlay;
                    if (overlay != null)
                    {
                        RemoveOneOverlay(overlay, ref needRefresh);
                    }
                }
            }

            else if (GisEditor.LayerListManager.SelectedLayerListItem != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Overlay)
            {
                var removingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay;
                if (removingOverlay != null)
                {
                    MeasureTrackInteractiveOverlay measureTrackInteractiveOverlay = removingOverlay as MeasureTrackInteractiveOverlay;
                    if (measureTrackInteractiveOverlay != null)
                    {
                        if (!measureTrackInteractiveOverlay.IsVisible) measureTrackInteractiveOverlay.IsVisible = true;
                        measureTrackInteractiveOverlay.ShapeLayer.MapShapes.Clear();
                        measureTrackInteractiveOverlay.History.Clear();
                        measureTrackInteractiveOverlay.Refresh();
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(measureTrackInteractiveOverlay, RefreshArgsDescription.ClearCommandDescription));
                    }
                    else
                        RemoveOneOverlay(removingOverlay, ref needRefresh);
                }
            }

            if (needRefresh)
            {
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItems, RefreshArgsDescription.GetRemoveOverlayMenuItemDescription));
            }
        }

        private static void RemoveOneOverlay(Overlay overlay, ref bool needRefresh)
        {
            if (overlay is LayerOverlay)
            {
                foreach (var item in ((LayerOverlay)overlay).Layers.OfType<FeatureLayer>())
                {
                    GisEditor.SelectionManager.ClearSelectedFeatures(item);
                }
            }

            GisEditor.ActiveMap.Overlays.Remove(overlay);
            overlay.Dispose();

            needRefresh = true;
            GisEditor.ActiveMap.ActiveOverlay = GisEditor.ActiveMap.Overlays.LastOrDefault();
        }
    }
}