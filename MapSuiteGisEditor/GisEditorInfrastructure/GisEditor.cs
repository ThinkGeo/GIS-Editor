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


using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class contains some global objects will be accessed by both shell and plugins.
    /// </summary>
    public static class GisEditor
    {
        private static UIPluginManager uiManager;
        private static DockWindowManager dockWindowManager;
        private static LoggerPluginManager loggerManager;
        private static LanguageManager languageManager;
        private static StylePluginManager styleManager;
        private static LayerPluginManager layerManager;
        private static ControlPluginManager controlManager;
        private static ProjectPluginManager projectManager;
        private static InfrastructureManager infrastructureManager;
        private static TaskPluginManager taskManager;
        private static SelectionManager selectionManager;
        private static DataRepositoryPluginManager dataRepositoryManager;
        private static LayerListManager layerListManager;
        private static GeoSerializer serializer;
        private static bool isReadOnly;

        static GisEditor()
        {
            serializer = new GisEditorGeoSerializer();
            infrastructureManager = CreateManager<InfrastructureManager>();
            Collection<Manager> managers = infrastructureManager.GetManagers();
            ProjectManager.CurrentProjectPlugin = ProjectManager.GetProjectPlugins().OrderBy(p => p.Index).FirstOrDefault();
            managers.OfType<PluginManager>().ForEach(m => m.GetPlugins());
            infrastructureManager.ApplySettings(managers.OfType<PluginManager>().SelectMany(m => m.GetPlugins()));
        }

        /// <summary>
        /// This static property gets or sets the map that is activated.
        /// </summary>
        /// <remarks>
        /// This property changes automatically when you switch your tabs.
        /// One tab maintains only one map, so the map is changed when the tab is changed.
        /// You can add any kind of overlay during overriding plugins;
        /// please clear the overlays you added before exiting the main application.
        /// </remarks>
        public static GisEditorWpfMap ActiveMap { get; set; }

        public static InfrastructureManager InfrastructureManager
        {
            get { return infrastructureManager; }
        }

        public static StylePluginManager StyleManager
        {
            get { return GetManager<StylePluginManager>(ref styleManager); }
        }

        public static LayerPluginManager LayerManager
        {
            get { return GetManager<LayerPluginManager>(ref layerManager); }
        }

        public static LayerListManager LayerListManager
        {
            get { return GetManager<LayerListManager>(ref layerListManager); }
        }

        public static DataRepositoryPluginManager DataRepositoryManager
        {
            get { return GetManager<DataRepositoryPluginManager>(ref dataRepositoryManager); }
        }

        public static UIPluginManager UIManager
        {
            get { return GetManager<UIPluginManager>(ref uiManager); }
        }

        public static ProjectPluginManager ProjectManager
        {
            get { return GetManager<ProjectPluginManager>(ref projectManager); }
        }

        public static DockWindowManager DockWindowManager
        {
            get { return GetManager<DockWindowManager>(ref dockWindowManager); }
        }

        public static LanguageManager LanguageManager
        {
            get { return GetManager<LanguageManager>(ref languageManager); }
        }

        public static TaskPluginManager TaskManager
        {
            get { return GetManager<TaskPluginManager>(ref taskManager); }
        }

        public static LoggerPluginManager LoggerManager
        {
            get { return GetManager<LoggerPluginManager>(ref loggerManager); }
        }

        public static ControlPluginManager ControlManager
        {
            get { return GetManager<ControlPluginManager>(ref controlManager); }
        }

        public static SelectionManager SelectionManager
        {
            get { return GetManager<SelectionManager>(ref selectionManager); }
        }

        public static GeoSerializer Serializer
        {
            get
            {
                return serializer;
            }
            set
            {
                serializer = value;
            }
        }

        public static Collection<GisEditorWpfMap> GetMaps()
        {
            return new Collection<GisEditorWpfMap>(DockWindowManager.DocumentWindows.Select(d => d.Content).OfType<GisEditorWpfMap>().ToList());
        }

        private static T CreateManager<T>()
        {
            string managerPath = Path.Combine(PluginHelper.GetEntryPath(), "Managers");
            var manager = PluginHelper.GetExportedPlugins<T>(managerPath).FirstOrDefault();
            if (manager == null) manager = PluginHelper.GetExportedPlugins<T>().FirstOrDefault();
            return manager;
        }

        private static T GetManager<T>(ref T manager) where T : Manager
        {
            if (manager == null)
            {
                var foundManagers = infrastructureManager.GetManagers().OfType<T>().ToArray();
                if (foundManagers.Length > 0)
                {
                    manager = foundManagers.First();
                    InfrastructureManager.ApplySettings(manager);
                }
            }

            return manager;
        }
    }
}