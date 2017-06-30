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
using System.Windows.Controls;
using Microsoft.Win32;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SelectStyleFileUserControl.xaml
    /// </summary>
    public partial class SelectStyleFileUserControl : UserControl
    {
        public SelectStyleFileUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void SelectFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "Map Suite Style File|*.tgsty" };
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                ((StyleWizardSharedObject)DataContext).StyleFileFullName = ofd.FileName;
            }
        }
    }
}