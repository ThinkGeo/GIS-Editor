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
using System.Reflection;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class DataSourceResolveTool
    {
        [Obfuscation]
        private bool canResolveDataSource;

        [Obfuscation]
        private string extensionFilter;

        public DataSourceResolveTool()
            : this(string.Empty)
        { }

        public DataSourceResolveTool(string extensionFilter)
        {
            this.extensionFilter = extensionFilter;
        }

        public string ExtensionFilter
        {
            get { return extensionFilter; }
            set { extensionFilter = value; }
        }

        public bool IsDataSourceAvailable(Layer layer)
        {
            return IsDataSourceAvailableCore(layer);
        }

        protected virtual bool IsDataSourceAvailableCore(Layer layer)
        {
            return true;
        }

        public bool CanResolveDataSource
        {
            get { return CanResolveDataSourceCore; }
        }

        protected virtual bool CanResolveDataSourceCore
        {
            get { return canResolveDataSource; }
            set { canResolveDataSource = value; }
        }

        public void ResolveDataSource(Layer layer)
        {
            ResolveDataSourceCore(layer);
        }

        protected virtual void ResolveDataSourceCore(Layer layer)
        { }
    }
}
