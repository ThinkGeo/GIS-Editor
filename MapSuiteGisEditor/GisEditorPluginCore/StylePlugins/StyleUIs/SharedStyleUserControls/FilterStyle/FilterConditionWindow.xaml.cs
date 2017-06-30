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


using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class FilterConditionWindow : Window
    {
        private FilterConditionViewModel conditionViewModel;

        public FilterConditionWindow()
            : this(null)
        { }

        public FilterConditionWindow(FilterConditionViewModel condition)
        {
            InitializeComponent();
            conditionViewModel = condition;
            DataContext = condition;
            if (condition == null || condition.FilterMode == FilterMode.Attributes)
            {
                AttributesRadioButton.IsChecked = true;
            }
            else
            {
                AreaRadioButton.IsChecked = true;
            }
        }

        public FilterConditionViewModel ViewModel
        {
            get { return (FilterConditionViewModel)DataContext; }
            set
            {
                DataContext = value;
            }
        }

        [Obfuscation]
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void AndRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Logical = true;
            }
        }

        [Obfuscation]
        private void OrRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Logical = false;
            }
        }

        [Obfuscation]
        private void AttributesRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FilterMode = FilterMode.Attributes;
            }
        }

        [Obfuscation]
        private void AreaRadioButtonClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FilterMode = FilterMode.Area;
            }
        }
    }
}