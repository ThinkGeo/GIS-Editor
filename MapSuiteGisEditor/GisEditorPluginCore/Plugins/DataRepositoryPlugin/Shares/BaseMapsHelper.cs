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
using System.Globalization;
using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class BaseMapsHelper
    {
        internal static readonly string WmkClientId = string.Empty;
        internal static readonly string WmkPrivateKey = string.Empty;
        internal static readonly string ConnectionFailed = "{0} service connection\r\nfailed or timeout.";

        public static WorldMapKitMapOverlay AddWorldMapKitOverlay(GisEditorWpfMap map)
        {
            var wmkOverlay = new WorldMapKitMapOverlay(WmkClientId, WmkPrivateKey);
            wmkOverlay.TileType = TileType.HybridTile;
            wmkOverlay.Name = GisEditor.LanguageManager.GetStringResource("WorldMapKitName");
            wmkOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            wmkOverlay.TileBuffer = 2;
            wmkOverlay.DrawingException += new EventHandler<DrawingExceptionTileOverlayEventArgs>(WmkOverlay_DrawingException);

            if (string.IsNullOrEmpty(map.DisplayProjectionParameters))
            {
                wmkOverlay.Projection = Layers.WorldMapKitProjection.DecimalDegrees;
                map.DisplayProjectionParameters = Proj4Projection.GetEpsgParametersString(4326);
            }
            else
            {
                wmkOverlay.Projection = map.MapUnit == GeographyUnit.DecimalDegree ? Layers.WorldMapKitProjection.DecimalDegrees : Layers.WorldMapKitProjection.SphericalMercator;
            }
            wmkOverlay.RefreshCache();
            if (map.MapUnit == GeographyUnit.Meter || map.MapUnit == GeographyUnit.DecimalDegree)
            {
                BaseMapsHelper.RemoveAllBaseOverlays(map);
                map.Overlays.Insert(0, wmkOverlay);
                SetExtent(map);
                map.Refresh(wmkOverlay);
            }
            else
            {
                AddOverlayInGoogleProjection(wmkOverlay, map);
            }
            return wmkOverlay;
        }

        public static BingMapsOverlay AddBingMapsOverlay(GisEditorWpfMap map)
        {
            BingMapsConfigWindow configWindow = new BingMapsConfigWindow();
            BingMapsOverlay bingOverlay = null;
            if (!string.IsNullOrEmpty(configWindow.BingMapsKey))
            {
                if (((BingMapsConfigViewModel)configWindow.DataContext).Validate())
                {
                    bingOverlay = AddBingMapOverlayToMap(map, configWindow, bingOverlay);
                }
            }
            else
            {
                if (configWindow.ShowDialog().GetValueOrDefault())
                {
                    bingOverlay = AddBingMapOverlayToMap(map, configWindow, bingOverlay);
                }
            }
            return bingOverlay;
        }

        private static BingMapsOverlay AddBingMapOverlayToMap(GisEditorWpfMap map, BingMapsConfigWindow configWindow, BingMapsOverlay bingOverlay)
        {
            bingOverlay = new BingMapsOverlay(configWindow.BingMapsKey, (Wpf.BingMapsMapType)configWindow.BingMapsStyle);//new BingMapsOverlay(configWindow.BingMapsKey);
            bingOverlay.Logo = null;
            bingOverlay.Name = GisEditor.LanguageManager.GetStringResource("BingMapsConfigWindowTitle");
            bingOverlay.TileType = TileType.HybridTile;
            bingOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            bingOverlay.DrawingException += new EventHandler<DrawingExceptionTileOverlayEventArgs>(BingOverlay_DrawingException);
            bingOverlay.RefreshCache();
            BaseMapsHelper.AddOverlayInGoogleProjection(bingOverlay, map);
            return bingOverlay;
        }

        public static OpenStreetMapOverlay AddOpenStreetMapOverlay(GisEditorWpfMap map)
        {
            OpenStreetMapOverlay osmOverlay = new OpenStreetMapOverlay();
            osmOverlay.TileType = TileType.HybridTile;
            osmOverlay.Name = "OpenStreetMap";
            osmOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            osmOverlay.DrawingException += new EventHandler<DrawingExceptionTileOverlayEventArgs>(OsmOverlay_DrawingException);
            osmOverlay.RefreshCache();
            BaseMapsHelper.AddOverlayInGoogleProjection(osmOverlay, map);
            return osmOverlay;
        }

        public static void AddZoomLevels(this ZoomLevelSet zoomLevelSet, bool addExtraZoomLevels = true)
        {
            var zoomLevels = zoomLevelSet.GetZoomLevels();
            if (zoomLevels.Count == 20)
            {
                foreach (var item in zoomLevels)
                {
                    item.Scale = Math.Round(item.Scale, 6);
                    zoomLevelSet.CustomZoomLevels.Add(item);
                }
                if (addExtraZoomLevels)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var currentScale = Math.Round(zoomLevelSet.CustomZoomLevels.LastOrDefault().Scale * 0.5, 6);
                        zoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(currentScale));
                    }
                }
            }
        }

        public static void SetZoomLevel(ZoomLevelSet zoomLevelSet, GisEditorWpfMap map)
        {
            zoomLevelSet.AddZoomLevels();
            map.ZoomLevelSet = zoomLevelSet;

            //the following line of code sets the zoom levels back to normal, if the Open Street Map was in the map previously.
            map.MinimumScale = map.ZoomLevelSet.GetZoomLevels().LastOrDefault().Scale;
        }

        private static void SetExtent(GisEditorWpfMap map)
        {
            if (map.Overlays.Count == 1)
            {
                RectangleShape extent = map.Overlays[0].GetBoundingBox();
                if (map.Overlays[0] is WorldMapKitMapOverlay)
                {
                    extent = new RectangleShape(-152.941811131969, 73.3011270968869, -63.395569383504, 4.88674180823698);
                }
                if (extent == null)
                {
                    extent = new RectangleShape(-130, 90, 130, -90);
                    var proj = new Proj4Projection();
                    proj.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
                    proj.ExternalProjectionParametersString = map.DisplayProjectionParameters;
                    proj.SyncProjectionParametersString();
                    proj.Open();
                    extent = proj.ConvertToExternalProjection(extent);
                }
                map.CurrentExtent = extent;
            }

            if (map.Overlays.Count <= 1
                && System.Windows.Application.Current != null
                && System.Windows.Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                var tmpSettings = System.Windows.Application.Current.MainWindow.Tag as Dictionary<string, string>;
                var currentExtentStrings = tmpSettings["CurrentExtent"].Split(',');

                if (currentExtentStrings.Length > 3)
                {
                    double minX = double.NaN;
                    double maxY = double.NaN;
                    double maxX = double.NaN;
                    double minY = double.NaN;

                    if (double.TryParse(currentExtentStrings[0], out minX)
                        && double.TryParse(currentExtentStrings[1], out maxY)
                        && double.TryParse(currentExtentStrings[2], out maxX)
                        && double.TryParse(currentExtentStrings[3], out minY))
                    {
                        RectangleShape extent = new RectangleShape(minX, maxY, maxX, minY);
                        Proj4Projection projection = new Proj4Projection(Proj4Projection.GetWgs84ParametersString(), map.DisplayProjectionParameters);
                        projection.SyncProjectionParametersString();
                        if (projection.CanProject())
                        {
                            try
                            {
                                projection.Open();
                                extent = projection.ConvertToExternalProjection(extent);
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            }
                            finally
                            {
                                projection.Close();
                            }
                        }

                        map.CurrentExtent = extent;
                    }
                }
            }
        }

        private static void RemoveAllBaseOverlays(GisEditorWpfMap map)
        {
            var baseOverlays = map.Overlays.Where(o => o is WorldMapKitMapOverlay ||
                                                                     o is OpenStreetMapOverlay ||
                                                                     o is BingMapsOverlay).ToArray();
            foreach (var overlay in baseOverlays)
            {
                map.Overlays.Remove(overlay);
            }
        }

        private static void AddOverlayInGoogleProjection(TileOverlay baseOverlay, GisEditorWpfMap map)
        {
            var extendedMap = map;
            string targetProj4 = Proj4Projection.GetGoogleMapParametersString();
            string sourceProj4 = extendedMap.DisplayProjectionParameters;
            var showConfirmationResult = System.Windows.Forms.DialogResult.Yes;
            if (ManagedProj4ProjectionExtension.CanProject(targetProj4, sourceProj4) && map.Overlays.Count > 0)
            {
                showConfirmationResult = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataRepositoryChangeProjectionWarningLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryProjectionWarningCaption"), System.Windows.Forms.MessageBoxButtons.YesNo);
            }

            if (showConfirmationResult == System.Windows.Forms.DialogResult.Yes)
            {
                RemoveAllBaseOverlays(map);
                extendedMap.Overlays.Insert(0, baseOverlay);
                extendedMap.DisplayProjectionParameters = targetProj4;

                //extendedMap.ReprojectMap(targetProj4);
                SetExtent(map);
                if (map.MapUnit != GeographyUnit.Meter)
                {
                    map.MapUnit = GeographyUnit.Meter;
                }
                extendedMap.Refresh(new Overlay[] { baseOverlay, extendedMap.ExtentOverlay });
            }
        }

        private static bool Proj4StringsEqual(string firstProj4, string secondProj4)
        {
            if (String.IsNullOrEmpty(firstProj4) || String.IsNullOrEmpty(secondProj4))
            {
                return false;
            }
            else
            {
                Dictionary<string, string> firstParameters = ParseParams(firstProj4);
                Dictionary<string, string> secondParameters = ParseParams(secondProj4);

                return AreTwoDictionariesAlike(firstParameters, secondParameters);
            }
        }

        private static bool AreTwoDictionariesAlike(Dictionary<string, string> firstParameters, Dictionary<string, string> secondParameters)
        {
            Dictionary<string, string> shortDictionary = null;
            Dictionary<string, string> longDictionary = null;
            if (firstParameters.Count > secondParameters.Count)
            {
                longDictionary = firstParameters;
                shortDictionary = secondParameters;
            }
            else
            {
                longDictionary = secondParameters;
                shortDictionary = firstParameters;
            }

            foreach (var item in shortDictionary)
            {
                if (!(longDictionary.ContainsKey(item.Key) && longDictionary.ContainsValue(item.Value)))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, string> ParseParams(string parameterString)
        {
            string upperCaseParameters = parameterString.ToUpperInvariant().Replace(" ", "");
            string[] parameters = upperCaseParameters.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> parameterDict = new Dictionary<string, string>();
            foreach (string parameter in parameters)
            {
                if (parameter.Contains("="))
                {
                    string[] keyValue = parameter.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    parameterDict.Add(keyValue[0], keyValue[1]);
                }
            }

            return parameterDict;
        }

        private static void OsmOverlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            RaiseDrawingException<OpenStreetMapOverlay>("OpenStreetMap", sender, e);
        }

        private static void WmkOverlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            RaiseDrawingException<WorldMapKitMapOverlay>("WorldMapKit", sender, e);
        }

        private static void BingOverlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            RaiseDrawingException<BingMapsOverlay>("Bing Maps", sender, e);
        }

        internal static void RaiseDrawingException<T>(string name, object sender, EventArgs e)
        {
            DrawingExceptionLayerEventArgs drawingExceptionLayerEventArgs = e as DrawingExceptionLayerEventArgs;
            DrawingExceptionTileOverlayEventArgs drawingExceptionTileOverlayEventArgs = e as DrawingExceptionTileOverlayEventArgs;

            if (!sender.Equals(default(T)))
            {
                var message = string.Format(CultureInfo.InvariantCulture, ConnectionFailed, name);
                var font = new GeoFont("Arial", 10, DrawingFontStyles.Italic);

                if (drawingExceptionLayerEventArgs != null)
                {
                    if (drawingExceptionLayerEventArgs.Canvas.ClippingArea != null)
                    {
                        var clippingCenter = drawingExceptionLayerEventArgs.Canvas.ClippingArea.GetCenterPoint();
                        drawingExceptionLayerEventArgs.Canvas.DrawTextWithWorldCoordinate(message, font, new GeoSolidBrush(GeoColor.StandardColors.GrayText), clippingCenter.X, clippingCenter.Y, DrawingLevel.LabelLevel);
                        drawingExceptionLayerEventArgs.Cancel = true;
                    }
                    else
                    {
                        var drawingArea = drawingExceptionLayerEventArgs.Canvas.MeasureText(message, font);
                        drawingExceptionLayerEventArgs.Canvas.DrawTextWithScreenCoordinate(message, font, new GeoSolidBrush(GeoColor.StandardColors.GrayText), 20 + drawingArea.Width * .5f, 20 + drawingArea.Height * .5f, DrawingLevel.LabelLevel);
                        drawingExceptionLayerEventArgs.Cancel = true;
                    }
                }
                else if (drawingExceptionTileOverlayEventArgs != null)
                {
                    if (drawingExceptionTileOverlayEventArgs.Canvas.ClippingArea != null)
                    {
                        var clippingCenter = drawingExceptionTileOverlayEventArgs.Canvas.ClippingArea.GetCenterPoint();
                        drawingExceptionTileOverlayEventArgs.Canvas.DrawTextWithWorldCoordinate(message, font, new GeoSolidBrush(GeoColor.StandardColors.GrayText), clippingCenter.X, clippingCenter.Y, DrawingLevel.LabelLevel);
                        drawingExceptionTileOverlayEventArgs.Cancel = true;
                    }
                    else
                    {
                        var drawingArea = drawingExceptionTileOverlayEventArgs.Canvas.MeasureText(message, font);
                        drawingExceptionTileOverlayEventArgs.Canvas.DrawTextWithScreenCoordinate(message, font, new GeoSolidBrush(GeoColor.StandardColors.GrayText), 20 + drawingArea.Width * .5f, 20 + drawingArea.Height * .5f, DrawingLevel.LabelLevel);
                        drawingExceptionTileOverlayEventArgs.Cancel = true;
                    }
                }
            }
        }
    }
}