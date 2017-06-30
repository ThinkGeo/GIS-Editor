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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        internal static MenuItem GetAddLayerMenuItem()
        {
            ObservedCommand command = new ObservedCommand(() =>
            {
                CommandHelper.AddNewLayersCommand.Execute(true);
            }, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));
            return GetMenuItem("Add layer", "/GisEditorPluginCore;component/Images/add.png", command);
        }

        internal static MenuItem GetTileTypeMenuItem(TileOverlay tileOverlay)
        {
            MenuItem tileTypeItem = new MenuItem();
            tileTypeItem.Header = "Tile type";
            tileTypeItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/tiltType.png", UriKind.Absolute)), Width = 16, Height = 16 };

            MenuItem singleMenuItem = new MenuItem();
            singleMenuItem.Header = "Single tile";
            ObservedCommand singleCommand = new ObservedCommand(() =>
            {
                foreach (var subItem in tileTypeItem.Items.OfType<MenuItem>())
                {
                    if (subItem.Header == singleMenuItem.Header)
                    {
                        singleMenuItem.IsChecked = true;
                    }
                    else
                    {
                        subItem.IsChecked = false;
                    }
                }
                tileOverlay.TileType = TileType.SingleTile;
                tileOverlay.RefreshWithBufferSettings();
            }, () => GisEditor.ActiveMap != null);
            singleMenuItem.Command = singleCommand;

            MenuItem hybridMenuItem = new MenuItem();
            hybridMenuItem.Header = "Multiple tile";
            ObservedCommand hybridCommand = new ObservedCommand(() =>
            {
                foreach (var subItem in tileTypeItem.Items.OfType<MenuItem>())
                {
                    subItem.IsChecked = false;
                }
                hybridMenuItem.IsChecked = true;
                tileOverlay.TileType = TileType.HybridTile;
                tileOverlay.RefreshWithBufferSettings();
            }, () => GisEditor.ActiveMap != null);
            hybridMenuItem.Command = hybridCommand;


            MenuItem preloadMenuItem = new MenuItem();
            preloadMenuItem.Header = "Hybrid tile";
            ObservedCommand preloadCommand = new ObservedCommand(() =>
            {
                foreach (var subItem in tileTypeItem.Items.OfType<MenuItem>())
                {
                    if (subItem.Header == preloadMenuItem.Header)
                    {
                        preloadMenuItem.IsChecked = true;
                    }
                    else
                    {
                        subItem.IsChecked = false;
                    }
                }
                tileOverlay.TileType = TileType.PreloadDataHybridTile;
                tileOverlay.RefreshWithBufferSettings();
            }, () => GisEditor.ActiveMap != null);
            preloadMenuItem.Command = preloadCommand;


            switch (tileOverlay.TileType)
            {
                case TileType.SingleTile:
                    singleMenuItem.IsChecked = true;
                    break;
                case TileType.PreloadDataHybridTile:
                    preloadMenuItem.IsChecked = true;
                    break;
                case TileType.MultipleTile:
                case TileType.HybridTile:
                default:
                    hybridMenuItem.IsChecked = true;
                    break;
            }

            tileTypeItem.Items.Add(singleMenuItem);
            tileTypeItem.Items.Add(hybridMenuItem);
            tileTypeItem.Items.Add(preloadMenuItem);

            return tileTypeItem;
        }

        internal static MenuItem GetNewLayerMenuItem()
        {
            if (GisEditor.LayerManager != null)
            {
                MenuItem newMenuItem = new MenuItem();
                newMenuItem.Header = GisEditor.LanguageManager.GetStringResource("LayerListUserControlNewLayerLabel");
                newMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/addNewLayer.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };

                foreach (var plugin in GisEditor.LayerManager.GetActiveLayerPlugins<FeatureLayerPlugin>()
                    .Where(p => p.CanCreateFeatureLayer))
                {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Header = plugin.Name;
                    var bitmap = plugin.SmallIcon as BitmapImage;
                    if (bitmap != null)
                    {
                        menuItem.Icon = new Image
                        {
                            Source = new BitmapImage(bitmap.UriSource),
                            Width = 16,
                            Height = 16
                        };
                    }
                    menuItem.Command = CommandHelper.CreateNewLayerCommand;
                    menuItem.CommandParameter = plugin.Name;
                    newMenuItem.Items.Add(menuItem);
                }
                if (newMenuItem.Items.Count > 0)
                {
                    return newMenuItem;
                }
            }

            return null;
        }

        internal static MenuItem GetRefreshLayersMenuItem(LayerOverlay overlay)
        {
            MenuItem item = new MenuItem();
            item.Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperRefreshlayersText");
            item.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/refresh.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 }; ;
            ObservedCommand command = new ObservedCommand(() =>
            {
                overlay.Invalidate();
            }, () => { return overlay != null; });
            item.Command = command;
            return item;
        }

        internal static MenuItem GetSetExceptionModeMenuItem()
        {
            //ObservedCommand<DrawingExceptionMode> SetDrawingModeCommand = new ObservedCommand<DrawingExceptionMode>((mode) =>
            //{
            //    var overlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay;
            //    if (overlay != null)
            //    {
            //        overlay.DrawingExceptionMode = mode;
            //    }

            //}, (mode) => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));

            MenuItem newLayerItem = GetMenuItem("Set ExceptionMode", "/GisEditorPluginCore;component/Images/addNewLayer.png", null);

            foreach (var enumItem in Enum.GetNames(typeof(DrawingExceptionMode)))
            {
                MenuItem item = new MenuItem();
                item.Header = enumItem;
                item.Click += new System.Windows.RoutedEventHandler(item_Click);
                //item.Command = SetDrawingModeCommand;
                //item.CommandParameter = Enum.Parse(typeof(DrawingExceptionMode), enumItem);
                newLayerItem.Items.Add(item);

                if (GisEditor.LayerListManager.SelectedLayerListItem != null)
                {
                    var layerOverlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay;
                    if (layerOverlay != null
                        && enumItem.Equals(layerOverlay.DrawingExceptionMode.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        item.IsChecked = true;
                    }
                }
            }

            return newLayerItem;
        }

        private static void item_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var menuItem = ((MenuItem)sender);

            var menu = menuItem.Parent as MenuItem;
            if (menu != null)
            {
                foreach (var item in menu.Items)
                {
                    var menuChild = item as MenuItem;
                    if (menuChild != null)
                        menuChild.IsChecked = false;
                }
            }

            if (GisEditor.LayerListManager.SelectedLayerListItem != null)
            {
                var overlay = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Overlay;
                if (overlay != null)
                {
                    var mode = (DrawingExceptionMode)Enum.Parse(typeof(DrawingExceptionMode), menuItem.Header.ToString());
                    overlay.DrawingExceptionMode = mode;
                    menuItem.IsChecked = true;
                }
            }
        }
    }
}