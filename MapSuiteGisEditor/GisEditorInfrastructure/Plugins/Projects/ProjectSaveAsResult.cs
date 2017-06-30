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
    /// <summary>
    /// This class represents a project which can be saved as a result
    /// </summary>
    [Serializable]
    public class ProjectSaveAsResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectSaveAsResult" /> class.
        /// </summary>
        public ProjectSaveAsResult()
            : this(null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectSaveAsResult" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public ProjectSaveAsResult(Uri uri)
            : this(uri, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectSaveAsResult" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="canceled">if set to <c>true</c> [canceled].</param>
        public ProjectSaveAsResult(Uri uri, bool canceled)
        {
            Uri = uri;
            Canceled = canceled;
        }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ProjectSaveAsResult" /> is canceled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if canceled; otherwise, <c>false</c>.
        /// </value>
        public bool Canceled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether keep the passwords
        /// </summary>
        public bool KeepPasswords { get; set; }
    }
}