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
using System.ComponentModel.Composition;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a base class of Managers in all GISEditor system. 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(Manager))]
    public abstract class Manager : IStorableSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Manager" /> class.
        /// </summary>
        protected Manager()
        { }

        /// <summary>
        /// Gets manager settings to save.
        /// </summary>
        /// <returns>Manager's settings to save</returns>
        public StorableSettings GetSettings()
        {
            return GetSettingsCore();
        }

        /// <summary>
        /// Gets manager settings to save.
        /// </summary>
        /// <returns>Manager's settings to save</returns>
        protected virtual StorableSettings GetSettingsCore()
        {
            return new StorableSettings();
        }

        /// <summary>
        /// Applies the settings to this manager.
        /// </summary>
        /// <param name="settings">The settings to be applied to this manager.</param>
        public void ApplySettings(StorableSettings settings)
        {
            ApplySettingsCore(settings);
        }

        /// <summary>
        /// Applies the settings to this manager.
        /// </summary>
        /// <param name="settings">The settings to be applied to this manager.</param>
        protected virtual void ApplySettingsCore(StorableSettings settings)
        {
        }

        /// <summary>
        /// Gets an UI that configures settings.
        /// </summary>
        /// <returns>A SettingUserControl that configures settings.</returns>
        public SettingUserControl GetSettingsUI()
        {
            return GetSettingsUICore();
        }

        /// <summary>
        /// Gets an UI that configures settings.
        /// </summary>
        /// <returns>A SettingUserControl that configures settings.</returns>
        protected virtual SettingUserControl GetSettingsUICore()
        {
            return null;
        }
    }
}