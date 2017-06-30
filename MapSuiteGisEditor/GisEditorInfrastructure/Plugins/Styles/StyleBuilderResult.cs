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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a style builder result
    /// </summary>
    [Serializable]
    public class StyleBuilderResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBuilderResult" /> class.
        /// </summary>
        public StyleBuilderResult()
            : this(null, false, 1, 20)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBuilderResult" /> class.
        /// </summary>
        /// <param name="compositeStyle">The composite style.</param>
        public StyleBuilderResult(CompositeStyle compositeStyle)
            : this(compositeStyle, false, 1, 20)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBuilderResult" /> class.
        /// </summary>
        /// <param name="compositeStyle">The composite style.</param>
        /// <param name="canceled">if set to <c>true</c> [canceled].</param>
        public StyleBuilderResult(CompositeStyle compositeStyle, bool canceled)
            : this(compositeStyle, canceled, 1, 20)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBuilderResult" /> class.
        /// </summary>
        /// <param name="compositeStyle">The composite style.</param>
        /// <param name="canceled">if set to <c>true</c> [canceled].</param>
        /// <param name="fromZoomLevelIndex">Index of from zoom level.</param>
        /// <param name="toZoomLevelIndex">Index of to zoom level.</param>
        public StyleBuilderResult(CompositeStyle compositeStyle, bool canceled, int fromZoomLevelIndex, int toZoomLevelIndex)
        {
            Canceled = canceled;
            CompositeStyle = compositeStyle;
            ToZoomLevelIndex = toZoomLevelIndex;
            FromZoomLevelIndex = fromZoomLevelIndex;
            //LowerScale = 0;
            //UpperScale = 10000000;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="StyleBuilderResult" /> is canceled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if canceled; otherwise, <c>false</c>.
        /// </value>
        public bool Canceled { get; set; }

        /// <summary>
        /// starts from 1.
        /// </summary>
        public int FromZoomLevelIndex { get; set; }

        /// <summary>
        /// starts from 1.
        /// </summary>
        public int ToZoomLevelIndex { get; set; }

        /// <summary>
        /// Gets or sets the composite style.
        /// </summary>
        /// <value>
        /// The composite style.
        /// </value>
        public CompositeStyle CompositeStyle { get; set; }

        //public double LowerScale { get; set; }

        //public double UpperScale { get; set; }
    }
}