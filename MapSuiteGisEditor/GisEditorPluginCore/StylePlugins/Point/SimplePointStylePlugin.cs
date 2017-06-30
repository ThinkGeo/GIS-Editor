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
using System.Reflection;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SimplePointStylePlugin : StylePlugin
    {
        [Obfuscation]
        private StyleSetting pointStyleOption;

        [NonSerialized]
        private StyleSettingUserControl optionUI;

        public SimplePointStylePlugin()
            : base()
        {
            IsDefault = true;
            IsRequired = true;
            Name = GisEditor.LanguageManager.GetStringResource("SimplePointStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("SimplePointStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_simplepoint.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_simplepoint.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Point;
            Index = StylePluginOrder.SimpleStyle;

            PointStyle pointStyle = new PointStyle
            {
                    Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginPointHeader"),
                    SymbolType = PointSymbolType.Circle,
                    SymbolSize = 6,
                    SymbolSolidBrush = new GeoSolidBrush(GeoColor.FromHtml("#FF4500")),
                    SymbolPen = new GeoPen(GeoColor.StandardColors.Black, 1),
                };
            StyleCandidates.Add(pointStyle);
            pointStyleOption = new StyleSetting(this);
        }

        protected override Style GetDefaultStyleCore()
        {
            int alpha = 255;
            PointStyle style = StyleCandidates.OfType<PointStyle>().FirstOrDefault();
            if (style != null)
            {
                alpha = style.SymbolSolidBrush.Color.AlphaComponent;
            }
            var fillColor = new GeoColor(alpha, GeoColorHelper.GetRandomColor());
            var outlineColor = new GeoColor(alpha, GeoColor.StandardColors.Black);
            return PointStyles.CreateSimpleCircleStyle(fillColor, 8, outlineColor);
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new StyleSettingUserControl("AnnotationStylesRibbonGroupPointStyleLabel", "DefaultStyleSettingTitle", "DefaultStyleSettingDescription");
            }
            optionUI.DataContext = new StyleSettingViewModel(pointStyleOption);
            return optionUI;
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<PointStyle>(style, arguments);
        //}

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in pointStyleOption.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            pointStyleOption.LoadState(settings.GlobalSettings);
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new SimplePointStyleItem(style);
        }
    }
}