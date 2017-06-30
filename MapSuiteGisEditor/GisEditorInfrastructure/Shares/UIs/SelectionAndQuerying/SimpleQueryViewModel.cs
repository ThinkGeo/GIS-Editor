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
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class SimpleQueryViewModel : ViewModelBase
    {
        private static Dictionary<WpfMap, Collection<FeatureLayer>> checkedFeatureLayers;
        private ObservableCollection<CheckableItemViewModel<FeatureLayer>> availableFeatureLayers;
        private string addressToSearch;

        [NonSerialized]
        private RelayCommand<bool> selectAllCommand;

        private ObservedCommand findCommand;

        [NonSerialized]
        private RelayCommand cancelCommand;

        private Func<FeatureLayer, string> generateNameFunc;

        public SimpleQueryViewModel()
        {
            generateNameFunc = (featureLayer) => featureLayer != null ? featureLayer.Name : GisEditor.LanguageManager.GetStringResource("SelectFeaturesPluginNoSelection");
            checkedFeatureLayers = new Dictionary<WpfMap, Collection<FeatureLayer>>();
            availableFeatureLayers = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();
            CollectFeatureLayers();
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

        public ObservedCommand FindCommand
        {
            get
            {
                if (findCommand == null)
                {
                    findCommand = new ObservedCommand(() =>
                    {
                        var allFeatureLayersInMap = GisEditor.ActiveMap.GetFeatureLayers(true);
                        var layerCheckItems = availableFeatureLayers.ToArray();

                        //Remove the layers which don't exist in current map
                        foreach (var item in layerCheckItems)
                        {
                            if (!allFeatureLayersInMap.Contains(item.Value))
                            {
                                availableFeatureLayers.Remove(item);
                            }
                        }

                        //Add the layers which don't exist in availableFeatureLayers
                        var layers = availableFeatureLayers.Select(f => f.Value);
                        foreach (var item in allFeatureLayersInMap.Where(f => !layers.Contains(f)))
                        {
                            availableFeatureLayers.Add(new CheckableItemViewModel<FeatureLayer>(item, true, generateNameFunc));
                        }

                        IEnumerable<FeatureLayer> selectedLayers = availableFeatureLayers
                            .Where(viewModel => viewModel.IsChecked)
                            .Select(viewModel => viewModel.Value);
                        var features = FindFeaturesByKeyword(AddressToSearch, selectedLayers);
                        int count = features.Count;
                        if (count == 0)
                        {
                            if (true)
                            {
                                Console.WriteLine();
                                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GeneralErrorInfo"));
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            var bboxes = features.Select(tmpFeature =>
                            {
                                if (tmpFeature.GetWellKnownType() == WellKnownType.Point)
                                {
                                    PointShape point = (PointShape)tmpFeature.GetShape();
                                    return point.Buffer(2, GisEditor.ActiveMap.MapUnit, DistanceUnit.Kilometer).GetBoundingBox();
                                }
                                else return tmpFeature.GetBoundingBox();
                            });

                            GisEditor.ActiveMap.CurrentExtent = ExtentHelper.GetBoundingBoxOfItems(bboxes);
                            GisEditor.ActiveMap.Refresh();
                        }

                        //make the features highlighted
                        var highlightOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                        if (highlightOverlay != null)
                        {
                            highlightOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
                            features.ForEach(feature => highlightOverlay.HighlightFeatureLayer.InternalFeatures.Add(feature));
                            highlightOverlay.HighlightFeatureLayer.BuildIndex();
                            highlightOverlay.Refresh();
                        }
                    }, () => GisEditor.ActiveMap != null && !string.IsNullOrEmpty(AddressToSearch));
                }
                return findCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        checkedFeatureLayers[GisEditor.ActiveMap].Clear();
                        foreach (var item in availableFeatureLayers.Where(tmpItem => tmpItem.IsChecked))
                        {
                            checkedFeatureLayers[GisEditor.ActiveMap].Add(item.Value);
                        }
                        Messenger.Default.Send(true, this);
                    });
                }
                return cancelCommand;
            }
        }

        public string AddressToSearch
        {
            get { return addressToSearch; }
            set
            {
                addressToSearch = value;
                RaisePropertyChanged(() => AddressToSearch);
            }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureLayer>> AvailableFeatureLayers
        {
            get { return availableFeatureLayers; }
        }

        private void CollectFeatureLayers()
        {
            availableFeatureLayers.Clear();
            if (GisEditor.ActiveMap != null)
            {
                bool isNew = false;
                if (!checkedFeatureLayers.ContainsKey(GisEditor.ActiveMap))
                {
                    checkedFeatureLayers.Add(GisEditor.ActiveMap, new Collection<FeatureLayer>());
                    isNew = true;
                }
                foreach (var item in GisEditor.ActiveMap.GetFeatureLayers(true))
                {
                    availableFeatureLayers.Add(new CheckableItemViewModel<FeatureLayer>
                        (item, isNew ? true : checkedFeatureLayers[GisEditor.ActiveMap].Contains(item), generateNameFunc));
                }
            }
        }

        public Collection<Feature> FindFeaturesByKeyword(string addressToSearch, IEnumerable<FeatureLayer> selectedLayers)
        {
            bool isContinue = MessageBoxHelper.ShowWarningMessageIfSoManyCount(selectedLayers);
            if (!isContinue) return new Collection<Feature>();

            var features = new Collection<Feature>();
            foreach (var layer in selectedLayers)
            {
                layer.SafeProcess(() =>
                {
                    var queriedFeatures = layer.QueryTools.GetAllFeatures(layer.GetDistinctColumnNames())
                        .Where(feature => feature.ColumnValues.Values
                        .Any(value => value.ToUpperInvariant().Contains(addressToSearch.ToUpperInvariant()))).ToList();

                    for (int i = 0; i < queriedFeatures.Count; i++)
                    {
                        var tmpFeature = queriedFeatures[i];
                        tmpFeature = GisEditor.SelectionManager.GetSelectionOverlay().CreateHighlightFeature(tmpFeature, layer);
                        features.Add(tmpFeature);
                    }
                });
            }
            return features;
        }
    }
}