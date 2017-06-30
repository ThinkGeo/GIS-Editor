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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class CacheHelper
    {
        public static void DeleteTileCacheFiles(string cachePath)
        {
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }

        public static decimal GetTileCacheFilesSizeByMB(string cachePath)
        {
            if (Directory.Exists(cachePath))
            {
                return Math.Round((decimal)GetDirectoryLength(cachePath) / (1024 * 1024), 2);
            }
            else
                return 0;
        }

        private static long GetDirectoryLength(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;
            long length = 0;
            DirectoryInfo dir = new DirectoryInfo(directoryPath);
            foreach (FileInfo fi in dir.GetFiles())
            {
                length += fi.Length;
            }
            DirectoryInfo[] directoryInfo = dir.GetDirectories();
            if (directoryInfo.Length > 0)
            {
                foreach (var item in directoryInfo)
                {
                    length += GetDirectoryLength(item.FullName);
                }
            }
            return length;
        }
    }
}
