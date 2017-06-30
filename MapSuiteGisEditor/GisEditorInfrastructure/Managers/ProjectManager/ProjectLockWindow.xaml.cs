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
    /// Interaction logic for ProjectLockWindow.xaml
    /// </summary>
    internal partial class ProjectLockWindow : Window
    {
        private bool isConfirmWindow;
        private string openPassword;
        private string savePassword;

        public ProjectLockWindow(string description, bool isConfirmWindow, string openPassword, string savePassword)
        {
            InitializeComponent();

            descriptionTB.Text = description;
            this.isConfirmWindow = isConfirmWindow;
            this.openPassword = openPassword;
            this.savePassword = savePassword;

            //okBtn.IsEnabled = false;
            //if (!string.IsNullOrEmpty(passwordBox.Password)
            //    && !string.IsNullOrEmpty(passwordBox2.Password))
            //{
            //    okBtn.IsEnabled = true;
            //}
        }

        [Obfuscation]
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Visibility == Visibility.Collapsed && passwordBox2.Visibility == Visibility.Collapsed)
            {
                passwordBox.Password = tb1.Text;
                passwordBox2.Password = tb2.Text;

                DialogResult = true;
            }
            //else if (string.IsNullOrEmpty(passwordBox.Password) && string.IsNullOrEmpty(passwordBox2.Password))
            //{
            //    DialogResult = false;
            //}
            else if (isConfirmWindow)
            {
                if (openPassword == passwordBox.Password && savePassword == passwordBox2.Password)
                {
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("The password doesn't match.");
                }
            }
            else
            {
                ProjectLockWindow comfirmWindow = new ProjectLockWindow("Reenter password to proceed.", true, passwordBox.Password, passwordBox2.Password);
                comfirmWindow.Title = "Comfirm Password";
                comfirmWindow.Owner = this;
                if (comfirmWindow.ShowDialog().GetValueOrDefault())
                {
                    DialogResult = true;
                }
            }
        }

        //[Obfuscation]
        //private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    okBtn.IsEnabled = GetOkButtonEnabledByPassword();
        //}

        //[Obfuscation]
        //private void passwordBox2_PasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    okBtn.IsEnabled = GetOkButtonEnabledByPassword();
        //}

        //[Obfuscation]
        //private void tb2_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    okBtn.IsEnabled = GetOkButtonEnabledByText();
        //}

        //[Obfuscation]
        //private void tb1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    okBtn.IsEnabled = GetOkButtonEnabledByText();
        //}

        [Obfuscation]
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            tb1.Text = passwordBox.Password;
            tb2.Text = passwordBox2.Password;
            passwordBox.Visibility = Visibility.Collapsed;
            passwordBox2.Visibility = Visibility.Collapsed;
        }

        [Obfuscation]
        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            passwordBox.Password = tb1.Text;
            passwordBox2.Password = tb2.Text;
            passwordBox.Visibility = Visibility.Visible;
            passwordBox2.Visibility = Visibility.Visible;
        }

        //private bool GetOkButtonEnabledByText()
        //{
        //    bool result = false;
        //    if (!string.IsNullOrEmpty(tb1.Text) || !string.IsNullOrEmpty(tb2.Text))
        //    {
        //        result = true;
        //    }
        //    return result;
        //}

        //private bool GetOkButtonEnabledByPassword()
        //{
        //    bool result = false;
        //    if (!string.IsNullOrEmpty(passwordBox.Password) || !string.IsNullOrEmpty(passwordBox2.Password))
        //    {
        //        result = true;
        //    }
        //    return result;
        //}
    }
}
