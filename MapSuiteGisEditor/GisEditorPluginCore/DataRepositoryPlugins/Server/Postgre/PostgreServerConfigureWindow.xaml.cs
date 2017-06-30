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


using System.Windows;
using GalaSoft.MvvmLight.Messaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PostgreConfigureWindow.xaml
    /// </summary>
    public partial class PostgreServerConfigureWindow : Window
    {
        private PostgreServerConfigure viewModel;
        private PostgreConfigureInfo configureInfo;

        public PostgreServerConfigureWindow()
            : this(string.Empty, 5432, string.Empty, string.Empty)
        { }

        public PostgreServerConfigureWindow(string server, int port, string userName, string password)
            : base()
        {
            InitializeComponent();

            viewModel = new PostgreServerConfigure();
            viewModel.ServerName = server;
            viewModel.PortName = port;
            viewModel.UserName = userName;
            viewModel.Password = password;

            DataContext = viewModel;
            Messenger.Default.Register<string>(this, viewModel, msg =>
            {
                switch (msg)
                {
                    case "OK":
                        DialogResult = true;
                        break;
                }
            });
        }

        public PostgreConfigureInfo Result
        {
            get
            {
                if (configureInfo == null)
                {
                    configureInfo = new PostgreConfigureInfo();
                }

                configureInfo.Server = viewModel.ServerName;
                configureInfo.Port = viewModel.PortName;
                configureInfo.UserName = viewModel.UserName;
                configureInfo.Password = viewModel.Password;
                configureInfo.DbaseNames.Clear();
                foreach (string newDbaseName in viewModel.DatabaseNames)
                {
                    configureInfo.DbaseNames.Add(newDbaseName);
                }

                return configureInfo;
            }
        }
    }
}