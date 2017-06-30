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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(LayerPluginManager))]
    public class LayerPluginManager : PluginManager
    {
        private Dictionary<string, string> featureIdColumnNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerPluginManager" /> class.
        /// </summary>
        public LayerPluginManager()
        {
            featureIdColumnNames = new Dictionary<string, string>();
        }

        public Dictionary<string, string> FeatureIdColumnNames
        {
            get { return featureIdColumnNames; }
        }

        public TimeSpan RequestDrawingBufferTime
        {
            get { return TimeSpan.FromMilliseconds(TileOverlayExtension.RefreshBufferTimeInMillisecond); }
            set { TileOverlayExtension.RefreshBufferTimeInMillisecond = (int)value.TotalMilliseconds; }
        }

        /// <summary>
        /// Gets the layers.
        /// </summary>
        /// <returns></returns>
        public Collection<Layer> GetLayers()
        {
            return GetLayersCore();
        }

        /// <summary>
        /// Gets the layers core.
        /// </summary>
        /// <returns></returns>
        protected virtual Collection<Layer> GetLayersCore()
        {
            Collection<Layer> resultsLayers = new Collection<Layer>();
            var supportedLayerProviders = GetLayerPlugins()
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ExtensionFilter)).ToList();

            int count = supportedLayerProviders.Count();
            if (count > 0)
            {
                StringBuilder filterStringBuilder = new StringBuilder();
                StringBuilder allFilesFilterStringBuilder = new StringBuilder();

                foreach (var fileLayerPlugin in supportedLayerProviders)
                {
                    string[] array = fileLayerPlugin.ExtensionFilter.Split('|');
                    if (array != null && array.Length >= 2)
                    {
                        allFilesFilterStringBuilder.Append(array[1] + ";");
                    }
                }

                filterStringBuilder.Append("All Supported Formats|" + allFilesFilterStringBuilder.ToString());

                foreach (var fileLayerPlugin in supportedLayerProviders)
                {
                    filterStringBuilder.Append("|" + fileLayerPlugin.ExtensionFilter);
                }

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = filterStringBuilder.ToString();
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    var groupedFileDataItems = openFileDialog.FileNames.GroupBy(item => Path.GetExtension(item));
                    foreach (var fileDataItems in groupedFileDataItems)
                    {
                        var extension = fileDataItems.Key.ToUpperInvariant();
                        var matchingLayerPlugin = GetActiveLayerPlugins()
                            .FirstOrDefault(tmpPlugin => tmpPlugin.ExtensionFilter.ToUpperInvariant()
                                .Contains(extension));

                        if (matchingLayerPlugin != null)
                        {
                            var getLayersParameters = new GetLayersParameters();
                            foreach (var item in fileDataItems)
                            {
                                getLayersParameters.LayerUris.Add(new Uri(item));
                            }
                            var layers = matchingLayerPlugin.GetLayers(getLayersParameters);
                            foreach (var layer in layers)
                            {
                                resultsLayers.Add(layer);
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataRepositoryCannotFindLayerProviderWarningLabel"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LayerPluginManagerNotFoundText"), GisEditor.LanguageManager.GetStringResource("LayerPluginManagerNotFoundCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }

            return resultsLayers;
        }

        public Collection<T> GetLayers<T>(GetLayersParameters getLayersParameters) where T : Layer
        {
            return GetLayersCore<T>(getLayersParameters);
        }

        protected virtual Collection<T> GetLayersCore<T>(GetLayersParameters getLayersParameters) where T : Layer
        {
            Collection<T> resultLayers = new Collection<T>();
            LayerPlugin layerPlugin = GetLayerPlugins(typeof(T)).FirstOrDefault();
            if (layerPlugin != null && layerPlugin.IsActive)
            {
                IEnumerable<T> layers = layerPlugin.GetLayers(getLayersParameters).OfType<T>();
                foreach (T layer in layers)
                {
                    resultLayers.Add(layer);
                }
            }

            return resultLayers;
        }

        /// <summary>
        /// Gets the active layer plugins.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Collection<T> GetActiveLayerPlugins<T>() where T : LayerPlugin
        {
            return new Collection<T>(GetActiveLayerPlugins().OfType<T>().ToList());
        }

        /// <summary>
        /// Gets the layer plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<LayerPlugin> GetActiveLayerPlugins()
        {
            var activePlugins = from p in GetLayerPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<LayerPlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Gets the layer plugins.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Collection<LayerPlugin> GetLayerPlugins<T>() where T : Layer
        {
            return GetLayerPluginsCore(typeof(T));
        }

        /// <summary>
        /// Gets the layer plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<LayerPlugin> GetLayerPlugins()
        {
            return new Collection<LayerPlugin>(GetPlugins().Cast<LayerPlugin>().ToList());
        }

        /// <summary>
        /// Gets the layer plugins.
        /// </summary>
        /// <param name="layerType">Type of the layer.</param>
        /// <returns></returns>
        public Collection<LayerPlugin> GetLayerPlugins(Type layerType)
        {
            return GetLayerPluginsCore(layerType);
        }

        /// <summary>
        /// Gets the layer plugins core.
        /// </summary>
        /// <param name="layerType">Type of the layer.</param>
        /// <returns></returns>
        protected virtual Collection<LayerPlugin> GetLayerPluginsCore(Type layerType)
        {
            var providers = GetPlugins().OfType<LayerPlugin>().Where(p => p.IsActive).OrderBy(p => p.Index).ToList();          
            Collection<LayerPlugin> supportedProviders = new Collection<LayerPlugin>();
            foreach (var supportedProvider in providers.Where(tmpProvider => tmpProvider.GetLayerType().Equals(layerType)))
            {
                supportedProviders.Add(supportedProvider);
            }

            if (supportedProviders.Count == 0)
            {
                foreach (var supportedProvider in providers.Where(tmpProvider => layerType.IsSubclassOf(tmpProvider.GetLayerType())))
                {
                    supportedProviders.Add(supportedProvider);
                }
            }

            return supportedProviders;
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            var result = new Collection<Plugin>();
            var exportedPlugins = CollectPlugins<LayerPlugin>().OrderBy(l => l.Index).OfType<LayerPlugin>().ToList();
            var groupPlugins = exportedPlugins.OfType<GroupLayerPlugin>().SelectMany(p => p.LayerPlugins).ToList();
            foreach (var plugin in groupPlugins)
            {
                exportedPlugins.Add(plugin);
            }
            var exportedPluginGroup = exportedPlugins.GroupBy(p => p.GetLayerType());

            foreach (var item in exportedPluginGroup)
            {
                int itemCount = item.Count();
                if (itemCount == 1)
                {
                    result.Add(item.First());
                }
                else if (itemCount > 1)
                {
                    var originalPlugin = item.FirstOrDefault(i => i.GetType().Assembly.Location.Equals(DefaultPluginPathFileName, StringComparison.OrdinalIgnoreCase));
                    Type originalType = originalPlugin.GetType();

                    var customPlugins = item.Where(i => !i.GetType().Assembly.Location.Equals(DefaultPluginPathFileName, StringComparison.OrdinalIgnoreCase)).ToArray();
                    var inheritedPlugins = new Collection<Plugin>();
                    foreach (var customPlugin in customPlugins)
                    {
                        if (customPlugin != null)
                        {
                            if (customPlugin.GetType().IsSubclassOf(originalType))
                            {
                                inheritedPlugins.Add(customPlugin);
                            }
                            else
                            {
                                result.Add(customPlugin);
                            }
                        }
                    }
                    if (inheritedPlugins.Count == 0)
                    {
                        result.Add(originalPlugin);
                    }
                    else
                    {
                        foreach (var inheritedPlugin in inheritedPlugins)
                        {
                            result.Add(inheritedPlugin);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the layer list item.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        public LayerListItem GetLayerListItem(Layer layer)
        {
            return GetLayerListItemCore(layer);
        }

        /// <summary>
        /// Gets the layer list item core.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        protected virtual LayerListItem GetLayerListItemCore(Layer layer)
        {
            LayerPlugin matchLayerPlugin = null;
            var matchLayerPlugins = GetLayerPlugins(layer.GetType());
            if (matchLayerPlugins.Count > 1)
                matchLayerPlugin = matchLayerPlugins.FirstOrDefault(p => !p.GetType().Assembly.Location.Equals(DefaultPluginPathFileName, StringComparison.OrdinalIgnoreCase));
            else
                matchLayerPlugin = matchLayerPlugins.FirstOrDefault();
            if (matchLayerPlugin != null) return matchLayerPlugin.GetLayerListItem(layer);
            else return null;
        }

        public SimpleShapeType GetFeatureSimpleShapeType(FeatureLayer featureLayer)
        {
            return GetFeatureSimpleShapeTypeCore(featureLayer);
        }

        protected virtual SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            SimpleShapeType result = SimpleShapeType.Unknown;
            FeatureLayerPlugin plugin = GetLayerPlugins(featureLayer.GetType()).OfType<FeatureLayerPlugin>().FirstOrDefault();
            if (plugin != null)
            {
                result = plugin.GetFeatureSimpleShapeType(featureLayer);
            }

            return result;
        }
    }
}