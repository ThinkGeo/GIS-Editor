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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for PromptDialog.xaml
    /// </summary>
    public partial class RenameMapWindow : Window
    {
        private static string originalName;

        private RenameMapViewModel renameMapViewModel;

        public RenameMapWindow(string name)
        {
            InitializeComponent();
            originalName = name;
            txtName.Focus();

            renameMapViewModel = new RenameMapViewModel(name);
            DataContext = renameMapViewModel;
        }

        public static string OriginalName
        {
            get { return originalName; }
        }

        public string NewMapName
        {
            get
            {
                return renameMapViewModel.Name;
            }
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (txtName.Text == OriginalName)
            {
                DialogResult = false;
            }
            else
            {
                DialogResult = true;
            }
        }

        [Obfuscation]
        private void TxtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (btnOK.IsEnabled && e.Key == Key.Enter)
            {
                OKClick(btnOK, new RoutedEventArgs());
            }
        }

        [Obfuscation]
        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnOK.IsEnabled = IsAllValidationPassed(this) && !originalName.Equals(NewMapName);
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
                    //if (node is IInputElement) Keyboard.Focus((IInputElement)node);
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
    }
}