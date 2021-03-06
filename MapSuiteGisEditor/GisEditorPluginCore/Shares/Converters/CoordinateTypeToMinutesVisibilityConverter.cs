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
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CoordinateTypeToMinutesVisibilityConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Hidden;
            CoordinateType type = (CoordinateType)value;

            switch (type)
            {
                case CoordinateType.DecimalDegrees:
                case CoordinateType.XY:
                    result = Visibility.Collapsed;
                    break;
                case CoordinateType.DegreesMinutes:
                    result = Visibility.Visible;
                    break;
                case CoordinateType.DegreesMinutesSeconds:
                    result = Visibility.Visible;
                    break;
            }

            return result;
        }
    }
}