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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class CheckBoxImage : Image
    {
        private static readonly BitmapImage unCheckedImage;
        private static readonly BitmapImage checkedImage;

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(CheckBoxImage), new UIPropertyMetadata(false, new PropertyChangedCallback(OnIsCheckedChanged)));

        static CheckBoxImage()
        {
            checkedImage = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/checkbox_yes.png", UriKind.Relative));
            unCheckedImage = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/checkbox_no.png", UriKind.Relative));
        }

        public CheckBoxImage()
            : base()
        {
            Width = 14;
            Height = 14;
            Source = unCheckedImage;
            Margin = new Thickness(7, 0, 6, 0);
            MouseLeftButtonDown += (s, e) => { IsChecked = !IsChecked; e.Handled = true; };
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        private static void OnIsCheckedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            CheckBoxImage checkBox = sender as CheckBoxImage;
            if (checkBox != null)
            {
                checkBox.Source = checkBox.IsChecked ? checkedImage : unCheckedImage;
            }
        }
    }
}