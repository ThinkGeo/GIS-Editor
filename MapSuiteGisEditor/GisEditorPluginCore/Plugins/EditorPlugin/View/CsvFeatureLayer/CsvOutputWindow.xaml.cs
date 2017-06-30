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
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class CsvOutputWindow : OutputWindow
    {
        private OutputUserControlViewModel viewModel;
        private DelimiterViewModel delimiterViewModel;
        private WellKnownType wellKnownType;

        public CsvOutputWindow(WellKnownType wellKnownType)
        {
            InitializeComponent();
            this.wellKnownType = wellKnownType;
            btnOK.IsEnabled = false;
            Loaded += GisEditorOutputWindow_Loaded;
            viewModel = new OutputUserControlViewModel(string.Empty, string.Empty);
            outputUserControl.DataContext = viewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            delimiterViewModel = new DelimiterViewModel(wellKnownType);
            delimiterPanel.DataContext = delimiterViewModel;
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

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            btnOK.IsEnabled = !string.IsNullOrEmpty(ViewModel.OutputMode == OutputMode.ToFile ? ViewModel.OutputPathFileName : ViewModel.TempFileName);
        }

        private void GisEditorOutputWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OutputMode = OutputMode;
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
                LayerUri = ViewModel.OutputMode == OutputMode.ToFile ? new Uri(ViewModel.OutputPathFileName) : new Uri(Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), ViewModel.TempFileName + Extension));
                string folderPath = Path.GetDirectoryName(LayerUri.LocalPath);
                if (wellKnownType != WellKnownType.Point && wellKnownType != WellKnownType.Point && delimiterViewModel.Delimiter == ",")
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("CsvOutputWindowDelimiterValidation"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
                }
                else if (!LayerUri.LocalPath.EndsWith(Extension, StringComparison.InvariantCultureIgnoreCase))
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
                    CustomData["Delimiter"] = delimiterViewModel.Delimiter;
                    DialogResult = true;
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
    }
}