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
using System.Reflection;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class QueryConditionViewModel : ViewModelBase
    {
        private static ObservableCollection<QueryOperater> queryOperators;

        private FeatureLayer layer;
        private string columnName;
        private string matchValue;
        private QueryOperater queryOperator;
        private List<FeatureLayer> featureLayers;
        private ObservedCommand confirmCommand;
        private ObservedCommand cancelCommand;
        private KeyValuePair<string, string> selectedColumnName;

        public QueryConditionViewModel()
        {
            matchValue = string.Empty;
        }

        public ObservableCollection<QueryOperater> QueryOperators
        {
            get
            {
                if (queryOperators == null)
                {
                    queryOperators = new ObservableCollection<QueryOperater>
                    {
                        new QueryOperater(QueryOperaterType.Equal),
                        new QueryOperater(QueryOperaterType.Contains),
                        new QueryOperater(QueryOperaterType.StartsWith),
                        new QueryOperater(QueryOperaterType.EndsWith),
                        new QueryOperater(QueryOperaterType.DoesNotEqual),
                        new QueryOperater(QueryOperaterType.DoesNotContain),
                        new QueryOperater(QueryOperaterType.GreaterThan),
                        new QueryOperater(QueryOperaterType.GreaterThanOrEqualTo),
                        new QueryOperater(QueryOperaterType.LessThan),
                        new QueryOperater(QueryOperaterType.LessThanOrEqualTo)
                    };

                    QueryOperator = queryOperators.FirstOrDefault(op => op.QueryOperaterType == QueryOperaterType.Contains);
                }
                return queryOperators;
            }
        }

        public List<FeatureLayer> FeatureLayers
        {
            get
            {
                if (featureLayers == null)
                {
                    featureLayers = GisEditor.ActiveMap != null ? GisEditor.ActiveMap.GetFeatureLayers(true).ToList() : new List<FeatureLayer>();
                    Layer = featureLayers.FirstOrDefault();
                }
                return featureLayers;
            }
        }

        public FeatureLayer Layer
        {
            get { return layer; }
            set
            {
                layer = value;
                RaisePropertyChanged(()=>Layer);
                RaisePropertyChanged(()=>ReadableText);
            }
        }

        public KeyValuePair<string, string> SelectedColumnName
        {
            get { return selectedColumnName; }
            set
            {
                selectedColumnName = value;
                ColumnName = value.Key;
                RaisePropertyChanged(()=>ReadableText);
            }
        }

        public string ColumnName
        {
            get { return columnName; }
            set
            {
                columnName = value;
                RaisePropertyChanged(()=>ReadableText);
            }
        }

        public string MatchValue
        {
            get { return matchValue; }
            set
            {
                matchValue = value;
                RaisePropertyChanged(()=>ReadableText);
            }
        }

        public QueryOperater QueryOperator
        {
            get { return queryOperator; }
            set
            {
                if (queryOperator != value)
                {
                    queryOperator = value;
                    RaisePropertyChanged(()=>QueryOperator);
                    RaisePropertyChanged(()=>ReadableText);
                }
            }
        }

        public string ReadableText
        {
            get
            {
                return string.Format("{0} in {1} {2} {3}", ColumnName, Layer.Name, QueryOperator.QueryOperaterName, MatchValue);
            }
        }

        public ObservedCommand ConfirmCommand
        {
            get
            {
                if (confirmCommand == null)
                {
                    confirmCommand = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send(true, this);
                    }, () => GetCanExcute());
                }
                return confirmCommand;
            }
        }

        public ObservedCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send(false, this);
                    }, () => true);
                }
                return cancelCommand;
            }
        }

        public QueryConditionViewModel CloneDeep()
        {
            var viewMode = new QueryConditionViewModel()
            {
                columnName = this.columnName,
                matchValue = this.matchValue,
                layer = this.layer,
                featureLayers = this.featureLayers,
                selectedColumnName = this.selectedColumnName
            };

            if (queryOperator != null)
            {
                viewMode.queryOperator = queryOperators.FirstOrDefault(op => op.QueryOperaterType == queryOperator.QueryOperaterType);
            }
            return viewMode;
        }

        private bool GetCanExcute()
        {
            if (QueryOperator != null)
            {
                bool isEqualOrNotEqual = QueryOperator.QueryOperaterType == QueryOperaterType.Equal || QueryOperator.QueryOperaterType == QueryOperaterType.DoesNotEqual;
                if (isEqualOrNotEqual)
                {
                    return !string.IsNullOrEmpty(ColumnName) && Layer != null;
                }
                else
                {
                    return !string.IsNullOrEmpty(ColumnName) && Layer != null && !string.IsNullOrEmpty(MatchValue);
                }
            }
            else return false;
        }
    }
}
