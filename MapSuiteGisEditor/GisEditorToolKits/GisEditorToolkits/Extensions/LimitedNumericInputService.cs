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
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor
{
    public static class LimitedNumericInputService
    {
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(LimitedNumericInputService), new PropertyMetadata(false, new PropertyChangedCallback(OnIsEnabledPropertyChanged)));

        private static void OnIsEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = d as TextBox;
            if (textBox != null && (bool)e.NewValue)
            {
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            }
            else
            {
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = !IsValidDecimalKey(e.Key, true);
        }

        public static bool IsValidDecimalKey(Key inputKey, bool allowDouble)
        {
            if (inputKey == Key.Delete) return true;
            if (inputKey == Key.Back) return true;
            if (inputKey == Key.OemMinus) return true;
            if (inputKey == Key.Subtract) return true;
            if (inputKey == Key.Left) return true;
            if (inputKey == Key.Right) return true;
            if (inputKey == Key.Tab) return true;
            if (inputKey == Key.Decimal) return allowDouble;
            if (inputKey == Key.OemPeriod) return allowDouble;
            if (inputKey < Key.D0 || inputKey > Key.D9)
            {
                if (inputKey < Key.NumPad0 || inputKey > Key.NumPad9)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
