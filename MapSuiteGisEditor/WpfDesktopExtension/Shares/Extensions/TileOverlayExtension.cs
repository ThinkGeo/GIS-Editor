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
using System.IO;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class TileOverlayExtension
    {
        public static string TemporaryPath { get; set; }
        private static int requestRefreshBufferTimeInMillisecond = 200;

        static TileOverlayExtension()
        { }

        public static int RefreshBufferTimeInMillisecond
        {
            get { return requestRefreshBufferTimeInMillisecond; }
            set { requestRefreshBufferTimeInMillisecond = value; }
        }

        public static TimeSpan RefreshBufferTime
        {
            get { return TimeSpan.FromMilliseconds(requestRefreshBufferTimeInMillisecond); }
        }

        public static void RefreshWithBufferSettings(this Overlay overlay)
        {
            overlay.Refresh(TimeSpan.FromMilliseconds(RefreshBufferTimeInMillisecond), RequestDrawingBufferTimeType.ResetDelay);
        }

        public static void Invalidate(this TileOverlay overlay)
        {
            Invalidate(overlay, true);
        }

        public static void Invalidate(this TileOverlay overlay, bool delay)
        {
            if (overlay.TileCache != null) overlay.RefreshCache();
            if (overlay.MapArguments != null)
            {
                if (delay) overlay.RefreshWithBufferSettings();
                else overlay.Refresh();
            }
        }

        public static void RefreshCache(this TileOverlay overlay)
        {
            overlay.RefreshCache(true);
        }

        public static void RefreshCache(this TileOverlay tileOverlay, RefreshCacheMode mode)
        {
            bool enabled = mode == RefreshCacheMode.ApplyNewCache;
            tileOverlay.RefreshCache(enabled);
        }

        public static void RefreshCache(this TileOverlay overlay, bool enabled)
        {
            BingMapsOverlay bingOverlay = overlay as BingMapsOverlay;
            WorldMapKitMapOverlay wmkOverlay = overlay as WorldMapKitMapOverlay;
            OpenStreetMapOverlay osmOverlay = overlay as OpenStreetMapOverlay;

            string cacheId = string.Empty;
            string cacheFolder = string.Empty;
            bool needRefresh = overlay.TileCache == null;
            FileBitmapTileCache tileCache = null;

            if (bingOverlay != null)
            {
                cacheId = bingOverlay.MapType.ToString();
                cacheFolder = Path.Combine(TemporaryPath, "BingMap");
                bingOverlay.TileCache = null;
                needRefresh = true;

                if (enabled) tileCache = GetTileCache(overlay, cacheFolder, cacheId);
                bingOverlay.TileCache = tileCache;
                overlay.TileCache = tileCache;
            }
            else if (wmkOverlay != null)
            {
                //cacheId = wmkOverlay.Projection.ToString();
                cacheId = GetDefaultCacheId(wmkOverlay.LayerType, wmkOverlay.Projection, wmkOverlay.MapType);
                string layerType = wmkOverlay.LayerType.ToString();
                if (layerType == Layers.WorldMapKitLayerType.Default.ToString())
                {
                    layerType = Layers.WorldMapKitLayerType.OSMWorldMapKitLayer.ToString();
                }
                cacheFolder = Path.Combine(TemporaryPath, layerType);
                needRefresh = true;

                if (enabled) tileCache = GetTileCache(overlay, cacheFolder, cacheId);
                overlay.TileCache = tileCache;
            }
            else if (osmOverlay != null)
            {
                cacheId = "SphereMercator";
                cacheFolder = Path.Combine(TemporaryPath, "OpenStreetMap");
                osmOverlay.TileCache = null;

                if (enabled) tileCache = GetTileCache(overlay, cacheFolder, cacheId);
                osmOverlay.TileCache = tileCache;
                overlay.TileCache = tileCache;
            }
            else
            {
                cacheId = Guid.NewGuid().ToString();
                cacheFolder = TemporaryPath;
                if (enabled) tileCache = GetTileCache(overlay, cacheFolder, cacheId);
                overlay.TileCache = tileCache;
                needRefresh = true;
            }

            if (needRefresh)
            {
                BitmapTileCache tempTileCache = overlay.TileCache as BitmapTileCache;
                if (!overlay.IsBase && tempTileCache != null)
                {
                    Task.Factory.StartNew(cache =>
                    {
                        BitmapTileCache removingCache = (BitmapTileCache)cache;
                        lock (removingCache)
                        {
                            try { removingCache.ClearCache(); }
                            catch { }
                        }
                    }, tempTileCache);
                }
            }
        }

        private static string GetDefaultCacheId(Layers.WorldMapKitLayerType layerType, Layers.WorldMapKitProjection projection, Layers.WorldMapKitMapType mapType)
        {
            Layers.WorldMapKitLayerType layerTypeTemp = layerType == Layers.WorldMapKitLayerType.Default ? Layers.WorldMapKitLayerType.OSMWorldMapKitLayer : layerType;

            string cacheIdFormat = projection == Layers.WorldMapKitProjection.SphericalMercator ? "{0}_Projected_{1}" : "{0}_{1}";

            return string.Format(cacheIdFormat, layerTypeTemp.ToString("g"), mapType);
        }

        private static FileBitmapTileCache GetTileCache(TileOverlay overlay, string cacheDirectory, string cacheId)
        {
            FileBitmapTileCache newCache = new FileBitmapTileCache(cacheDirectory, cacheId);
            if (overlay.MapArguments != null)
            {
                newCache.TileMatrix.BoundingBoxUnit = overlay.MapArguments.MapUnit;
            }

            if (newCache != null)
            {
                newCache.TileMatrix.TileHeight = overlay.TileHeight;
                newCache.TileMatrix.TileWidth = overlay.TileWidth;
            }

            return newCache;
        }

        public static void RefreshCache(this TileOverlay overlay, bool enabled, string cacheId, string cacheDirectory)
        {
            BitmapTileCache tempTileCache = overlay.TileCache as BitmapTileCache;

            FileBitmapTileCache newCache = null;
            if (enabled)
            {
                newCache = new FileBitmapTileCache(cacheDirectory, cacheId);
                if (overlay.MapArguments != null)
                {
                    newCache.TileMatrix.BoundingBoxUnit = overlay.MapArguments.MapUnit;
                }
            }

            if (newCache != null)
            {
                newCache.TileMatrix.TileHeight = overlay.TileHeight;
                newCache.TileMatrix.TileWidth = overlay.TileWidth;
            }

            overlay.TileCache = newCache;
            if (!overlay.IsBase && tempTileCache != null)
            {
                Task.Factory.StartNew(cache =>
                {
                    BitmapTileCache removingCache = (BitmapTileCache)cache;
                    lock (removingCache)
                    {
                        try { removingCache.ClearCache(); }
                        catch { }
                    }
                }, tempTileCache);
            }
        }

        public static void ClearCaches(this TileOverlay overlay, RectangleShape extent)
        {
            BitmapTileCache tileCache = overlay.TileCache;
            if (tileCache != null)
            {
                lock (tileCache)
                {
                    try
                    {
                        tileCache.DeleteTiles(extent);
                    }
                    catch { }
                }
            }
        }

        public static void ClearCaches(this TileOverlay overlay)
        {
            BitmapTileCache tileCache = overlay.TileCache;
            if (tileCache != null)
            {
                lock (tileCache)
                {
                    try
                    {
                        tileCache.ClearCache();
                    }
                    catch { }
                }
            }
        }

        public static void OpenCacheDirectory(this TileOverlay overlay)
        {
            FileBitmapTileCache tileCache = overlay.TileCache as FileBitmapTileCache;
            if (tileCache != null)
            {
                if (Directory.Exists(tileCache.CacheDirectory))
                {
                    ProcessUtils.OpenPath(tileCache.CacheDirectory);
                }
            }
        }

        public static bool CacheDirectoryExist(FileBitmapTileCache tileCache)
        {
            bool isExist = false;
            if (tileCache != null)
            {
                if (Directory.Exists(tileCache.CacheDirectory))
                {
                    isExist = true;
                }
            }

            return isExist;
        }
    }
}