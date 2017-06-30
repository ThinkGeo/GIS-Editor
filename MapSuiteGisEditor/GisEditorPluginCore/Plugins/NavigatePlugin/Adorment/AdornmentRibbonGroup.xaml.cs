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
using System.Linq;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SnappingAdornmentRibbonGroup.xaml
    /// </summary>
    public partial class AdornmentRibbonGroup : RibbonGroup
    {
        private LegendManagerWindow legendManagerWindow;

        public AdornmentRibbonGroup()
        {
            InitializeComponent();
            Messenger.Default.Register<string>(this, DataContext, (message) =>
            {
                switch (message)
                {
                    case "Title":
                        ShowTitlesWindow(); break;
                    case "NorthArrow":
                        ShowNorthArrowsWindow(); break;
                    case "Logo":
                        ShowLogoWindow(); break;
                    //case "Graticules":
                    //    ShowGraticules(); break;
                    case "ManageLegends":
                        ShowManageLegends(); break;
                    case "Scale":
                    default:
                        ShowScaleBarWindow(); break;
                }
            });
            Unloaded += (s, e) => adornmentRibbonGroupViewModel.Cleanup();
        }

        private void ShowNorthArrowsWindow()
        {
            NorthArrowsWindow northArrowWindow = new NorthArrowsWindow();
            northArrowWindow.ShowDialog();
        }

        private void ShowLogoWindow()
        {
            LogoWindow logoWindow = new LogoWindow();
            logoWindow.ShowDialog();
        }

        private void ShowTitlesWindow()
        {
            TitleManageWindow titleWindow = new TitleManageWindow();
            titleWindow.ShowDialog();
        }

        private void ShowScaleBarWindow()
        {
            ScaleBarsConfigureWindow window = new ScaleBarsConfigureWindow();
            window.ShowDialog();
        }

        //private void ShowGraticules()
        //{
        //    if (GisEditor.ActiveMap != null)
        //    {
        //        GisEditorWpfMap extendedMap = GisEditor.ActiveMap;
        //        GraticuleAdornmentLayer graticuleLayer = extendedMap.AdornmentOverlay.Layers.OfType<GraticuleAdornmentLayer>().FirstOrDefault();
        //        if (graticuleLayer == null)
        //        {
        //            graticuleLayer = new GraticuleAdornmentLayer() { IsVisible = false, WrappingMode = WrappingMode.None };
        //            extendedMap.AdornmentOverlay.Layers.Add(graticuleLayer);
        //        }

        //        var projection = new Proj4Projection(Proj4Projection.GetEpsgParametersString(4326), GisEditor.ActiveMap.DisplayProjectionParameters);
        //        projection.SyncProjectionParametersString();
        //        graticuleLayer.Projection = projection;
        //        graticuleLayer.Projection.Open();

        //        graticuleLayer.IsVisible = !adornmentRibbonGroupViewModel.IsGraticulesVisible;
        //        adornmentRibbonGroupViewModel.IsGraticulesVisible = graticuleLayer.IsVisible;
        //        extendedMap.AdornmentOverlay.Refresh();
        //    }
        //}

        private void ShowManageLegends()
        {
            legendManagerWindow = new LegendManagerWindow();
            var result = GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<LegendManagerAdornmentLayer>();
            var legendLayers = result.SelectMany(l => l.LegendLayers);
            foreach (var legend in legendLayers)
            {
                var copiedLayer = legend.CloneDeep();
                copiedLayer.IsVisible = legend.IsVisible;
                legendManagerWindow.Legends.Add(copiedLayer);
            }

            if (legendManagerWindow.ShowDialog().GetValueOrDefault())
            {
                RefreshLegendOverlay(legendManagerWindow.Legends);
            }
        }

        internal static void RefreshLegendOverlay(IEnumerable<LegendAdornmentLayerViewModel> legends)
        {
            foreach (var legend in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<LegendManagerAdornmentLayer>().ToArray())
            {
                GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.Remove(legend);
            }

            LegendManagerAdornmentLayer newLayer = new LegendManagerAdornmentLayer();
            foreach (var legend in legends)
            {
                newLayer.LegendLayers.Add(legend);
            }

            if (newLayer.LegendLayers.Count > 0)
            {
                GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.Add(newLayer);
            }

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                GisEditor.ActiveMap.FixedAdornmentOverlay.Refresh();
            }));
        }
    }
}