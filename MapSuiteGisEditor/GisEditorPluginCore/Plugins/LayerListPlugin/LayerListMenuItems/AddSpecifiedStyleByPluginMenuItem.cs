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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    //public class AddSpecifiedStyleByPluginMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetAddSpecifiedStyleByPluginMenuItem(StylePlugin styleProvider)
        {
            var image = new Image();
            image.BeginInit();
            image.Source = styleProvider.SmallIcon;
            image.EndInit();
            var command = new ObservedCommand(() => { AddStyle(styleProvider); },
                () =>
                {
                    string stylePluginName = styleProvider.GetType().FullName;
                    return GisEditor.StyleManager.GetActiveStylePlugins().Any(p => p.GetType().FullName.Equals(stylePluginName, StringComparison.Ordinal));
                });

            return GetMenuItem(styleProvider.GetShortName(), image, command);
        }

        private static void AddStyle(StylePlugin styleProvider)
        {
            Style style = null;
            StyleBuilderArguments arguments = new StyleBuilderArguments();
            FeatureLayer currentFeatureLayer = null;
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;

            //add a new style by right-clicking on a layer node
            if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is FeatureLayer)
            {
                currentFeatureLayer = (FeatureLayer)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject;
            }

            //add a new style by right-clicking on a zoomlevel node
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is ZoomLevel)
            {
                ZoomLevel editingZoomLevel = (ZoomLevel)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject;
                arguments.FromZoomLevelIndex = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(editingZoomLevel.Scale) + 1;
                arguments.ToZoomLevelIndex = (int)editingZoomLevel.ApplyUntilZoomLevel;
                currentFeatureLayer = (FeatureLayer)GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject;
            }

            //replace an existing style
            else if (GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Style)
            {
                Style currentStyle = (Style)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject;
                currentFeatureLayer = LayerListHelper.FindMapElementInLayerList<FeatureLayer>(GisEditor.LayerListManager.SelectedLayerListItem);
            }

            arguments.AvailableStyleCategories = StylePluginHelper.GetStyleCategoriesByFeatureLayer(currentFeatureLayer);
            arguments.FeatureLayer = currentFeatureLayer;
            arguments.FillRequiredColumnNames();
            arguments.AppliedCallback = args =>
            {
                if (args.CompositeStyle != null)
                {
                    ZoomLevelHelper.ApplyStyle(args.CompositeStyle, currentFeatureLayer, args.FromZoomLevelIndex, args.ToZoomLevelIndex);
                }
            };

            style = styleProvider.GetDefaultStyle();
            style.Name = styleProvider.Name;
            var componentStyle = new CompositeStyle(style) { Name = currentFeatureLayer.Name };
            arguments.StyleToEdit = componentStyle;
            var styleResults = GisEditor.StyleManager.EditStyle(arguments);
            if (!styleResults.Canceled)
            {
                ZoomLevelHelper.ApplyStyle(styleResults.CompositeStyle, currentFeatureLayer, styleResults.FromZoomLevelIndex, styleResults.ToZoomLevelIndex);
            }
        }
    }
}