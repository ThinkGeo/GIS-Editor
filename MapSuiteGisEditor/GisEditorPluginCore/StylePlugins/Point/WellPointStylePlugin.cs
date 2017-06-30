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
using System.Windows.Media.Imaging;
using System.Reflection;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class WellPointStylePlugin : StylePlugin
    {
        [Obfuscation]
        private StyleSetting styleOption;

        [NonSerialized]
        private StyleSettingUserControl optionUI;

        public WellPointStylePlugin()
            : base()
        {
            Name = "Well Point Style";
            Description = "The Well Point Style provides robust control over the symbol that you want to use for your point features.  You can select a symbol type and size, fill and outline colors, outline thickness and more.";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_simplepoint.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_simplepoint.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Point;
            Index = StylePluginOrder.WellPointStyle;

            WellPointStyle pointStyle = new WellPointStyle
            {
                Name = "Well Point Style",
                WellPointIndex = 1,
                SymbolSize = 8,
                SymbolSolidBrush = new GeoSolidBrush(GeoColor.FromHtml("#FF4500")),
                SymbolPen = new GeoPen(GeoColor.StandardColors.Black, 1),
            };
            StyleCandidates.Add(pointStyle);
            styleOption = new StyleSetting(this);
        }

        protected override Style GetDefaultStyleCore()
        {
            int alpha = 255;
            WellPointStyle style = StyleCandidates.OfType<WellPointStyle>().FirstOrDefault();
            if (style != null)
            {
                alpha = style.SymbolSolidBrush.Color.AlphaComponent;
            }
            var fillColor = new GeoColor(alpha, GeoColorHelper.GetRandomColor());
            var outlineColor = new GeoColor(alpha, GeoColor.StandardColors.Black);
            return new WellPointStyle(1, new GeoSolidBrush(fillColor), new GeoPen(outlineColor), 8);
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new StyleSettingUserControl("AnnotationStylesRibbonGroupWellPointStyleLabel", "DefaultStyleSettingTitle", "DefaultStyleSettingDescription");
            }
            optionUI.DataContext = new StyleSettingViewModel(styleOption);
            return optionUI;
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new WellPointStyleItem(style);
        }
    }
}
