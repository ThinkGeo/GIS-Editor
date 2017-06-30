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
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class StyleExtension
    {
        public static AreaStyle EditStyles(this StylePluginManager styleManager, StyleBuilderArguments styleArguments, AreaStyle areaStyle)
        {
            return EditStyles<AreaStyle>(styleManager, styleArguments, areaStyle, s => s.CustomAreaStyles);
        }

        public static LineStyle EditStyles(this StylePluginManager styleManager, StyleBuilderArguments styleArguments, LineStyle lineStyle)
        {
            return EditStyles<LineStyle>(styleManager, styleArguments, lineStyle, s => s.CustomLineStyles);
        }

        public static PointStyle EditStyles(this StylePluginManager styleManager, StyleBuilderArguments styleArguments, PointStyle pointStyle)
        {
            return EditStyles<PointStyle>(styleManager, styleArguments, pointStyle, s => new Collection<PointStyle>(s.CustomPointStyles.OfType<PointStyle>().ToList()));
        }

        public static IconTextStyle EditStyles(this StylePluginManager styleManager, StyleBuilderArguments styleArguments, IconTextStyle textStyle)
        {
            TextStyle resultTextStyle = EditStyles<TextStyle>(styleManager, styleArguments, textStyle, s => s.CustomTextStyles);
            if (resultTextStyle == null) return null;

            IconTextStyle iconTextStyle = new IconTextStyle();
            iconTextStyle.Name = resultTextStyle.Name;
            foreach (var tmpStyle in resultTextStyle.CustomTextStyles)
            {
                iconTextStyle.CustomTextStyles.Add(tmpStyle);
            }

            return iconTextStyle;
        }

        private static T EditStyles<T>(StylePluginManager styleManager, StyleBuilderArguments styleArguments, T editingStyle, Func<T, Collection<T>> fetchInnerStyles) where T : Style, new()
        {
            CompositeStyle compositeStyle = new CompositeStyle();
            compositeStyle.Name = editingStyle.Name;

            if (fetchInnerStyles(editingStyle).Count > 0)
            {
                foreach (var style in fetchInnerStyles(editingStyle))
                {
                    compositeStyle.Styles.Add(style);
                }
            }
            else
            {
                compositeStyle.Styles.Add(editingStyle);
            }

            styleArguments.StyleToEdit = compositeStyle;
            var result = styleManager.EditStyle(styleArguments);
            if (result.Canceled) return null;
            else
            {
                T resultAreaStyle = new T();
                resultAreaStyle.Name = compositeStyle.Name;
                PointStyle pointStyle = resultAreaStyle as PointStyle;
                if (pointStyle != null)
                {
                    foreach (var tmpAreaStyle in compositeStyle.Styles.OfType<PointStyle>())
                    {
                        pointStyle.CustomPointStyles.Add(tmpAreaStyle);
                    }
                }
                else
                {
                    foreach (var tmpAreaStyle in compositeStyle.Styles.OfType<T>())
                    {
                        fetchInnerStyles(resultAreaStyle).Add(tmpAreaStyle);
                    }
                }
                return resultAreaStyle;
            }
        }

        public static bool CheckIsValid(this Style style)
        {
            AreaStyle areaStyle = style as AreaStyle;
            LineStyle lineStyle = style as LineStyle;
            PointStyle pointStyle = style as PointStyle;
            TextStyle textStyle = style as TextStyle;
            DotDensityStyle dotDensityStyle = style as DotDensityStyle;
            ClassBreakStyle classBreakStyle = style as ClassBreakStyle;
            RegexStyle regexStyle = style as RegexStyle;
            FilterStyle filterStyle = style as FilterStyle;
            CompositeStyle componentStyle = style as CompositeStyle;

            bool isStyleValid = style.IsActive && !string.IsNullOrEmpty(style.Name);

            if (areaStyle != null)
            {
                isStyleValid &= (!areaStyle.FillSolidBrush.Color.IsTransparent
                    || !areaStyle.OutlinePen.Color.IsTransparent
                    || areaStyle.Advanced.FillCustomBrush != null);
            }
            else if (lineStyle != null)
            {
                isStyleValid &= (!lineStyle.CenterPen.Color.IsTransparent
                    || !lineStyle.OuterPen.Color.IsTransparent
                    || !lineStyle.InnerPen.Color.IsTransparent);
            }
            else if (pointStyle != null)
            {
                switch (pointStyle.PointType)
                {
                    case PointType.Symbol:
                        isStyleValid &= (!pointStyle.SymbolPen.Color.IsTransparent
                            || pointStyle.Image != null
                            || !pointStyle.SymbolSolidBrush.Color.IsTransparent
                            || pointStyle.Advanced.CustomBrush != null);
                        break;

                    case PointType.Bitmap:
                        isStyleValid &= pointStyle.Image != null;
                        break;

                    case PointType.Character:
                        isStyleValid &= pointStyle.CharacterFont != null
                            && (!pointStyle.CharacterSolidBrush.Color.IsTransparent
                            || pointStyle.Advanced.CustomBrush != null);
                        break;
                    default:
                        break;
                }
            }
            else if (textStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(textStyle.TextColumnName)
                    && (!textStyle.HaloPen.Color.IsTransparent
                    || !textStyle.TextSolidBrush.Color.IsTransparent
                    || textStyle.Advanced.TextCustomBrush != null);
            }
            else if (dotDensityStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(dotDensityStyle.ColumnName)
                    && (dotDensityStyle.CustomPointStyle != null
                    && CheckIsValid(dotDensityStyle.CustomPointStyle)
                    && dotDensityStyle.PointToValueRatio != 0);
            }
            else if (classBreakStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(classBreakStyle.ColumnName)
                    && classBreakStyle.ClassBreaks.Count != 0;
            }
            else if (regexStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(regexStyle.ColumnName)
                    && regexStyle.RegexItems.Count != 0;
            }
            else if (filterStyle != null)
            {
                isStyleValid &= filterStyle.Conditions.Count > 0;
            }
            else if (componentStyle != null)
            {
                isStyleValid = true;
            }
            return isStyleValid;
        }

        public static void FillRequiredColumnNames(this StyleBuilderArguments arguments)
        {
            if (arguments.ColumnNames.Count == 0 && arguments.FeatureLayer != null)
            {
                arguments.FeatureLayer.SafeProcess(() =>
                {
                    arguments.ColumnNames.Clear();
                    foreach (var columnName in arguments.FeatureLayer.QueryTools.GetColumns().Select(column => column.ColumnName))
                    {
                        arguments.ColumnNames.Add(columnName);
                    }

                    if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(arguments.FeatureLayer.FeatureSource.Id)
                        && CalculatedDbfColumn.CalculatedColumns[arguments.FeatureLayer.FeatureSource.Id].Count > 0)
                    {
                        foreach (var item in CalculatedDbfColumn.CalculatedColumns[arguments.FeatureLayer.FeatureSource.Id])
                        {
                            arguments.ColumnNames.Add(item.ColumnName);
                        }
                    }
                });
            }
        }

        internal static BitmapSource GetPreviewSource(this StyleLayerListItem styleItem, int screenWidth, int screenHeight)
        {
            BitmapImage bitmapImage = new BitmapImage();
            var imageBuffer = styleItem.GetPreviewImage(screenWidth, screenHeight);
            if (imageBuffer != null)
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(imageBuffer);
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }
    }
}