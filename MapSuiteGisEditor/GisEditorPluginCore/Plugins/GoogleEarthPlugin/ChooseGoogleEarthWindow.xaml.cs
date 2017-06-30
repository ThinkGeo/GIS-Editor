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


using Microsoft.Win32;
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseGoogleEarthWindow.xaml
    /// </summary>
    public partial class ChooseGoogleEarthWindow : Window
    {
        private string googleEarthPath;

        public ChooseGoogleEarthWindow()
        {
            InitializeComponent();
        }

        public string GoogleEarthPath
        {
            get { return googleEarthPath; }
        }

        [Obfuscation]
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Program File (*.exe)|*.exe";
            dialog.Multiselect = false;
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                this.pathTB.Text = dialog.FileName;
            }
        }

        [Obfuscation]
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            googleEarthPath = pathTB.Text;
            this.DialogResult = true;
        }
    }
}
