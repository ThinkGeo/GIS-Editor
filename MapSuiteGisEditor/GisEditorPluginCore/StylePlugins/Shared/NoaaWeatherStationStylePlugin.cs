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
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NoaaWeatherStationStylePlugin : StylePlugin
    {
        public NoaaWeatherStationStylePlugin()
        {
            Author = "ThinkGeo";
            Description = "NOAA Weather Stations Style.";
            Name = "NOAA Weather Stations Style";
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/clear_day.png", UriKind.RelativeOrAbsolute));
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/clear_day.png", UriKind.RelativeOrAbsolute));
        }

        protected override Style GetDefaultStyleCore()
        {
            NoaaWeatherStationStyle noaaMetarStationStyle = new NoaaWeatherStationStyle();
            noaaMetarStationStyle.Name = "Weather Stations Style";

            return noaaMetarStationStyle;
        }
    }
}