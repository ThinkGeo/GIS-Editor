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
using System.Collections.Specialized;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LegendManagerViewModel : ViewModelBase
    {
        [NonSerialized]
        private ObservableCollection<LegendAdornmentLayerViewModel> legends;
        [NonSerialized]
        private LegendAdornmentLayerViewModel selectedLegend;
        [NonSerialized]
        private RelayCommand addNewCommand;
        [NonSerialized]
        private ObservedCommand editCommand;
        [NonSerialized]
        private ObservedCommand deleteCommand;

        public LegendManagerViewModel()
        {
            legends = new ObservableCollection<LegendAdornmentLayerViewModel>();
            legends.CollectionChanged += Legends_CollectionChanged;
        }

        public ObservableCollection<LegendAdornmentLayerViewModel> Legends
        {
            get { return legends; }
        }

        public LegendAdornmentLayerViewModel SelectedLegend
        {
            get { return selectedLegend; }
            set
            {
                selectedLegend = value;
                RaisePropertyChanged(()=>SelectedLegend);
            }
        }

        public RelayCommand AddNewCommand
        {
            get
            {
                if (addNewCommand == null)
                {
                    addNewCommand = new RelayCommand(() =>
                    {
                        LegendAdornmentLayerViewModel legendEntity = new LegendAdornmentLayerViewModel { Parent = this };
                        LegendEditor legendEditor = new LegendEditor(legendEntity);
                        if (legendEditor.ShowDialog().GetValueOrDefault())
                        {
                            if (!Legends.Contains(legendEntity)) Legends.Add(legendEntity);
                            AdornmentRibbonGroup.RefreshLegendOverlay(Legends);
                        }
                    });
                }
                return addNewCommand;
            }
        }

        public ObservedCommand EditCommand
        {
            get
            {
                if (editCommand == null)
                {
                    editCommand = new ObservedCommand(() =>
                    {
                        var tempItem = SelectedLegend.CloneDeep();
                        LegendEditor legendEditor = new LegendEditor(SelectedLegend);
                        if (!legendEditor.ShowDialog().GetValueOrDefault())
                        {
                            var index = legends.IndexOf(SelectedLegend);
                            legends.RemoveAt(index);
                            legends.Insert(index, tempItem);
                        }
                    }, () => SelectedLegend != null);
                }
                return editCommand;
            }
        }

        public ObservedCommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new ObservedCommand(() =>
                    {
                        Legends.Remove(SelectedLegend);
                    }, () => SelectedLegend != null);
                }
                return deleteCommand;
            }
        }

        public void SetSource(IEnumerable<LegendAdornmentLayer> legendLayers) => Legends.Clear();

        private void Legends_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.NewItems.Count <= 0) return;
            foreach (LegendAdornmentLayerViewModel item in e.NewItems)
            {
                item.Parent = this;
            }
        }
    }
}
