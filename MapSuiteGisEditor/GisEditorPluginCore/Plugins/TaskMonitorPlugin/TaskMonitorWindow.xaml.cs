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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class TaskMonitorWindow : TaskbarNotifier
    {
        private TaskMonitorViewModel viewModel;

        public TaskMonitorWindow()
        {
            InitializeComponent();
            Stream streamSource = Application.GetResourceStream(new Uri("/GisEditorPluginCore;component/Images/TrayIconforBackgroundSpatialIndexBuilder.ico", UriKind.RelativeOrAbsolute)).Stream;

            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = streamSource;
            imageSource.EndInit();

            NotifyIcon.Icon = imageSource;
            Topmost = true;
            viewModel = new TaskMonitorViewModel();
            viewModel.RunningTasks.CollectionChanged += new NotifyCollectionChangedEventHandler(RunningTasks_CollectionChanged);
            DataContext = viewModel;
        }

        private void RunningTasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (!IsVisible)
                {
                    Show();
                }

                Notify();
            }
        }

        public ObservableCollection<TaskViewModel> RunningTasks
        {
            get { return viewModel.RunningTasks; }
        }

        [Obfuscation]
        private void TaskbarNotifier_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int delayInterval = 4000;
            NotifyIcon.ShowBalloonTip(delayInterval, "MapSuite GIS Editor", "All tasks completed", BalloonTipIcon.Info);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(delayInterval);
            timer.Tick += new EventHandler((s, args) =>
            {
                NotifyIcon.Visibility = Visibility.Collapsed;
                timer.Stop();
            });
            timer.Start();
        }
    }
}