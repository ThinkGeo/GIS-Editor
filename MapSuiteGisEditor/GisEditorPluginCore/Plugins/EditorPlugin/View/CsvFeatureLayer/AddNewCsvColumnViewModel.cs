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


using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class AddNewCsvColumnViewModel : ViewModelBase
    {
        private string originalColumnName;
        private string columnName;
        private string aliasName;
        private Collection<CsvColumnType> csvColumnTypes;
        private CsvColumnType selectedCsvColumnType;
        private FeatureSourceColumnChangedStatus changedStatus;

        public AddNewCsvColumnViewModel(Collection<CsvColumnType> columnTypes)
        {
            this.csvColumnTypes = new Collection<CsvColumnType>();
            //MappingType = CsvMappingType.LongitudeAndLatitude;
            if (columnTypes.Count == 0)
            {
                csvColumnTypes.Add(CsvColumnType.String);
                csvColumnTypes.Add(CsvColumnType.Longitude);
                csvColumnTypes.Add(CsvColumnType.Latitude);
                csvColumnTypes.Add(CsvColumnType.WKT);
            }
            else
            {
                foreach (var item in columnTypes)
                {
                    csvColumnTypes.Add(item);
                }
            }
        }
        
        public string OriginalColumnName
        {
            get { return originalColumnName; }
            set { originalColumnName = value; }
        }

        public string ColumnName
        {
            get { return columnName; }
            set
            {
                columnName = value;
                RaisePropertyChanged(()=>ColumnName);
                AliasName = value;
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

        public Collection<CsvColumnType> CsvColumnTypes
        {
            get { return csvColumnTypes; }
        }

        public CsvColumnType SelectedCsvColumnType
        {
            get { return selectedCsvColumnType; }
            set
            {
                selectedCsvColumnType = value;
                RaisePropertyChanged(()=>SelectedCsvColumnType);
            }
        }

        public FeatureSourceColumnChangedStatus ChangedStatus
        {
            get { return changedStatus; }
            set
            {
                changedStatus = value;
                RaisePropertyChanged(()=>ChangedStatus);
            }
        }

        public FeatureSourceColumn ToFeatureSourceColumn()
        {
            return new FeatureSourceColumn(columnName, "string", 0);
        }
    }
}
