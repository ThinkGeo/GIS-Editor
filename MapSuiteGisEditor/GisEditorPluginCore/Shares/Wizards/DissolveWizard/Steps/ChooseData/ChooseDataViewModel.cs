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
using System.Linq;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ChooseDataViewModel : INotifyPropertyChanged
    {
        internal const string AddAllString = "Add All";
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<CheckableItemViewModel<FeatureSourceColumn>> extraColumns;
        private ObservableCollection<OperatorPair> operatorPairs;
        private CheckableItemViewModel<FeatureSourceColumn> selectedColumn;
        private DissolveOperatorMode selectedOperator;
        private ObservableCollection<DissolveOperatorMode> filteredOperatorSource;
        private FeatureLayer selectedFeatureLayer;
        private bool dissolveSelectedFeaturesOnly;

        private ObservedCommand addOperatorPairCommand;
        [NonSerialized]
        private RelayCommand<OperatorPair> removeOperatorPairCommand;
        [NonSerialized]
        private RelayCommand viewDataCommand;
        [NonSerialized]
        private RelayCommand showCommendsCommand;

        public ChooseDataViewModel(FeatureLayer featureLayer, bool selectedFeaturesOnly)
        {
            selectedFeatureLayer = featureLayer;
            dissolveSelectedFeaturesOnly = selectedFeaturesOnly;
            filteredOperatorSource = new ObservableCollection<DissolveOperatorMode>();
            extraColumns = new ObservableCollection<CheckableItemViewModel<FeatureSourceColumn>>();
            operatorPairs = new ObservableCollection<OperatorPair>();
        }

        public ObservedCommand AddOperatorPairCommand
        {
            get
            {
                if (addOperatorPairCommand == null)
                {
                    addOperatorPairCommand = new ObservedCommand(() =>
                    {
                        if (SelectedColumn.Value.ColumnName.Equals(AddAllString, StringComparison.Ordinal))
                        {
                            OperatorPairs.Clear();
                            foreach (var columnName in ExtraColumns)
                            {
                                if (columnName.Value.ColumnName.Equals(AddAllString, StringComparison.Ordinal)) continue;
                                var operatorPair = new OperatorPair(columnName.Value.ColumnName, columnName.Value.TypeName, SelectedOperator, selectedFeatureLayer);
                                operatorPair.AliasName = selectedFeatureLayer.FeatureSource.GetColumnAlias(columnName.Value.ColumnName);
                                OperatorPairs.Add(operatorPair);
                                //OperatorPairs.Add(new OperatorPair(columnName.Value.ColumnName, columnName.Value.TypeName, SelectedOperator, selectedFeatureLayer));
                            }

                            ExtraColumns.Clear();
                        }
                        else
                        {
                            var operatorPair = new OperatorPair(SelectedColumn.Value.ColumnName, SelectedColumn.Value.TypeName, SelectedOperator, selectedFeatureLayer);
                            operatorPair.AliasName = selectedFeatureLayer.FeatureSource.GetColumnAlias(SelectedColumn.Value.ColumnName);
                            OperatorPairs.Add(operatorPair);
                            //OperatorPairs.Add(new OperatorPair(SelectedColumn.Value.ColumnName, SelectedColumn.Value.TypeName, SelectedOperator, selectedFeatureLayer));
                            ExtraColumns.Remove(SelectedColumn);
                        }

                        if (OperatorPairs.Count > 0)
                        {
                            var addAllColumn = ExtraColumns.FirstOrDefault(tmpColumn => tmpColumn.Value.ColumnName.Equals(AddAllString, StringComparison.Ordinal));
                            if (addAllColumn != null) ExtraColumns.Remove(addAllColumn);
                        }

                        if (ExtraColumns.Count > 0) SelectedColumn = ExtraColumns[0];
                    }, () => ExtraColumns.Count > 0);
                }
                return addOperatorPairCommand;
            }
        }

        public RelayCommand<OperatorPair> RemoveOperatorPairCommand
        {
            get
            {
                if (removeOperatorPairCommand == null)
                {
                    removeOperatorPairCommand = new RelayCommand<OperatorPair>((parameter) =>
                    {
                        OperatorPair operatorPair = parameter;
                        if (OperatorPairs.Contains(operatorPair))
                        {
                            OperatorPairs.Remove(operatorPair);

                            // 10 means nothing
                            FeatureSourceColumn newColumn = new FeatureSourceColumn(operatorPair.ColumnName, operatorPair.ColumnType, 10);
                            ExtraColumns.Add(new CheckableItemViewModel<FeatureSourceColumn>(newColumn));
                        }

                        if (OperatorPairs.Count == 0 && ExtraColumns.FirstOrDefault(tmpColumn => tmpColumn.Value.ColumnName.Equals(AddAllString, StringComparison.Ordinal)) == null)
                        {
                            FeatureSourceColumn addAllStringColumn = new FeatureSourceColumn(ChooseDataViewModel.AddAllString);

                            //ExtraColumns.Insert(0, new CheckableModel<FeatureSourceColumn>
                            //{
                            //    Value = AddAllString,
                            //    Tag =
                            //        operatorPair.ColumnType
                            //});
                            ExtraColumns.Insert(0, new CheckableItemViewModel<FeatureSourceColumn>(addAllStringColumn));
                        }
                    });
                }
                return removeOperatorPairCommand;
            }
        }

        public RelayCommand ViewDataCommand
        {
            get
            {
                if (viewDataCommand == null)
                {
                    viewDataCommand = new RelayCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl(selectedFeatureLayer);
                        content.IsHighlightFeatureOnly = dissolveSelectedFeaturesOnly;
                        content.ShowDialog();
                    });
                }
                return viewDataCommand;
            }
        }

        public RelayCommand ShowCommendsCommand
        {
            get
            {
                if (showCommendsCommand == null)
                {
                    showCommendsCommand = new RelayCommand(() =>
                    {
                        Singleton<OperatorCommendsWindow>.Instance.Show();
                    });
                }
                return showCommendsCommand;
            }
        }

        public CheckableItemViewModel<FeatureSourceColumn> SelectedColumn
        {
            get { return selectedColumn; }
            set
            {
                selectedColumn = value;
                OnPropertyChanged("SelectedColumn");
                OnPropertyChanged("FilteredOperatorSource");
            }
        }

        public DissolveOperatorMode SelectedOperator
        {
            get { return selectedOperator; }
            set
            {
                selectedOperator = value;
                OnPropertyChanged("SelectedOperator");
            }
        }

        public ObservableCollection<DissolveOperatorMode> FilteredOperatorSource
        {
            get
            {
                if (SelectedColumn != null && SelectedColumn.Value != null)
                {
                    filteredOperatorSource.Clear();
                    filteredOperatorSource.Add(DissolveOperatorMode.First);
                    filteredOperatorSource.Add(DissolveOperatorMode.Last);
                    filteredOperatorSource.Add(DissolveOperatorMode.Count);
                    if ((!String.IsNullOrEmpty(SelectedColumn.Value.ColumnName) &&
                        (SelectedColumn.Value.TypeName.Equals("INTEGER", StringComparison.OrdinalIgnoreCase)
                        || SelectedColumn.Value.TypeName.ToString().Equals("DOUBLE", StringComparison.OrdinalIgnoreCase)))
                        || selectedFeatureLayer is CsvFeatureLayer)
                    {
                        filteredOperatorSource.Add(DissolveOperatorMode.Sum);
                        filteredOperatorSource.Add(DissolveOperatorMode.Average);
                        filteredOperatorSource.Add(DissolveOperatorMode.Min);
                        filteredOperatorSource.Add(DissolveOperatorMode.Max);
                    }
                }

                SelectedOperator = filteredOperatorSource.FirstOrDefault(o => o == SelectedOperator);
                return filteredOperatorSource;
            }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureSourceColumn>> ExtraColumns
        {
            get { return extraColumns; }
        }

        public ObservableCollection<OperatorPair> OperatorPairs
        {
            get { return operatorPairs; }
        }

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