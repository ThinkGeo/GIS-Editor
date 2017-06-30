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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DistanceUnitToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DistanceUnit)
            {
                DistanceUnit unit = (DistanceUnit)value;
                switch (unit)
                {
                    case DistanceUnit.Meter:
                        return "Meters";
                    case DistanceUnit.Kilometer:
                        return "Kilometers";
                    case DistanceUnit.Mile:
                        return "Miles";
                    case DistanceUnit.UsSurveyFeet:
                        return "US Survey Feet";
                    case DistanceUnit.Yard:
                        return "Yards";
                    case DistanceUnit.NauticalMile:
                        return "Nautical Miles";
                    case DistanceUnit.Feet:
                    default:
                        return value.ToString();
                }
            }
            else if (value is String)
            {
                string unitInString = value.ToString();
                DistanceUnit result = DistanceUnit.Feet;

                switch (unitInString)
                {
                    case "Meters":
                        result = DistanceUnit.Meter;
                        break;
                    case "Feet":
                        result = DistanceUnit.Feet;
                        break;
                    case "Kilometers":
                        result = DistanceUnit.Kilometer;
                        break;
                    case "Miles":
                        result = DistanceUnit.Mile;
                        break;
                    case "US Survey Feet":
                        result = DistanceUnit.UsSurveyFeet;
                        break;
                    case "Yards":
                        result = DistanceUnit.Yard;
                        break;
                    case "Nautical Miles":
                        result = DistanceUnit.NauticalMile;
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