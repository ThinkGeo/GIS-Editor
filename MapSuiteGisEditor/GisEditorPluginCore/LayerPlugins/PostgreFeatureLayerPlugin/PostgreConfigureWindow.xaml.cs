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
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PostgreConfigureWindow.xaml
    /// </summary>
    public partial class PostgreConfigureWindow : Window
    {
        private PostgreConfigure viewModel;

        public PostgreConfigureWindow()
        {
            InitializeComponent();

            viewModel = new PostgreConfigure();
            DataContext = viewModel;
            Messenger.Default.Register<string>(this, viewModel, msg =>
            {
                switch (msg)
                {
                    case "OK":
                        DialogResult = true;
                        break;
                }
            });
        }

        public Uri GetResultLayerUri()
        {
            string connectionString = viewModel.GetConnectionString();
            //string[] tableNameSegs = viewModel.CurrentItem.Name.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            PostgreSchemaDataRepositoryItem schemaItem = (PostgreSchemaDataRepositoryItem)viewModel.CurrentItem.Parent.Parent;
            string schemaName = schemaItem.Name;
            string tableName = viewModel.CurrentItem.Name;
            string columnName = viewModel.FeatureIdColumnName;

            string url = "postgreSqlFeatureLayer:{0}|{1}|{2}|{3}";
            url = String.Format(CultureInfo.InvariantCulture, url, tableName, schemaName, connectionString, columnName);
            return new Uri(url);
        }

        [Obfuscation]
        private void dropdownTB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropdownButton.IsChecked = true;
        }

        [Obfuscation]
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataRepositoryItem currentItem = sender.GetDataContext<DataRepositoryItem>();
            if (currentItem != null && currentItem.IsLeaf)
            {
                viewModel.CurrentItem = currentItem;
                DropdownButton.IsChecked = false;
            }
        }
    }
}