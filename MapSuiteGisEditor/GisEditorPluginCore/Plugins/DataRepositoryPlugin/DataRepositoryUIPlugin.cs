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
using System.Linq;
using System.Xml.Linq;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DataRepositoryUIPlugin : UIPlugin
    {
        [NonSerialized]
        private DataRepositoryUserControl dataRepositoryUserControl;
        private DockWindow dockWindow;

        public DataRepositoryUIPlugin()
        { }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            if (refreshArgs is DataRepositoryUIRefreshArgs)
            {
                foreach (var dataRepositoryPlugin in ((DataRepositoryUIRefreshArgs)refreshArgs).RefreshPlugins)
                {
                    Type type = dataRepositoryPlugin.GetType();
                    DataRepositoryPlugin resultDataRepositoryPlugin = GisEditor.DataRepositoryManager.GetActiveDataRepositoryPlugins().FirstOrDefault(d => d.GetType() == type);
                    var treeViewItem = DataRepositoryContentViewModel.Current.Children.FirstOrDefault(t => t.SourcePlugin == resultDataRepositoryPlugin);
                    treeViewItem.Children.Clear();
                    foreach (var dataRepositoryItem in resultDataRepositoryPlugin.RootDataRepositoryItem.Children)
                    {
                        treeViewItem.Children.Add(dataRepositoryItem);
                    }
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.GlobalSettings["DataRepository"] = DataRepositoryContentViewModel.Current.GetSettings().ToString();
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("DataRepository"))
            {
                try
                {
                    DataRepositoryContentViewModel.Current.ApplySettings(XElement.Parse(settings.GlobalSettings["DataRepository"]));
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            if (dataRepositoryUserControl == null)
            {
                dataRepositoryUserControl = new DataRepositoryUserControl();
                dataRepositoryUserControl.DataContext = DataRepositoryContentViewModel.Current;
                dockWindow = new DockWindow(dataRepositoryUserControl, DockWindowPosition.Right, "DataRepositoryPluginTitle");
                dockWindow.Name = "DataRepository";
                DockWindows.Add(dockWindow);
            }
            if (!DockWindows.Contains(dockWindow))
            {
                DockWindows.Add(dockWindow);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            DataRepositoryContentViewModel.Current.CurrentPluginItemViewModel = null;
        }
    }
}
