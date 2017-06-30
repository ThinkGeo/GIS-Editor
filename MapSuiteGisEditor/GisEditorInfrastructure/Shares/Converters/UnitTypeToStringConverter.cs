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
using System.Reflection;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class UnitTypeToStringConverter : IValueConverter
    {
        private static string noSelect = GisEditor.LanguageManager.GetStringResource("ProjectionConfigurationCommonProjectionsPleaseSelect");

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is UnitType)
            {
                return Convert((UnitType)value);
            }
            else return ConvertBack(value.ToString());
        }

        public static string Convert(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Feet:
                case UnitType.Meters:
                    return unitType.ToString();
                case UnitType.DecimalDegree:
                    return "Decimal Degree";
                case UnitType.None:
                default:
                    return noSelect;
            }
        }

        public static UnitType ConvertBack(string unitTypeName)
        {
            switch (unitTypeName)
            {
                case "Meters":
                case "Feet":
                    return (UnitType)Enum.Parse(typeof(UnitType), unitTypeName);
                case "Decimal Degree":
                    return UnitType.DecimalDegree;
                default:
                    return UnitType.None;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}