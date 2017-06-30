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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for StyleLibraryWindow.xaml
    /// </summary>
    public partial class StyleLibraryWindow : Window
    {

        private StyleLibraryViewModel viewModel;
        private StyleBuilderResult styleBuilderResult;

        public StyleLibraryWindow()
        {
            InitializeComponent();
            viewModel = new StyleLibraryViewModel();
            DataContext = viewModel;
        }

        public StyleBuilderResult Result
        {
            get { return styleBuilderResult; }
        }

        [Obfuscation]
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var styleFolder = e.NewValue as StyleFolderViewModel;
            if (styleFolder != null)
            {
                if (!viewModel.SearchKey.Equals(StyleLibraryViewModel.SearchHint))
                {
                    viewModel.SearchKey = StyleLibraryViewModel.SearchHint;
                }
                viewModel.SelectedStyleCategory.SelectedFolder = styleFolder;
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            styleBuilderResult = new StyleBuilderResult(null, true);
            DialogResult = false;
        }

        [Obfuscation]
        private void OkClick(object sender, RoutedEventArgs e)
        {
            styleBuilderResult = new StyleBuilderResult(viewModel.SelectedStylePreview.Style, false);
            styleBuilderResult.ToZoomLevelIndex = GetClosestZoomLevelIndex(viewModel.SelectedStylePreview.LowerScale);
            styleBuilderResult.FromZoomLevelIndex = GetClosestZoomLevelIndex(viewModel.SelectedStylePreview.UpperScale);
            DialogResult = true;
        }

        [Obfuscation]
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (viewModel.SearchKey.Equals(StyleLibraryViewModel.SearchHint))
            {
                viewModel.SearchKey = "";
            }
        }

        [Obfuscation]
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.SearchKey))
            {
                viewModel.SearchKey = StyleLibraryViewModel.SearchHint;
            }
        }

        [Obfuscation]
        private void StylePreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var stylePreviewViewModel = sender.GetDataContext<StylePreviewViewModel>();
            if (stylePreviewViewModel != null)
            {
                if (stylePreviewViewModel.IsStyle)
                {
                    styleBuilderResult = new StyleBuilderResult(stylePreviewViewModel.Style, false);
                    styleBuilderResult.ToZoomLevelIndex = GetClosestZoomLevelIndex(stylePreviewViewModel.LowerScale);
                    styleBuilderResult.FromZoomLevelIndex = GetClosestZoomLevelIndex(stylePreviewViewModel.UpperScale);
                    DialogResult = true;
                }
                else
                {
                    var targetFolder = viewModel.SelectedStyleCategory.SelectedFolder.Folders.FirstOrDefault(f => f.FolderPath.Equals(stylePreviewViewModel.StyleFilePath));
                    if (targetFolder != null)
                    {
                        viewModel.SelectedStyleCategory.SelectedFolder.IsExpanded = true;
                        targetFolder.IsSelected = true;
                    }
                }
            }
        }

        [Obfuscation]
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string uri = GisEditor.LanguageManager.GetStringResource("StyleLibraryHelp");
            Process.Start(uri);
        }

        private int GetClosestZoomLevelIndex(double scale)
        {
            ZoomLevelSet zoomLevelSet = GisEditor.ActiveMap.ZoomLevelSet;
            var zoomLevel = zoomLevelSet.CustomZoomLevels.OrderBy(z => Math.Abs(z.Scale - scale)).FirstOrDefault();
            return zoomLevelSet.CustomZoomLevels.IndexOf(zoomLevel) + 1;
        }
    }
}