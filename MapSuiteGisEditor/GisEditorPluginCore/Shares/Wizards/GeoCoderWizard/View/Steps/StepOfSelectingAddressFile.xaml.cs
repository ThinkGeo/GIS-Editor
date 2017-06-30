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
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for StepOfSelectingAddressFile.xaml
    /// </summary>
    public partial class StepOfSelectingAddressFile : UserControl
    {
        private GeocoderWizardSharedObject sharedObject;

        public StepOfSelectingAddressFile(GeocoderWizardSharedObject parameter)
        {
            InitializeComponent();
            sharedObject = parameter;
            DataContext = parameter;
            Messenger.Default.Register<NotificationMessageAction<string>>(this, parameter, (msg) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = msg.Notification;
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    msg.Execute(openFileDialog.FileName);
                }
            });

            Messenger.Default.Register<DialogMessage>(this, (msg) =>
            {
                System.Windows.Forms.MessageBox.Show(msg.Content, msg.Caption);
            });
        }

        [Obfuscation]
        private void ViewDataClick(object sender, RoutedEventArgs e)
        {
            DataViewer previewInputFile = new DataViewer(sharedObject.PreviewDataTable);
            previewInputFile.ShowDialog();
        }

        [Obfuscation]
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }
    }
}