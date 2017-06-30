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


using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for AddToDataRepositoryWindow.xaml
    /// </summary>
    public partial class AddToDataRepositoryWindow : Window
    {
        public AddToDataRepositoryWindow()
        {
            InitializeComponent();
            Closing += AddToDataRepositoryWindow_Closing;
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("DataRepositoryHelp", HelpButtonMode.NormalButton);
        }

        private void AddToDataRepositoryWindow_Closing(object sender, CancelEventArgs e)
        {
            var contentPlugin = GisEditor.UIManager.GetActiveUIPlugins<ContentUIPlugin>().FirstOrDefault();
            if (contentPlugin != null)
            {
                GisEditor.InfrastructureManager.SaveSettings(contentPlugin);
            }
        }

        [Obfuscation]
        private void YesClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void NoClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        [Obfuscation]
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            Singleton<ContentSetting>.Instance.IsShowAddDataRepositoryDialog = !cb.IsChecked.GetValueOrDefault();
        }
    }
}