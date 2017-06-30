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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CustomSymbolPointStylePlugin : StylePlugin
    {
        public CustomSymbolPointStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("CustomPointStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("CustomSymbolPointStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_customsymbolpoint.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_customsymbolpoint.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Point;
            Index = StylePluginOrder.AdvancedStyle;
        }

        protected override Style GetDefaultStyleCore()
        {
            Uri defaultIconUri = new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/markerOverlay.png", UriKind.RelativeOrAbsolute);
            Stream defaultIconStream = System.Windows.Application.GetResourceStream(defaultIconUri).Stream;

            //Fix unmanaged memory stream issue when serialize.
            Image image = Image.FromStream(defaultIconStream);
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            SymbolPointStyle defaultIconStyle = new SymbolPointStyle();
            defaultIconStyle.Image = new GeoImage(ms);
            return defaultIconStyle;
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new CustomSymbolPointStyleItem(style);
        }
    }
}