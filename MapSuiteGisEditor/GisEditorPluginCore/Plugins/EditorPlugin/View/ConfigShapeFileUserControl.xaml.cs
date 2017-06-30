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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ConfigShapeFileUserControl.xaml
    /// </summary>
    public partial class ConfigShapeFileUserControl : CreateFeatureLayerUserControl
    {
        private Dictionary<string, string> tempIdColumnNames;
        private ConfigShapeFileViewModel createNewShapeFileViewModel;
        private bool isSorted;

        public ConfigShapeFileUserControl(ShapeFileFeatureLayer shapeFileFeatureLayer)
        {
            InitializeComponent();

            if (EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                GridView.Columns.Insert(1, (GridViewColumn)Resources["AliasColumn"]);
            }

            createNewShapeFileViewModel = new ConfigShapeFileViewModel(shapeFileFeatureLayer);
            DataContext = createNewShapeFileViewModel;

            tempIdColumnNames = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(FolderHelper.LastSelectedFolder) && createNewShapeFileViewModel.ShpFileFeatureLayer == null)
            {
                createNewShapeFileViewModel.FolderPath = FolderHelper.LastSelectedFolder;
            }
        }

        protected override string InvalidMessageCore
        {
            get
            {
                return createNewShapeFileViewModel.GetInvalidMessage();
            }
        }

        protected override bool IsReadonlyCore
        {
            get
            {
                return !createNewShapeFileViewModel.IsAliasEnabled;
            }
            set
            {
                createNewShapeFileViewModel.IsAliasEnabled = !value;
            }
        }

        protected override ConfigureFeatureLayerParameters GetFeatureLayerInfoCore()
        {
            ConfigShapeFileViewModel viewModel = DataContext as ConfigShapeFileViewModel;
            ConfigureFeatureLayerParameters parameters = new ConfigureFeatureLayerParameters();

            Collection<FeatureSourceColumn> originalColumns = new Collection<FeatureSourceColumn>();
            if (createNewShapeFileViewModel.ShpFileFeatureLayer != null)
            {
                createNewShapeFileViewModel.ShpFileFeatureLayer.SafeProcess(() =>
                {
                    originalColumns = createNewShapeFileViewModel.ShpFileFeatureLayer.FeatureSource.GetColumns();
                });
            }

            IEnumerable<DbfColumn> addedColumns = viewModel.ColumnListItemSource.Where(c => isSorted || c.ChangedStatus == FeatureSourceColumnChangedStatus.Added).Select(l => l.DbfColumn);
            IEnumerable<DbfColumnItem> updatedColumnItems = viewModel.ColumnListItemSource.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Updated);
            IEnumerable<FeatureSourceColumn> deletedColumns = originalColumns.Where(o => isSorted || (viewModel.ColumnListItemSource.All(c => c.ColumnName != o.ColumnName) && updatedColumnItems.All(u => u.OrignalColumnName != o.ColumnName)));

            foreach (var item in deletedColumns)
            {
                parameters.DeletedColumns.Add(item);
            }

            foreach (var item in updatedColumnItems)
            {
                parameters.UpdatedColumns[item.OrignalColumnName] = item.DbfColumn;
            }

            if (viewModel.TruncatedColumns.Count > 0)
            {
                viewModel.ShpFileFeatureLayer.SafeProcess(() =>
                {
                    foreach (var feature in viewModel.ShpFileFeatureLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns))
                    {
                        foreach (var truncatedColumn in viewModel.TruncatedColumns)
                        {
                            string tempValue = feature.ColumnValues[truncatedColumn.Key];
                            if (tempValue.Length > truncatedColumn.Value)
                            {
                                feature.ColumnValues[truncatedColumn.Key] = tempValue.Substring(0, truncatedColumn.Value);
                            }
                        }
                        parameters.UpdatedFeatures[feature.Id] = feature;
                    }
                });
            }

            foreach (var item in addedColumns)
            {
                item.MaxLength = item.MaxLength;
                parameters.AddedColumns.Add(item);
            }

            foreach (var item in viewModel.ColumnListItemSource)
            {
                if (item.AliasName != item.ColumnName)
                {
                    parameters.CustomData[item.ColumnName] = item.AliasName;
                }
            }

            string pathFileName = string.Format(@"{0}\{1}.shp", viewModel.FolderPath, viewModel.LayerName);
            parameters.LayerUri = new Uri(Path.GetFullPath(pathFileName));
            switch (viewModel.ShpFileType)
            {
                case GeneralShapeFileType.Point:
                    parameters.WellKnownType = WellKnownType.Point;
                    break;

                case GeneralShapeFileType.Multipoint:
                    parameters.WellKnownType = WellKnownType.Multipoint;
                    break;

                case GeneralShapeFileType.Line:
                    parameters.WellKnownType = WellKnownType.Line;
                    break;

                case GeneralShapeFileType.Area:
                    parameters.WellKnownType = WellKnownType.Polygon;
                    break;

                default:
                    parameters.WellKnownType = WellKnownType.Invalid;
                    break;
            }

            return parameters;
        }

        [Obfuscation]
        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK || tmpResult == System.Windows.Forms.DialogResult.Yes)
                {
                    createNewShapeFileViewModel.FolderPath = tmpDialog.SelectedPath;
                }
            });
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                var model = item.Content as DbfColumnItem;
                if (model != null) createNewShapeFileViewModel.EditCommand.Execute(model.Id);
            }
        }

        [Obfuscation]
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = createNewShapeFileViewModel.ColumnListItemSource[currentIndex];
                createNewShapeFileViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                createNewShapeFileViewModel.ColumnListItemSource.Insert(currentIndex - 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex - 1;
                isSorted = true;
            }
        }

        [Obfuscation]
        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = createNewShapeFileViewModel.ColumnListItemSource[currentIndex];
                createNewShapeFileViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                createNewShapeFileViewModel.ColumnListItemSource.Insert(currentIndex + 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex + 1;
                isSorted = true;
            }
        }

        [Obfuscation]
        private void MoveButtomButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = createNewShapeFileViewModel.ColumnListItemSource[currentIndex];
                createNewShapeFileViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                createNewShapeFileViewModel.ColumnListItemSource.Insert(ColumnList.Items.Count - 1, selectedItem);
                ColumnList.SelectedIndex = ColumnList.Items.Count - 1;
                isSorted = true;
            }
        }

        [Obfuscation]
        private void MoveTopButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = createNewShapeFileViewModel.ColumnListItemSource[currentIndex];
                createNewShapeFileViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                createNewShapeFileViewModel.ColumnListItemSource.Insert(0, selectedItem);
                ColumnList.SelectedIndex = 0;
                isSorted = true;
            }
        }

        [Obfuscation]
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string id = button.CommandParameter.ToString();
            createNewShapeFileViewModel.EditCommand.Execute(id);
        }

        [Obfuscation]
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string id = button.CommandParameter.ToString();
            createNewShapeFileViewModel.RemoveCommand.Execute(id);
        }
    }
}