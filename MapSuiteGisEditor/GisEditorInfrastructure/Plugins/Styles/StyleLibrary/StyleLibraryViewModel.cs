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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StyleLibraryViewModel : ViewModelBase
    {
        public static readonly string SearchHint = GisEditor.LanguageManager.GetStringResource("SearchFolderDescription");
        public static string BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Map Suite Gis Editor", "StyleLibrary");

        private static readonly SolidColorBrush grayBrush = new SolidColorBrush(Colors.LightGray);
        private static readonly SolidColorBrush blackBrush = new SolidColorBrush(Colors.Black);

        private string searchKey;
        private Brush textColor;
        private Collection<Style> resultStyles;
        private ObservableCollection<StyleCategoryViewModel> styleCategories;
        private StyleCategoryViewModel selectedStyleCategory;
        private ObservedCommand addFolderCommand;
        private ObservedCommand refreshCommand;
        private static RelayCommand<string> showInExplorerCommand;
        private StylePreviewViewModel selectedStylePreview;

        public StyleLibraryViewModel()
        {
            searchKey = SearchHint;
            textColor = grayBrush;

            resultStyles = new Collection<Style>();
            if (!Directory.Exists(BaseFolder)) Directory.CreateDirectory(BaseFolder);
            styleCategories = new ObservableCollection<StyleCategoryViewModel>();

            selectedStyleCategory = new StyleCategoryViewModel(BaseFolder);
            foreach (var folder in GisEditor.StyleManager.Folders)
            {
                try
                {
                    if (!folder.Contains(BaseFolder)) selectedStyleCategory.Folders[0].Folders.Add(new StyleFolderViewModel(folder));
                }
                catch (Exception)
                { }
            }
            selectedStyleCategory.Folders[0].IsExpanded = true;
            if (selectedStyleCategory.Folders[0].Folders.Count > 0) selectedStyleCategory.Folders[0].Folders.First().IsSelected = true;
        }

        public static string StyleLibraryFolder
        {
            get { return BaseFolder; }
        }

        public string SearchKey
        {
            get { return searchKey; }
            set
            {
                searchKey = value;
                RaisePropertyChanged(() => SearchKey);

                if (searchKey.Equals(SearchHint)) TextColor = grayBrush;
                else
                {
                    Search();
                    if (TextColor != blackBrush) TextColor = blackBrush;
                }
            }
        }

        public Brush TextColor
        {
            get { return textColor; }
            set
            {
                textColor = value;
                RaisePropertyChanged(() => TextColor);
            }
        }

        public StylePreviewViewModel SelectedStylePreview
        {
            get { return selectedStylePreview; }
            set
            {
                selectedStylePreview = value;
                RaisePropertyChanged(() => SelectedStylePreview);
                RaisePropertyChanged(() => IsOKButtonEnabled);
            }
        }

        public ObservableCollection<StyleCategoryViewModel> StyleCategories
        {
            get { return styleCategories; }
        }

        public StyleCategoryViewModel SelectedStyleCategory
        {
            get { return selectedStyleCategory; }
            set
            {
                selectedStyleCategory = value;
                RaisePropertyChanged(() => SelectedStyleCategory);
            }
        }

        public bool IsOKButtonEnabled
        {
            get { return SelectedStylePreview != null; }
        }

        public ObservedCommand AddFolderCommand
        {
            get
            {
                if (addFolderCommand == null)
                {
                    addFolderCommand = new ObservedCommand(() =>
                    {
                        FolderBrowserDialogHelper.OpenDialog((tmpDialog, tmpResult) =>
                        {
                            if (tmpResult == System.Windows.Forms.DialogResult.OK)
                            {
                                try
                                {
                                    selectedStyleCategory.Folders[0].Folders.Add(new StyleFolderViewModel(tmpDialog.SelectedPath));
                                    if (!GisEditor.StyleManager.Folders.Contains(tmpDialog.SelectedPath))
                                        GisEditor.StyleManager.Folders.Add(tmpDialog.SelectedPath);
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.Forms.MessageBox.Show(ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                                }
                            }
                        });
                    }, () =>
                    {
                        if (selectedStyleCategory != null) return selectedStyleCategory.Folders[0] == selectedStyleCategory.SelectedFolder;
                        else return false;
                    });
                }
                return addFolderCommand;
            }
        }

        public ObservedCommand RefreshCommand
        {
            get
            {
                if (refreshCommand == null)
                {
                    refreshCommand = new ObservedCommand(() =>
                    {
                        resultStyles.Clear();
                        selectedStyleCategory.SelectedFolder.Folders.Clear();
                        selectedStyleCategory.Previews.Clear();
                        if (Directory.Exists(selectedStyleCategory.SelectedFolder.FolderPath))
                        {
                            foreach (var folder in Directory.GetDirectories(selectedStyleCategory.SelectedFolder.FolderPath))
                            {
                                selectedStyleCategory.SelectedFolder.Folders.Add(new StyleFolderViewModel(folder));
                            }
                            selectedStyleCategory.AddPreviews();
                        }
                        else { System.Windows.MessageBox.Show(selectedStyleCategory.SelectedFolder.FolderPath + " " + GisEditor.LanguageManager.GetStringResource("DataRepositoryDoesntexistLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryNotExistedLabel")); }
                    }, () => selectedStyleCategory != null && selectedStyleCategory.SelectedFolder != null);
                }
                return refreshCommand;
            }
        }

        public static RelayCommand<string> ShowInExplorerCommand
        {
            get
            {
                if (showInExplorerCommand == null)
                {
                    showInExplorerCommand = new RelayCommand<string>(tmpFolder =>
                    {
                        ProcessUtils.OpenPath(tmpFolder);
                    });
                }
                return showInExplorerCommand;
            }
        }

        private void Search()
        {
            if (selectedStyleCategory != null)
            {
                selectedStyleCategory.Previews.Clear();
                var searchKeyUpper = searchKey.ToUpperInvariant();
                foreach (var file in Directory.GetFiles(selectedStyleCategory.SelectedFolder.FolderPath, "*.tgsty", SearchOption.AllDirectories))
                {
                    var tmpFileNameUpper = Path.GetFileName(file).ToUpperInvariant();
                    if (tmpFileNameUpper.Contains(searchKeyUpper))
                        selectedStyleCategory.Previews.Add(new StylePreviewViewModel(file));
                }
            }
        }
    }
}