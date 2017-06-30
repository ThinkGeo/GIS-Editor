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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class StylePluginHelper
    {
        private static readonly string common = " Style";
        private static readonly string area = " Area Style";
        private static readonly string line = " Line Style";
        private static readonly string point = " Point Style";
        private static readonly string text = " Style";
        private static readonly string newValue = "...";

        public static StyleBuilderResult EditCompondStyle<T>(T style, StyleBuilderArguments styleArguments) where T : Style
        {
            CompositeStyle compositeStyle = new CompositeStyle();
            compositeStyle.Styles.Add(style);
            styleArguments.StyleToEdit = compositeStyle;
            return GisEditor.StyleManager.EditStyle(styleArguments);
        }

        public static string GetShortName(this StylePlugin stylePlugin)
        {
            if (stylePlugin.Name.Contains(area)
                || stylePlugin.Name.Contains(line)
                || stylePlugin.Name.Contains(point)
                || stylePlugin.Name.Contains(text))
            {
                return stylePlugin.Name.Replace(area, newValue).Replace(line, newValue).Replace(point, newValue).Replace(text, newValue);
            }
            return stylePlugin.Name.Replace(common, newValue);
        }

        public static T GetDefaultStyle<T>(StyleCategories styleProviderTypes) where T : Style
        {
            var provider = GisEditor.StyleManager.GetDefaultStylePlugin(styleProviderTypes);
            if (provider != null)
            {
                T tmpStyle = provider.GetDefaultStyle() as T;
                if (tmpStyle != null)
                {
                    return (T)tmpStyle.CloneDeep();
                }
            }
            return null;
        }


        public static void FillRequiredValueForStyleArguments(StyleBuilderArguments styleArguments)
        {
            if (styleArguments.ColumnNames.Count == 0 && styleArguments.FeatureLayer != null)
            {
                styleArguments.FeatureLayer.SafeProcess(() =>
                {
                    styleArguments.ColumnNames.Clear();
                    foreach (var columnName in styleArguments.FeatureLayer.QueryTools.GetColumns().Select(column => column.ColumnName))
                    {
                        styleArguments.ColumnNames.Add(columnName);
                    }
                });
            }
        }

        public static SimpleShapeType GetWellKnownType(FeatureLayer featureLayer)
        {
            SimpleShapeType type = SimpleShapeType.Unknown;
            if (featureLayer != null)
            {
                var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                type = featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);
            }
            return type;
        }

        public static StyleCategories GetStyleCategoriesByFeatureLayer(FeatureLayer featureLayer)
        {
            StyleCategories resultStyleCategories = StyleCategories.None;
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null)
            {
                var type = featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);
                switch (type)
                {
                    case SimpleShapeType.Point:
                        resultStyleCategories = StyleCategories.Point | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Line:
                        resultStyleCategories = StyleCategories.Line | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Area:
                        resultStyleCategories = StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Unknown:
                    default:
                        resultStyleCategories = StyleCategories.Point | StyleCategories.Line | StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                        break;
                }
            }

            return resultStyleCategories;
        }
    }
}