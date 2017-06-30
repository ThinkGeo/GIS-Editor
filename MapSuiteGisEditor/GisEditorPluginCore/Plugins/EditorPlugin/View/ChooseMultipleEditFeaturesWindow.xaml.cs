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


using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ChooseMultipleEditFeaturesWindow.xaml
    /// </summary>
    public partial class ChooseMultipleEditFeaturesWindow : Window
    {
        internal static string ChooseMultipleEditFeaturesWindowName = "ChooseMultipleEditFeaturesWindow";

        private Collection<Feature> selectedFeatures;

        public event EventHandler<SelectedFeaturesChangedEventArgs> SelectedFeaturesChanged;

        protected virtual void OnSelectedFeaturesChanged(SelectedFeaturesChangedEventArgs e)
        {
            EventHandler<SelectedFeaturesChangedEventArgs> handler = SelectedFeaturesChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public ChooseMultipleEditFeaturesWindow(IEnumerable<FeatureIdItem> featureItems)
        {
            InitializeComponent();

            this.Name = ChooseMultipleEditFeaturesWindowName;
            this.selectedFeatures = new Collection<Feature>();

            featureList.ItemsSource = featureItems;
        }

        public Collection<Feature> SelectedFeatures
        {
            get { return selectedFeatures; }
        }

        [Obfuscation]
        private void AllHyperlink_Click(object sender, RoutedEventArgs e)
        {
            var items = featureList.ItemsSource as Collection<FeatureIdItem>;
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.IsChecked = true;
                }
            }

            RaiseSelectedFeaturesChangedEvent();
        }

        [Obfuscation]
        private void NoneHyperlink_Click(object sender, RoutedEventArgs e)
        {
            var items = featureList.ItemsSource as Collection<FeatureIdItem>;
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.IsChecked = false;
                }
            }

            RaiseSelectedFeaturesChangedEvent();
        }

        [Obfuscation]
        private void ListBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FeatureIdItem featureIdItem = featureList.SelectedItem as FeatureIdItem;
            if (featureIdItem != null)
            {
                featureIdItem.IsChecked = !featureIdItem.IsChecked;
            }

            RaiseSelectedFeaturesChangedEvent();
        }

        private void RaiseSelectedFeaturesChangedEvent()
        {
            Collection<Feature> features = new Collection<Feature>();
            var items = featureList.ItemsSource as Collection<FeatureIdItem>;
            if (items != null)
            {
                foreach (var item in items.Where(i => i.IsChecked))
                {
                    features.Add(item.Feature);
                }
            }

            var args = new SelectedFeaturesChangedEventArgs(features);
            OnSelectedFeaturesChanged(args);
        }

        [Obfuscation]
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var items = featureList.ItemsSource as Collection<FeatureIdItem>;
            if (items != null)
            {
                foreach (var item in items.Where(i => i.IsChecked))
                {
                    selectedFeatures.Add(item.Feature);
                }
            }

            this.Close();
        }

        [Obfuscation]
        internal void RefreshData(Collection<FeatureIdItem> featureItems)
        {
            featureList.ItemsSource = featureItems;
        }
    }

    public class FeatureIdItem : ViewModelBase
    {
        private string name;
        private bool isChecked;
        private Feature feature;

        public FeatureIdItem(Feature feature)
        {
            this.feature = feature;
            this.name = feature.Id;
        }

        public Feature Feature
        {
            get { return feature; }
            set { feature = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                RaisePropertyChanged(() => IsChecked);
            }
        }
    }
}
