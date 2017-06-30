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
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal abstract class ManagerTabItemSource
    {
        public Action<ManagerTabItemSource> Applying;

        public Action<ManagerTabItemSource, bool> PluginsSelected;

        public abstract string HeaderText { get; }

        public abstract ImageSource HeaderBackground { get; }

        public UserControl Content { get; set; }

        public virtual bool DelayLoad
        {
            get { return false; }
        }

        public IEnumerable<Plugin> CollectPlugins()
        {
            return CollectPluginsCore();
        }

        protected virtual IEnumerable<Plugin> CollectPluginsCore()
        {
            return null;
        }

        //public IEnumerable<PluginInfo> CollectPluginConfigurations()
        //{
        //    return CollectPluginConfigurationsCore();
        //}

        //protected virtual IEnumerable<PluginInfo> CollectPluginConfigurationsCore()
        //{
        //    return null;
        //}

        public object BindingSource { get; set; }

        public object GetBindingSource()
        {
            return GetBindingSourceCore();
        }

        protected virtual object GetBindingSourceCore()
        {
            return null;
        }

        public void Apply()
        {
            OnApplying();
            ApplyCore();
        }

        protected virtual void ApplyCore() { }

        public void CheckAll(bool isChecked)
        {
            CheckAllCore(isChecked);
            OnPluginsSelected(isChecked);
        }

        protected virtual void CheckAllCore(bool isChecked) { }

        /// <summary>
        /// This API is only used in one place for the online plugin.
        ///
        /// We have serveral Manager tabs:
        /// </summary>
        public void Activate()
        {
            ActivateCore();
        }

        protected virtual void ActivateCore()
        { }

        protected virtual void OnApplying()
        {
            Action<ManagerTabItemSource> handler = Applying;
            if (handler != null)
            {
                handler(this);
            }
        }

        protected virtual void OnPluginsSelected(bool isSelected)
        {
            Action<ManagerTabItemSource, bool> handler = PluginsSelected;
            if (handler != null)
            {
                handler(this, isSelected);
            }
        }
    }
}