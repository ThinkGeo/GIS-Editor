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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for MergeWizardWindow.xaml
    /// </summary>
    public partial class MergeWizardWindow : Window, IGeoProcessWizard
    {
        public MergeWizardWindow()
        {
            InitializeComponent();
        }

        public WizardShareObject GetShareObject()
        {
            return mergeWizardViewModel.TargetObject;
        }

        public MergeWizardShareObject Entity { get { return mergeWizardViewModel.TargetObject; } }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mergeWizardViewModel.Finished += new EventHandler<FinishedWizardEventArgs<MergeWizardShareObject>>(MergeWizardViewModel_Finished);
            mergeWizardViewModel.Cancelled += new EventHandler<CancelledWizardEventArgs<MergeWizardShareObject>>(MergeWizardViewModel_Cancelled);
            mergeWizardViewModel.MoveNext();
        }

        private void MergeWizardViewModel_Cancelled(object sender, CancelledWizardEventArgs<MergeWizardShareObject> e)
        {
            DialogResult = false;
        }

        private void MergeWizardViewModel_Finished(object sender, FinishedWizardEventArgs<MergeWizardShareObject> e)
        {
            DialogResult = true;
        }
    }
}