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


namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This enumeration represents where to place a dock window at startup.
    /// </summary>
    public enum DockWindowStartupMode
    {
        /// <summary>
        /// Same as Display.
        /// </summary>
        Default = 0,
        /// <summary>
        /// This indicates the dock window will be autohiden at startup.
        /// </summary>
        AutoHide = 1,
        /// <summary>
        /// This indicates the dock window will be hiden at startup.
        /// </summary>
        Hide = 2,
        /// <summary>
        /// This indicates the dock window will be displayed at startup.
        /// </summary>
        Display = 3
    }
}