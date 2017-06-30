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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GeoProcessingUIPlugin : UIPlugin
    {
        [NonSerialized]
        private GeoProcessingGroup geoprocessingGroup;
        private RibbonEntry geoprocessingEntry;
        private RibbonEntry dataEntry;
        private DataRibbonGroup dataRibbonGroup;

        public GeoProcessingUIPlugin()
        {
            Index = UIPluginOrder.GeoProcessingPlugin;
            geoprocessingGroup = new GeoProcessingGroup();
            geoprocessingEntry = new RibbonEntry(geoprocessingGroup, RibbonTabOrder.Tools, "ToolsRibbonTabHeader");

            dataRibbonGroup = new DataRibbonGroup();
            dataEntry = new RibbonEntry(dataRibbonGroup, RibbonTabOrder.Tools, "ToolsRibbonTabHeader");
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (!RibbonEntries.Contains(geoprocessingEntry)) RibbonEntries.Add(geoprocessingEntry);
            if (!RibbonEntries.Contains(dataEntry)) RibbonEntries.Add(dataEntry);
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            if (RibbonEntries.Contains(geoprocessingEntry)) RibbonEntries.Remove(geoprocessingEntry);
            if (RibbonEntries.Contains(dataEntry)) RibbonEntries.Remove(dataEntry);
        }
    }
}
