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


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    public partial class PreferenceWindow : Window
    {
        public PreferenceWindow()
        {
            InitializeComponent();
            HelpButton.Click += HelpButton_Click;
            SaveState();
            LoadState();

            var optionModels = GisEditorHelper.GetManagers().Select(m => m.GetSettingsUI()).Where(c => c != null)
                .Concat(GisEditorHelper.GetManagers().OfType<PluginManager>().SelectMany(m => m.GetPlugins().Where(p => p.IsActive).OrderByDescending(p => p.Index)
                    .Select(p => p.GetSettingsUI()).Where(c => c != null)))
                .Select(c => new TreeViewItemModel(c)).ToList();

            GroupTreeViewItems(optionModels);

            foreach (var optionModel in optionModels)
            {
                PreferenceTreeView.Items.Add(optionModel.ToTreeViewItem());
            }

            if (PreferenceTreeView.HasItems)
            {
                TreeViewItem item = PreferenceTreeView.Items[0] as TreeViewItem;
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpUri = GisEditor.LanguageManager.GetStringResource("OptionsHelp");
            if (!string.IsNullOrEmpty(helpUri)) Process.Start(helpUri);
        }

        private void GroupTreeViewItems(List<TreeViewItemModel> items)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var currentItem = items[i];
                if (!String.IsNullOrEmpty(currentItem.Category))
                {
                    var parentItem = items.FirstOrDefault(tmpItem => tmpItem.Title.Equals(currentItem.Category));
                    if (parentItem == null)
                    {
                        parentItem = new TreeViewItemModel(null);
                        parentItem.Title = currentItem.Category;
                        items.Add(parentItem);
                    }

                    parentItem.Items.Add(currentItem);
                    items.Remove(currentItem);
                }
            }
        }

        [Obfuscation]
        private void PreferenceTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem)
            {
                TreeViewItem item = (TreeViewItem)e.NewValue;
                Binding binding = new Binding();
                binding.ElementName = "PreferenceTreeView";
                binding.Path = new PropertyPath("SelectedItem.Header");
                PreferenceTitle.SetBinding(TextBlock.TextProperty, binding);
                string description = GetDescription(item);
                if (string.IsNullOrEmpty(description))
                    TxtDescription.Visibility = Visibility.Collapsed;
                else
                {
                    var descriptionString = GisEditor.LanguageManager.GetStringResource(description);
                    if (!string.IsNullOrEmpty(descriptionString))
                    {
                        TxtDescription.SetResourceReference(TextBlock.TextProperty, description);
                    }
                    else
                    {
                        TxtDescription.Text = description;
                    }
                    TxtDescription.Visibility = Visibility.Visible;
                }
                PreferenceContent.Content = item.Tag;
            }
        }

        private static string GetDescription(TreeViewItem item)
        {
            string description = "";
            var settingUserControl = item.Tag as SettingUserControl;
            var settingGroupTabUserControl = item.Tag as SettingGroupTabUserControl;

            if (settingUserControl != null)
                description = settingUserControl.Description;
            else if (settingGroupTabUserControl != null) description = settingGroupTabUserControl.Description;
            return description;
        }

        [Obfuscation]
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SaveState();
            RefreshAllMaps();
        }

        [Obfuscation]
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            LoadState();
            Close();
        }

        [Obfuscation]
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SaveState();
            RefreshAllMaps();
            DialogResult = true;
        }

        private void RefreshAllMaps()
        {
            foreach (var map in GisEditor.DockWindowManager.DocumentWindows.Select(d => d.Content).OfType<WpfMap>())
            {
                map.Overlays.ForEach(o => o.RefreshWithBufferSettings());
            }
        }

        private void SaveState()
        {
            GisEditor.InfrastructureManager.SaveSettings(GetSettings());
            GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.SaveStateDescription));
        }

        private void LoadState()
        {
            GisEditor.InfrastructureManager.ApplySettings(GetSettings());
        }

        private static IEnumerable<IStorableSettings> GetSettings()
        {
            var managerSettings = GisEditorHelper.GetManagers().Cast<IStorableSettings>();
            var pluginSettings = GisEditor.InfrastructureManager.GetManagers()
                .OfType<PluginManager>()
                .SelectMany(m => m.GetPlugins());

            return managerSettings.Concat(pluginSettings);
        }

        [Obfuscation]
        private void Window_Closed(object sender, EventArgs e)
        {
            PreferenceContent.Content = null;
        }

        private class TreeViewItemModel
        {
            private Collection<TreeViewItemModel> items;

            public TreeViewItemModel(SettingUserControl optionUI)
            {
                OptionUI = optionUI;
                if (optionUI != null)
                {
                    Title = optionUI.Title;
                    Category = optionUI.Category;
                    Description = optionUI.Description;
                    OptionUI.Loaded -= OptionUI_Loaded;
                    OptionUI.Loaded += OptionUI_Loaded;
                }

                items = new Collection<TreeViewItemModel>();
            }

            public string Title { get; set; }

            public string Category { get; set; }

            public string Description { get; set; }

            public UserControl OptionUI { get; set; }

            public Collection<TreeViewItemModel> Items { get { return items; } }

            public TreeViewItem ToTreeViewItem()
            {
                TreeViewItem treeViewItem = new TreeViewItem();

                string titleString = GisEditor.LanguageManager.GetStringResource(Title);
                if (!string.IsNullOrEmpty(titleString))
                {
                    treeViewItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, Title);
                }
                else
                {
                    treeViewItem.Header = Title;
                }

                treeViewItem.Tag = OptionUI;
                treeViewItem.Margin = new Thickness(0, 0, 0, 0);

                if (OptionUI == null && Items.Count > 0)
                {
                    var optionUI = new SettingGroupTabUserControl();
                    optionUI.Description = items[0].Description;
                    foreach (var tmpItem in Items)
                    {
                        optionUI.TabItemSources.Add(new { Header = GisEditor.LanguageManager.GetStringResource(tmpItem.Title), Content = tmpItem.OptionUI });
                    }

                    treeViewItem.Tag = OptionUI = optionUI;
                }
                else if (Items.Count > 0)
                {
                    foreach (var tmpItem in Items)
                    {
                        treeViewItem.Items.Add(tmpItem.ToTreeViewItem());
                    }
                }

                if (OptionUI != null)
                {
                    var viewModel = OptionUI.DataContext;
                    OptionUI.DataContext = null;
                    OptionUI.DataContext = viewModel;
                }
                return treeViewItem;
            }

            private void OptionUI_Loaded(object sender, RoutedEventArgs e)
            {
                //((UserControl)sender).UpdateLayout();
                var userControl = (UserControl)sender;
                var dataContext = userControl.DataContext;
                userControl.DataContext = null;
                userControl.DataContext = dataContext;
            }
        }
    }
}