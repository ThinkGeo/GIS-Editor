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
    public class CanAutoBackupChangedProjectPluginManagerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CanAutoBackupChangedProjectPluginManagerEventArgs" /> class.
        /// </summary>
        public CanAutoBackupChangedProjectPluginManagerEventArgs()
            : this(true)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CanAutoBackupChangedProjectPluginManagerEventArgs" /> class.
        /// </summary>
        /// <param name="canAutoBackup">if set to <c>true</c> [can auto backup].</param>
        public CanAutoBackupChangedProjectPluginManagerEventArgs(bool canAutoBackup)
        {
            CanAutoBackup = canAutoBackup;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can auto backup.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can auto backup; otherwise, <c>false</c>.
        /// </value>
        public bool CanAutoBackup { get; set; }
    }
}