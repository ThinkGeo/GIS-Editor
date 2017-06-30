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


using Microsoft.Windows.Controls.Ribbon;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SelectionAndQueryingPluginRibbonGroup.xaml
    /// </summary>
    public partial class SelectionAndQueryingRibbonGroup : RibbonGroup
    {
        private SelectionAndQueryingRibbonGroupViewModel viewModel;
        private Func<FeatureLayer, string> generateNameFunc;
        private Collection<object> needToRemove;

        public SelectionAndQueryingRibbonGroup()
        {
            InitializeComponent();

            needToRemove = new Collection<object>();
            viewModel = new SelectionAndQueryingRibbonGroupViewModel();
            DataContext = viewModel;
            ribbonSelection.DropDownClosed += RibbonSelectionDropDown_Closed;
            generateNameFunc = (featureLayer) => featureLayer != null ? featureLayer.Name : SelectionAndQueryingRibbonGroupViewModel.DefaultTitle;
        }

        public SelectionTrackInteractiveOverlay SelectionOverlay { get { return viewModel.SelectionOverlay; } }

        public SelectionAndQueryingRibbonGroupViewModel ViewModel { get { return viewModel; } }

        public void Synchronize(GisEditorWpfMap wpfMap, RefreshArgs synchronizeArgs)
        {
            RemoveConditionsLayerNotExist(wpfMap);
            SynchronizeTargetLayersComboBox(synchronizeArgs);
            SynchronizeSpatialQueryMode();
            SynchronizeButtonStatus();
        }

        private static void RemoveConditionsLayerNotExist(GisEditorWpfMap wpfMap)
        {
            var allLayers = wpfMap.GetFeatureLayers();

            QueryFeatureLayerWindow.ClearConditions(allLayers);
        }

        private void SynchronizeSpatialQueryMode()
        {
            if (SelectionOverlay != null)
            {
                var queryMode = SelectionOverlay.SpatialQueryMode;
                foreach (var tmpEntity in viewModel.SpatialQueryModeEntities)
                {
                    tmpEntity.IsChecked = tmpEntity.Value == queryMode;
                }
            }
        }

        private void SynchronizeButtonStatus()
        {
            if (SelectionOverlay != null && SelectionOverlay.IsEnabled())
            {
                switch (SelectionOverlay.TrackMode)
                {
                    case TrackMode.Point:
                        viewModel.IsPointChecked = true;
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawPoint;
                        break;

                    case TrackMode.Rectangle:
                        viewModel.IsRectangleChecked = true;
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawRectangle;
                        break;

                    case TrackMode.Circle:
                        viewModel.IsCircleChecked = true;
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawCircle;
                        break;

                    case TrackMode.Polygon:
                        viewModel.IsPolygonChecked = true;
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawPolygon;
                        break;

                    case TrackMode.Line:
                        viewModel.IsLineChecked = true;
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawLine;
                        break;
                }
            }
            else
            {
                viewModel.UncheckAllSelectionButton();
            }
        }

        private void SynchronizeTargetLayersComboBox(RefreshArgs synchronizeArgs)
        {
            var selectionOverlay = SelectionOverlay;
            if (selectionOverlay != null)
            {
                selectionOverlay.TargetFeatureLayers.Clear();
                GisEditor.ActiveMap.GetFeatureLayers(true).ForEach(tmpLayer => selectionOverlay.TargetFeatureLayers.Add(tmpLayer));

                if (synchronizeArgs != null
                    && synchronizeArgs.Description == RefreshArgsDescription.DockManagerActiveDocumentChangedDescription)
                {
                    selectionOverlay.FilteredLayers.Clear();
                }

                selectionOverlay.TargetFeatureLayers.Where(tmpLayer
                    => !viewModel.Layers.Select(tmpLayerModel
                        => tmpLayerModel.Value).Contains(tmpLayer))
                    .ForEach(tmpLayer => selectionOverlay.FilteredLayers.Add(tmpLayer));

                viewModel.Layers.Clear();
                selectionOverlay.TargetFeatureLayers.ForEach(tmpLayer =>
                    viewModel.Layers.Add(new CheckableItemViewModel<FeatureLayer>(tmpLayer, selectionOverlay.FilteredLayers.Contains(tmpLayer), generateNameFunc))
                );
            }
            else
            {
                viewModel.Layers.Clear();
            }

            viewModel.RaiseDisplayTextPropertyChanged();
        }

        [Obfuscation]
        private void RibbonSelectionDropDown_Closed(object sender, EventArgs e)
        {
            ribbonSelection.IsDropDownOpen = !ribbonSelection.IsFocused;
        }

        [Obfuscation]
        private void OutlineColorPicker_SelectedItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //OutlineColorRibbonButton.IsChecked = false;
        }

        [Obfuscation]
        private void RibbonSplitButton_DropDownOpened(object sender, EventArgs e)
        {
            if (GisEditor.ActiveMap != null)
            {
                editingLayerMenuItem.Visibility = (GisEditor.ActiveMap.FeatureLayerEditOverlay != null &&
                    GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer != null) ? Visibility.Visible : Visibility.Collapsed;

                foreach (var item in needToRemove)
                {
                    ribbonSplitButton.Items.Remove(item);
                }
                needToRemove.Clear();

                var visibleInMemoryLayers = GisEditor.ActiveMap.GetFeatureLayers(true).OfType<InMemoryFeatureLayer>().ToList();
                if (visibleInMemoryLayers.Count > 0)
                {
                    RibbonSeparator ribbonSeparator = new RibbonSeparator();
                    needToRemove.Add(ribbonSeparator);
                    ribbonSplitButton.Items.Add(ribbonSeparator);

                    foreach (var item in visibleInMemoryLayers)
                    {
                        RibbonMenuItem ribbonMenuItem = new RibbonMenuItem();
                        ribbonMenuItem.Header = item.Name;
                        ribbonMenuItem.Tag = item;
                        ribbonMenuItem.Click += new RoutedEventHandler(CopyToExistingLayerRibbonMenuItem_Click);
                        ribbonSplitButton.Items.Add(ribbonMenuItem);
                        needToRemove.Add(ribbonMenuItem);
                    }
                }
            }
        }

        private void CopyToExistingLayerRibbonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RibbonMenuItem ribbonMenuItem = sender as RibbonMenuItem;
            if (ribbonMenuItem != null)
            {
                InMemoryFeatureLayer inMemoryFeatureLayer = ribbonMenuItem.Tag as InMemoryFeatureLayer;
                if (inMemoryFeatureLayer != null)
                {
                    Collection<FeatureLayer> selectedFeatureLayers = new Collection<FeatureLayer>();
                    foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                    {
                        Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                        if (features.Count > 0) selectedFeatureLayers.Add(layer);
                    }

                    HighlightedFeaturesHelper.CopyToExistingLayer(selectedFeatureLayers, inMemoryFeatureLayer);
                }
            }
        }
    }
}