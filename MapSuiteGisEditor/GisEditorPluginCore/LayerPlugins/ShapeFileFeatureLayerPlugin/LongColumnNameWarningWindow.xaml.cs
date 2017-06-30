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
    /// <summary>
    /// Interaction logic for LongColumnNameWarningWindow.xaml
    /// </summary>
    public partial class LongColumnNameWarningWindow : Window
    {
        private LongColumnTruncateMode longColumnNameMode;

        public LongColumnNameWarningWindow(string title, string content)
        {
            InitializeComponent();
            Title = title;
            txtContent.Text = content;
            Owner = Application.Current.MainWindow;
        }

        public LongColumnTruncateMode LongColumnNameMode
        {
            get { return longColumnNameMode; }
        }

        [Obfuscation]
        private void TrimAndSaveClick(object sender, RoutedEventArgs e)
        {
            longColumnNameMode = LongColumnTruncateMode.Truncate;
            DialogResult = true;
        }

        [Obfuscation]
        private void SaveClick(object sender, RoutedEventArgs e)
        {
            longColumnNameMode = LongColumnTruncateMode.None;
            DialogResult = true;
        }
    }
}
