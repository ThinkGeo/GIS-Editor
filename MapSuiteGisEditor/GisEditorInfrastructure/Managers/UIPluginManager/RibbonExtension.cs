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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class RibbonExtension : DependencyObject
    {
        public static readonly DependencyProperty IsAppliedUnderscoreProperty =
            DependencyProperty.RegisterAttached("IsAppliedUnderscore", typeof(bool), typeof(RibbonExtension), new UIPropertyMetadata(default(bool), OnIsAppliedUnderscoreChanged));

        public static readonly DependencyProperty RibbonTabHeaderProperty =
            DependencyProperty.Register("RibbonTabHeader", typeof(string), typeof(RibbonExtension));

        public static readonly DependencyProperty RibbonTabIndexProperty =
            DependencyProperty.Register("RibbonTabIndex", typeof(double), typeof(RibbonExtension));

        public static string GetRibbonTabHeader(DependencyObject obj)
        {
            return obj.GetValue(RibbonTabHeaderProperty) as string;
        }

        public static void SetRibbonTabHeader(DependencyObject obj, string value)
        {
            obj.SetValue(RibbonTabHeaderProperty, value);
        }

        public static double GetRibbonTabIndex(DependencyObject obj)
        {
            return (double)obj.GetValue(RibbonTabIndexProperty);
        }

        public static void SetRibbonTabIndex(DependencyObject obj, double value)
        {
            obj.SetValue(RibbonTabIndexProperty, value);
        }

        public static bool GetIsAppliedUnderscore(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAppliedUnderscoreProperty);
        }

        public static void SetIsAppliedUnderscore(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAppliedUnderscoreProperty, value);
        }

        public static void OnIsAppliedUnderscoreChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var isAppliedUnderscore = (bool)e.NewValue;
            if (isAppliedUnderscore)
            {
                if (s is RibbonApplicationMenuItem)
                {
                    var item = (RibbonApplicationMenuItem)s;
                    string totalString = item.Header == null ? string.Empty : item.Header.ToString();
                    if (totalString.Contains('_'))
                    {
                        item.Header = ConstructStackPanel(totalString);
                    }
                }
            }
        }

        private static StackPanel ConstructStackPanel(string totalString)
        {
            int index = totalString.IndexOf('_');
            string prefixString = (index == 0) ? String.Empty : totalString.Substring(0, index);
            string underscoreString = totalString[index + 1].ToString();
            string suffixString = totalString.Substring(index + 2, totalString.Length - index - 2);

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            TextBlock prefixText = new TextBlock();
            prefixText.Text = prefixString;
            panel.Children.Add(prefixText);

            TextBlock underscoreText = new TextBlock();
            underscoreText.TextDecorations = TextDecorations.Underline;
            underscoreText.Text = underscoreString;
            panel.Children.Add(underscoreText);

            TextBlock suffixText = new TextBlock();
            suffixText.Text = suffixString;
            panel.Children.Add(suffixText);

            return panel;
        }
    }
}