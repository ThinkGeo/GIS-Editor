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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal static class GisEditorHelper
    {
        private static readonly string windowLocationFormat = "{0},{1},{2},{3},{4}";
        private static readonly GeneralManager generalManager;
        private static readonly QuickAccessToolbarManager quickAccessToolbarManager;

        static GisEditorHelper()
        {
            generalManager = new GeneralManager();
            quickAccessToolbarManager = new QuickAccessToolbarManager();
        }

        public static QuickAccessToolbarManager QuickAccessToolbarManager
        {
            get { return GisEditorHelper.quickAccessToolbarManager; }
        }

        public static IEnumerable<Manager> GetManagers()
        {
            yield return generalManager;
            foreach (var mananger in GisEditor.InfrastructureManager.GetManagers())
            {
                yield return mananger;
            }
            yield return quickAccessToolbarManager;
        }

        //public static IEnumerable<T> GetSortedPlugins<T>(this PluginManager pluginManager) where T : Plugin
        //{
        //    return pluginManager.GetPlugins().OfType<T>().Where(p => p.IsActive).OrderBy(p => p.Index);
        //}

        public static void SaveRecentFiles(IEnumerable<RecentProjectModel> recentProjectFiles)
        {
            var recentFilesXElement = new XElement("Items");
            foreach (var item in recentProjectFiles)
            {
                if (item.FullPath != null)
                {
                    XElement itemXElement = new XElement("Item",
                        new XAttribute("Type", item.ProjectPluginType),
                        item.FullPath);
                    recentFilesXElement.Add(itemXElement);
                }
            }
            if (recentFilesXElement.HasElements)
            {
                generalManager.RecentProjectFiles = recentFilesXElement.ToString();
            }
        }

        public static string GetBackupProjectFolder()
        {
            return Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "BackupProject");
        }

        public static string GetLastOpenBackupProjectFolder()
        {
            return Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "OpenBackupProject");
        }

        public static string GetLastSavedBackupProjectFolder()
        {
            return Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "SaveBackupProject");
        }

        public static ObservableCollection<RecentProjectModel> GetRecentFileList()
        {
            ObservableCollection<RecentProjectModel> recentProjectFiles = new ObservableCollection<RecentProjectModel>();
            if (!string.IsNullOrEmpty(generalManager.RecentProjectFiles))
            {
                try
                {
                    var xElement = XElement.Parse(generalManager.RecentProjectFiles);
                    var plugintype = "FileProjectPlugin";
                    int index = 1;
                    foreach (var item in xElement.Descendants("Item"))
                    {
                        var attribute = item.Attribute("Type");
                        if (attribute != null)
                        {
                            plugintype = attribute.Value;
                        }
                        recentProjectFiles.Add(new RecentProjectModel(new Uri(item.Value), plugintype, index));
                        index++;
                    }
                }
                catch { }
            }
            return recentProjectFiles;
        }

        public static string SimplifyPath(string path, int maxLength)
        {
            if (path.Length > maxLength)
            {
                string[] stringSegments = path.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder();

                int fileNamePath = stringSegments[stringSegments.Length - 1].Length;
                if (fileNamePath > maxLength)
                {
                    sb.Append(stringSegments[stringSegments.Length - 1]);
                }
                else
                {
                    for (int i = 0; i < stringSegments.Length; i++)
                    {
                        if (sb.Length + stringSegments[i].Length < maxLength - fileNamePath - 4)
                        {
                            sb.AppendFormat(@"{0}\", stringSegments[i]);
                        }
                        else
                        {
                            sb.AppendFormat(@"...\{0}", stringSegments[stringSegments.Length - 1]);
                            break;
                        }
                    }
                }
                return sb.ToString();
            }
            else
            {
                return path;
            }
        }

        public static void SaveWindowLocation(Window shell)
        {
            generalManager.WindowLocation = string.Format(windowLocationFormat, shell.Left, shell.Top, shell.Width, shell.Height, shell.WindowState);
        }

        public static void RestoreWindowLocation(Window shell, Screen startScreen)
        {
            double top, left, width, height;
            WindowState windowState;
            if (!string.IsNullOrEmpty(generalManager.WindowLocation))
            {
                string[] windowLocationStates = generalManager.WindowLocation.Split(',');
                if (windowLocationStates.Length == 5
                    && double.TryParse(windowLocationStates[0], out left)
                    && double.TryParse(windowLocationStates[1], out top)
                    && double.TryParse(windowLocationStates[2], out width)
                    && double.TryParse(windowLocationStates[3], out height)
                    && Enum.TryParse<WindowState>(windowLocationStates[4], out windowState))
                {
                    if (windowState == WindowState.Maximized)
                    {
                        if (startScreen != null)
                        {
                            var workingArea = startScreen.WorkingArea;
                            shell.Left = workingArea.Left;
                            shell.Top = workingArea.Top;
                            shell.Width = workingArea.Width;
                            shell.Height = workingArea.Height;
                        }
                        shell.Loaded += Shell_Loaded;
                        //if (shell.IsLoaded)
                        //    shell.WindowState = windowState;
                    }
                    else
                    {
                        shell.Top = top;
                        shell.Left = left;
                        shell.Height = height;
                        shell.Width = width;
                        shell.WindowState = windowState;
                    }
                }
                else
                {
                    SetWindowBounds(shell);
                }
            }
            else
            {
                SetWindowBounds(shell);
            }
        }

        private static void Shell_Loaded(object sender, RoutedEventArgs e)
        {
            ((Window)sender).Loaded -= Shell_Loaded;
            ((Window)sender).WindowState = WindowState.Maximized;
        }

        internal static Collection<T> GetExportedPlugins<T>(string directory, string searchPattern = "*.dll")
        {
            Collection<T> plugins = new Collection<T>();
            DirectoryCatalog catalog = new DirectoryCatalog(directory, searchPattern);
            CompositionContainer container = new CompositionContainer(catalog);

            try
            {
                foreach (var plugin in container.GetExportedValues<T>())
                {
                    plugins.Add(plugin);
                }
            }
            finally
            {
                catalog.Dispose();
                container.Dispose();
            }

            return plugins;
        }

        private static void SetWindowBounds(Window shell)
        {
            Screen screen = Screen.PrimaryScreen;
            shell.Top = screen.Bounds.Top;
            shell.Left = screen.Bounds.Left;
            shell.Height = screen.WorkingArea.Height;
            shell.Width = screen.WorkingArea.Width;
        }
    }
}