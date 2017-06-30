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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DistanceUnitToShortStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DistanceUnit)
            {
                DistanceUnit unit = (DistanceUnit)value;
                switch (unit)
                {
                    case DistanceUnit.Meter:
                        return "m";
                    case DistanceUnit.Feet:
                        return "ft.";
                    case DistanceUnit.Kilometer:
                        return "km";
                    case DistanceUnit.Mile:
                        return "mi.";
                    case DistanceUnit.UsSurveyFeet:
                        return "us-ft.";
                    case DistanceUnit.Yard:
                        return "yd.";
                    case DistanceUnit.NauticalMile:
                        return "nmi.";
                    case DistanceUnit.Inch:
                        return "in.";
                    case DistanceUnit.Link:
                        return "li.";
                    case DistanceUnit.Chain:
                        return "ch.";
                    case DistanceUnit.Pole:
                        return "pole";
                    case DistanceUnit.Rod:
                        return "rd.";
                    case DistanceUnit.Furlong:
                        return "fur.";
                    case DistanceUnit.Vara:
                        return "vara";
                    case DistanceUnit.Arpent:
                        return "arpent";
                    default:
                        return value.ToString();
                }
            }

            return value.ToString();
        }
    }
}