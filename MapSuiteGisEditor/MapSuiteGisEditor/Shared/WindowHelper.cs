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


using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    public static class WindowHelper
    {
        public static readonly DependencyProperty WindowShowUpLocationProperty =
            DependencyProperty.RegisterAttached("WindowShowUpLocation",
            typeof(WindowStartupLocation), typeof(WindowHelper), new FrameworkPropertyMetadata(WindowStartupLocation.Manual, OnWindowShowUpLocationPropertyChanged));

        public static readonly DependencyProperty WindowOwnerProperty =
            DependencyProperty.RegisterAttached("WindowOwner",
            typeof(Window), typeof(WindowHelper), new FrameworkPropertyMetadata(null, OnWindowOwnerPropertyChanged));

        public static void SetWindowShowUpLocation(DependencyObject dp, WindowStartupLocation value)
        {
            dp.SetValue(WindowShowUpLocationProperty, value);
        }

        public static WindowStartupLocation GetWindowShowUpLocation(DependencyObject dp)
        {
            return (WindowStartupLocation)dp.GetValue(WindowShowUpLocationProperty);
        }

        public static void SetWindowOwner(DependencyObject dp, Window value)
        {
            dp.SetValue(WindowOwnerProperty, value);
        }

        public static Window GetWindowOwner(DependencyObject dp)
        {
            return (Window)dp.GetValue(WindowOwnerProperty);
        }

        public static void OnWindowOwnerPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Window window = sender as Window;
            if (window != null && window != e.NewValue)
            {
                window.Owner = (Window)e.NewValue;
            }
        }

        public static void OnWindowShowUpLocationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Window window = sender as Window;
            if (window != null)
            {
                window.WindowStartupLocation = (WindowStartupLocation)e.NewValue;
            }
        }
    }
}