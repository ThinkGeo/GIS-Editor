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
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetMovementMenuItem(MovementAction movementAction, bool isMovementEnabled = true)
        {
            var command = new ObservedCommand(() => { MoveItem(movementAction); }, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0) && isMovementEnabled);
            string headerString = String.Empty;
            string imageName = String.Empty;
            switch (movementAction)
            {
                case MovementAction.Up:
                    headerString = "Move up";
                    imageName = "moveUp";
                    break;

                case MovementAction.Down:
                    headerString = "Move down";
                    imageName = "moveDown";
                    break;

                case MovementAction.ToTop:
                    headerString = "Move to top";
                    imageName = "toTop";
                    break;

                case MovementAction.ToBottom:
                    headerString = "Move to bottom";
                    imageName = "toBottom";
                    break;
                default:
                    break;
            }
            return GetMenuItem(headerString, string.Format("/GisEditorPluginCore;component/Images/{0}.png", imageName), command);
        }

        private static void MoveItem(MovementAction movementAction)
        {
            var selectedItem = GisEditor.LayerListManager.SelectedLayerListItem;
            if (selectedItem != null && selectedItem.ConcreteObject != null)
            {

                Layer layer = selectedItem.ConcreteObject as Layer;
                Overlay overlay = selectedItem.ConcreteObject as Overlay;
                Style styleItem = selectedItem.ConcreteObject as Style;
                bool needRefresh = false;

                if (layer != null)
                {
                    Overlay parentOverlay = GisEditor.ActiveMap.GetOverlaysContaining(layer).FirstOrDefault();
                    if (parentOverlay is LayerOverlay)
                    {
                        LayerOverlay layerOverlay = (LayerOverlay)parentOverlay;
                        needRefresh = MoveLayerInLayerOverlay(layer, layerOverlay, movementAction);
                    }
                }
                else if (overlay != null)
                {
                    needRefresh = MoveOverlay(overlay, movementAction);
                }
                else if (styleItem != null)
                {
                    var featureLayer = selectedItem.Parent.ConcreteObject as FeatureLayer;
                    if (featureLayer != null && selectedItem is StyleLayerListItem)
                    {
                        int from = 0, to = 0;
                        var array = ((StyleLayerListItem)selectedItem).ZoomLevelRange.Split(" to ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (array.Length == 2)
                        {
                            int.TryParse(array[0].Replace("(", "").Trim(), out from);
                            int.TryParse(array[1].Replace(")", "").Trim(), out to);
                        }
                        needRefresh = MoveStyle(styleItem, featureLayer, from, to, movementAction);
                    }
                    else
                    {
                        var parent = selectedItem.Parent as StyleLayerListItem;
                        if (parent != null)
                        {
                            var currentIndex = parent.Children.IndexOf(selectedItem);
                            var styleCount = parent.Children.Count;
                            switch (movementAction)
                            {
                                case MovementAction.Down:
                                    if (currentIndex + 1 <= styleCount - 1)
                                    {
                                        parent.Children.RemoveAt(currentIndex);
                                        parent.Children.Insert(currentIndex + 1, selectedItem);
                                        parent.UpdateConcreteObject();
                                        needRefresh = true;
                                    }
                                    break;

                                case MovementAction.Up:
                                    if (currentIndex - 1 >= 0)
                                    {
                                        parent.Children.RemoveAt(currentIndex);
                                        parent.Children.Insert(currentIndex - 1, selectedItem);
                                        parent.UpdateConcreteObject();
                                        needRefresh = true;
                                    }
                                    break;

                                case MovementAction.ToTop:
                                    if (currentIndex != 0)
                                    {
                                        parent.Children.RemoveAt(currentIndex);
                                        parent.Children.Insert(0, selectedItem);
                                        parent.UpdateConcreteObject();
                                        needRefresh = true;
                                    }
                                    break;

                                case MovementAction.ToBottom:
                                    if (currentIndex != styleCount - 1)
                                    {
                                        parent.Children.RemoveAt(currentIndex);
                                        parent.Children.Add(selectedItem);
                                        parent.UpdateConcreteObject();
                                        needRefresh = true;
                                    }
                                    break;
                            }
                        }
                    }
                    if (needRefresh)
                    {
                        var tileOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                        if (tileOverlay != null && tileOverlay.MapArguments != null)
                        {
                            tileOverlay.Invalidate();
                        }
                    }
                }
                if (needRefresh) GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(selectedItem, RefreshArgsDescription.MoveItemDescription));
            }
        }

        private static bool MoveStyle(Style style, FeatureLayer featureLayer, int from, int to, MovementAction movementAction)
        {
            var customZoomLevels = featureLayer.ZoomLevelSet.CustomZoomLevels;
            bool needRefresh = false;
            for (int i = from - 1; i < to; i++)
            {
                var zoomLevel = customZoomLevels[i];
                var currentIndex = zoomLevel.CustomStyles.IndexOf(style);
                var styleCount = zoomLevel.CustomStyles.Count;
                switch (movementAction)
                {
                    case MovementAction.Down:
                        if (currentIndex - 1 >= 0)
                        {
                            zoomLevel.CustomStyles.RemoveAt(currentIndex);
                            zoomLevel.CustomStyles.Insert(currentIndex - 1, style);
                            needRefresh = true;
                        }
                        break;

                    case MovementAction.Up:
                        if (currentIndex + 1 <= styleCount - 1)
                        {
                            zoomLevel.CustomStyles.RemoveAt(currentIndex);
                            zoomLevel.CustomStyles.Insert(currentIndex + 1, style);
                            needRefresh = true;
                        }
                        break;

                    case MovementAction.ToTop:
                        if (currentIndex != styleCount - 1)
                        {
                            zoomLevel.CustomStyles.RemoveAt(currentIndex);
                            zoomLevel.CustomStyles.Add(style);
                            needRefresh = true;
                        }
                        break;

                    case MovementAction.ToBottom:
                        if (currentIndex != 0)
                        {
                            zoomLevel.CustomStyles.RemoveAt(currentIndex);
                            zoomLevel.CustomStyles.Insert(0, style);
                            needRefresh = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (needRefresh)
            {
                TileOverlay overlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                if (overlay != null)
                {
                    overlay.Invalidate();
                }
            }
            return needRefresh;
        }

        private static bool MoveOverlay(Overlay overlay, MovementAction movementAction)
        {
            var originalIndex = GisEditor.ActiveMap.Overlays.IndexOf(overlay);
            switch (movementAction)
            {
                case MovementAction.Up:
                    GisEditor.ActiveMap.Overlays.MoveUp(overlay);
                    break;

                case MovementAction.Down:
                    GisEditor.ActiveMap.Overlays.MoveDown(overlay);
                    break;

                case MovementAction.ToTop:
                    GisEditor.ActiveMap.Overlays.MoveToTop(overlay);
                    break;

                case MovementAction.ToBottom:
                    GisEditor.ActiveMap.Overlays.MoveToBottom(overlay);
                    break;
                default:
                    break;
            }
            var currentIndex = GisEditor.ActiveMap.Overlays.IndexOf(overlay);
            var needRefresh = currentIndex != originalIndex;
            if (needRefresh)
                GisEditor.ActiveMap.Refresh();
            return needRefresh;
        }

        private static bool MoveLayerInLayerOverlay(Layer layer, LayerOverlay layerOverlay, MovementAction movementAction)
        {
            var originalIndex = layerOverlay.Layers.IndexOf(layer);
            switch (movementAction)
            {
                case MovementAction.Up:
                    layerOverlay.Layers.MoveUp(layer);
                    break;

                case MovementAction.Down:
                    layerOverlay.Layers.MoveDown(layer);
                    break;

                case MovementAction.ToTop:
                    layerOverlay.Layers.MoveToTop(layer);
                    break;

                case MovementAction.ToBottom:
                    layerOverlay.Layers.MoveToBottom(layer);
                    break;
                default:
                    break;
            }
            var currentIndex = layerOverlay.Layers.IndexOf(layer);
            var needReresh = currentIndex != originalIndex;
            if (needReresh)
            {
                layerOverlay.Invalidate();
            }
            return needReresh;
        }
    }
}