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
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class RibbonGalleryExtension : DependencyObject
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(RibbonGalleryExtension), new FrameworkPropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem
        {
            get
            {
                return GetValue(SelectedItemProperty);
            }
            set
            {
                SetValue(SelectedItemProperty, value);
            }
        }

        public static void SetSelectedItem(DependencyObject dp, object value)
        {
            dp.SetValue(SelectedItemProperty, value);
        }

        public static object GetSelectedItem(DependencyObject dp)
        {
            return dp.GetValue(SelectedItemProperty);
        }

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ribbonGallery = sender as RibbonGallery;
            if (ribbonGallery != null)
            {
                ribbonGallery.SelectionChanged -= new RoutedPropertyChangedEventHandler<object>(RibbonGallery_SelectionChanged);
                ribbonGallery.SelectionChanged += new RoutedPropertyChangedEventHandler<object>(RibbonGallery_SelectionChanged);
                if (ribbonGallery.SelectedItem != e.NewValue)
                {
                    ribbonGallery.SelectedItem = e.NewValue;
                }
            }
        }

        private static void RibbonGallery_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var ribbonGallery = sender as RibbonGallery;
            if (ribbonGallery != null)
            {
                SetSelectedItem(ribbonGallery, e.NewValue);
            }
        }
    }
}