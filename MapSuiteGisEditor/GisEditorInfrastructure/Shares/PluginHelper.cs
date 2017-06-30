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
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal static class PluginHelper
    {
        private static string entryPath;
        private static readonly string searchPattern = "*.dll";

        internal static Collection<T> GetExportedPlugins<T>()
        {
            return GetExportedPlugins<T>(typeof(PluginHelper).Assembly);
        }

        internal static Collection<T> GetExportedPlugins<T>(Assembly assembly)
        {
            Collection<T> plugins = new Collection<T>();
            AssemblyCatalog catalog = null;
            CompositionContainer container = null;
            try
            {
                catalog = new AssemblyCatalog(assembly);
                container = new CompositionContainer(catalog);
                FillExportedPlugins<T>(container, plugins);
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
            }
            finally
            {
                catalog.Dispose();
                container.Dispose();
            }

            return plugins;
        }

        internal static Collection<T> GetExportedPlugins<T>(IEnumerable<string> directories)
        {
            Collection<T> plugins = new Collection<T>();
            MultiDirectoryCatalog catalog = null;
            CompositionContainer container = null;
            try
            {
                catalog = new MultiDirectoryCatalog(directories, searchPattern);
                container = new CompositionContainer(catalog);
                FillExportedPlugins<T>(container, plugins);
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
            }
            finally
            {
                if (catalog != null) catalog.Dispose();
                if (container != null) container.Dispose();
            }

            return plugins;
        }

        internal static Collection<T> GetExportedPlugins<T>(string directory)
        {
            return GetExportedPlugins<T>(new string[] { directory });
        }

        internal static void FillExportedPlugins<T>(ExportProvider exportProvider, Collection<T> plugins)
        {
            var pluginExports = exportProvider.GetExports<T>();
            foreach (var pluginExport in pluginExports)
            {
                try { plugins.Add(pluginExport.Value); }
                catch (Exception e)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                }
            }
        }

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

        internal static void RestoreEnum<T>(Dictionary<string, string> items, string key, Action<T> action) where T : struct
        {
            Restore(items, key, str =>
            {
                T oldValue = default(T);
                if (Enum.TryParse<T>(str, out oldValue))
                {
                    action(oldValue);
                }
            });
        }

        internal static string GetEntryPath()
        {
            if (string.IsNullOrEmpty(entryPath))
            {
#if GISEditorUnitTest
            entryPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
#else
                entryPath = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
#endif
            }

            return entryPath;
        }

        internal static T GetDataContext<T>(this object sender) where T : class
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                return element.DataContext as T;
            }
            return null;
        }
    }
}