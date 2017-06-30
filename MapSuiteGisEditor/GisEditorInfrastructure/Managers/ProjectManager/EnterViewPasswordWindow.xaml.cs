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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for EnterViewPasswordWindow.xaml
    /// </summary>
    [Obfuscation]
    internal partial class EnterViewPasswordWindow : Window
    {
        public EnterViewPasswordWindow(string description)
        {
            InitializeComponent();

            Description.Text = description;
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        [Obfuscation]
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        [Obfuscation]
        private void Image_MouseEnter_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //tb1.Text = passwordBox.Password;
            //passwordBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        [Obfuscation]
        private void Image_MouseLeave_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //passwordBox.Visibility = System.Windows.Visibility.Visible;
        }

        [Obfuscation]
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            tb1.Text = passwordBox.Password;
            passwordBox.Visibility = Visibility.Collapsed;
        }

        [Obfuscation]
        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            passwordBox.Password = tb1.Text;
            passwordBox.Visibility = Visibility.Visible;
        }
    }
}
