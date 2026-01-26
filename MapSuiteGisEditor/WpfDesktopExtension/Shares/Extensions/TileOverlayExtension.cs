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
using System.Reflection;
using System.Threading.Tasks;
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

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
            if (overlay == null) return;

            try
            {
                var refreshAsync = overlay.GetType().GetMethod("RefreshAsync", BindingFlags.Instance | BindingFlags.Public);
                if (refreshAsync != null)
                {
                    refreshAsync.Invoke(overlay, null);
                    return;
                }

                var refresh = overlay.GetType().GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public);
                refresh?.Invoke(overlay, null);
            }
            catch
            {
                // Best-effort only.
            }
        }

        public static async Task Invalidate(this TileOverlay overlay)
        {
            await Invalidate(overlay, true);
        }

        public static async Task Invalidate(this TileOverlay overlay, bool delay)
        {
            if (overlay.TileCache != null) overlay.RefreshCache();
            if (overlay.MapArguments != null)
            {
                //if (delay) overlay.RefreshWithBufferSettings();
                //else 
                    await overlay.RefreshAsync();
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
            ThinkGeoCloudRasterMapsOverlay rasterOverlay = overlay as ThinkGeoCloudRasterMapsOverlay;
            OpenStreetMapOverlay osmOverlay = overlay as OpenStreetMapOverlay;

            string cacheId = string.Empty;
            string cacheFolder = string.Empty;
            bool needRefresh = overlay.TileCache == null;
            FileRasterTileCache tileCache = null;

            if (rasterOverlay != null)
            {
                cacheId = rasterOverlay.MapType.ToString();
                cacheFolder = Path.Combine(TemporaryPath, "ThinkGeoCloudRasterMaps");
                rasterOverlay.TileCache = null;
                needRefresh = true;

                if (enabled) tileCache = GetTileCache(overlay, cacheFolder, cacheId);
                rasterOverlay.TileCache = tileCache;
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
                RasterTileCache tempTileCache = overlay.TileCache as RasterTileCache;
                if (!overlay.IsBase && tempTileCache != null)
                {
                    Task.Factory.StartNew(cache =>
                    {
                        RasterTileCache removingCache = (RasterTileCache)cache;
                        lock (removingCache)
                        {
                            try { removingCache.ClearCache(); }
                            catch { }
                        }
                    }, tempTileCache);
                }
            }
        }

        private static FileRasterTileCache GetTileCache(TileOverlay overlay, string cacheDirectory, string cacheId)
        {
            FileRasterTileCache newCache = new FileRasterTileCache(cacheDirectory, cacheId);
            //if (overlay.MapArguments != null)
            //{
            //    newCache.TileMatrix.BoundingBoxUnit = overlay.MapArguments.MapUnit;
            //}

            //if (newCache != null)
            //{
            //    newCache.TileMatrix.TileHeight = overlay.TileHeight;
            //    newCache.TileMatrix.TileWidth = overlay.TileWidth;
            //}

            return newCache;
        }

        public static void RefreshCache(this TileOverlay overlay, bool enabled, string cacheId, string cacheDirectory)
        {
            RasterTileCache tempTileCache = overlay.TileCache as RasterTileCache;

            FileRasterTileCache newCache = null;
            if (enabled)
            {
                newCache = new FileRasterTileCache(cacheDirectory, cacheId);
                if (overlay.MapArguments != null)
                {
              //      newCache.TileMatrix.BoundingBoxUnit = overlay.MapArguments.MapUnit;
                }
            }

            //if (newCache != null)
            //{
            //    newCache.TileMatrix.TileHeight = overlay.TileHeight;
            //    newCache.TileMatrix.TileWidth = overlay.TileWidth;
            //}

            overlay.TileCache = newCache;
            if (tempTileCache != null)
            {
                Task.Factory.StartNew(cache =>
                {
                    RasterTileCache removingCache = (RasterTileCache)cache;
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
            RasterTileCache tileCache = overlay.TileCache;
            if (tileCache != null)
            {
                lock (tileCache)
                {
                    try
                    {
                        //tileCache.DeleteTiles(extent);
                    }
                    catch { }
                }
            }
        }

        public static void ClearCaches(this TileOverlay overlay)
        {
            RasterTileCache tileCache = overlay.TileCache;
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
            FileRasterTileCache tileCache = overlay.TileCache as FileRasterTileCache;
            if (tileCache != null)
            {
                if (Directory.Exists(tileCache.CacheDirectory))
                {
                    ProcessUtils.OpenPath(tileCache.CacheDirectory);
                }
            }
        }

        public static bool CacheDirectoryExist(FileRasterTileCache tileCache)
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
