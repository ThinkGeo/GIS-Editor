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
using System.Globalization;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NavigateViewModel
    {
        [NonSerialized]
        private ObservableCollection<ZoomLevelItemViewModel> currentZoomLevels;

        [NonSerialized]
        private ObservedCommand openSearchWindowCommand;

        [NonSerialized]
        private RelayCommand<int> zoomToLevelCommand;

        [NonSerialized]
        private ObservedCommand switchToPanModeCommand;

        [NonSerialized]
        private ObservedCommand switchToTrackZoomModeCommand;

        [NonSerialized]
        private ObservedCommand switchToIdentifyModeCommand;

        [NonSerialized]
        private ObservedCommand refreshMapCommand;

        [NonSerialized]
        private ObservedCommand setScaleCommand;

        public NavigateViewModel()
        {
            currentZoomLevels = new ObservableCollection<ZoomLevelItemViewModel>();
        }

        public DockWindow SearchDockWindow { get; set; }

        public ObservableCollection<ZoomLevelItemViewModel> CurrentZoomLevels { get { return currentZoomLevels; } }

        public RelayCommand<int> ZoomToLevelCommand
        {
            get
            {
                if (zoomToLevelCommand == null)
                {
                    zoomToLevelCommand = new RelayCommand<int>(level =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            var zoomLevels = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels();
                            if (zoomLevels.Count > level) GisEditor.ActiveMap.ZoomToScale(zoomLevels[level].Scale);
                        }
                    });
                }
                return zoomToLevelCommand;
            }
        }

        public ObservedCommand SetScaleCommand
        {
            get
            {
                if (setScaleCommand == null)
                {
                    setScaleCommand = new ObservedCommand(() =>
                    {
                        SetScale();
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return setScaleCommand;
            }
        }

        public ObservedCommand OpenSearchWindowCommand
        {
            get
            {
                if (openSearchWindowCommand == null)
                {
                    openSearchWindowCommand = new ObservedCommand(() =>
                    {
                        SearchDockWindow.Show(DockWindowPosition.Right);
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return openSearchWindowCommand;
            }
        }

        public ObservedCommand SwitchToPanModeCommand
        {
            get
            {
                if (switchToPanModeCommand == null)
                {
                    switchToPanModeCommand = new ObservedCommand(() =>
                    {
                        ChangeMapSwitcherMode(SwitcherMode.Pan);
                    }, CommandHelper.CheckMapIsNotNull);
                }

                return switchToPanModeCommand;
            }
        }

        public ObservedCommand SwitchToTrackZoomModeCommand
        {
            get
            {
                if (switchToTrackZoomModeCommand == null)
                {
                    switchToTrackZoomModeCommand = new ObservedCommand(() =>
                    {
                        ChangeMapSwitcherMode(SwitcherMode.TrackZoom);
                    }, CommandHelper.CheckMapIsNotNull);
                }

                return switchToTrackZoomModeCommand;
            }
        }

        public ObservedCommand SwitchToIdentifyModeCommand
        {
            get
            {
                if (switchToIdentifyModeCommand == null)
                {
                    switchToIdentifyModeCommand = new ObservedCommand(() =>
                    {
                        ChangeMapSwitcherMode(SwitcherMode.Identify);
                    }, CommandHelper.CheckMapIsNotNull);
                }

                return switchToIdentifyModeCommand;
            }
        }

        public ObservedCommand RefreshMapCommand
        {
            get
            {
                refreshMapCommand = new ObservedCommand(() =>
                {
                    if (GisEditor.ActiveMap != null)
                    {
                        var overlays = GisEditor.ActiveMap.Overlays.Concat(GisEditor.ActiveMap.InteractiveOverlays).ToList();
                        foreach (var overlay in overlays)
                        {
                            TileOverlay tileOverlay = overlay as TileOverlay;
                            if (tileOverlay != null)
                            {
                                tileOverlay.Invalidate();
                            }
                            else overlay.RefreshWithBufferSettings();
                        }
                    }
                }, () => GisEditor.ActiveMap != null);

                return refreshMapCommand;
            }
        }

        private static void ChangeMapSwitcherMode(SwitcherMode switcherMode)
        {
            SwitcherPanZoomBarMapTool panZoom = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (panZoom != null)
            {
                panZoom.SwitcherMode = switcherMode;
            }
        }

        public void SysnchCurrentZoomLevels(WpfMap currentMap)
        {
            CurrentZoomLevels.Clear();
            List<ZoomLevel> zoomLevels = currentMap.ZoomLevelSet.CustomZoomLevels.Where(c => !(c is PreciseZoomLevel)).ToList();
            for (int i = 0; i < zoomLevels.Count; i++)
            {
                string number = String.Format(CultureInfo.InvariantCulture, "Level {0:D2} - Scale 1:{1:N0}", i + 1, zoomLevels[i].Scale);
                ZoomLevelItemViewModel currentLevel = new ZoomLevelItemViewModel();
                currentLevel.Name = number;
                currentLevel.ScaleIndex = i;
                CurrentZoomLevels.Add(currentLevel);
            }
        }

        private static void SetScale()
        {
            ScaleSettingsRibbonGroupViewModel viewModel = new ScaleSettingsRibbonGroupViewModel();
            double currentScale = GisEditor.ActiveMap.CurrentScale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, viewModel.SelectedDistanceUnit);
            viewModel.UpdateValues();
            viewModel.SelectedScale = viewModel.Scales.FirstOrDefault(s => Math.Abs(s.Scale - currentScale) < 1);
            viewModel.Value = viewModel.SelectedScale.DisplayScale;

            AddZoomLevelWindow window = new AddZoomLevelWindow();
            window.Title = GisEditor.LanguageManager.GetStringResource("NavigateViewModelSetScaleContent");
            window.DataContext = viewModel;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Owner = System.Windows.Application.Current.MainWindow;
            if (window.ShowDialog().GetValueOrDefault())
            {
                ScaleSettingsRibbonGroupViewModel.SetNewScale(window.Scale);
            }
        }
    }
}