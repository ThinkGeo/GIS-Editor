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


using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetRemoveFeatureMenuItem()
        {
            var command = new ObservedCommand(RemoveFeature, () => true);
            return GetMenuItem("Remove", "/GisEditorPluginCore;component/Images/unload.png", command);
        }

        private static void RemoveFeature()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is MapShape)
            {
                var measureOverlay = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as MeasureTrackInteractiveOverlay;
                if (measureOverlay != null)
                {
                    var key = (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as MapShape).Feature.Id;
                    measureOverlay.ShapeLayer.MapShapes.Remove(key);
                    GisEditor.ActiveMap.Refresh(measureOverlay);
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(measureOverlay, RefreshArgsDescription.RemoveFeatureDescription));
                }
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Feature)
            {
                Feature feature = (Feature)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject;
                InMemoryFeatureLayer inMemoryFeatureLayer = GetInMemoryFeatureLayerByFeature(GisEditor.ActiveMap, feature);
                if (inMemoryFeatureLayer != null)
                {
                    inMemoryFeatureLayer.InternalFeatures.Remove(feature);
                    if (inMemoryFeatureLayer.FeatureIdsToExclude.Contains(feature.Id))
                    {
                        inMemoryFeatureLayer.FeatureIdsToExclude.Remove(feature.Id);
                    }

                    Overlay overlay = GisEditor.ActiveMap.GetOverlaysContaining(inMemoryFeatureLayer).FirstOrDefault();
                    if (overlay == null)
                    {
                        overlay = GetFirstOverlayContainsLayer(inMemoryFeatureLayer, GisEditor.ActiveMap);
                    }

                    if (overlay != null)
                    {
                        GisEditor.ActiveMap.Refresh(overlay);
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(feature, RefreshArgsDescription.RemoveFeatureDescription));
                    }
                }
            }
        }

        private static InMemoryFeatureLayer GetInMemoryFeatureLayerByFeature(WpfMap wpfMap, Feature feature)
        {
            var result = (from featureLayer in
                              (from overlay in wpfMap.Overlays.OfType<LayerOverlay>()
                               from layer in overlay.Layers.OfType<InMemoryFeatureLayer>()
                               select layer).Concat(CollectFeatureLayersInInteractiveOverlay(wpfMap))
                          where featureLayer.InternalFeatures.Contains(feature)
                          select featureLayer).FirstOrDefault();

            return result;
        }

        private static IEnumerable<InMemoryFeatureLayer> CollectFeatureLayersInInteractiveOverlay(WpfMap wpfMap)
        {
            return new InMemoryFeatureLayer[] {
                wpfMap.TrackOverlay.TrackShapeLayer,
                wpfMap.EditOverlay.EditShapesLayer,
            }.Concat(wpfMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>().Select(o => o.TrackShapeLayer))
            .Concat(wpfMap.InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>().Select(o => o.TrackShapeLayer));
        }

        private static Overlay GetFirstOverlayContainsLayer(Layer layer, WpfMap wpfMap)
        {
            if (wpfMap.TrackOverlay.TrackShapeLayer == layer) return wpfMap.TrackOverlay;
            else if (wpfMap.EditOverlay.EditShapesLayer == layer) return wpfMap.EditOverlay;
            else
            {
                Overlay overlay = null;
                foreach (var tmpOverlay in wpfMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>())
                {
                    if (tmpOverlay.TrackShapeLayer == layer)
                    {
                        overlay = tmpOverlay;
                        break;
                    }
                }

                if (overlay == null)
                {
                    foreach (var tmpOverlay in wpfMap.InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>())
                    {
                        if (tmpOverlay.TrackShapeLayer == layer)
                        {
                            overlay = tmpOverlay;
                            break;
                        }
                    }
                }

                return overlay;
            }
        }
    }
}