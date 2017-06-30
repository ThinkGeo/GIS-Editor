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
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DissolveWizardChooseColumnViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private FeatureLayer selectedFeatureLayer;
        private ObservableCollection<CheckableItemViewModel<string>> matchColumns;
        private bool dissolveSelectedFeatures;

        [NonSerialized]
        private RelayCommand viewDataCommand;

        public DissolveWizardChooseColumnViewModel(FeatureLayer featureLayer, bool dissolveSelectedFeatures)
        {
            this.dissolveSelectedFeatures = dissolveSelectedFeatures;
            this.matchColumns = new ObservableCollection<CheckableItemViewModel<string>>();
            this.SelectedFeatureLayer = featureLayer;
        }

        public ObservableCollection<CheckableItemViewModel<string>> MatchColumns { get { return matchColumns; } }

        public RelayCommand ViewDataCommand
        {
            get
            {
                if (viewDataCommand == null)
                {
                    viewDataCommand = new RelayCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl(SelectedFeatureLayer);
                        content.IsHighlightFeatureOnly = dissolveSelectedFeatures;
                        content.ShowDialog();
                    });
                }
                return viewDataCommand;
            }
        }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set
            {
                selectedFeatureLayer = value;
                if (selectedFeatureLayer != null)
                {
                    matchColumns.Clear();
                    if (!selectedFeatureLayer.IsOpen) selectedFeatureLayer.Open();
                    foreach (var column in selectedFeatureLayer.QueryTools.GetColumns())
                    {
                        var columnItem = new CheckableItemViewModel<string>(column.ColumnName);
                        columnItem.AliasName = selectedFeatureLayer.FeatureSource.GetColumnAlias(column.ColumnName);
                        matchColumns.Add(columnItem);
                    }
                }
                OnPropertyChanged("SelectedFeatureLayer");
            }
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