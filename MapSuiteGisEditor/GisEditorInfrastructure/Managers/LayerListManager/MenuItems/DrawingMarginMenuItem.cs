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
    //public class DrawingMarginMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        private static int[] marginValues = new int[] { 50, 60, 80, 100, 150, 200 };

        public static MenuItem GetDrawingMarginMenuItem(FeatureLayer featureLayer)
        {
            var command = new ObservedCommand(() => { }, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));
            var menuItem = GetMenuItem("Drawing Margin", "/GisEditorInfrastructure;component/Images/drawing_margin_32x32.png", command);
            menuItem.ToolTip = "Increase your drawing margin percentage if you are seeing cut off labels or symbols on your map.";

            var subMenuItems = marginValues.Select(marginValue =>
            {
                var subMenuItem = new MenuItem
                {
                    Header = marginValue + "%",
                    IsChecked = marginValue == featureLayer.DrawingMarginInPixel,
                    IsCheckable = true
                };
                subMenuItem.Click += (s, e) => { SetDrawingMargin((MenuItem)s, marginValue); };
                return subMenuItem;
            });

            foreach (var item in subMenuItems)
            {
                menuItem.Items.Add(item);
            }
            return menuItem;
        }

        private static void SetDrawingMargin(MenuItem menuItem, int marginValue)
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                featureLayer.DrawingMarginInPixel = marginValue;
                var parentOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                parentOverlay.Invalidate();

                foreach (MenuItem item in ((MenuItem)menuItem.Parent).Items)
                {
                    item.IsChecked = item.Header.Equals(marginValue.ToString() + "%");
                }
            }
        }
    }
}