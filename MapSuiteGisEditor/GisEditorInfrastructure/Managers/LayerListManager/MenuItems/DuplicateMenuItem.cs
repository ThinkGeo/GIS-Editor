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


using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    //    public class DuplicateMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetDuplicateMenuItem()
        {
            var command = new ObservedCommand(Duplicate, CanExecute);
            var menuItem = GetMenuItem("Duplicate", "pack://application:,,,/GisEditorInfrastructure;component/Images/duplicate.png", command);
            return menuItem;
        }

        private static bool CanExecute()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem != null)
                return GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Style;
            else return false;
        }

        private static void Duplicate()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            var compositeStyle = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as CompositeStyle;
            bool needRefersh = false;
            if (compositeStyle != null)
            {
                var newStyle = compositeStyle.CloneDeep();
                var featureLayer = LayerListHelper.FindMapElementInTree<FeatureLayer>(GisEditor.LayerListManager.SelectedLayerListItem);
                if (newStyle != null && featureLayer != null)
                {
                    foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
                    {
                        var index = zoomLevel.CustomStyles.IndexOf(compositeStyle);
                        if (index >= 0) zoomLevel.CustomStyles.Insert(index, newStyle);
                    }
                    needRefersh = true;
                }
            }
            else
            {
                var newStyle = (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Style).CloneDeep();
                if (newStyle != null)
                {
                    var newStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(newStyle);
                    if (newStyleItem != null)
                    {
                        var parent = GisEditor.LayerListManager.SelectedLayerListItem.Parent as StyleLayerListItem;
                        if (parent != null)
                        {
                            parent.Children.Insert(0, newStyleItem);
                            parent.UpdateConcreteObject();
                            needRefersh = true;
                        }
                    }
                }
            }
            if (needRefersh)
            {
                var tileOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                if (tileOverlay != null)
                {
                    tileOverlay.Invalidate();
                    GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescriptions.DuplicateDescription));
                }
            }
        }
    }
}
