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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class LegendHelper
    {
        private const string legendNamePattern = @"(?<=Legend )\d+";
        private const string legendItemNamePattern = @"(?<=Legend Item )\d+";

        public static string AddSpaceToLastUpperChar(string str)
        {
            string lastUpper = str.Last(c => Char.IsUpper(c)).ToString();
            return str.Replace(lastUpper, " " + lastUpper).Trim();
        }

        public static string GenerateLegendName()
        {
            var names = from legendManageredLayer in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<LegendManagerAdornmentLayer>()
                        from notifiedLayer in legendManageredLayer.LegendLayers
                        select notifiedLayer.Name;
            return GenerateOrderedNewName(names, legendNamePattern, "Legend");
        }

        public static string GenerateLegendItemName(GeoCollection<LegendItemViewModel> legendItems)
        {
            return GenerateOrderedNewName(legendItems.Select(li => li.Text), legendItemNamePattern, "Legend Item");
        }

        public static SizeF Measure(this LegendItem legendItem, GeoCanvas geoCanvas)
        {
            DrawingRectangleF rect = geoCanvas.MeasureText(legendItem.TextStyle.TextColumnName, legendItem.TextStyle.Font);

            float width = rect.Width;
            width += legendItem.LeftPadding;
            width += legendItem.ImageLeftPadding;
            width += legendItem.ImageWidth;
            width += legendItem.ImageRightPadding;
            width += legendItem.TextLeftPadding;
            width += legendItem.TextRightPadding;
            width += legendItem.RightPadding;

            float imageHeight = legendItem.ImageTopPadding + legendItem.ImageHeight + legendItem.ImageBottomPadding;
            float textHeight = legendItem.TextTopPadding + rect.Height + legendItem.TextBottomPadding;
            float height = imageHeight > textHeight ? imageHeight : textHeight;
            return new SizeF(width, height);
        }

        public static Style CreateDefaultStyle(SymbolStyleType providerType)
        {
            switch (providerType)
            {
                case SymbolStyleType.Point_Simple:
                    var point = new PointStyle();
                    point.CustomPointStyles.Add(PointStyles.City4);
                    return point;
                case SymbolStyleType.Point_Font:
                    return new FontPointStyle();
                case SymbolStyleType.Point_Custom:
                    return new SymbolPointStyle();
                case SymbolStyleType.Area_Area:
                    return new AreaStyle();
                case SymbolStyleType.Line_Line:
                    return new LineStyle();
                case SymbolStyleType.None:
                default:
                    return null;
            }
        }

        private static string GenerateOrderedNewName(IEnumerable<string> names, string pattern, string prefix)
        {
            int latestNumber = 0;
            int number = 0;
            foreach (string name in names)
            {
                string result = Regex.Match(name, pattern).Value;
                if (!String.IsNullOrEmpty(result) && int.TryParse(result, out number))
                {
                    latestNumber = latestNumber > number ? latestNumber : number;
                }
            }
            return String.Format(CultureInfo.InvariantCulture, "{0} {1}", prefix, latestNumber + 1).Trim();
        }

        private static StyleType CreateActualPredefinedStyle<StyleType>(string propertyName, Type styleSetType) where StyleType : Style, new()
        {
            var propertyInfo = styleSetType.GetProperties().FirstOrDefault(pInfo => pInfo.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (propertyInfo != null)
            {
                var style = (StyleType)propertyInfo.GetValue(null, null);
                if (style != null) style.Name = propertyName;
                return style;
            }
            else return null;
        }

        private static T CreateNormalStyle<T>(Style oldStyle,
            SymbolStyleType symbolStyleType = SymbolStyleType.None,
            Func<SymbolStyleType, T> creatDefaultCallback = null,
            Action<T> settingCallback = null) where T : Style, new()
        {
            if (oldStyle == null || oldStyle.GetType() != typeof(T))
            {
                T instance = new T();
                if (creatDefaultCallback != null)
                {
                    instance = creatDefaultCallback(symbolStyleType);
                }
                if (settingCallback != null) settingCallback(instance);
                return instance;
            }
            else return (T)oldStyle;
        }
    }
}
