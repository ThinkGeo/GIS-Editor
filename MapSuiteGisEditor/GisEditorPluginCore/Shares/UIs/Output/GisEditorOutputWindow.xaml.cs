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


using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    [Obfuscation]
    public partial class GisEditorOutputWindow : OutputWindow
    {
        [Obfuscation]
        private Collection<ColumnEntity> entities;
        [Obfuscation]
        private OutputUserControlViewModel viewModel;
        private string tempOriginalName;

        public GisEditorOutputWindow()
            : this(string.Empty)
        { }

        public GisEditorOutputWindow(string fileName)
        {
            InitializeComponent();
            btnOK.IsEnabled = false;
            Loaded += new RoutedEventHandler(GisEditorOutputWindow_Loaded);
            viewModel = new OutputUserControlViewModel(string.Empty, fileName);
            outputUserControl.DataContext = viewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override string ExtensionFilterCore
        {
            get
            {
                return viewModel.ExtensionFilter;
            }
            set
            {
                viewModel.ExtensionFilter = value;
            }
        }

        protected override string DefaultPrefixCore
        {
            get
            {
                return viewModel.DefaultPrefix;
            }
            set
            {
                viewModel.DefaultPrefix = value;
            }
        }

        protected override string Proj4ProjectionParametersStringCore
        {
            get
            {
                return txtProjection.Text;
            }
            set
            {
                txtProjection.Text = value;
            }
        }

        [Obfuscation]
        private void AllHyperlink_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModel in entities)
            {
                viewModel.IsChecked = true;
            }
        }

        [Obfuscation]
        private void NoneHyperlink_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModel in entities)
            {
                viewModel.IsChecked = false;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            btnOK.IsEnabled = !string.IsNullOrEmpty(ViewModel.OutputMode == OutputMode.ToFile ? ViewModel.OutputPathFileName : ViewModel.TempFileName);
        }

        [Obfuscation]
        private void GisEditorOutputWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OutputMode = OutputMode;

            entities = new Collection<ColumnEntity>();
            if (CustomData.ContainsKey("Columns"))
            {
                var columns = (Collection<FeatureSourceColumn>)CustomData["Columns"];

                Collection<string> fixedColumns = new Collection<string>();
                foreach (var column in columns)
                {
                    string editedName = column.ColumnName;
                    if (column.ColumnName.Contains("."))
                    {
                        int index = column.ColumnName.IndexOf(".") + 1;
                        editedName = column.ColumnName.Substring(index, column.ColumnName.Length - index);
                    }
                    if (fixedColumns.Contains(editedName))
                    {
                        int i = 1;
                        while (fixedColumns.Select(a => a).Contains(editedName))
                        {
                            int length = i.ToString(CultureInfo.InvariantCulture).Length;
                            editedName = editedName.Substring(0, editedName.Length - length) + i;
                            i++;
                        }
                    }
                    fixedColumns.Add(editedName);
                    entities.Add(new ColumnEntity() { ColumnName = column.ColumnName, EditedColumnName = editedName, ColumnType = column.TypeName, IsChecked = true });
                }

                ColumnList.ItemsSource = entities;
            }

            if (CustomData.ContainsKey("CustomizeColumnNames"))
            {
                GridView.Columns.Insert(2, (GridViewColumn)Resources["EditedColumn"]);
            }
        }

        public OutputUserControlViewModel ViewModel
        {
            get { return viewModel; }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            try
            {
                //If column's count is more than 128, we will trim them. Becasue esri cannot support more than 128 columns.
                if (entities.Where(n => n.IsChecked).Count() > 128 && ExtensionFilter.EndsWith(".shp"))
                {
                    var result = MessageBox.Show("The column count is out of Shapefile structure limit(Max 128 fields). It might cause compatibility problem when opening with some other Shapefile utilities. Click Yes to keep top of 128 columns, or click No to keep origin.", "Info", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        entities = new Collection<ColumnEntity>(entities.Take(128).ToList());
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                LayerUri = ViewModel.OutputMode == OutputMode.ToFile ? new Uri(ViewModel.OutputPathFileName) : new Uri(Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), ViewModel.TempFileName + Extension));
                string folderPath = Path.GetDirectoryName(LayerUri.LocalPath);
                if (!LayerUri.LocalPath.EndsWith(Extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    var result = MessageBox.Show("The path you entered is not valid, please re-enter.", "Info");
                }
                else if (!Directory.Exists(folderPath))
                {
                    var result = MessageBox.Show("Directory does not exsit, do you want to create it?", "Info", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        Directory.CreateDirectory(folderPath);
                        DialogResult = true;
                    }
                }
                else
                {
                    Collection<string> columnNames = new Collection<string>();
                    Dictionary<string, string> editColumnNames = new Dictionary<string, string>();
                    foreach (var entry in entities)
                    {
                        if (entry.IsChecked)
                        {
                            columnNames.Add(entry.ColumnName);
                            editColumnNames[entry.ColumnName] = entry.EditedColumnName;
                        }
                    }

                    if (CustomData.ContainsKey("CustomizeColumnNames"))
                    {
                        bool isTooLong = editColumnNames.Values.Any(c => c.Length > 10);
                        if (isTooLong)
                        {
                            MessageBoxResult result = MessageBox.Show("One or more columns are longer than 10 bytes. A column cannot be more than 10 bytes in a shape file through specification. Click 'Yes' would truncate the column name to 10 bytes. Do you want to continue?", "Info", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                    }

                    if (columnNames.Count == 0)
                    {
                        MessageBox.Show("Please select at least one column.", "Info", MessageBoxButton.OK);
                    }
                    else
                    {
                        CustomData["Columns"] = columnNames;
                        CustomData["EditedColumns"] = editColumnNames;
                        DialogResult = true;
                    }
                }
            }
            catch
            {
                var result = MessageBox.Show("The path you entered is not valid, please re-enter.", "Info");
            }
        }

        [Obfuscation]
        private void ChooseProjectionClick(object sender, RoutedEventArgs e)
        {
            ProjectionWindow projectionWindow = new ProjectionWindow(Proj4ProjectionParametersStringCore, "Choose the projection you want to save", "");
            if (projectionWindow.ShowDialog().GetValueOrDefault())
            {
                string selectedProj4Parameter = projectionWindow.Proj4ProjectionParameters;
                if (!string.IsNullOrEmpty(selectedProj4Parameter))
                {
                    Proj4ProjectionParametersStringCore = selectedProj4Parameter;
                }
            }
        }

        [Obfuscation]
        private void ShowOptionsClick(object sender, RoutedEventArgs e)
        {
            if (ProjectionGrid.Visibility == Visibility.Visible)
            {
                ProjectionGrid.Visibility = System.Windows.Visibility.Collapsed;
                ((Button)sender).Template = this.FindResource("ShowOptionsTemplte") as ControlTemplate;
            }
            else
            {
                ProjectionGrid.Visibility = System.Windows.Visibility.Visible;
                ((Button)sender).Template = this.FindResource("HideOptionsTemplte") as ControlTemplate;
            }
        }

        [Obfuscation]
        private void ShowColumnsOptionsClick(object sender, RoutedEventArgs e)
        {
            if (ColumnGrid.Visibility == Visibility.Visible)
            {
                ColumnGrid.Visibility = System.Windows.Visibility.Collapsed;
                ((Button)sender).Template = this.FindResource("ShowColumnOptionsTemplte") as ControlTemplate;
            }
            else
            {
                ColumnGrid.Visibility = System.Windows.Visibility.Visible;
                ((Button)sender).Template = this.FindResource("HideColumnOptionsTemplte") as ControlTemplate;
            }
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = new SolidColorBrush(Color.FromRgb(120, 174, 229));
            ((Border)sender).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 226, 242));
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = null;
            ((Border)sender).Background = null;
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
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ColumnEntity item = (ColumnEntity)((ListViewItem)sender).DataContext;
            tempOriginalName = item.EditedColumnName;
            item.RenameVisibility = System.Windows.Visibility.Visible;
            item.ViewVisibility = System.Windows.Visibility.Collapsed;
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

        public bool CheckHasDuplicateAlias()
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

    [Obfuscation]
    public class ColumnEntity : ViewModelBase
    {
        [Obfuscation]
        private bool isChecked;
        private string editedColumnName;
        private Visibility viewVisibility;
        private Visibility renameVisibility;

        public ColumnEntity()
        {
            viewVisibility = Visibility.Visible;
            renameVisibility = Visibility.Collapsed;
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                RaisePropertyChanged(() => IsChecked);
            }
        }

        public string ColumnName { get; set; }

        public string EditedColumnName
        {
            get { return editedColumnName; }
            set
            {
                editedColumnName = value;
                RaisePropertyChanged(() => EditedColumnName);
            }
        }

        public string ColumnType { get; set; }

        public Visibility RenameVisibility
        {
            get { return renameVisibility; }
            set
            {
                renameVisibility = value;
                RaisePropertyChanged(() => RenameVisibility);
            }
        }

        public Visibility ViewVisibility
        {
            get { return viewVisibility; }
            set
            {
                viewVisibility = value;
                RaisePropertyChanged(() => ViewVisibility);
            }
        }

        public int MaxLength { get; set; }
    }
}