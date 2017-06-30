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
    public class AdvancedAreaStylePlugin : StylePlugin
    {
        [Obfuscation]
        private StyleSetting areaStyleOption;

        [NonSerialized]
        private StyleSettingUserControl optionUI;

        public AdvancedAreaStylePlugin()
            : base()
        {
            IsDefault = true;
            IsRequired = true;
            Name = GisEditor.LanguageManager.GetStringResource("AdvancedAreaStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("AdvancedAreaStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_advancedarea.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_advancedarea.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Area;
            Index = StylePluginOrder.SimpleStyle;

            AreaStyle areaStyle = new AreaStyle
            {
                Name = Name,
                FillSolidBrush = new GeoSolidBrush(GeoColor.FromHtml("#C0C0C0")),
                OutlinePen = new GeoPen(GeoColor.FromHtml("#808080"), 1),
            };
            StyleCandidates.Add(areaStyle);
            areaStyleOption = new StyleSetting(this);
        }

        protected override Style GetDefaultStyleCore()
        {
            int alpha = 255;
            AreaStyle areaStyle = StyleCandidates.OfType<AreaStyle>().FirstOrDefault();
            if (areaStyle != null)
            {
                alpha = areaStyle.FillSolidBrush.Color.AlphaComponent;
            }
            var fillColor = new GeoColor(alpha, GeoColorHelper.GetRandomColor());
            var outlineColor = new GeoColor(alpha, GeoColor.SimpleColors.Black);
            return AreaStyles.CreateSimpleAreaStyle(fillColor, outlineColor);
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new StyleSettingUserControl("AnnotationStylesRibbonGroupShapeStyleLabel", "DefaultStyleSettingTitle", "DefaultStyleSettingDescription");
            }
            optionUI.DataContext = new StyleSettingViewModel(areaStyleOption);
            return optionUI;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            areaStyleOption.LoadState(settings.GlobalSettings);
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in areaStyleOption.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<AdvancedAreaStyle>(style, arguments);
        //}

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new AdvancedAreaStyleItem(style);
        }
    }
}