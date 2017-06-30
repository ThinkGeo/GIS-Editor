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


using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ManageBookmarksUserControl.xaml
    /// </summary>
    public partial class BookmarkListUserControl : UserControl
    {
        private BookmarkRibbonGroupViewModel viewModel;
        private bool isDesending;

        public BookmarkListUserControl()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(BookmarkListUserControl_Loaded);

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("BookmarksHelp", HelpButtonMode.NormalButton);
        }

        private void BookmarkListUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = this.DataContext as BookmarkRibbonGroupViewModel;
            viewModel.SyncBookmarkMenuItems();
        }

        [Obfuscation]
        private void RenameTextBlock_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            var viewModel = DataContext as BookmarkRibbonGroupViewModel;
            var existingNames = viewModel.DisaplayBookmarks.Select(b => b.Name).ToList();
            existingNames.Remove(viewModel.SelectedBookmark.Name);
            if (BookmarkNamePromptViewModel.ValidateBookmarkName(e.NewText, existingNames))
            {
                if (!viewModel.SelectedBookmark.Name.Equals(e.NewText))
                {
                    viewModel.SelectedBookmark.Name = e.NewText;
                }
            }
            else
            {
                e.IsCancelled = true;
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("BookmarkListUserControlNameIllegalText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"));
            }
        }

        [Obfuscation]
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader)
            {
                //Get clicked column
                GridViewColumn clickedColumn = (e.OriginalSource as GridViewColumnHeader).Column;
                if (clickedColumn != null)
                {
                    BookmarkViewModel[] globalBookmarks = viewModel.DisaplayBookmarks.Where(b => b.IsGlobal).ToArray();
                    BookmarkViewModel[] projectBookmarks = viewModel.DisaplayBookmarks.Where(b => !b.IsGlobal).ToArray();
                    switch (clickedColumn.Header.ToString())
                    {
                        case "Name":
                        default:
                            globalBookmarks = !isDesending ? globalBookmarks.OrderBy(v => v.Name).ToArray() : globalBookmarks.OrderByDescending(b => b.Name).ToArray();
                            projectBookmarks = !isDesending ? projectBookmarks.OrderBy(v => v.Name).ToArray() : projectBookmarks.OrderByDescending(b => b.Name).ToArray();
                            break;
                        case "Date Created":
                            globalBookmarks = !isDesending ? globalBookmarks.OrderBy(v => v.DateCreated).ToArray() : globalBookmarks.OrderByDescending(b => b.DateCreated).ToArray();
                            projectBookmarks = !isDesending ? projectBookmarks.OrderBy(v => v.DateCreated).ToArray() : projectBookmarks.OrderByDescending(b => b.DateCreated).ToArray();
                            break;
                        case "Date Modified":
                            globalBookmarks = !isDesending ? globalBookmarks.OrderBy(v => v.DateModified).ToArray() : globalBookmarks.OrderByDescending(b => b.DateModified).ToArray();
                            projectBookmarks = !isDesending ? projectBookmarks.OrderBy(v => v.DateModified).ToArray() : projectBookmarks.OrderByDescending(b => b.DateModified).ToArray();
                            break;
                    }
                    viewModel.DisaplayBookmarks.Clear();
                    if (isDesending)
                    {
                        foreach (var item in projectBookmarks.Concat(globalBookmarks))
                        {
                            viewModel.DisaplayBookmarks.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in globalBookmarks.Concat(projectBookmarks))
                        {
                            viewModel.DisaplayBookmarks.Add(item);
                        }
                    }

                    isDesending = !isDesending;
                }
            }
        }
    }
}