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


// AForge Image Processing Library
// AForge.NET framework
//
// Copyright ?Andrew Kirillov, 2005-2007
// andrew.kirillov@gmail.com
//
using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Converts colors from different color spaces.
    /// </summary>
    /// 
    /// <remarks>The class provides static methods, which implement conversation
    /// between <b>RGB</b> and other color palettes.</remarks>
    /// 
    [Serializable]
    public sealed class ColorConverter
    {
        // Avoid class instantiation
        private ColorConverter() { }

        /// <summary>
        /// Convert from RGB to HSL color space.
        /// </summary>
        /// 
        /// <param name="rgb">Source color in <b>RGB</b> color space.</param>
        /// <param name="hsl">Destination color in <b>HSL</b> color space.</param>
        /// 
        public static void RGB2HSL(RGB rgb, HSL hsl)
        {
            double r = (rgb.Red / 255.0);
            double g = (rgb.Green / 255.0);
            double b = (rgb.Blue / 255.0);

            double min = Math.Min(Math.Min(r, g), b);
            double max = Math.Max(Math.Max(r, g), b);
            double delta = max - min;

            // get luminance value
            hsl.Luminance = (max + min) / 2;

            if (delta == 0)
            {
                // gray color
                hsl.Hue = 0;
                hsl.Saturation = 0.0;
            }
            else
            {
                // get saturation value
                hsl.Saturation = (hsl.Luminance < 0.5) ? (delta / (max + min)) : (delta / (2 - max - min));

                // get hue value
                double del_r = (((max - r) / 6) + (delta / 2)) / delta;
                double del_g = (((max - g) / 6) + (delta / 2)) / delta;
                double del_b = (((max - b) / 6) + (delta / 2)) / delta;
                double hue;

                if (r == max)
                    hue = del_b - del_g;
                else if (g == max)
                    hue = (1.0 / 3) + del_r - del_b;
                else
                    hue = (2.0 / 3) + del_g - del_r;

                // correct hue if needed
                if (hue < 0)
                    hue += 1;
                if (hue > 1)
                    hue -= 1;

                hsl.Hue = (int)(Math.Ceiling(hue * 100) * 3.6);
            }
        }

        /// <summary>
        /// Convert from HSL to RGB color space.
        /// </summary>
        /// 
        /// <param name="hsl">Source color in <b>HSL</b> color space.</param>
        /// <param name="rgb">Destination color in <b>RGB</b> color space.</param>
        /// 
        public static void HSL2RGB(HSL hsl, RGB rgb)
        {
            if (hsl.Saturation == 0)
            {
                // gray values
                rgb.Red = rgb.Green = rgb.Blue = (byte)(hsl.Luminance * 255);
            }
            else
            {
                double v1, v2;
                double hue = (double)hsl.Hue / 360;

                v2 = (hsl.Luminance < 0.5) ?
                    (hsl.Luminance * (1 + hsl.Saturation)) :
                    ((hsl.Luminance + hsl.Saturation) - (hsl.Luminance * hsl.Saturation));
                v1 = 2 * hsl.Luminance - v2;

                rgb.Red = (byte)(255 * Hue_2_RGB(v1, v2, hue + (1.0 / 3)));
                rgb.Green = (byte)(255 * Hue_2_RGB(v1, v2, hue));
                rgb.Blue = (byte)(255 * Hue_2_RGB(v1, v2, hue - (1.0 / 3)));
            }
        }

        /// <summary>
        /// Convert from RGB to YCbCr color space (Rec 601-1 specification). 
        /// </summary>
        /// 
        /// <param name="rgb">Source color in <b>RGB</b> color space.</param>
        /// <param name="ycbcr">Destination color in <b>YCbCr</b> color space.</param>
        /// 
        public static void RGB2YCbCr(RGB rgb, YCbCr ycbcr)
        {
            double r = (double)rgb.Red / 255;
            double g = (double)rgb.Green / 255;
            double b = (double)rgb.Blue / 255;

            ycbcr.Y = 0.2989 * r + 0.5866 * g + 0.1145 * b;
            ycbcr.Cb = -0.1687 * r - 0.3313 * g + 0.5000 * b;
            ycbcr.Cr = 0.5000 * r - 0.4184 * g - 0.0816 * b;
        }

        /// <summary>
        /// Convert from YCbCr to RGB color space.
        /// </summary>
        /// 
        /// <param name="ycbcr">Source color in <b>YCbCr</b> color space.</param>
        /// <param name="rgb">Destination color in <b>RGB</b> color spacs.</param>
        /// 
        public static void YCbCr2RGB(YCbCr ycbcr, RGB rgb)
        {
            // don't warry about zeros. compiler will remove them
            double r = Math.Max(0.0, Math.Min(1.0, ycbcr.Y + 0.0000 * ycbcr.Cb + 1.4022 * ycbcr.Cr));
            double g = Math.Max(0.0, Math.Min(1.0, ycbcr.Y - 0.3456 * ycbcr.Cb - 0.7145 * ycbcr.Cr));
            double b = Math.Max(0.0, Math.Min(1.0, ycbcr.Y + 1.7710 * ycbcr.Cb + 0.0000 * ycbcr.Cr));

            rgb.Red = (byte)(r * 255);
            rgb.Green = (byte)(g * 255);
            rgb.Blue = (byte)(b * 255);
        }

        public static System.Windows.Media.Color FromHtml(string htmlColor)
        {
            htmlColor = htmlColor.TrimStart('#');
            try
            {
                if (htmlColor.Length == 8)
                {
                    byte a, r, g, b;
                    a = Convert.ToByte(htmlColor.Substring(0, 2), 0x10);
                    r = Convert.ToByte(htmlColor.Substring(2, 2), 0x10);
                    g = Convert.ToByte(htmlColor.Substring(4, 2), 0x10);
                    b = Convert.ToByte(htmlColor.Substring(6, 2), 0x10);

                    return System.Windows.Media.Color.FromArgb(a, r, g, b);
                }
                else if (htmlColor.Length == 6)
                {
                    byte r, g, b;
                    r = Convert.ToByte(htmlColor.Substring(0, 2), 0x10);
                    g = Convert.ToByte(htmlColor.Substring(2, 2), 0x10);
                    b = Convert.ToByte(htmlColor.Substring(4, 2), 0x10);

                    return System.Windows.Media.Color.FromArgb(255, r, g, b);
                }

                return System.Windows.Media.Colors.Transparent;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return System.Windows.Media.Colors.Transparent;
            }
        }

        public static System.Windows.Media.Color FromHSL(int h, double s, double l)
        {
            HSL hsl = new HSL(h, s, l);
            return hsl.ToColor();
        }

        #region Private members
        // HSL to RGB helper routine
        private static double Hue_2_RGB(double v1, double v2, double vH)
        {
            if (vH < 0)
                vH += 1;
            if (vH > 1)
                vH -= 1;
            if ((6 * vH) < 1)
                return (v1 + (v2 - v1) * 6 * vH);
            if ((2 * vH) < 1)
                return v2;
            if ((3 * vH) < 2)
                return (v1 + (v2 - v1) * ((2.0 / 3) - vH) * 6);
            return v1;
        }
        #endregion
    }
}
