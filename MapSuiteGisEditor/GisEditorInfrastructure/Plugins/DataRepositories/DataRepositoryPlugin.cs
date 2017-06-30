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
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This plugin is the base class of all data repository plugins.
    /// GisEditor collects those plugins to create the data repository tree.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(DataRepositoryPlugin))]
    public abstract class DataRepositoryPlugin : Plugin
    {
        protected DataRepositoryPlugin()
        {
            CanRefreshDynamically = false;
        }

        public bool CanDropOnMap
        {
            get { return CanDropOnMapCore; }
        }

        protected virtual bool CanDropOnMapCore
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the context menu of data repository plugin.
        /// </summary>
        /// <value>
        /// The context menu of data repository plugin.
        /// </value>
        public ContextMenu ContextMenu { get; protected set; }

        /// <summary>
        /// Gets or sets the content of data repository plugin.
        /// </summary>
        /// <value>
        /// The content of data repository plugin.
        /// </value>
        public UserControl Content { get; protected set; }


        public DataRepositoryItem RootDataRepositoryItem
        {
            get
            {
                DataRepositoryItem rootDataRepositoryItem = RootDataRepositoryItemCore;
                if (rootDataRepositoryItem != null)
                {
                    if (rootDataRepositoryItem.SourcePlugin == null) rootDataRepositoryItem.SourcePlugin = this;
                    if (string.IsNullOrEmpty(rootDataRepositoryItem.Name)) rootDataRepositoryItem.Name = this.Name;
                    if (rootDataRepositoryItem.ContextMenu == null) rootDataRepositoryItem.ContextMenu = this.ContextMenu;
                    if (rootDataRepositoryItem.Content == null) rootDataRepositoryItem.Content = this.Content;
                    if (rootDataRepositoryItem.Icon == null) rootDataRepositoryItem.Icon = this.SmallIcon;
                }

                return rootDataRepositoryItem;
            }
        }

        protected virtual DataRepositoryItem RootDataRepositoryItemCore
        {
            get { return null; }
        }

        public bool CanRefreshDynamically { get; protected set; }

        [Obsolete("This method is obsolete, please use CreateDataRepositoryItem instead. This API is obsolete and may be removed in or after version 9.0")]
        public DataRepositoryItem GetDataRepositoryItem()
        {
            return GetDataRepositoryItemCore();
        }

        [Obsolete("This method is obsolete, please use CreateDataRepositoryItemCore instead. This API is obsolete and may be removed in or after version 9.0")]
        protected virtual DataRepositoryItem GetDataRepositoryItemCore()
        {
            return CreateDataRepositoryItem();
        }

        [Obsolete("This property is obsolete. This API is obsolete and may be removed in or after version 9.0")]
        public ObservableCollection<DataRepositoryItem> RootDataRepositoryItems
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the data repository item of data repository plugin.
        /// </summary>
        /// <returns>Data repository item.</returns>
        public DataRepositoryItem CreateDataRepositoryItem()
        {
            return CreateDataRepositoryItemCore();
        }

        /// <summary>
        /// Gets the root data repository item of data repository plugin.
        /// This is the core method of GetDataRepositoryItem() to override.
        /// </summary>
        /// <returns>Root data repository item.</returns>
        protected virtual DataRepositoryItem CreateDataRepositoryItemCore()
        {
            return null;
        }

        public void DropOnMap(IEnumerable<DataRepositoryItem> dataRepositoryItems)
        {
            if (CanDropOnMap)
            {
                DropOnMapCore(dataRepositoryItems);
            }
        }

        protected virtual void DropOnMapCore(IEnumerable<DataRepositoryItem> dataRepositoryItems)
        {
        }
    }
}