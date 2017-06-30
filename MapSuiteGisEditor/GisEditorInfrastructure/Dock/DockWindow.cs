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
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class is wrapper for the actual dock control used in the application.
    /// </summary>
    [Serializable]
    public class DockWindow
    {
        /// <summary>
        /// Initializes a new instance of the DockablePanel class.
        /// </summary>
        public DockWindow()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        public DockWindow(Control content)
            : this(content, DockWindowPosition.Left)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="dockPlacement">The dock placement.</param>
        public DockWindow(Control content, DockWindowPosition dockPlacement)
            : this(content, DockWindowPosition.Left, "Untitled")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockWindow" /> class.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="position">The position.</param>
        /// <param name="title">The title.</param>
        public DockWindow(Control content, DockWindowPosition position, string title)
        {
            Content = content;
            FloatingSize = new Size(250, 400);
            Title = title;
            if (string.IsNullOrEmpty(title))
            {
                Name = Guid.NewGuid().ToString();
            }
            else
            {
                Name = title.Replace(" ", string.Empty);
            }
            Position = position;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the floating size of the dock control.
        /// </summary>
        public Size FloatingSize { get; set; }

        /// <summary>
        /// Gets or sets the position of the  window when first shown.
        /// </summary>
        public WindowStartupLocation FloatingLocation { get; set; }

        /// <summary>
        /// Gets or sets a DockablePanel's title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets which side of the main window should the dock control be.
        /// </summary>
        public DockWindowPosition Position { get; set; }

        /// <summary>
        /// Gets or sets the content of DockablePanel.
        /// </summary>
        public Control Content { get; set; }

        /// <summary>
        /// Gets or sets the startup mode of DockablePanel.
        /// </summary>
        public DockWindowStartupMode StartupMode { get; set; }

        /// <summary>
        /// Shows a popup dock window.
        /// </summary>
        /// <param name="dockWindowPosition">Indicates where the dock window should be shown.</param>
        public void Show(DockWindowPosition dockWindowPosition)
        {
            ShowCore(dockWindowPosition);
        }

        /// <summary>
        /// Shows a popup dock window as floating window.
        /// </summary>
        public void Show()
        {
            Show(DockWindowPosition.Floating);
        }

        /// <summary>
        /// Shows the core.
        /// </summary>
        /// <param name="dockWindowPosition">The dock window position.</param>
        protected virtual void ShowCore(DockWindowPosition dockWindowPosition)
        {
            GisEditor.DockWindowManager.OpenDockWindow(this, dockWindowPosition);
        }
    }
}