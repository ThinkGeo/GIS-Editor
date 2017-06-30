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
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class BookmarkRibbonGroup : RibbonGroup
    {
        public BookmarkRibbonGroup()
        {
            InitializeComponent();
        }

        public void RegisterMessenger(BookmarkRibbonGroupViewModel viewModel)
        {
            Messenger.Default.Register<string>(this, viewModel, msg =>
            {
                switch (msg)
                {
                    case BookmarkRibbonGroupViewModel.AddBookmarkMessage:
                        if (!viewModel.Bookmarks.ContainsKey(GisEditor.ActiveMap.Name))
                        {
                            viewModel.Bookmarks.Add(GisEditor.ActiveMap.Name, new ObservableCollection<BookmarkViewModel>());
                        }

                        var displayBookmarks = viewModel.DisaplayBookmarks;
                        BookmarkNamePromptWindow bookmarkNameWindow = new BookmarkNamePromptWindow(null, viewModel.Bookmarks[GisEditor.ActiveMap.Name].Select(tmpBookmark => tmpBookmark.Name));
                        if (bookmarkNameWindow.ShowDialog().GetValueOrDefault())
                        {
                            BookmarkViewModel bookmark = new BookmarkViewModel();
                            bookmark.Name = bookmarkNameWindow.BookmarkName;
                            bookmark.IsGlobal = bookmarkNameWindow.IsGlobal;
                            if (bookmark.IsGlobal)
                            {
                                bookmark.ImageUri = "/GisEditorPluginCore;component/Images/bookmark_global.png";
                            }
                            else
                            {
                                bookmark.ImageUri = "/GisEditorPluginCore;component/Images/bookmark project.png";
                            }
                            bookmark.Center = GisEditor.ActiveMap.CurrentExtent.GetCenterPoint();
                            bookmark.Scale = GisEditor.ActiveMap.CurrentScale;
                            bookmark.InternalProj4Projection = GisEditor.ActiveMap.DisplayProjectionParameters;
                            displayBookmarks.Add(bookmark);
                            if (bookmark.IsGlobal) viewModel.SaveGlobalBookmarks();
                            viewModel.SyncBookmarkMenuItems();
                        }
                        break;
                    case BookmarkRibbonGroupViewModel.OpenBookmarkMessage:
                        var dockWindow = GisEditor.DockWindowManager.DockWindows.FirstOrDefault(d => d.Title.Equals("BookmarksPluginTitle", StringComparison.Ordinal));
                        if (dockWindow != null)
                        {
                            dockWindow.Show(DockWindowPosition.Right);
                        }
                        break;
                    case BookmarkRibbonGroupViewModel.DeleteBookmarkMessage:
                        if (viewModel.DisaplayBookmarks != null && viewModel.DisaplayBookmarks.Contains(viewModel.SelectedBookmark))
                        {
                            viewModel.DisaplayBookmarks.Remove(viewModel.SelectedBookmark);
                            viewModel.SyncBookmarkMenuItems();
                        }
                        break;

                    case BookmarkRibbonGroupViewModel.GotoBookmarkMessage:
                        if (viewModel.DisaplayBookmarks != null && viewModel.DisaplayBookmarks.Contains(viewModel.SelectedBookmark))
                        {
                            var targetCenter = viewModel.SelectedBookmark.Center;
                            if (!viewModel.SelectedBookmark.InternalProj4Projection.Equals(GisEditor.ActiveMap.DisplayProjectionParameters))
                            {
                                Proj4Projection projection = new Proj4Projection();
                                projection.InternalProjectionParametersString = viewModel.SelectedBookmark.InternalProj4Projection;
                                projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                                projection.Open();
                                targetCenter = (PointShape)projection.ConvertToExternalProjection(targetCenter);
                                projection.Close();
                            }
                            GisEditor.ActiveMap.ZoomTo(targetCenter, viewModel.SelectedBookmark.Scale);
                        }
                        break;
                }
            });
        }

        public void UnRegisterMessenger()
        {
            Messenger.Default.Unregister(this);
        }

        [Obfuscation]
        private void BookmarkList_DropDownOpened(object sender, EventArgs e)
        {
            ((BookmarkRibbonGroupViewModel)DataContext).SyncBookmarkMenuItems();

            bookmarkList.Items.Clear();
            if (GisEditor.ActiveMap != null && ((BookmarkRibbonGroupViewModel)DataContext).Bookmarks.ContainsKey(GisEditor.ActiveMap.Name))
            {
                foreach (var bookmark in ((BookmarkRibbonGroupViewModel)DataContext).Bookmarks[GisEditor.ActiveMap.Name])
                {
                    bookmarkList.Items.Add(bookmark);
                }
            }

            bookmarkList.Items.Add(new RibbonSeparator());
            var menuItem = new RibbonMenuItem
            {
                Header = GisEditor.LanguageManager.GetStringResource("BookmarkRibbonGroupAddLabel"),
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/addbookmark.png", UriKind.RelativeOrAbsolute)),
                Command = ((BookmarkRibbonGroupViewModel)DataContext).AddBookmarkCommand
            };
            bookmarkList.Items.Add(menuItem);
        }
    }
}