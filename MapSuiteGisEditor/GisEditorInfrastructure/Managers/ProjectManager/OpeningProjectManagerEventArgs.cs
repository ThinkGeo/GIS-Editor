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
    /// 
    /// </summary>
    [Serializable]
    public class OpeningProjectManagerEventArgs : EventArgs
    {
        private bool isCanceled;
        private ProjectStreamInfo projectStreamInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpeningProjectManagerEventArgs" /> class.
        /// </summary>
        public OpeningProjectManagerEventArgs()
            : this(null, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpeningProjectManagerEventArgs" /> class.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        public OpeningProjectManagerEventArgs(ProjectStreamInfo projectStreamInfo)
            : this(projectStreamInfo, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpeningProjectManagerEventArgs" /> class.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        /// <param name="isCanceled">if set to <c>true</c> [is canceled].</param>
        public OpeningProjectManagerEventArgs(ProjectStreamInfo projectStreamInfo, bool isCanceled)
        {
            this.isCanceled = isCanceled;
            this.projectStreamInfo = projectStreamInfo;
        }

        /// <summary>
        /// Gets or sets the project stream info.
        /// </summary>
        /// <value>
        /// The project stream info.
        /// </value>
        public ProjectStreamInfo ProjectStreamInfo
        {
            get { return projectStreamInfo; }
            set { projectStreamInfo = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is canceled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is canceled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCanceled
        {
            get { return isCanceled; }
            set { isCanceled = value; }
        }
    }
}