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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DotDensityAreaStylePlugin : StylePlugin
    {
        public DotDensityAreaStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("DotDensityAreaStylePluginName");
            Description = GisEditor.LanguageManager.GetStringResource("DotDensityAreaStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_dotdensityarea.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_dotdensityarea.png", UriKind.RelativeOrAbsolute));
            RequireColumnNames = true;
            StyleCategories = StyleCategories.Area;
            Index = StylePluginOrder.DotDensityStyle;
        }

        protected override Style GetDefaultStyleCore()
        {
            PointStyle innerStyle = PointStyles.City1;
            innerStyle.SymbolPen.Color = GeoColor.StandardColors.Transparent;
            innerStyle.SymbolSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.Transparent);
            innerStyle.Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginPointHeader");
            return new DotDensityStyle { CustomPointStyle = innerStyle };
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style) => new DotDensityAreaStyleItem(style);
    }
}