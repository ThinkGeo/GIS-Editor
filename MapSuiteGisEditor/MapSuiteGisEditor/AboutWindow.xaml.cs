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


using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            var editionName = GisEditor.InfrastructureManager.EditionName;
            if (!string.IsNullOrEmpty(editionName)) editionName = editionName + " - ";

            version.Text = string.Format(editionName + "{0}", GetVersionInfo());
        }

        internal static string GetVersionInfo()
        {
            return string.Format("{0}Version {1}"
                , string.Empty
                , System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location).FileVersion);
        }

        [System.Reflection.Obfuscation]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [System.Reflection.Obfuscation]
        private void Grid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }
    }
}