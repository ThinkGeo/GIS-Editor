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
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for IdentifyWindow.xaml
    /// </summary>
    [Serializable]
    public partial class FeatureInfoWindow : DockWindow
    {
        private static FeatureInfoWindow instance;

        protected FeatureInfoWindow()
            : this(new Dictionary<FeatureLayer, Collection<Feature>>())
        { }

        protected FeatureInfoWindow(Dictionary<FeatureLayer, Collection<Feature>> selectedFeaturesEntities)
        {
            InitializeComponent();

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("FeatureInformationHelp", HelpButtonMode.NormalButton);
            featureInfoControl.Refresh(selectedFeaturesEntities);
        }

        public static FeatureInfoWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FeatureInfoWindow();
                }
                return instance;
            }
        }

        public void Refresh(Collection<Feature> selectedFeatures)
        {
            Dictionary<FeatureLayer, Collection<Feature>> features = new Dictionary<FeatureLayer, Collection<Feature>>();
            foreach (var group in selectedFeatures.GroupBy(f => f.Tag))
            {
                FeatureLayer featureLayer = group.Key as FeatureLayer;
                if (featureLayer != null)
                {
                    features[featureLayer] = new Collection<Feature>();
                    foreach (var feature in group)
                    {
                        Feature newFeature = feature.CloneDeep();
                        newFeature.Id = GisEditor.SelectionManager.GetSelectionOverlay().GetOriginalFeatureId(newFeature);
                        newFeature.Tag = featureLayer;
                        features[featureLayer].Add(newFeature);
                    }
                }
            }

            Refresh(features);
        }

        public void Refresh(Dictionary<FeatureLayer, Collection<Feature>> selectedFeatureEntities)
        {
            featureInfoControl.Refresh(selectedFeatureEntities);
        }

        [System.Reflection.Obfuscation]
        private void ClearClick(object sender, RoutedEventArgs e)
        {
            var identifyOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (identifyOverlay != null)
            {
                identifyOverlay.HighlightFeatureLayer.InternalFeatures.Clear();
                identifyOverlay.HighlightFeatureLayer.BuildIndex();
                identifyOverlay.StandOutHighlightFeatureLayer.InternalFeatures.Clear();
                identifyOverlay.Refresh();
            }

            featureInfoControl.Refresh(new Dictionary<FeatureLayer,Collection<Feature>>());
        }
    }
}