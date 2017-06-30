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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextBoxExtension : DependencyObject
    {
        public static readonly DependencyProperty AutoFocusProperty =
            DependencyProperty.RegisterAttached("AutoFocus", typeof(bool), typeof(FrameworkElement)
            , new UIPropertyMetadata(false, OnAutoFocusPropertyChanged));

        public bool AutoFocus
        {
            get { return (bool)GetValue(AutoFocusProperty); }
            set { SetValue(AutoFocusProperty, value); }
        }

        public static void SetAutoFocus(DependencyObject sender, bool autoFocus)
        {
            sender.SetValue(AutoFocusProperty, autoFocus);
        }

        public static bool GetAutoFocus(DependencyObject sender)
        {
            return (bool)(sender.GetValue(AutoFocusProperty));
        }

        private static void OnAutoFocusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement textbox = d as FrameworkElement;
            if (d != null && (bool)e.NewValue)
            {
                textbox.Loaded += new RoutedEventHandler(Textbox_Loaded);
            }
            else if (d != null)
            {
                textbox.Loaded -= new RoutedEventHandler(Textbox_Loaded);
            }
        }

        private static void Textbox_Loaded(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).Focus();
        }
    }
}