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
using System.Linq;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class OpenStreetMapDataRepositoryItem : DataRepositoryItem
    {
        public OpenStreetMapDataRepositoryItem()
        {
            Name = GisEditor.LanguageManager.GetStringResource("OpenStreetMapName");
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/OSM.png", UriKind.RelativeOrAbsolute));
        }

        protected override bool IsLeafCore
        {
            get { return true; }
        }

        protected override bool IsLoadableCore
        {
            get { return true; }
        }

        protected override void LoadCore()
        {
            BaseMapsHelper.AddOpenStreetMapOverlay(GisEditor.ActiveMap);
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadCoreDescription));
        }

        protected override Collection<DataRepositoryItem> GetSearchResultCore(IEnumerable<string> keywords)
        {
            var result = new Collection<DataRepositoryItem>();
            if (keywords.Any(keyWord => Name.IndexOf(keyWord, StringComparison.OrdinalIgnoreCase) != -1))
            {
                var item = new OpenStreetMapDataRepositoryItem();
                item.Icon = null;
                result.Add(item);
            }
            return result;
        }
    }
}