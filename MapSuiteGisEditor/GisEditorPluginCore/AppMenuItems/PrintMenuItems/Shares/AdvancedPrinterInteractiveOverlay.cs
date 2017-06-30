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
using System.Collections.ObjectModel;
using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AdvancedPrinterInteractiveOverlay : PrinterInteractiveOverlay
    {
        private PrinterLayer gridLayer;

        public AdvancedPrinterInteractiveOverlay()
        {
        }

        public PrinterLayer GridLayer
        {
            get { return gridLayer; }
            set { gridLayer = value; }
        }

        protected override void DrawTileCore(GeoCanvas geoCanvas)
        {
            base.DrawTileCore(geoCanvas);
            if (gridLayer != null)
            {
                PagePrinterLayer pagePrinterLayer = PrinterLayers.OfType<PagePrinterLayer>().FirstOrDefault();
                if (pagePrinterLayer != null)
                {
                    if (!gridLayer.IsOpen) gridLayer.Open();
                    gridLayer.SetPosition(pagePrinterLayer.GetPosition());
                    gridLayer.Draw(geoCanvas, new Collection<SimpleCandidate>());
                }
            }
        }
    }
}
