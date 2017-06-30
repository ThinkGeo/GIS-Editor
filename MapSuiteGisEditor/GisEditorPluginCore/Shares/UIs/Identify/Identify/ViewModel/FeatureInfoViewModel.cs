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
using System.Data;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FeatureInfoViewModel : ViewModelBase
    {
        private bool isBusy;
        private ObservableCollection<FeatureLayerViewModel> featureEntities;
        private FeatureViewModel selectedEntity;

        public FeatureInfoViewModel()
        {
            isBusy = true;
            featureEntities = new ObservableCollection<FeatureLayerViewModel>();
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

        public ObservableCollection<FeatureLayerViewModel> FeatureEntities
        {
            get { return featureEntities; }
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

        public void RefreshFeatureEntities(Dictionary<FeatureLayer, Collection<Feature>> selectedFeatureEntities)
        {
            FeatureEntities.Clear();
            SelectedEntity = null;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                FeatureEntities.Clear();
                foreach (var item in selectedFeatureEntities)
                {
                    FeatureLayerViewModel entity = new FeatureLayerViewModel { LayerName = item.Key.Name };
                    foreach (var tmpFeature in item.Value)
                    {
                        entity.FoundFeatures.Add(new FeatureViewModel(tmpFeature, item.Key));
                    }
                    FeatureEntities.Add(entity);
                }

                if (FeatureEntities.Count > 0 && FeatureEntities[0].FoundFeatures.Count > 0)
                {
                    selectedEntity = FeatureEntities[0].FoundFeatures[0];
                    RaisePropertyChanged(() => SelectedDataView);
                }
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        internal static Collection<LayerOverlay> FindLayerOverlayContaining(GisEditorWpfMap gisEditorWpfMap, FeatureLayer selectedFeatureLayer)
        {
            Collection<LayerOverlay> results = new Collection<LayerOverlay>();
            foreach (LayerOverlay overlay in gisEditorWpfMap.Overlays.OfType<LayerOverlay>())
            {
                if (overlay.Layers.OfType<FeatureLayer>().Any<FeatureLayer>(
                    layer => layer == selectedFeatureLayer))
                {
                    results.Add(overlay);
                }
            }
            return results;
        }

        internal void ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode shapeFileReadWriteMode, FeatureLayer selectedLayer)
        {
            ShapeFileFeatureLayer layer = selectedLayer as ShapeFileFeatureLayer;
            if (layer != null || layer.ReadWriteMode != shapeFileReadWriteMode)
            {
                lock (layer)
                {
                    layer.Close();
                    layer.ReadWriteMode = shapeFileReadWriteMode;
                }
            }
        }
    }
}