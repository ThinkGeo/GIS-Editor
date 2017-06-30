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


using AvalonDock;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    public static class GisEditorCommands
    {
        private static ObservedCommand newProjectCommand;
        private static ObservedCommand openProjectCommand;
        private static ObservedCommand openHelpPageCommand;
        private static ObservedCommand openWelcomePageCommand;
        private static ObservedCommand saveProjectCommand;
        private static ObservedCommand saveProjectAsCommand;
        private static ObservedCommand packageProjectCommand;
        private static ObservedCommand openPluginManagerDialogCommand;
        private static ObservedCommand openOptionManagerDialogCommand;
        private static ObservedCommand exitCommand;
        private static ObservedCommand newDocumentCommand;
        private static ObservedCommand<string> mapOperationCommand;
        private static ObservedCommand<Uri> openRecentProjectFileCommand;
        private static ObservedCommand<DockableContent> openDockableContentCommand;
        private static ObservedCommand<object> deleteActiveDocumentCommand;
        private static ObservedCommand<MenuItem> renameDocumentCommand;
        private static ObservedCommand<string> findForgottenPasswordCommand;
        private static RelayCommand refreshMapCommand;
        private static RelayCommand projectLockCommand;

        public static RelayCommand RefreshMapCommand
        {
            get
            {
                if (refreshMapCommand == null)
                {
                    refreshMapCommand = new RelayCommand(() =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            var overlays = GisEditor.ActiveMap.Overlays.Concat(GisEditor.ActiveMap.InteractiveOverlays).ToList();
                            foreach (var overlay in overlays)
                            {
                                TileOverlay tileOverlay = overlay as TileOverlay;
                                if (tileOverlay != null)
                                {
                                    tileOverlay.ClearCaches(GisEditor.ActiveMap.CurrentExtent);
                                    LayerOverlay layerOverlay = tileOverlay as LayerOverlay;
                                    tileOverlay.Invalidate(false);
                                }
                                else
                                {
                                    overlay.Refresh();
                                }
                            }
                        }
                    }, () => GisEditor.ActiveMap != null);
                }
                return refreshMapCommand;
            }
        }

        public static GisEditorUserControl GisEditorUserControl { get; internal set; }

        public static ObservedCommand NewDocumentCommand
        {
            get
            {
                if (newDocumentCommand == null)
                {
                    newDocumentCommand = new ObservedCommand(() =>
                    {
                        GisEditorUserControl.AddDocument();
                    }, () => true);
                }
                return newDocumentCommand;
            }
        }

        public static ObservedCommand<string> FindForgottenPasswordCommand
        {
            get
            {
                if (findForgottenPasswordCommand == null)
                {
                    findForgottenPasswordCommand = new ObservedCommand<string>(e =>
                    {
                        ProcessUtils.OpenUri(new Uri(e));
                    }, e => true);
                }
                return findForgottenPasswordCommand;
            }
        }

        public static ObservedCommand<MenuItem> RenameDocumentCommand
        {
            get
            {
                if (renameDocumentCommand == null)
                {
                    renameDocumentCommand = new ObservedCommand<MenuItem>(e =>
                    {
                        GisEditorUserControl.RenameDocument(e);
                    }, e => true);
                }
                return renameDocumentCommand;
            }
        }

        public static ObservedCommand<object> DeleteActiveDocumentCommand
        {
            get
            {
                if (deleteActiveDocumentCommand == null)
                {
                    deleteActiveDocumentCommand = new ObservedCommand<object>(e =>
                    {
                        GisEditorUserControl.DeleteSelectDocument();
                    }, e => true);
                }
                return deleteActiveDocumentCommand;
            }
        }

        public static ObservedCommand<string> MapOperationCommand
        {
            get
            {
                if (mapOperationCommand == null)
                {
                    mapOperationCommand = new ObservedCommand<string>(name =>
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            GisEditorUserControl.ProcessMapContextMenu(name);
                        }
                    }, e => GisEditor.ActiveMap != null);
                }
                return mapOperationCommand;
            }
        }

        public static ObservedCommand<Uri> OpenRecentProjectFileCommand
        {
            get
            {
                if (openRecentProjectFileCommand == null)
                {
                    openRecentProjectFileCommand = new ObservedCommand<Uri>(s =>
                    {
                        GisEditorUserControl.OpenRecentProjectFile(s);
                    },
                    s => (GisEditor.ProjectManager.ProjectUri != null && !s.LocalPath.Equals(GisEditor.ProjectManager.ProjectUri.LocalPath)));
                }

                return openRecentProjectFileCommand;
            }
        }

        public static ObservedCommand<DockableContent> OpenDockableContentCommand
        {
            get
            {
                if (openDockableContentCommand == null)
                {
                    openDockableContentCommand = new ObservedCommand<DockableContent>(e =>
                    {
                        if (e.Tag != null && (bool)e.Tag)
                        {
                            e.Hide();
                            e.Tag = false;
                        }
                        else
                        {
                            e.Show();
                            e.Tag = true;
                        }
                    }, e => true);
                }
                return openDockableContentCommand;
            }
        }

        public static ObservedCommand NewProjectCommand
        {
            get
            {
                if (newProjectCommand == null)
                {
                    newProjectCommand = new ObservedCommand(() => GisEditorUserControl.CreateNewProject(), () => true);
                }

                return newProjectCommand;
            }
        }

        public static ObservedCommand ExitCommand
        {
            get
            {
                if (exitCommand == null)
                {
                    exitCommand = new ObservedCommand(() => Window.GetWindow(GisEditorUserControl).Close(), () => true);
                }

                return exitCommand;
            }
        }

        //the following code is comented because we should not use the type PrintMapWindow here.
        //because it's in a plugin

        //public static ObservedCommand PrintCommand
        //{
        //    get
        //    {
        //        if (printCommand == null)
        //        {
        //            printCommand = new ObservedCommand(() =>
        //            {
        //                PrintMapWindow printMapWindow = new PrintMapWindow();
        //                printMapWindow.ShowDialog();
        //            }, () => true);
        //        }

        //        return printCommand;
        //    }
        //}

        public static ObservedCommand OpenProjectCommand
        {
            get
            {
                if (openProjectCommand == null)
                {
                    openProjectCommand = new ObservedCommand(() => GisEditorUserControl.OpenProject(), () => true);
                }
                return openProjectCommand;
            }
        }

        public static ObservedCommand OpenHelpPageCommand
        {
            get
            {
                if (openHelpPageCommand == null)
                {
                    openHelpPageCommand = new ObservedCommand(() =>
                    {
                        Process.Start("http://giseditorwiki.thinkgeo.com");
                    }, () => true);
                }

                return openHelpPageCommand;
            }
        }

        public static ObservedCommand OpenWelcomePageCommand
        {
            get
            {
                if (openWelcomePageCommand == null)
                {
                    openWelcomePageCommand = new ObservedCommand(() =>
                    {
                        AboutWindow about = new AboutWindow();
                        about.ShowDialog();
                    }, () => true);
                }

                return openWelcomePageCommand;
            }
        }

        public static ObservedCommand SaveProjectCommand
        {
            get
            {
                if (saveProjectCommand == null)
                {
                    saveProjectCommand = new ObservedCommand(() =>
                    {
                        GisEditorUserControl.SaveProject();
                    }, CheckCanSaveProject);
                }
                return saveProjectCommand;
            }
        }

        public static ObservedCommand SaveProjectAsCommand
        {
            get
            {
                if (saveProjectAsCommand == null)
                {
                    saveProjectAsCommand = new ObservedCommand(() =>
                    {
                        //Uri newUri = GisEditor.ProjectManager.CurrentProjectPlugin.GetProjectUriToSave();
                        //if (newUri != null)
                        //{
                        //    MainWindow.SaveProject(newUri);
                        //}

                        GisEditorUserControl.SaveProject(null);
                    }, CheckCanSaveProject);
                }
                return saveProjectAsCommand;
            }
        }

        public static ObservedCommand PackProjectCommand
        {
            get
            {
                if (packageProjectCommand == null)
                {
                    packageProjectCommand = new ObservedCommand(() =>
                    {
                        var plugin = GetFileProjectPlugin();

                        var tempPlugin = GisEditor.ProjectManager.CurrentProjectPlugin;

                        if (plugin != null)
                        {
                            GisEditor.ProjectManager.CurrentProjectPlugin = plugin;
                        }

                        //var tempUri = GisEditor.ProjectManager.ProjectUri;
                        //GisEditor.ProjectManager.ProjectUri = new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Map Suite Gis Editor", "TempProject.tgproj"));
                        //GisEditorUserControl.SaveProject();
                        GisEditor.ProjectManager.PackageProjectFile();
                        //GisEditor.ProjectManager.ProjectUri = tempUri;
                        GisEditor.ProjectManager.CurrentProjectPlugin = tempPlugin;
                    }, CheckCanSaveProject);
                }
                return packageProjectCommand;
            }
        }

        public static ObservedCommand OpenPluginsDialogCommand
        {
            get
            {
                if (openPluginManagerDialogCommand == null)
                {
                    openPluginManagerDialogCommand = new ObservedCommand(() =>
                    {
                        GisEditorUserControl.OpenPluginManagerDialog();
                    }, () => true);
                }
                return openPluginManagerDialogCommand;
            }
        }

        public static RelayCommand ProjectLockCommand
        {
            get
            {
                if (projectLockCommand == null)
                {
                    projectLockCommand = new RelayCommand(() =>
                    {
                        bool canOpenProject = GisEditor.ProjectManager.CanSaveProject(new ProjectStreamInfo(GisEditor.ProjectManager.ProjectUri, null));
                        if (!canOpenProject) return;

                        GisEditor.ProjectManager.SetPassword();
                    });
                }
                return projectLockCommand;
            }
        }

        public static ObservedCommand OpenOptionsDialogCommand
        {
            get
            {
                if (openOptionManagerDialogCommand == null)
                {
                    openOptionManagerDialogCommand = new ObservedCommand(() =>
                    {
                        List<string> oldPluginDirectories = new List<string>();
                        oldPluginDirectories.AddRange(GisEditor.InfrastructureManager.PluginDirectories);

                        PreferenceWindow optionWindow = new PreferenceWindow();
                        if (optionWindow.ShowDialog().GetValueOrDefault())
                        {
                            List<string> newPluginDirectories = new List<string>();
                            newPluginDirectories.AddRange(GisEditor.InfrastructureManager.PluginDirectories);

                            if ((oldPluginDirectories.Count != 0
                                || newPluginDirectories.Count != 0)
                                && (oldPluginDirectories.Count != newPluginDirectories.Count
                                || !oldPluginDirectories.Any(d => newPluginDirectories.Contains(d))
                                || !newPluginDirectories.Any(d => oldPluginDirectories.Contains(d))))
                            {
                                foreach (PluginManager manager in GisEditor.InfrastructureManager.GetManagers().OfType<PluginManager>())
                                {
                                    UIPluginManager uiManager = manager as UIPluginManager;
                                    if (uiManager != null)
                                    {
                                        GeoCollection<UIPlugin> uiPlugins = new GeoCollection<UIPlugin>();
                                        uiManager.GetActiveUIPlugins().ForEach(p => uiPlugins.Add(p.Id, p));
                                        GisEditorUserControl.ApplyEnabledPlugins(new GeoCollection<UIPlugin>(), uiPlugins);
                                    }
                                    manager.UnloadPlugins();

                                    if (uiManager != null)
                                    {
                                        GeoCollection<UIPlugin> uiPlugins = new GeoCollection<UIPlugin>();
                                        uiManager.GetActiveUIPlugins().ForEach(p => uiPlugins.Add(p.Id, p));
                                        GisEditorUserControl.ApplyEnabledPlugins(uiPlugins, new GeoCollection<UIPlugin>());
                                    }
                                }
                            }

                            GeneralManager generalManager = GisEditorHelper.GetManagers().OfType<GeneralManager>().FirstOrDefault();
                            if (generalManager != null)
                            {
                                GisEditorUserControl.RemoveOrAddAutoSaveStackPanel(generalManager.IsDisplayAutoSave);
                            }
                        }
                    }, () => true);
                }
                return openOptionManagerDialogCommand;
            }
        }

        private static bool CheckCanSaveProject()
        {
            if (GisEditorUserControl != null)
            {
                return GisEditor.ProjectManager != null;
            }
            else return false;
        }

        private static ProjectPlugin GetFileProjectPlugin()
        {
            return GisEditor.ProjectManager.GetPlugins().OfType<ProjectPlugin>().FirstOrDefault(p => p.GetType().Name.Equals("FileProjectPlugin", StringComparison.Ordinal));
        }
    }
}