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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class InteractiveOverlayHelper
    {
        private const string inTrackingFeatureKey = "InTrackingFeature";
        private const string defaultLayerTileName = "DefaultLayerTile";
        private static readonly string tracksInProcessLineName = "TracksInProcessLine";
        private static BitmapImage blankImageSource = new BitmapImage();

        public static bool UpdateInProcessInteractiveOverlayImageSource(InteractiveOverlay overlay
            , MapArguments mapArguments
            , IEnumerable<InMemoryFeatureLayer> inProcessLayers
            , Collection<SimpleCandidate> simpleCandidates
            , RenderMode renderMode, PolygonTrackMode polygonTrackMode = PolygonTrackMode.Default, bool refreshAll = false, bool isTrackingPolygon = false)
        {
            bool isUpdated = false;
            var inProcessLayer = inProcessLayers.FirstOrDefault();
            var inProcessLineLayer = inProcessLayers.Count() == 2 ? inProcessLayers.Last() : null;

            simpleCandidates.Clear();
            if (inProcessLineLayer != null && inProcessLineLayer.InternalFeatures.Count > 0)
            {
                Image defaultImage = GetTileImage(overlay, mapArguments, tracksInProcessLineName, 2);
                inProcessLineLayer.InternalFeatures.First().ColumnValues[inTrackingFeatureKey] = "True";
                UpdateImageSource(mapArguments, simpleCandidates, renderMode, inProcessLineLayer, defaultImage, OutlineDrawMode.Dynamic);
                inProcessLineLayer.InternalFeatures.First().ColumnValues[inTrackingFeatureKey] = string.Empty;
                isUpdated = true;

                if (refreshAll)
                {
                    defaultImage = GetTileImage(overlay, mapArguments);
                    var outlineDrawing = isTrackingPolygon ? OutlineDrawMode.Open : OutlineDrawMode.Sealed;
                    UpdateImageSource(mapArguments, simpleCandidates, renderMode, inProcessLayer, defaultImage, outlineDrawing);
                }
            }
            else if (inProcessLayer != null && inProcessLayer.InternalFeatures.Count > 0)
            {
                Image defaultImage = GetTileImage(overlay, mapArguments);
                var outlineDrawMode = polygonTrackMode == PolygonTrackMode.LineOnly ? OutlineDrawMode.Open : OutlineDrawMode.LineWithFill;
                if (outlineDrawMode == OutlineDrawMode.Open && !isTrackingPolygon) outlineDrawMode = OutlineDrawMode.Sealed;
                UpdateImageSource(mapArguments, simpleCandidates, renderMode, inProcessLayer, defaultImage, outlineDrawMode);
                isUpdated = true;
            }
            else
            {
                Image defaultImage = GetTileImage(overlay, mapArguments);
                defaultImage.Source = blankImageSource;
            }

            return isUpdated;
        }

        private static void UpdateImageSource(MapArguments mapArguments, Collection<SimpleCandidate> simpleCandidates, RenderMode renderMode, InMemoryFeatureLayer inProcessLayer, Image defaultImage, OutlineDrawMode outlineDrawMode = OutlineDrawMode.LineWithFill)
        {
            if (renderMode == RenderMode.DrawingVisual)
            {
                defaultImage.Source = GetEditTraceLineImageSourceWithDrawingVisualGeoCanvas(mapArguments, inProcessLayer, simpleCandidates, outlineDrawMode);
            }
            else
            {
                defaultImage.Source = GetEditTraceLineImageSourceWithGdiPlusGeoCanvas(mapArguments, inProcessLayer, simpleCandidates, outlineDrawMode);
            }
        }

        private static Image GetTileImage(InteractiveOverlay overlay, MapArguments mapArguments, string tileName = defaultLayerTileName, int zIndex = 1)
        {
            LayerTile layerTile = overlay.OverlayCanvas.Children.OfType<LayerTile>().FirstOrDefault(tmpTile
               => tmpTile.GetValue(Canvas.NameProperty).Equals(tileName));

            if (layerTile == null)
            {
                layerTile = new LayerTile();
                layerTile.IsAsync = false;
                layerTile.SetValue(Canvas.NameProperty, tileName);
                layerTile.SetValue(Canvas.ZIndexProperty, zIndex);
                overlay.OverlayCanvas.Children.Add(layerTile);
            }

            Image image = layerTile.Content as Image;
            if (image == null)
            {
                image = new Image();
                layerTile.Content = image;
            }

            image.Width = mapArguments.ActualWidth;
            image.Height = mapArguments.ActualHeight;
            return image;
        }

        public static void ResetInProcessInteractiveOverlayImageSource(InteractiveOverlay overlay)
        {
            var defaultLayerTiles = overlay.OverlayCanvas.Children.OfType<LayerTile>().Where(tmpTile
                    => tmpTile.GetValue(Canvas.NameProperty).Equals(defaultLayerTileName)).Concat(
                    overlay.OverlayCanvas.Children.OfType<LayerTile>().Where(tmpTile
                        => tmpTile.GetValue(Canvas.NameProperty).Equals(tracksInProcessLineName)));

            foreach (var tile in defaultLayerTiles)
            {
                Image editsInProcessImage = tile.Content as Image;
                if (editsInProcessImage != null) editsInProcessImage.Source = blankImageSource;
            }
        }

        private static ImageSource GetEditTraceLineImageSourceWithGdiPlusGeoCanvas(MapArguments mapArguments, InMemoryFeatureLayer inProcessLayer, Collection<SimpleCandidate> simpleCandidates, OutlineDrawMode outlineDrawMode)
        {
            MemoryStream streamSource = null;
            OutlineGdiPlusGeoCanvas geoCanvas = new OutlineGdiPlusGeoCanvas();
            geoCanvas.OutlineDrawMode = outlineDrawMode;
            using (var nativeImage = new System.Drawing.Bitmap((int)mapArguments.ActualWidth, (int)mapArguments.ActualHeight))
            {
                geoCanvas.BeginDrawing(nativeImage, mapArguments.CurrentExtent, mapArguments.MapUnit);
                lock (inProcessLayer)
                {
                    if (!inProcessLayer.IsOpen) inProcessLayer.Open();
                    inProcessLayer.Draw(geoCanvas, simpleCandidates);
                }
                geoCanvas.EndDrawing();

                streamSource = new MemoryStream();
                nativeImage.Save(streamSource, System.Drawing.Imaging.ImageFormat.Png);
                streamSource.Seek(0, SeekOrigin.Begin);
            }

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = streamSource;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private static ImageSource GetEditTraceLineImageSourceWithDrawingVisualGeoCanvas(MapArguments mapArguments, InMemoryFeatureLayer inProcessLayer, Collection<SimpleCandidate> simpleCandidates, OutlineDrawMode outlineDrawMode)
        {
            OutlineDrawingVisualGeoCanvas geoCanvas = new OutlineDrawingVisualGeoCanvas();
            geoCanvas.OutlineDrawMode = outlineDrawMode;
            RenderTargetBitmap nativeImage = new RenderTargetBitmap((int)mapArguments.ActualWidth, (int)mapArguments.ActualHeight, geoCanvas.Dpi, geoCanvas.Dpi, PixelFormats.Pbgra32);
            geoCanvas.BeginDrawing(nativeImage, mapArguments.CurrentExtent, mapArguments.MapUnit);
            lock (inProcessLayer)
            {
                if (!inProcessLayer.IsOpen) inProcessLayer.Open();
                inProcessLayer.Draw(geoCanvas, simpleCandidates);
            }
            geoCanvas.EndDrawing();
            return nativeImage;
        }
    }
}