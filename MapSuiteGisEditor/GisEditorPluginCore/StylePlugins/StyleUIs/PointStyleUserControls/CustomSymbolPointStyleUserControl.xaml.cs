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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for CustomSymbolPointStyleUserControl.xaml
    /// </summary>
    public partial class CustomSymbolPointStyleUserControl : StyleUserControl
    {
        private PointStyleViewModel pointStyleViewModel;
        private int fixedSize = 70;

        public CustomSymbolPointStyleUserControl(PointStyle style)
        {
            InitializeComponent();
            pointStyleViewModel = new PointStyleViewModel(style);
            DataContext = pointStyleViewModel;

            var heigth = pointStyleViewModel.ActualImage.GetHeight();
            var width = pointStyleViewModel.ActualImage.GetWidth();

            previewImage.Height = heigth > fixedSize ? fixedSize : heigth;
            previewImage.Width = width > fixedSize ? fixedSize : width;

            string helpUri = GisEditor.LanguageManager.GetStringResource("CustomPointStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }
        }

        [Obfuscation]
        private void browseClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Multiselect = false, Filter = "Png Images | *.png|Jpg Images | *.jpg|Gif  Images | *.gif" };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                pointStyleViewModel.ImagePath = openFileDialog.FileName;

                var heigth = pointStyleViewModel.ActualImage.GetHeight();
                var width = pointStyleViewModel.ActualImage.GetWidth();

                previewImage.Height = heigth > fixedSize ? fixedSize : heigth;
                previewImage.Width = width > fixedSize ? fixedSize : width;
            }
        }

        [Obfuscation]
        private void selectIconChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var entity = e.AddedItems[0] as IconEntity;

                if (entity != null)
                {
                    var icon = entity.Icon;
                    previewImage.Height = icon.Width > fixedSize ? fixedSize : icon.Width;
                    previewImage.Width = icon.Height > fixedSize ? fixedSize : icon.Height;
                }
            }
        }

        [Obfuscation]
        private void Numeric_ValueChanged(object sender, RoutedEventArgs e)
        {
            var heigth = pointStyleViewModel.ActualImage.GetHeight();
            var width = pointStyleViewModel.ActualImage.GetWidth();

            previewImage.Height = heigth > fixedSize ? fixedSize : heigth;
            previewImage.Width = width > fixedSize ? fixedSize : width;
        }
    }
}