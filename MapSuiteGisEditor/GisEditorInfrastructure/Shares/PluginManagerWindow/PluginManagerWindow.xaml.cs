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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for PluginConfigurationWindow.xaml
    /// </summary>
    public partial class PluginManagerWindow : Window
    {
        private ObservableCollection<ManagerTabItemSource> managerTabs;
        private Dictionary<string, Collection<Tuple<string, int, bool>>> state;
        private bool isPluginsChanged = false;

        public PluginManagerWindow()
        {
            InitializeComponent();
            managerTabs = new ObservableCollection<ManagerTabItemSource>();
            foreach (var source in CollectManagerTabItemSources())
            {
                AddProviderManager(source);
            }
            SaveCurrentState();
            tabControl1.ItemsSource = managerTabs;
        }

        internal ObservableCollection<ManagerTabItemSource> ManagerTabs { get { return managerTabs; } }

        private void AddProviderManager(ManagerTabItemSource tabItem)
        {
            UserControl contentList = null;
            if (tabItem.DelayLoad)
            {
                //contentList = new OnlinePluginListUserControl();
                //contentList.DataContext = tabItem.GetBindingSource();
            }
            else
            {
                var bindingSource = tabItem as ManagerTabItemSource;
                if (bindingSource != null)
                {
                    bindingSource.Applying = new Action<ManagerTabItemSource>(TabItemBindingSource_Applying);
                    bindingSource.PluginsSelected = new Action<ManagerTabItemSource, bool>(TabItemBindingSource_PluginsSelected);

                    contentList = new PluginManagerListUserControl();
                    contentList.DataContext = new PluginManagerListViewModel(bindingSource.CollectPlugins());
                }
            }
            tabItem.Content = contentList;
            ManagerTabs.Add(tabItem);
        }

        [Obfuscation]
        private static void TabItemBindingSource_PluginsSelected(ManagerTabItemSource tabItemSource, bool isSelected)
        {
            PluginManagerListViewModel tabItemController = (PluginManagerListViewModel)tabItemSource.Content.DataContext;
            foreach (var pluginModel in tabItemController.ItemsSource)
            {
                pluginModel.IsEnabled = isSelected;
            }
        }

        [Obfuscation]
        private static void TabItemBindingSource_Applying(ManagerTabItemSource tabItemSource)
        {
            PluginManagerListViewModel tabItemController = (PluginManagerListViewModel)tabItemSource.Content.DataContext;
            tabItemController.SyncPluginConfiguration(tabItemSource.CollectPlugins());
        }

        private static IEnumerable<ManagerTabItemSource> CollectManagerTabItemSources()
        {
            yield return new PluginManagerTabItemSource(GisEditor.UIManager
                , GisEditor.LanguageManager.GetStringResource("PluginManagerPluginsTabHeader"), new Uri("/MapSuiteGisEditor;component/Images/pluginManager_32.png", UriKind.RelativeOrAbsolute));

            yield return new PluginManagerTabItemSource(GisEditor.LayerManager
                , GisEditor.LanguageManager.GetStringResource("PluginManagerDataFormatsTabHeader"), new Uri("/MapSuiteGisEditor;component/Images/dataformats.png", UriKind.RelativeOrAbsolute));

            yield return new PluginManagerTabItemSource(GisEditor.StyleManager
                , GisEditor.LanguageManager.GetStringResource("PluginManagerStylesTabHeader"), new Uri("/MapSuiteGisEditor;component/Images/styles.png", UriKind.RelativeOrAbsolute));

            yield return new PluginManagerTabItemSource(GisEditor.ControlManager
                , GisEditor.LanguageManager.GetStringResource("PluginManagerUIssTabHeader"), new Uri("/MapSuiteGisEditor;component/Images/UIs.png", UriKind.RelativeOrAbsolute));
        }

        internal void SaveCurrentState()
        {
            state = new Dictionary<string, Collection<Tuple<string, int, bool>>>();

            //foreach (var managerTab in ManagerTabs.Where(tmpTab => !(tmpTab is OnlinePluginManagerTabItemSource)))
            foreach (var managerTab in ManagerTabs)
            {
                Collection<Tuple<string, int, bool>> tabState = new Collection<Tuple<string, int, bool>>();
                state.Add(managerTab.HeaderText, tabState);

                //foreach (var pluginInfo in managerTab.CollectPluginConfigurations())
                //{
                //    tabState.Add(new Tuple<string, int, bool>(pluginInfo.Plugin.GetType().FullName, pluginInfo.Index, pluginInfo.IsActive));
                //}

                foreach (var plugin in managerTab.CollectPlugins())
                {
                    tabState.Add(new Tuple<string, int, bool>(plugin.GetType().FullName, plugin.Index, plugin.IsActive));
                }
            }
        }

        private IEnumerable<ManagerTabItemSource> GetTabItemSourcesToApply()
        {
            //foreach (var managerTab in ManagerTabs.Where(tmpTab => !(tmpTab is OnlinePluginManagerTabItemSource)))
            foreach (var managerTab in ManagerTabs)
            {
                var oldState = state[managerTab.HeaderText].OrderBy(item => item.Item2);
                PluginManagerListViewModel tabItemController = (PluginManagerListViewModel)managerTab.Content.DataContext;
                for (int i = 0; i < tabItemController.ItemsSource.Count; i++)
                {
                    var tmpPluginInfo = tabItemController.ItemsSource[i];
                    var oldPluginInfoState = oldState.FirstOrDefault(tmpState => tmpState.Item1.Equals(tmpPluginInfo.Plugin.GetType().FullName));
                    if (oldState == null || oldPluginInfoState.Item2 != i + 1 || oldPluginInfoState.Item3 != tmpPluginInfo.IsEnabled)
                    {
                        yield return managerTab;
                        break;
                    }
                }
            }
        }

        [Obfuscation]
        private void btnEnableAll_Click(object sender, RoutedEventArgs e)
        {
            ManagerTabs[tabControl1.SelectedIndex].CheckAll(true);
        }

        [Obfuscation]
        private void btnDisableAll_Click(object sender, RoutedEventArgs e)
        {
            ManagerTabs[tabControl1.SelectedIndex].CheckAll(false);
        }

        [Obfuscation]
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            isPluginsChanged = false;
            var tabItemsToApply = GetTabItemSourcesToApply().ToArray();
            foreach (var tabItem in tabItemsToApply)
            {
                if (tabItem.HeaderText.Equals("Plugins"))
                {
                    isPluginsChanged = true;
                }
                tabItem.Apply();
            }

            //ManagerTabs[tabControl1.SelectedIndex].Apply();
            DialogResult = isPluginsChanged;
            Close();
        }

        [Obfuscation]
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        [Obfuscation]
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl1.SelectedIndex > 0 && tabControl1.SelectedIndex < ManagerTabs.Count)
            {
                ManagerTabs[tabControl1.SelectedIndex].Activate();
            }
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ManagerTabs.Count != 0) { tabControl1.SelectedIndex = 0; }
        }

        [Obfuscation]
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://giseditorwiki.thinkgeo.com/w/Plugin_Manager");
        }
    }
}