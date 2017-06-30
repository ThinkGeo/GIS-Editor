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


using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for LoadCustomSetWindow.xaml
    /// </summary>
    public partial class LoadCustomSetWindow : Window
    {
        private LoadCustomSetViewModel viewModel;

        public LoadCustomSetWindow()
        {
            InitializeComponent();
            viewModel = new LoadCustomSetViewModel();
            DataContext = viewModel;
        }

        public List<double> SelectedScales
        {
            get { return viewModel.SelectedScales; }
        }

        [Obfuscation]
        private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.SelectedSetName))
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LoadCustomSeWindowSelectNameText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            else DialogResult = true;
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        [Obfuscation]
        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(viewModel.SelectedSetName) && System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LoadCustomSeWindowAreYouSureText"), GisEditor.LanguageManager.GetStringResource("LoadCustomSetWindowDeleteContent"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                viewModel.AllCustomSetNames.Remove(viewModel.SelectedSetName);
            }
        }
    }
}
