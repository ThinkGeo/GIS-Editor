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
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class PropertiesMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        //private Window propertiesDockWindow;

        public static MenuItem GetPropertiesMenuItem()
        {
            var command = new ObservedCommand(ShowProperties, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));
            return GetMenuItem(GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties"), "/GisEditorInfrastructure;component/Images/properties.png", command);
        }

        private static void ShowProperties()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            FeatureLayer featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            UserControl propertyUI = null;
            if (featureLayer != null)
            {
                var layerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault();
                if (layerPlugin != null)
                {
                    propertyUI = layerPlugin.GetPropertiesUI(featureLayer);
                }
                if (propertyUI == null) propertyUI = new UserControl();//FeatureLayerPropertiesUserControl(featureLayer);
                Window propertiesDockWindow = new Window()
                {
                    Content = propertyUI,
                    Title = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties"),
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style
                };

                propertiesDockWindow.Closing += (s, e1) =>
                {
                    Window window = (Window)s;
                    FeatureLayerPropertiesUserControlViewModel featureLayerViewModel = window.Content.GetDataContext<FeatureLayerPropertiesUserControlViewModel>();

                    if (window.DialogResult.GetValueOrDefault())
                    {
                        if (featureLayerViewModel != null)
                        {
                            GisEditor.LayerManager.FeatureIdColumnNames[featureLayerViewModel.TargetFeatureLayer.FeatureSource.Id] = featureLayerViewModel.FeatureIDColumn;
                        }
                    }

                    e1.Cancel = true;
                    (s as Window).Hide();
                };

                propertiesDockWindow.ShowDialog();
            }
        }
    }
}