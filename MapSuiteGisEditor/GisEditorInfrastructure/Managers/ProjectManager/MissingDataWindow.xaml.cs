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


using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for MissingDataWindow.xaml
    /// </summary>
    internal partial class MissingDataWindow : Window
    {
        public MissingDataWindow()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var datas = this.DataContext as Collection<string>;

            Collection<MissingData> missingDatas = new Collection<MissingData>();

            foreach (var data in datas)
            {
                missingDatas.Add(new MissingData() { FileName = Path.GetFileName(data), FilePath = data });
            }

            ColumnList.ItemsSource = missingDatas;
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void ShowMissingClick(object sender, RoutedEventArgs e)
        {
            if (ColumnGrid.Visibility == Visibility.Visible)
            {
                ColumnGrid.Visibility = Visibility.Collapsed;
                ((Button)sender).Template = this.FindResource("ShowMissingTemplte") as ControlTemplate;
            }
            else
            {
                ColumnGrid.Visibility = Visibility.Visible;
                ((Button)sender).Template = this.FindResource("HideMissingTemplte") as ControlTemplate;
            }
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 174, 229));
            ((Border)sender).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 226, 242));
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = null;
            ((Border)sender).Background = null;
        }
    }

    [Obfuscation]
    internal class MissingData
    {
        [Obfuscation]
        private string fileName;
        [Obfuscation]
        private string filePath;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }
    }
}
