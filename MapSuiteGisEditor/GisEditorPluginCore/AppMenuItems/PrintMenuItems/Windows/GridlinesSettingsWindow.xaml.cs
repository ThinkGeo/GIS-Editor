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
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class GridlinesSettingsWindow : Window
    {
        private GridlinesSettingsViewModel viewModel;

        public GridlinesSettingsWindow(PrinterLayer printerLayer)
        {
            InitializeComponent();
            viewModel = new GridlinesSettingsViewModel(printerLayer);
            DataContext = viewModel;

            if (viewModel.UseCellSize)
            {
                gridColumns.IsEnabled = false;
                gridRows.IsEnabled = false;
                cellWidth.IsEnabled = true;
                cellHeight.IsEnabled = true;
                cellUnit.IsEnabled = true;
            }
        }

        internal GridlinesSettingsViewModel ViewModel
        {
            get { return viewModel; }
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (IsAllValidationPassed(this))
            {
                DialogResult = true;
                Close();
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        [Obfuscation]
        private void rdbShow_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                viewModel.ShowGridlines = true;
                gbxGridStyle.IsEnabled = true;
                gbxMargins.IsEnabled = true;
                gbxGridLayout.IsEnabled = true;
            }
        }

        [Obfuscation]
        private void rdbHide_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                viewModel.ShowGridlines = false;
                gbxGridStyle.IsEnabled = false;
                gbxMargins.IsEnabled = false;
                gbxGridLayout.IsEnabled = false;
            }
        }

        private bool IsAllValidationPassed(DependencyObject node)
        {
            // Check if dependency object was passed
            if (node != null)
            {
                // Check if dependency object is valid.
                // NOTE: Validation.GetHasError works for controls that have validation rules attached
                bool isValid = !Validation.GetHasError(node);
                if (!isValid)
                {
                    // If the dependency object is invalid, and it can receive the focus,
                    // set the focus
                    if (node is IInputElement) Keyboard.Focus((IInputElement)node);
                    return false;
                }
            }

            // If this dependency object is valid, check all child dependency objects
            foreach (object subnode in LogicalTreeHelper.GetChildren(node))
            {
                if (subnode is DependencyObject)
                {
                    // If a child dependency object is invalid, return false immediately,
                    // otherwise keep checking
                    if (IsAllValidationPassed((DependencyObject)subnode) == false) return false;
                }
            }

            // All dependency objects are valid
            return true;
        }

        private void cellChk_Checked(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).IsChecked.Value)
            {
                gridColumns.IsEnabled = false;
                gridRows.IsEnabled = false;
                cellWidth.IsEnabled = true;
                cellHeight.IsEnabled = true;
                cellUnit.IsEnabled = true;
            }
            else
            {
                gridColumns.IsEnabled = true;
                gridRows.IsEnabled = true;
                cellWidth.IsEnabled = false;
                cellHeight.IsEnabled = false;
                cellUnit.IsEnabled = false;
            }
        }
    }
}