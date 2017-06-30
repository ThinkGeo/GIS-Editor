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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DataJoinChooseDelimitedFileUserControl.xaml
    /// </summary>
    public partial class DataJoinChooseDelimitedFileUserControl : UserControl
    {
        public DataJoinChooseDelimitedFileUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog { Filter = "Data Join Files (*.csv;*.txt;*.dbf)|*.csv;*.txt;*.dbf" };
                if (dialog.ShowDialog().GetValueOrDefault())
                {
                    DataJoinWizardShareObject entity = DataContext as DataJoinWizardShareObject;
                    if (entity != null)
                    {
                        entity.SelectedDataFilePath = dialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {

                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
    }
}