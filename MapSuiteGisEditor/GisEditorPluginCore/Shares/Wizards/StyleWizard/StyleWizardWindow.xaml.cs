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
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for StyleWizard.xaml
    /// </summary>
    public partial class GisEditorStyleWizardWindow : StyleWizardWindow
    {
        public GisEditorStyleWizardWindow()
        {
            InitializeComponent();
            Closing += StyleWizardWindow_Closing;
        }

        [Obfuscation]
        private void Cancelled(object sender, CancelledWizardEventArgs<StyleWizardSharedObject> e)
        {
            DialogResult = false;
            Close();
        }

        [Obfuscation]
        private void Finished(object sender, FinishedWizardEventArgs<StyleWizardSharedObject> e)
        {
            this.StyleWizardResult.StylePlugin = viewModel.TargetObject.ResultStylePlugin;
            DialogResult = true;
            Close();
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.TargetObject.TargetStyleCategories = StyleCategories;
            viewModel.Finished += Finished;
            viewModel.Cancelled += Cancelled;
            viewModel.MoveNext();
        }

        private void StyleWizardWindow_Closing(object sender, CancelEventArgs e)
        {
            if (GisEditor.StyleManager.UseWizard != viewModel.TargetObject.IsAlwaysShow)
            {
                GisEditor.StyleManager.UseWizard = viewModel.TargetObject.IsAlwaysShow;
                GisEditor.InfrastructureManager.SaveSettings(GisEditor.StyleManager);
            }
        }
    }
}