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
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.Core;

using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ViewRibbonViewModel : ViewModelBase
    {
        [NonSerialized]
        private RelayCommand<bool> togglePanZoomBarVisibilityCommand;
        [NonSerialized]
        private RelayCommand<bool> toggleGraticuleVisibilityCommand;

        public ViewRibbonViewModel() { }

        public RelayCommand<bool> TogglePanZoomBarVisibilityCommand
        {
            get
            {
                if (togglePanZoomBarVisibilityCommand == null)
                {
                    togglePanZoomBarVisibilityCommand = new RelayCommand<bool>(isEnabled =>
                    {
                        if (GisEditor.ActiveMap == null) return;
                        var panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                        if (panZoomBar != null) panZoomBar.IsEnabled = isEnabled;
                    });
                }
                return togglePanZoomBarVisibilityCommand;
            }
        }

        //public RelayCommand<bool> ToggleGraticuleVisibilityCommand
        //{
        //    get
        //    {
        //        if (toggleGraticuleVisibilityCommand == null)
        //        {
        //            toggleGraticuleVisibilityCommand = new RelayCommand<bool>(isEnabled =>
        //            {
        //                if (GisEditor.ActiveMap != null)
        //                {
        //                    GisEditorWpfMap extendedMap = GisEditor.ActiveMap;

        //                    GraticuleFeatureLayer graticuleLayer = extendedMap.AdornmentOverlay.Layers.OfType<GraticuleFeatureLayer>().FirstOrDefault();
        //                    if (graticuleLayer == null)
        //                    {
        //                        graticuleLayer = new GraticuleFeatureLayer();
        //                        extendedMap.AdornmentOverlay.Layers.Add(graticuleLayer);
        //                    }

        //                    graticuleLayer.IsVisible = isEnabled;
        //                    extendedMap.AdornmentOverlay.RefreshAsync();
        //                }
        //            });
        //        }
        //        return toggleGraticuleVisibilityCommand;
        //    }
        //}

        public GeoBrush SelectedBackground
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                {
                    return GisEditor.ActiveMap.BackgroundOverlay.BackgroundBrush;
                }
                else return new GeoSolidBrush(GeoColors.Transparent);
            }
            set
            {
                if (GisEditor.ActiveMap != null)
                {
                    GisEditor.ActiveMap.BackgroundOverlay.BackgroundBrush = value;
                    if (GisEditor.ActiveMap.IsLoaded)
                    {
                        GisEditor.ActiveMap.RefreshAsync(GisEditor.ActiveMap.BackgroundOverlay);
                    }
                }
                RaisePropertyChanged(() => SelectedBackground);
                RaisePropertyChanged(() => SelectedBackgroundPreview);
            }
        }

        public ImageSource SelectedBackgroundPreview
        {
            get
            {
                AreaStyle areaStyle = new AreaStyle();
                areaStyle.FillBrush = SelectedBackground;
                return areaStyle.GetPreviewImage(32, 32);
            }
        }
    }
}


