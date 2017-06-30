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
    public partial class ConfigFileGeoDataBaseUserControl : CreateFeatureLayerUserControl
    {
        private ConfigFileGeoLayerStructureViewMode configLayerStructureViewModel;

        public ConfigFileGeoDataBaseUserControl(FeatureLayer featureLayer)
        {
            InitializeComponent();

            if (EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                GridView.Columns.Insert(GridView.Columns.Count - 2, (GridViewColumn)Resources["AliasColumn"]);
            }

            var types = Enum.GetValues(typeof(GeneralShapeFileType)).Cast<GeneralShapeFileType>().ToList();
            types.RemoveAt(1);
            ShapeFileTypes.ItemsSource = types;

            configLayerStructureViewModel = new ConfigFileGeoLayerStructureViewMode(featureLayer);
            DataContext = configLayerStructureViewModel;
        }

        protected override string InvalidMessageCore
        {
            get
            {
                return configLayerStructureViewModel.GetInvalidMessage();
            }
        }

        protected override bool IsReadonlyCore
        {
            get
            {
                return !configLayerStructureViewModel.IsAliasEnabled;
            }
            set
            {
                configLayerStructureViewModel.IsAliasEnabled = !value;
            }
        }

        protected override ConfigureFeatureLayerParameters GetFeatureLayerInfoCore()
        {
            ConfigFileGeoLayerStructureViewMode viewModel = DataContext as ConfigFileGeoLayerStructureViewMode;
            ConfigureFeatureLayerParameters parameters = new ConfigureFeatureLayerParameters();

            Collection<FeatureSourceColumn> originalColumns = new Collection<FeatureSourceColumn>();
            if (configLayerStructureViewModel.FeatureLayer != null)
            {
                configLayerStructureViewModel.FeatureLayer.SafeProcess(() =>
                {
                    originalColumns = configLayerStructureViewModel.FeatureLayer.FeatureSource.GetColumns();
                });
            }
            IEnumerable<FeatureSourceColumn> addedColumns = viewModel.ColumnListItemSource.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Added).Select(l => l.FeatureSourceColumn);
            IEnumerable<FeatureSourceColumnItem> updatedColumnItems = viewModel.ColumnListItemSource.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Updated);
            IEnumerable<FeatureSourceColumn> deletedColumns = originalColumns.Where(o => viewModel.ColumnListItemSource.All(c => c.ColumnName != o.ColumnName) && updatedColumnItems.All(u => u.OrignalColumnName != o.ColumnName));

            foreach (var item in deletedColumns)
            {
                parameters.DeletedColumns.Add(item);
            }

            foreach (var item in updatedColumnItems)
            {
                parameters.UpdatedColumns[item.OrignalColumnName] = item.FeatureSourceColumn;
            }

            foreach (var item in addedColumns)
            {
                parameters.AddedColumns.Add(item);
            }

            string pathFileName = string.Format(@"{0}\{1}.{2}", viewModel.LayerUri.OriginalString, viewModel.LayerName, "gdb");
            parameters.LayerUri = new Uri(Path.GetFullPath(pathFileName));
            parameters.CustomData["TableName"] = viewModel.TableName;
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

        private void RestoreTargetFolderIfExist(FeatureLayer fileFeatureLayer)
        {
            if (!string.IsNullOrEmpty(FolderHelper.LastSelectedFolder) && fileFeatureLayer == null)
            {
                configLayerStructureViewModel.LayerUri = new Uri(FolderHelper.LastSelectedFolder);
            }
        }

        [Obfuscation]
        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK || tmpResult == System.Windows.Forms.DialogResult.Yes)
                {
                    configLayerStructureViewModel.LayerUri = new Uri(tmpDialog.SelectedPath);
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
                if (model != null) configLayerStructureViewModel.EditCommand.Execute(model.Id);
            }
        }

        [Obfuscation]
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = configLayerStructureViewModel.ColumnListItemSource[currentIndex];
                configLayerStructureViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                configLayerStructureViewModel.ColumnListItemSource.Insert(currentIndex - 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex - 1;
            }
        }

        [Obfuscation]
        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = configLayerStructureViewModel.ColumnListItemSource[currentIndex];
                configLayerStructureViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                configLayerStructureViewModel.ColumnListItemSource.Insert(currentIndex + 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex + 1;
            }
        }

        [Obfuscation]
        private void MoveButtomButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = configLayerStructureViewModel.ColumnListItemSource[currentIndex];
                configLayerStructureViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                configLayerStructureViewModel.ColumnListItemSource.Insert(ColumnList.Items.Count - 1, selectedItem);
                ColumnList.SelectedIndex = ColumnList.Items.Count - 1;
            }
        }

        [Obfuscation]
        private void MoveTopButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = configLayerStructureViewModel.ColumnListItemSource[currentIndex];
                configLayerStructureViewModel.ColumnListItemSource.RemoveAt(ColumnList.SelectedIndex);
                configLayerStructureViewModel.ColumnListItemSource.Insert(0, selectedItem);
                ColumnList.SelectedIndex = 0;
            }
        }

        [Obfuscation]
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string id = button.CommandParameter.ToString();
            configLayerStructureViewModel.EditCommand.Execute(id);
        }

        [Obfuscation]
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string id = button.CommandParameter.ToString();
            configLayerStructureViewModel.RemoveCommand.Execute(id);
        }
    }
}