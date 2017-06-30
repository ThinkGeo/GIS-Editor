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
    //public class ReplaceFromLibraryMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetReplaceFromLibraryMenuItem()
        {
            var command = new ObservedCommand(ReplaceFromLibrary, () => true);

            return GetMenuItem("Replace from Library...", "/GisEditorInfrastructure;component/Images/replace_from_library.png", command);
        }

        public static void ReplaceFromLibrary()
        {
            StyleLibraryWindow library = new StyleLibraryWindow();
            if (library.ShowDialog().GetValueOrDefault())
            {
                if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                var styleItem = GisEditor.LayerListManager.SelectedLayerListItem;
                //var styleItem = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as StyleItem;
                //if (styleItem != null)
                {
                    TileOverlay containingOverlay = null;
                    var compositeStyle = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as CompositeStyle;
                    if (compositeStyle != null)
                    {
                        FeatureLayer currentFeatureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer;
                        if (currentFeatureLayer != null)
                        {
                            foreach (var zoomLevel in currentFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                            {
                                var index = zoomLevel.CustomStyles.IndexOf(compositeStyle);
                                if (index >= 0)
                                {
                                    zoomLevel.CustomStyles.RemoveAt(index);
                                    zoomLevel.CustomStyles.Insert(index, library.Result.CompositeStyle);
                                }
                            }
                            containingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Parent.ConcreteObject as TileOverlay;
                        }
                    }
                    else if (styleItem.ConcreteObject is Styles.Style && styleItem.Parent.ConcreteObject is Styles.Style)
                    {
                        var index = styleItem.Parent.Children.IndexOf(styleItem);
                        styleItem.Parent.Children.RemoveAt(index);
                        var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                        foreach (var item in compositeStyleItem.Children)
                        {
                            styleItem.Parent.Children.Insert(index, item);
                            index++;
                        }
                        ((StyleLayerListItem)styleItem.Parent).UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    else
                    {
                        styleItem.Children.Clear();
                        var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                        foreach (var item in compositeStyleItem.Children)
                        {
                            styleItem.Children.Add(item);
                        }
                        ((StyleLayerListItem)styleItem).UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    if (containingOverlay != null)
                    {
                        containingOverlay.Invalidate();
                        GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(containingOverlay, RefreshArgsDescriptions.ReplaceFromLibraryDescription));
                    }
                }
            }
        }
    }
}
