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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FontPointStylePlugin : StylePlugin
    {
        public FontPointStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("FontPointStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("FontPointStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_fontpoint.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_fontpoint.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Point;
            Index = StylePluginOrder.PredefinedStyle;
        }

        protected override Style GetDefaultStyleCore()
        {
            return new FontPointStyle
            {
                CharacterFont = new GeoFont("Arial", 9, DrawingFontStyles.Regular),
                CharacterIndex = FontPicker.FromCharactorIndex,
                CharacterSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Black)
            };
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<FontPointStyle>(style, arguments);
        //}

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new FontPointStyleItem(style);
        }
    }
}