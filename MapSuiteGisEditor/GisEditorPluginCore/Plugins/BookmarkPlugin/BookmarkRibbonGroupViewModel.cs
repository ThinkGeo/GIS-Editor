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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BookmarkRibbonGroupViewModel : ViewModelBase
    {
        public const string OpenBookmarkMessage = "OpenBookmark";
        public const string AddBookmarkMessage = "AddBookmark";
        public const string DeleteBookmarkMessage = "DeleteMessage";
        public const string GotoBookmarkMessage = "GotoBookmark";

        private ObservedCommand addBookmarkCommand;
        private ObservedCommand showBookmarkManagerCommand;
        private ObservedCommand renameBookmarkCommand;
        private ObservedCommand deleteBookmarkCommand;
        private ObservedCommand gotoBookmarkCommand;
        private ObservedCommand refreshBookmarkCommand;
        private BookmarkViewModel selectedBookmark;
        private Dictionary<string, ObservableCollection<BookmarkViewModel>> bookmarks;

        public BookmarkRibbonGroupViewModel()
        {
            bookmarks = new Dictionary<string, ObservableCollection<BookmarkViewModel>>();
        }

        public ObservedCommand ShowBookmarkManagerCommand
        {
            get
            {
                if (showBookmarkManagerCommand == null)
                {
                    showBookmarkManagerCommand = new ObservedCommand(() =>
                    {
                        SyncBookmarkMenuItems();
                        MessengerInstance.Send(OpenBookmarkMessage, this);
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return showBookmarkManagerCommand;
            }
        }

        public ObservedCommand AddBookmarkCommand
        {
            get
            {
                if (addBookmarkCommand == null)
                {
                    addBookmarkCommand = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send(AddBookmarkMessage, this);
                        SyncBookmarkMenuItems();
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return addBookmarkCommand;
            }
        }

        public ObservedCommand RenameBookmarkCommand
        {
            get
            {
                if (renameBookmarkCommand == null)
                {
                    renameBookmarkCommand = new ObservedCommand(() =>
                    {
                        //MessengerInstance.Send(RenameBookmarkMessage, this);
                        selectedBookmark.IsRenaming = true;
                        SyncBookmarkMenuItems();
                    }, () => { return SelectedBookmark != null; });
                }
                return renameBookmarkCommand;
            }
        }

        public ObservedCommand DeleteBookmarkCommand
        {
            get
            {
                if (deleteBookmarkCommand == null)
                {
                    deleteBookmarkCommand = new ObservedCommand(() =>
                    {
                        Messenger.Default.Send(DeleteBookmarkMessage, this);
                        SyncBookmarkMenuItems();
                    }, () => { return SelectedBookmark != null; });
                }
                return deleteBookmarkCommand;
            }
        }

        public ObservedCommand GotoBookmarkCommand
        {
            get
            {
                if (gotoBookmarkCommand == null)
                {
                    gotoBookmarkCommand = new ObservedCommand(() =>
                    {
                        Messenger.Default.Send(GotoBookmarkMessage, this);
                    }, () => { return SelectedBookmark != null; });
                }
                return gotoBookmarkCommand;
            }
        }


        //public ObservedCommand RefreshBookmarkCommand
        //{
        //    get
        //    {
        //        if (refreshBookmarkCommand == null)
        //        {
        //            refreshBookmarkCommand = new ObservedCommand(() =>
        //            {
        //                SyncBookmarkMenuItems();
        //            }, CommandHelper.CheckMapIsNotNull);
        //        }
        //        return refreshBookmarkCommand;
        //    }
        //}

        public ObservableCollection<object> BookmarkMenus
        {
            get
            {
                ObservableCollection<object> items = new ObservableCollection<object>();
                if (GisEditor.ActiveMap != null && Bookmarks.ContainsKey(GisEditor.ActiveMap.Name))
                {
                    foreach (var bookmark in Bookmarks[GisEditor.ActiveMap.Name])
                    {
                        items.Add(bookmark);
                    }
                }

                items.Add(new RibbonSeparator());
                var menuItem = new RibbonMenuItem
                {
                    Header = GisEditor.LanguageManager.GetStringResource("BookmarkRibbonGroupAddLabel"),
                    ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/addbookmark.png", UriKind.RelativeOrAbsolute)),
                    Command = AddBookmarkCommand
                };
                items.Add(menuItem);
                return items;
            }
        }

        public Dictionary<string, ObservableCollection<BookmarkViewModel>> Bookmarks
        {
            get { return bookmarks; }
        }

        public ObservableCollection<BookmarkViewModel> DisaplayBookmarks
        {
            get
            {
                if (GisEditor.ActiveMap != null && bookmarks.ContainsKey(GisEditor.ActiveMap.Name))
                {
                    return bookmarks[GisEditor.ActiveMap.Name];
                }
                else return null;
            }
        }

        public BookmarkViewModel SelectedBookmark
        {
            get { return selectedBookmark; }
            set
            {
                selectedBookmark = value;
                RaisePropertyChanged(() => SelectedBookmark);
            }
        }

        public void SyncBookmarkMenuItems()
        {
            RaisePropertyChanged(() => BookmarkMenus);
            RaisePropertyChanged(() => DisaplayBookmarks);
        }

        public void SaveGlobalBookmarks()
        {
            XElement root = new XElement("GlobalBookmarks");

            foreach (var bookmark in bookmarks)
            {
                foreach (var mark in bookmark.Value)
                {
                    if (mark.IsGlobal)
                    {
                        XElement sub = new XElement("Bookmark");
                        sub.Add(new XElement("ActiveMap", bookmark.Key));
                        sub.Add(new XElement("Name", mark.Name));
                        sub.Add(new XElement("Scale", mark.Scale));
                        sub.Add(new XElement("DateCreated", mark.DateCreated));
                        sub.Add(new XElement("DateModified", mark.DateModified));
                        sub.Add(new XElement("InternalProj4Projection", mark.InternalProj4Projection));
                        sub.Add(new XElement("Center", mark.Center.ToString()));
                        root.Add(sub);
                    }
                }
            }

            root.Save(Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "GlobalBookmarks.xml"));
        }

        public void RestoreGlobalBookmarks()
        {
            var filePath = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "GlobalBookmarks.xml");

            if (File.Exists(filePath))
            {
                var allText = File.ReadAllText(filePath);
                var xml = XElement.Parse(allText);

                foreach (var element in xml.Elements("Bookmark"))
                {
                    var activeMapElement = element.Element("ActiveMap");
                    var nameElement = element.Element("Name");
                    var scaleStringElement = element.Element("Scale");
                    var internalProj4ProjectionElement = element.Element("InternalProj4Projection");
                    var centerElement = element.Element("Center");
                    var dateCreatedElement = element.Element("DateCreated");
                    var dateModifiedElement = element.Element("DateModified");

                    if (activeMapElement == null) continue;
                    if (nameElement == null) continue;
                    if (scaleStringElement == null) continue;
                    if (internalProj4ProjectionElement == null) continue;
                    if (centerElement == null) continue;
                    //if (dateCreatedElement == null) continue;
                    //if (dateModifiedElement == null) continue;

                    var mapName = activeMapElement.Value;
                    var name = nameElement.Value;
                    var scaleString = scaleStringElement.Value;
                    var internalProj4Projection = internalProj4ProjectionElement.Value;
                    var center = centerElement.Value;
                    //var dateCreatedString = dateCreatedElement.Value;
                    //var dateModifiedString = dateModifiedElement.Value;
                    if (!bookmarks.ContainsKey(mapName))
                    {
                        bookmarks.Add(mapName, new ObservableCollection<BookmarkViewModel>());
                    }

                    double scale = double.NaN;
                    double x = double.NaN;
                    double y = double.NaN;
                    DateTime dateCreated = DateTime.Now;
                    DateTime dateModified = DateTime.Now;

                    var centerInfo = center.Split(',');

                    if (!double.TryParse(scaleString, out scale)) continue;
                    if (!double.TryParse(centerInfo[0], out x)) continue;
                    if (!double.TryParse(centerInfo[1], out y)) continue;
                    if (dateCreatedElement != null)
                        DateTime.TryParse(dateCreatedElement.Value, out dateCreated);
                    if (dateModifiedElement != null)
                        DateTime.TryParse(dateModifiedElement.Value, out dateModified);

                    if (!bookmarks[mapName].Any(b => b.Name == name))
                    {
                        bookmarks[mapName].Add(new BookmarkViewModel(name, dateCreated)
                        {
                            Scale = scale,
                            Center = new PointShape(x, y),
                            InternalProj4Projection = internalProj4Projection,
                            DateModified = dateModified,
                            IsGlobal = true,
                            ImageUri = "/GisEditorPluginCore;component/Images/bookmark_global.png"
                        });
                    }
                }
            }
        }
    }
}