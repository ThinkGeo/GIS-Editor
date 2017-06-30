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
using System.Linq;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BaseMapDataRepositoryPlugin : DataRepositoryPlugin
    {
        private Dictionary<string, string> baseMapsInfo;
        private DataRepositoryItem rootDataRepositoryItem;

        public BaseMapDataRepositoryPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("BaseMapDataRepositoryPluginName");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_base_maps.png", UriKind.RelativeOrAbsolute));
            Content = new BaseMapsDataRepositoryUserControl();

            Index = DataRepositoryOrder.BaseMap;
            baseMapsInfo = new Dictionary<string, string>();
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            foreach (var item in RootDataRepositoryItem.Children.OfType<IStorableSettings>())
            {
                item.ApplySettings(settings);
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in RootDataRepositoryItem.Children.OfType<IStorableSettings>())
            {
                var currentSettings = item.GetSettings();
                foreach (var setting in currentSettings.GlobalSettings)
                {
                    settings.GlobalSettings[setting.Key] = setting.Value;
                }
                foreach (var setting in currentSettings.ProjectSettings)
                {
                    settings.GlobalSettings[setting.Key] = setting.Value;
                }
            }
            return settings;
        }

        protected override DataRepositoryItem RootDataRepositoryItemCore
        {
            get
            {
                if (rootDataRepositoryItem == null)
                {
                    rootDataRepositoryItem = DataRepositoryHelper.CreateRootDataRepositoryItem(this);
                    rootDataRepositoryItem.Children.Add(new WorldMapKitMapDataRepositoryItem());
                    rootDataRepositoryItem.Children.Add(new OpenStreetMapDataRepositoryItem());
                    rootDataRepositoryItem.Children.Add(new BingMapDataRepositoryItem());
                }
                return rootDataRepositoryItem;
            }
        }
    }
}
