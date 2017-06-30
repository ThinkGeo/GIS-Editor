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


using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseLayersUserControl.xaml
    /// </summary>
    public partial class BlendWizardChooseLayersUserControl : UserControl
    {
        private BlendWizardShareObject entity;

        public BlendWizardChooseLayersUserControl(BlendWizardShareObject parameter)
        {
            InitializeComponent();

            entity = parameter;
        }

        public IEnumerable<FeatureLayer> SelectedLayers
        {
            get
            {
                Collection<FeatureLayer> layers = new Collection<FeatureLayer>();
                foreach (var item in layersList.SelectedItems)
                {
                    layers.Add(item as FeatureLayer);
                }
                if (GisEditor.SelectionManager.GetSelectionOverlay() != null)
                {
                    GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures.ForEach(f =>
                    {
                        entity.HasSelectedFeatures = layers.Any(l => f.Tag != null && f.Tag == l);
                    });
                }
                foreach (var layer in layersList.SelectedItems.OfType<FeatureLayer>().OrderBy(f => entity.AreaLayers.IndexOf(f)))
                {
                    yield return layer as FeatureLayer;
                }
            }
        }
    }
}