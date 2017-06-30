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


using System.Globalization;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class ZoomLevelModel
    {
        public ZoomLevelModel(int index, double scale)
        {
            Index = index;
            Scale = scale;
        }

        public int Index { get; set; }

        public double Scale { get; set; }

        public string Text
        {
            get
            {
                string zoomLevelTitle = string.Format(CultureInfo.InvariantCulture, "Level {0:D2}", Index);
                string zoomLevelScale = string.Format(CultureInfo.InvariantCulture, "Scale 1:{0:N0}", Scale);
                return zoomLevelTitle + " - " + zoomLevelScale;
            }
        }
    }
}