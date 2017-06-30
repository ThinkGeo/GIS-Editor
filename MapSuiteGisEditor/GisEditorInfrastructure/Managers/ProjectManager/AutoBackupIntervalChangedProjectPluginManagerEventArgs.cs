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
    public class AutoBackupIntervalChangedProjectPluginManagerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoBackupIntervalChangedProjectPluginManagerEventArgs" /> class.
        /// </summary>
        public AutoBackupIntervalChangedProjectPluginManagerEventArgs()
            : this(default(TimeSpan), default(TimeSpan))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoBackupIntervalChangedProjectPluginManagerEventArgs" /> class.
        /// </summary>
        /// <param name="newAutoBackupInterval">The new auto backup interval.</param>
        /// <param name="oldAutoBackupInterval">The old auto backup interval.</param>
        public AutoBackupIntervalChangedProjectPluginManagerEventArgs(TimeSpan newAutoBackupInterval, TimeSpan oldAutoBackupInterval)
        {
            NewAutoBackupInterval = newAutoBackupInterval;
            OldAutoBackupInterval = oldAutoBackupInterval;
        }

        /// <summary>
        /// Gets or sets the new auto backup interval.
        /// </summary>
        /// <value>
        /// The new auto backup interval.
        /// </value>
        public TimeSpan NewAutoBackupInterval { get; set; }

        /// <summary>
        /// Gets or sets the old auto backup interval.
        /// </summary>
        /// <value>
        /// The old auto backup interval.
        /// </value>
        public TimeSpan OldAutoBackupInterval { get; set; }
    }
}