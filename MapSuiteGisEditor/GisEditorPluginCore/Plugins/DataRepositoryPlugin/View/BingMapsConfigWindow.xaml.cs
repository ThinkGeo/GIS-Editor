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


using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for BingMapsConfigWindow.xaml
    /// </summary>
    public partial class BingMapsConfigWindow : Window
    {
        public BingMapsConfigWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<DialogMessage>(this, bingMapsConfigViewModel, (msg) =>
            {
                MessageBox.Show(msg.Content, msg.Caption, msg.Button, msg.Icon);
            });
            Messenger.Default.Register<bool>(this, bingMapsConfigViewModel, (msg) =>
            {
                DialogResult = msg;
            });
            Closing += (s, e) => Messenger.Default.Unregister(this);
        }

        public string BingMapsKey
        {
            get { return bingMapsConfigViewModel.BingMapsKey; }
        }

        public BingMapsMapType BingMapsStyle
        {
            get { return bingMapsConfigViewModel.MapType; }
        }

        public bool ShowMapTypeOptions
        {
            get { return bingMapsConfigViewModel.ShowMapTypeOptions; }
            set { bingMapsConfigViewModel.ShowMapTypeOptions = value; }
        }

        [Obfuscation]
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.bingmapsportal.com/");
        }
    }
}
