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
using System.Windows;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    //    public class ViewDataMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetViewDataMenuItem(FeatureLayerPlugin featureLayerPlugin)
        {
            var command = new ObservedCommand(() =>
            {
                UserControl userControl = null;
                if (featureLayerPlugin == null)
                {
                    FeatureLayer selectedLayer = null;
                    if (GisEditor.LayerListManager.SelectedLayerListItem != null)
                        selectedLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
                    userControl = new DataViewerUserControl(selectedLayer);
                }
                else userControl = featureLayerPlugin.GetViewDataUI();
                ViewData(userControl);
            }, () => !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0));
            return GetMenuItem("View data", "/GisEditorInfrastructure;component/Images/viewdata.png", command);
        }

        private static void ViewData(UserControl userControl)
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null ||
                !(GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is FeatureLayer)) return;
            ShowDbfViewer(userControl);
        }

        private static void ShowDbfViewer(UserControl content)
        {
            string newTitle = GisEditor.LanguageManager.GetStringResource("ViewDataViewDataTitle");
            if (GisEditor.ActiveMap != null)
            {
                newTitle += " " + GisEditor.ActiveMap.Name;
            }

            var floatingSize = new Size(800, 600);
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                double floatingWidth = Application.Current.MainWindow.ActualWidth - 100;
                double floatingHeight = Application.Current.MainWindow.ActualHeight - 100;
                if (floatingWidth < 800) floatingWidth = 800;
                if (floatingHeight < 600) floatingHeight = 600;

                floatingSize = new Size(floatingWidth, floatingHeight);
            }

            DockWindow dockWindow = new DockWindow(content, DockWindowPosition.Bottom, newTitle);
            dockWindow.FloatingSize = floatingSize;
            dockWindow.Show(DockWindowPosition.Floating);
        }
    }
}