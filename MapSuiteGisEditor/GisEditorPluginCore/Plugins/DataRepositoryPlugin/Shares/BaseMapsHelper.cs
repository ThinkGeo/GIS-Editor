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
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using ThinkGeo.Core;

using ThinkGeo.UI.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class BaseMapsHelper
    {
        private const string DefaultThinkGeoClientId = "AOf22-EmFgIEeK4qkdx5HhwbkBjiRCmIDbIYuP8jWbc~";
        private const string DefaultThinkGeoClientSecret = "xK0pbuywjaZx4sqauaga8DMlzZprz0qQSjLTow90EhBx5D8gFd2krw~~";
        private const string DefaultWorldMapsStyleUri = "https://tiles.preludemaps.com/styles/WorldStreets_Light/style.json";
        internal const string WorldMapsOverlayTag = "WorldMapKitOverlay";

        internal static string ThinkGeoCloudClientId
        {
            get
            {
                var value = GetSettingValue("ThinkGeoCloudClientId", "WorldMapKitClientId");
                return string.IsNullOrWhiteSpace(value) ? DefaultThinkGeoClientId : value;
            }
        }

        internal static string ThinkGeoCloudClientSecret
        {
            get
            {
                var value = GetSettingValue("ThinkGeoCloudClientSecret", "WorldMapKitPrivateKey", "WorldMapKitSecret");
                return string.IsNullOrWhiteSpace(value) ? DefaultThinkGeoClientSecret : value;
            }
        }

        internal static string WmkClientId => ThinkGeoCloudClientId;

        internal static string WmkPrivateKey => ThinkGeoCloudClientSecret;
        internal static readonly string ConnectionFailed = "{0} service connection\r\nfailed or timeout.";

        internal sealed class WorldMapsStyleOption
        {
            public WorldMapsStyleOption(string name, string styleJsonUri)
            {
                Name = name;
                StyleJsonUri = styleJsonUri;
            }

            public string Name { get; }

            public string StyleJsonUri { get; }
        }

        internal static IReadOnlyList<WorldMapsStyleOption> WorldMapsStyleOptions => new[]
        {
            new WorldMapsStyleOption("Default", ResolveWorldMapsStyleUri(DefaultWorldMapsStyleUri, "ThinkGeoVectorMapsStyleDefault", "WorldMapKitStyleDefault")),
            new WorldMapsStyleOption("Light", ResolveWorldMapsStyleUri(DefaultWorldMapsStyleUri, "ThinkGeoVectorMapsStyleLight", "WorldMapKitStyleLight")),
            new WorldMapsStyleOption("Dark", ResolveWorldMapsStyleUri(DefaultWorldMapsStyleUri, "ThinkGeoVectorMapsStyleDark", "WorldMapKitStyleDark")),
            new WorldMapsStyleOption("Transparent", ResolveWorldMapsStyleUri(DefaultWorldMapsStyleUri, "ThinkGeoVectorMapsStyleTransparent", "WorldMapKitStyleTransparent"))
        };

        public static async Task<LayerOverlay> AddWorldMapKitOverlayAsync(GisEditorWpfMap map)
        {
            if (map == null) return null;

            if (string.IsNullOrEmpty(map.DisplayProjectionParameters))
            {
                map.DisplayProjectionParameters = Proj4Projection.GetEpsgParametersString(4326);
            }

            var wmkOverlay = CreateWorldMapsOverlay();
            ConfigureWorldMapsOverlay(wmkOverlay, map, map.DisplayProjectionParameters);

            if (map.MapUnit == GeographyUnit.Meter || map.MapUnit == GeographyUnit.DecimalDegree)
            {
                BaseMapsHelper.RemoveAllBaseOverlays(map);
                map.Overlays.Insert(0, wmkOverlay);
                await SetExtentAsync(map);
                await map.RefreshAsync(wmkOverlay);
            }
            else
            {
                await AddOverlayInGoogleProjectionAsync(wmkOverlay, map);
            }
            return wmkOverlay;
        }

        private static string GetSettingValue(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                var envValue = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    return envValue.Trim();
                }

                var appValue = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrWhiteSpace(appValue))
                {
                    return appValue.Trim();
                }
            }

            return string.Empty;
        }

        public static async Task<ThinkGeoCloudRasterMapsOverlay> AddThinkGeoCloudRasterMapsOverlayAsync(GisEditorWpfMap map)
        {
            BingMapsConfigWindow configWindow = new BingMapsConfigWindow();
            ThinkGeoCloudRasterMapsOverlay rasterOverlay = null;
            if (!string.IsNullOrEmpty(configWindow.BingMapsKey) && !string.IsNullOrEmpty(configWindow.ClientSecret))
            {
                if (((BingMapsConfigViewModel)configWindow.DataContext).Validate())
                {
                    rasterOverlay = await AddThinkGeoCloudRasterMapsOverlayToMapAsync(map, configWindow, rasterOverlay);
                }
            }
            else
            {
                if (configWindow.ShowDialog().GetValueOrDefault())
                {
                    rasterOverlay = await AddThinkGeoCloudRasterMapsOverlayToMapAsync(map, configWindow, rasterOverlay);
                }
            }
            return rasterOverlay;
        }

        private static async Task<ThinkGeoCloudRasterMapsOverlay> AddThinkGeoCloudRasterMapsOverlayToMapAsync(GisEditorWpfMap map, BingMapsConfigWindow configWindow, ThinkGeoCloudRasterMapsOverlay rasterOverlay)
        {
            var clientId = string.IsNullOrWhiteSpace(configWindow.BingMapsKey) ? ThinkGeoCloudClientId : configWindow.BingMapsKey;
            var clientSecret = string.IsNullOrWhiteSpace(configWindow.ClientSecret) ? ThinkGeoCloudClientSecret : configWindow.ClientSecret;
            rasterOverlay = new ThinkGeoCloudRasterMapsOverlay(clientId, clientSecret, configWindow.BingMapsStyle);
            rasterOverlay.Name = GisEditor.LanguageManager.GetStringResource("BingMapsConfigWindowTitle");
            rasterOverlay.TileType = TileType.PreloadDataMultiTile;
            rasterOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            rasterOverlay.DrawingException += new EventHandler<DrawingExceptionTileOverlayEventArgs>(ThinkGeoCloudRasterOverlay_DrawingException);
            rasterOverlay.RefreshCache();
            await BaseMapsHelper.AddOverlayInGoogleProjectionAsync(rasterOverlay, map);
            return rasterOverlay;
        }

        public static async Task<OpenStreetMapOverlay> AddOpenStreetMapOverlayAsync(GisEditorWpfMap map)
        {
            OpenStreetMapOverlay osmOverlay = new OpenStreetMapOverlay();
            osmOverlay.TileType = TileType.PreloadDataMultiTile;
            osmOverlay.Name = "OpenStreetMap";
            osmOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
            osmOverlay.DrawingException += new EventHandler<DrawingExceptionTileOverlayEventArgs>(OsmOverlay_DrawingException);
            osmOverlay.RefreshCache();
            await BaseMapsHelper.AddOverlayInGoogleProjectionAsync(osmOverlay, map);
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
            map.ZoomScales.Clear();
            foreach (var item in zoomLevelSet.CustomZoomLevels)
            {
                map.ZoomScales.Add(item.Scale);
            }

            //the following line of code sets the zoom levels back to normal, if the Open Street Map was in the map previously.
            map.MinimumScale = map.ZoomScales.LastOrDefault();
        }

        private static async Task SetExtentAsync(GisEditorWpfMap map)
        {
            if (map == null) return;

            if ((map.Overlays.Count <= 1 || IsPlaceholderExtent(map.CurrentExtent))
                && TryGetStoredExtent(map, out var storedExtent))
            {
                map.CurrentExtent = storedExtent;
                return;
            }

            if (map.Overlays.Count > 0 && (map.Overlays.Count == 1 || IsPlaceholderExtent(map.CurrentExtent)))
            {
                await map.Overlays[0].OpenAsync();
                RectangleShape extent = map.Overlays[0].GetBoundingBox();
                if (IsWorldMapsOverlay(map.Overlays[0])
                    || map.Overlays[0] is OpenStreetMapOverlay
                    || map.Overlays[0] is ThinkGeoCloudRasterMapsOverlay)
                {
                    extent = GetThinkGeoMapsExtent(map);
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
        }

        private static bool TryGetStoredExtent(GisEditorWpfMap map, out RectangleShape extent)
        {
            extent = null;
            if (System.Windows.Application.Current == null || System.Windows.Application.Current.MainWindow == null)
            {
                return false;
            }

            if (!(System.Windows.Application.Current.MainWindow.Tag is Dictionary<string, string> tmpSettings))
            {
                return false;
            }

            if (!tmpSettings.TryGetValue("CurrentExtent", out var extentText) || string.IsNullOrWhiteSpace(extentText))
            {
                return false;
            }

            var currentExtentStrings = extentText.Split(',');
            if (currentExtentStrings.Length <= 3) return false;

            double minX;
            double maxY;
            double maxX;
            double minY;

            if (!double.TryParse(currentExtentStrings[0], out minX)
                || !double.TryParse(currentExtentStrings[1], out maxY)
                || !double.TryParse(currentExtentStrings[2], out maxX)
                || !double.TryParse(currentExtentStrings[3], out minY))
            {
                return false;
            }

            extent = new RectangleShape(minX, maxY, maxX, minY);
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

            return true;
        }

        private static bool IsPlaceholderExtent(RectangleShape extent)
        {
            if (extent == null) return true;
            const double epsilon = 1e-9;
            return Math.Abs(extent.UpperLeftPoint.X) < epsilon
                && Math.Abs(extent.UpperLeftPoint.Y - 1) < epsilon
                && Math.Abs(extent.LowerRightPoint.X - 1) < epsilon
                && Math.Abs(extent.LowerRightPoint.Y) < epsilon;
        }

        private static void RemoveAllBaseOverlays(GisEditorWpfMap map)
        {
            var baseOverlays = map.Overlays.Where(o => IsWorldMapsOverlay(o) ||
                                                      o is OpenStreetMapOverlay ||
                                                      o is ThinkGeoCloudRasterMapsOverlay).ToArray();
            foreach (var overlay in baseOverlays)
            {
                map.Overlays.Remove(overlay);
            }
        }

        internal static RectangleShape GetThinkGeoMapsExtent(GisEditorWpfMap map)
        {
            RectangleShape extent = MaxExtents.ThinkGeoMaps;
            if (map == null) return extent;

            var targetProj4 = map.DisplayProjectionParameters;
            if (string.IsNullOrWhiteSpace(targetProj4))
            {
                return extent;
            }

            var sourceProj4 = Proj4Projection.GetGoogleMapParametersString();
            if (Proj4StringsEqual(sourceProj4, targetProj4))
            {
                return extent;
            }

            var projection = new Proj4Projection();
            projection.InternalProjectionParametersString = sourceProj4;
            projection.ExternalProjectionParametersString = targetProj4;
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

            return extent;
        }

        internal static bool IsWorldMapsOverlay(Overlay overlay)
        {
            if (overlay == null) return false;

            var layerOverlay = overlay as LayerOverlay;
            if (layerOverlay == null) return false;

            if (layerOverlay.Tag is string tag && tag.Equals(WorldMapsOverlayTag, StringComparison.Ordinal))
            {
                return true;
            }

            return layerOverlay.Layers.OfType<MvtTilesAsyncLayer>().Any();
        }

        internal static bool TryGetWorldMapsLayer(Overlay overlay, out MvtTilesAsyncLayer worldMapsLayer)
        {
            worldMapsLayer = null;
            var layerOverlay = overlay as LayerOverlay;
            if (layerOverlay == null) return false;

            worldMapsLayer = layerOverlay.Layers.OfType<MvtTilesAsyncLayer>().FirstOrDefault();
            return worldMapsLayer != null;
        }

        internal static void ConfigureWorldMapsOverlay(LayerOverlay overlay, GisEditorWpfMap map, string targetProj4, GeographyUnit? targetUnitOverride = null)
        {
            if (overlay == null || map == null) return;
            if (!TryGetWorldMapsLayer(overlay, out var worldMapsLayer)) return;

            ConfigureWorldMapsLayer(worldMapsLayer, map, targetProj4, targetUnitOverride);
        }

        internal static void ConfigureWorldMapsLayer(MvtTilesAsyncLayer worldMapsLayer, GisEditorWpfMap map, string targetProj4, GeographyUnit? targetUnitOverride = null)
        {
            if (worldMapsLayer == null || map == null) return;

            if (string.IsNullOrWhiteSpace(worldMapsLayer.StyleJsonUri))
            {
                worldMapsLayer.StyleJsonUri = WorldMapsStyleOptions.FirstOrDefault()?.StyleJsonUri ?? DefaultWorldMapsStyleUri;
            }

            var targetUnit = targetUnitOverride ?? map.MapUnit;
            ApplyWorldMapsProjection(worldMapsLayer, targetProj4, targetUnit);
            worldMapsLayer.MapUnit = targetUnit;
            ApplyWorldMapsCache(worldMapsLayer);
        }

        private static void ApplyWorldMapsProjection(MvtTilesAsyncLayer layer, string targetProj4, GeographyUnit mapUnit)
        {
            if (layer == null) return;
            if (string.IsNullOrWhiteSpace(targetProj4)) return;

            var googleProj4 = Proj4Projection.GetGoogleMapParametersString();
            if (Proj4StringsEqual(googleProj4, targetProj4))
            {
                layer.ProjectionConverter = null;
                return;
            }

            if (mapUnit == GeographyUnit.DecimalDegree)
            {
                layer.ProjectionConverter = new ProjectionConverter(3857, 4326);
            }
            else
            {
                layer.ProjectionConverter = new ProjectionConverter(3857, targetProj4);
            }
        }

        private static LayerOverlay CreateWorldMapsOverlay()
        {
            var styleUri = WorldMapsStyleOptions.FirstOrDefault()?.StyleJsonUri ?? DefaultWorldMapsStyleUri;
            var worldMapsLayer = new MvtTilesAsyncLayer(styleUri)
            {
                Name = "ThinkGeo Maps",
                DrawingExceptionMode = DrawingExceptionMode.DrawException
            };
            worldMapsLayer.DrawingException += new EventHandler<DrawingExceptionLayerEventArgs>(WorldMapsLayer_DrawingException);

            var overlay = new LayerOverlay
            {
                TileType = TileType.SingleTile,
                Name = GisEditor.LanguageManager.GetStringResource("WorldMapKitName"),
                DrawingExceptionMode = DrawingExceptionMode.DrawException,
                IsBase = true,
                Tag = WorldMapsOverlayTag
            };
            overlay.Layers.Add(worldMapsLayer);

            ApplyWorldMapsCache(worldMapsLayer);
            return overlay;
        }

        private static void ApplyWorldMapsCache(MvtTilesAsyncLayer worldMapsLayer)
        {
            if (worldMapsLayer == null) return;
            if (string.IsNullOrWhiteSpace(TileOverlayExtension.TemporaryPath)) return;

            string cacheKey = GetStyleCacheKey(worldMapsLayer.StyleJsonUri);
            string cacheFolder = Path.Combine(TileOverlayExtension.TemporaryPath, "ThinkGeoVectorMaps", cacheKey);
            worldMapsLayer.VectorTileCache = new FileTileCache(cacheFolder);
        }

        private static string GetStyleCacheKey(string styleJsonUri)
        {
            if (string.IsNullOrWhiteSpace(styleJsonUri)) return "default";

            if (Uri.TryCreate(styleJsonUri, UriKind.Absolute, out var uri))
            {
                if (uri.Segments.Length >= 2)
                {
                    var segment = uri.Segments[uri.Segments.Length - 2].Trim('/').Trim();
                    if (!string.IsNullOrWhiteSpace(segment))
                    {
                        return SanitizePathSegment(segment);
                    }
                }
            }

            return SanitizePathSegment(styleJsonUri);
        }

        private static string SanitizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "default";

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }
            return value;
        }

        private static string ResolveWorldMapsStyleUri(string fallback, params string[] keys)
        {
            var value = GetSettingValue(keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static async Task AddOverlayInGoogleProjectionAsync(Overlay baseOverlay, GisEditorWpfMap map)
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
                await SetExtentAsync(map);
                if (map.MapUnit != GeographyUnit.Meter)
                {
                    map.MapUnit = GeographyUnit.Meter;
                }
                if (IsWorldMapsOverlay(baseOverlay))
                {
                    ConfigureWorldMapsOverlay(baseOverlay as LayerOverlay, map, targetProj4, GeographyUnit.Meter);
                }
                await extendedMap.RefreshAsync();
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

        private static void WorldMapsLayer_DrawingException(object sender, DrawingExceptionLayerEventArgs e)
        {
            RaiseDrawingException<MvtTilesAsyncLayer>("ThinkGeo Maps", sender, e);
        }

        private static void ThinkGeoCloudRasterOverlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            RaiseDrawingException<ThinkGeoCloudRasterMapsOverlay>("ThinkGeo Cloud Raster Maps", sender, e);
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
                        drawingExceptionLayerEventArgs.Canvas.DrawTextWithWorldCoordinate(message, font, new GeoSolidBrush(GeoColors.GrayText), clippingCenter.X, clippingCenter.Y, DrawingLevel.LabelLevel);
                        drawingExceptionLayerEventArgs.Cancel = true;
                    }
                    else
                    {
                        var drawingArea = drawingExceptionLayerEventArgs.Canvas.MeasureText(message, font);
                        drawingExceptionLayerEventArgs.Canvas.DrawTextWithScreenCoordinate(message, font, new GeoSolidBrush(GeoColors.GrayText), 20 + drawingArea.Width * .5f, 20 + drawingArea.Height * .5f, DrawingLevel.LabelLevel);
                        drawingExceptionLayerEventArgs.Cancel = true;
                    }
                }
                else if (drawingExceptionTileOverlayEventArgs != null)
                {
                    if (drawingExceptionTileOverlayEventArgs.Canvas.ClippingArea != null)
                    {
                        var clippingCenter = drawingExceptionTileOverlayEventArgs.Canvas.ClippingArea.GetCenterPoint();
                        drawingExceptionTileOverlayEventArgs.Canvas.DrawTextWithWorldCoordinate(message, font, new GeoSolidBrush(GeoColors.GrayText), clippingCenter.X, clippingCenter.Y, DrawingLevel.LabelLevel);
                        drawingExceptionTileOverlayEventArgs.Cancel = true;
                    }
                    else
                    {
                        var drawingArea = drawingExceptionTileOverlayEventArgs.Canvas.MeasureText(message, font);
                        drawingExceptionTileOverlayEventArgs.Canvas.DrawTextWithScreenCoordinate(message, font, new GeoSolidBrush(GeoColors.GrayText), 20 + drawingArea.Width * .5f, 20 + drawingArea.Height * .5f, DrawingLevel.LabelLevel);
                        drawingExceptionTileOverlayEventArgs.Cancel = true;
                    }
                }
            }
        }
    }
}
