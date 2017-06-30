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
using System.Windows.Data;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SnappingDistanceUnitToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SnappingDistanceUnit)
            {
                SnappingDistanceUnit unit = (SnappingDistanceUnit)value;
                switch (unit)
                {
                    case SnappingDistanceUnit.Meter:
                        return "Meters";
                    case SnappingDistanceUnit.Kilometer:
                        return "Kilometers";
                    case SnappingDistanceUnit.Mile:
                        return "Miles";
                    case SnappingDistanceUnit.UsSurveyFeet:
                        return "US Survey Feet";
                    case SnappingDistanceUnit.Yard:
                        return "Yards";
                    case SnappingDistanceUnit.NauticalMile:
                        return "Nautical Miles";
                    case SnappingDistanceUnit.Feet:
                    default:
                        return value.ToString();
                }
            }
            else if (value is String)
            {
                string unitInString = value.ToString();
                SnappingDistanceUnit result = SnappingDistanceUnit.Feet;

                switch (unitInString)
                {
                    case "Meters":
                        result = SnappingDistanceUnit.Meter;
                        break;

                    case "Feet":
                        result = SnappingDistanceUnit.Feet;
                        break;

                    case "Kilometers":
                        result = SnappingDistanceUnit.Kilometer;
                        break;

                    case "Miles":
                        result = SnappingDistanceUnit.Mile;
                        break;

                    case "US Survey Feet":
                        result = SnappingDistanceUnit.UsSurveyFeet;
                        break;

                    case "Yards":
                        result = SnappingDistanceUnit.Yard;
                        break;

                    case "Nautical Miles":
                        result = SnappingDistanceUnit.NauticalMile;
                        break;
                    default:
                        break;
                }

                return result;
            }
            else return Binding.DoNothing;
        }
    }
}