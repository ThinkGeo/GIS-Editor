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
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ViewColumnsUserControl.xaml
    /// </summary>
    public partial class ViewColumnsWindow : Window
    {
        private bool isClosing;
        private string tempOriginalName;
        private FeatureLayer featureLayer;

        public ViewColumnsWindow(FeatureLayer featureLayer)
        {
            InitializeComponent();

            this.featureLayer = featureLayer;

            if (EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                GridView.Columns.Add((GridViewColumn)Resources["AliasColumn"]);
            }
            LayerNameTb.Text = featureLayer.Name;
            RefreshColumnList(featureLayer);
        }

        public Dictionary<string, string> AliasNames
        {
            get
            {
                Dictionary<string, string> alias = new Dictionary<string, string>();
                Collection<ViewColumnItem> items = (Collection<ViewColumnItem>)ColumnList.ItemsSource;
                foreach (var item in items)
                {
                    if (item.ColumnName != item.AliasName)
                    {
                        alias[item.ColumnName] = item.AliasName;
                    }
                }
                return alias;
            }
        }

        private void RefreshColumnList(FeatureLayer featureLayer)
        {
            Collection<ViewColumnItem> viewColumnItems = new Collection<ViewColumnItem>();
            featureLayer.SafeProcess(() =>
            {
                foreach (var column in featureLayer.FeatureSource.GetColumns())
                {
                    if (!string.IsNullOrEmpty(column.ColumnName))
                    {
                        string alias = featureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
                        ViewColumnItem item = new ViewColumnItem(column, alias);
                        viewColumnItems.Add(item);
                    }
                }
            });

            if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
            {
                foreach (var column in CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id])
                {
                    string alias = featureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
                    ViewColumnItem item = new ViewColumnItem(column, alias, true);
                    item.EditAction = c =>
                    {
                        DbfColumn dbfColumn = (DbfColumn)c;
                        Collection<string> columns = new Collection<string>();
                        columns.Add(c.ColumnName);
                        string tempAlias = featureLayer.FeatureSource.GetColumnAlias(dbfColumn.ColumnName);
                        AddDbfColumnWindow window = new AddDbfColumnWindow(dbfColumn, columns, DbfColumnMode.Calculated, true, tempAlias);
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        window.Owner = Application.Current.MainWindow;
                        if (window.ShowDialog().GetValueOrDefault())
                        {
                            CalculatedDbfColumn newColumn = window.DbfColumn as CalculatedDbfColumn;
                            if (newColumn != null)
                            {
                                //Check does edit
                                CalculatedDbfColumn tempColumn = (CalculatedDbfColumn)c;
                                if (newColumn.ColumnName == tempColumn.ColumnName
                                    && newColumn.CalculationType == tempColumn.CalculationType
                                    && newColumn.ColumnType == tempColumn.ColumnType
                                    && newColumn.DecimalLength == tempColumn.DecimalLength
                                    && newColumn.Length == tempColumn.Length
                                    && newColumn.LengthUnit == tempColumn.LengthUnit
                                    && newColumn.MaxLength == tempColumn.MaxLength
                                    && newColumn.AreaUnit == tempColumn.AreaUnit
                                    && newColumn.TypeName == tempColumn.TypeName)
                                {
                                    return;
                                }

                                if (CheckHasDuplicatedColumn(newColumn)) return;

                                if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
                                {
                                    CalculatedDbfColumn calColumn = (CalculatedDbfColumn)c;
                                    if (CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Contains(calColumn))
                                    {
                                        CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Remove(calColumn);
                                    }
                                    CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Add(newColumn);
                                }
                                RefreshColumnList(featureLayer);
                            }
                        }
                    };

                    item.DeleteAction = c =>
                    {
                        if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
                        {
                            CalculatedDbfColumn calColumn = (CalculatedDbfColumn)c;
                            if (CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Contains(calColumn))
                            {
                                CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Remove(calColumn);
                                RefreshColumnList(featureLayer);
                            }
                        }
                    };
                    viewColumnItems.Add(item);
                }
            }

            ColumnList.ItemsSource = viewColumnItems;
        }

        private bool CheckHasDuplicatedColumn(CalculatedDbfColumn newColumn)
        {
            Collection<ViewColumnItem> columnItems = (Collection<ViewColumnItem>)ColumnList.ItemsSource;
            string alias1 = featureLayer.FeatureSource.GetColumnAlias(newColumn.ColumnName);
            if (columnItems.Any(col => col.ColumnName == newColumn.ColumnName || col.AliasName == alias1))
            {
                MessageBox.Show("Column Name or Alias is duplicate, please input another name.", "Info", MessageBoxButton.OK);
                return true;
            }
            return false;
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewColumnItem item = (ViewColumnItem)((ListViewItem)sender).DataContext;
            tempOriginalName = item.AliasName;
            item.RenameVisibility = System.Windows.Visibility.Visible;
            item.ViewVisibility = System.Windows.Visibility.Collapsed;
        }

        [Obfuscation]
        private void ListViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!CheckHasDuplicateAlias())
            {
                ViewColumnItem currentItem = (ViewColumnItem)((ListViewItem)sender).DataContext;
                Collection<ViewColumnItem> columnItems = (Collection<ViewColumnItem>)ColumnList.ItemsSource;
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
                ViewColumnItem item = ((Collection<ViewColumnItem>)ColumnList.ItemsSource).FirstOrDefault(v => v.RenameVisibility == Visibility.Visible);
                if (item != null)
                {
                    item.RenameVisibility = System.Windows.Visibility.Collapsed;
                    item.ViewVisibility = System.Windows.Visibility.Visible;
                    item.AliasName = tempOriginalName;
                }
            }
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

        public bool CheckHasDuplicateAlias()
        {
            bool result = false;
            Collection<ViewColumnItem> items = (Collection<ViewColumnItem>)ColumnList.ItemsSource;
            if (items.GroupBy(i => i.AliasName).Any(i => i.Count() > 1))
            {
                MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AliasIsDuplicateInfoText"), "Info", MessageBoxButton.OK);
                result = true;
            }
            return result;
        }

        private void SetRenameVisibility(object sender)
        {
            if (!isClosing)
            {
                TextBox textBox = (TextBox)sender;
                ViewColumnItem item = (ViewColumnItem)textBox.DataContext;
                if (CheckHasDuplicateAlias())
                {
                    item.AliasName = tempOriginalName;
                }
                item.RenameVisibility = System.Windows.Visibility.Collapsed;
                item.ViewVisibility = System.Windows.Visibility.Visible;
            }
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
        }

        [Obfuscation]
        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isClosing = true;
        }

        [Obfuscation]
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CalculatedDbfColumn column = new CalculatedDbfColumn("CalculatedArea", DbfColumnType.Float, 20, 5, CalculatedDbfColumnType.Area, AreaUnit.Acres);
            featureLayer.Open();
            switch (featureLayer.FeatureSource.GetFirstFeaturesWellKnownType())
            {
                case WellKnownType.Invalid:
                case WellKnownType.GeometryCollection:
                case WellKnownType.Point:
                case WellKnownType.Multipoint:
                    break;
                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    column = new CalculatedDbfColumn("CalculatedLength", DbfColumnType.Float, 20, 5, CalculatedDbfColumnType.Length, DistanceUnit.Meter);
                    break;
                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    column = new CalculatedDbfColumn("CalculatedArea", DbfColumnType.Float, 20, 5, CalculatedDbfColumnType.Area, AreaUnit.Acres);
                    break;
            }


            Collection<string> columns = new Collection<string>();
            columns.Add(column.ColumnName);
            string alias = featureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
            AddDbfColumnWindow window = new AddDbfColumnWindow(column, columns, DbfColumnMode.Calculated, true, alias);
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = Application.Current.MainWindow;
            if (window.ShowDialog().GetValueOrDefault())
            {
                CalculatedDbfColumn newColumn = window.DbfColumn as CalculatedDbfColumn;
                if (newColumn != null)
                {
                    if (CheckHasDuplicatedColumn(newColumn)) return;

                    if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
                    {
                        CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Add(newColumn);
                    }
                    else
                    {
                        CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id] = new Collection<CalculatedDbfColumn>();
                        CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id].Add(newColumn);
                    }
                    RefreshColumnList(featureLayer);
                }
            }
        }
    }
}