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

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class StringProtector
    {
        private static StringProtector instance;
        private static string empty;

        public StringProtector()
        { }

        internal static StringProtector Instance
        {
            get
            {
                if (instance == null) instance = new StringProtector();
                return instance;
            }
        }

        internal string Encrypt(string source)
        {
            //TODO: implement the encryption logic here.
            return source;
        }

        internal string Decrypt(string source)
        {
            //TODO: implement the decryption logic here.
            return source;
        }

        internal string Empty
        {
            get
            {
                if (string.IsNullOrEmpty(empty)) empty = Encrypt(string.Empty);
                return empty;
            }
        }
    }
}