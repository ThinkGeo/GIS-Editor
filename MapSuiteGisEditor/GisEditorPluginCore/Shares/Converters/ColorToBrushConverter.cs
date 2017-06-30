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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Color to Brush value converter
    /// </summary>
    [ValueConversion(typeof(Color), typeof(SolidColorBrush))]
    [Serializable]
    public class ColorToBrushConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            if (typeof(Brush).IsAssignableFrom(targetType))
            {
                if (value is Color)
                {
                    if (value.Equals(Colors.Transparent))
                    {
                        return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/checkboard_swatch.png", UriKind.RelativeOrAbsolute)));
                    }
                    else
                    {
                        return new SolidColorBrush((Color)value);
                    }
                }
            }
            if (targetType == typeof(Color))
            {
                if (value is SolidColorBrush)
                    return ((SolidColorBrush)value).Color;
            }
            return Binding.DoNothing;
        }
    }
}