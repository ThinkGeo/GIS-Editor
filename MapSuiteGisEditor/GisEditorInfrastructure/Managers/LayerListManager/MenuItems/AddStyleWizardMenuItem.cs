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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal partial class LayerListMenuItemHelper
    //public class AddStyleWizardMenuItemViewModel : LayerListMenuItem
    {
        public static MenuItem GetAddStyleWizardMenuItem(FeatureLayer featureLayer)
        {
            var command = new ObservedCommand(() =>
            {
                if (AddStyleToLayerWithStyleWizard(new Layer[] { featureLayer }))
                {
                    LayerListHelper.InvalidateTileOverlay();
                    GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(featureLayer, RefreshArgsDescriptions.GetAddStyleWizardMenuItemDescription));
                }
            }, () => true);

            return GetMenuItem("Style Wizard", "/GisEditorInfrastructure;component/Images/addstyle.png", command);
        }

        public static bool AddStyleToLayerWithStyleWizard(IEnumerable<Layer> layers, bool replaceStyle = false)
        {
            bool addedStyle = false;
            var newLayers = layers.ToArray();
            foreach (var tmpLayer in newLayers)
            {
                var shapeFileFeatureLayer = tmpLayer as FeatureLayer;
                if (shapeFileFeatureLayer != null
                    && newLayers.Length == 1)
                {
                    var styleWizardWindow = GisEditor.ControlManager.GetUI<StyleWizardWindow>();
                    styleWizardWindow.StyleCategories = LayerListHelper.GetStyleCategoriesByFeatureLayer(shapeFileFeatureLayer);
                    styleWizardWindow.StyleCategories = styleWizardWindow.StyleCategories ^ StyleCategories.Composite;
                    styleWizardWindow.StyleCategories = styleWizardWindow.StyleCategories ^ StyleCategories.Label;
                    if ((styleWizardWindow as System.Windows.Window).ShowDialog().GetValueOrDefault())
                    {
                        if (styleWizardWindow.StyleWizardResult != null)
                        {
                            if (GisEditor.ActiveMap != null) GisEditor.ActiveMap.ActiveLayer = shapeFileFeatureLayer;

                            StyleBuilderArguments arguments = new StyleBuilderArguments();
                            arguments.FeatureLayer = shapeFileFeatureLayer;
                            arguments.AvailableStyleCategories = LayerListHelper.GetStyleCategoriesByFeatureLayer(shapeFileFeatureLayer);
                            StylePlugin styleProvider = styleWizardWindow.StyleWizardResult.StylePlugin;

                            arguments.AppliedCallback = new Action<StyleBuilderResult>(args =>
                            {
                                if (args.CompositeStyle != null)
                                {
                                    if (replaceStyle)
                                    {
                                        foreach (var customZoomLevel in shapeFileFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                                        {
                                            customZoomLevel.CustomStyles.Clear();
                                        }
                                    }
                                    AddNewStyleToLayer(shapeFileFeatureLayer, args.CompositeStyle, args.FromZoomLevelIndex, args.ToZoomLevelIndex);
                                }
                            });

                            var newStyle = styleProvider.GetDefaultStyle();
                            newStyle.Name = styleProvider.Name;
                            CompositeStyle componentStyle = new CompositeStyle();
                            componentStyle.Name = shapeFileFeatureLayer.Name;
                            componentStyle.Styles.Add(newStyle);
                            arguments.StyleToEdit = componentStyle;

                            arguments.FillRequiredColumnNames();
                            var styleResult = GisEditor.StyleManager.EditStyle(arguments);
                            if (!styleResult.Canceled)
                            {
                                componentStyle = (CompositeStyle)styleResult.CompositeStyle;
                                arguments.AppliedCallback(styleResult);
                                addedStyle = true;
                            }
                        }
                    }
                    //if (GisEditor.StyleManager.UseWizard != styleWizardWindow.IsAlwaysShowWhenLayerIsAdded)
                    //{
                    //    GisEditor.StyleManager.UseWizard = styleWizardWindow.IsAlwaysShowWhenLayerIsAdded;
                    //    GisEditor.InfrastructureManager.SaveSettings(GisEditor.StyleManager);
                    //}
                }
            }
            return addedStyle;
        }

        private static void AddNewStyleToLayer(FeatureLayer featureLayer, Styles.Style style, int from, int to)
        {
            for (int i = 0; i < GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count; i++)
            {
                var tmpZoomLevel = featureLayer.ZoomLevelSet.CustomZoomLevels[i];
                if (i >= from - 1 && i <= to - 1)
                {
                    tmpZoomLevel.CustomStyles.Add(style);
                }
            }
        }
    }
}