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


using System.Linq;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for Navigate.xaml
    /// </summary>
    public partial class NavigateRibbonGroup : RibbonGroup
    {
        private static string errorMessage = GisEditor.LanguageManager.GetStringResource("NavigateRibbonGroupInvalidCoordinatesText");
        private NavigateViewModel viewModel;

        public NavigateRibbonGroup()
        {
            InitializeComponent();
            viewModel = new NavigateViewModel();
            DataContext = viewModel;
        }

        public DockWindow SearchDockWindow
        {
            get { return viewModel.SearchDockWindow; }
            set { viewModel.SearchDockWindow = value; }
        }

        public NavigateViewModel ViewModel { get { return viewModel; } }

        public void SynchronizeState(WpfMap currentMap)
        {
            viewModel.SysnchCurrentZoomLevels(currentMap);
            var panZoom = currentMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (panZoom == null) return;

            panZoom.DisableModeChangedEvent = true;
            if (panZoom.SwitcherMode == SwitcherMode.None
                && currentMap.ExtentOverlay != null
                && !currentMap.ExtentOverlay.IsEnabled())
            {
                panZoom.SwitcherMode = SwitcherMode.None;
            }
            else if (panZoom.SwitcherMode != SwitcherMode.Identify)
            {
                if (CurrentOverlays.ExtentOverlay.LeftClickDragKey == System.Windows.Forms.Keys.None)
                {
                    panZoom.SwitcherMode = SwitcherMode.TrackZoom;
                }
                else panZoom.SwitcherMode = SwitcherMode.Pan;
            }
            panZoom.DisableModeChangedEvent = false;
        }
    }
}