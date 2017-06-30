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
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using AvalonDock;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a dock window manager.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(DockWindowManager))]
    public class DockWindowManager : Manager
    {
        private static readonly string themeKey = "ThemeKey";
        private int activeDocumentIndex;
        private Theme theme;
        private ObservableCollection<DockWindow> dockWindows;
        private ObservableCollection<DocumentWindow> documentWindows;

        public event EventHandler<DockWindowOpenedDockWindowManagerEventArgs> DockWindowOpened;

        public event EventHandler<ThemeChangedDockWindowManagerEventArgs> ThemeChanged;

        public event EventHandler<SortingDockWindowsEventArgs> SortingDockWindows;
        public event EventHandler<SortedDockWindowsEventArgs> SortedDockWindows;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindowManager" /> class.
        /// </summary>
        public DockWindowManager()
        {
            theme = Theme.Blue;
            dockWindows = new ObservableCollection<DockWindow>();
            documentWindows = new ObservableCollection<DocumentWindow>();
            activeDocumentIndex = -1;
        }

        /// <summary>
        /// Gets or sets the theme of dock window.
        /// </summary>
        /// <value>
        /// The theme of dock window.
        /// </value>
        [DataMember]
        public Theme Theme
        {
            get { return theme; }
            set
            {
                var oldTheme = theme;
                if (oldTheme != value)
                {
                    theme = value;
                    OnThemeChanged(new ThemeChangedDockWindowManagerEventArgs(theme, oldTheme));
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the active document.
        /// </summary>
        /// <value>
        /// The index of the active document.
        /// </value>
        public int ActiveDocumentIndex
        {
            get { return activeDocumentIndex; }
            set { activeDocumentIndex = value; }
        }

        /// <summary>
        /// Gets all the dock windows.
        /// </summary>
        /// <value>
        /// All the dock windows.
        /// </value>
        public ObservableCollection<DockWindow> DockWindows
        {
            get { return dockWindows; }
        }

        /// <summary>
        /// Gets all the document windows.
        /// </summary>
        /// <value>
        /// All document windows.
        /// </value>
        public ObservableCollection<DocumentWindow> DocumentWindows
        {
            get { return documentWindows; }
        }

        /// <summary>
        /// Gets the settings of dock window manager to save.
        /// </summary>
        /// <returns></returns>
        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();
            settings.GlobalSettings[themeKey] = Theme.ToString();
            return settings;
        }

        /// <summary>
        /// Applies the settings of dock window manager.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            PluginHelper.RestoreEnum<Theme>(settings.GlobalSettings, themeKey, t => Theme = t);
        }

        /// <summary>
        /// Opens the dock window.
        /// </summary>
        /// <param name="dockWindow">The dock window.</param>
        public void OpenDockWindow(DockWindow dockWindow)
        {
            OpenDockWindow(dockWindow, DockWindowPosition.Default);
        }

        /// <summary>
        /// Opens the dock window with specific position.
        /// </summary>
        /// <param name="dockWindow">The dock window.</param>
        /// <param name="dockWindowPosition">The dock window position.</param>
        public void OpenDockWindow(DockWindow dockWindow, DockWindowPosition dockWindowPosition)
        {
            OpenDockWindowCore(dockWindow, dockWindowPosition);
        }

        /// <summary>
        /// Opens the dock window with specific position.
        /// </summary>
        /// <param name="dockWindow">The dock window.</param>
        /// <param name="dockWindowPosition">The dock window position.</param>
        protected virtual void OpenDockWindowCore(DockWindow dockWindow, DockWindowPosition dockWindowPosition)
        {
            if (!DockWindows.Contains(dockWindow)) DockWindows.Add(dockWindow);
            OnDockWindowOpened(new DockWindowOpenedDockWindowManagerEventArgs(dockWindow, dockWindowPosition));
        }

        /// <summary>
        /// Raises the <see cref="E:DockWindowOpened" /> event.
        /// </summary>
        /// <param name="e">The <see cref="DockWindowOpenedDockWindowManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnDockWindowOpened(DockWindowOpenedDockWindowManagerEventArgs e)
        {
            EventHandler<DockWindowOpenedDockWindowManagerEventArgs> handler = DockWindowOpened;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Raises the <see cref="E:ThemeChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ThemeChangedDockWindowManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnThemeChanged(ThemeChangedDockWindowManagerEventArgs e)
        {
            EventHandler<ThemeChangedDockWindowManagerEventArgs> handler = ThemeChanged;
            if (handler != null) handler(this, e);
        }

        public DockableContentState GetDockWindowState(DockWindow dockWindow)
        {
            if (Application.Current.MainWindow != null)
            {
                return GetDockWindowStateCore(dockWindow);
            }
            return DockableContentState.None;
        }

        protected virtual DockableContentState GetDockWindowStateCore(DockWindow dockWindow)
        {
            DockingManager dockingManager = (Application.Current.MainWindow.Content as UserControl).FindName("DockManager") as DockingManager;
            if (dockingManager != null)
            {
                DockableContent dockableContent = dockingManager.DockableContents.FirstOrDefault(d => d.Content == dockWindow.Content);
                if (dockableContent != null)
                {
                    return dockableContent.State;
                }
            }
            return DockableContentState.None;
        }

        public Collection<DockWindow> GetSortedDockWindows()
        {
            return OnSortedDockWindows(OnSortingDockWindows());
        }

        protected virtual Collection<DockWindow> OnSortingDockWindows()
        {
            EventHandler<SortingDockWindowsEventArgs> handler = SortingDockWindows;
            if (handler != null)
            {
                SortingDockWindowsEventArgs args = new SortingDockWindowsEventArgs(dockWindows);
                handler(this, args);
                if (args.Cancel)
                {
                    return dockWindows;
                }
                else return args.DockWindows;
            }
            return dockWindows;
        }

        protected virtual Collection<DockWindow> OnSortedDockWindows(IEnumerable<DockWindow> sortingDockWindows)
        {
            EventHandler<SortedDockWindowsEventArgs> handler = SortedDockWindows;
            if (handler != null)
            {
                SortedDockWindowsEventArgs args = new SortedDockWindowsEventArgs(sortingDockWindows);
                handler(this, args);
                return args.DockWindows;
            }
            else return new Collection<DockWindow>(sortingDockWindows.ToList());
        }
    }
}