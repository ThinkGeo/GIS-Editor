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
using System.Windows.Controls;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetRemoveMarkerMenuItem()
        {
            var command = new ObservedCommand(RemoveMarker, () => true);
            return GetMenuItem("Remove", "/GisEditorInfrastructure;component/Images/unload.png", command);
        }

        private static void RemoveMarker()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            Marker marker = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Marker;
            if (marker != null)
            {
                SimpleMarkerOverlay markerOverlay = GetMarkerOverlayByMarker(GisEditor.ActiveMap, marker);
                markerOverlay.Markers.Remove(marker);
                GisEditor.ActiveMap.Refresh(markerOverlay);
                GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(markerOverlay.Markers, RefreshArgsDescriptions.RemoveMarkerDescription));
            }
        }

        private static SimpleMarkerOverlay GetMarkerOverlayByMarker(WpfMap map, Marker marker)
        {
            return map.Overlays.OfType<SimpleMarkerOverlay>()
                               .Where(overlay => overlay.Markers.Contains(marker))
                               .FirstOrDefault();
        }
    }
}