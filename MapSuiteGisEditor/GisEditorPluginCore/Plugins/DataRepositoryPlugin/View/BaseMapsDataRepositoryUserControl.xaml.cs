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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for BaseMapsList.xaml
    /// </summary>
    public partial class BaseMapsDataRepositoryUserControl : UserControl
    {
        public BaseMapsDataRepositoryUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void ListBoxItem_GotFocus(object sender, RoutedEventArgs e)
        {
            var currentItemViewModel = sender.GetDataContext<DataRepositoryItem>();
            DataRepositoryContentViewModel.SelectedDataRepositoryItem = currentItemViewModel;
            if (currentItemViewModel.ContextMenu != null && currentItemViewModel.ContextMenu.HasItems)
            {
                DataRepositoryContentViewModel.Current.PlaceOnMapCommand =
                    ((MenuItem)currentItemViewModel.ContextMenu.Items[0]).Command;
            }
            e.Handled = true;
        }

        [Obfuscation]
        private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
            if (dataRepositoryItem != null)
            {
                dataRepositoryItem.Load();
            }
        }
    }
}
