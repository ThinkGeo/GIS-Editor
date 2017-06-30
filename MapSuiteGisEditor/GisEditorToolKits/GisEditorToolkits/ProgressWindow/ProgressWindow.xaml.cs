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
using System;
using System.Threading.Tasks;
using System.Globalization;
using System.ComponentModel;

namespace ThinkGeo.MapSuite.GisEditor.Toolkits
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private bool isCanceled;

        public ProgressWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(ProgressWindow_Loaded);
            Closing += new CancelEventHandler(ProgressWindow_Closing);

            progressBar.Maximum = 100;
        }

        public string MainContent
        {
            get { return mainContentTextBlock.Text; }
            set { mainContentTextBlock.Text = value; }
        }

        public Action ProgressAction
        {
            get;
            set;
        }

        public int ProgressValue
        {
            get { return (int)progressBar.Value; }
            set
            {
                progressBar.Value = value;
                SetPercentage();
            }
        }

        public int Maximum
        {
            get { return (int)progressBar.Maximum; }
            set
            {
                progressBar.Maximum = value;
                SetPercentage();
            }
        }

        public bool IsCanceled
        {
            get { return isCanceled; }
        }

        private void SetPercentage()
        {
            if (progressBar.Maximum != 0)
            {
                percentageTextBlock.Text = string.Format(CultureInfo.InvariantCulture, "{0:N0} %", (100 * progressBar.Value / progressBar.Maximum));
            }
            else
            {
                percentageTextBlock.Text = "Pending...";
            }
        }

        private void ProgressWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isCanceled = true;
        }

        private void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (ProgressAction != null)
            {
                Task.Factory.StartNew(ProgressAction);
            }
        }
    }
}