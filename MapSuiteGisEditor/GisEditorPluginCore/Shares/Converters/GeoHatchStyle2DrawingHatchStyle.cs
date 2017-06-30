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
using System.Drawing.Drawing2D;
using System.Windows.Data;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GeoHatchStyle2DrawingHatchStyle : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is GeoHatchStyle)
            {
                return Convert((GeoHatchStyle)value);
            }
            else if (value is HatchStyle)
            {
                return ConvertBack((HatchStyle)value);
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public static HatchStyle Convert(GeoHatchStyle geoHatchStyle)
        {
            HatchStyle drawingHatchStyle = HatchStyle.BackwardDiagonal;
            Enum.TryParse<HatchStyle>(geoHatchStyle.ToString(), out drawingHatchStyle);
            return drawingHatchStyle;
        }

        public static GeoHatchStyle ConvertBack(HatchStyle drawingHatchStyle)
        {
            GeoHatchStyle geoHatchStyle = GeoHatchStyle.BackwardDiagonal;
            Enum.TryParse<GeoHatchStyle>(drawingHatchStyle.ToString(), out geoHatchStyle);
            return geoHatchStyle;
        }
    }
}