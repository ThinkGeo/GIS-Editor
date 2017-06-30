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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class StylePropertyUIPlugin : UIPlugin
    {
        private StylePropertyUI stylePropertyUI;
        private StylePropertyViewModel stylePropertyViewModel;
        private DockWindow stylePropertyDockWindow;

        public StylePropertyUIPlugin()
            : base()
        {
            Description = "This plugin allows us to change styles in a dock window";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/tree_view.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/tree_view.png", UriKind.RelativeOrAbsolute));
            Index = UIPluginOrder.StylePropertyPlugin;
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (stylePropertyUI == null)
            {
                stylePropertyUI = new StylePropertyUI();
            }

            if (stylePropertyViewModel == null)
            {
                stylePropertyViewModel = new StylePropertyViewModel();
            }

            if (stylePropertyDockWindow == null)
            {
                stylePropertyDockWindow = new DockWindow();
                stylePropertyDockWindow.Content = stylePropertyUI;
                stylePropertyDockWindow.Content.DataContext = stylePropertyViewModel;
                stylePropertyDockWindow.Name = "StyleProperties";
                stylePropertyDockWindow.Title = "Style Properties";
                stylePropertyDockWindow.Position = DockWindowPosition.Right;
                stylePropertyDockWindow.StartupMode = DockWindowStartupMode.Hide;
            }

            if (!DockWindows.Contains(stylePropertyDockWindow))
            {
                DockWindows.Add(stylePropertyDockWindow);
            }

            GisEditor.LayerListManager.SelectedLayerListItemChanged -= LayerListManager_SelectedLayerListItemChanged;
            GisEditor.LayerListManager.SelectedLayerListItemChanged += LayerListManager_SelectedLayerListItemChanged;
        }

        private void LayerListManager_SelectedLayerListItemChanged(object sender, SelectedLayerListItemChangedLayerListManagerEventArgs e)
        {
            StyleUserControl currentStyleUI = null;
            StyleLayerListItem styleLayerListItem = e.NewValue as StyleLayerListItem;
            if (styleLayerListItem != null)
            {
                Style style = styleLayerListItem.ConcreteObject as Style;
                FeatureLayer featureLayer = GetConcreteObjectContaining<FeatureLayer>(styleLayerListItem);
                if (featureLayer != null && featureLayer.IsVisible && style != null)
                {
                    StyleBuilderArguments argument = new StyleBuilderArguments();
                    argument.FeatureLayer = featureLayer;
                    argument.StyleToEdit = new CompositeStyle(style);
                    argument.FillRequiredColumnNames();

                    currentStyleUI = styleLayerListItem.GetUI(argument);
                    stylePropertyViewModel.FeatureLayer = featureLayer;
                }
            }

            stylePropertyViewModel.StylePropertyContent = currentStyleUI;
        }

        private static T GetConcreteObjectContaining<T>(LayerListItem currentItem) where T : class
        {
            T featureLayer = null;
            LayerListItem layerListItem = GetSpecificLayerListItem<T>(currentItem);
            if (layerListItem != null)
            {
                featureLayer = (T)layerListItem.ConcreteObject;
            }

            return featureLayer;
        }

        public static LayerListItem GetSpecificLayerListItem<T>(LayerListItem currentItem) where T : class
        {
            if (currentItem.ConcreteObject is T)
            {
                return currentItem;
            }
            else if (currentItem.Parent != null)
            {
                return GetSpecificLayerListItem<T>(currentItem.Parent);
            }
            else return null;
        }
    }
}
