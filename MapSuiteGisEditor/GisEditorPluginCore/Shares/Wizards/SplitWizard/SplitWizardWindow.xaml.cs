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
    public partial class SplitWizardWindow : Window, IGeoProcessWizard
    {
        public SplitWizardWindow()
        {
            InitializeComponent();
        }

        public WizardShareObject GetShareObject()
        {
            return SplitViewModel.TargetObject;
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SplitViewModel.Cancelled += new EventHandler<CancelledWizardEventArgs<SplitWizardShareObject>>(SplitViewModel_Cancelled);
            SplitViewModel.Finished += new EventHandler<FinishedWizardEventArgs<SplitWizardShareObject>>(SplitViewModel_Finished);
        }

        private void SplitViewModel_Finished(object sender, FinishedWizardEventArgs<SplitWizardShareObject> e)
        {
            DialogResult = true;
        }

        [Obfuscation]
        private void SplitViewModel_Cancelled(object sender, CancelledWizardEventArgs<SplitWizardShareObject> e)
        {
            DialogResult = false;
        }
    }
}