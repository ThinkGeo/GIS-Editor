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
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SnappingToolsRibbonGroup.xaml
    /// </summary>
    public partial class SnappingToolsRibbonGroup : RibbonGroup
    {
        public SnappingToolsRibbonGroup()
        {
            InitializeComponent();
        }

        public SnappingToolsViewModel ViewModel
        {
            get { return viewModel; }
        }

        [Obfuscation]
        private void SnappingDistanceUnitChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null && e.NewValue is CheckableItemViewModel<SnappingDistanceUnit>)
            {
                CheckableItemViewModel<SnappingDistanceUnit> model = (CheckableItemViewModel<SnappingDistanceUnit>)e.NewValue;
                viewModel.SnappingDistanceUnit = model.Value;

                var gisEditorTrackOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
                if (gisEditorTrackOverlay != null)
                {
                    gisEditorTrackOverlay.SnappingDistanceUnit = model.Value;
                }
            }
        }

        [Obfuscation]
        private void TargetLayerSelected(object sender, RoutedEventArgs e)
        {
            RibbonMenuItem menuItem = (RibbonMenuItem)sender;

            var gisEditorTrackOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
            if (gisEditorTrackOverlay != null)
            {
                gisEditorTrackOverlay.EditOverlay = EditingToolsViewModel.Instance.EditOverlay;
                gisEditorTrackOverlay.IsDirty = true;
                gisEditorTrackOverlay.GeographyUnit = GisEditor.ActiveMap.MapUnit;
                gisEditorTrackOverlay.SnappingDistanceUnit = viewModel.SnappingDistanceUnit;
                gisEditorTrackOverlay.SnappingDistance = viewModel.SnappingDistance;
            }

            var dataContext = (CheckableItemViewModel<FeatureLayer>)menuItem.DataContext;
            if (dataContext.Value == null && !dataContext.IsChecked)
            {
                viewModel.SnappingLayers.Clear();
                if (gisEditorTrackOverlay != null)
                {
                    gisEditorTrackOverlay.SnappingLayers.Clear();
                }
            }
            else if (dataContext.IsChecked && dataContext.Value != null && viewModel.SnappingLayers.Contains(dataContext.Value))
            {
                viewModel.SnappingLayers.Remove(dataContext.Value);
                if (gisEditorTrackOverlay != null)
                {
                    gisEditorTrackOverlay.SnappingLayers.Remove(dataContext.Value); ;
                }
            }
            else if (dataContext.Value != null)
            {
                viewModel.SnappingLayers.Add(dataContext.Value);

                //var gisEditorTrackOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
                if (gisEditorTrackOverlay != null)
                {
                    //gisEditorTrackOverlay.Layer = EditingToolsViewModel.Instance.SelectedLayer.Value;
                    //gisEditorTrackOverlay.SnappingLayers = viewModel.SnappingLayers;


                    gisEditorTrackOverlay.SnappingLayers.Add(dataContext.Value);

                    GisEditor.ActiveMap.CurrentExtentChanged -= new System.EventHandler<CurrentExtentChangedWpfMapEventArgs>(ActiveMap_CurrentExtentChanged);
                    GisEditor.ActiveMap.CurrentExtentChanged += new System.EventHandler<CurrentExtentChangedWpfMapEventArgs>(ActiveMap_CurrentExtentChanged);
                }
            }

            if (viewModel.SnappingLayers.Count == 0)
            {
                viewModel.TargetLayers.Where(l => l.Value == null).First().IsChecked = true;
            }
            else
            {
                viewModel.TargetLayers.ForEach(l =>
                {
                    if (l.Value == null) l.IsChecked = false;
                    else if (viewModel.SnappingLayers.Contains(l.Value))
                    {
                        l.IsChecked = true;
                    }
                });
            }

            viewModel.EditOverlay.Refresh();
        }

        private void ActiveMap_CurrentExtentChanged(object sender, CurrentExtentChangedWpfMapEventArgs e)
        {
            var gisEditorTrackOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
            if (gisEditorTrackOverlay != null)
            {
                gisEditorTrackOverlay.IsDirty = true;
            }
        }
    }
}