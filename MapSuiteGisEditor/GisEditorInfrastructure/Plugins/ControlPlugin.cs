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
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a control plugin
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(ControlPlugin))]
    public abstract class ControlPlugin : Plugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlugin" /> class.
        /// </summary>
        protected ControlPlugin()
        {
            Description = GisEditor.LanguageManager.GetStringResource("ControlPluginProvidesUIDescription");
        }

        /// <summary>
        /// Gets the UI.
        /// </summary>
        /// <returns>The framework element</returns>
        public FrameworkElement GetUI()
        {
            return GetUICore();
        }

        /// <summary>
        /// Gets the UI core.
        /// </summary>
        /// <returns>The framework element</returns>
        protected abstract FrameworkElement GetUICore();
    }
}