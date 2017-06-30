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


using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ConnectToDatabaseUserControl.xaml
    /// </summary>
    public partial class DatabaseLayerInfoUserControl : UserControl
    {
        public DatabaseLayerInfoUserControl()
        {
            InitializeComponent();

            autoCompleteServerName.IsTextCompletionEnabled = true;
            autoCompleteServerName.FilterMode = AutoCompleteFilterMode.Contains;
        }

        [Obfuscation]
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MsSqlTableDataRepositoryItem currentItem = sender.GetDataContext<MsSqlTableDataRepositoryItem>();
            if (currentItem != null && currentItem.IsLeaf)
            {
                DataRepositoryItem databaseItem = currentItem.Parent.Parent.Parent.Parent as DataRepositoryItem;
                DatabaseLayerInfoViewModel<MsSqlFeatureLayer> viewModel = DataContext as DatabaseLayerInfoViewModel<MsSqlFeatureLayer>;
                viewModel.Model.DatabaseName = databaseItem.Name;
                viewModel.CurrentItem = currentItem;
                DropdownButton.IsChecked = false;
            }
        }

        [Obfuscation]
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

        [Obfuscation]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DatabaseLayerInfoViewModel<MsSqlFeatureLayer> viewModel = DataContext as DatabaseLayerInfoViewModel<MsSqlFeatureLayer>;
            MsSql2008FeatureLayerInfo info = new MsSql2008FeatureLayerInfo();
            info.Password = viewModel.Password;
            info.ServerName = viewModel.ServerName;
            info.UserName = viewModel.UserName;
            info.UseTrustAuthority = viewModel.UseTrustAuthentication;
            viewModel.IsServerConnected = true;
            DataRepositoryTree.DataContext = new DatabaseTreeViewModel(info);
        }

        [Obfuscation]
        private void dropdownTB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropdownButton.IsChecked = true;
        }
    }
}