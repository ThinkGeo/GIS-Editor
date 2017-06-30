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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class PluginHelper
    {
        private static readonly string searchPattern = "*.dll";
        private static MultiDirectoryCatalog defaultCatalog;
        private static CompositionContainer defaultContainer;

        internal static void Restore(Dictionary<string, string> dictionary, string key, Action<string> action)
        {
            if (dictionary.ContainsKey(key))
            {
                action(dictionary[key]);
            }
        }

        internal static void RestoreBoolean(Dictionary<string, string> dictionary, string key, Action<bool> action)
        {
            Restore(dictionary, key, str =>
            {
                bool result;
                if (bool.TryParse(str, out result))
                {
                    action(result);
                }
            });
        }

        internal static void RestoreInteger(Dictionary<string, string> dictionary, string key, Action<int> action)
        {
            Restore(dictionary, key, str =>
            {
                int result;
                if (int.TryParse(str, out result))
                {
                    action(result);
                }
            });
        }

        internal static void RestoreDouble(Dictionary<string, string> dictionary, string key, Action<double> action)
        {
            Restore(dictionary, key, str =>
            {
                double result;
                if (double.TryParse(str, out result))
                {
                    action(result);
                }
            });
        }

        internal static Collection<T> GetExportedPlugins<T>(CompositionContainer container)
        {
            return new Collection<T>(GetExportedPlugins(typeof(T), container).Cast<T>().ToList());
        }

        internal static Collection<T> GetExportedPlugins<T>(string directory)
        {
            MultiDirectoryCatalog catalog = new MultiDirectoryCatalog(new string[] { directory }, searchPattern);
            CompositionContainer container = new CompositionContainer(catalog);
            try
            {
                var plugins = GetExportedPlugins(typeof(T), container);
                return new Collection<T>(plugins.Cast<T>().ToList());
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return new Collection<T>();
            }
            finally
            {
                if (catalog != null) catalog.Dispose();
                if (container != null) container.Dispose();
            }
        }

        internal static Collection<T> GetDefaultPlugins<T>()
        {
            string directory = Path.Combine(FolderHelper.GetEntryPath(), "Plugins");

            if (defaultCatalog == null)
            {
                defaultCatalog = new MultiDirectoryCatalog(new string[] { directory }, searchPattern);
                defaultContainer = new CompositionContainer(defaultCatalog);
            }

            return new Collection<T>(GetExportedPlugins(typeof(T), defaultContainer).Cast<T>().ToList());
        }

        private static Collection<object> GetExportedPlugins(Type targetObject, CompositionContainer container)
        {
            Collection<object> plugins = new Collection<object>();

            foreach (var plugin in container.GetExports(targetObject, null, null))
            {
                try
                {
                    plugins.Add(plugin.Value);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }

            return plugins;
        }
    }
}