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
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class DatabaseLayerInfoWindow : Window
    {
        public DatabaseLayerInfoWindow()
        {
            InitializeComponent();
        }

        public void SetSource<T>(DatabaseLayerInfo<T> model) where T : FeatureLayer
        {
            Messenger.Default.Unregister(this);

            DatabaseLayerInfoViewModel<T> viewModel = new DatabaseLayerInfoViewModel<T>(model);
            DataContext = viewModel;
            UpdateLayout();

            Messenger.Default.Register<bool>(this, viewModel, msg => DialogResult = true);
            Messenger.Default.Register<NotificationMessage<Exception>>(this, viewModel, msg =>
            {
                System.Windows.Forms.MessageBox.Show(msg.Content.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            });

            Closing -= WindowClosing;
            Closing += WindowClosing;
        }

        private void WindowClosing(object sender, EventArgs e)
        {
            Messenger.Default.Unregister(this);
        }
    }
}