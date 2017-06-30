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
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for FindWindow.xaml
    /// </summary>
    public partial class FindWindow : Window
    {
        private FindViewModel viewModel;

        public FindWindow(IEnumerable<LoggerMessageViewModel> messages, Action<LoggerMessageViewModel> action)
        {
            InitializeComponent();
            viewModel = new FindViewModel(messages, action);
            DataContext = viewModel;
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [Obfuscation]
        private void UpClick(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedDirection = FindLoggerDirection.Up;
        }

        [Obfuscation]
        private void DownClick(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedDirection = FindLoggerDirection.Down;
        }
    }
}
