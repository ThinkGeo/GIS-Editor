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


using System.IO;
using System.Reflection;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for RestoreBackupWindow.xaml
    /// </summary>
    public partial class RestoreBackupWindow : Window
    {
        private string backupProjectFilePath;
        private string pluginType;

        private string[] autoSavedProjectFileContent;
        private string[] savedProjectFileContent;
        private string[] openProjectFileContent;

        private bool needOpen;

        public RestoreBackupWindow()
        {
            InitializeComponent();

            autoSavedProjectFileContent = GetBackupProjectFileContent(GisEditorHelper.GetBackupProjectFolder());
            savedProjectFileContent = GetBackupProjectFileContent(GisEditorHelper.GetLastSavedBackupProjectFolder());
            openProjectFileContent = GetBackupProjectFileContent(GisEditorHelper.GetLastOpenBackupProjectFolder());

            rbtnAutoSave.IsEnabled = !string.IsNullOrEmpty(autoSavedProjectFileContent[0]);
            rbtnSave.IsEnabled = !string.IsNullOrEmpty(savedProjectFileContent[0]);
            rbtnOpen.IsEnabled = !string.IsNullOrEmpty(openProjectFileContent[0]);

            needOpen = rbtnAutoSave.IsEnabled || rbtnSave.IsEnabled || rbtnOpen.IsEnabled;
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (rbtnAutoSave.IsEnabled) rbtnAutoSave.IsChecked = true;
            else if (rbtnSave.IsEnabled) rbtnSave.IsChecked = true;
            else if (rbtnOpen.IsEnabled) rbtnOpen.IsChecked = true;
        }

        [Obfuscation]
        private void rbtnOpen_Checked(object sender, RoutedEventArgs e)
        {
            pluginType = openProjectFileContent[0];
            backupProjectFilePath = openProjectFileContent[1];
        }

        [Obfuscation]
        private void rbtnSave_Checked(object sender, RoutedEventArgs e)
        {
            pluginType = savedProjectFileContent[0];
            backupProjectFilePath = savedProjectFileContent[1];
        }

        [Obfuscation]
        private void rbtnAutoSave_Checked(object sender, RoutedEventArgs e)
        {
            pluginType = autoSavedProjectFileContent[0];
            backupProjectFilePath = autoSavedProjectFileContent[1];
        }

        [Obfuscation]
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private string[] GetBackupProjectFileContent(string backupProjectFolder)
        {
            string[] resultContent = new string[2];

            if (Directory.Exists(backupProjectFolder))
            {
                string[] files = Directory.GetFiles(backupProjectFolder, "*.tgproj.txt");

                string pluginType = string.Empty;

                if (files.Length > 0)
                {
                    string[] contents = File.ReadAllLines(files[0]);

                    if (contents.Length == 2)
                    {
                        string projectFileName = contents[1];
                        string directory = Path.GetDirectoryName(files[0]);

                        resultContent[0] = contents[0];
                        resultContent[1] = Path.Combine(directory, Path.ChangeExtension(projectFileName, ".tgproj"));
                    }
                }
            }

            return resultContent;
        }

        public string BackupProjectFilePath
        {
            get { return backupProjectFilePath; }
        }

        public string PluginType
        {
            get { return pluginType; }
        }

        public bool NeedOpen
        {
            get { return needOpen; }
        }

    }
}
