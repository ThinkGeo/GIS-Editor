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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class InfrastructureSettingViewModel : ViewModelBase
    {
        private string selectDirectory;
        private FolderBrowserDialog dialog;
        private RelayCommand addDirectoryCommand;
        private ObservedCommand removeDirectoryCommand;
        private ObservableCollection<string> pluginDirectories;

        public InfrastructureSettingViewModel()
        {
            pluginDirectories = new ObservableCollection<string>();
            addDirectoryCommand = new RelayCommand(() =>
            {
                if (dialog == null)
                {
                    dialog = new FolderBrowserDialog();
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (PluginDirectories.Any(d => d.Equals(dialog.SelectedPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DirectoryExistsSelectAnotherLabel"), GisEditor.LanguageManager.GetStringResource("DirectoryExistsLabel"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        PluginDirectories.Add(dialog.SelectedPath);
                    }
                }
            });

            removeDirectoryCommand = new ObservedCommand(() =>
            {
                if (!String.IsNullOrEmpty(SelectedDirectory) && PluginDirectories.Contains(SelectedDirectory))
                {
                    int selectedIndex = PluginDirectories.IndexOf(SelectedDirectory);
                    PluginDirectories.Remove(SelectedDirectory);
                    if (PluginDirectories.Count > selectedIndex)
                    {
                        SelectedDirectory = PluginDirectories[selectedIndex];
                    }
                }
            }, () => !String.IsNullOrEmpty(SelectedDirectory));
        }

        public string SelectedDirectory
        {
            get { return selectDirectory; }
            set
            {
                selectDirectory = value;
                RaisePropertyChanged(()=>SelectedDirectory);
            }
        }

        public ObservableCollection<string> PluginDirectories { get { return pluginDirectories; } }

        public RelayCommand AddDirectoryCommand
        {
            get { return addDirectoryCommand; }
        }

        public ObservedCommand RemoveDirectoryCommand
        {
            get { return removeDirectoryCommand; }
        }
    }
}