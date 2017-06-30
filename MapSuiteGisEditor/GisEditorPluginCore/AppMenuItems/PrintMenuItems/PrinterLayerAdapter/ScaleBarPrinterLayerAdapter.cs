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
    public class ScaleBarPrinterLayerAdapter : PrinterLayerAdapter
    {
        private SimplifyMapPrinterLayer mapPrinterLayer;
        private ScaleBarAdornmentLayer scaleBarAdornmentLayer;

        public ScaleBarPrinterLayerAdapter(SimplifyMapPrinterLayer mapPrinterLayer, ScaleBarAdornmentLayer scaleBarAdornmentLayer)
        {
            this.mapPrinterLayer = mapPrinterLayer;
            this.scaleBarAdornmentLayer = scaleBarAdornmentLayer;
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerLayer)
        {
            ScaleBarPrinterLayer scaleBarPrinterLayer = null;
            if (scaleBarAdornmentLayer != null && (scaleBarPrinterLayer = printerLayer as ScaleBarPrinterLayer) != null)
            {
                scaleBarPrinterLayer.AlternateBarBrush = scaleBarAdornmentLayer.AlternateBarBrush;
                var backgroundStyle = scaleBarAdornmentLayer.BackgroundMask.CloneDeep() as AreaStyle;
                if (backgroundStyle != null)
                {
                    backgroundStyle.SetDrawingLevel();
                    scaleBarPrinterLayer.BackgroundMask = backgroundStyle;
                }
                scaleBarPrinterLayer.BarBrush = scaleBarAdornmentLayer.BarBrush;
                scaleBarPrinterLayer.TextStyle.NumericFormat = scaleBarAdornmentLayer.TextStyle.NumericFormat;
                scaleBarPrinterLayer.UnitFamily = scaleBarAdornmentLayer.UnitFamily;
                scaleBarPrinterLayer.MapPrinterLayer = mapPrinterLayer;
                scaleBarPrinterLayer.MapUnit = scaleBarPrinterLayer.MapPrinterLayer.MapUnit;
            }
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if (scaleBarAdornmentLayer != null)
            {
                ScaleBarPrinterLayer printerLayer = new ScaleBarPrinterLayer(this.mapPrinterLayer);
                LoadFromActiveMap(printerLayer);
                double left = PrinterHelper.ConvertLength(scaleBarAdornmentLayer.XOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double top = PrinterHelper.ConvertLength(scaleBarAdornmentLayer.YOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                PrinterLayerAdapter.SetPosition(scaleBarAdornmentLayer.Location, boudingBox, printerLayer, boudingBox.Width * 0.1, 0.5, left, top);

                return printerLayer;
            }
            else return null;
        }

        internal static ScaleBarPrinterLayer GetScaleBarPrinterLayer(ScaleBarElementViewModel scaleBarViewModel)
        {
            ScaleBarPrinterLayer scaleBarPrinterLayer = new ScaleBarPrinterLayer(scaleBarViewModel.MapPrinterLayer) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            scaleBarPrinterLayer.LoadFromViewModel(scaleBarViewModel);
            scaleBarPrinterLayer.Open();
            RectangleShape pageBoundingbox = scaleBarViewModel.MapPrinterLayer.GetPosition(PrintingUnit.Inch);
            var pageCenter = pageBoundingbox.LowerLeftPoint;
            scaleBarPrinterLayer.SetPosition(1.25, .25, pageCenter.X + 0.75, pageCenter.Y + .5, PrintingUnit.Inch);
            return scaleBarPrinterLayer;
        }
    }
}
