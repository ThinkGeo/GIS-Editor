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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class ColorEx
    {
        public static int GetHue(this System.Windows.Media.Color color)
        {
            return color.GetHSL().Hue;
        }

        public static int GetLuminance(this System.Windows.Media.Color color)
        {
            return (int)Math.Ceiling(color.GetHSL().Luminance * 100);
        }

        public static int GetSaturation(this System.Windows.Media.Color color)
        {
            return (int)Math.Ceiling(color.GetHSL().Saturation * 100);
        }

        public static HSL GetHSL(this System.Windows.Media.Color color)
        {
            RGB rgb = new RGB(color.R, color.G, color.B);
            HSL hsl = new HSL();
            ColorConverter.RGB2HSL(rgb, hsl);
            hsl.Luminance = Math.Ceiling(hsl.Luminance * 100) / 100;
            hsl.Saturation = Math.Ceiling(hsl.Saturation * 100) / 100;
            return hsl;
        }
    }
}
