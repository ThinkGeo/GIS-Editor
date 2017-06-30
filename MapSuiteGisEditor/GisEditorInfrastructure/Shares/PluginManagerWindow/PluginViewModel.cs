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
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class PluginViewModel : ViewModelBase
    {
        private bool isEnabled;
        private bool isEditable;

        public PluginViewModel(Plugin plugin)
        {
            Name = plugin.Name;
            Author = plugin.Author;
            Description = plugin.Description;
            IconSource = plugin.LargeIcon;
            Version = FileVersionInfo.GetVersionInfo(plugin.GetType().Assembly.Location).ProductVersion;
            Plugin = plugin;
            IsEnabled = plugin.IsActive;
            isEditable = !plugin.IsRequired;
        }

        public string Name { get; set; }

        public string Author { get; set; }

        public string Version { get; set; }

        public string Description { get; set; }

        public string Keywords { get; set; }

        public ImageSource IconSource { get; set; }

        //public PluginInfo Configuration { get; set; }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; RaisePropertyChanged(()=>IsEnabled); }
        }

        public bool IsEditable
        {
            get { return isEditable; }
        }

        public Plugin Plugin { get; set; }
    }
}