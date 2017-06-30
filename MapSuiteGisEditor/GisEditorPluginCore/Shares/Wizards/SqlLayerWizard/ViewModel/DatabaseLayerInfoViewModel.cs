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
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DatabaseLayerInfoViewModel<T> : ViewModelBase where T : FeatureLayer
    {
        private bool needSavePassword;
        private bool isServerConnected;
        private DatabaseLayerInfo<T> model;
        private ObservedCommand connectToDatabaseCommand;
        private ObservedCommand comfirmCommand;
        private ObservableCollection<string> databases;
        private ObservableCollection<string> dataTables;
        private ObservableCollection<string> columns;
        //private static readonly int MaxHistoryServerNameCount = 20;
        private MsSqlTableDataRepositoryItem currentItem;

        public DatabaseLayerInfoViewModel(DatabaseLayerInfo<T> model)
        {
            this.model = model;
            this.dataTables = new ObservableCollection<string>();
            this.databases = new ObservableCollection<string>();
            this.columns = new ObservableCollection<string>();
        }

        public string Description { get { return model.Description; } set { model.Description = value; } }

        public string ServerName { get { return model.ServerName; } set { model.ServerName = value; RaisePropertyChanged(() => ServerName); } }

        public string UserName { get { return model.UserName; } set { model.UserName = value; RaisePropertyChanged(() => UserName); } }

        public string Password { get { return model.Password; } set { model.Password = value; RaisePropertyChanged(() => Password); } }

        public List<string> HistoryServerNames { get { return MsSql2008FeatureLayerPlugin.HistoryServerNames; } }

        //public string DatabaseName
        //{
        //    get { return model.DatabaseName; }
        //    set
        //    {
        //        if (model.DatabaseName != value)
        //        {
        //            model.DatabaseName = value;
        //            RaisePropertyChanged(() => DatabaseName);
        //        }
        //    }
        //}

        public MsSqlTableDataRepositoryItem CurrentItem
        {
            get { return currentItem; }
            set
            {
                currentItem = value;
                model.TableName = currentItem.Name;
                RaisePropertyChanged(() => CurrentItem);
                InitializeColumns();
            }
        }

        public string FeatureIDColumnName
        {
            get { return model.FeatureIDColumnName; }
            set
            {
                if (model.FeatureIDColumnName != value)
                {
                    model.FeatureIDColumnName = value;
                    RaisePropertyChanged(() => FeatureIDColumnName);
                }
            }
        }

        public DatabaseLayerInfo<T> Model { get { return model; } }

        public bool UseTrustAuthentication
        {
            get { return model.UseTrustAuthority; }
            set { model.UseTrustAuthority = value; RaisePropertyChanged(() => UseTrustAuthentication); }
        }

        public bool NeedSavePassword
        {
            get { return needSavePassword; }
            set
            {
                needSavePassword = value;
                RaisePropertyChanged(() => NeedSavePassword);
            }
        }

        public bool IsServerConnected
        {
            get { return isServerConnected; }
            set
            {
                isServerConnected = value;
                RaisePropertyChanged(() => IsServerConnected);
            }
        }

        public bool IsValid
        {
            get
            {
                return IsServerConnected
                    && CurrentItem != null
                    && !String.IsNullOrEmpty(FeatureIDColumnName);
            }
        }

        //public ObservableCollection<string> DataTables
        //{
        //    get { return dataTables; }
        //}

        //public ObservableCollection<string> Databases
        //{
        //    get { return databases; }
        //}

        public ObservableCollection<string> Columns
        {
            get { return columns; }
        }

        public ObservedCommand ConnectToDatabaseCommand
        {
            get
            {
                if (connectToDatabaseCommand == null)
                {
                    connectToDatabaseCommand = new ObservedCommand(() =>
                    {
                        //InitializeDatabases();
                    }, () =>
                    {
                        return (UseTrustAuthentication && !string.IsNullOrEmpty(ServerName)) ||
                            (!string.IsNullOrEmpty(ServerName) && !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password));
                    });
                }

                return connectToDatabaseCommand;
            }
        }

        public ObservedCommand ConfirmCommand
        {
            get
            {
                if (comfirmCommand == null)
                {
                    comfirmCommand = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send(true, this);
                    }, () => IsValid);
                }
                return comfirmCommand;
            }
        }

        //private void InitializeTableNames()
        //{
        //    TryExecute(() =>
        //    {
        //        DataTables.Clear();
        //        foreach (var tableName in model.CollectTablesFromDatabase())
        //        {
        //            DataTables.Add(tableName);
        //        }

        //        TableName = DataTables.FirstOrDefault();
        //    });
        //}

        private void InitializeColumns()
        {
            TryExecute(() =>
            {
                Columns.Clear();
                foreach (var columnName in model.CollectColumnsFromTable())
                {
                    Columns.Add(columnName);
                }

                FeatureIDColumnName = Columns.FirstOrDefault();
            });
        }

        //private void InitializeDatabases()
        //{
        //    IsServerConnected = false;
        //    TryExecute(() =>
        //    {
        //        Databases.Clear();
        //        foreach (var database in model.CollectDatabaseFromServer())
        //        {
        //            Databases.Add(database);
        //        }

        //        DatabaseName = Databases.FirstOrDefault();
        //        IsServerConnected = true;

        //        bool updateCachedServerNames = false;

        //        if (!HistoryServerNames.Contains(model.ServerName))
        //        {
        //            HistoryServerNames.Add(model.ServerName);
        //            updateCachedServerNames = true;
        //        }
        //        if (HistoryServerNames.Count > MaxHistoryServerNameCount)
        //        {
        //            HistoryServerNames.RemoveAt(0);
        //            updateCachedServerNames = true;
        //        }
        //        if (updateCachedServerNames)
        //        {
        //            RaisePropertyChanged(() => HistoryServerNames);
        //        }
        //    });
        //}

        private void TryExecute(Action action)
        {
            try
            {
                if (action != null) action();
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                NotificationMessage<Exception> message = new NotificationMessage<Exception>(ex, "Connect to database error.");
                MessengerInstance.Send(message, this);
            }
        }
    }
}
