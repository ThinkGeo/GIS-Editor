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
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class InsertFromLibraryMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetInsertFromLibraryMenuItem()
        {
            var command = new ObservedCommand(InsertFromLibrary, () => true);
            return GetMenuItem("Insert from Library...", "/GisEditorInfrastructure;component/Images/insert_from_library.png", command);
        }

        public static void InsertFromLibrary()
        {
            StyleLibraryWindow library = new StyleLibraryWindow();
            if (library.ShowDialog().GetValueOrDefault())
            {
                var styleItem = GisEditor.LayerListManager.SelectedLayerListItem as StyleLayerListItem;
                if (styleItem != null)
                {
                    TileOverlay containingOverlay = null;
                    var compositeStyle = styleItem.ConcreteObject as CompositeStyle;
                    var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                    if (compositeStyle != null)
                    {
                        foreach (var item in compositeStyleItem.Children.Reverse())
                        {
                            styleItem.Children.Insert(0, item);
                        }
                        styleItem.UpdateConcreteObject();
                        containingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Parent.ConcreteObject as TileOverlay;
                    }
                    else if (styleItem.ConcreteObject is Styles.Style && styleItem.Parent.ConcreteObject is Styles.Style)
                    {
                        var index = styleItem.Parent.Children.IndexOf(styleItem);
                        foreach (var item in compositeStyleItem.Children)
                        {
                            index++;
                            styleItem.Parent.Children.Insert(index, item);
                        }
                        ((StyleLayerListItem)styleItem.Parent).UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    else
                    {
                        foreach (var item in compositeStyleItem.Children.Reverse())
                        {
                            styleItem.Children.Insert(0, item);
                        }
                        styleItem.UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    if (containingOverlay != null)
                    {
                        containingOverlay.Invalidate();
                        GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(containingOverlay, RefreshArgsDescriptions.InsertFromLibraryDescription));
                    }
                }
            }
        }
    }
}