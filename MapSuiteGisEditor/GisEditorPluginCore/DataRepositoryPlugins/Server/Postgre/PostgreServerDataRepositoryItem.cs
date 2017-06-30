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


using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PostgreServerDataRepositoryItem : DataRepositoryItem
    {
        private string server;
        private int port;
        private string userName;
        private string password;

        public PostgreServerDataRepositoryItem()
        {
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/server.png", UriKind.RelativeOrAbsolute));

            MenuItem editServerMenuItem = new MenuItem();
            editServerMenuItem.Header = "Configure...";
            editServerMenuItem.Command = new RelayCommand(() =>
            {
                PostgreServerConfigureWindow window = new PostgreServerConfigureWindow(Server, Port, UserName, Password);
                window.Owner = Application.Current.MainWindow;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (window.ShowDialog().GetValueOrDefault())
                {
                    PostgreServerDataRepositoryPlugin.SyncToServerItem(window.Result, this);
                }
            });
            editServerMenuItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/server_edit.png", UriKind.RelativeOrAbsolute)),
                Width = 16,
                Height = 16
            };

            MenuItem deleteServerMenuItem = new MenuItem();
            deleteServerMenuItem.Header = "Remove";
            deleteServerMenuItem.Command = new RelayCommand(() =>
            {
                if (Parent != null)
                {
                    Parent.Children.Remove(this);
                }

                GisEditor.InfrastructureManager.SaveSettings(GisEditor.DataRepositoryManager.GetPlugins().OfType<PostgreServerDataRepositoryPlugin>());
            });
            deleteServerMenuItem.Command = new RelayCommand(() =>
            {
                if (Parent != null)
                {
                    Parent.Children.Remove(this);
                }

                GisEditor.InfrastructureManager.SaveSettings(GisEditor.DataRepositoryManager.GetPlugins().OfType<PostgreServerDataRepositoryPlugin>());
            });
            deleteServerMenuItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/server_delete.png", UriKind.RelativeOrAbsolute)),
                Width = 16,
                Height = 16
            };

            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(editServerMenuItem);
            ContextMenu.Items.Add(deleteServerMenuItem);
        }

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public string Server
        {
            get { return server; }
            set { server = value; }
        }

        protected override void RefreshCore()
        {
            Task.Factory.StartNew(new Action(() =>
            {
                PostgreConfigureInfo postgreInfo = new PostgreConfigureInfo();
                Collection<string> newDbNames = PostgreSqlFeatureSource.GetDatabaseNames(Server, Port, UserName, Password);
                foreach (var item in newDbNames)
                {
                    postgreInfo.DbaseNames.Add(item);
                }
                postgreInfo.Server = server;
                postgreInfo.Port = port;
                postgreInfo.UserName = userName;
                postgreInfo.Password = password;
                PostgreServerDataRepositoryPlugin.SyncToServerItem(postgreInfo, this);
            }));
        }
    }
}