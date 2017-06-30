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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PostgreConfigure : ViewModelBase
    {
        private int portName;
        private bool loginSuccess;
        private string serverName;

        //private string tableName;
        private string featureIdColumnName;

        private string userName;
        private string password;

        //private string databaseName;
        //private ObservableCollection<string> databaseNames;
        //private ObservableCollection<string> tableNames;
        private ObservableCollection<string> columnNames;

        private RelayCommand connectCommand;
        private RelayCommand confirmCommand;
        private ObservableCollection<DataRepositoryItem> children;
        private DataRepositoryItem currentItem;

        public PostgreConfigure()
        {
            portName = 5432;
            //tableNames = new ObservableCollection<string>();
            columnNames = new ObservableCollection<string>();
            //databaseNames = new ObservableCollection<string>();
            children = new ObservableCollection<DataRepositoryItem>();

            //serverName = "192.168.0.213";
            //userName = "postgres";
            //password = "thinkgeo";
        }

        public DataRepositoryItem CurrentItem
        {
            get { return currentItem; }
            set
            {
                currentItem = value;
                RaisePropertyChanged(() => CurrentItem);

                try
                {
                    string connectionString = GetConnectionString();
                    string tempTableName = CurrentItem.Name;
                    if (tempTableName.Contains(":")) tempTableName = tempTableName.Substring(tempTableName.IndexOf(":", StringComparison.Ordinal) + 1);

                    PostgreSchemaDataRepositoryItem schemaItem = (PostgreSchemaDataRepositoryItem)currentItem.Parent.Parent;
                    PostgreSqlFeatureSource featureSource = new PostgreSqlFeatureSource(connectionString, tempTableName, "oid");
                    featureSource.SchemaName = schemaItem.SchemaName;
                    featureSource.Open();
                    List<string> newColumnNames = featureSource.GetColumns().Select(c => c.ColumnName).ToList();
                    featureSource.Close();

                    columnNames.Clear();
                    foreach (var item in newColumnNames)
                    {
                        columnNames.Add(item);
                    }

                    if (columnNames.Contains("oid", StringComparer.OrdinalIgnoreCase))
                    {
                        FeatureIdColumnName = columnNames.FirstOrDefault(c => c.Equals("oid", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        FeatureIdColumnName = columnNames.FirstOrDefault();
                    }

                    ConfirmCommand.RaiseCanExecuteChanged();
                }
                catch { }
            }
        }

        public ObservableCollection<DataRepositoryItem> Children
        {
            get { return children; }
        }

        //public ObservableCollection<string> DatabaseNames
        //{
        //    get { return databaseNames; }
        //}

        //public ObservableCollection<string> TableNames
        //{
        //    get { return tableNames; }
        //}

        public ObservableCollection<string> ColumnNames
        {
            get { return columnNames; }
        }

        public string ServerName
        {
            get { return serverName; }
            set
            {
                serverName = value;
                RaisePropertyChanged(() => ServerName);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        public int PortName
        {
            get { return portName; }
            set
            {
                portName = value;
                RaisePropertyChanged(() => PortName);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        public string FeatureIdColumnName
        {
            get { return featureIdColumnName; }
            set
            {
                featureIdColumnName = value;
                RaisePropertyChanged(() => FeatureIdColumnName);
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }

        //public string TableName
        //{
        //    get { return tableName; }
        //    set
        //    {
        //        tableName = value;
        //        RaisePropertyChanged(() => TableName);
        //        string connectionString = GetConnectionString();

        //        try
        //        {
        //            string tempTableName = tableName;
        //            if (tempTableName.Contains(":")) tempTableName = tempTableName.Substring(tempTableName.IndexOf(":") + 1);

        //            PostgreSqlFeatureSource featureSource = new PostgreSqlFeatureSource(connectionString, tempTableName, "oid");
        //            featureSource.Open();
        //            List<string> newColumnNames = featureSource.GetColumns().Select(c => c.ColumnName).ToList();
        //            featureSource.Close();

        //            columnNames.Clear();
        //            foreach (var item in newColumnNames)
        //            {
        //                columnNames.Add(item);
        //            }

        //            if (columnNames.Contains("oid", StringComparer.OrdinalIgnoreCase))
        //            {
        //                FeatureIdColumnName = columnNames.FirstOrDefault(c => c.Equals("oid", StringComparison.OrdinalIgnoreCase));
        //            }
        //            else
        //            {
        //                FeatureIdColumnName = columnNames.FirstOrDefault();
        //            }

        //            ConfirmCommand.RaiseCanExecuteChanged();
        //        }
        //        catch { }
        //    }
        //}

        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                RaisePropertyChanged(() => UserName);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                RaisePropertyChanged(() => Password);
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        //public string DatabaseName
        //{
        //    get { return databaseName; }
        //    set
        //    {
        //        databaseName = value;
        //        RaisePropertyChanged(() => DatabaseName);

        //        tableNames.Clear();
        //        string connectionString = GetConnectionString();
        //        try
        //        {
        //            Collection<string> newTbNames = PostgreSqlFeatureSource.GetTableNames(connectionString);
        //            foreach (var item in newTbNames)
        //            {
        //                tableNames.Add(item);
        //            }

        //            TableName = tableNames.FirstOrDefault();
        //            ConfirmCommand.RaiseCanExecuteChanged();
        //        }
        //        catch
        //        { }
        //    }
        //}

        public bool LoginSuccess
        {
            get { return loginSuccess; }
            set
            {
                loginSuccess = value;
                RaisePropertyChanged(() => LoginSuccess);
            }
        }

        public RelayCommand ConnectCommand
        {
            get
            {
                return connectCommand ?? (connectCommand = new RelayCommand(() =>
                {
                    try
                    {
                        LoginSuccess = false;

                        PostgreServerDataRepositoryItem serverItem = new PostgreServerDataRepositoryItem();
                        Collection<string> newDbNames = PostgreSqlFeatureSource.GetDatabaseNames(serverName, portName,
                            userName, password);
                        PostgreConfigureInfo info = new PostgreConfigureInfo();
                        info.Password = Password;
                        info.Server = ServerName;
                        info.UserName = UserName;
                        info.Port = PortName;
                        foreach (var item in newDbNames)
                        {
                            info.DbaseNames.Add(item);
                        }
                        PostgreServerDataRepositoryPlugin.SyncToServerItem(info, serverItem);
                        Children.Clear();
                        Children.Add(serverItem);
                        if (Children.Count > 0)
                        {
                            Children[0].IsSelected = true;
                        }

                        //databaseNames.Clear();
                        //foreach (var item in newDbNames)
                        //{
                        //    databaseNames.Add(item);
                        //}

                        //DatabaseName = DatabaseNames.FirstOrDefault();
                        LoginSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Connect failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, () => !string.IsNullOrEmpty(serverName)
                         && !string.IsNullOrEmpty(userName)
                         && !string.IsNullOrEmpty(password)));
            }
        }

        public RelayCommand ConfirmCommand
        {
            get
            {
                return confirmCommand ??
                       (confirmCommand = new RelayCommand(() => MessengerInstance.Send("OK", this), () => CurrentItem != null && !string.IsNullOrEmpty(FeatureIdColumnName)));
            }
        }

        public string GetConnectionString()
        {
            DataRepositoryItem databaseItem = currentItem.Parent.Parent.Parent.Parent;
            return String.Format(CultureInfo.InvariantCulture, "Server={0};Database={1};Port={2};User Id={3};Password={4};", serverName, databaseItem.Name, portName, userName, password);
        }
    }
}