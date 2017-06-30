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


using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for FindFeatureUserControl.xaml
    /// </summary>
    public partial class FindFeatureUserControl : UserControl
    {
        private FindFeatureViewModel viewModel;
        private static FindFeatureUserControl instance;

        public FindFeatureUserControl()
        {
            InitializeComponent();
            MouseLeftButtonDown += FindFeatureUserControl_MouseLeftButtonDown;

            viewModel = new FindFeatureViewModel();
            DataContext = viewModel;
        }

        public static FindFeatureUserControl Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FindFeatureUserControl();
                }
                return instance;
            }
        }

        public void RefreshFeatureLayers()
        {
            viewModel.RefreshFeatureLayers();
        }

        [Obfuscation]
        private void FindFeatureUserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewModel.RefreshFeatureLayers();
        }

        [Obfuscation]
        private void FeatureNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement clickedElement = sender as FrameworkElement;
            if (clickedElement != null)
            {
                FeatureViewModel entity = clickedElement.DataContext as FeatureViewModel;
                if (entity != null)
                {
                    var tmpFeature = GisEditor.SelectionManager.GetSelectionOverlay().CreateHighlightFeature(entity.Feature, entity.OwnerFeatureLayer);
                    tmpFeature.Tag = entity.OwnerFeatureLayer;
                    var highlightLayer = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer;
                    highlightLayer.InternalFeatures.Clear();
                    highlightLayer.InternalFeatures.Add(tmpFeature.Id, tmpFeature);
                    GisEditor.ActiveMap.CurrentExtent = entity.Feature.GetBoundingBox();
                    GisEditor.ActiveMap.Refresh();
                }
            }
        }

        [Obfuscation]
        private void FeatureNode_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var featrueEntity = sender.GetDataContext<FeatureViewModel>();
            if (featrueEntity != null) featrueEntity.IsSelected = true;
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem != null) treeViewItem.ContextMenu.IsOpen = true;
        }

        [Obfuscation]
        private void FeaturesList_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            FeatureViewModel entity = featuresList.SelectedValue as FeatureViewModel;
            if (entity != null)
            {
                e.Handled = true;
                viewModel.SelectedEntity = entity;
            }
            else
            {
                FeatureLayerViewModel layerEntity = featuresList.SelectedValue as FeatureLayerViewModel;
                if (layerEntity != null)
                {
                    var featureEntity = layerEntity.FoundFeatures.FirstOrDefault();
                    if (featureEntity != null)
                    {
                        featureEntity.IsSelected = true;
                        viewModel.SelectedEntity = featureEntity;
                    }
                }
            }
        }

        [Obfuscation]
        private void ZoomToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FeatureViewModel entity = featuresList.SelectedValue as FeatureViewModel;
            if (entity != null)
            {
                var tmpFeature = GisEditor.SelectionManager.GetSelectionOverlay().CreateHighlightFeature(entity.Feature, entity.OwnerFeatureLayer);
                tmpFeature.Tag = entity.OwnerFeatureLayer;
                var highlightLayer = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer;
                highlightLayer.InternalFeatures.Clear();
                highlightLayer.InternalFeatures.Add(tmpFeature.Id, tmpFeature);
                GisEditor.ActiveMap.CurrentExtent = entity.Feature.GetBoundingBox();
                GisEditor.ActiveMap.Refresh();
            }
        }
    }
}
