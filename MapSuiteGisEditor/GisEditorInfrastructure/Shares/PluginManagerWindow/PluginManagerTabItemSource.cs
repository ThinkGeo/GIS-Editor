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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class PluginManagerTabItemSource : ManagerTabItemSource
    {
        private PluginManager manager;
        private string headerText;
        [NonSerialized]
        private ImageSource headerBackground;

        public PluginManagerTabItemSource(PluginManager manager, string headerText, Uri imageSource)
        {
            this.manager = manager;
            this.headerText = headerText;
            this.headerBackground = new BitmapImage(imageSource);
        }

        public override string HeaderText
        {
            get { return headerText; }
        }

        public override ImageSource HeaderBackground
        {
            get { return headerBackground; }
        }

        protected override IEnumerable<Plugin> CollectPluginsCore()
        {
            var plugins = manager.GetPlugins();
            if (manager is LayerPluginManager)
            {
                var layerPlugins = plugins.OfType<GroupLayerPlugin>().SelectMany(p => p.LayerPlugins).ToList();
                foreach (var item in layerPlugins)
                {
                    if (plugins.Contains(item)) 
                    {
                        plugins.Remove(item);
                    }
                }
            }
            return plugins;
        }

        protected override void ApplyCore()
        {
            GisEditor.InfrastructureManager.SaveSettings(manager.GetPlugins());
        }
    }
}