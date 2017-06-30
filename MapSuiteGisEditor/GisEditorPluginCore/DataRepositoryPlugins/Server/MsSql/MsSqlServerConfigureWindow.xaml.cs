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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for MsSqlServerConfigureWindow.xaml
    /// </summary>
    public partial class MsSqlServerConfigureWindow : Window
    {
        private MsSql2008FeatureLayerInfo model;
        private Collection<string> databases;

        public MsSqlServerConfigureWindow()
        {
            InitializeComponent();

            databases = new Collection<string>();

            autoCompleteServerName.IsTextCompletionEnabled = true;
            autoCompleteServerName.FilterMode = AutoCompleteFilterMode.Contains;
        }

        public Collection<string> Databases
        {
            get { return databases; }
        }

        public void SetSource(MsSql2008FeatureLayerInfo featureLayerInfo)
        {
            model = featureLayerInfo;
            MsSqlServerConfigureViewModel viewModel = new MsSqlServerConfigureViewModel(featureLayerInfo);
            DataContext = viewModel;
            UpdateLayout();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DatabaseLayerInfoViewModel<MsSqlFeatureLayer> viewModel = DataContext as DatabaseLayerInfoViewModel<MsSqlFeatureLayer>;
                if (viewModel != null)
                {
                    viewModel.ConnectToDatabaseCommand.Execute(null);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TryExecute(() =>
            {
                Databases.Clear();
                foreach (var database in model.CollectDatabaseFromServer())
                {
                    Databases.Add(database);
                }
            });
        }

        private void TryExecute(Action action)
        {
            try
            {
                if (action != null) action();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void autoCompleteServerName_TextChanged(object sender, RoutedEventArgs e)
        {
            ConnectButton.IsEnabled = !string.IsNullOrEmpty(autoCompleteServerName.Text);
        }
    }
}