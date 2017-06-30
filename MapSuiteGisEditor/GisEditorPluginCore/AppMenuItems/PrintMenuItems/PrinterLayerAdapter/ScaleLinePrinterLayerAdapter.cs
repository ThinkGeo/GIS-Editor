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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ScaleLinePrinterLayerAdapter : PrinterLayerAdapter
    {
        private SimplifyMapPrinterLayer mapPrinterLayer;
        private ScaleLineAdornmentLayer scaleLineAdornmentLayer;

        public ScaleLinePrinterLayerAdapter(SimplifyMapPrinterLayer mapPrinterLayer, ScaleLineAdornmentLayer scaleLineAdornmentLayer)
        {
            this.mapPrinterLayer = mapPrinterLayer;
            this.scaleLineAdornmentLayer = scaleLineAdornmentLayer;
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerLayer)
        {
            ScaleLinePrinterLayer scaleLinePrinterLayer = null;
            if (scaleLineAdornmentLayer != null && (scaleLinePrinterLayer = printerLayer as ScaleLinePrinterLayer) != null)
            {
                scaleLinePrinterLayer.MapPrinterLayer = mapPrinterLayer;
                scaleLinePrinterLayer.MapUnit = scaleLinePrinterLayer.MapPrinterLayer.MapUnit;
                var backgroundStyle = scaleLineAdornmentLayer.BackgroundMask.CloneDeep() as AreaStyle;
                if (backgroundStyle != null)
                {
                    backgroundStyle.SetDrawingLevel();
                    scaleLinePrinterLayer.BackgroundMask = backgroundStyle;
                }
            }
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if (scaleLineAdornmentLayer != null)
            {
                ScaleLinePrinterLayer scaleLinePrinterLayer = new ScaleLinePrinterLayer(mapPrinterLayer);
                LoadFromActiveMap(scaleLinePrinterLayer);
                double left = PrinterHelper.ConvertLength(scaleLineAdornmentLayer.XOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double top = PrinterHelper.ConvertLength(scaleLineAdornmentLayer.YOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                PrinterLayerAdapter.SetPosition(scaleLineAdornmentLayer.Location, boudingBox, scaleLinePrinterLayer, boudingBox.Width * 0.1, 0.5, left, top);
                return scaleLinePrinterLayer;
            }
            else return null;
        }

        internal static ScaleLinePrinterLayer GetScaleLinePrinterLayer(ScaleLineElementViewModel scaleLineViewModel)
        {
            ScaleLinePrinterLayer scaleLinePrinterLayer = new ScaleLinePrinterLayer(scaleLineViewModel.MapPrinterLayer) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            scaleLinePrinterLayer.LoadFromViewModel(scaleLineViewModel);

            RectangleShape pageBoundingbox = scaleLineViewModel.MapPrinterLayer.GetPosition(PrintingUnit.Inch);
            var pageCenter = pageBoundingbox.LowerLeftPoint;
            scaleLinePrinterLayer.SetPosition(1.25, .25, pageCenter.X + 0.75, pageCenter.Y + .5, PrintingUnit.Inch);
            return scaleLinePrinterLayer;
        }
    }
}
