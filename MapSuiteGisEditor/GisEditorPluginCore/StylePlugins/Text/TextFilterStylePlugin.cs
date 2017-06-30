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
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextFilterStylePlugin : StylePlugin
    {
        public TextFilterStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("FilterTextStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("TextFilterStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filtertext.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filtertext.png", UriKind.RelativeOrAbsolute));

            RequireColumnNames = true;
            Index = StylePluginOrder.FilterStyle;
            StyleCategories = StyleCategories.Label;
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);

            if (parameters.LayerListItem.ConcreteObject is FilterStyle)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "--";
                menuItems.Add(menuItem);

                MenuItem viewDataMenuItem = new MenuItem();
                viewDataMenuItem.Header = "View filtered data";
                viewDataMenuItem.Click += new System.Windows.RoutedEventHandler(ViewDataMenuItem_Click);
                viewDataMenuItem.Tag = parameters.LayerListItem;
                viewDataMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png", UriKind.RelativeOrAbsolute)) };
                menuItems.Add(viewDataMenuItem);

                MenuItem zoomToFilterMenuItem = new MenuItem();
                zoomToFilterMenuItem.Header = GisEditor.LanguageManager.GetStringResource("FilterStylePluginZoomtofilter");
                zoomToFilterMenuItem.Tag = parameters.LayerListItem;
                zoomToFilterMenuItem.Click += ZoomToFilterMenuItem_Click;
                zoomToFilterMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/zoomto.png", UriKind.RelativeOrAbsolute)) };
                menuItems.Add(zoomToFilterMenuItem);
            }

            return menuItems;
        }

        protected override Style GetDefaultStyleCore()
        {
            return new TextFilterStyle();
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new TextFilterStyleItem(style);
        }

        private void ViewDataMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            LayerListItem layerListItem = (LayerListItem)menuItem.Tag;
            LayerListItem tempItem = layerListItem;
            while (!(tempItem.ConcreteObject is FeatureLayer))
            {
                tempItem = tempItem.Parent;
            }
            FeatureLayer selectedLayer = tempItem.ConcreteObject as FeatureLayer;
            if (selectedLayer != null)
            {
                FilterStyle filterStyle = (FilterStyle)layerListItem.ConcreteObject;
                FilterStyleViewModel.ShowFilteredData(selectedLayer, filterStyle.Conditions, layerListItem.Name);
            }
        }

        private void ZoomToFilterMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            LayerListItem layerListItem = (LayerListItem)menuItem.Tag;
            LayerListItem tempItem = layerListItem;
            while (!(tempItem.ConcreteObject is FeatureLayer))
            {
                tempItem = tempItem.Parent;
            }
            FeatureLayer selectedLayer = tempItem.ConcreteObject as FeatureLayer;
            if (selectedLayer != null)
            {
                Collection<Feature> resultFeatures = new Collection<Feature>();
                selectedLayer.SafeProcess(() =>
                {
                    resultFeatures = selectedLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                });

                FilterStyle filterStyle = (FilterStyle)layerListItem.ConcreteObject;
                foreach (var condition in filterStyle.Conditions)
                {
                    resultFeatures = condition.GetMatchingFeatures(resultFeatures);
                }
                if (resultFeatures.Count > 0)
                {
                    RectangleShape boundingBox = ExtentHelper.GetBoundingBoxOfItems(resultFeatures);
                    GisEditor.ActiveMap.CurrentExtent = boundingBox;
                    GisEditor.ActiveMap.Refresh();
                }
            }
        }
    }
}