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
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class ExtentInteractiveOverlayPlugin : InteractiveOverlayPlugin
    {
        public ExtentInteractiveOverlayPlugin()
        { }

        protected override Type GetInteractiveOverlayTypeCore()
        {
            return typeof(ExtentInteractiveOverlay);
        }

        protected override void DisableCore(InteractiveOverlay interactiveOverlay)
        {
            var overlay = interactiveOverlay as ExtentInteractiveOverlay;
            if (overlay != null)
            {
                overlay.PanMode = MapPanMode.Disabled;
                overlay.LeftClickDragKey = System.Windows.Forms.Keys.ShiftKey;
                overlay.OverlayCanvas.IsEnabled = false;
                if (GisEditor.ActiveMap != null)
                {
                    var switcherPanZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                    if (switcherPanZoomBar != null)
                    {
                        switcherPanZoomBar.SwitcherMode = SwitcherMode.None;
                    }
                }
            }
        }

        protected override bool GetIsEnabledCore(InteractiveOverlay interactiveOverlay)
        {
            bool isEnabled = true;
            var overlay = interactiveOverlay as ExtentInteractiveOverlay;
            if (overlay != null)
            {
                isEnabled = overlay.PanMode != MapPanMode.Disabled && overlay.OverlayCanvas.IsEnabled;
            }

            return isEnabled;
        }
    }
}