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
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class FolderHelper
    {
        private readonly static string gisFolderName = "Map Suite Gis Editor";
        private static string gisFolderPath;
        private static string lastSelectedFolder;
        private static string entryPath;

        public static string LastSelectedFolder
        {
            get { return lastSelectedFolder; }
        }

        public static void OpenFolderBrowserDialog(Action<FolderBrowserDialog, DialogResult> openedAction, Action<FolderBrowserDialog> initializeAction = null)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(lastSelectedFolder))
                {
                    dialog.SelectedPath = lastSelectedFolder;
                }
                if (initializeAction != null) initializeAction(dialog);
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK || result == DialogResult.Yes)
                {
                    lastSelectedFolder = dialog.SelectedPath;
                }
                if (openedAction != null) openedAction(dialog, result);
            }
        }

        public static string GetEntryPath()
        {
            if (string.IsNullOrEmpty(entryPath))
            {
#if GISEditorUnitTest
            entryPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
#else
                entryPath = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
#endif
            }
            return entryPath;
        }

        public static string GetGisEditorFolder()
        {
            if (string.IsNullOrEmpty(gisFolderPath))
            {
                string documentFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                gisFolderPath = Path.Combine(documentFolder, gisFolderName);
            }
            if (!Directory.Exists(gisFolderPath)) Directory.CreateDirectory(gisFolderPath);
            return gisFolderPath;
        }

        public static string GetCurrentProjectTaskResultFolder()
        {
            string tempFolder = GisEditor.InfrastructureManager.TemporaryPath;
            string tempProjectFolder = Path.Combine(tempFolder, "TaskResults", GisEditor.ProjectManager.Id);

            if (!Directory.Exists(tempProjectFolder))
                Directory.CreateDirectory(tempProjectFolder);

            return tempProjectFolder;
        }

        public static string GetWizardTempFileName(string layerName, string wizardName)
        {
            string name = string.Empty;
            if (wizardName == "Grid")
            {
                name = layerName + "_Grid_";
            }
            else if (wizardName == "Blend")
            {
                name = "BlendResult_";
            }
            else if (wizardName == "Merge")
            {
                name = "MergeResult_";
            }
            else
            {
                name = layerName + "_" + wizardName + "_";
            }

            string tempProjectFolder = GetCurrentProjectTaskResultFolder();
            int index = 1;

            while (File.Exists(Path.Combine(tempProjectFolder, name + "_" + index.ToString() + ".shp")) || File.Exists(Path.Combine(tempProjectFolder, name + "_" + index.ToString() + ".grd")))
            {
                index++;
            }

            return name += "_" + index.ToString();
        }
    }
}
