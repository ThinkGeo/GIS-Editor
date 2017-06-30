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
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class PrinterLayerHelper
    {
        public static Bitmap GetPreviewBitmap(GisEditorWpfMap printMap)
        {
            var printerOverlay = printMap.InteractiveOverlays.OfType<PrinterInteractiveOverlay>().FirstOrDefault();
            var pagePrinterLayer = printerOverlay.PrinterLayers.OfType<PagePrinterLayer>().FirstOrDefault();

            RectangleShape pageBoundingBox = pagePrinterLayer.GetBoundingBox();
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap((int)pageBoundingBox.Width, (int)pageBoundingBox.Height);
            PlatformGeoCanvas canvas = new PlatformGeoCanvas();
            canvas.ClippingArea = pageBoundingBox;
            canvas.BeginDrawing(bitmap, pageBoundingBox, printMap.MapUnit);
            try
            {
                SetLegendIsPrinting(true, printerOverlay);
                SetDescriptionLabelVisible(false, printerOverlay);
                printerOverlay.PrinterLayers.OfType<GisEditorLegendPrinterLayer>().ForEach(l => l.IsPrinting = true);
                Collection<SimpleCandidate> labelsInAllLayers = new Collection<SimpleCandidate>();
                foreach (var printerLayer in printerOverlay.PrinterLayers.Where(l => !(l is PagePrinterLayer)))
                {
                    if (printerLayer is SimplifyMapPrinterLayer)
                    {
                        (printerLayer as SimplifyMapPrinterLayer).DrawingMode = MapPrinterDrawingMode.Vector;
                    }
                    printerLayer.IsDrawing = !(printerLayer is SimplifyMapPrinterLayer);
                    if (!printerLayer.IsOpen) printerLayer.Open();
                    printerLayer.Draw(canvas, labelsInAllLayers);
                }
                canvas.EndDrawing();
                foreach (var mapPrinterLayer in printerOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>())
                {
                    mapPrinterLayer.DrawingMode = MapPrinterDrawingMode.Raster;
                    mapPrinterLayer.DrawDescription = true;
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return bitmap;
            }
            finally
            {
                SetLegendIsPrinting(false, printerOverlay);
                SetDescriptionLabelVisible(true, printerOverlay);
            }
        }

        public static PrintDocument GetPrintDocument(GisEditorWpfMap printMap, PrinterGeoCanvas printerGeoCanvas)
        {
            var printerOverlay = printMap.InteractiveOverlays.OfType<PrinterInteractiveOverlay>().FirstOrDefault();
            var pagePrinterLayer = printerOverlay.PrinterLayers.OfType<PagePrinterLayer>().FirstOrDefault();

            PrintDocument printDocument = new PrintDocument();
            printDocument.DefaultPageSettings.PaperSize = GetPrintPreviewPaperSize(pagePrinterLayer);
            printDocument.DefaultPageSettings.Landscape = true;
            var printHeight = printDocument.DefaultPageSettings.PaperSize.Height;
            var printWidth = printDocument.DefaultPageSettings.PaperSize.Width;
            var drawingArea = new Rectangle(0, 0, printHeight, printWidth);
            if (pagePrinterLayer.Orientation == PrinterOrientation.Portrait)
            {
                printDocument.DefaultPageSettings.Landscape = false;
                drawingArea = new Rectangle(0, 0, printWidth, printHeight);
            }

            printerGeoCanvas.DrawingArea = drawingArea;
            printerGeoCanvas.BeginDrawing(printDocument, pagePrinterLayer.GetBoundingBox(), printMap.MapUnit);

            try
            {
                SetLegendIsPrinting(true, printerOverlay);
                SetDescriptionLabelVisible(false, printerOverlay);
                Collection<SimpleCandidate> labelsInAllLayers = new Collection<SimpleCandidate>();
                foreach (PrinterLayer printerLayer in printerOverlay.PrinterLayers.Where(l => !(l is PagePrinterLayer)))
                {
                    printerLayer.IsDrawing = true;

                    if (printerLayer is DatePrinterLayer)
                    {
                        string currentDate = DateTime.Now.ToString(((DatePrinterLayer)printerLayer).DateFormat);
                        ((DatePrinterLayer)printerLayer).DateString = currentDate;
                    }
                    if (printerLayer is ProjectPathPrinterLayer)
                    {
                        string projectPath = ((ProjectPathPrinterLayer)printerLayer).ProjectPath;
                        Uri uri = GisEditor.ProjectManager.ProjectUri;
                        if (!File.Exists(projectPath) && File.Exists(uri.LocalPath))
                        {
                            ((ProjectPathPrinterLayer)printerLayer).ProjectPath = uri.LocalPath;
                        }
                    }

                    SimplifyMapPrinterLayer mapPrinterLayer = null;
                    if (printerLayer.GetType() == typeof(SimplifyMapPrinterLayer))
                    {
                        printerLayer.IsDrawing = false;
                        mapPrinterLayer = printerLayer as SimplifyMapPrinterLayer;
                        if (mapPrinterLayer.BackgroundMask != null) mapPrinterLayer.BackgroundMask.DrawingLevel = DrawingLevel.LevelOne;
                        mapPrinterLayer.DrawingMode = MapPrinterDrawingMode.Vector;
                    }

                    printerLayer.Draw(printerGeoCanvas, labelsInAllLayers);
                    printerLayer.IsDrawing = false;
                    printerGeoCanvas.Flush();

                    if (printerLayer is DatePrinterLayer)
                    {
                        ((DatePrinterLayer)printerLayer).DateString = DateElementViewModel.DefaultDate.ToString(((DatePrinterLayer)printerLayer).DateFormat);
                    }
                }
                foreach (var item in printerOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)))
                {
                    (item as SimplifyMapPrinterLayer).DrawingMode = MapPrinterDrawingMode.Raster;
                }
                return printDocument;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return printDocument;
            }
            finally
            {
                SetLegendIsPrinting(false, printerOverlay);
                SetDescriptionLabelVisible(true, printerOverlay);
            }
        }

        public static T CloneDeep<T>(object instance) where T : class
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, instance);
            stream.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(stream) as T;
        }

        public static MapPrinterLayer GetMapPrinterLayer(double width, double height, double centerX, double centerY)
        {
            var mapPrinterLayer = new SimplifyMapPrinterLayer { Name = "Map1", DrawingExceptionMode = DrawingExceptionMode.DrawException };
            mapPrinterLayer.Open();
            var adapter = new MapPrinterLayerAdapter(GisEditor.ActiveMap);
            adapter.LoadFromActiveMap(mapPrinterLayer);
            mapPrinterLayer.SetDescriptionLayerBackground();
            mapPrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return mapPrinterLayer;
        }

        public static ScaleBarPrinterLayer GetScaleBarPrinterLayer(MapPrinterLayer mapPrinterLayer, double width, double height, double centerX, double centerY)
        {
            ScaleBarPrinterLayer scaleBarPrinterLayer = new ScaleBarPrinterLayer(mapPrinterLayer);
            scaleBarPrinterLayer.MapUnit = mapPrinterLayer.MapUnit;
            scaleBarPrinterLayer.BackgroundMask = new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent));
            scaleBarPrinterLayer.UnitFamily = UnitSystem.Metric;
            scaleBarPrinterLayer.AlternateBarBrush = new GeoSolidBrush(GeoColor.StandardColors.White);
            scaleBarPrinterLayer.Open();
            scaleBarPrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return scaleBarPrinterLayer;
        }

        public static LegendPrinterLayer GetLegendPrinterLayer(double width, double height, double centerX, double centerY)
        {
            LegendPrinterLayer legendPrinterLayer = new GisEditorLegendPrinterLayer();
            legendPrinterLayer.LegendItems.Add(GetLegendItem(GeoColor.StandardColors.LightBlue, "Sample1", 3, 3));
            legendPrinterLayer.LegendItems.Add(GetLegendItem(GeoColor.StandardColors.LawnGreen, "Sample2", 3, 15));
            legendPrinterLayer.LegendItems.Add(GetLegendItem(GeoColor.StandardColors.LightGreen, "Sample3", 3, 18));
            legendPrinterLayer.Open();
            legendPrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return legendPrinterLayer;
        }

        public static LabelPrinterLayer GetLabelPrinterLayer(string text, double width, double height, double centerX, double centerY)
        {
            LabelPrinterLayer labelPrinterLayer = new LabelPrinterLayer { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            labelPrinterLayer.LoadFromViewModel(new TextElementViewModel(false) { Text = text, FontSize = 34 });
            labelPrinterLayer.Open();
            labelPrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return labelPrinterLayer;
        }

        public static DatePrinterLayer GetCurrentDatePrinterLayer(double width, double height, double centerX, double centerY)
        {
            DatePrinterLayer datePrinterLayer = new DatePrinterLayer { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            datePrinterLayer.LoadFromViewModel(new DateElementViewModel() { SelectedFormat = DateTime.Now.ToShortDateString(), FontSize = 12 });
            datePrinterLayer.Open();
            datePrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return datePrinterLayer;
        }

        internal static PrinterLayer GetImagePrinterLayer(double width, double height, double centerX, double centerY)
        {
            ImageElementViewModel imageEntity = new ImageElementViewModel();
            GeoImage geoImage = null;
            geoImage = new GeoImage(new MemoryStream(imageEntity.SelectedImage));
            ImagePrinterLayer imagePrinterLayer = new ImagePrinterLayer(geoImage, 0, 0, PrintingUnit.Inch) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            imagePrinterLayer.LoadFromViewModel(imageEntity);
            var imgWidth = imagePrinterLayer.Image.Width;
            var imgHeight = imagePrinterLayer.Image.Height;

            imagePrinterLayer.Open();
            imagePrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return imagePrinterLayer;
        }

        internal static DataGridPrinterLayer GetDataGridPrinterLayer(double width, double height, double centerX, double centerY)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Header1");
            dataTable.Columns.Add("Header2");
            dataTable.Columns.Add("Header3");
            string text = "content";
            for (int i = 0; i < 3; i++)
            {
                var row = dataTable.NewRow();
                row[0] = text;
                row[1] = text;
                row[2] = text;
                dataTable.Rows.Add(row);
            }
            DataGridPrinterLayer dataGridPrinterLayer = new DataGridPrinterLayer { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            dataGridPrinterLayer.LoadFromViewModel(new DataGridViewModel { CurrentDataTable = dataTable, FontSize = 22 });
            dataGridPrinterLayer.Open();
            dataGridPrinterLayer.SetPosition(width, height, centerX, centerY, PrintingUnit.Inch);
            return dataGridPrinterLayer;
        }

        internal static void AddBaseOverlays(WpfMap map, SimplifyMapPrinterLayer mapPrinterLayer)
        {
            var osmOverlay = map.Overlays.OfType<OpenStreetMapOverlay>().FirstOrDefault();
            if (osmOverlay != null && osmOverlay.IsVisible)
            {
                OpenStreetMapLayer osmLayer = new OpenStreetMapLayer();
                osmLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                osmLayer.TimeoutInSeconds = 5;
                osmLayer.DrawingException += new EventHandler<DrawingExceptionLayerEventArgs>(osmLayer_DrawingException);
                mapPrinterLayer.Layers.Add(osmLayer);
            }

            var bingOverlay = map.Overlays.OfType<BingMapsOverlay>().FirstOrDefault();
            if (bingOverlay != null && bingOverlay.IsVisible)
            {
                BingMapsLayer bingMapsLayer = new BingMapsLayer(bingOverlay.ApplicationId, (Layers.BingMapsMapType)bingOverlay.MapType);
                bingMapsLayer.TileCache = null;
                bingMapsLayer.TimeoutInSeconds = 5;
                bingMapsLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                bingMapsLayer.DrawingException += new EventHandler<DrawingExceptionLayerEventArgs>(BingMapsLayer_DrawingException);
                mapPrinterLayer.Layers.Add(bingMapsLayer);
            }

            var wmlkOverlay = map.Overlays.OfType<WorldMapKitMapOverlay>().FirstOrDefault();
            if (wmlkOverlay != null && wmlkOverlay.IsVisible)
            {
                WorldMapKitLayer worldMapKitLayer = new WorldMapKitLayer(wmlkOverlay.ClientId, wmlkOverlay.PrivateKey);
                worldMapKitLayer.TimeoutInSecond = 5;
                worldMapKitLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                worldMapKitLayer.DrawingException += new EventHandler<DrawingExceptionLayerEventArgs>(WorldMapKitLayer_DrawingException);
                worldMapKitLayer.TileCache = null;
                worldMapKitLayer.Projection = wmlkOverlay.Projection;
                mapPrinterLayer.Layers.Add(worldMapKitLayer);
            }
        }

        internal static bool CheckDecimalDegreeIsInRange(RectangleShape extent)
        {
            var uLPoint = extent.UpperLeftPoint;
            var lRPoint = extent.LowerRightPoint;
            if (PrinterLayerHelper.CheckDecimalDegreeLatitudeIsInRange(uLPoint.Y)
                && PrinterLayerHelper.CheckDecimalDegreeLongitudeIsInRange(uLPoint.X)
                && PrinterLayerHelper.CheckDecimalDegreeLatitudeIsInRange(lRPoint.Y)
                && PrinterLayerHelper.CheckDecimalDegreeLongitudeIsInRange(lRPoint.X))
            {
                return true;
            }
            else return false;
        }

        internal static double GetInchValue(SizeUnit unit, double dpi, double value)
        {
            double result = 0;
            switch (unit)
            {
                case SizeUnit.Pixels:
                default:
                    result = value / dpi;
                    break;
                case SizeUnit.Inches:
                    result = value;
                    break;
                case SizeUnit.Cm:
                    result = value / dpi;
                    break;
            }
            return result;
        }

        private static void WorldMapKitLayer_DrawingException(object sender, DrawingExceptionLayerEventArgs e)
        {
            BaseMapsHelper.RaiseDrawingException<WorldMapKitLayer>("World Map Kit", sender, e);
        }

        private static void BingMapsLayer_DrawingException(object sender, DrawingExceptionLayerEventArgs e)
        {
            BaseMapsHelper.RaiseDrawingException<BingMapsLayer>("Bing Map", sender, e);
        }

        private static void osmLayer_DrawingException(object sender, DrawingExceptionLayerEventArgs e)
        {
            BaseMapsHelper.RaiseDrawingException<OpenStreetMapLayer>("OpenStreetMap", sender, e);
        }

        public static PaperSize GetPrintPreviewPaperSize(PagePrinterLayer pagePrinterLayer)
        {
            PaperSize printPreviewPaperSize = new PaperSize("AnsiA", 850, 1100);
            switch (pagePrinterLayer.PageSize)
            {
                case PrinterPageSize.AnsiA:
                    printPreviewPaperSize = new PaperSize("AnsiA", 850, 1100);
                    break;
                case PrinterPageSize.AnsiB:
                    printPreviewPaperSize = new PaperSize("AnsiB", 1100, 1700);
                    break;
                case PrinterPageSize.AnsiC:
                    printPreviewPaperSize = new PaperSize("AnsiC", 1700, 2200);
                    break;
                case PrinterPageSize.AnsiD:
                    printPreviewPaperSize = new PaperSize("AnsiD", 2200, 3400);
                    break;
                case PrinterPageSize.AnsiE:
                    printPreviewPaperSize = new PaperSize("AnsiE", 3400, 4400);
                    break;
                case PrinterPageSize.Custom:
                    printPreviewPaperSize = new PaperSize("Custom Size", (int)pagePrinterLayer.CustomWidth, (int)pagePrinterLayer.CustomHeight);
                    break;
                default:
                    break;
            }

            return printPreviewPaperSize;
        }

        private static LegendItem GetLegendItem(GeoColor geoColor, string text, float left, float top)
        {
            LegendItem legendItem = new LegendItem(1, 2, 3f, 3f, AreaStyles.Antarctica1, TextStyles.Antarctical(text));
            legendItem.ImageMask = AreaStyles.Antarctica1;
            legendItem.TextMask = new AreaStyle(new GeoSolidBrush(geoColor));
            legendItem.LeftPadding = left;
            legendItem.TopPadding = top;
            return legendItem;
        }

        private static bool CheckDecimalDegreeLatitudeIsInRange(double latitude)
        {
            return !(Math.Round((double)latitude, 4) > 90 || Math.Round((double)latitude, 4) < -90);
        }

        private static bool CheckDecimalDegreeLongitudeIsInRange(double longitude)
        {
            return !(Math.Round((double)longitude, 4) > 180 || Math.Round((double)longitude, 4) < -180);
        }

        private static void SetDescriptionLabelVisible(bool visible, PrinterInteractiveOverlay printerOverlay)
        {
            printerOverlay.PrinterLayers.Where(IsSimplifyMapPrinterLayer).OfType<SimplifyMapPrinterLayer>().ForEach(l => l.DrawDescription = visible);
        }

        private static void SetLegendIsPrinting(bool isPrinting, PrinterInteractiveOverlay printerOverlay)
        {
            printerOverlay.PrinterLayers.OfType<GisEditorLegendPrinterLayer>().ForEach(l => l.IsPrinting = isPrinting);
        }

        private static bool IsSimplifyMapPrinterLayer(PrinterLayer printerLayer)
        {
            return printerLayer.GetType().Equals(typeof(SimplifyMapPrinterLayer));
        }
    }
}