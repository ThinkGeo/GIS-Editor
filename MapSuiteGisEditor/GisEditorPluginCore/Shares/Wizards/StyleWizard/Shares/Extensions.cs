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
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class Extensions
    {
        private const string resourcePrefix = "images/stylepreview/sample_";

        private static Dictionary<string, BitmapImage> allStylePluginPreviews;

        static Extensions()
        {
            allStylePluginPreviews = new Dictionary<string, BitmapImage>();
            allStylePluginPreviews.Add("AdvancedAreaStylePlugin_area", Extensions.FindImageSource(CombineResourceFullName("area_advanced")));
            allStylePluginPreviews.Add("FilterStylePlugin_area", Extensions.FindImageSource(CombineResourceFullName("area_filter")));
            allStylePluginPreviews.Add("DotDensityAreaStylePlugin_area", Extensions.FindImageSource(CombineResourceFullName("area_dotdensity")));
            allStylePluginPreviews.Add("ClassBreakStylePlugin_area", Extensions.FindImageSource(CombineResourceFullName("area_classbreak")));
            allStylePluginPreviews.Add("ValueStylePlugin_area", Extensions.FindImageSource(CombineResourceFullName("area_valuestyle")));

            allStylePluginPreviews.Add("AdvancedLineStylePlugin_line", Extensions.FindImageSource(CombineResourceFullName("line_advanced")));
            allStylePluginPreviews.Add("FilterStylePlugin_line", Extensions.FindImageSource(CombineResourceFullName("line_filter")));
            allStylePluginPreviews.Add("ClassBreakStylePlugin_line", Extensions.FindImageSource(CombineResourceFullName("line_classbreak")));
            allStylePluginPreviews.Add("ValueStylePlugin_line", Extensions.FindImageSource(CombineResourceFullName("line_valuestyle")));

            allStylePluginPreviews.Add("SimplePointStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_simple")));
            allStylePluginPreviews.Add("WellPointStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_simple")));
            allStylePluginPreviews.Add("CustomSymbolPointStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_customsymbol")));
            allStylePluginPreviews.Add("FilterStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_filter")));
            allStylePluginPreviews.Add("FontPointStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_font")));
            allStylePluginPreviews.Add("ClassBreakStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_classbreak")));
            allStylePluginPreviews.Add("ValueStylePlugin_point", Extensions.FindImageSource(CombineResourceFullName("point_valuestyle")));

            allStylePluginPreviews.Add("IconTextStylePlugin_text", Extensions.FindImageSource(CombineResourceFullName("text_simple")));
            allStylePluginPreviews.Add("TextFilterStylePlugin_text", Extensions.FindImageSource(CombineResourceFullName("text_filter")));
        }

        private static string CombineResourceFullName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}.png", resourcePrefix, name);
        }

        public static Dictionary<string, BitmapImage> AllStylePluginPreviews
        {
            get { return allStylePluginPreviews; }
        }

        internal static BitmapImage FindImageSource(string resourceName)
        {
            BitmapImage previewSource = new BitmapImage();
            previewSource.BeginInit();
            previewSource.StreamSource = Application.GetResourceStream(
                new Uri(String.Format(CultureInfo.InvariantCulture,
                "/GisEditorPluginCore;component/{0}", resourceName), UriKind.RelativeOrAbsolute)).Stream;
            previewSource.EndInit();
            previewSource.Freeze();

            return previewSource;
        }
    }
}
