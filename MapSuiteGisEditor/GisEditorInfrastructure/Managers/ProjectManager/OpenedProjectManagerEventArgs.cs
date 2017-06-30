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
    public class OpenedProjectManagerEventArgs : EventArgs
    {
        private Exception error;
        private ProjectStreamInfo projectStreamInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedProjectManagerEventArgs" /> class.
        /// </summary>
        public OpenedProjectManagerEventArgs()
            : this(null, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedProjectManagerEventArgs" /> class.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        /// <param name="error">The error.</param>
        public OpenedProjectManagerEventArgs(ProjectStreamInfo projectStreamInfo, Exception error)
        {
            this.projectStreamInfo = projectStreamInfo;
            this.error = error;
        }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error
        {
            get { return error; }
            set { error = value; }
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
    }
}