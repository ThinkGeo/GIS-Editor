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


using System.Collections.ObjectModel;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetZoomToExtentMenuItem()
        {
            var command = new ObservedCommand(ZoomToExtent, () => true);
            return GetMenuItem("Zoom to Extent", "/GisEditorPluginCore;component/Images/zoomextent.png", command);
        }

        private static void ZoomToExtent()
        {
            RectangleShape resultExtent = null;
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Overlay && GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                Collection<RectangleShape> extents = new Collection<RectangleShape>();
                foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    var tmpOverlay = item.ConcreteObject as Overlay;
                    if (tmpOverlay != null)
                    {
                        extents.Add(tmpOverlay.GetBoundingBox());
                    }
                }
                resultExtent = ExtentHelper.GetBoundingBoxOfItems(extents);
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItems.Count > 0)
            {
                Collection<RectangleShape> extents = new Collection<RectangleShape>();
                foreach (var item in GisEditor.LayerListManager.SelectedLayerListItems)
                {
                    Layer tmpLayer = item.ConcreteObject as Layer;
                    if (tmpLayer != null && tmpLayer.HasBoundingBox)
                    {
                        tmpLayer.SafeProcess(() =>
                        {
                            extents.Add(tmpLayer.GetBoundingBox());
                        });
                    }
                }
                resultExtent = ExtentHelper.GetBoundingBoxOfItems(extents);
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Overlay)
            {
                resultExtent = (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay).GetBoundingBox();
            }
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Layer)
            {
                Layer layer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Layer;
                if (layer.HasBoundingBox)
                {
                    layer.SafeProcess(() =>
                    {
                        resultExtent = layer.GetBoundingBox();
                    });
                }
            }

            if (resultExtent != null)
            {
                GisEditor.ActiveMap.CurrentExtent = resultExtent;
                GisEditor.ActiveMap.Refresh();
            }
        }
    }
}