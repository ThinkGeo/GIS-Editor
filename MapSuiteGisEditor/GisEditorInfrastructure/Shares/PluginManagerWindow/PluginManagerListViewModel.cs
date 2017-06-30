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
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class PluginManagerListViewModel : ViewModelBase
    {
        private int selectedIndex;
        private PluginViewModel selectedItem;
        private Visibility orderButtonVisibility;
        private ObservedCommand<string> reorderCommand;
        private GeoCollection<Plugin> discoveredPlugins;
        private ObservableCollection<PluginViewModel> itemsSource;

        public PluginManagerListViewModel(IEnumerable<Plugin> plugins)
        {
            itemsSource = new ObservableCollection<PluginViewModel>();
            Initialize(plugins);
            CollectItemsSource();
            if (plugins.All(p => p is UIPlugin))
            {
                orderButtonVisibility = Visibility.Visible;
            }
            else
            {
                orderButtonVisibility = Visibility.Collapsed;
            }
        }

        public Visibility OrderButtonVisibility
        {
            get { return orderButtonVisibility; }
        }

        public ObservedCommand<string> ReorderCommand
        {
            get
            {
                if (reorderCommand == null)
                {
                    reorderCommand = new ObservedCommand<string>((param) =>
                    {
                        var selectedEntity = new { SelectedIndex = SelectedIndex, SelectedItem = (PluginViewModel)SelectedItem };
                        if (param.Equals("Up"))
                        {
                            ItemsSource.Remove(selectedEntity.SelectedItem);
                            ItemsSource.Insert(selectedEntity.SelectedIndex - 1, selectedEntity.SelectedItem);
                            SelectedItem = ItemsSource[selectedEntity.SelectedIndex - 1];
                        }
                        else if (param.Equals("Down"))
                        {
                            ItemsSource.Remove(selectedEntity.SelectedItem);
                            ItemsSource.Insert(selectedEntity.SelectedIndex + 1, selectedEntity.SelectedItem);
                            SelectedItem = ItemsSource[selectedEntity.SelectedIndex + 1];
                        }
                    }, (param) =>
                    {
                        if (SelectedItem == null)
                        {
                            return false;
                        }
                        else if (param.Equals("Up"))
                        {
                            return SelectedIndex != 0;
                        }
                        else if (param.Equals("Down"))
                        {
                            return SelectedIndex != ItemsSource.Count - 1;
                        }
                        else
                        {
                            return true;
                        }
                    });
                }
                return reorderCommand;
            }
        }

        public PluginViewModel SelectedItem
        {
            get { return selectedItem; }
            set { selectedItem = value; RaisePropertyChanged(()=>SelectedItem); }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; RaisePropertyChanged(()=>SelectedIndex); }
        }

        public ObservableCollection<PluginViewModel> ItemsSource { get { return itemsSource; } }

        public void SyncPluginConfiguration(IEnumerable<Plugin> targetPluginConfigurations)
        {
            if (targetPluginConfigurations.All(t => t is UIPlugin))
            {
                int index = 0;
                foreach (var item in ItemsSource)
                {
                    if (item.Plugin != null)
                    {
                        item.Plugin.Index = ++index;
                        item.Plugin.IsActive = item.IsEnabled;
                    }
                }
            }
            //int index = 0;
            //GeoCollection<PluginInfo> result = new GeoCollection<PluginInfo>();
            //foreach (var item in ItemsSource)
            //{
            //    PluginInfo configuration = item.Configuration;
            //    string pluginFullName = item.Plugin.Id;
            //    if (configuration != null) { configuration.IsActive = item.IsEnabled; }
            //    else
            //    {
            //        configuration = new PluginInfo()
            //        {
            //            FullName = pluginFullName,
            //            IsActive = false,
            //            IsRequired = false,
            //            Name = item.Name,
            //            Plugin = discoveredPlugins.Contains(pluginFullName) ? discoveredPlugins[pluginFullName] : null
            //        };
            //    }

            //    configuration.Index = ++index;
            //    result.Add(configuration.FullName, configuration);
            //}

            //pluginConfigurations = result;
            //GeoCollection<PluginInfo> tempTargetPluginConfigurations = targetPluginConfigurations as GeoCollection<PluginInfo>;
            //if (tempTargetPluginConfigurations != null)
            //{
            //    tempTargetPluginConfigurations.Clear();
            //    foreach (var configuration in pluginConfigurations)
            //    {
            //        tempTargetPluginConfigurations.Add(configuration.FullName, configuration);
            //    }
            //}
        }

        private void Initialize(IEnumerable<Plugin> discoveredPlugins)
        {
            this.discoveredPlugins = new GeoCollection<Plugin>();
            foreach (var plugin in discoveredPlugins.OrderBy(p => p.Name))
            {
                this.discoveredPlugins.Add(plugin.Id, plugin);
            }
        }

        private void CollectItemsSource()
        {
            foreach (var plugin in discoveredPlugins)
            {
                string pluginFullName = plugin.Id;
                PluginViewModel pluginModel = new PluginViewModel(plugin);
                if (pluginModel.IconSource == null) { pluginModel.IconSource = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/Gear.png", UriKind.RelativeOrAbsolute)); }
                itemsSource.Add(pluginModel);
            }
        }
    }
}