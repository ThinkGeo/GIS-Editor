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
using System.Net;
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Backward-compatible overlay kept for GIS Editor extensions.
    /// 
    /// Map Suite v10 shipped with a "WorldMapKit" service based overlay.
    /// In ThinkGeo v14+, the recommended base maps are provided by modern
    /// built-in overlays (for example OpenStreetMapOverlay or ThinkGeoCloudRasterMapsOverlay).
    /// 
    /// To keep the editor extension buildable and runnable, this class maps
    /// the old WorldMapKit overlay to the built-in <see cref="OpenStreetMapOverlay"/>.
    /// </summary>
    [Serializable]
    public class WorldMapKitMapOverlay : OpenStreetMapOverlay
    {
        private string clientId;
        private string privateKey;
        private int timeoutInSeconds;
        private WebProxy webProxy;
        private WorldMapKitMapType mapType;
        private WorldMapKitProjection projection;

        // These events existed on the legacy implementation. They are kept to
        // avoid breaking consumers, even though the underlying OSM overlay does
        // not raise them.
        public event EventHandler<SendingWebRequestEventArgs> SendingWebRequest;
        public event EventHandler<SentWebRequestEventArgs> SentWebRequest;

        public WorldMapKitMapOverlay()
            : this(null, string.Empty, string.Empty)
        {
        }

        public WorldMapKitMapOverlay(WebProxy webProxy)
            : this(webProxy, string.Empty, string.Empty)
        {
        }

        public WorldMapKitMapOverlay(string clientId, string privateKey)
            : this(null, clientId, privateKey)
        {
        }

        public WorldMapKitMapOverlay(WebProxy webProxy, string clientId, string privateKey)
            : base()
        {
            this.clientId = clientId;
            this.privateKey = privateKey;
            this.webProxy = webProxy;
            mapType = WorldMapKitMapType.Default;
            projection = WorldMapKitProjection.SphericalMercator;

            // Keep behavior similar to the legacy overlay.
            IsBase = true;
            DrawingExceptionMode = DrawingExceptionMode.DrawException;
            Attribution = "© ThinkGeo © OpenStreetMap contributors";

            // Preserve the legacy tile cache behavior (best-effort; depends on the version).
            try
            {
                TileCache = new FileRasterTileCache(GetTemporaryFolder(), GetDefaultCacheId());
            }
            catch
            {
                // Ignore if the tile cache API changes between versions.
            }

            ApplyNetworkSettings();
        }

        /// <summary>
        /// Gets or sets the length of time, in seconds, before the request times out.
        /// </summary>
        public new int TimeoutInSeconds
        {
            get => timeoutInSeconds;
            set
            {
                timeoutInSeconds = value;
                ApplyNetworkSettings();
            }
        }

        /// <summary>
        /// Gets or sets a value that is your client id. (Kept for compatibility.)
        /// </summary>
        public string ClientId
        {
            get => clientId;
            set => clientId = value;
        }

        /// <summary>
        /// Gets or sets a value that is your private key. (Kept for compatibility.)
        /// </summary>
        public string PrivateKey
        {
            get => privateKey;
            set => privateKey = value;
        }

        public WorldMapKitMapType MapType
        {
            get => mapType;
            set => mapType = value;
        }

        public WorldMapKitProjection Projection
        {
            get => projection;
            set => projection = value;
        }

        /// <summary>
        /// Gets or sets the proxy used for requesting web resources.
        /// </summary>
        public new WebProxy WebProxy
        {
            get => webProxy;
            set
            {
                webProxy = value;
                ApplyNetworkSettings();
            }
        }

        private void ApplyNetworkSettings()
        {
            // Apply proxy/timeout directly to the base overlay.
            base.WebProxy = webProxy;
            if (timeoutInSeconds > 0)
            {
                base.TimeoutInSeconds = timeoutInSeconds;
            }
        }

        protected virtual void OnSendingWebRequest(SendingWebRequestEventArgs e)
        {
            SendingWebRequest?.Invoke(this, e);
        }

        protected virtual void OnSentWebRequest(SentWebRequestEventArgs e)
        {
            SentWebRequest?.Invoke(this, e);
        }
    

        private static string GetTemporaryFolder()
        {
            // Keep cache in the user temp directory by default.
            var folder = Path.Combine(Path.GetTempPath(), "ThinkGeo", "TileCache");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string GetDefaultCacheId()
        {
            return "WorldMapKit";
        }
}
}
