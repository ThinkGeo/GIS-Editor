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
using System.Reflection;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StyleFolderViewModel : ViewModelBase
    {
        private string folderPath;
        private string folderName;
        private bool isSelected;
        private bool isExpanded;
        private ObservableCollection<StyleFolderViewModel> folders;

        public StyleFolderViewModel(string folderPath)
        {
            this.folderPath = folderPath;
            folderName = Path.GetFileName(folderPath);
            folders = new ObservableCollection<StyleFolderViewModel>();

            if (Directory.Exists(folderPath))
            {
                foreach (var item in Directory.GetDirectories(folderPath))
                {
                    folders.Add(new StyleFolderViewModel(item));
                }
            }
        }

        public string FolderPath
        {
            get { return folderPath; }
        }

        public string FolderName
        {
            get { return folderName; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                RaisePropertyChanged(()=>IsSelected);
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                RaisePropertyChanged(()=>IsExpanded);
            }
        }

        public ObservableCollection<StyleFolderViewModel> Folders
        {
            get { return folders; }
        }
    }
}