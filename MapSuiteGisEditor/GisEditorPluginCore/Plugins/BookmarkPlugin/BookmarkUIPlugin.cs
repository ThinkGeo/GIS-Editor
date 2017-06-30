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
using System.Globalization;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BookmarkUIPlugin : UIPlugin
    {
        [NonSerialized]
        private BookmarkRibbonGroup bookmarkGroup;
        private BookmarkRibbonGroupViewModel viewModel;
        private GisEditorWpfMap lastMap;
        private DockWindow bookMarkDockWindow;
        private RibbonEntry bookmarkEntry;

        public BookmarkUIPlugin()
        {
            viewModel = new BookmarkRibbonGroupViewModel();
            Description = GisEditor.LanguageManager.GetStringResource("ManageBookmarksPluginDescreption");
            Index = UIPluginOrder.BookmarkPlugin;
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/bookmarks.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/bookmarks.png", UriKind.RelativeOrAbsolute));

            bookmarkGroup = new BookmarkRibbonGroup();
            bookmarkGroup.DataContext = viewModel;

            bookmarkEntry = new RibbonEntry();
            bookmarkEntry.RibbonGroup = bookmarkGroup;
            bookmarkEntry.RibbonTabName = "HomeRibbonTabHeader";
            bookmarkEntry.RibbonTabIndex = RibbonTabOrder.Home;

            bookMarkDockWindow = new DockWindow();
            bookMarkDockWindow.Content = new BookmarkListUserControl() { DataContext = viewModel };
            bookMarkDockWindow.Title = "BookmarksPluginTitle";
            bookMarkDockWindow.Name = "Bookmarks";
            bookMarkDockWindow.StartupMode = DockWindowStartupMode.Hide;
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            bookmarkGroup.RegisterMessenger(viewModel);

            if (!RibbonEntries.Contains(bookmarkEntry))
            {
                RibbonEntries.Add(bookmarkEntry);
            }

            if (!DockWindows.Contains(bookMarkDockWindow))
            {
                DockWindows.Add(bookMarkDockWindow);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            if (bookmarkGroup != null) bookmarkGroup.UnRegisterMessenger();
            RibbonEntries.Clear();
            DockWindows.Clear();
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            base.RefreshCore(currentMap, refreshArgs);
            if (lastMap != currentMap && bookmarkGroup != null)
            {
                ((BookmarkRibbonGroupViewModel)bookmarkGroup.DataContext).SyncBookmarkMenuItems();
            }

            lastMap = currentMap;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var bookmark in viewModel.Bookmarks)
            {
                foreach (var tmpBookmark in bookmark.Value)
                {
                    if (tmpBookmark.IsGlobal) continue;
                    string bookmarkKey = String.Format(CultureInfo.InvariantCulture, "{0}||{1}", bookmark.Key, tmpBookmark.Name);
                    string bookmarkValue = String.Format(CultureInfo.InvariantCulture
                        , "{0}||{1}||{2}||{3}||{4}||{5}"
                        , tmpBookmark.Scale
                        , tmpBookmark.Center.X
                        , tmpBookmark.Center.Y
                        , tmpBookmark.InternalProj4Projection, tmpBookmark.DateCreated, tmpBookmark.DateModified);
                    settings.ProjectSettings.Add(bookmarkKey, bookmarkValue);
                }
            }
            viewModel.SaveGlobalBookmarks();
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.Count > 0)
            {
                viewModel.Bookmarks.Clear();
                foreach (var setting in settings.ProjectSettings)
                {
                    string[] bookmarkKey = setting.Key.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    if (bookmarkKey.Length == 2)
                    {
                        string mapName = bookmarkKey[0];
                        string bookmarkName = bookmarkKey[1];
                        if (!viewModel.Bookmarks.ContainsKey(mapName))
                        {
                            viewModel.Bookmarks.Add(mapName, new ObservableCollection<BookmarkViewModel>());
                        }

                        var posInfo = setting.Value.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                        if (posInfo.Length == 6)
                        {
                            double scale = double.NaN;
                            double x = double.NaN;
                            double y = double.NaN;
                            DateTime dateCreated = DateTime.MinValue;
                            DateTime dateModified = DateTime.MinValue;

                            if (!double.TryParse(posInfo[0], out scale)) continue;
                            if (!double.TryParse(posInfo[1], out x)) continue;
                            if (!double.TryParse(posInfo[2], out y)) continue;
                            if (!DateTime.TryParse(posInfo[4], out dateCreated)) continue;
                            if (!DateTime.TryParse(posInfo[5], out dateModified)) continue;

                            viewModel.Bookmarks[mapName].Add(new BookmarkViewModel(bookmarkName,dateCreated)
                            {
                                Scale = scale,
                                Center = new PointShape(x, y),
                                InternalProj4Projection = posInfo[3],
                                DateModified = dateModified,
                                ImageUri = "/GisEditorPluginCore;component/Images/bookmark project.png",
                                IsGlobal = false
                            });
                        }
                    }
                }
            }

            viewModel.RestoreGlobalBookmarks();
            viewModel.SyncBookmarkMenuItems();
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            viewModel.Bookmarks.Clear();
            viewModel.SyncBookmarkMenuItems();
        }
    }
}