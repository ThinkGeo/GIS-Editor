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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ScaleLineUnitSystemToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ScaleLineUnitSystem)
            {
                switch ((ScaleLineUnitSystem)value)
                {
                    case ScaleLineUnitSystem.MetricAndNauticalMile:
                        return "Metric And Nautical Mile";
                    case ScaleLineUnitSystem.NauticalMileAndImperial:
                        return "Nautical Mile And Imperial";
                    case ScaleLineUnitSystem.ImperialAndMetric:
                        return "Imperial And Metric";
                    case ScaleLineUnitSystem.Default:
                    default:
                        return "Default";
                }
            }
            else if (value is UnitSystem)
            {
                switch ((UnitSystem)value)
                {

                    case UnitSystem.Metric:
                        return "Metric";
                    case UnitSystem.NauticalMile:
                        return "NauticalMile";
                    case UnitSystem.Imperial:
                    default:
                        return "Imperial";
                }
            }
            else return Binding.DoNothing;
        }
    }
}
