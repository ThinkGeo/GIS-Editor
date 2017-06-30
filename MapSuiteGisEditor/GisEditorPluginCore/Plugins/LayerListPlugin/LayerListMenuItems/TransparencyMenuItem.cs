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
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetTransparencyMenuItem(double transparent)
        {
            var menuItem = GetMenuItem("Transparency", "/GisEditorInfrastructure;component/Images/Transparent.png", null);
            menuItem.Items.Add(GetChangeTransparencyItem(100));
            menuItem.Items.Add(GetChangeTransparencyItem(90));
            menuItem.Items.Add(GetChangeTransparencyItem(80));
            menuItem.Items.Add(GetChangeTransparencyItem(70));
            menuItem.Items.Add(GetChangeTransparencyItem(60));
            menuItem.Items.Add(GetChangeTransparencyItem(50));
            menuItem.Items.Add(GetChangeTransparencyItem(40));
            menuItem.Items.Add(GetChangeTransparencyItem(30));
            menuItem.Items.Add(GetChangeTransparencyItem(20));
            menuItem.Items.Add(GetChangeTransparencyItem(10));
            foreach (MenuItem item in menuItem.Items)
            {
                double value = Convert.ToDouble(item.Tag);
                item.IsChecked = value == transparent;
            }
            return menuItem;
        }

        private static MenuItem GetChangeTransparencyItem(double transparencyPa)
        {
            var transparencyMenuItem = new MenuItem();
            transparencyMenuItem.Header = transparencyPa + " %";
            transparencyMenuItem.IsCheckable = true;
            double value = transparencyPa / 100;
            transparencyMenuItem.Tag = value;

            transparencyMenuItem.Click += (s, e) =>
            {
                var selectedMenuItem = (MenuItem)s;
                var transparency = Convert.ToDouble(selectedMenuItem.Tag);
                if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;


                BingMapsOverlay bingMapsOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as BingMapsOverlay;
                OpenStreetMapOverlay openStreetMapOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as OpenStreetMapOverlay;
                WorldMapKitMapOverlay worldMapKitMapOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as WorldMapKitMapOverlay;

                if (bingMapsOverlay != null && bingMapsOverlay.OverlayCanvas.Opacity != transparency)
                {
                    bingMapsOverlay.OverlayCanvas.Opacity = transparency;
                    bingMapsOverlay.Invalidate();
                }
                else if (openStreetMapOverlay != null && openStreetMapOverlay.OverlayCanvas.Opacity != transparency)
                {
                    openStreetMapOverlay.OverlayCanvas.Opacity = transparency;
                    openStreetMapOverlay.Invalidate();
                }
                else if (worldMapKitMapOverlay != null && worldMapKitMapOverlay.OverlayCanvas.Opacity != transparency)
                {
                    worldMapKitMapOverlay.OverlayCanvas.Opacity = transparency;
                    worldMapKitMapOverlay.Invalidate();
                }


                foreach (MenuItem item in ((MenuItem)selectedMenuItem.Parent).Items)
                {
                    if (!item.Equals(selectedMenuItem))
                    {
                        item.IsChecked = false;
                    }
                }
            };
            return transparencyMenuItem;
        }
    }
}