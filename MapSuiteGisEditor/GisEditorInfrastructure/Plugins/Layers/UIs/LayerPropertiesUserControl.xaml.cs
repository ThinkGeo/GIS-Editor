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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for LayerPropertiesUserControl.xaml
    /// </summary>
    [Obfuscation]
    internal partial class LayerPropertiesUserControl : UserControl
    {
        private LayerPropertiesUserControlViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerPropertiesUserControl" /> class.
        /// </summary>
        /// <param name="layer">The layer.</param>
        public LayerPropertiesUserControl(Layer layer)
        {
            InitializeComponent();

            viewModel = new LayerPropertiesUserControlViewModel(layer);
            DataContext = viewModel;
        }

        [Obfuscation]
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }

        [Obfuscation]
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ((Window)Parent).Close();
        }
    }
}
