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


using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseExportLayerWindow.xaml
    /// </summary>
    public partial class ChooseExportLayerWindow : Window
    {
        private Collection<FeatureLayer> sourceFeatureLayers;
        private Collection<FeatureLayer> targetFeatureLayers;

        private FeatureLayer selectedSourceFeatureLayer;
        private FeatureLayer selectedTargetFeatureLayer;

        public ChooseExportLayerWindow()
            : this(new Collection<FeatureLayer>(), new Collection<FeatureLayer>())
        {
        }

        public ChooseExportLayerWindow(Collection<FeatureLayer> sourceFeatureLayers, Collection<FeatureLayer> targetFeatureLayers)
        {
            InitializeComponent();
            this.sourceFeatureLayers = sourceFeatureLayers;
            this.targetFeatureLayers = targetFeatureLayers;

            SourceFeatureLayerList.ItemsSource = sourceFeatureLayers;
            TargetFeatureLayerList.ItemsSource = targetFeatureLayers;

            SourceFeatureLayerList.SelectedItem = sourceFeatureLayers.FirstOrDefault();
            TargetFeatureLayerList.SelectedItem = targetFeatureLayers.FirstOrDefault();
        }

        public FeatureLayer SelectedSourceFeatureLayer
        {
            get { return selectedSourceFeatureLayer; }
            set { selectedSourceFeatureLayer = value; }
        }

        public FeatureLayer SelectedTargetFeatureLayer
        {
            get { return selectedTargetFeatureLayer; }
            set { selectedTargetFeatureLayer = value; }
        }

        public Collection<FeatureLayer> SourceFeatureLayers
        {
            get { return sourceFeatureLayers; }
        }

        public Collection<FeatureLayer> TargetFeatureLayers
        {
            get { return targetFeatureLayers; }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            selectedSourceFeatureLayer = SourceFeatureLayerList.SelectedItem as FeatureLayer;
            selectedTargetFeatureLayer = TargetFeatureLayerList.SelectedItem as FeatureLayer;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SourceFeatureLayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetFeatureLayerList.SelectedIndex > -1) OKButton.IsEnabled = true;
        }

        private void TargetFeatureLayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceFeatureLayerList.SelectedIndex > -1) OKButton.IsEnabled = true;
        }
    }
}