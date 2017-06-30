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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PrinterLayerAdapter
    {
        public static PrinterLayerAdapter Create(PrinterLayer printerLayer, SimplifyMapPrinterLayer mapPrinterLayer)
        {
            var imagePrinterLayer = printerLayer as ImagePrinterLayer;
            if (printerLayer is SimplifyMapPrinterLayer)
                return new MapPrinterLayerAdapter(GisEditor.ActiveMap);
            else if (imagePrinterLayer != null)
            {
                var northArrow = GisEditor.ActiveMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
                if (northArrow != null) return new ImagePrinterLayerAdapter(northArrow);
                else return new ImagePrinterLayerAdapter(GisEditor.ActiveMap.MapTools.OfType<AdornmentLogo>().FirstOrDefault());
            }
            else if (printerLayer is LabelPrinterLayer)
                return new LabelPrinterLayerAdapter(GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<TitleAdornmentLayer>().FirstOrDefault());
            else if (printerLayer is LegendPrinterLayer)
            {
                var legendLayer = GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<LegendManagerAdornmentLayer>().SelectMany(l => l.LegendLayers.Select(ll => ll.ToLegendAdornmentLayer())).FirstOrDefault();
                return new LegendPrinterLayerAdapter(legendLayer);
            }
            else if (printerLayer is ScaleBarPrinterLayer)
                return new ScaleBarPrinterLayerAdapter(mapPrinterLayer, GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<ScaleBarAdornmentLayer>().FirstOrDefault());
            else if (printerLayer is ScaleLinePrinterLayer)
                return new ScaleLinePrinterLayerAdapter(mapPrinterLayer, GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<ScaleLineAdornmentLayer>().FirstOrDefault());
            else
                return new PrinterLayerAdapter();
        }

        public PrinterLayerAdapter()
        { }

        public void LoadFromActiveMap(PrinterLayer printerLayer)
        {
            LoadFromActiveMapCore(printerLayer);
        }

        protected virtual void LoadFromActiveMapCore(PrinterLayer printerLayer)
        { }

        public PrinterLayer GetPrinterLayerFromActiveMap(RectangleShape boudingBox)
        {
            return GetPrinterLayerFromActiveMapCore(boudingBox);
        }

        protected virtual PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            return null;
        }

        public static void SetPosition(AdornmentLocation location, RectangleShape boudingBox, PrinterLayer printerLayer, double width, double height, double left, double top)
        {
            switch (location)
            {
                case AdornmentLocation.UseOffsets:
                    printerLayer.SetPosition(width, height, 0, 0, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.UpperLeft:
                    printerLayer.SetPosition(width, height, boudingBox.UpperLeftPoint.X + width * 0.5 + left, boudingBox.UpperLeftPoint.Y - height * 0.5 - top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.UpperCenter:
                    printerLayer.SetPosition(width, height, left, boudingBox.UpperLeftPoint.Y - height * 0.5 - top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.UpperRight:
                    printerLayer.SetPosition(width, height, boudingBox.UpperRightPoint.X - width * 0.5 + left, boudingBox.UpperLeftPoint.Y - height * 0.5 - top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.CenterLeft:
                    printerLayer.SetPosition(width, height, boudingBox.UpperLeftPoint.X + width * 0.5 + left, -top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.Center:
                    printerLayer.SetPosition(width, height, left, -top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.CenterRight:
                    printerLayer.SetPosition(width, height, boudingBox.UpperRightPoint.X - width * 0.5 + left, -top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.LowerLeft:
                    printerLayer.SetPosition(width, height, boudingBox.UpperLeftPoint.X + width * 0.5 + left, boudingBox.LowerLeftPoint.Y + height * 0.5 - top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.LowerCenter:
                    printerLayer.SetPosition(width, height, left, boudingBox.LowerLeftPoint.Y + height * 0.5 - top, PrintingUnit.Inch);
                    break;
                case AdornmentLocation.LowerRight:
                    printerLayer.SetPosition(width, height, boudingBox.UpperRightPoint.X - width * 0.5 + left, boudingBox.LowerLeftPoint.Y + height * 0.5 - top, PrintingUnit.Inch);
                    break;
            }
        }
    }
}
