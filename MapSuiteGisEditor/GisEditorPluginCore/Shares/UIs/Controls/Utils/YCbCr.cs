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
    /// YCbCr components.
    /// </summary>
    /// 
    /// <remarks>The class encapsulates <b>YCbCr</b> color components.</remarks>
    /// 
    [Serializable]
    public class YCbCr
    {
        /// <summary>
        /// Index of <b>Y</b> component.
        /// </summary>
        public const short YIndex = 0;

        /// <summary>
        /// Index of <b>Cb</b> component.
        /// </summary>
        public const short CbIndex = 1;

        /// <summary>
        /// Index of <b>Cr</b> component.
        /// </summary>
        public const short CrIndex = 2;

        /// <summary>
        /// <b>Y</b> component.
        /// </summary>
        public double Y;

        /// <summary>
        /// <b>Cb</b> component.
        /// </summary>
        public double Cb;

        /// <summary>
        /// <b>Cr</b> component.
        /// </summary>
        public double Cr;

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCr"/> class.
        /// </summary>
        public YCbCr() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="YCbCr"/> class.
        /// </summary>
        /// 
        /// <param name="y"><b>Y</b> component.</param>
        /// <param name="cb"><b>Cb</b> component.</param>
        /// <param name="cr"><b>Cr</b> component.</param>
        /// 
        public YCbCr(double y, double cb, double cr)
        {
            this.Y = Math.Max(0.0, Math.Min(1.0, y));
            this.Cb = Math.Max(-0.5, Math.Min(0.5, cb));
            this.Cr = Math.Max(-0.5, Math.Min(0.5, cr));
        }
    }
}
