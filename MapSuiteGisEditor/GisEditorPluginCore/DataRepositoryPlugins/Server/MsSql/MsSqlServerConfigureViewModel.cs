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
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class MsSqlServerConfigureViewModel : ViewModelBase
    {
        private bool needSavePassword;
        private bool isServerConnected;
        private MsSql2008FeatureLayerInfo model;
        private ObservableCollection<string> databases;
        private ObservableCollection<string> dataTables;

        public MsSqlServerConfigureViewModel(MsSql2008FeatureLayerInfo model)
        {
            this.model = model;
            this.dataTables = new ObservableCollection<string>();
            this.databases = new ObservableCollection<string>();
        }

        public string Description { get { return model.Description; } set { model.Description = value; } }

        public string ServerName { get { return model.ServerName; } set { model.ServerName = value; RaisePropertyChanged(() => ServerName); } }

        public string UserName { get { return model.UserName; } set { model.UserName = value; RaisePropertyChanged(() => UserName); } }

        public string Password { get { return model.Password; } set { model.Password = value; RaisePropertyChanged(() => Password); } }

        public List<string> HistoryServerNames { get { return MsSql2008FeatureLayerPlugin.HistoryServerNames; } }

        public string DatabaseName
        {
            get { return model.DatabaseName; }
            set
            {
                if (model.DatabaseName != value)
                {
                    model.DatabaseName = value;
                    RaisePropertyChanged(() => DatabaseName);
                }
            }
        }

        public string TableName
        {
            get { return model.TableName; }
            set
            {
                if (model.TableName != value)
                {
                    model.TableName = value;
                    RaisePropertyChanged(() => TableName);
                }
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

        public MsSql2008FeatureLayerInfo Model { get { return model; } }

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
                    && !String.IsNullOrEmpty(DatabaseName)
                    && !String.IsNullOrEmpty(TableName)
                    && !String.IsNullOrEmpty(FeatureIDColumnName);
            }
        }

        public ObservableCollection<string> DataTables
        {
            get { return dataTables; }
        }

        public ObservableCollection<string> Databases
        {
            get { return databases; }
        }
    }
}