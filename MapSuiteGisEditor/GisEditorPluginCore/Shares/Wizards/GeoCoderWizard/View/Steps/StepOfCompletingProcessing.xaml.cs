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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for StepOfShowResults.xaml
    /// </summary>
    public partial class StepOfCompletingProcessing : UserControl
    {
        public event AsyncCompletedEventHandler ProcessCompleted;

        private CancellationTokenSource tokenSource;
        private GeocoderWizardSharedObject sharedObject;

        public StepOfCompletingProcessing(GeocoderWizardSharedObject parameter)
        {
            InitializeComponent();
            sharedObject = parameter;
            DataContext = parameter;
            tokenSource = new CancellationTokenSource();
        }

        [Obfuscation]
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            sharedObject.ProgressBarVisibility = Visibility.Visible;
            sharedObject.ErrorTableVisibility = Visibility.Collapsed;
            sharedObject.CurrentValue = 0;
            tokenSource = new CancellationTokenSource();

            sharedObject.ErrorTable = null;
            sharedObject.MaxValue = sharedObject.PreviewDataTable.Rows.Count;
            Task task = Task.Factory.StartNew(new Action(() =>
            {
                if (sharedObject.ErrorTable != null)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        errorDg.ItemsSource = sharedObject.ErrorTable.DefaultView;
                        sharedObject.ProgressBarVisibility = Visibility.Collapsed;
                        sharedObject.ErrorTableVisibility = Visibility.Visible;
                    }));
                }

                if (sharedObject.ErrorTable == null && ProcessCompleted != null && !tokenSource.IsCancellationRequested)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProcessCompleted(this, new AsyncCompletedEventArgs(null, false, null));
                    }));
                }
            }), tokenSource.Token);
        }

        [Obfuscation]
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        [Obfuscation]
        private void ExportClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files(*.CSV)|*.CSV";
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                sharedObject.WriteErrorMessage(saveFileDialog.FileName);
            }
        }
    }
}