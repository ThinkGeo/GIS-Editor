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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ProjectStreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectStreamInfo" /> class.
        /// </summary>
        public ProjectStreamInfo()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectStreamInfo" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="stream">The stream.</param>
        public ProjectStreamInfo(Uri uri, MemoryStream stream)
        {
            Uri = uri;
            Stream = stream;
        }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        /// <value>
        /// The stream.
        /// </value>
        public MemoryStream Stream { get; set; }

        ///// <summary>
        ///// Gets or sets the view password.
        ///// </summary>
        ///// <value>
        ///// The password.
        ///// </value>
        //public string ViewPassword { get; set; }

        ///// <summary>
        ///// Gets or sets the save password.
        ///// </summary>
        ///// <value>
        ///// The password.
        ///// </value>
        //public string SavePassword { get; set; }
    }
}