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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SnappingToolsViewModel : ViewModelBase
    {
        private ObservableCollection<CheckableItemViewModel<FeatureLayer>> targetLayers;
        private Collection<CheckableItemViewModel<SnappingDistanceUnit>> distanceUnits;
        private GisEditorEditInteractiveOverlay editOverlay;

        public SnappingToolsViewModel()
        {
            targetLayers = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();
            distanceUnits = new Collection<CheckableItemViewModel<SnappingDistanceUnit>>();
            foreach (SnappingDistanceUnit item in Enum.GetValues(typeof(SnappingDistanceUnit)))
            {
                if (item == SnappingDistanceUnit.NauticalMile) continue;
                distanceUnits.Add(new CheckableItemViewModel<SnappingDistanceUnit>(item));
            }
        }

        public GisEditorEditInteractiveOverlay EditOverlay
        {
            get { return editOverlay; }
            set
            {
                if (editOverlay != value)
                {
                    editOverlay = value;
                    if (editOverlay != null)
                    {
                        SnappingDistance = editOverlay.SnappingDistance;
                        SnappingDistanceUnit = editOverlay.SnappingDistanceUnit;
                        editOverlay.SnappingLayers.CollectionChanged -= new NotifyCollectionChangedEventHandler(SnappingLayers_CollectionChanged);
                        editOverlay.SnappingLayers.CollectionChanged += new NotifyCollectionChangedEventHandler(SnappingLayers_CollectionChanged);
                    }
                    RaisePropertyChanged(()=>SnappingLayerNameTooltip);
                    RaisePropertyChanged(()=>SnappingDistance);
                    RaisePropertyChanged(()=>SnappingDistanceUnit);
                }
            }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureLayer>> TargetLayers
        {
            get
            {
                targetLayers.Clear();
                Func<FeatureLayer, string> generateNameFunc = l =>
                {
                    if (l == null) return GisEditor.LanguageManager.GetStringResource("SnappingToolsText");
                    else return l.Name;
                };

                targetLayers.Add(new CheckableItemViewModel<FeatureLayer>(null, false, generateNameFunc));
                if (GisEditor.ActiveMap != null)
                {
                    foreach (var featureLayer in GisEditor.ActiveMap.GetFeatureLayers(true))
                    {
                        targetLayers.Add(new CheckableItemViewModel<FeatureLayer>(featureLayer, false, generateNameFunc));
                    }
                }

                if (SnappingLayers != null && SnappingLayers.Count == 0)
                {
                    targetLayers.Where(l => l.Value == null).First().IsChecked = true;
                }
                else
                {
                    foreach (var tmpTargetLayer in targetLayers)
                    {
                        if (SnappingLayers != null && SnappingLayers.Contains(tmpTargetLayer.Value))
                        {
                            tmpTargetLayer.IsChecked = true;
                        }
                    }
                }

                return targetLayers;
            }
        }

        public Collection<CheckableItemViewModel<SnappingDistanceUnit>> DistanceUnits
        {
            get { return distanceUnits; }
        }

        public ObservableCollection<FeatureLayer> SnappingLayers
        {
            get
            {
                if (EditOverlay != null) return EditOverlay.SnappingLayers;
                else return null;
            }
        }

        public string SnappingLayerName
        {
            get
            {
                if (SnappingLayers == null || SnappingLayers.Count == 0)
                {
                    return GisEditor.LanguageManager.GetStringResource("SnappingToolsText");
                }
                else if (SnappingLayers.Count == 1)
                {
                    return SnappingLayers[0].Name;
                }
                else
                {
                    return "Multiple Layers";
                }
            }
        }

        public string SnappingLayerNameTooltip
        {
            get
            {
                if (SnappingLayers != null && SnappingLayers.Count > 1)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("Selected snapping layers: ");
                    foreach (var snappingLayer in SnappingLayers)
                    {
                        stringBuilder.AppendFormat("{0},", snappingLayer.Name);
                    }
                    return stringBuilder.ToString().TrimEnd(',');
                }
                else return "Select Snapping Layers";
            }
        }

        public double SnappingDistance
        {
            get { return EditOverlay == null ? 0.1 : EditOverlay.SnappingDistance; }
            set
            {
                if (EditOverlay != null)
                {
                    EditOverlay.SnappingDistance = value;
                    var gisEditorTrackOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
                    if (gisEditorTrackOverlay != null)
                    {
                        gisEditorTrackOverlay.SnappingDistance = value;
                    }
                    RaisePropertyChanged(()=>SnappingDistance);
                }
            }
        }

        public SnappingDistanceUnit SnappingDistanceUnit
        {
            get { return EditOverlay == null ? SnappingDistanceUnit.Kilometer : EditOverlay.SnappingDistanceUnit; }
            set
            {
                if (EditOverlay != null)
                {
                    EditOverlay.SnappingDistanceUnit = value;
                    foreach (var distanceUnit in DistanceUnits)
                    {
                        distanceUnit.IsChecked = distanceUnit.Value == value;
                    }
                    RaisePropertyChanged(()=>SnappingDistanceUnit);

                    //To make sure the validation runs again.
                    SnappingDistance = SnappingDistance;
                }
            }
        }

        public bool IsSnappingOptionEnabled
        {
            get { return EditOverlay != null && EditOverlay.SnappingLayers != null && EditOverlay.SnappingLayers.Count != 0; }
        }

        public void Refresh(GisEditorWpfMap wpfMap)
        {
            RaisePropertyChanged(()=>TargetLayers);
            RaisePropertyChanged(()=>SnappingLayerName);

            if (SnappingLayers != null && SnappingLayers.Count > 0)
            {
                var count = wpfMap.GetFeatureLayers().Count(l => SnappingLayers.Contains(l));
                if (count == 0)
                {
                    SnappingLayers.Clear();
                    EditOverlay.Refresh();
                }
            }
        }

        private void SnappingLayers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(()=>SnappingLayerName);
            RaisePropertyChanged(()=>SnappingLayerNameTooltip);
            RaisePropertyChanged(()=>IsSnappingOptionEnabled);
        }
    }
}