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
using System.Drawing;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Color to Brush value converter
    /// </summary>
    [ValueConversion(typeof(Color), typeof(System.Windows.Media.SolidColorBrush))]
    [Serializable]
    public class DrawingColorToBrushConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            if (typeof(System.Windows.Media.Brush).IsAssignableFrom(targetType))
            {
                if (value is Color)
                {
                    Color drawingColor = (Color)value;
                    return Convert(drawingColor);
                }
            }
            if (targetType == typeof(Color))
            {
                if (value is System.Windows.Media.SolidColorBrush)
                {
                    System.Windows.Media.SolidColorBrush mediaBrush = (System.Windows.Media.SolidColorBrush)value;
                    return ConvertBack(mediaBrush);
                }
            }
            return Binding.DoNothing;
        }

        public static System.Windows.Media.SolidColorBrush Convert(Color drawingColor)
        {
            System.Windows.Media.Color mediaColor = System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            return new System.Windows.Media.SolidColorBrush(mediaColor);
        }

        public static Color ConvertBack(System.Windows.Media.SolidColorBrush mediaBrush)
        {
            return DrawingColorToMediaColorConverter.ConvertBack(mediaBrush.Color);
        }
    }
}