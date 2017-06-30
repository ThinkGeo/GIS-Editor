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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This is a manager of all plugins,
    /// It maintains loading, getting, removing , refreshing plugins etc.
    /// In order to discover this manager,
    /// assembly contains inherited class should be named with "*.Infrastructures.*.dll" pattern.
    /// </remarks>
    [Serializable]
    [InheritedExport(typeof(UIPluginManager))]
    public class UIPluginManager : PluginManager
    {
        private TimeSpan refreshingTime;

        /// <summary>
        /// Occurs when [got map context menu items].
        /// </summary>
        public event EventHandler<GottenMapContextMenuItemsUIPluginManagerEventArgs> GottenMapContextMenuItems;

        public event EventHandler<RefreshedPluginsUIPluginManagerEventArgs> RefreshedPlugins;

        //public event EventHandler<BuildingRibbonBarUIPluginManagerEventArgs> BuildingRibbonBar;

        //public event EventHandler<BuiltRibbonBarUIPluginManagerEventArgs> BuiltRibbonBar;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIPluginManager" /> class.
        /// </summary>
        public UIPluginManager()
            : base()
        { }

        public TimeSpan RefreshingTime
        {
            get { return refreshingTime; }
        }

        /// <summary>
        /// Gets the UI plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<UIPlugin> GetUIPlugins()
        {
            return new Collection<UIPlugin>(GetPlugins().Cast<UIPlugin>().ToList());
        }

        public Collection<T> GetActiveUIPlugins<T>() where T : UIPlugin
        {
            return new Collection<T>(GetActiveUIPlugins().OfType<T>().ToList());
        }

        /// <summary>
        /// Gets the UI plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<UIPlugin> GetActiveUIPlugins()
        {
            var activePlugins = from p in GetUIPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<UIPlugin>(activePlugins.ToList());
        }

        public void BuildRibbonBar(Ribbon ribbon)
        {
            BuildRibbonBar(ribbon, GetActiveUIPlugins());
        }

        public void BuildRibbonBar(Ribbon ribbon, IEnumerable<UIPlugin> uiPlugins)
        {
            var defaultHomeTab = ribbon.Items.OfType<RibbonTab>().FirstOrDefault(t => t.GetValue(RibbonExtension.RibbonTabHeaderProperty) == null);
            if (defaultHomeTab != null)
            {
                defaultHomeTab.SetValue(RibbonExtension.RibbonTabHeaderProperty, "HomeRibbonTabHeader");
                defaultHomeTab.SetValue(RibbonExtension.RibbonTabIndexProperty, 0d);
            }

            //OnBuildingRibbonBar(new BuildingRibbonBarUIPluginManagerEventArgs(ribbon));
            BuildRibbonBarCore(ribbon, uiPlugins);
            //OnBuiltRibbonBar(new BuiltRibbonBarUIPluginManagerEventArgs(ribbon));
        }

        protected virtual void BuildRibbonBarCore(Ribbon ribbon, IEnumerable<UIPlugin> uiPlugins)
        {
            InitializeRibbonGroups(uiPlugins.SelectMany(p => p.RibbonEntries), ribbon);
            ReorderRibbonTabs(ribbon);
            ReorderRibbonGroups(ribbon);
        }

        private void InitializeRibbonGroups(IEnumerable<RibbonEntry> ribbonEnties, Ribbon ribbonContainer)
        {
            foreach (RibbonEntry item in ribbonEnties)
            {
                string targetTabHeaderKey = item.RibbonTabName;
                if (String.IsNullOrEmpty(targetTabHeaderKey))
                {
                    targetTabHeaderKey = "HomeRibbonTabHeader";
                }

                RibbonTab ribbonTab = null;
                foreach (RibbonTab tab in ribbonContainer.Items)
                {
                    string currentTabHeaderKey = RibbonExtension.GetRibbonTabHeader(tab);
                    if (!String.IsNullOrEmpty(currentTabHeaderKey) && currentTabHeaderKey.Equals(targetTabHeaderKey, StringComparison.OrdinalIgnoreCase))
                    {
                        ribbonTab = tab;
                        break;
                    }
                }
                if (ribbonTab == null)
                {
                    ribbonTab = new RibbonTab();

                    string targetTabHeader = GisEditor.LanguageManager.GetStringResource(targetTabHeaderKey);
                    if (string.IsNullOrEmpty(targetTabHeader)) ribbonTab.Header = targetTabHeaderKey;
                    else ribbonTab.SetResourceReference(RibbonTab.HeaderProperty, targetTabHeaderKey);

                    ribbonContainer.Items.Add(ribbonTab);
                }

                RibbonExtension.SetRibbonTabHeader(ribbonTab, targetTabHeaderKey);
                RibbonExtension.SetRibbonTabIndex(ribbonTab, item.RibbonTabIndex);
                if (!ribbonTab.Items.Contains(item.RibbonGroup)) ribbonTab.Items.Add(item.RibbonGroup);
            }
        }

        private void ReorderRibbonGroups(Ribbon ribbonContainer)
        {
            var enabledPlugins = GetActiveUIPlugins().ToArray();
            for (int i = enabledPlugins.Length - 1; i >= 0; i--)
            {
                UIPlugin currentPlugin = enabledPlugins[i];
                foreach (var ribbonEntry in currentPlugin.RibbonEntries.OrderBy(r => r.RibbonGroupIndex).Reverse())
                {
                    RibbonTab containingRibbonTab = GetRibbonTabContaining(ribbonEntry, ribbonContainer);
                    containingRibbonTab.Items.Remove(ribbonEntry.RibbonGroup);

                    int insertIndex = 0;
                    string ribbonTabHeaderKey = RibbonExtension.GetRibbonTabHeader(containingRibbonTab);
                    if ("HomeRibbonTabHeader".Equals(ribbonTabHeaderKey, StringComparison.Ordinal))
                    {
                        insertIndex = 1;
                    }
                    containingRibbonTab.Items.Insert(insertIndex, ribbonEntry.RibbonGroup);
                }
            }
        }

        private void ReorderRibbonTabs(Ribbon ribbonContainer)
        {
            var tabs = ribbonContainer.Items.Cast<RibbonTab>().ToList();
            ribbonContainer.Items.Clear();
            var ordedTabs = tabs.OrderBy(t => t.GetValue(RibbonExtension.RibbonTabIndexProperty)).ToList();
            ordedTabs.ForEach(t => ribbonContainer.Items.Add(t));
        }

        private RibbonTab GetRibbonTabContaining(RibbonEntry ribbonEntry, Ribbon ribbonContainer)
        {
            RibbonTab ribbonTab = null;
            string tabHeaderKey = ribbonEntry.RibbonTabName;
            if (String.IsNullOrEmpty(tabHeaderKey))
            {
                tabHeaderKey = "HomeRibbonTabHeader";
            }

            if (!String.IsNullOrEmpty(tabHeaderKey))
            {
                ribbonTab = ribbonContainer.Items.OfType<RibbonTab>().FirstOrDefault(t =>
                {
                    string tmpTabHeaderKey = RibbonExtension.GetRibbonTabHeader(t);
                    return !String.IsNullOrEmpty(tmpTabHeaderKey) && tmpTabHeaderKey.Equals(tabHeaderKey, StringComparison.Ordinal);
                });
            }

            return ribbonTab;
        }

        /// <summary>
        /// Refreshes the plugins.
        /// </summary>
        public void RefreshPlugins()
        {
            RefreshPlugins(null);
        }

        /// <summary>
        /// Refreshes the plugins.
        /// </summary>
        /// <param name="refreshArgs">The refresh args.</param>
        public void RefreshPlugins(RefreshArgs refreshArgs)
        {
            RefreshPluginsCore(refreshArgs);
        }

        /// <summary>
        /// Refreshes the plugins core.
        /// </summary>
        /// <param name="refreshArgs">The refresh args.</param>
        protected virtual void RefreshPluginsCore(RefreshArgs refreshArgs)
        {
            RefreshedPluginsUIPluginManagerEventArgs e = new RefreshedPluginsUIPluginManagerEventArgs();
            foreach (UIPlugin plugin in GetActiveUIPlugins())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                plugin.Refresh(refreshArgs);
                stopwatch.Stop();
                e.RefreshingTimes[plugin] = stopwatch.Elapsed;
            }

            OnRefreshedPlugins(e);
            refreshingTime = e.TotalRefreshingTime;
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            //Collection<Plugin> uiPlugins = GetPluginsInternal<UIPlugin>();
            //if (PluginDirectories.Count > 0)
            //{
            //    Collection<Plugin> tempPlugins = GetPluginsInternal<UIPlugin>(PluginDirectories);
            //    foreach (var tempPlugin in tempPlugins)
            //    {
            //        uiPlugins.Add(tempPlugin);
            //    }
            //}

            //return uiPlugins;

            return CollectPlugins<UIPlugin>();
        }

        /// <summary>
        /// Gets the map context menu items.
        /// </summary>
        /// <param name="contextMenuArugments">The context menu arugments.</param>
        /// <returns></returns>
        public Collection<MenuItem> GetMapContextMenuItems(GetMapContextMenuParameters contextMenuArugments)
        {
            return GetMapContextMenuItemsCore(contextMenuArugments);
        }

        /// <summary>
        /// Gets the map context menu items core.
        /// </summary>
        /// <param name="contextMenuArugments">The context menu arugments.</param>
        /// <returns></returns>
        protected virtual Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters contextMenuArugments)
        {
            Collection<MenuItem> resultMenuItems = new Collection<MenuItem>();
            foreach (var uiPlugin in GetActiveUIPlugins().Reverse())
            {
                var menuItems = uiPlugin.GetMapContextMenuItems(contextMenuArugments);
                if (menuItems.Count > 0)
                {
                    resultMenuItems.Add(new MenuItem() { Header = "--" });
                    foreach (var menuItem in menuItems.Reverse())
                    {
                        resultMenuItems.Add(menuItem);
                    }
                }
            }

            GottenMapContextMenuItemsUIPluginManagerEventArgs e = new GottenMapContextMenuItemsUIPluginManagerEventArgs(resultMenuItems);
            OnGottenMapContextMenuItems(e);
            return e.MenuItems;
        }

        /// <summary>
        /// Raises the <see cref="E:GotMapContextMenuItems" /> event.
        /// </summary>
        /// <param name="e">The <see cref="GottenMapContextMenuItemsUIPluginManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnGottenMapContextMenuItems(GottenMapContextMenuItemsUIPluginManagerEventArgs e)
        {
            EventHandler<GottenMapContextMenuItemsUIPluginManagerEventArgs> handler = GottenMapContextMenuItems;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRefreshedPlugins(RefreshedPluginsUIPluginManagerEventArgs e)
        {
            EventHandler<RefreshedPluginsUIPluginManagerEventArgs> handler = RefreshedPlugins;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        //protected virtual void OnBuildingRibbonBar(BuildingRibbonBarUIPluginManagerEventArgs e)
        //{
        //    EventHandler<BuildingRibbonBarUIPluginManagerEventArgs> handler = BuildingRibbonBar;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        //protected virtual void OnBuiltRibbonBar(BuiltRibbonBarUIPluginManagerEventArgs e)
        //{
        //    EventHandler<BuiltRibbonBarUIPluginManagerEventArgs> handler = BuiltRibbonBar;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}
    }
}