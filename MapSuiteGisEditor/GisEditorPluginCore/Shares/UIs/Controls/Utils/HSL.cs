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
    /// <summary>
    /// HSL components.
    /// </summary>
    /// 
    /// <remarks>The class encapsulates <b>HSL</b> color components.</remarks>
    /// 
    [Serializable]
    public class HSL
    {
        /// <summary>
        /// Hue component.
        /// </summary>
        /// 
        /// <remarks>Hue is measured in the range of [0, 359].</remarks>
        /// 
        public int Hue;

        /// <summary>
        /// Saturation component.
        /// </summary>
        /// 
        /// <remarks>Saturation is measured in the range of [0, 1].</remarks>
        /// 
        public double Saturation;

        /// <summary>
        /// Luminance value.
        /// </summary>
        /// 
        /// <remarks>Luminance is measured in the range of [0, 1].</remarks>
        /// 
        public double Luminance;

        /// <summary>
        /// Initializes a new instance of the <see cref="HSL"/> class.
        /// </summary>
        public HSL() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HSL"/> class.
        /// </summary>
        /// 
        /// <param name="hue">Hue component.</param>
        /// <param name="saturation">Saturation component.</param>
        /// <param name="luminance">Luminance component.</param>
        /// 
        public HSL(int hue, double saturation, double luminance)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Luminance = luminance;
        }

        public System.Windows.Media.Color ToColor()
        {
            return ToColor(255);
        }

        public System.Windows.Media.Color ToColor(int alpha)
        {
            RGB rgb = new RGB();
            ColorConverter.HSL2RGB(this, rgb);
            return System.Windows.Media.Color.FromArgb((byte)alpha, rgb.Red, rgb.Green, rgb.Blue);
        }
    };

}
