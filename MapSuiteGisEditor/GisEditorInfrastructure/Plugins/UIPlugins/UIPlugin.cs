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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This is an abstract class.
    /// Also this is a base class which will be used for creating our own plugins
    /// </summary>
    /// <remarks>
    /// When override this class,
    /// consider the OnConnect and OnDisconnect method are the main methods in this class.
    /// OnConnect is used for adding UIs in the explorer
    /// while OnDisconnect indicates remove the UIs from the explorer.
    ///
    /// In OnConnect method, you can handle several properties:
    ///
    /// 1, RibbonGroups is a collection of the Microsoft.Windows.Controls.Ribbon.RibbonGroup.
    /// It allows adding a customize RibbonGroup with several ribbon buttons in it.
    /// Before using it, please reference RibbonControlsLibrary.dll in our reference folder.
    ///
    /// 2, DockablePanels is a collection of wrapper AvalonDock.DockableContent.
    /// It allows adding a customize DockableContent in it;
    /// DockableContent has a Content property whose type is object;
    /// in another word, the content accepts any user control and window.
    ///
    /// 3, OptionContent is a way to setting the options for this plugin.
    /// When set this property, a Option button will display in the PluginManager window.
    ///
    /// That all for the critial properties.
    /// A plugin all needs its providers information; so we have an attribute for you.
    /// To set it, the explorer will analyse it.
    /// </remarks>
    [Serializable]
    [InheritedExport(typeof(UIPlugin))]
    public abstract class UIPlugin : Plugin
    {
        private Dictionary<UIElement, SelectionAdorner> adornerDictionary = new Dictionary<UIElement, SelectionAdorner>();

        private bool isHighlighted;
        private Collection<object> statusBarItems;

        //private Collection<RibbonGroup> ribbonGroups;
        private Collection<RibbonEntry> ribbonEntries;

        private Collection<DockWindow> dockablePanels;
        private Collection<RibbonApplicationMenuItem> applicationMenuItems;
        private Collection<RibbonContextualTabGroup> ribbonContextualTabGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIPlugin" /> class.
        /// </summary>
        protected UIPlugin()
        {
            statusBarItems = new Collection<object>();

            //ribbonGroups = new Collection<RibbonGroup>();
            ribbonEntries = new Collection<RibbonEntry>();
            dockablePanels = new Collection<DockWindow>();
            applicationMenuItems = new Collection<RibbonApplicationMenuItem>();
            ribbonContextualTabGroups = new Collection<RibbonContextualTabGroup>();
        }

        ///// <summary>
        ///// Each plugin can have multiple RibbonGroup
        ///// or different ribbon groups can be in different tabs.
        ///// Key is the actual ribbon group that being added while value is a tab name which the ribbon group will be contained.
        ///// If it returns null, it means the plugin doesn't affect ribbon.
        ///// </summary>
        //public Collection<RibbonGroup> RibbonGroups
        //{
        //    get { return ribbonGroups; }
        //}

        /// <summary>
        /// Gets the ribbon entries.
        /// </summary>
        /// <value>
        /// The ribbon entries.
        /// </value>
        public Collection<RibbonEntry> RibbonEntries
        {
            get { return ribbonEntries; }
        }

        // I think DockablePane has the ability to set its postion.
        // If we can have a collection for the DocableContent and we write the DockablePane for the users, the code should be shorter.
        /// <summary>
        /// Each plugin can have multiple dockable contents.
        /// If it returns null, it meamns the plugin doesn't have DOCK window.
        /// </summary>
        public Collection<DockWindow> DockWindows
        {
            get { return dockablePanels; }
        }

        /// <summary>
        /// Gets the ribbon contextual tab groups.
        /// </summary>
        /// <value>
        /// The ribbon contextual tab groups.
        /// </value>
        //public Collection<RibbonContextualTabGroup> RibbonContextualTabGroups { get { return ribbonContextualTabGroups; } }

        /// <summary>
        /// Use this property to add status bar items on the explorere.
        /// </summary>
        public Collection<object> StatusBarItems { get { return statusBarItems; } }

        /// <summary>
        /// Gets the application menu items.
        /// </summary>
        /// <value>
        /// The application menu items.
        /// </value>
        public Collection<RibbonApplicationMenuItem> ApplicationMenuItems { get { return applicationMenuItems; } }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is highlighted.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is highlighted; otherwise, <c>false</c>.
        /// </value>
        public bool IsHighlighted
        {
            get { return isHighlighted; }
            set
            {
                isHighlighted = value;
                if (isHighlighted) Highlight(this);
                else ClearHighlight(this);
            }
        }

        /// <summary>
        /// Attaches the map.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        public void AttachMap(GisEditorWpfMap wpfMap)
        {
            try
            {
                AttachMapCore(wpfMap);
            }
            catch (TypeLoadException ex)
            {
                HandleTypeLoadException(ex);
            }
        }

        /// <summary>
        /// Attaches the map core.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        protected virtual void AttachMapCore(GisEditorWpfMap wpfMap)
        { }

        /// <summary>
        /// Detaches the map.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        public void DetachMap(GisEditorWpfMap wpfMap)
        {
            try
            {
                DetachMapCore(wpfMap);
            }
            catch (TypeLoadException ex)
            {
                HandleTypeLoadException(ex);
            }
        }

        /// <summary>
        /// Detaches the map core.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        protected virtual void DetachMapCore(GisEditorWpfMap wpfMap)
        { }

        /// <summary>
        /// This method raises when load this plugin.
        /// </summary>
        protected override void LoadCore()
        {
            RibbonEntries.Clear();
            DockWindows.Clear();
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            RibbonEntries.Clear();
            DockWindows.Clear();
        }

        /// <summary>
        /// This method sychronizes status from map to controls in this plugin.
        /// For example, a map is changed by another plugin such as adding a new layer;
        /// we need to notice the shell that the map's status is changed;
        /// then shells send a message to all plugins that to synchronize status from map by their selfies.
        /// </summary>
        public void Refresh(RefreshArgs refreshArgs)
        {
            try
            {
                if (GisEditor.ActiveMap != null)
                {
#if GISEditorUnitTest
#else
                    RefreshCore(GisEditor.ActiveMap, refreshArgs);
#endif
                }
            }
            catch (TypeLoadException ex)
            {
                HandleTypeLoadException(ex);
            }
        }

        /// <summary>
        /// Refreshes the core.
        /// </summary>
        /// <param name="currentMap">The current map.</param>
        /// <param name="refreshArgs">The refresh args.</param>
        protected virtual void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        { }

        /// <summary>
        /// Gets the map context menu items.
        /// </summary>
        /// <param name="parameters">The e.</param>
        /// <returns>Menu item collection</returns>
        public Collection<MenuItem> GetMapContextMenuItems(GetMapContextMenuParameters parameters)
        {
            return GetMapContextMenuItemsCore(parameters);
        }

        /// <summary>
        /// Gets the map context menu items core.
        /// </summary>
        /// <param name="parameters">The e.</param>
        /// <returns>Menu item collection</returns>
        protected virtual Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            return new Collection<MenuItem>();
        }

        /// <summary>
        /// Gets the layer list item context menu items.
        /// </summary>
        /// <param name="parameters">The e.</param>
        /// <returns>Menu item collection</returns>
        public Collection<MenuItem> GetLayerListItemContextMenuItems(GetLayerListItemContextMenuParameters parameters)
        {
            return GetLayerListItemContextMenuItemsCore(parameters);
        }

        /// <summary>
        /// Gets the layer list item context menu items core.
        /// </summary>
        /// <param name="parameters">The e.</param>
        /// <returns>Menu item collection</returns>
        protected virtual Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            return new Collection<MenuItem>();
        }

        /// <summary>
        /// Gets the layer list item.
        /// </summary>
        /// <param name="concreteObject">The concrete object.</param>
        /// <returns>a list item in the layer</returns>
        public LayerListItem GetLayerListItem(object concreteObject)
        {
            return GetLayerListItemCore(concreteObject);
        }

        /// <summary>
        /// Gets the layer list item core.
        /// </summary>
        /// <param name="concreteObject">The concrete object.</param>
        /// <returns>a list item in the layer</returns>
        protected virtual LayerListItem GetLayerListItemCore(object concreteObject)
        {
            return null;
        }

        /// <summary>
        /// Highlights the specified plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        private void Highlight(UIPlugin plugin)
        {
            foreach (var dockWindow in plugin.DockWindows)
            {
                var content = dockWindow.Content as UIElement;

                if (content != null)
                {
                    //bring the dock window to the top
                    GisEditor.DockWindowManager.OpenDockWindow(dockWindow);
                    content.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //add the red border
                        AdornUIElement(content);
                    }), DispatcherPriority.Background);
                }
            }

            foreach (var ribbonGroup in plugin.RibbonEntries.Select(r => r.RibbonGroup))
            {  //switch the tab
                var containerTab = ribbonGroup.Ribbon.Items
                                                     .OfType<RibbonTab>()
                                                     .Where(tab => tab.Items.Contains(ribbonGroup))
                                                     .FirstOrDefault();

                if (containerTab != null)
                {
                    containerTab.IsSelected = true;
                    containerTab.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //add the red border
                        AdornUIElement(ribbonGroup);
                    }), DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Adorns the UI element.
        /// </summary>
        /// <param name="uiElement">The UI element.</param>
        private void AdornUIElement(UIElement uiElement)
        {
            if (uiElement != null && !adornerDictionary.ContainsKey(uiElement))
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);
                if (adornerLayer != null)
                {
                    var selectionAdorner = new SelectionAdorner(uiElement);
                    adornerLayer.Add(selectionAdorner);
                    adornerDictionary.Add(uiElement, selectionAdorner);
                }
            }
        }

        /// <summary>
        /// Clears the highlight.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        private void ClearHighlight(UIPlugin plugin)
        {
            foreach (var dockWindow in plugin.DockWindows)
            {
                var content = dockWindow.Content as UIElement;
                if (content != null && adornerDictionary.ContainsKey(content))
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(content);
                    if (adornerLayer != null)
                    {
                        adornerLayer.Remove(adornerDictionary[content]);
                        adornerDictionary.Remove(content);
                    }
                }
            }

            foreach (var ribbonGroup in plugin.RibbonEntries.Select(r => r.RibbonGroup))
            {
                if (adornerDictionary.ContainsKey(ribbonGroup))
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(ribbonGroup);
                    if (adornerLayer != null)
                    {
                        adornerLayer.Remove(adornerDictionary[ribbonGroup]);
                        adornerDictionary.Remove(ribbonGroup);
                    }
                }
            }
        }
    }
}