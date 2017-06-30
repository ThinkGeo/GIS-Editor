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
using System.Collections.ObjectModel;
using System.ComponentModel;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class OperatorPair : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private DissolveOperatorMode selectedOperator;
        private Collection<DissolveOperatorMode> operatorModes;
        private string columnName;
        private string columnType;
        private string aliasName;

        public OperatorPair(string columnName = "", string columnType = "", DissolveOperatorMode columnOperator = DissolveOperatorMode.First, FeatureLayer featureLayer = null)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            Operator = columnOperator;

            operatorModes = new Collection<DissolveOperatorMode>();
            operatorModes.Add(DissolveOperatorMode.First);
            operatorModes.Add(DissolveOperatorMode.Last);
            operatorModes.Add(DissolveOperatorMode.Count);

            if (ColumnType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase)
                || ColumnType.Equals("DOUBLE", StringComparison.OrdinalIgnoreCase)
                || featureLayer is CsvFeatureLayer)
            {
                operatorModes.Add(DissolveOperatorMode.Sum);
                operatorModes.Add(DissolveOperatorMode.Average);
                operatorModes.Add(DissolveOperatorMode.Min);
                operatorModes.Add(DissolveOperatorMode.Max);
            }
        }

        public DissolveOperatorMode Operator
        {
            get { return selectedOperator; }
            set
            {
                selectedOperator = value;
                OnPropertyChanged("Operator");
            }
        }

        public string AliasName
        {
            get { return aliasName; }
            set { aliasName = value; }
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

        public Collection<DissolveOperatorMode> OperatorModes { get { return operatorModes; } }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
