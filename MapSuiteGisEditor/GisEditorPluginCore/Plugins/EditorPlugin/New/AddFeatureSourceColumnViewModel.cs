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
using System.Linq;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AddFeatureSourceColumnViewModel : ViewModelBase
    {
        [NonSerialized]
        private DbfColumnType columnType;

        [NonSerialized]
        private List<DbfColumnType> columnTypes;

        [NonSerialized]
        private string columnName;

        [NonSerialized]
        private string aliasName;

        [NonSerialized]
        private string columnLength;

        [NonSerialized]
        private List<string> columnNames;

        [NonSerialized]
        private ObservedCommand okCommand;

        public AddFeatureSourceColumnViewModel()
        {
            columnTypes = new List<DbfColumnType>();
            var result = Enum.GetValues(typeof(DbfColumnType)).Cast<DbfColumnType>().Where(c => c != DbfColumnType.Null && c != DbfColumnType.Memo);
            foreach (var item in result)
            {
                columnTypes.Add(item);
            }

            columnType = DbfColumnType.Character;
            columnName = string.Empty;
            columnLength = "10";
        }

        public DbfColumnType ColumnType
        {
            get { return columnType; }
            set
            {
                columnType = value;
                RaisePropertyChanged(()=>ColumnType);

                switch (columnType)
                {
                    case DbfColumnType.Float:
                        ColumnLength = "10";
                        break;

                    case DbfColumnType.Date:
                        ColumnLength = "8";
                        break;

                    case DbfColumnType.Numeric:
                        ColumnLength = "10";
                        break;

                    case DbfColumnType.Character:
                        ColumnLength = "10";
                        break;

                    default:
                        break;
                }
            }
        }

        public string ColumnName
        {
            get { return columnName; }
            set
            {
                columnName = value;
                AliasName = columnName;
                RaisePropertyChanged(()=>ColumnName);
            }
        }

        public string AliasName
        {
            get { return aliasName; }
            set
            {
                aliasName = value;
                RaisePropertyChanged(()=>AliasName);
            }
        }

        public string ColumnLength
        {
            get { return columnLength; }
            set
            {
                columnLength = value;
                RaisePropertyChanged(()=>ColumnLength);
            }
        }

        public List<string> ColumnNames
        {
            get { return columnNames; }
            internal set
            {
                columnNames = value;
            }
        }

        public List<DbfColumnType> ColumnTypes
        {
            get { return columnTypes; }
        }

        public ObservedCommand OKCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new ObservedCommand(() =>
                    {
                        Messenger.Default.Send(
                                !string.IsNullOrEmpty(ColumnName) &&
                                !ColumnNames.Contains(ColumnName) &&
                                !string.IsNullOrEmpty(ColumnLength), this);
                    }, () => !string.IsNullOrEmpty(ColumnName) &&
                                !ColumnNames.Contains(ColumnName) &&
                                !string.IsNullOrEmpty(ColumnLength));
                }
                return okCommand;
            }
        }
    }
}