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
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal partial class LayerListMenuItemHelper
    //public class BingMapStyleMenuItemViewModel
    {
        public static MenuItem GetBingMapStyleMenuItem()
        {
            var menuItem = GetMenuItem("Style", new Image(), null);
            CreateSubItems(menuItem);
            return menuItem;
        }

        private static void CreateSubItems(MenuItem menuItem)
        {
            var enumNames = Enum.GetNames(typeof(BingMapsMapType)).Where(name => !name.Contains("Birdseye")).ToArray();

            for (int i = 0; i < enumNames.Length; i++)
            {
                var subEntity = new MenuItem
                {
                    Header = enumNames[i],
                    IsChecked = GisEditor.ActiveMap.Overlays.OfType<BingMapsOverlay>().First().MapType.ToString() == enumNames[i]
                };

                string enumName = enumNames[i];
                subEntity.Click += (s, e) =>
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                    var bingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as BingMapsOverlay;
                    if (bingOverlay != null)
                    {
                        bingOverlay.MapType = (BingMapsMapType)Enum.Parse(typeof(BingMapsMapType), enumName);
                        bingOverlay.Invalidate();

                        menuItem.Items.OfType<MenuItem>().ForEach(item =>
                        {
                            item.IsChecked = enumName.Equals(item.Header);
                        });
                    }
                };
                menuItem.Items.Add(subEntity);
            }
        }
    }
}