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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DualList.xaml
    /// </summary>
    public partial class DualList : UserControl
    {
        private ObservableCollection<FeatureSourceColumnDefinition> columnsToInclude;
        private ObservableCollection<FeatureSourceColumnDefinition> leftColumns;

        public DualList()
        {
            InitializeComponent();
            Loaded += DualList_Loaded;
        }

        private void DualList_Loaded(object sender, RoutedEventArgs e)
        {
            BlendWizardShareObject blendWizardShareObject = DataContext as BlendWizardShareObject;
            if (blendWizardShareObject != null)
            {
                if (blendWizardShareObject.ColumnsToInclude.Count == 0)
                {
                    //LeftList.SelectedItem = ((ObservableCollection<FeatureSourceColumnDefinition>)LeftList.ItemsSource).First();
                    LayersComboBox.SelectedIndex = 0;
                }
            }
        }

        private void CheckColumnSources()
        {
            columnsToInclude = (ObservableCollection<FeatureSourceColumnDefinition>)RightList.ItemsSource;
            leftColumns = (ObservableCollection<FeatureSourceColumnDefinition>)LeftList.ItemsSource;
        }

        [Obfuscation]
        private void Left2Right_Click(object sender, RoutedEventArgs e)
        {
            CheckColumnSources();
            var selectedItems = LeftList.SelectedItems.Cast<FeatureSourceColumnDefinition>().OrderBy(f => leftColumns.IndexOf(f)).ToArray();

            foreach (var item in selectedItems)
            {
                leftColumns.Remove(item);
                columnsToInclude.Add(item);
            }
        }

        [Obfuscation]
        private void Right2Left_Click(object sender, RoutedEventArgs e)
        {
            CheckColumnSources();

            var selectedItems = RightList.SelectedItems.Cast<FeatureSourceColumnDefinition>().OrderBy(f => columnsToInclude.IndexOf(f)).ToArray();
            foreach (var item in selectedItems)
            {
                columnsToInclude.Remove(item);
            }
            List<SelectColumnsEntity> entities = ((List<SelectColumnsEntity>)LayersComboBox.ItemsSource);
            foreach (var group in selectedItems.GroupBy(s => s.LayerName))
            {
                var result = entities.FirstOrDefault(en => en.LayerName.Equals(group.Key));
                if (result != null)
                {
                    foreach (var item in group)
                    {
                        result.Columns.Add(item);
                    }
                }
            }
        }

        [Obfuscation]
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            FeatureSourceColumnDefinition selectedColumn = (FeatureSourceColumnDefinition)RightList.SelectedItem;
            if (selectedColumn != null && selectedColumn != columnsToInclude.FirstOrDefault())
            {
                int index = columnsToInclude.IndexOf(selectedColumn) - 1;
                columnsToInclude.Remove(selectedColumn);
                columnsToInclude.Insert(index, selectedColumn);
                RightList.SelectedIndex = index;
            }
        }

        [Obfuscation]
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            FeatureSourceColumnDefinition selectedColumn = (FeatureSourceColumnDefinition)RightList.SelectedItem;
            if (selectedColumn != null && selectedColumn != columnsToInclude.LastOrDefault())
            {
                int index = columnsToInclude.IndexOf(selectedColumn) + 1;
                columnsToInclude.Remove(selectedColumn);
                columnsToInclude.Insert(index, selectedColumn);
                RightList.SelectedIndex = index;
            }
        }
    }
}