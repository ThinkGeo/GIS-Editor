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


using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class MapPrinterLayerAdapter : PrinterLayerAdapter
    {
        private WpfMap wpfMap;
        private bool doesLoadLayers;

        public MapPrinterLayerAdapter(WpfMap wpfMap, bool doesLoadLayers = false)
        {
            this.wpfMap = wpfMap;
            this.doesLoadLayers = doesLoadLayers;
        }

        public bool DoesLoadLayers
        {
            get { return doesLoadLayers; }
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerLayer)
        {
            SimplifyMapPrinterLayer mapPrinterLayer = null;
            if (wpfMap != null && (mapPrinterLayer = printerLayer as SimplifyMapPrinterLayer) != null)
            {
                if (DoesLoadLayers)
                {
                    PrinterLayerHelper.AddBaseOverlays(wpfMap, mapPrinterLayer);
                    var allLayers = wpfMap.Overlays.OfType<LayerOverlay>().SelectMany(layerOverlay => layerOverlay.Layers);
                    var allLayerTypes = GisEditor.LayerManager.GetLayerPlugins().Select(layerPlugin => layerPlugin.GetLayerType());
                    foreach (var layer in allLayers)
                    {
                        if (allLayerTypes.Contains(layer.GetType()))
                            mapPrinterLayer.Layers.Add(layer);
                    }
                    var measureOverlay = wpfMap
    .InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>().FirstOrDefault();

                    if (measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0) mapPrinterLayer.Layers.Add(measureOverlay.ShapeLayer);

                    var annotationOverlay = wpfMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>().FirstOrDefault();
                    if (annotationOverlay != null && annotationOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
                    {
                        annotationOverlay.TrackShapeLayer.Name = "AnnotationLayer";
                        mapPrinterLayer.Layers.Add(annotationOverlay.TrackShapeLayer);
                    }

                    if (CurrentOverlays.PopupOverlay != null
                        && CurrentOverlays.PopupOverlay.Popups.Count > 0)
                    {
                        var imageByte = GetCroppedMapPopupOverlayPreviewImage(wpfMap, new System.Windows.Int32Rect(0, 0, (int)GisEditor.ActiveMap.RenderSize.Width, (int)GisEditor.ActiveMap.RenderSize.Height));
                        MemoryRasterLayer layer = new MemoryRasterLayer(new MemoryStream(imageByte));

                        mapPrinterLayer.Layers.Add(layer);
                    }
                }

                mapPrinterLayer.Name = wpfMap.Name;
                mapPrinterLayer.MapUnit = wpfMap.MapUnit;
                mapPrinterLayer.DragMode = PrinterDragMode.Draggable;
                mapPrinterLayer.ResizeMode = PrinterResizeMode.Resizable;
                mapPrinterLayer.MapExtent = wpfMap.CurrentExtent;
                //RectangleShape currentMapExtent = GetFixedScaledExtent(boudingBox, wpfMap.CurrentResolution, wpfMap.CurrentExtent);
                //ResetFixedExtent(mapPrinterLayer, currentMapExtent);
                mapPrinterLayer.BackgroundMask.Advanced.FillCustomBrush = null;
                mapPrinterLayer.BackgroundMask.OutlinePen = new GeoPen(GeoColor.StandardColors.Black);
                var backgroundBrush = wpfMap.BackgroundOverlay.BackgroundBrush as GeoSolidBrush;
                if (backgroundBrush != null)
                    mapPrinterLayer.BackgroundMask.FillSolidBrush = backgroundBrush;
                else
                    mapPrinterLayer.BackgroundMask.FillSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.Transparent);
                mapPrinterLayer.BackgroundMask.SetDrawingLevel();
            }
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if (wpfMap != null)
            {
                SimplifyMapPrinterLayer mapPrinterLayer = new SimplifyMapPrinterLayer();
                LoadFromActiveMap(mapPrinterLayer);

                if (AppMenuUIPlugin.PreserveScale)
                {
                    mapPrinterLayer.SetPosition(wpfMap.ActualWidth, wpfMap.ActualHeight, 0, 0, PrintingUnit.Point);
                }
                else
                {
                    mapPrinterLayer.SetPosition(boudingBox.Width - 2, boudingBox.Height - 2, 0, 0, PrintingUnit.Inch);
                }

                //RectangleShape currentMapExtent = GetFixedScaledExtent(boudingBox, wpfMap.CurrentResolution, wpfMap.CurrentExtent);
                //ResetFixedExtent(mapPrinterLayer, currentMapExtent);

                mapPrinterLayer.SetDescriptionLayerBackground();
                mapPrinterLayer.MapImageCache = new GeoImage(new MemoryStream(BoundingBoxSelectorMapTool.GetCroppedMapPreviewImage(wpfMap, new System.Windows.Int32Rect(0, 0, (int)wpfMap.RenderSize.Width, (int)wpfMap.RenderSize.Height))));
                mapPrinterLayer.LastmapExtent = mapPrinterLayer.MapExtent;
                return mapPrinterLayer;
            }
            else return null;
        }

        public static RectangleShape GetFixedScaledExtent(RectangleShape bbox, double currentResolution, RectangleShape sourceExtent)
        {
            if (AppMenuUIPlugin.PreserveScale)
            {
                double widthDistanceInPoint = PrinterHelper.ConvertLength(bbox.Width, PrintingUnit.Inch, PrintingUnit.Point);
                double heightDistanceInPoint = PrinterHelper.ConvertLength(bbox.Height, PrintingUnit.Inch, PrintingUnit.Point);

                double halfWidthDistanceInMapUnit = widthDistanceInPoint * .5 * currentResolution;
                double halfHeightDistanceInMapUnit = heightDistanceInPoint * .5 * currentResolution;
                PointShape currentCenter = sourceExtent.GetCenterPoint();
                RectangleShape currentMapExtent = new RectangleShape(currentCenter.X - halfWidthDistanceInMapUnit, currentCenter.Y + halfHeightDistanceInMapUnit, currentCenter.X + halfWidthDistanceInMapUnit, currentCenter.Y - halfHeightDistanceInMapUnit);
                return currentMapExtent;
            }
            else return sourceExtent;
        }

        public static void ResetFixedExtent(MapPrinterLayer mapPrinterLayer, RectangleShape targetExtent)
        {
            mapPrinterLayer.MapExtent = targetExtent;
            var simplifyMapPrinterLayer = mapPrinterLayer as SimplifyMapPrinterLayer;
            if (simplifyMapPrinterLayer != null)
            {
                simplifyMapPrinterLayer.LastmapExtent = mapPrinterLayer.MapExtent;
            }
        }

        private static byte[] GetCroppedMapPopupOverlayPreviewImage(WpfMap wpfMap, Int32Rect drawingRect)
        {
            Canvas rootCanvas = wpfMap.ToolsGrid.Parent as Canvas;
            byte[] imageBytes = null;
            if (rootCanvas != null)
            {
                Canvas eventCanvas = rootCanvas.FindName("EventCanvas") as Canvas;
                if (eventCanvas != null)
                {
                    RenderTargetBitmap imageSource = new RenderTargetBitmap((int)wpfMap.RenderSize.Width, (int)wpfMap.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

                    Canvas popupCanvas = eventCanvas.FindName("PopupCanvas") as Canvas;
                    if (popupCanvas != null) imageSource.Render(popupCanvas);

                    CroppedBitmap croppedSource = new CroppedBitmap(imageSource, drawingRect);
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(croppedSource));
                    using (var streamSource = new MemoryStream())
                    {
                        encoder.Save(streamSource);
                        imageBytes = streamSource.ToArray();
                    }
                }
            }

            return imageBytes;
        }
    }
}
