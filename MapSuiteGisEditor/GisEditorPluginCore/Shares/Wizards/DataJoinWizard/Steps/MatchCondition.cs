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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MatchCondition : INotifyPropertyChanged
    {
        private int number;
        private bool canAdd;
        private bool canRomove;
        private string title;
        private string layerSubTitle;
        private string delimitedSubTitle;
        [NonSerialized]
        private RelayCommand addCommand;
        [NonSerialized]
        private RelayCommand removeCommand;
        private FeatureSourceColumn selectedLayerColumn;
        private FeatureSourceColumn selectedDelimitedColumn;
        private ObservableCollection<FeatureSourceColumn> layerColumns;
        private ObservableCollection<FeatureSourceColumn> delimitedColumns;
        private ObservableCollection<MatchCondition> matchConditions;
        [NonSerialized]
        private PropertyChangedEventHandler propertyChanged;

        protected MatchCondition() { }

        public MatchCondition(int number,
            ObservableCollection<FeatureSourceColumn> layerColumns,
            ObservableCollection<FeatureSourceColumn> delimitedColumns,
            ObservableCollection<MatchCondition> matchConditions)
        {
            this.title = GisEditor.LanguageManager.GetStringResource("MatchConditionTitle") + " " + number;
            this.layerColumns = layerColumns;
            this.canAdd = true;
            this.layerSubTitle = GisEditor.LanguageManager.GetStringResource("MatchConditionSubTitle");
            this.delimitedColumns = delimitedColumns;
            this.delimitedSubTitle = GisEditor.LanguageManager.GetStringResource("MatchConditiondelimitedSubTitle");
            this.matchConditions = matchConditions;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { propertyChanged += value; }
            remove { propertyChanged -= value; }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                OnPropertyChanged("Title");
            }
        }

        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        public RelayCommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand(() =>
                    {
                        MatchConditions.Remove(this);
                        MatchConditions[MatchConditions.Count - 1].CanAdd = true;
                        MatchConditions[MatchConditions.Count - 1].CanRomove = true;
                        MatchConditions[0].CanRomove = false;
                        for (int i = 0; i < MatchConditions.Count; i++)
                        {
                            MatchConditions[i].Title = GisEditor.LanguageManager.GetStringResource("MatchConditionTitle") + " " + (i + 1).ToString();
                        }
                    });
                }
                return removeCommand;
            }
        }

        public RelayCommand AddCommand
        {
            get
            {
                if (addCommand == null)
                {
                    addCommand = new RelayCommand(() =>
                    {
                        MatchConditions.Add(new MatchCondition(MatchConditions.Count + 1, LayerColumns, DelimitedColumns, MatchConditions));
                        MatchConditions[MatchConditions.Count - 2].CanAdd = false;
                        MatchConditions[MatchConditions.Count - 1].CanRomove = true;
                        if (MatchConditions.Count == 3)
                        {
                            MatchConditions[MatchConditions.Count - 1].CanAdd = false;
                        }
                        MatchConditions[0].CanRomove = false;

                        MatchConditions[MatchConditions.Count - 1].SelectedDelimitedColumn = MatchConditions[MatchConditions.Count - 1].DelimitedColumns[0];
                        MatchConditions[MatchConditions.Count - 1].SelectedLayerColumn = MatchConditions[MatchConditions.Count - 1].LayerColumns[0];
                    });
                }
                return addCommand;
            }
        }

        public ObservableCollection<MatchCondition> MatchConditions
        {
            get { return matchConditions; }
            set { matchConditions = value; }
        }

        public FeatureSourceColumn SelectedDelimitedColumn
        {
            get { return selectedDelimitedColumn; }
            set
            {
                selectedDelimitedColumn = value;
                OnPropertyChanged("SelectedDelimitedColumn");
            }
        }

        public FeatureSourceColumn SelectedLayerColumn
        {
            get { return selectedLayerColumn; }
            set
            {
                selectedLayerColumn = value;
                OnPropertyChanged("SelectedLayerColumn");
            }
        }

        public bool CanRomove
        {
            get { return canRomove; }
            set
            {
                canRomove = value;
                OnPropertyChanged("CanRomove");
            }
        }

        public bool CanAdd
        {
            get { return canAdd; }
            set
            {
                canAdd = value;
                OnPropertyChanged("CanAdd");
            }
        }

        public ObservableCollection<FeatureSourceColumn> DelimitedColumns
        {
            get { return delimitedColumns; }
        }

        public ObservableCollection<FeatureSourceColumn> LayerColumns
        {
            get { return layerColumns; }
        }

        public string DelimitedSubTilte
        {
            get { return delimitedSubTitle; }
        }

        public string LayerSubTitle
        {
            get { return layerSubTitle; }
        }

        private void OnPropertyChanged(string propName)
        {
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
