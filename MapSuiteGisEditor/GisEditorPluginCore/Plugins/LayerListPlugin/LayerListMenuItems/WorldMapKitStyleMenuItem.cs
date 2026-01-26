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
using System.Windows.Media.Imaging;
using ThinkGeo.UI.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetWorldMapKitStyleMenuItem()
        {
            var menuItem = GetMenuItem("Style", new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/bingmapstyle1.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 }, null);
            CreateWorldMapKitSubItems(menuItem);
            return menuItem;
        }

        private static void CreateWorldMapKitSubItems(MenuItem menuItem)
        {
            var styleOptions = BaseMapsHelper.WorldMapsStyleOptions;
            var currentOverlay = GisEditor.LayerListManager.SelectedLayerListItem?.ConcreteObject as Overlay;
            BaseMapsHelper.TryGetWorldMapsLayer(currentOverlay, out var currentLayer);
            var currentStyleUri = currentLayer?.StyleJsonUri;

            foreach (var styleOption in styleOptions)
            {
                var styleName = styleOption.Name;
                var subEntity = new MenuItem
                {
                    Header = styleName,
                    IsChecked = !string.IsNullOrWhiteSpace(currentStyleUri)
                        && currentStyleUri.Equals(styleOption.StyleJsonUri, StringComparison.OrdinalIgnoreCase)
                };

                var selectedStyle = styleOption;
                subEntity.Click += (s, e) =>
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                    var overlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay;
                    if (overlay == null) return;

                    if (BaseMapsHelper.TryGetWorldMapsLayer(overlay, out var worldMapsLayer))
                    {
                        if (!selectedStyle.StyleJsonUri.Equals(worldMapsLayer.StyleJsonUri, StringComparison.OrdinalIgnoreCase))
                        {
                            worldMapsLayer.StyleJsonUri = selectedStyle.StyleJsonUri;
                            if (GisEditor.ActiveMap != null)
                            {
                                BaseMapsHelper.ConfigureWorldMapsLayer(worldMapsLayer, GisEditor.ActiveMap, GisEditor.ActiveMap.DisplayProjectionParameters);
                            }
                        }

                        overlay.RefreshWithBufferSettings();

                        menuItem.Items.OfType<MenuItem>().ForEach(item =>
                        {
                            item.IsChecked = item.Header.Equals(styleName);
                        });
                    }
                };
                menuItem.Items.Add(subEntity);
            }
        }
    }
}
