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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PostgreServerConfigure : ViewModelBase
    {
        private int portName;
        private bool loginSuccess;
        private string serverName;
        private string userName;
        private string password;
        private Collection<string> databaseNames;
        private RelayCommand confirmCommand;

        public PostgreServerConfigure()
        {
            portName = 5432;
            databaseNames = new Collection<string>();
        }

        public string ServerName
        {
            get { return serverName; }
            set
            {
                serverName = value;
                RaisePropertyChanged(() => ServerName);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        public int PortName
        {
            get { return portName; }
            set
            {
                portName = value;
                RaisePropertyChanged(() => PortName);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                RaisePropertyChanged(() => UserName);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                RaisePropertyChanged(() => Password);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        public bool LoginSuccess
        {
            get { return loginSuccess; }
            set
            {
                loginSuccess = value;
                RaisePropertyChanged(() => LoginSuccess);
            }
        }

        public Collection<string> DatabaseNames
        {
            get { return databaseNames; }
        }

        public RelayCommand ConfirmCommand
        {
            get
            {
                return confirmCommand ?? (confirmCommand = new RelayCommand(() =>
                {
                    try
                    {
                        Collection<string> newDbNames = PostgreSqlFeatureSource.GetDatabaseNames(serverName, portName,
                            userName, password);
                        databaseNames.Clear();

                        foreach (var item in newDbNames)
                        {
                            databaseNames.Add(item);
                        }

                        MessengerInstance.Send("OK", this);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, () => !string.IsNullOrEmpty(serverName)
                         && !string.IsNullOrEmpty(UserName)
                         && !string.IsNullOrEmpty(Password)));
            }
        }
    }
}