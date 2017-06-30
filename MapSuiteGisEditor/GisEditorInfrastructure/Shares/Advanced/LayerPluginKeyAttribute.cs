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
using System.Security.Cryptography;
using System.Text;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class LayerPluginKeyAttribute : Attribute
    {
        private string key;

        public LayerPluginKeyAttribute(string key)
        {
            this.key = key;
        }

        public string Key
        {
            get { return key; }
            set { key = value; }
        }
    }

    public static class LayerPluginKeyHelper
    {
        private static string leftFactor = "HIOLJ1H5K5LNLOIUY";
        private static string rightFactor = "QTGFW2SF8W6ERTFGV";

        public static string Sha1Encrypt(string layerPluginTypeFullName, string layerTypeFullName)
        {
            string originalString = string.Format("{0}{1}{2}{3}", leftFactor, layerPluginTypeFullName, layerTypeFullName, rightFactor);
            SHA1CryptoServiceProvider SHA = new SHA1CryptoServiceProvider();
            byte[] b = UnicodeEncoding.UTF8.GetBytes(originalString);
            byte[] Hash = SHA.ComputeHash(b);
            string Results = Convert.ToBase64String(Hash);

            return Results;
        }
    }
}
