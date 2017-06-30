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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for StatusBar.xaml
    /// </summary>
    public partial class StatusBar : UserControl
    {
        private static StatusBar instance;

        public event EventHandler<CancelEventArgs> Cancelled;

        private StatusBar()
        {
            InitializeComponent();
        }

        public static StatusBar GetInstance()
        {
            if (instance == null)
            {
                instance = new StatusBar() { Visibility = Visibility.Collapsed };
            }
            return instance;
        }

        public ProgressBar CurrentProgressBar { get { return ProgressBar1; } }

        protected void OnCancelled(CancelEventArgs e)
        {
            EventHandler<CancelEventArgs> handler = Cancelled;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        [Obfuscation]
        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnCancelled(new CancelEventArgs(true));
            Visibility = Visibility.Collapsed;
        }
    }
}
