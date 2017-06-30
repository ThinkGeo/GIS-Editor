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
using System.Reflection;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for QuickAccessToolbarSettingUserControl.xaml
    /// </summary>
    [Obfuscation]
    public partial class QuickAccessToolbarSettingUserControl : SettingUserControl
    {
        public QuickAccessToolbarSettingUserControl()
        {
            Title = "QuickAccessToolbarSettingTitle";
            InitializeComponent();

            RefreshRibbonListBox();
        }

        [Obfuscation]
        private void RemoveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Collection<IInputElement> selectedItems = new Collection<IInputElement>();
            foreach (var selectedItem in listBox.SelectedItems)
            {
                var item = selectedItem as IInputElement;
                selectedItems.Add(item);
            }
            foreach (var item in selectedItems)
            {
                RibbonCommands.RemoveFromQuickAccessToolBarCommand.Execute(null, item);
            }
            RefreshRibbonListBox();
        }

        [Obfuscation]
        private void AddButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var selectedItem in ribbonListBox.SelectedItems)
            {
                var item = selectedItem as IInputElement;
                RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, item);
            }
            RefreshRibbonListBox();
        }

        private void RefreshRibbonListBox()
        {
            if (quickAccessToolbarSettingViewModel.GisEditorUserControl != null)
            {
                ribbonListBox.ItemsSource = quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.Items.OfType<RibbonTab>()
                     .SelectMany(g => g.Items.OfType<RibbonGroup>())
                     .SelectMany(g => g.Items.OfType<object>())
                     .Where(i => quickAccessToolbarSettingViewModel.CheckCanAddToQuickAccessBar(i)).OfType<object>();

                listBox.ItemsSource = quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items;
            }
        }

        [Obfuscation]
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            var index = listBox.SelectedIndex;
            if (index < quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items.Count - 1)
            {
                var selectedItem = quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items[index];
                quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items.RemoveAt(index);
                quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items.Insert(index + 1, selectedItem);
                listBox.SelectedItem = selectedItem;
            }
        }

        [Obfuscation]
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var index = listBox.SelectedIndex;
            if (index > 0)
            {
                var selectedItem = quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items[index];
                quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items.RemoveAt(index);
                quickAccessToolbarSettingViewModel.GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items.Insert(index - 1, selectedItem);
                listBox.SelectedItem = selectedItem;
            }
        }
    }
}