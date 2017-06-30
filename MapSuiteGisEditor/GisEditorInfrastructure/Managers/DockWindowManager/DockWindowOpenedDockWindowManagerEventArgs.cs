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
    public class DockWindowOpenedDockWindowManagerEventArgs : EventArgs
    {
        private DockWindow dockWindow;
        private DockWindowPosition dockWindowPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindowOpenedDockWindowManagerEventArgs" /> class.
        /// </summary>
        public DockWindowOpenedDockWindowManagerEventArgs()
            : this(null, DockWindowPosition.Default)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindowOpenedDockWindowManagerEventArgs" /> class.
        /// </summary>
        /// <param name="dockWindow">The dock window.</param>
        public DockWindowOpenedDockWindowManagerEventArgs(DockWindow dockWindow)
            : this(dockWindow, DockWindowPosition.Default)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindowOpenedDockWindowManagerEventArgs" /> class.
        /// </summary>
        /// <param name="dockWindow">The dock window.</param>
        /// <param name="dockWindowPosition">The dock window position.</param>
        public DockWindowOpenedDockWindowManagerEventArgs(DockWindow dockWindow, DockWindowPosition dockWindowPosition)
        {
            this.dockWindow = dockWindow;
            this.DockWindowPosition = dockWindowPosition;
        }

        /// <summary>
        /// Gets or sets the dock window.
        /// </summary>
        /// <value>
        /// The dock window.
        /// </value>
        public DockWindow DockWindow
        {
            get { return dockWindow; }
            set { dockWindow = value; }
        }

        /// <summary>
        /// Gets or sets the dock window position.
        /// </summary>
        /// <value>
        /// The dock window position.
        /// </value>
        public DockWindowPosition DockWindowPosition
        {
            get { return dockWindowPosition; }
            set { dockWindowPosition = value; }
        }
    }
}