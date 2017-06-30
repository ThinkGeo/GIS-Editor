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
using System.ComponentModel.Composition;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// This class is the base class for all Options.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(Setting))]
    public class Setting
    {
        /// <summary>
        /// Initializes a new instance of the Option class.
        /// </summary>
        public Setting()
        { }

        public virtual Dictionary<string, string> SaveState()
        {
            return new Dictionary<string, string>();
        }

        public virtual void LoadState(Dictionary<string, string> state)
        { 
            
        }
    }
}