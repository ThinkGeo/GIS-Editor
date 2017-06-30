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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for QueryFeatureLayerWindow.xaml
    /// </summary>
    public partial class QueryFeatureLayerWindow : Window
    {
        private static ObservableCollection<CheckableItemViewModel<FeatureLayer>> featureLayersDataSource;
        private static ObservableCollection<QueryConditionViewModel> savedConditions;
        private static string addressToSearch;
        private static QueryFeatureLayerWindow instance;
        private static bool isOpen = false;

        internal static ObservableCollection<QueryConditionViewModel> SavedConditions
        {
            get
            {
                if (savedConditions == null)
                {
                    savedConditions = new ObservableCollection<QueryConditionViewModel>();
                }
                return savedConditions;
            }
        }

        protected QueryFeatureLayerWindow()
        {
            InitializeComponent();
        }

        public static void OpenQuery()
        {
            if (!isOpen)
            {
                if (featureLayersDataSource == null)
                    featureLayersDataSource = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();

                var quickFindUserControl = new SimpleQueryUserControl { Margin = new Thickness(5) };
                var advancedQueryUserControl = new AdvancedQueryUserControl();

                foreach (var item in SavedConditions)
                {
                    if (!advancedQueryUserControl.ViewModel.Conditions.Contains(item))
                        advancedQueryUserControl.ViewModel.Conditions.Add(item);
                }

                if (!advancedQueryUserControl.ViewModel.IsQueryMatchModeEnabled)
                {
                    advancedQueryUserControl.ViewModel.SelectedQueryMatchMode = QueryMatchMode.Any;
                }

                var quickFindViewModel = quickFindUserControl.DataContext as SimpleQueryViewModel;
                if (quickFindViewModel != null)
                {
                    quickFindViewModel.AddressToSearch = addressToSearch;
                    foreach (var item in featureLayersDataSource)
                    {
                        var result = quickFindViewModel.AvailableFeatureLayers.FirstOrDefault(checkableItem => checkableItem.Value == item.Value);
                        if (result != null)
                        {
                            result.IsChecked = item.IsChecked;
                        }
                    }
                }

                TabControl tabControl = new TabControl() { Width = 400, Height = 385 };
                tabControl.Items.Add(new TabItem() { Content = quickFindUserControl, Header = GisEditor.LanguageManager.GetStringResource("FindFeaturesWindowQuickFindTab") });
                tabControl.Items.Add(new TabItem() { Content = advancedQueryUserControl, Header = GisEditor.LanguageManager.GetStringResource("FindFeaturesWindowAdvancedQueryTab") });
                tabControl.Margin = new Thickness(5);

                string helpUri = GisEditor.LanguageManager.GetStringResource("FindFeaturesHelp");
                FrameworkElement helpButton = HelpButtonHelper.GetHelpButton(helpUri, HelpButtonMode.IconOnly);
                helpButton.HorizontalAlignment = HorizontalAlignment.Right;
                helpButton.VerticalAlignment = VerticalAlignment.Top;
                helpButton.Margin = new Thickness(0, 5, 5, 0);

                Grid tabControlContainer = new Grid();
                tabControlContainer.Children.Add(tabControl);
                tabControlContainer.Children.Add(helpButton);

                instance = new QueryFeatureLayerWindow()
                {
                    Title = GisEditor.LanguageManager.GetStringResource("FindFeaturesWindowTitle"),
                    Content = tabControlContainer,
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Tag = GisEditor.ActiveMap
                };

                instance.Closing += (s, e) =>
                {
                    isOpen = false;
                    SavedConditions.Clear();
                    foreach (var item in advancedQueryUserControl.ViewModel.Conditions)
                    {
                        SavedConditions.Add(item);
                    }
                    addressToSearch = quickFindViewModel.AddressToSearch;
                    featureLayersDataSource.Clear();
                    foreach (var item in quickFindViewModel.AvailableFeatureLayers)
                    {
                        featureLayersDataSource.Add(item);
                    }
                };

                instance.Show();
                isOpen = true;
            }
        }

        public static void CloseQuery()
        {
            if (isOpen && instance != null && instance.Tag != GisEditor.ActiveMap)
            {
                instance.Close();
            }
        }

        public static void ClearConditions(IEnumerable<FeatureLayer> allLayers)
        {
            var conditions = QueryFeatureLayerWindow.SavedConditions.Where(c => !allLayers.Contains(c.Layer)).ToArray();
            foreach (var item in conditions)
            {
                QueryFeatureLayerWindow.SavedConditions.Remove(item);
            }
        }
    }
}