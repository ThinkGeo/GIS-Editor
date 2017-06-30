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
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetBaseMapsCacheMenuItem()
        {
            MenuItem menuItem = GetMenuItem("Caching", new Image() { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/managecache.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 }, null);
            AddSubItems(menuItem);
            return menuItem;
        }

        private static void AddSubItems(MenuItem menuItem)
        {
            MenuItem openCacheFolderMenuItem = new MenuItem();
            openCacheFolderMenuItem.Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperWindowOpencachefolderContent");
            openCacheFolderMenuItem.Icon = new Image() { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/openfolder.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            openCacheFolderMenuItem.Click += (s, e) =>
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;

                    TileOverlay overlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as TileOverlay;
                    if (overlay != null)
                    {
                        overlay.OpenCacheDirectory();
                    }
                };

            MenuItem clearCacheMenuItem = new MenuItem();
            clearCacheMenuItem.Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperWindowClearcacheContent");
            clearCacheMenuItem.Icon = new Image() { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/clearcache.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            clearCacheMenuItem.Click += (s, e) =>
                {
                    if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;

                    TileOverlay overlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as TileOverlay;
                    if (overlay != null)
                    {
                        overlay.ClearCaches();
                    }
                };

            menuItem.Items.Add(openCacheFolderMenuItem);
            menuItem.Items.Add(clearCacheMenuItem);
        }
    }
}