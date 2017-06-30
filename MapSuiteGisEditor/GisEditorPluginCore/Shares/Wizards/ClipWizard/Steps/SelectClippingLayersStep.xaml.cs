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
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SelectClippingLayersStep.xaml
    /// </summary>
    public partial class SelectClippingLayersStep : UserControl
    {
        private ClippingWizardSharedObject sharedObject;

        public SelectClippingLayersStep(ClippingWizardSharedObject parameter)
        {
            InitializeComponent();
            DataContext = parameter;
            sharedObject = parameter;
        }


        [Obfuscation]
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object obj in e.AddedItems)
            {
                ((ClippingLayerViewModel)obj).IsSelected = true;
            }
            foreach (object obj in e.RemovedItems)
            {
                ((ClippingLayerViewModel)obj).IsSelected = false;
            }
        }

        [Obfuscation]
        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            UpdateCheckBoxStatus();
        }

        [Obfuscation]
        private void ListBoxItem_Unselected(object sender, RoutedEventArgs e)
        {
            UpdateCheckBoxStatus();
        }

        private void UpdateCheckBoxStatus()
        {
            var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
            if (selectionOverlay != null)
            {
                sharedObject.IsUseSelectedFeaturesEnable = sharedObject.ClippingLayers.Where(l => l.IsSelected).Any(l => selectionOverlay.HighlightFeatureLayer.InternalFeatures.Any(f => f.Tag.Equals(l.FeatureLayer)));
            }
            else
            {
                sharedObject.IsUseSelectedFeaturesEnable = false;
            }
        }
    }
}