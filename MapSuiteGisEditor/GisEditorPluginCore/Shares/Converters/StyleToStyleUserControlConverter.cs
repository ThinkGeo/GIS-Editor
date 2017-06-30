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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class StyleToStyleUserControlConverter : ValueConverter
    {
        private Dictionary<object, StyleUserControl> styleUIs = new Dictionary<object, StyleUserControl>();

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (styleUIs.ContainsKey(value))
            {
                return styleUIs[value];
            }
            else
            {
                StyleUserControl styleUserControl = null;
                if (value is AreaStyle)
                {
                    styleUserControl = new SimpleAreaStyleUserControl((AreaStyle)value) { Width = double.NaN, Height = double.NaN };
                }
                else if (value is LineStyle)
                {
                    styleUserControl = new SimpleLineStyleUserControl((LineStyle)value) { Width = double.NaN, Height = double.NaN };
                }
                else if (value is PointStyle)
                {
                    styleUserControl = new SimplePointStyleUserControl((PointStyle)value) { Width = double.NaN, Height = double.NaN };
                }
                else if (value is WellPointStyle)
                {
                    styleUserControl = new WellPointStyleUserControl((WellPointStyle)value) { Width = double.NaN, Height = double.NaN };
                }
                styleUIs[value] = styleUserControl;
                return styleUserControl;
            }
        }
    }
}