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
using ThinkGeo.Core;

namespace ThinkGeo.Core
{
    [Serializable]
    public class LegendPrinterLayer1 : PrinterLayer
    {
        private readonly Collection<LegendItem> legendItems;
        [NonSerialized]
        private LegendAdornmentLayer legendAdornmentLayer;

        public LegendPrinterLayer1()
        {
            legendItems = new Collection<LegendItem>();
            BackgroundMask = new AreaStyle(new GeoSolidBrush(GeoColors.Transparent));
        }

        public LegendPrinterLayer1(LegendAdornmentLayer legendAdornmentLayer)
            : this()
        {
            if (legendAdornmentLayer != null)
            {
                this.legendAdornmentLayer = legendAdornmentLayer;
                Title = legendAdornmentLayer.Title;
                Footer = legendAdornmentLayer.Footer;
                BackgroundMask = legendAdornmentLayer.BackgroundMask;
                Width = legendAdornmentLayer.Width;
                Height = legendAdornmentLayer.Height;
                XOffsetInPixel = legendAdornmentLayer.XOffsetInPixel;
                YOffsetInPixel = legendAdornmentLayer.YOffsetInPixel;
                foreach (var item in legendAdornmentLayer.LegendItems)
                {
                    legendItems.Add(item);
                }
            }
        }

        public Collection<LegendItem> LegendItems
        {
            get { return legendItems; }
        }

        public LegendItem Title { get; set; }

        public LegendItem Footer { get; set; }

        public AreaStyle BackgroundMask { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double XOffsetInPixel { get; set; }

        public double YOffsetInPixel { get; set; }

        public double GetWidth()
        {
            return Width;
        }

        public double GetHeight()
        {
            return Height;
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            base.DrawCore(canvas, labelsInAllLayers);

            if (legendAdornmentLayer == null)
            {
                legendAdornmentLayer = new LegendAdornmentLayer();
            }

            legendAdornmentLayer.LegendItems.Clear();
            foreach (var item in legendItems)
            {
                legendAdornmentLayer.LegendItems.Add(item);
            }

            legendAdornmentLayer.Title = Title;
            legendAdornmentLayer.Footer = Footer;
            legendAdornmentLayer.BackgroundMask = BackgroundMask;
            //legendAdornmentLayer.Width = Width;
            //legendAdornmentLayer.Height = Height;
            legendAdornmentLayer.XOffsetInPixel = (float)XOffsetInPixel;
            legendAdornmentLayer.YOffsetInPixel = (float)YOffsetInPixel;
            legendAdornmentLayer.Location = AdornmentLocation.UseOffsets;

            if (!legendAdornmentLayer.IsOpen)
            {
                legendAdornmentLayer.Open();
            }

            legendAdornmentLayer.Draw(canvas, labelsInAllLayers);
        }
    }
}
