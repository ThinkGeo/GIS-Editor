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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a data repository manager.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(DataRepositoryPluginManager))]
    public class DataRepositoryPluginManager : PluginManager
    {
        private Collection<string> expandedFolders;
        private string currentSelectedItem;

        public DataRepositoryPluginManager()
        {
            expandedFolders = new Collection<string>();
        }

        public Collection<string> ExpandedFolders
        {
            get { return expandedFolders; }
        }

        internal string CurrentSelectedItem
        {
            get { return currentSelectedItem; }
            set { currentSelectedItem = value; }
        }

        /// <summary>
        /// Gets the data repository plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<DataRepositoryPlugin> GetDataRepositoryPlugins()
        {
            return new Collection<DataRepositoryPlugin>(GetPlugins().Cast<DataRepositoryPlugin>().ToList());
        }

        public Collection<T> GetActiveDataRepositoryPlugins<T>() where T : DataRepositoryPlugin
        {
            return new Collection<T>(GetActiveDataRepositoryPlugins().OfType<T>().ToList());
        }

        public Collection<DataRepositoryPlugin> GetActiveDataRepositoryPlugins()
        {
            var activePlugins = from p in GetDataRepositoryPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<DataRepositoryPlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Gets the data repository plugins.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            return CollectPlugins<DataRepositoryPlugin>();
        }
    }
}