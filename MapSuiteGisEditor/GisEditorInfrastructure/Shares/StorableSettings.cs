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
using System.Collections.Generic;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents storable settings
    /// </summary>
    [Serializable]
    public class StorableSettings
    {
        private Dictionary<string, string> globalSettings;
        private Dictionary<string, string> projectSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorableSettings" /> class.
        /// </summary>
        public StorableSettings() :
            this(null, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorableSettings" /> class.
        /// </summary>
        /// <param name="globalSettings">The global settings.</param>
        /// <param name="projectSettings">The project settings.</param>
        public StorableSettings(Dictionary<string, string> globalSettings, Dictionary<string, string> projectSettings)
        {
            this.projectSettings = new Dictionary<string, string>();
            this.globalSettings = new Dictionary<string, string>();

            if (globalSettings != null)
            {
                foreach (var item in globalSettings)
                {
                    this.globalSettings.Add(item.Key, item.Value);
                }
            }

            if (projectSettings != null)
            {
                foreach (var item in projectSettings)
                {
                    this.projectSettings.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Gets the project settings.
        /// </summary>
        /// <value>
        /// The project settings.
        /// </value>
        public Dictionary<string, string> ProjectSettings
        {
            get { return projectSettings; }
        }

        /// <summary>
        /// Gets the global settings.
        /// </summary>
        /// <value>
        /// The global settings.
        /// </value>
        public Dictionary<string, string> GlobalSettings
        {
            get { return globalSettings; }
        }
    }
}