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
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class NoaaDataRepositoryPlugin : DataRepositoryPlugin
    {
        private NoaaWeatherOptionUserControl optionUI;
        private NoaaRootDataRepositoryItem rootDataRepositoryItem;

        public NoaaDataRepositoryPlugin()
        {
            Name = "Noaa Weather";
            Content = new NoaaDataRepositoryContent();
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_logo.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Noaa/noaa_logo.png", UriKind.RelativeOrAbsolute));
        }

        protected override DataRepositoryItem RootDataRepositoryItemCore
        {
            get { return rootDataRepositoryItem ?? (rootDataRepositoryItem = new NoaaRootDataRepositoryItem()); }
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new NoaaWeatherOptionUserControl();
                optionUI.DataContext = new NoaaWeatherOptionViewModel(Singleton<NoaaWeatherSetting>.Instance);
            }
            return optionUI;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in Singleton<NoaaWeatherSetting>.Instance.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            Singleton<NoaaWeatherSetting>.Instance.LoadState(settings.GlobalSettings);
        }
    }
}