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

using ThinkGeo.MapSuite.WpfDesktop.Extension;
using ThinkGeo.Core;

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
            CompositeStyle compositeStyle = new CompositeStyle { Name = textStyle.Name };
            compositeStyle.Styles.Add(textStyle);

            styleArguments.StyleToEdit = compositeStyle;
            var result = styleManager.EditStyle(styleArguments);
            if (result.Canceled) return null;

            var edited = compositeStyle.Styles.OfType<IconTextStyle>().FirstOrDefault();
            if (edited == null)
            {
                edited = new IconTextStyle();
            }

            edited.Name = compositeStyle.Name;
            return edited;
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
                var fillBrush = areaStyle.FillBrush;
                var solidBrush = fillBrush as GeoSolidBrush;
                bool hasFill = fillBrush != null && !(solidBrush != null && solidBrush.Color.IsTransparent);
                bool hasOutline = areaStyle.OutlinePen != null && !areaStyle.OutlinePen.Color.IsTransparent;

                isStyleValid &= (hasFill || hasOutline);
            }
            else if (lineStyle != null)
            {
                isStyleValid &= (!lineStyle.CenterPen.Color.IsTransparent
                    || !lineStyle.OuterPen.Color.IsTransparent
                    || !lineStyle.InnerPen.Color.IsTransparent);
            }
            else if (pointStyle != null)
            {
                var fillBrush = pointStyle.FillBrush;
                var solidBrush = fillBrush as GeoSolidBrush;
                bool hasFill = fillBrush != null && !(solidBrush != null && solidBrush.Color.IsTransparent);
                bool hasOutline = pointStyle.OutlinePen != null && !pointStyle.OutlinePen.Color.IsTransparent;

                switch (pointStyle.PointType)
                {
                    case PointType.Symbol:
                        isStyleValid &= (hasOutline
                            || pointStyle.Image != null
                            || hasFill);
                        break;

                    case PointType.Image:
                        isStyleValid &= pointStyle.Image != null;
                        break;

                    case PointType.Glyph:
                        isStyleValid &= pointStyle.GlyphFont != null
                            && !string.IsNullOrEmpty(pointStyle.GlyphContent)
                            && hasFill;
                        break;
                    default:
                        break;
                }
            }
            else if (textStyle != null)
            {
                var textBrush = textStyle.TextBrush;
                var solidBrush = textBrush as GeoSolidBrush;
                bool hasText = textBrush != null && !(solidBrush != null && solidBrush.Color.IsTransparent);
                bool hasHalo = textStyle.HaloPen != null && !textStyle.HaloPen.Color.IsTransparent;

                isStyleValid &= !string.IsNullOrEmpty(textStyle.TextColumnName)
                    && (hasHalo || hasText);
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
