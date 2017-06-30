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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ExportWizardCustomizeColumnsUserControl.xaml
    /// </summary>
    public partial class ExportWizardCustomizeColumnsUserControl : UserControl
    {
        private string tempOriginalName;

        public ExportWizardCustomizeColumnsUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void ListViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!CheckHasDuplicateAlias())
            {
                ColumnEntity currentItem = (ColumnEntity)((ListViewItem)sender).DataContext;
                Collection<ColumnEntity> columnItems = (Collection<ColumnEntity>)ColumnList.ItemsSource;
                foreach (var item in columnItems)
                {
                    if (currentItem != item)
                    {
                        item.RenameVisibility = System.Windows.Visibility.Collapsed;
                        item.ViewVisibility = System.Windows.Visibility.Visible;
                    }
                }
            }
            else
            {
                ColumnEntity item = ((Collection<ColumnEntity>)ColumnList.ItemsSource).FirstOrDefault(v => v.RenameVisibility == Visibility.Visible);
                if (item != null)
                {
                    item.RenameVisibility = System.Windows.Visibility.Collapsed;
                    item.ViewVisibility = System.Windows.Visibility.Visible;
                    item.EditedColumnName = tempOriginalName;
                }
            }
        }

        [Obfuscation]
        private void AllHyperlink_Click(object sender, RoutedEventArgs e)
        {
            Collection<ColumnEntity> items = (Collection<ColumnEntity>)ColumnList.ItemsSource;
            foreach (var viewModel in items)
            {
                viewModel.IsChecked = true;
            }
        }

        [Obfuscation]
        private void NoneHyperlink_Click(object sender, RoutedEventArgs e)
        {
            Collection<ColumnEntity> items = (Collection<ColumnEntity>)ColumnList.ItemsSource;
            foreach (var viewModel in items)
            {
                viewModel.IsChecked = false;
            }
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ColumnEntity item = (ColumnEntity)((ListViewItem)sender).DataContext;
            tempOriginalName = item.EditedColumnName;
            item.RenameVisibility = System.Windows.Visibility.Visible;
            item.ViewVisibility = System.Windows.Visibility.Collapsed;
        }

        [Obfuscation]
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SetRenameVisibility(sender);
        }

        [Obfuscation]
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetRenameVisibility(sender);
            }
        }

        private void SetRenameVisibility(object sender)
        {
            TextBox textBox = (TextBox)sender;
            ColumnEntity item = (ColumnEntity)textBox.DataContext;
            if (CheckHasDuplicateAlias())
            {
                item.EditedColumnName = tempOriginalName;
            }
            item.RenameVisibility = System.Windows.Visibility.Collapsed;
            item.ViewVisibility = System.Windows.Visibility.Visible;
        }

        private bool CheckHasDuplicateAlias()
        {
            bool result = false;
            Collection<ColumnEntity> items = (Collection<ColumnEntity>)ColumnList.ItemsSource;
            if (items.GroupBy(i => i.EditedColumnName).Any(i => i.Count() > 1))
            {
                MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AliasIsDuplicateInfoText"), "Info", MessageBoxButton.OK);
                result = true;
            }
            return result;
        }
    }
}
