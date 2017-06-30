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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a base class of PluginManagers in all GISEditor system.
    /// </summary>
    [Serializable]
    public abstract class PluginManager : Manager
    {
        private static readonly string defaultPluginPath;
        private static readonly string defaultPluginPathFileName;
        private GeoCollection<Plugin> plugins;
        private static Collection<string> pluginDirectories;
        private static MultiDirectoryCatalog catalog;
        private static CompositionContainer container;

        public event EventHandler<GottenPluginsPluginManagerEventArgs> GottenPlugins;

        public event EventHandler<GettingPluginsPluginManagerEventArgs> GettingPlugins;

        static PluginManager()
        {
            defaultPluginPath = Path.Combine(PluginHelper.GetEntryPath(), "Plugins");
            defaultPluginPathFileName = Path.Combine(defaultPluginPath, "ThinkGeo", "GisEditorPluginCore.dll");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager" /> class.
        /// </summary>
        protected PluginManager()
        {
            pluginDirectories = new Collection<string>();
            pluginDirectories.Add(defaultPluginPath);
        }

        internal static string DefaultPluginPathFileName { get { return defaultPluginPathFileName; } }

        /// <summary>
        /// Gets the related plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<Plugin> GetPlugins()
        {
            Collection<Plugin> resultPlugins = new Collection<Plugin>();

            GettingPluginsPluginManagerEventArgs args = new GettingPluginsPluginManagerEventArgs();
            OnGettingPlugins(args);

            foreach (var item in args.Plugins)
            {
                resultPlugins.Add(item);
            }

            if (plugins == null)
            {
                plugins = new GeoCollection<Plugin>();
                GetPluginsCore().OrderBy(p => p.Index).ForEach(p =>
                {
                    if (!plugins.Contains(p.Id)) plugins.Add(p.Id, p);
                });
            }

            foreach (var item in plugins)
            {
                resultPlugins.Add(item);
            }

            GottenPluginsPluginManagerEventArgs gottenArgs = new GottenPluginsPluginManagerEventArgs(resultPlugins);
            OnGottenPlugins(gottenArgs);

            return gottenArgs.Plugins;
        }

        /// <summary>
        /// Gets the related plugins.
        /// </summary>
        /// <returns></returns>
        protected abstract Collection<Plugin> GetPluginsCore();

        public void UnloadPlugins()
        {
            UnloadPluginsCore();
        }

        protected virtual void UnloadPluginsCore()
        {
            plugins = null;
            catalog = null;
            container = null;
        }

        protected virtual void OnGottenPlugins(GottenPluginsPluginManagerEventArgs e)
        {
            EventHandler<GottenPluginsPluginManagerEventArgs> handler = GottenPlugins;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnGettingPlugins(GettingPluginsPluginManagerEventArgs e)
        {
            EventHandler<GettingPluginsPluginManagerEventArgs> handler = GettingPlugins;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual Collection<Plugin> CollectPlugins<T>() where T : Plugin
        {
            Collection<Plugin> plugins = new Collection<Plugin>();

            if (catalog == null || container == null)
            {
                List<string> tempPluginDirectories = new List<string>();
                tempPluginDirectories.AddRange(pluginDirectories.Concat(GisEditor.InfrastructureManager.PluginDirectories).Distinct());

                catalog = new MultiDirectoryCatalog(tempPluginDirectories, "*.dll");
                container = new CompositionContainer(catalog);
#if GISEditorUnitTest
                if (pluginDirectories.Count > 0 && !Directory.Exists(pluginDirectories[0]))
                {
                    var file = Path.Combine(Path.GetDirectoryName(pluginDirectories[0]), "GisEditorPluginCore.dll");
                    if (File.Exists(file))
                        container = new CompositionContainer(new AssemblyCatalog(Assembly.LoadFrom(file)));
                }
#endif
            }

            Collection<T> tmpPlugins = new Collection<T>();
            PluginHelper.FillExportedPlugins<T>(container, tmpPlugins);
            tmpPlugins.ForEach(p =>
            {
                if (p.IsRequired && !p.IsActive) p.IsActive = true;
                plugins.Add(p);
            });

            #region Layer Plugin Key Attribute Authorizing.
            var allLayerTypeGroup = plugins.OfType<LayerPlugin>().GroupBy(g => g.GetLayerType());
            foreach (var group in allLayerTypeGroup)
            {
                int count = group.Count();
                if (count > 1)
                {
                    var attributes = group.Key.GetCustomAttributes(typeof(LayerPluginKeyAttribute), false).OfType<LayerPluginKeyAttribute>();
                    if (attributes.Count() > 0)
                    {
                        List<string> keys = attributes.Select(a => a.Key).ToList();
                        string layerName = group.Key.FullName;
                        Collection<string> layerPluginNames = new Collection<string>();
                        foreach (var item in group)
                        {
                            string pluginName = item.GetType().FullName;
                            layerPluginNames.Add(pluginName);
                        }
                        Collection<Plugin> willRemovingPlugins = new Collection<Plugin>();
                        foreach (var item in layerPluginNames)
                        {
                            string key = LayerPluginKeyHelper.Sha1Encrypt(item, layerName);
                            if (!keys.Contains(key))
                            {
                                var plugin = plugins.First(p => p.GetType().FullName == item);
                                willRemovingPlugins.Add(plugin);
                            }
                        }
                        if (willRemovingPlugins.Count < layerPluginNames.Count)
                        {
                            foreach (var item in willRemovingPlugins)
                            {
                                plugins.Remove(item);
                            }
                        }
                    }
                }
            }
            #endregion

            return plugins;
        }
    }
}