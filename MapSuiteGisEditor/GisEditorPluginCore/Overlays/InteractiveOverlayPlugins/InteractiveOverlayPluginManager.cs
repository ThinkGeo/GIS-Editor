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
using System.Linq;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class InteractiveOverlayPluginManager : PluginManager
    {
        private static InteractiveOverlayPluginManager instance;

        protected InteractiveOverlayPluginManager()
        { }

        public static InteractiveOverlayPluginManager Instance
        {
            get { return instance ?? (instance = new InteractiveOverlayPluginManager()); }
        }

        public Collection<InteractiveOverlayPlugin> GetInteractiveOverlayPlugins()
        {
            return new Collection<InteractiveOverlayPlugin>(GetPlugins().Cast<InteractiveOverlayPlugin>().ToList());
        }

        public bool CheckIsEnabled(InteractiveOverlay overlay)
        {
            return CheckIsEnabledCore(overlay);
        }

        protected virtual bool CheckIsEnabledCore(InteractiveOverlay interactiveOverlay)
        {
            bool isEnabled = true;
            var plugin = GetPlugins().Cast<InteractiveOverlayPlugin>().FirstOrDefault(p => p.GetInteractiveOverlayType() == interactiveOverlay.GetType());
            if (plugin == null)
            {
                plugin = GetPlugins().Cast<InteractiveOverlayPlugin>().FirstOrDefault(p => interactiveOverlay.GetType().IsSubclassOf(p.GetInteractiveOverlayType()));
            }

            if (plugin != null)
            {
                isEnabled = plugin.GetIsEnabled(interactiveOverlay);
            }

            return isEnabled;
        }

        public void Disable(InteractiveOverlay interactiveOverlay)
        {
            DisableCore(interactiveOverlay);
        }

        protected virtual void DisableCore(InteractiveOverlay interactiveOverlay)
        {
            var plugin = GetPlugins().Cast<InteractiveOverlayPlugin>().FirstOrDefault(p => p.GetInteractiveOverlayType() == interactiveOverlay.GetType());
            if (plugin == null)
            {
                plugin = GetPlugins().Cast<InteractiveOverlayPlugin>().FirstOrDefault(p => interactiveOverlay.GetType().IsSubclassOf(p.GetInteractiveOverlayType()));
            }

            if (plugin != null)
            {
                plugin.Disable(interactiveOverlay);
            }
        }

        protected override Collection<Plugin> GetPluginsCore()
        {
            return new Collection<Plugin>(PluginHelper.GetDefaultPlugins<InteractiveOverlayPlugin>().Cast<Plugin>().ToList());
        }
    }
}