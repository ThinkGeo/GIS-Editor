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


namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class SearchEntriesModel
    {
        private string name;
        private bool isChecked;
        private LayerPlugin layerPlugin;

        public SearchEntriesModel(string name, bool isChecked, LayerPlugin layerPlugin)
        {
            this.name = name;
            this.isChecked = isChecked;
            this.layerPlugin = layerPlugin;
        }

        public string Name
        {
            get { return name; }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set { isChecked = value; }
        }

        public LayerPlugin LayerPlugin
        {
            get { return layerPlugin; }
        }
    }
}