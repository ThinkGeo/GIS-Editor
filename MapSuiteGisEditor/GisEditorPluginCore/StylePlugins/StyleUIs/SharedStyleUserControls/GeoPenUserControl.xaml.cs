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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class GeoPenUserControl : StyleUserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public GeoPenUserControl()
        {
            InitializeComponent();

            string[] patternType = new string[2] { "Basic", "Custom" };

            PatternType.ItemsSource = patternType;
            DashStyleComboBox.ItemsSource = Enum.GetNames(typeof(LineDashStyle)).Where(enumName => enumName != LineDashStyle.Custom.ToString());
            DashCapComboBox.ItemsSource = Enum.GetNames(typeof(GeoDashCap));
            var drawingLineCapItems = Enum.GetNames(typeof(DrawingLineCap)).Where(d => d != DrawingLineCap.AnchorMask.ToString() && d != DrawingLineCap.Custom.ToString() && d != DrawingLineCap.NoAnchor.ToString());
            StartCapComboBox.ItemsSource = drawingLineCapItems;
            EndCapComboBox.ItemsSource = drawingLineCapItems;
            LineJoinComboBox.ItemsSource = Enum.GetNames(typeof(DrawingLineJoin));

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var parentWindow = Window.GetWindow(this) as StyleBuilderWindow;
                if (parentWindow != null)
                {
                    parentWindow.Closing += (sender, e) =>
                    {
                        UpdateSourceManually();
                    };
                }
                //throw new NotImplementedException();
            }));
        }


        [Obfuscation]
        private void DashStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSourceManually();
        }

        private void UpdateSourceManually()
        {
            if (DashStyleComboBox.Visibility == Visibility.Visible)
            {
                BindingExpression expression = DashStyleComboBox.GetBindingExpression(ComboBox.SelectedValueProperty);
                expression.UpdateSource();
            }
            else
            {
                BindingExpression expression = DashPatternTextBox.GetBindingExpression(TextBox.TextProperty);
                expression.UpdateSource();
            }
        }
    }
}