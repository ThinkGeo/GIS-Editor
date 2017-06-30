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
using ThinkGeo.MapSuite.Layers;
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
            var enumNames = Enum.GetNames(typeof(WorldMapKitMapType)).ToArray();

            for (int i = 0; i < enumNames.Length; i++)
            {
                var subEntity = new MenuItem
                {
                    Header = enumNames[i],
                    IsChecked = GisEditor.ActiveMap.Overlays.OfType<WorldMapKitMapOverlay>().First().MapType.ToString() == enumNames[i]
                };

                string enumName = enumNames[i];
                subEntity.Click += (s, e) =>
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                    var worldMapKitMapOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as WorldMapKitMapOverlay;
                    if (worldMapKitMapOverlay != null)
                    {
                        worldMapKitMapOverlay.MapType = (WorldMapKitMapType)Enum.Parse(typeof(WorldMapKitMapType), enumName);
                        worldMapKitMapOverlay.Invalidate();

                        menuItem.Items.OfType<MenuItem>().ForEach(item =>
                        {
                            item.IsChecked = item.Header.Equals(enumName);
                        });
                    }
                };
                menuItem.Items.Add(subEntity);
            }
        }
    }
}
