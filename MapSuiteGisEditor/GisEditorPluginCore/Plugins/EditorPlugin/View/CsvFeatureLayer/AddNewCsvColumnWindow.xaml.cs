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
using System.Collections.ObjectModel;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AddNewCsvColumnWindow.xaml
    /// </summary>
    public partial class AddNewCsvColumnWindow : Window
    {
        public AddNewCsvColumnWindow(Collection<CsvColumnType> columnTypes)
        {
            InitializeComponent();

            if (!EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                mainGrid.RowDefinitions[2].Height = new GridLength(0);
            }

            DataContext = new AddNewCsvColumnViewModel(columnTypes);
            txtColumnName.Focus();
        }

        public AddNewCsvColumnViewModel ViewModel
        {
            get
            {
                return (AddNewCsvColumnViewModel)DataContext;
            }
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ViewModel.ColumnName))
            {
                DialogResult = true;
                Close();
            }
            else if (string.IsNullOrEmpty(ViewModel.ColumnName) || ViewModel.ColumnName.Length > 255)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AddNewCsvColumnWindowColumnNameValidation"), GisEditor.LanguageManager.GetStringResource("AddNewCsvColumnWindowInvalidColumnLabel"));
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
