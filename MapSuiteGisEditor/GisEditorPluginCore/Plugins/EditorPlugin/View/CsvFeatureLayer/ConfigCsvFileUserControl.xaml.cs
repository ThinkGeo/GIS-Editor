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
using System.ComponentModel;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ConfigCsvFileUserControl : CreateFeatureLayerUserControl
    {
        private ConfigCsvFileViewModel viewModel;
        private FeatureLayer featureLayer;

        public ConfigCsvFileUserControl(FeatureLayer featureLayer)
        {
            InitializeComponent();

            if (EditorUIPlugin.IsRelateAndAliasEnabled)
            {
                GridView.Columns.Insert(GridView.Columns.Count - 2, (GridViewColumn)Resources["AliasColumn"]);
            }

            this.featureLayer = featureLayer;

            viewModel = new ConfigCsvFileViewModel(featureLayer);
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = viewModel;
        }

        public ConfigCsvFileViewModel ViewModel
        {
            get { return viewModel; }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("SelectedShapeType"))
            {
                if (ViewModel.SelectedShapeType == GeneralShapeFileType.Point)
                {
                    lonLatRadioButton.IsEnabled = true;
                    wktRadioButton.IsEnabled = true;
                }
                else
                {
                    lonLatRadioButton.IsEnabled = false;
                    wktRadioButton.IsEnabled = true;
                    wktRadioButton.IsChecked = true;
                    WKTClick(sender, null);
                }
            }
        }

        protected override string InvalidMessageCore
        {
            get
            {
                return GetInvalidMessage();
            }
        }

        private string GetInvalidMessage()
        {
            string message = string.Empty;
            if (string.IsNullOrEmpty(ViewModel.FileName))
            {
                message = "Layer name can't be empty.";
            }
            if (!string.IsNullOrEmpty(ViewModel.FileName) && !ValidateFileName(ViewModel.FileName))
            {
                message = "A file name can't contain any of the following characters:" + Environment.NewLine + "\\ / : * ? \" < > |";
            }
            else if (ViewModel.CsvColumns.Count == 0)
            {
                message = "There must be at least one column.";
            }
            else if (!Directory.Exists(ViewModel.OutputFolder))
            {
                message = "Folder path is invalid.";
            }

            if (string.IsNullOrEmpty(message))
            {
                if (viewModel.MappingType == DelimitedSpatialColumnsType.WellKnownText && viewModel.Delimiter == ",")
                {
                    message = "When mapping type is WellKnownText, delimiter cannot be comma(,).";
                }
                else if (viewModel.MappingType == DelimitedSpatialColumnsType.WellKnownText && viewModel.CsvColumns.FirstOrDefault(c => c.SelectedCsvColumnType == CsvColumnType.WKT) == null)
                {
                    message = "There must be one WKT type column.";
                }
                else if (viewModel.MappingType == DelimitedSpatialColumnsType.XAndY && viewModel.CsvColumns.FirstOrDefault(c => c.SelectedCsvColumnType == CsvColumnType.Longitude) == null)
                {
                    message = "There must be one Longitude type column.";
                }
                else if (viewModel.MappingType == DelimitedSpatialColumnsType.XAndY && viewModel.CsvColumns.FirstOrDefault(c => c.SelectedCsvColumnType == CsvColumnType.Latitude) == null)
                {
                    message = "There must be one Latitude type column.";
                }
            }

            return message;
        }

        private bool ValidateFileName(string fileName)
        {
            return !(fileName.Contains("\\")
                || fileName.Contains("/")
                || fileName.Contains(":")
                || fileName.Contains("*")
                || fileName.Contains("?")
                || fileName.Contains("\"")
                || fileName.Contains("<")
                || fileName.Contains(">")
                || fileName.Contains("|")
                );
        }

        protected override ConfigureFeatureLayerParameters GetFeatureLayerInfoCore()
        {
            ConfigureFeatureLayerParameters parameters = new ConfigureFeatureLayerParameters();

            if (featureLayer != null)
            {
                IEnumerable<FeatureSourceColumn> addedColumns = viewModel.CsvColumns.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Added).Select(l => l.ToFeatureSourceColumn());
                IEnumerable<AddNewCsvColumnViewModel> updatedColumnItems = viewModel.CsvColumns.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Updated);
                IEnumerable<FeatureSourceColumn> deletedColumns = viewModel.CsvColumns.Where(c => c.ChangedStatus == FeatureSourceColumnChangedStatus.Deleted).Select(l => l.ToFeatureSourceColumn());

                foreach (var item in deletedColumns)
                {
                    parameters.DeletedColumns.Add(item);
                }

                foreach (var item in updatedColumnItems)
                {
                    parameters.UpdatedColumns[item.OriginalColumnName] = item.ToFeatureSourceColumn();
                }

                foreach (var item in addedColumns)
                {
                    parameters.AddedColumns.Add(item);
                }
            }
            else
            {
                foreach (var item in viewModel.CsvColumns.Where(c => c.ChangedStatus != FeatureSourceColumnChangedStatus.Deleted).Select(c => c.ToFeatureSourceColumn()))
                {
                    parameters.AddedColumns.Add(item);
                }
            }

            string pathFileName = string.Format(@"{0}\{1}.{2}", ViewModel.OutputFolder, ViewModel.FileName, "csv");
            parameters.LayerUri = new Uri(Path.GetFullPath(pathFileName));
            switch (ViewModel.SelectedShapeType)
            {
                case GeneralShapeFileType.Point:
                    parameters.WellKnownType = WellKnownType.Point;
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
        private void LonLatClick(object sender, RoutedEventArgs e)
        {
            //ViewModel.MappingType = CsvMappingType.LongitudeAndLatitude;
        }

        private void WKTClick(object sender, RoutedEventArgs e)
        {
            //ViewModel.MappingType = CsvMappingType.WellKnownText;
        }

        [Obfuscation]
        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK || tmpResult == System.Windows.Forms.DialogResult.Yes)
                {
                    ViewModel.OutputFolder = tmpDialog.SelectedPath;
                }
            });
        }

        [Obfuscation]
        private void MoveBottomClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = ViewModel.CsvColumns[currentIndex];
                ViewModel.CsvColumns.RemoveAt(ColumnList.SelectedIndex);
                ViewModel.CsvColumns.Insert(ColumnList.Items.Count, selectedItem);
                ColumnList.SelectedIndex = ColumnList.Items.Count - 1;
            }
        }

        [Obfuscation]
        private void MoveTopClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = ViewModel.CsvColumns[currentIndex];
                ViewModel.CsvColumns.RemoveAt(ColumnList.SelectedIndex);
                ViewModel.CsvColumns.Insert(0, selectedItem);
                ColumnList.SelectedIndex = 0;
            }
        }

        [Obfuscation]
        private void MoveUpClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != 0
                && currentIndex != -1)
            {
                var selectedItem = ViewModel.CsvColumns[currentIndex];
                ViewModel.CsvColumns.RemoveAt(ColumnList.SelectedIndex);
                ViewModel.CsvColumns.Insert(currentIndex - 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex - 1;
            }
        }

        [Obfuscation]
        private void MoveDownClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = ColumnList.SelectedIndex;
            if (currentIndex != ColumnList.Items.Count - 1
                && currentIndex != -1)
            {
                var selectedItem = ViewModel.CsvColumns[currentIndex];
                ViewModel.CsvColumns.RemoveAt(ColumnList.SelectedIndex);
                ViewModel.CsvColumns.Insert(currentIndex + 1, selectedItem);
                ColumnList.SelectedIndex = currentIndex + 1;
            }
        }

        [Obfuscation]
        private void EditColumnClick(object sender, RoutedEventArgs e)
        {
            AddNewCsvColumnViewModel columnViewModel = sender.GetDataContext<AddNewCsvColumnViewModel>();
            if (columnViewModel != null)
            {
                var columnTyps = viewModel.GetAvailableCsvColumnType();
                if (!columnTyps.Contains(columnViewModel.SelectedCsvColumnType))
                {
                    columnTyps.Add(columnViewModel.SelectedCsvColumnType);
                }
                AddNewCsvColumnWindow addNewCsvColumnWindow = new AddNewCsvColumnWindow(columnTyps);

                addNewCsvColumnWindow.ViewModel.ColumnName = columnViewModel.ColumnName;
                addNewCsvColumnWindow.ViewModel.AliasName = columnViewModel.AliasName;
                addNewCsvColumnWindow.ViewModel.SelectedCsvColumnType = columnViewModel.SelectedCsvColumnType;
                if (addNewCsvColumnWindow.ShowDialog().GetValueOrDefault())
                {
                    int index = ViewModel.CsvColumns.IndexOf(columnViewModel);
                    ViewModel.CsvColumns.Insert(index, addNewCsvColumnWindow.ViewModel);
                    ViewModel.CsvColumns.RemoveAt(index + 1);
                    addNewCsvColumnWindow.ViewModel.ChangedStatus = FeatureSourceColumnChangedStatus.Updated;
                    if (!string.IsNullOrEmpty(columnViewModel.OriginalColumnName))
                    {
                        addNewCsvColumnWindow.ViewModel.OriginalColumnName = columnViewModel.OriginalColumnName;
                    }
                    else
                    {
                        addNewCsvColumnWindow.ViewModel.OriginalColumnName = columnViewModel.ColumnName;
                    }
                }
            }
        }

        [Obfuscation]
        private void DeleteColumnClick(object sender, RoutedEventArgs e)
        {
            AddNewCsvColumnViewModel columnViewModel = sender.GetDataContext<AddNewCsvColumnViewModel>();
            if (columnViewModel != null)
            {
                columnViewModel.ChangedStatus = FeatureSourceColumnChangedStatus.Deleted;
            }
        }
    }
}