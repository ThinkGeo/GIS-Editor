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


using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for AdvancedQueryUserControl.xaml
    /// </summary>
    [Obfuscation]
    internal partial class AdvancedQueryUserControl : UserControl
    {
        private AdvancedQueryViewModel viewModel;

        public AdvancedQueryUserControl()
            : this(null)
        { }

        public AdvancedQueryUserControl(FeatureLayer targetLayer)
        {
            InitializeComponent();
            viewModel = new AdvancedQueryViewModel(targetLayer);
            DataContext = viewModel;

            Loaded += (s, e) =>
            {
                Messenger.Default.Register<bool>(this, viewModel, (value) => { CloseWindow(value); });
                Messenger.Default.Register<DialogMessage>(this, viewModel, (m) => { System.Windows.Forms.MessageBox.Show(m.Content, m.Caption); });

                MatchComboBox.IsEnabledChanged -= MatchComboBox_IsEnabledChanged;
                MatchComboBox.IsEnabledChanged += MatchComboBox_IsEnabledChanged;
            };
        }

        public IEnumerable<Feature> ResultFeatures
        {
            get { return viewModel.ResultFeatures; }
        }

        internal AdvancedQueryViewModel ViewModel { get { return viewModel; } }

        [Obfuscation]
        private void MatchComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue && viewModel.SelectedQueryMatchMode == QueryMatchMode.All)
            {
                System.Windows.Forms.MessageBox.Show("\"Match all\" cannot be selected when conditions are set for more than one layer."
                    , "Info", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                viewModel.SelectedQueryMatchMode = QueryMatchMode.Any;
            }
        }

        [Obfuscation]
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        private void CloseWindow(bool value)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                if (!value)
                    parentWindow.Close();
                else
                    parentWindow.DialogResult = value;
            }
        }

        [Obfuscation]
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = e.Source as ListBoxItem;
            if (item != null)
            {
                var entity = item.Content as QueryConditionViewModel;
                if (entity != null) viewModel.EditConditionCommand.Execute(entity);
            }
        }
    }
}