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
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for OtherProjection.xaml
    /// </summary>
    [Obfuscation]
    internal partial class OtherProjection : UserControl
    {
        private OtherProjectionViewModel viewModel;

        public OtherProjection()
        {
            InitializeComponent();
            viewModel = new OtherProjectionViewModel();
            DataContext = viewModel;
        }

        [Obfuscation]
        private void LoadFromFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Projection File(*.prj)|*.prj";
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                viewModel.SelectedProj4Model = new Proj4Model();
                string wktString = File.ReadAllText(openFileDialog.FileName);
                if (!string.IsNullOrEmpty(wktString))
                {
                    try
                    {
                        viewModel.SelectedProj4Model.Proj4Parameter = Proj4Projection.ConvertPrjToProj4(wktString);
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    if (string.IsNullOrEmpty(viewModel.SelectedProj4Model.Proj4Parameter))
                    {
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("OtherProjectionNotSupportedText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                    var hostWindow = Window.GetWindow(this);
                    if (hostWindow != null)
                    {
                        var projectionSelectionViewModel = hostWindow.DataContext as ProjectionSelectionViewModel;
                        if (projectionSelectionViewModel != null)
                        {
                            projectionSelectionViewModel.SelectedViewModel = viewModel;
                        }
                    }
                }
            }
        }
    }
}