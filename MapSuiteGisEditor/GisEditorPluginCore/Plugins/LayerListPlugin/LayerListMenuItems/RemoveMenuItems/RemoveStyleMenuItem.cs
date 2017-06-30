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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetRemoveStyleMenuItem()
        {
            var command = new ObservedCommand(RemoveStyle, () => true);
            return GetMenuItem("Remove", "/GisEditorPluginCore;component/Images/unload.png", command);
        }

        private static void RemoveStyle()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer;
            if (featureLayer != null)
            {
                var style = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Style;
                foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
                {
                    if (zoomLevel.CustomStyles.Contains(style))
                        zoomLevel.CustomStyles.Remove(style);
                }
            }
            else
            {
                var parent = GisEditor.LayerListManager.SelectedLayerListItem.Parent as StyleLayerListItem;
                if (parent != null)
                {
                    parent.Children.Remove(GisEditor.LayerListManager.SelectedLayerListItem);
                    parent.UpdateConcreteObject();
                }
            }
            LayerListHelper.RefreshCache();
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescription.RemoveStyleDescription));
        }
    }
}