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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FeatureSourceColumnItem
    {
        private string id;
        private string columnName;
        private string columnType;
        private string aliasName;
        private FeatureSourceColumn featureSourceColumn;
        private string orignalColumnName;
        private FeatureSourceColumnChangedStatus changedStatus;

        public FeatureSourceColumnItem(string orignalColumnName)
        {
            this.orignalColumnName = orignalColumnName;
            id = Guid.NewGuid().ToString();
            changedStatus = FeatureSourceColumnChangedStatus.NoChanged;
        }

        // It is use to save the orignal column name when the column has been edit.
        public string OrignalColumnName
        {
            get { return orignalColumnName; }
        }

        public FeatureSourceColumnChangedStatus ChangedStatus
        {
            get { return changedStatus; }
            set { changedStatus = value; }
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public string ColumnName
        {
            get { return columnName; }
            set { columnName = value; }
        }

        public string ColumnType
        {
            get { return columnType; }
            set { columnType = value; }
        }

        public string AliasName
        {
            get { return aliasName; }
            set { aliasName = value; }
        }

        public FeatureSourceColumn FeatureSourceColumn
        {
            get { return featureSourceColumn; }
            set { featureSourceColumn = value; }
        }
    }
}