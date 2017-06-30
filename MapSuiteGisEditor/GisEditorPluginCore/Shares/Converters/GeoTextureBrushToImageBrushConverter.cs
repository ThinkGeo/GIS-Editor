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
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GeoTextureBrushToImageBrushConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is GeoTextureBrush)
            {
                return Convert((GeoTextureBrush)value);
            }
            else if (value is ImageBrush)
            {
                return ConvertBack((ImageBrush)value);
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public static ImageBrush Convert(GeoTextureBrush geoTextureBrush)
        {
            string fileName = geoTextureBrush.GeoImage.GetPathFilename();

            ImageBrush imageBrush = new ImageBrush();
            if (File.Exists(fileName))
            {
                imageBrush.ImageSource = new BitmapImage(new Uri("file://" + fileName));
                imageBrush.SetValue(Canvas.TagProperty, fileName);
                return imageBrush;
            }
            else
            {
                return imageBrush;
            }
        }

        public static GeoTextureBrush ConvertBack(ImageBrush imageBrush)
        {
            return new GeoTextureBrush(new GeoImage(((BitmapImage)imageBrush.ImageSource).UriSource.LocalPath));
        }
    }
}