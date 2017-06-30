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


using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LabelPrinterLayerAdapter : PrinterLayerAdapter
    {
        private TitleAdornmentLayer titleAdornmentLayer;

        public LabelPrinterLayerAdapter(TitleAdornmentLayer titleAdornmentLayer)
        {
            this.titleAdornmentLayer = titleAdornmentLayer;
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerLayer)
        {
            LabelPrinterLayer labelPrinterLayer = null;
            if (titleAdornmentLayer != null && (labelPrinterLayer = printerLayer as LabelPrinterLayer) != null)
            {
                labelPrinterLayer.Text = titleAdornmentLayer.Title;
                labelPrinterLayer.TextBrush = titleAdornmentLayer.FontColor;
                labelPrinterLayer.Font = titleAdornmentLayer.TitleFont;
            }
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if (this.titleAdornmentLayer != null)
            {
                LabelPrinterLayer printerLayer = new LabelPrinterLayer();
                LoadFromActiveMap(printerLayer);
                var newScreenBBox = new PlatformGeoCanvas().MeasureText(printerLayer.Text, printerLayer.Font);
                var scaledWidth = newScreenBBox.Width * 1.1;
                var scaledHeight = newScreenBBox.Height * 1.1;

                double width = PrinterHelper.ConvertLength(scaledWidth, PrintingUnit.Point, PrintingUnit.Inch);
                double height = PrinterHelper.ConvertLength(scaledHeight, PrintingUnit.Point, PrintingUnit.Inch);
                double left = PrinterHelper.ConvertLength(titleAdornmentLayer.XOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double top = PrinterHelper.ConvertLength(titleAdornmentLayer.YOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                SetPosition(titleAdornmentLayer.Location, boudingBox, printerLayer, width, height, left, top);
                return printerLayer;
            }
            else return null;
        }
    }
}
