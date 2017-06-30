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
    ///
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(ControlPluginManager))]
    public class ControlPluginManager : PluginManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPluginManager" /> class.
        /// </summary>
        public ControlPluginManager()
        { }

        /// <summary>
        /// Gets the UI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetUI<T>() where T : class
        {
            return GetUICore<T>();
        }

        /// <summary>
        /// Gets the UI core.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected virtual T GetUICore<T>() where T : class
        {
            var uiPlugins = GetActiveControlPlugins();

            var type = typeof(T);
            if (type.IsInterface)
            {
                var matchingProviders = uiPlugins.Where(uiProvider => uiProvider.GetUI().GetType().GetInterface(type.Name) != null).ToArray();
                if (matchingProviders.Length == 1)
                {
                    return matchingProviders[0].GetUI() as T;
                }
                else if (matchingProviders.Length > 1)
                {
                    var resultProvider = matchingProviders.FirstOrDefault(uiProvider => uiProvider.GetType().Assembly != type.Assembly);
                    if (resultProvider != null)
                    {
                        return resultProvider.GetUI() as T;
                    }
                    else return matchingProviders[0].GetUI() as T;
                }
                else return null;
            }
            else
            {
                // here children class first.
                var matchingProvider = uiPlugins.FirstOrDefault(uiProvider => uiProvider.GetUI().GetType().IsSubclassOf(type));
                if (matchingProvider == null)
                {
                    // if no children class then the super class.
                    matchingProvider = uiPlugins.FirstOrDefault(uiProvider => uiProvider.GetUI().GetType() == type);
                }

                if (matchingProvider != null)
                {
                    return matchingProvider.GetUI() as T;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            return CollectPlugins<ControlPlugin>();
        }

        /// <summary>
        /// Gets the control plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<ControlPlugin> GetControlPlugins()
        {
            return new Collection<ControlPlugin>(GetPlugins().Cast<ControlPlugin>().ToList());
        }

        public Collection<T> GetActiveControlPlugins<T>() where T : ControlPlugin
        {
            return new Collection<T>(GetActiveControlPlugins().OfType<T>().ToList());
        }

        public Collection<ControlPlugin> GetActiveControlPlugins()
        {
            var activePlugins = from p in GetControlPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<ControlPlugin>(activePlugins.ToList());
        }
    }
}