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
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class BuildIndexFileDialog : Window
    {
        private BuildIndexFileViewModel viewModel;

        public BuildIndexFileDialog()
        {
            InitializeComponent();
            viewModel = new BuildIndexFileViewModel();
            DataContext = viewModel;

            Messenger.Default.Register<bool>(this, viewModel, (result) =>
            {
                if (IsActive)
                {
                    DialogResult = result;
                    Close();
                }
            });
            Closing += (s, e) => Messenger.Default.Unregister(this); ;
        }

        public bool HasMultipleFiles
        {
            get { return viewModel.HasMultipleFiles; }
            set { viewModel.HasMultipleFiles = value; }
        }

        public BuildIndexFileMode BuildIndexFileMode
        {
            get { return viewModel.BuildIndexFileMode; }
        }

        public static void BuildIndexInBackgroundThread(ShapeFileFeatureLayer layer)
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (sender, arg) =>
            {
                BuildIndexFileViewModel.BuildIndex(layer);
            };

            worker.RunWorkerCompleted += (sender, arg) =>
            {
                StatusBar.GetInstance().Visibility = Visibility.Collapsed;
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("BuildIndexFileDialogBuildComletedText"));
            };

            worker.RunWorkerAsync();
            StatusBar.GetInstance().Visibility = Visibility.Visible;
        }

        //private static void ShowBusyIndicatorWindow(string filename)
        //{
        //    if (busyIndicatorWindow == null)
        //    {
        //        busyIndicatorWindow = new BusyIndicatorWindow
        //        {
        //            WindowState = Application.Current.MainWindow.WindowState,
        //            Width = Application.Current.MainWindow.Width,
        //            Height = Application.Current.MainWindow.Height,
        //        };
        //    }

        //    busyIndicatorWindow.BusyContent = string.Format(CultureInfo.InvariantCulture, "Build Index for {0}", filename);
        //    busyIndicatorWindow.ShowDialog();
        //}
    }
}
