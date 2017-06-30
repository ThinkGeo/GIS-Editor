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
using System.Windows.Controls;
using Microsoft.Win32;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseTargetPage.xaml
    /// </summary>
    public partial class ChooseTargetPage : UserControl
    {
        private OpenFileDialog openFileDialog;
        private ReprojectionShareObject model;

        public ChooseTargetPage(ReprojectionShareObject parameter)
        {
            InitializeComponent();
            model = parameter;
            DataContext = parameter;
        }

        [Obfuscation]
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "ShapeFiles(*.shp)|*.shp";
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                model.LoadSourceFiles(openFileDialog.FileNames);
            }
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                var viewModel = item.Content as ReprojectionShpFileEntity;
                if (viewModel != null) viewModel.EditCommand.Execute(viewModel);
            }
        }
    }
}