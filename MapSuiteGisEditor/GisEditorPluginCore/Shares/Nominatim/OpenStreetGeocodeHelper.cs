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


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class OpenStreetGeocodeHelper
    {
        public static async Task<Collection<OpenStreetGeocodeMatch>> GeocodeAsync(string query)
        {
            string entryUri = $"http://nominatim.openstreetmap.org/search?q={query}&format=json&addressdetails=1";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(entryUri);
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.86 Safari/537.36";

            return await GetResponseAsync(webRequest, stream =>
            {
                JsonSerializer serializer = new JsonSerializer();
                using (var sr = new StreamReader(stream))
                {
                    JsonTextReader reader = new JsonTextReader(sr);
                    Collection<OpenStreetGeocodeMatch> addresses = serializer.Deserialize<Collection<OpenStreetGeocodeMatch>>(reader);
                    return addresses;
                }
            });
        }

        public static async Task<OpenStreetGeocodeMatch> ReverseGeocodeAsync(double longitude, double latitude)
        {
            string entryUri = $"http://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(entryUri);
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.86 Safari/537.36";

            return await GetResponseAsync(webRequest, stream =>
            {
                JsonSerializer serializer = new JsonSerializer();
                using (var sr = new StreamReader(stream))
                {
                    JsonTextReader reader = new JsonTextReader(sr);
                    OpenStreetGeocodeMatch addresses = serializer.Deserialize<OpenStreetGeocodeMatch>(reader);
                    return addresses;
                }
            });
        }

        private static async Task<T> GetResponseAsync<T>(HttpWebRequest webRequest, Func<Stream, T> readStream) where T : new()
        {
            T response = new T();

            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.86 Safari/537.36";
            WebResponse webResponse = null;
            Stream webResponseStream = null;
            try
            {
                webResponse = await webRequest.GetResponseAsync();
                webResponseStream = webResponse.GetResponseStream();
                return readStream.Invoke(webResponseStream);
            }
            catch { }
            finally
            {
                if (webResponseStream != null)
                {
                    webResponseStream.Close();
                    webResponseStream.Dispose();
                    webResponseStream = null;
                }

                if (webResponse != null)
                {
                    webResponse.Close();
                    webResponse.Dispose();
                    webResponse = null;
                }
            }

            return default(T);
        }
    }
}
