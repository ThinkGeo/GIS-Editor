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


using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LegendPrinterLayerAdapter : PrinterLayerAdapter
    {
        private LegendAdornmentLayer legendAdornmentLayer;

        public LegendPrinterLayerAdapter(LegendAdornmentLayer legendAdornmentLayer)
        {
            this.legendAdornmentLayer = legendAdornmentLayer;
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerlayer)
        {
            LegendPrinterLayer legendPrinterLayer = null;
            if (legendAdornmentLayer != null && (legendPrinterLayer = printerlayer as LegendPrinterLayer) != null)
            {
                SetPropertiesInGeneral(legendAdornmentLayer, legendPrinterLayer);
            }
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if (legendAdornmentLayer != null)
            {
                LegendPrinterLayer printerLayer = new GisEditorLegendPrinterLayer();
                double width = 0;
                double height = 0;
                SetPropertiesInGeneral(legendAdornmentLayer, printerLayer);
                width = width > legendAdornmentLayer.Width ? width : legendAdornmentLayer.Width;
                height += legendAdornmentLayer.Height;
                width = PrinterHelper.ConvertLength(width, PrintingUnit.Point, PrintingUnit.Inch);
                height = PrinterHelper.ConvertLength(height, PrintingUnit.Point, PrintingUnit.Inch);
                double left = PrinterHelper.ConvertLength(legendAdornmentLayer.XOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double top = PrinterHelper.ConvertLength(legendAdornmentLayer.YOffsetInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                SetPosition(legendAdornmentLayer.Location, boudingBox, printerLayer, width, height, left, top);
                return printerLayer;
            }
            else return null;
        }

        private void SetPropertiesInGeneral(LegendAdornmentLayer legendLayer, LegendPrinterLayer legendPrinterLayer)
        {
            legendPrinterLayer.LegendItems.Clear();
            foreach (var item in legendLayer.LegendItems)
            {
                var copiedItem = PrinterLayerHelper.CloneDeep<LegendItem>(item);
                if (copiedItem != null)
                {
                    copiedItem.SetDrawingLevel();
                    legendPrinterLayer.LegendItems.Add(copiedItem);
                }
            }
            legendPrinterLayer.Width = legendLayer.Width;
            legendPrinterLayer.Height = legendLayer.Height;
            legendPrinterLayer.BackgroundMask = PrinterLayerHelper.CloneDeep<AreaStyle>(legendLayer.BackgroundMask);
            legendPrinterLayer.BackgroundMask.SetDrawingLevel();
            legendPrinterLayer.Title = legendLayer.Title;
            legendPrinterLayer.Footer = legendLayer.Footer;
            legendPrinterLayer.XOffsetInPixel = legendLayer.XOffsetInPixel;
            legendPrinterLayer.YOffsetInPixel = legendLayer.YOffsetInPixel;
        }

    }
}
