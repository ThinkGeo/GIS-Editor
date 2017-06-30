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
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    public class GottenMapContextMenuItemsUIPluginManagerEventArgs : EventArgs
    {
        private Collection<MenuItem> menuItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="GottenMapContextMenuItemsUIPluginManagerEventArgs" /> class.
        /// </summary>
        public GottenMapContextMenuItemsUIPluginManagerEventArgs()
            : this(new Collection<MenuItem>())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GottenMapContextMenuItemsUIPluginManagerEventArgs" /> class.
        /// </summary>
        /// <param name="menuItems">The menu items.</param>
        public GottenMapContextMenuItemsUIPluginManagerEventArgs(IEnumerable<MenuItem> menuItems)
        {
            this.menuItems = new Collection<MenuItem>();
            foreach (var item in menuItems)
            {
                this.menuItems.Add(item);
            }
        }

        /// <summary>
        /// Gets the menu items.
        /// </summary>
        /// <value>
        /// The menu items.
        /// </value>
        public Collection<MenuItem> MenuItems
        {
            get { return menuItems; }
        }
    }
}