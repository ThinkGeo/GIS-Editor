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
    public class HelpUIPlugin : UIPlugin
    {
        private static HelpViewModel viewModel;
        private RibbonEntry helpEntry;

        static HelpUIPlugin()
        {
            viewModel = new HelpViewModel();
        }

        public HelpUIPlugin()
        {
            Index = UIPluginOrder.HelpPlugin;
            Description = GisEditor.LanguageManager.GetStringResource("HelpPluginDescription"); 
            HelpRibbonGroup helpGroup = new HelpRibbonGroup();
            helpGroup.DataContext = viewModel;

            helpEntry = new RibbonEntry();
            helpEntry.RibbonGroup = helpGroup;
            helpEntry.RibbonTabName = "HelpRibbonTabHeader";
            helpEntry.RibbonTabIndex = RibbonTabOrder.Help;
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (!RibbonEntries.Contains(helpEntry))
            {
                RibbonEntries.Add(helpEntry);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            RibbonEntries.Clear();
        }
    }
}
