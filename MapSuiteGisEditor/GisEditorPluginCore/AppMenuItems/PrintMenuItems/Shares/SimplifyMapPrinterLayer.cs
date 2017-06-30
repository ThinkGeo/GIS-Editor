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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SimplifyMapPrinterLayer : MapPrinterLayer
    {
        [NonSerialized]
        private RectangleShape lastmapExtent;

        [NonSerialized]
        private GeoImage mapImageCache;

        [Obfuscation(Exclude = true)]
        private bool enableClipping;

        [Obfuscation(Exclude = true)]
        private bool drawDescription;

        public SimplifyMapPrinterLayer()
            : base()
        {
            drawDescription = true;
            enableClipping = true;
        }

        public RectangleShape LastmapExtent
        {
            get { return lastmapExtent; }
            set { lastmapExtent = value; }
        }

        public GeoImage MapImageCache
        {
            get { return mapImageCache; }
            set { mapImageCache = value; }
        }

        protected bool EnableClipping
        {
            get { return enableClipping; }
            set { enableClipping = value; }
        }

        public bool DrawDescription
        {
            get { return drawDescription; }
            set { drawDescription = value; }
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            Dictionary<Layer, Tuple<int, int>> layerStatus = new Dictionary<Layer, Tuple<int, int>>();
            foreach (Layer layer in Layers)
            {
                ShapeFileFeatureLayer shapeFileFeatureLayer = layer as ShapeFileFeatureLayer;
                if (shapeFileFeatureLayer != null)
                {
                    Tuple<int, int> tuple = new Tuple<int, int>(shapeFileFeatureLayer.SimplificationAreaInPixel, shapeFileFeatureLayer.MaxRecordsToDraw);
                    layerStatus.Add(shapeFileFeatureLayer, tuple);
                    shapeFileFeatureLayer.SimplificationAreaInPixel = 6;
                }
            }

            DrawInternal(canvas, labelsInAllLayers);

            foreach (Layer layer in Layers)
            {
                ShapeFileFeatureLayer shapeFileFeatureLayer = layer as ShapeFileFeatureLayer;
                if (shapeFileFeatureLayer != null && layerStatus.ContainsKey(shapeFileFeatureLayer))
                {
                    shapeFileFeatureLayer.SimplificationAreaInPixel = layerStatus[shapeFileFeatureLayer].Item1;
                    shapeFileFeatureLayer.MaxRecordsToDraw = layerStatus[shapeFileFeatureLayer].Item2;
                }
            }
        }

        private void AWaitAsync(Action action)
        {
            bool isRunning = true;
            Task.Factory.StartNew(() =>
            {
                Monitor.Enter(this);
                try
                {
                    if (action != null) action();
                }
                finally
                {
                    Monitor.Exit(this);
                    isRunning = false;
                }
            });

            while (isRunning)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1);
            }
        }

        private void DrawInternal(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            RectangleShape currentBoundingBox = GetBoundingBox();

            AreaStyle brushStyle = BackgroundMask.CloneDeep() as AreaStyle;
            brushStyle.OutlinePen.Width = 0;
            brushStyle.Draw(new BaseShape[] { currentBoundingBox }, canvas, new Collection<SimpleCandidate>(), labelsInAllLayers);

            // Clear Cache images
            if (LastmapExtent != null)
            {
                if (LastmapExtent.UpperLeftPoint.X != MapExtent.UpperLeftPoint.X
                || LastmapExtent.UpperLeftPoint.Y != MapExtent.UpperLeftPoint.Y
                || LastmapExtent.LowerRightPoint.X != MapExtent.LowerRightPoint.X
                || LastmapExtent.LowerRightPoint.Y != MapExtent.LowerRightPoint.Y
                || IsDrawing)
                {
                    mapImageCache = null;
                }
            }

            //For adjusting the world extent of the map to the ratio of the drawing area.
            RectangleShape adjustedWorldExtent = ExtentHelper.GetDrawingExtent(MapExtent, (float)currentBoundingBox.Width, (float)currentBoundingBox.Height);
            RectangleShape boundingBox = GetBoundingBox();
            PointShape ajustedWorldCenter = adjustedWorldExtent.GetCenterPoint();
            PageGeoCanvas pageGeoCanvas = new PageGeoCanvas(new RectangleShape(0, boundingBox.Height, boundingBox.Width, 0), currentBoundingBox);
            //if (EnableClipping)
            //{
            //    pageGeoCanvas.EnableCliping = true;
            //    pageGeoCanvas.ClipingArea = adjustedWorldExtent;
            //}

            //if (canvas is GdiPlusGeoCanvas && DrawingMode != Core.DrawingMode.Vector)
            // only display on map.
            if (DrawingMode != MapPrinterDrawingMode.Vector && !(canvas is PrinterGeoCanvas))
            {
                double width = boundingBox.Width / canvas.CurrentWorldExtent.Width * canvas.Width;
                double height = boundingBox.Height / canvas.CurrentWorldExtent.Height * canvas.Height;
                GeoImage image = GetCacheImage(pageGeoCanvas, canvas.MapUnit, adjustedWorldExtent, labelsInAllLayers, width, height);

                pageGeoCanvas.BeginDrawing(canvas, adjustedWorldExtent, MapUnit);
                if (image != null)
                    pageGeoCanvas.DrawWorldImage(image, ajustedWorldCenter.X, ajustedWorldCenter.Y, (float)boundingBox.Width - 0.5f, (float)boundingBox.Height - 0.5f, DrawingLevel.LabelLevel);
                if (DrawDescription) DrawDescriptionText(pageGeoCanvas);
                //pageGeoCanvas.EnableCliping = false;
                pageGeoCanvas.EndDrawing();
            }
            // display on map or printer.
            else
            {
                pageGeoCanvas.BeginDrawing(canvas, adjustedWorldExtent, MapUnit);
                double increase = 0;
                if (BackgroundMask.OutlinePen != null)
                {
                    float haflPenWidth = BackgroundMask.OutlinePen.Width / 2;
                    increase = haflPenWidth * canvas.CurrentWorldExtent.Width / canvas.Width;
                }
                canvas.ClippingArea = new RectangleShape(
                    currentBoundingBox.UpperLeftPoint.X - increase,
                    currentBoundingBox.UpperLeftPoint.Y + increase,
                    currentBoundingBox.LowerRightPoint.X + increase,
                    currentBoundingBox.LowerRightPoint.Y - increase);
                if (canvas is PrinterGeoCanvas)
                {
                    foreach (Layer layer in Layers)
                    {
                        pageGeoCanvas.Flush();

                        float savedDrawingMarginPercentage = 0;
                        FeatureLayer featureLayer = layer as FeatureLayer;
                        if (featureLayer != null)
                        {
                            savedDrawingMarginPercentage = featureLayer.DrawingMarginInPixel;
                            featureLayer.DrawingMarginInPixel = 0;
                        }
                        layer.SafeProcess(() =>
                        {
                            layer.Draw(pageGeoCanvas, labelsInAllLayers);
                        });
                        if (featureLayer != null && savedDrawingMarginPercentage != 0)
                        {
                            featureLayer.DrawingMarginInPixel = savedDrawingMarginPercentage;
                        }
                    }
                }
                else
                {
                    using (MemoryStream ms = GetImageStream(canvas, labelsInAllLayers, adjustedWorldExtent, boundingBox))
                    {
                        pageGeoCanvas.DrawWorldImage(new GeoImage(ms), ajustedWorldCenter.X, ajustedWorldCenter.Y, (float)boundingBox.Width - 0.5f, (float)boundingBox.Height - 0.5f, DrawingLevel.LabelLevel);
                    }
                    //foreach (Layer layer in Layers)
                    //{
                    //    pageGeoCanvas.Flush();
                    //    layer.SafeProcess(() =>
                    //    {
                    //        layer.Draw(pageGeoCanvas, labelsInAllLayers);
                    //    });
                    //}
                }
                if (DrawDescription) DrawDescriptionText(pageGeoCanvas);
                //pageGeoCanvas.EnableCliping = false;
                var areaStyle = new AreaStyle(BackgroundMask.OutlinePen) { DrawingLevel = DrawingLevel.LabelLevel };
                areaStyle.Draw(new BaseShape[1] { currentBoundingBox }, canvas, new Collection<SimpleCandidate>(), labelsInAllLayers);
                canvas.ClippingArea = null;
            }

            AreaStyle lineStyle = new AreaStyle(BackgroundMask.OutlinePen.CloneDeep());
            lineStyle.DrawingLevel = DrawingLevel.LabelLevel;
            lineStyle.Draw(new BaseShape[] { currentBoundingBox }, canvas, new Collection<SimpleCandidate>(), labelsInAllLayers);
            LastmapExtent = new RectangleShape(MapExtent.UpperLeftPoint.X, MapExtent.UpperLeftPoint.Y, MapExtent.LowerRightPoint.X, MapExtent.LowerRightPoint.Y);
        }

        private MemoryStream GetImageStream(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers, RectangleShape adjustedWorldExtent, RectangleShape boundingBox)
        {
            using (Bitmap bitmap = new Bitmap((int)boundingBox.Width, (int)boundingBox.Height))
            {
                MemoryStream ms = new MemoryStream();
                PlatformGeoCanvas gdiPlusGeoCanvas = new PlatformGeoCanvas();
                gdiPlusGeoCanvas.DrawingQuality = DrawingQuality.HighQuality;
                gdiPlusGeoCanvas.BeginDrawing(bitmap, adjustedWorldExtent, MapUnit);
                foreach (Layer layer in Layers)
                {
                    gdiPlusGeoCanvas.Flush();
                    layer.SafeProcess(() =>
                    {
                        layer.Draw(gdiPlusGeoCanvas, labelsInAllLayers);
                    });
                }
                gdiPlusGeoCanvas.EndDrawing();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms;
            }
        }

        private static void DrawDescriptionText(PageGeoCanvas pageGeoCanvas)
        {
            pageGeoCanvas.DrawTextWithScreenCoordinate("Map", new GeoFont("Arial", 22), new GeoSolidBrush(GeoColor.StandardColors.Black), (float)(pageGeoCanvas.Width * 0.5), (float)(pageGeoCanvas.Height * 0.5 - 15), DrawingLevel.LabelLevel);
            pageGeoCanvas.DrawTextWithScreenCoordinate(GisEditor.LanguageManager.GetStringResource("RightClickToSetExtentText"), new GeoFont("Arial", 18), new GeoSolidBrush(GeoColor.StandardColors.Black), (float)(pageGeoCanvas.Width * 0.5), (float)(pageGeoCanvas.Height * 0.5) + 15, DrawingLevel.LabelLevel);
        }

        private GeoImage GetCacheImage(PageGeoCanvas pageGeoCanvas, GeographyUnit geographyUnit, RectangleShape adjustedWorldExtent, Collection<SimpleCandidate> labelsInAllLayers, double width, double height)
        {
            RectangleShape currentBoundingBox = GetBoundingBox();

            if (mapImageCache == null && Layers.Count > 0)
            {
                Bitmap bitmap = null;
                MemoryStream memoryStream = null;
                try
                {
                    bitmap = new Bitmap((int)width, (int)height);
                    PlatformGeoCanvas tmpCanvas = new PlatformGeoCanvas();
                    tmpCanvas.DrawingQuality = DrawingQuality.HighSpeed;
                    tmpCanvas.BeginDrawing(bitmap, currentBoundingBox, geographyUnit);

                    pageGeoCanvas.BeginDrawing(tmpCanvas, adjustedWorldExtent, MapUnit);

                    lock (Layers)
                    {
                        foreach (Layer layer in Layers)
                        {
                            if (!layer.IsOpen) layer.Open();
                            pageGeoCanvas.Flush();
                            layer.Draw(pageGeoCanvas, labelsInAllLayers);
                        }
                    }

                    pageGeoCanvas.EndDrawing();
                    tmpCanvas.EndDrawing();
                    IsDrawing = false;
                    memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    mapImageCache = new GeoImage(memoryStream);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    bitmap?.Dispose();
                }
            }
            return mapImageCache;
        }
    }
}