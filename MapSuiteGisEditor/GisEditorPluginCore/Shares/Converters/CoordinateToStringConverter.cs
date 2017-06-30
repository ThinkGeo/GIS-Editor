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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CoordinateToStringConverter : ValueConverter
    {
        public const string degreesMinutesText = "Degrees Minutes";
        public const string degreesMinutesSecondsText = "Degrees Minutes Seconds";
        public const string XYText = "X/Y";
        public const string decimalDegreesText = "Decimal Degrees";

        public override object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CoordinateType)
            {
                switch ((CoordinateType)value)
                {
                    case CoordinateType.DegreesMinutes:
                        return degreesMinutesText;
                    case CoordinateType.DegreesMinutesSeconds:
                        return degreesMinutesSecondsText;
                    case CoordinateType.XY:
                        return XYText;
                    case CoordinateType.DecimalDegrees:
                    default:
                        return decimalDegreesText;
                }
            }
            else
            {
                switch (value.ToString())
                {
                    case degreesMinutesText:
                        return CoordinateType.DegreesMinutes;
                    case degreesMinutesSecondsText:
                        return CoordinateType.DegreesMinutesSeconds;
                    case XYText:
                        return CoordinateType.XY;
                    case decimalDegreesText:
                    default:
                        return CoordinateType.DecimalDegrees;
                }
            }
        }
    }
}