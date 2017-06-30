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
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseLayers.xaml
    /// </summary>
    public partial class MergeWizardChooseLayersUserControl : UserControl
    {
        private MergeWizardShareObject entity;

        public MergeWizardChooseLayersUserControl(MergeWizardShareObject parameter)
        {
            InitializeComponent();
            DataContext = parameter;
            entity = parameter;
        }

        [Obfuscation]
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (object addedObj in e.AddedItems)
                {
                    var addedLayer = addedObj as FeatureLayer;
                    if (addedLayer != null)
                    {
                        if (!entity.SelectedLayers.Contains(addedLayer))
                        {
                            entity.SelectedLayers.Add(addedLayer);
                        }
                    }
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                foreach (object removedObj in e.RemovedItems)
                {
                    var removedLayer = removedObj as FeatureLayer;
                    if (removedLayer != null)
                    {
                        if (entity.SelectedLayers.Contains(removedLayer))
                        {
                            entity.SelectedLayers.Remove(removedLayer);
                        }
                    }
                }
            }

            GisEditor.SelectionManager.GetSelectedFeatures().ForEach(f =>
            {
                entity.OnlyMergeSelectedFeatures = entity.SelectedLayers.Any(l => f.Tag != null && f.Tag == l);
            });
        }
    }
}