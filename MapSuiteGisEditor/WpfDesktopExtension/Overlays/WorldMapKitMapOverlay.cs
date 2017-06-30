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
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// This class represents an overlay that requesting images from OpenStreet Map Service.
    /// </summary>
    [Serializable]
    public class WorldMapKitMapOverlay : TileOverlay
    {
        private const int defaultTileWidth = 256;
        private const int defaultTileHeight = 256;

        [Obfuscation(Exclude = true)]
        private WorldMapKitLayer wktLayer;

        [Obfuscation(Exclude = true)]
        private int timeoutInSeconds;

        public event EventHandler<SendingWebRequestEventArgs> SendingWebRequest;

        public event EventHandler<SentWebRequestEventArgs> SentWebRequest;


        /// <summary>
        /// Constructor of WorldMapKitMapOverlay class.
        /// </summary>
        public WorldMapKitMapOverlay()
            : this(null, string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Constructor of WorldMapKitMapOverlay class.
        /// </summary>
        /// <param name="webProxy">The proxy used for requesting a Web Response</param>
        public WorldMapKitMapOverlay(WebProxy webProxy)
            : this(webProxy, string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Constructor of WorldMapKitMapOverlay class.
        /// </summary>
        /// <param name="clientId">The clientId for the WMS Server.</param>
        /// <param name="privateKey">The privateKey for the WMS Server.</param>
        public WorldMapKitMapOverlay(string clientId, string privateKey)
            : this(null, clientId, privateKey)
        {
        }

        /// <summary>
        /// Constructor of WorldMapKitMapOverlay class.
        /// </summary>
        /// <param name="webProxy">Proxy to use for the WMS Server.</param>
        /// <param name="clientId">The clientId for the WMS Server.</param>
        /// <param name="privateKey">The privateKey for the WMS Server.</param>
        public WorldMapKitMapOverlay(WebProxy webProxy, string clientId, string privateKey)
            : base()
        {
            wktLayer = new FastWorldMapKitRasterLayer(clientId, privateKey);
            wktLayer.SendingWebRequest += new EventHandler<SendingWebRequestEventArgs>(osmLayer_SendingWebRequest);
            wktLayer.SentWebRequest += new EventHandler<SentWebRequestEventArgs>(osmLayer_SentWebRequest);
            wktLayer.TimeoutInSecond = 10;
            wktLayer.TileCache = null;
            TileCache = new FileBitmapTileCache(GetTemporaryFolder(), GetDefaultCacheId());
            TileWidth = defaultTileWidth;
            TileHeight = defaultTileHeight;
            DrawingExceptionMode = DrawingExceptionMode.DrawException;
            IsBase = true;
            Attribution = "© ThinkGeo © OpenStreetMap contributors";
        }

        /// <summary>
        /// Gets or sets the length of time, in seconds, before the request times out.
        /// </summary>
        public int TimeoutInSeconds
        {
            get { return timeoutInSeconds; }
            set { timeoutInSeconds = value; }
        }

        /// <summary>
        /// Gets or sets a value that is your Client Id.
        /// </summary>
        public string ClientId
        {
            get { return wktLayer.ClientId; }
            set { wktLayer.ClientId = value; }
        }

        /// <summary>
        /// Gets or sets a value that is map type.
        /// </summary>
        public Layers.WorldMapKitMapType MapType
        {
            get { return wktLayer.MapType; }
            set
            {
                wktLayer.MapType = value;

                if (TileCache != null && IsDefaultCacheId(TileCache.CacheId))
                {
                    TileCache.CacheId = GetDefaultCacheId();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that is unique to your client ID, please keep your key secure.
        /// </summary>
        public string PrivateKey
        {
            get { return wktLayer.PrivateKey; }
            set { wktLayer.PrivateKey = value; }
        }

        /// <summary>
        /// Gets or sets the projection for the this overlay.
        /// </summary>
        /// <remarks>
        /// This property needs work with map unit setting on the map object.
        /// All overlay adding to the map must keep the same unit or else the map won't display properly.
        /// </remarks>
        public Layers.WorldMapKitProjection Projection
        {
            get { return wktLayer.Projection; }
            set { wktLayer.Projection = value; }
        }

        /// <summary>
        /// Gets or sets the WebProxy for using OpenStreet map service.
        /// </summary>
        public IWebProxy WebProxy
        {
            get { return wktLayer.WebProxy; }
            set { wktLayer.WebProxy = value; }
        }

        public Layers.WorldMapKitLayerType LayerType
        {
            get { return wktLayer.LayerType; }
            set { wktLayer.LayerType = value; }
        }

        /// <summary>
        /// This method gets a concrete tile class to form this overlay.
        /// </summary>
        /// <returns>A concrete Tile object that for requesting images from OpenStreet serverice.</returns>
        protected override Wpf.Tile GetTileCore()
        {
            LayerTile tile = new LayerTile();
            if (TileType == TileType.SingleTile)
            {
                tile.IsAsync = false;
            }

            return tile;
        }

        protected override void CloseCore()
        {
            base.CloseCore();
            if (wktLayer != null && wktLayer.IsOpen)
            {
                wktLayer.Close();
            }
        }

        protected override void OpenCore()
        {
            base.OpenCore();
            if (wktLayer != null)
            {
                wktLayer.Open();
            }
        }

        /// <summary>
        /// This method overrides from its base class TileOverlay.
        /// It actually requests image for the passed tile with the passed world extent.
        /// </summary>
        /// <param name="tile">A tile object that creating by the GetTileCore method. It's the tile which needs to be draw in this mehtod.</param>
        /// <param name="targetExtent">A world extent that to draw the passed tile object.</param>
        protected override void DrawTileCore(Wpf.Tile tile, RectangleShape targetExtent)
        {
            LayerTile layerTile = tile as LayerTile;

            if (layerTile != null)
            {
                layerTile.TileCache = TileCache;
                layerTile.DrawingLayers.Clear();
                layerTile.DrawingLayers.Add(wktLayer);

                Bitmap nativeImage = new Bitmap((int)tile.Width, (int)tile.Height);
                PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas();
                geoCanvas.BeginDrawing(nativeImage, targetExtent, MapArguments.MapUnit);
                if (tile.IsAsync)
                {
                    layerTile.DrawAsync(geoCanvas);
                }
                else
                {
                    layerTile.Draw(geoCanvas);
                    geoCanvas.EndDrawing();
                    layerTile.CommitDrawing(geoCanvas, MapUtils.GetImageSourceFromNativeImage(nativeImage));
                }
            }
        }

        /// <summary>
        /// This method gets a world extent for holding the entire OpenStreed map.
        /// </summary>
        /// <returns></returns>
        protected override RectangleShape GetBoundingBoxCore()
        {
            RectangleShape bbox = null;
            switch (Projection)
            {
                case Layers.WorldMapKitProjection.SphericalMercator:
                    bbox = MapUtils.GetDefaultMaxExtent(GeographyUnit.Meter);
                    break;
                default: bbox = MapUtils.GetDefaultMaxExtent(GeographyUnit.DecimalDegree);
                    break;
            }
            return bbox;
        }

        /// <summary>
        /// This method saves overlay state to a byte array.
        /// </summary>
        /// <returns>A byte array indicates current overlay state.</returns>
        protected override byte[] SaveStateCore()
        {
            byte[] baseState = base.SaveStateCore();

            Stack<object> state = new Stack<object>();
            state.Push(baseState);
            state.Push(TimeoutInSeconds);

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, state);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// This method restore the overlay state back from the specified state.
        /// </summary>
        /// <param name="state">This parameter indicates the state for restore the overlay.</param>
        protected override void LoadStateCore(byte[] state)
        {
            using (MemoryStream stream = new MemoryStream(state))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                Stack<object> newStates = (Stack<object>)formatter.Deserialize(stream);

                TimeoutInSeconds = (int)newStates.Pop();
                base.LoadStateCore((byte[])newStates.Pop());
            }
        }

        protected virtual void OnSendingWebRequest(SendingWebRequestEventArgs e)
        {
            EventHandler<SendingWebRequestEventArgs> handler = SendingWebRequest;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnSentWebRequest(SentWebRequestEventArgs e)
        {
            EventHandler<SentWebRequestEventArgs> handler = SentWebRequest;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void osmLayer_SentWebRequest(object sender, SentWebRequestEventArgs e)
        {
            OnSentWebRequest(e);
        }

        private void osmLayer_SendingWebRequest(object sender, SendingWebRequestEventArgs e)
        {
            OnSendingWebRequest(e);
        }

        private string GetTemporaryFolder()
        {
            string returnValue = string.Empty;
            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable("Temp");
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable("Tmp");
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = "c:\\MapSuiteTemp\\";
            }
            else
            {
                returnValue += "\\MapSuite\\";
            }

            return returnValue;
        }

        private bool IsDefaultCacheId(string cacheId)
        {
            foreach (var layerTypeName in Enum.GetNames(typeof(Layers.WorldMapKitLayerType)))
            {
                foreach (var mapTypeName in Enum.GetNames(typeof(Layers.WorldMapKitMapType)))
                {
                    var isDefault = (cacheId.Equals(string.Format("{0}_Projected_{1}", layerTypeName, mapTypeName), StringComparison.InvariantCultureIgnoreCase))
                                     || (cacheId.Equals(string.Format("{0}_{1}", layerTypeName, mapTypeName), StringComparison.InvariantCultureIgnoreCase));
                    if (isDefault)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string GetDefaultCacheId()
        {
            Layers.WorldMapKitLayerType layerTypeTemp = wktLayer.LayerType == Layers.WorldMapKitLayerType.Default ? Layers.WorldMapKitLayerType.OSMWorldMapKitLayer : wktLayer.LayerType;

            string cacheIdFormat = Projection == Layers.WorldMapKitProjection.SphericalMercator ? "{0}_Projected_{1}" : "{0}_{1}";

            return string.Format(cacheIdFormat, layerTypeTemp.ToString("g"), MapType);
        }

        [OnGeodeserialized]
        private void OnGeoDeserialized()
        {
            wktLayer.TileCache = TileCache;
        }
    }
}