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
using System.Reflection;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StyleCategoryViewModel : ViewModelBase
    {
        private string header;
        private string rootFolder;
        private bool isEnabled;
        private ObservableCollection<StyleFolderViewModel> folders;
        private StyleFolderViewModel selectedFolder;
        private ObservableCollection<StylePreviewViewModel> previews;

        public StyleCategoryViewModel(string rootFolder)
        {
            this.rootFolder = rootFolder;
            header = Path.GetFileName(rootFolder);
            folders = new ObservableCollection<StyleFolderViewModel>();
            folders.Add(new StyleFolderViewModel(rootFolder));
            previews = new ObservableCollection<StylePreviewViewModel>();
            isEnabled = true;
        }

        public string Header
        {
            get { return header; }
        }

        public string RootFolder
        {
            get { return rootFolder; }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                RaisePropertyChanged(()=>IsEnabled);
            }
        }

        public ObservableCollection<StyleFolderViewModel> Folders
        {
            get { return folders; }
        }

        public StyleFolderViewModel SelectedFolder
        {
            get { return selectedFolder; }
            set
            {
                selectedFolder = value;
                selectedFolder.IsSelected = true;
                RaisePropertyChanged(()=>SelectedFolder);
                previews.Clear();
                AddPreviews();
            }
        }

        public ObservableCollection<StylePreviewViewModel> Previews
        {
            get { return previews; }
        }

        public void AddPreviews()
        {
            if (Directory.Exists(selectedFolder.FolderPath))
            {
                foreach (var folder in Directory.GetDirectories(selectedFolder.FolderPath, "*", SearchOption.TopDirectoryOnly))
                {
                    previews.Add(new StylePreviewViewModel(folder));
                }
                foreach (var file in Directory.GetFiles(selectedFolder.FolderPath, "*.tgsty"))
                {
                    var stylePreviewVM = new StylePreviewViewModel(file);
                    if (stylePreviewVM.Style != null) previews.Add(stylePreviewVM);
                }
                var plugins = GisEditor.StyleManager.GetActiveStylePlugins().Where(p => p.IsDefault);
                foreach (var item in plugins)
                {
                    string path = Path.Combine(StyleLibraryViewModel.BaseFolder, item.StyleCategories.ToString() + " Styles");
                    if (selectedFolder.FolderPath.Equals(path, System.StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var style in item.StyleCandidates)
                        {
                            previews.Add(new StylePreviewViewModel(new CompositeStyle(style) { Name = style.Name }));
                        }
                    }
                }
            }
        }
    }
}