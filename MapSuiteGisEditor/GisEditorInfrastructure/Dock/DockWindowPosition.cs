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
    /// Specifies the side of the main window that a dock control will be shown on.
    /// </summary>
    public enum DockWindowPosition
    {
        /// <summary>
        /// The default
        /// </summary>
        Default = 0,

        /// <summary>
        /// A dock control will shown on the left side of the main window.
        /// </summary>
        Left = 1,

        /// <summary>
        /// A dock control will shown on the right side of the main window.
        /// </summary>
        Right = 2,

        /// <summary>
        /// A dock control will shown on the bottom of the main window.
        /// </summary>
        Bottom = 3,

        /// <summary>
        /// The floating
        /// </summary>
        Floating = 4
    }
}