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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Collections.Generic;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AreaTextStyleUserControl.xaml
    /// </summary>
    public partial class TextStyleUserControl : StyleUserControl
    {
        private IconTextStyle iconTextStyle;
        private TextStyleViewModel viewModel;
        private int fixedSize = 70;

        public TextStyleUserControl(IconTextStyle style, StyleBuilderArguments requiredValues)
        {
            InitializeComponent();
            StyleBuilderArguments = requiredValues;
            viewModel = new TextStyleViewModel(style, StyleBuilderArguments);
            DataContext = viewModel;
            cm.DataContext = viewModel;
            iconTextStyle = style;
            if (requiredValues.FeatureLayer is InMemoryFeatureLayer)
            {
                ViewDataButton.IsEnabled = false;
            }

            var image = viewModel.ActualObject.GetPreviewImage();

            previewImage.Width = image.Width > fixedSize ? fixedSize : image.Width;
            previewImage.Height = image.Height > fixedSize ? fixedSize : image.Height;

            string helpUri = GisEditor.LanguageManager.GetStringResource("LabelStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }
        }

        protected override bool ValidateCore()
        {
            string errorMessage = GetErrorMessage();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                System.Windows.Forms.MessageBox.Show(errorMessage, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return false;
            }
            else return true;
        }

        private string GetErrorMessage()
        {
            StringBuilder errorMessage = new StringBuilder();

            if (string.IsNullOrEmpty(iconTextStyle.TextColumnName))
            {
                errorMessage.AppendLine("Text column name cannot be empty.");
            }

            return errorMessage.ToString();
        }

        [Obfuscation]
        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Multiselect = false, Filter = "Image Files(*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png;" };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                viewModel.IconFilePathName = openFileDialog.FileName;
                var image = viewModel.ActualObject.GetPreviewImage();

                previewImage.Width = image.Width > fixedSize ? fixedSize : image.Width;
                previewImage.Height = image.Height > fixedSize ? fixedSize : image.Height;
            }
        }

        [Obfuscation]
        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            var value = (KeyValuePair<string, string>)((MenuItem)sender).Header;

            if (!string.IsNullOrEmpty(value.Key))
            {
                viewModel.TextColumnName += "[" + value.Key + "]";
            }
        }

        [Obfuscation]
        private void ViewDataClick(object sender, RoutedEventArgs e)
        {
            DataViewerUserControl content = new DataViewerUserControl();
            content.ShowDialog();
        }

        [Obfuscation]
        private void RemoveIconClick(object sender, RoutedEventArgs e)
        {
            viewModel.IconFilePathName = string.Empty;
        }
    }
}