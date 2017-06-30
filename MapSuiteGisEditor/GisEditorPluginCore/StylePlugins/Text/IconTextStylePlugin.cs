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
    public class IconTextStylePlugin : StylePlugin
    {
        public IconTextStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("IconTextStyleName");
            Description = GisEditor.LanguageManager.GetStringResource("IconTextStylePluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_text.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_text.png", UriKind.RelativeOrAbsolute));

            IsDefault = true;
            RequireColumnNames = true;
            Index = StylePluginOrder.AdvancedStyle;
            StyleCategories = StyleCategories.Label;
        }

        protected override Style GetDefaultStyleCore()
        {
            var textStyle = new IconTextStyle()
            {
                Font = new GeoFont("Arial", 9),
                TextSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Black),
                XOffsetInPixel = 0,
                YOffsetInPixel = 0,
                RotationAngle = 0,
                OverlappingRule = LabelOverlappingRule.NoOverlapping,
                ForceHorizontalLabelForLine = false,
                GridSize = 100,
                SplineType = SplineType.Default,
                DuplicateRule = LabelDuplicateRule.UnlimitedDuplicateLabels,
                SuppressPartialLabels = false,
                LabelAllLineParts = false,
                LabelAllPolygonParts = true,
                TextLineSegmentRatio = 1.5,
                IconImageScale = 1,
                MaskMargin = 3,
                PolygonLabelingLocationMode = PolygonLabelingLocationMode.BoundingBoxCenter
            };

            textStyle.HaloPen.Width = 3;
            return textStyle;
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<IconTextStyle>(style, arguments);
        //}

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            return new IconTextStyleItem(style);
        }
    }
}