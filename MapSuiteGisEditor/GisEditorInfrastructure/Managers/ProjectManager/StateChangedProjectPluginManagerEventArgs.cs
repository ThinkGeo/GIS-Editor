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
    public class StateChangedProjectPluginManagerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateChangedProjectPluginManagerEventArgs" /> class.
        /// </summary>
        /// <param name="state"></param>
        public StateChangedProjectPluginManagerEventArgs(ProjectReadWriteMode state)
        {
            State = state;
        }

        /// <summary>
        /// Gets or sets IsReadOnly.
        /// </summary>
        /// <value>
        /// The project state.
        /// </value>
        public ProjectReadWriteMode State { get; set; }
    }
}
