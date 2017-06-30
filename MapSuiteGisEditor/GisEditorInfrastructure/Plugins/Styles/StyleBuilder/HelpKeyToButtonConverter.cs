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
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class HelpKeyToButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Binding.DoNothing;

            return GetHelpButton((Uri)value);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private FrameworkElement GetHelpButton(Uri helpUri)
        {
            FrameworkElement frameworkElement = null;
            frameworkElement = new Image { Source = new BitmapImage(new Uri("/Images/help.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            frameworkElement.MouseLeftButtonUp += NavigateToHelpUri_Click;
            frameworkElement.ToolTip = "Help";
            frameworkElement.Tag = helpUri;

            return frameworkElement;
        }

        private static void NavigateToHelpUri_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement image = sender as FrameworkElement;
            if (image != null)
            {
                if (image.Tag is Uri)
                {
                    Uri uri = (Uri)image.Tag;
                    if (!string.IsNullOrEmpty(uri.AbsoluteUri)) Process.Start(uri.AbsoluteUri);
                }
            }
        }
    }
}
