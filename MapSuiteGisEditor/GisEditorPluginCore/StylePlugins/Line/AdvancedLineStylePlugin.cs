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
    public class AdvancedLineStylePlugin : StylePlugin
    {
        [Obfuscation]
        private StyleSetting lineStyleOption;

        [NonSerialized]
        private StyleSettingUserControl optionUI;

        public AdvancedLineStylePlugin()
            : base()
        {
            IsDefault = true;
            IsRequired = true;
            Name = GisEditor.LanguageManager.GetStringResource("AdvancedLineStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("AdvancedLineStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_advancedline.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_advancedline.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Line;
            Index = StylePluginOrder.SimpleStyle;

            LineStyle lineStyle = new LineStyle
            {
                Name = GisEditor.LanguageManager.GetStringResource("AnnotationStylesRibbonGroupLineStyleLabel"),
                OuterPen = new GeoPen(GeoColor.SimpleColors.Black, 1),
                InnerPen = new GeoPen(GeoColor.StandardColors.Transparent, 1),
                CenterPen = new GeoPen(GeoColor.StandardColors.Transparent, 1),
            };
            StyleCandidates.Add(lineStyle);
            lineStyleOption = new StyleSetting(this);
        }

        protected override Style GetDefaultStyleCore()
        {
            int alpha = 255;
            LineStyle style = StyleCandidates.OfType<LineStyle>().FirstOrDefault();
            if (style != null)
            {
                alpha = style.OuterPen.Color.AlphaComponent;
            }
            GeoColor outerColor = new GeoColor(alpha, GeoColorHelper.GetRandomColor(RandomColorType.Bright));
            LineStyle lineStyle = new LineStyle(new GeoPen(outerColor));
            lineStyle.OuterPen.StartCap = DrawingLineCap.Round;
            lineStyle.OuterPen.EndCap = DrawingLineCap.Round;
            return lineStyle;
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new StyleSettingUserControl("AnnotationStylesRibbonGroupLineStyleLabel", "DefaultStyleSettingTitle", "DefaultStyleSettingDescription");
            }
            optionUI.DataContext = new StyleSettingViewModel(lineStyleOption);
            return optionUI;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in lineStyleOption.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings storableSettings)
        {
            base.ApplySettingsCore(storableSettings);
            lineStyleOption.LoadState(storableSettings.GlobalSettings);
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<AdvancedLineStyle>(style, arguments);
        //}

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new AdvancedLineStyleItem(style);
        }
    }
}