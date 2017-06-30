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
using System.Windows;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GeocoderWizardWindow : Window
    {
        public GeocoderWizardWindow()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            geocoderWizardViewModel.Finished += new EventHandler<FinishedWizardEventArgs<GeocoderWizardSharedObject>>(geocoderWizardViewModel_Finished);
            geocoderWizardViewModel.Cancelled += new EventHandler<CancelledWizardEventArgs<GeocoderWizardSharedObject>>(geocoderWizardViewModel_Cancelled);
            geocoderWizardViewModel.MoveNext();
        }

        private void geocoderWizardViewModel_Cancelled(object sender, CancelledWizardEventArgs<GeocoderWizardSharedObject> e)
        {
            Close();
        }

        private void geocoderWizardViewModel_Finished(object sender, FinishedWizardEventArgs<GeocoderWizardSharedObject> e)
        {
            if (!String.IsNullOrEmpty(geocoderWizardViewModel.TargetObject.OutputFilePath))
            {
                geocoderWizardViewModel.Execute();
                DialogResult = true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeocoderWizardWindowCannotNullMessage"), "Warning");
            }
        }
    }
}