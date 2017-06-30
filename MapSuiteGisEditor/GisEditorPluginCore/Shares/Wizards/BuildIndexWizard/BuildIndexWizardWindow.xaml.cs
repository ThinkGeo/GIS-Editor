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


using System.Collections.Generic;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for BuildIndexWizardWindow.xaml
    /// </summary>
    public partial class BuildIndexWizardWindow : Window
    {
        public BuildIndexWizardWindow()
        {
            InitializeComponent();

            Closing += (s, e) =>
            {
                Messenger.Default.Unregister(this);
                buildIndexWizardViewModel.TargetObject.Cancel();
            };
            Messenger.Default.Register<NotificationMessageAction<IEnumerable<string>>>(this, buildIndexWizardViewModel.TargetObject, (message) =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "shapefiles(*.shp)|*.shp";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    message.Execute(openFileDialog.FileNames);
                }
            });

            buildIndexWizardViewModel.MoveNext();
            buildIndexWizardViewModel.Cancelled += (s, e) => Close();
            buildIndexWizardViewModel.Finished += new System.EventHandler<FinishedWizardEventArgs<BuildIndexViewModel>>(buildIndexWizardViewModel_Finished);
        }

        [Obfuscation]
        private void buildIndexWizardViewModel_Finished(object sender, FinishedWizardEventArgs<BuildIndexViewModel> e)
        {
            if (!buildIndexWizardViewModel.IsBatchTask)
            {
                buildIndexWizardViewModel.Execute();
            }
            else
                Close();
        }
    }
}