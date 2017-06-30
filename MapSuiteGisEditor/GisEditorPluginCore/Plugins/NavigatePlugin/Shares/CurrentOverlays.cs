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


using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class CurrentOverlays
    {
        private static readonly string popupOverlayKey = "PopupOverlay";
        private static readonly string plottedMarkerOverlayKey = "PlottedMarkerOverlay";

        public static SelectionTrackInteractiveOverlay SelectionOverlay
        {
            get
            {
                return GisEditor.SelectionManager.GetSelectionOverlay();
            }
        }

        public static ExtentInteractiveOverlay ExtentOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                    return GisEditor.ActiveMap.ExtentOverlay;
                else return null;
            }
        }

        public static PopupOverlay PopupOverlay
        {
            get
            {
                PopupOverlay popupOverlay = null;
                if (!GisEditor.ActiveMap.Overlays.Contains(popupOverlayKey))
                {
                    popupOverlay = new PopupOverlay();
                    GisEditor.ActiveMap.Overlays.Add(popupOverlayKey, popupOverlay);
                }
                else
                {
                    popupOverlay = GisEditor.ActiveMap.Overlays[popupOverlayKey] as PopupOverlay;
                }
                return popupOverlay;
            }
        }

        public static SimpleMarkerOverlay PlottedMarkerOverlay
        {
            get
            {
                SimpleMarkerOverlay markerOverlay = null;
                if (!GisEditor.ActiveMap.Overlays.Contains(plottedMarkerOverlayKey))
                {
                    markerOverlay = new SimpleMarkerOverlay();
                    markerOverlay.Name = "Plotted points";
                    markerOverlay.DragMode = MarkerDragMode.Drag;
                    GisEditor.ActiveMap.Overlays.Add(plottedMarkerOverlayKey, markerOverlay);
                }
                else
                {
                    markerOverlay = GisEditor.ActiveMap.Overlays[plottedMarkerOverlayKey] as SimpleMarkerOverlay;
                }
                return markerOverlay;
            }
        }
    }
}