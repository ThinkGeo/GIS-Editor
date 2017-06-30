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
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class FastWorldMapKitRasterLayer : WorldMapKitLayer
    {
        private static readonly Random random = new Random();

        public FastWorldMapKitRasterLayer()
            : this(string.Empty, string.Empty)
        { }

        public FastWorldMapKitRasterLayer(string clientId, string privateKey)
            : base(clientId, privateKey)
        {
            PrivateKey = privateKey;
            DrawingExceptionMode = DrawingExceptionMode.DrawException;
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            Collection<string> urls = GetRequestUrl(canvas.CurrentWorldExtent, (int)canvas.Width, (int)canvas.Height);
            string currentUrl = urls[random.Next(0, urls.Count - 1)];

            bool isSuccess = false;
            int failedCount = 0;

            while (!isSuccess && failedCount < 2)
            {
                try
                {
                    currentUrl = SignUrl(currentUrl, PrivateKey);
                    HttpWebRequest myWebRequest = (HttpWebRequest)WebRequest.Create(currentUrl);
                    myWebRequest.Timeout = TimeoutInSecond * 1000;
                    myWebRequest.Proxy = WebProxy;
                    WebResponse response = myWebRequest.GetResponse();
                    Stream stream = response.GetResponseStream();
                    GeoImage geoImage = new GeoImage(stream);
                    PointShape centerPoint = canvas.CurrentWorldExtent.GetCenterPoint();
                    canvas.DrawWorldImage(geoImage, centerPoint.X, centerPoint.Y, canvas.Width, canvas.Height, DrawingLevel.LevelOne);
                    isSuccess = true;
                }
                catch
                {
                    failedCount++;
                }
            }
        }

        private static string SignUrl(string url, string privateKey)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] privateKeyBytes = encoding.GetBytes(privateKey); //Convert.FromBase64String(usablePrivateKey);

            Uri uri = new Uri(url);
            byte[] encodedPathAndQueryBytes = encoding.GetBytes(uri.LocalPath + uri.Query);

            // compute the hash
            HMACSHA1 algorithm = new HMACSHA1(privateKeyBytes);
            byte[] hash = algorithm.ComputeHash(encodedPathAndQueryBytes);

            // convert the bytes to string and make url-safe by replacing '+' and '/' characters
            string signature = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_");

            // Add the signature to the existing URI.
            return uri.Scheme + "://" + uri.Authority + uri.LocalPath + uri.Query + "&signature=" + signature;
        }
    }
}