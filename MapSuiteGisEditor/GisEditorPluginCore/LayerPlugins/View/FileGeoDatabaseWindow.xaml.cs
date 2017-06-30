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
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for FileGeoDatabaseWindow.xaml
    /// </summary>
    public partial class FileGeoDatabaseWindow : Window
    {
        private string gdbPath;
        private string tableName;
        private string featureIdColumn;

        public FileGeoDatabaseWindow(string gdbPath)
        {
            InitializeComponent();

            okButton.IsEnabled = false;
            this.gdbPath = gdbPath;
            Initialize();
        }

        public string TableName
        {
            get { return tableName; }
        }

        public string FeatureIdColumn
        {
            get { return featureIdColumn; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tableName = (string)tableComBox.SelectedItem;
            featureIdColumn = (string)fieldComBox.SelectedItem;
            DialogResult = true;
        }

        private void TableComBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fieldComBox.Items.Clear();
            if (e.AddedItems.Count > 0)
            {
                Collection<string> fields = FileGeoDatabaseFeatureSource.GetColumnNames(gdbPath, e.AddedItems[0] as string);
                foreach (string item in fields)
                {
                    fieldComBox.Items.Add(item);
                }
                if (fields.Contains("OBJECTID"))
                {
                    fieldComBox.SelectedItem = "OBJECTID";
                }
                else
                {
                    fieldComBox.SelectedIndex = 0;
                }

                okButton.IsEnabled = tableComBox.SelectedItem != null && fieldComBox.SelectedItem != null;
            }
        }

        private void FieldComBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            okButton.IsEnabled = tableComBox.SelectedItem != null && fieldComBox.SelectedItem != null;
        }

        private void Initialize()
        {
            Collection<string> tables = FileGeoDatabaseFeatureSource.GetTableNames(gdbPath);
            foreach (string table in tables)
            {
                tableComBox.Items.Add(table.TrimStart('\\'));
                tableComBox.SelectedIndex = 0;
            }
        }
    }
}