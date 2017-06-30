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


using Microsoft.Win32;
using System;
using System.IO;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FileDataSourceResolveTool<T> : DataSourceResolveTool where T : Layer
    {
        [NonSerialized]
        private Func<T, string> getPathFilename;

        [NonSerialized]
        private Action<T, string> setPathFilename;

        public FileDataSourceResolveTool(string extensionFilter, Func<T, string> getPathFilename, Action<T, string> setPathFilename)
            : base(extensionFilter)
        {
            this.getPathFilename = getPathFilename;
            this.setPathFilename = setPathFilename;
        }

        protected override bool CanResolveDataSourceCore
        {
            get { return true; }
        }

        protected override bool IsDataSourceAvailableCore(Layer layer)
        {
            T currentLayer = (T)layer;
            string pathFilename = string.Empty;
            if (getPathFilename != null) pathFilename = getPathFilename(currentLayer);
            return File.Exists(pathFilename);
        }

        protected override void ResolveDataSourceCore(Layer layer)
        {
            T currentLayer = (T)layer;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = ExtensionFilter;
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                setPathFilename?.Invoke(currentLayer, openFileDialog.FileName);
            }
        }
    }
}
