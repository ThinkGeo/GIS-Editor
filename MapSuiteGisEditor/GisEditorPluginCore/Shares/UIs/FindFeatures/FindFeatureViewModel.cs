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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FindFeatureViewModel : ViewModelBase
    {
        private string findValue;
        private ObservedCommand findCommand;
        private ObservedCommand clearCommand;
        private RelayCommand<bool> selectAllCommand;
        private ObservableCollection<CheckableItemViewModel<FeatureLayer>> availableFeatureLayers;
        private Func<FeatureLayer, string> generateNameFunc;
        private ObservableCollection<FeatureLayerViewModel> featureEntities;
        private FeatureViewModel selectedEntity;
        private bool isBusy;

        public FindFeatureViewModel()
        {
            generateNameFunc = (featureLayer) => featureLayer != null ? featureLayer.Name : GisEditor.LanguageManager.GetStringResource("SelectFeaturesPluginNoSelection");

            featureEntities = new ObservableCollection<FeatureLayerViewModel>();
            availableFeatureLayers = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();
            RefreshFeatureLayers();
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        public string FindValue
        {
            get { return findValue; }
            set
            {
                findValue = value;
                RaisePropertyChanged(() => FindValue);
            }
        }

        public FeatureViewModel SelectedEntity
        {
            get { return selectedEntity; }
            set
            {
                selectedEntity = value;
                RaisePropertyChanged(() => SelectedEntity);
                RaisePropertyChanged(() => SelectedDataView);
            }
        }

        public DataView SelectedDataView
        {
            get
            {
                if (selectedEntity != null) { return selectedEntity.DefaultView; }
                return null;
            }
        }

        public ObservableCollection<FeatureLayerViewModel> FeatureEntities
        {
            get { return featureEntities; }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureLayer>> AvailableFeatureLayers
        {
            get { return availableFeatureLayers; }
        }

        public ObservedCommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new ObservedCommand(() =>
                    {
                        Clear();
                    }, () => GisEditor.ActiveMap != null && FeatureEntities.Count > 0);
                }
                return clearCommand;
            }
        }

        private void Clear()
        {
            FeatureEntities.Clear();
            SelectedEntity = null;
        }

        public ObservedCommand FindCommand
        {
            get
            {
                if (findCommand == null)
                {
                    findCommand = new ObservedCommand(() =>
                    {
                        if (!string.IsNullOrEmpty(FindValue))
                        {
                            Clear();
                            IEnumerable<FeatureLayer> selectedLayers = availableFeatureLayers
                               .Where(viewModel => viewModel.IsChecked)
                               .Select(viewModel => viewModel.Value);

                            FindFeatures(selectedLayers);
                        }
                    }, () => !string.IsNullOrEmpty(FindValue) && !IsBusy && availableFeatureLayers.Count > 0 && GisEditor.ActiveMap != null);
                }
                return findCommand;
            }
        }

        private void FindFeatures(IEnumerable<FeatureLayer> selectedLayers)
        {
            IsBusy = true;
            foreach (var layer in selectedLayers)
            {
                Task.Factory.StartNew(() =>
                {
                    Collection<Feature> features = new Collection<Feature>();
                    layer.SafeProcess(() =>
                    {
                        Collection<Feature> allFeatures = new Collection<Feature>();
                        allFeatures = layer.QueryTools.GetAllFeatures(layer.GetDistinctColumnNames());
                        var queriedFeatures = allFeatures.Where(f =>
                        {
                            bool hasValue = f.ColumnValues.Values
                            .Any(value => value.ToUpperInvariant().Contains(FindValue.ToUpperInvariant()));
                            return hasValue;
                        });

                        foreach (var item in queriedFeatures)
                        {
                            features.Add(item);
                        }
                    });

                    FeatureLayerViewModel entity = new FeatureLayerViewModel { LayerName = layer.Name };
                    foreach (var tmpFeature in features)
                    {
                        entity.FoundFeatures.Add(new FeatureViewModel(tmpFeature, layer));
                    }

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!FeatureEntities.Any(f => f.LayerName == entity.LayerName) && entity.FoundFeatures.Count > 0)
                        {
                            FeatureEntities.Add(entity);
                            if (FeatureEntities.Count > 0 && FeatureEntities[0].FoundFeatures.Count > 0)
                            {
                                selectedEntity = FeatureEntities[0].FoundFeatures[0];
                                RaisePropertyChanged(() => SelectedDataView);
                            }
                        }
                        IsBusy = false;
                    }, DispatcherPriority.ApplicationIdle);
                });
            }
        }

        public RelayCommand<bool> SelectAllCommand
        {
            get
            {
                if (selectAllCommand == null)
                {
                    selectAllCommand = new RelayCommand<bool>((isAllSelected) =>
                    {
                        foreach (var viewModel in AvailableFeatureLayers)
                        {
                            viewModel.IsChecked = isAllSelected;
                        }
                    });
                }
                return selectAllCommand;
            }
        }

        public void RefreshFeatureLayers()
        {
            availableFeatureLayers.Clear();
            if (GisEditor.ActiveMap != null)
            {
                var layers = GisEditor.ActiveMap.GetFeatureLayers(true);
                foreach (var item in layers)
                {
                    availableFeatureLayers.Add(new CheckableItemViewModel<FeatureLayer>(item, true, generateNameFunc));
                }

                var names = layers.Select(l => l.Name);

                var notExistEntites = FeatureEntities.Where(f => !names.Contains(f.LayerName)).ToList();
                foreach (var item in notExistEntites)
                {
                    FeatureEntities.Remove(item);
                }
                if (FeatureEntities.Count > 0 && FeatureEntities[0].FoundFeatures.Count > 0)
                {
                    selectedEntity = FeatureEntities[0].FoundFeatures[0];
                    RaisePropertyChanged(() => SelectedDataView);
                }
                else
                {
                    SelectedEntity = null;
                }
            }
        }
    }
}
