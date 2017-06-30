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
using System.Globalization;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ComponentInnerStyleTypeToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComponentInnerStyleType)
            {
                ComponentInnerStyleType styleType = (ComponentInnerStyleType)value;
                return Convert(styleType);
            }
            else return Binding.DoNothing;
        }

        public static string Convert(ComponentInnerStyleType styleType)
        {
            switch (styleType)
            {
                case ComponentInnerStyleType.SimplePointStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimplePointStyleName");
                case ComponentInnerStyleType.CustomPointStyle:
                    return GisEditor.LanguageManager.GetStringResource("CustomPointStyleName");
                case ComponentInnerStyleType.FontPointStyle:
                    return GisEditor.LanguageManager.GetStringResource("FontPointStyleName");
                case ComponentInnerStyleType.SimpleAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimpleAreaStyleName");
                case ComponentInnerStyleType.AdvancedAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("AdvancedAreaStyleName");
                case ComponentInnerStyleType.SimpleLineStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimpleLineStyleName");
                case ComponentInnerStyleType.AdvancedLineStyle:
                    return GisEditor.LanguageManager.GetStringResource("AdvancedLineStyleName");
                case ComponentInnerStyleType.IconTextStyle:
                    return GisEditor.LanguageManager.GetStringResource("IconTextStyleName");
                case ComponentInnerStyleType.FilterTextStyle:
                    return GisEditor.LanguageManager.GetStringResource("FilterTextStyleName");
                default:
                    return styleType.ToString();
            }
        }
    }
}