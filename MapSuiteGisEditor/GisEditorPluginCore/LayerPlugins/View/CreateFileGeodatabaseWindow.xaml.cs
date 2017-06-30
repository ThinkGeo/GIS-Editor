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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for CreateFileGeodatabaseWindow.xaml
    /// </summary>
    public partial class CreateFileGeodatabaseWindow : OutputWindow
    {
        private const string tableNamePattern = @"^[a-zA-Z_]\w*$";
        private OutputUserControlViewModel viewModel;

        private string tableName;
        private string featureIdColumn;

        public CreateFileGeodatabaseWindow()
        {
            InitializeComponent();

            string tempTableName = "TableName";
            if (GisEditor.ActiveMap.ActiveLayer != null)
            {
                tempTableName = GisEditor.ActiveMap.ActiveLayer.Name;
            }
            tableNameTb.Text = tempTableName;
            featureIdColumn = "OBJECTID";

            viewModel = new OutputUserControlViewModel();
            outputUserControl.DataContext = viewModel;
            viewModel.PropertyChanged += viewModel_PropertyChanged;
        }

        public string TableName
        {
            get { return tableName; }
        }

        public string FeatureIdColumn
        {
            get { return featureIdColumn; }
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

        private void SetValues()
        {
            tableName = tableNameTb.Text.Trim();
            CustomData.Add("TableName", TableName);
            CustomData.Add("ObjectId", FeatureIdColumn);
            //Tag = string.Format(CultureInfo.InvariantCulture, "{0}|{1}", TableName, FeatureIdColumn);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                    SetValues();
                    DialogResult = true;
                }
            }
            catch
            {
                var result = MessageBox.Show("The path you entered is not valid, please re-enter.", "Info");
            }
        }

        private void tableNameTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtWarning.Visibility = Visibility.Collapsed;
            bool isMatch = Regex.IsMatch(tableNameTb.Text, tableNamePattern);
            if (!isMatch)
            {
                txtWarning.Visibility = Visibility.Visible;
                OkButton.IsEnabled = false;
            }
            else
            {
                OkButton.IsEnabled = GetOkButtonEnabled();
            }
        }

        private void viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (viewModel.TempFileName != null)
            {
                if (viewModel.OutputMode == OutputMode.ToFile)
                {
                    LayerUri = null;
                    if (!string.IsNullOrEmpty(viewModel.OutputPathFileName))
                    {
                        try
                        {
                            LayerUri = new Uri(viewModel.OutputPathFileName);
                        }
                        catch
                        { }
                    }
                }
                else
                {
                    try
                    {
                        LayerUri = new Uri(Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), Path.ChangeExtension(viewModel.TempFileName, Extension)));
                    }
                    catch
                    { }
                }
            }
            if (LayerUri != null && !string.IsNullOrEmpty(LayerUri.LocalPath))
            {
                geodatabaseTb.Text = Path.GetFileName(LayerUri.LocalPath);
            }

            OkButton.IsEnabled = GetOkButtonEnabled();
        }

        private bool GetOkButtonEnabled()
        {
            bool result = !string.IsNullOrEmpty(tableNameTb.Text) && !string.IsNullOrEmpty(geodatabaseTb.Text);

            if (result)
            {
                switch (viewModel.OutputMode)
                {
                    case OutputMode.ToTemporary:
                        result = !string.IsNullOrEmpty(viewModel.TempFileName);
                        break;
                    case OutputMode.ToFile:
                        result = !string.IsNullOrEmpty(viewModel.OutputPathFileName);
                        break;
                    default:
                        result = false;
                        break;
                }
            }

            return result;
        }

        [Obfuscation]
        private void OutputWindow_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.OutputMode = OutputMode;
        }

        [Obfuscation]
        private void ShowOptionsClick(object sender, RoutedEventArgs e)
        {
            if (ProjectionGrid.Visibility == Visibility.Visible)
            {
                ProjectionGrid.Visibility = Visibility.Collapsed;
                ((Button)sender).Template = FindResource("ShowOptionsTemplte") as ControlTemplate;
            }
            else
            {
                ProjectionGrid.Visibility = Visibility.Visible;
                ((Button)sender).Template = FindResource("HideOptionsTemplte") as ControlTemplate;
            }
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = new SolidColorBrush(Color.FromRgb(120, 174, 229));
            ((Border)sender).Background = new SolidColorBrush(Color.FromRgb(209, 226, 242));
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = null;
            ((Border)sender).Background = null;
        }

        [Obfuscation]
        private void ChooseProjectionClick(object sender, RoutedEventArgs e)
        {
            ProjectionWindow projectionWindow = new ProjectionWindow(GisEditor.ActiveMap.DisplayProjectionParameters, "Choose the projection you want to save", "");
            if (projectionWindow.ShowDialog().GetValueOrDefault())
            {
                string selectedProj4Parameter = projectionWindow.Proj4ProjectionParameters;
                if (!string.IsNullOrEmpty(selectedProj4Parameter))
                {
                    Proj4ProjectionParametersStringCore = selectedProj4Parameter;
                }
            }
        }
    }
}