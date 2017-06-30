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
using System.Collections.ObjectModel;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class PointStylesModel : Collection<StyleCheckableItemModel>
    {
        public PointStylesModel()
        {
            foreach (var stylePlugin in GisEditor.StyleManager.GetStylePlugins(StyleCategories.Point | StyleCategories.Composite))
            {
                var item = new StyleCheckableItemModel();
                item.Name = stylePlugin.Name;
                item.ProviderName = stylePlugin.GetType().Name;
                item.Description = stylePlugin.Description;
                item.Preview = Extensions.AllStylePluginPreviews[item.ProviderName + "_point"];
                Add(item);
            }

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "Simple Style",
            //    ProviderName = "SimplePointStylePlugin",
            //    Description = Properties.Resources.PointSimpleStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("simple"))
            //});

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "Custom Symbol Style",
            //    ProviderName = "CustomSymbolPointStylePlugin",
            //    Description = Properties.Resources.PointCustomSymbolStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("customsymbol"))
            //});

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "Filter Style",
            //    ProviderName = "FilterStylePlugin",
            //    Description = Properties.Resources.PointFilterStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("filter"))
            //});

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "Font Style",
            //    ProviderName = "FontPointStylePlugin",
            //    Description = Properties.Resources.PointFontStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("font"))
            //});

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "ClassBreak Style",
            //    ProviderName = "ClassBreakStylePlugin",
            //    Description = Properties.Resources.PointClassBreakStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("classbreak"))
            //});

            //Add(new StyleCheckableItemModel
            //{
            //    Name = "Value Style",
            //    ProviderName = "ValueStylePlugin",
            //    Description = Properties.Resources.PointValueStyle,
            //    Preview = Extensions.FindImageSource(CombineResourceFullName("valuestyle"))
            //});
        }
    }
}
