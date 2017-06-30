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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a data repository item.
    /// </summary>
    /// <remarks>
    /// The instance of this object is used to create a hierarchy object to represent the base map list, data folder etc.
    /// </remarks>
    [Serializable]
    public class DataRepositoryItem : ViewModelBase
    {
        private string name;
        private bool isExpanded;
        private bool isSelected;
        private bool isRenaming;
        private DataRepositoryPlugin sourcePlugin;
        private DataRepositoryItem parent;
        private UserControl content;
        private ObservableCollection<DataRepositoryItem> children;
        private Dictionary<string, object> customData;

        public event EventHandler<LoadingDataRepositoryItemEventArgs> Loading;
        public event EventHandler<LoadedDataRepositoryItemEventArgs> Loaded;
        public event EventHandler<CancelEventArgs> Refreshing;
        public event EventHandler<EventArgs> Refreshed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRepositoryItem" /> class.
        /// </summary>
        public DataRepositoryItem()
        {
            children = new ObservableCollection<DataRepositoryItem>();
            children.CollectionChanged += Children_CollectionChanged;
            customData = new Dictionary<string, object>();
            if (IsLoadable)
            {
                ContextMenu = new ContextMenu();
                MenuItem placeOnMapMenuItem = new MenuItem();
                placeOnMapMenuItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryItemPlaceMapHeader");
                placeOnMapMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/dr_place_on_map.png", UriKind.RelativeOrAbsolute)) };
                placeOnMapMenuItem.Command = new RelayCommand(Load);
                ContextMenu.Items.Add(placeOnMapMenuItem);
            }
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in children)
            {
                item.Parent = this;
            }

            RaisePropertyChanged(() => Children);
        }

        /// <summary>
        /// Gets or sets the name of data repository item instance.
        /// </summary>
        /// <value>
        /// The name of DataRepositoryItem.
        /// </value>
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        /// <summary>
        /// Gets or sets the icon of data repository item instance.
        /// </summary>
        /// <value>
        /// The icon of data repository item instance.
        /// </value>
        public ImageSource Icon { get; set; }

        /// <summary>
        /// Gets or sets the context menu of data repository item instance.
        /// </summary>
        /// <value>
        /// The context menu of data repository item instance.
        /// </value>
        public ContextMenu ContextMenu { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is allow to load onto the map.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is loadable; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoadable
        {
            get { return IsLoadableCore; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is allow to load onto the map.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is loadable; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool IsLoadableCore
        {
            get { return false; }
        }

        public bool IsLeaf
        {
            get { return IsLeafCore; }
        }

        protected virtual bool IsLeafCore
        {
            get { return false; }
        }

        public bool CanRename
        {
            get { return CanRenameCore; }
        }

        protected virtual bool CanRenameCore
        {
            get { return false; }
        }

        public bool CanRemove
        {
            get { return CanRemoveCore; }
        }

        protected virtual bool CanRemoveCore
        {
            get { return false; }
        }

        public string Category
        {
            get { return CategoryCore; }
        }

        protected virtual string CategoryCore { get { return string.Empty; } }

        public string Id
        {
            get { return IdCore; }
        }

        protected virtual string IdCore
        {
            get
            {
                string id = Name;
                if (parent != null)
                {
                    id = parent.Id + "/" + Name;
                }
                return id;
            }
        }

        public Dictionary<string, object> CustomData
        {
            get { return customData; }
        }

        public DataRepositoryItem Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public UserControl Content
        {
            get { return content; }
            set { content = value; }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                RaisePropertyChanged(() => IsExpanded);
                if (isExpanded)
                {
                    if (!GisEditor.DataRepositoryManager.ExpandedFolders.Contains(Id))
                        GisEditor.DataRepositoryManager.ExpandedFolders.Add(Id);
                }
                else
                {
                    if (GisEditor.DataRepositoryManager.ExpandedFolders.Contains(Id))
                        GisEditor.DataRepositoryManager.ExpandedFolders.Remove(Id);
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                RaisePropertyChanged(() => IsSelected);
                if (Id != null && isSelected && !Id.Equals("Data Folders") && !File.Exists(Id))
                {
                    GisEditor.DataRepositoryManager.CurrentSelectedItem = Id;
                }
            }
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                isRenaming = value;
                RaisePropertyChanged(() => IsRenaming);
            }
        }

        /// <summary>
        /// Gets the children of this data repository item.
        /// </summary>
        public ObservableCollection<DataRepositoryItem> Children
        {
            get { return children; }
        }

        public DataRepositoryPlugin SourcePlugin
        {
            get { return sourcePlugin; }
            set { sourcePlugin = value; }
        }

        public ObservableCollection<DataRepositoryItem> GetChildren()
        {
            return GetChildrenCore();
        }

        protected virtual ObservableCollection<DataRepositoryItem> GetChildrenCore()
        {
            return Children;
        }

        /// <summary>
        /// Loads current data repository onto active map.
        /// </summary>
        public void Load()
        {
            if (GisEditor.ActiveMap != null)
            {
                LoadingDataRepositoryItemEventArgs loadingArgs = new LoadingDataRepositoryItemEventArgs();
                OnLoading(loadingArgs);

                if (!loadingArgs.Cancel)
                {
                    LoadCore();

                    LoadedDataRepositoryItemEventArgs loadedArgs = new LoadedDataRepositoryItemEventArgs();
                    OnLoaded(loadedArgs);
                }
            }
        }

        /// <summary>
        /// Loads current data repository onto active map.
        /// </summary>
        protected virtual void LoadCore() { }

        /// <summary>
        /// Gets the search result from its children tree.
        /// </summary>
        /// <param name="keywords">The keywords.</param>
        /// <returns>A set of data repository that matches the keywords.</returns>
        public Collection<DataRepositoryItem> GetSearchResult(IEnumerable<string> keywords)
        {
            return GetSearchResultCore(keywords);
        }

        /// <summary>
        /// Gets the search result from its children tree.
        /// It is the core method for GetSearchResult(IEnumerable<string>) to override.
        /// </summary>
        /// <param name="keywords">The keywords.</param>
        /// <returns>A set of data repository that matches the keywords.</returns>
        protected virtual Collection<DataRepositoryItem> GetSearchResultCore(IEnumerable<string> keywords)
        {
            var result = new Collection<DataRepositoryItem>();
            foreach (var item in Children)
            {
                if (keywords.Any(keyWord => item.Name.IndexOf(keyWord, StringComparison.OrdinalIgnoreCase) != -1) && item.IsLeaf)
                {
                    result.Add(item);
                }

                foreach (var subItem in item.GetSearchResult(keywords))
                {
                    result.Add(subItem);
                }
            }

            return result;
        }

        public DataRepositoryItem GetRootDataRepositoryItem()
        {
            return Parent == null ? this : Parent.GetRootDataRepositoryItem();
        }

        public void Refresh()
        {
            CancelEventArgs e = new CancelEventArgs();
            OnRefreshing(e);
            if (e.Cancel) return;
            RefreshCore();
            OnRefreshed(new EventArgs());
        }

        protected virtual void RefreshCore()
        {
        }

        public bool Rename(string newName)
        {
            if (CanRename)
            {
                return RenameCore(newName);
            }
            return false;
        }

        protected virtual bool RenameCore(string newName)
        {
            return false;
        }

        protected virtual void OnLoading(LoadingDataRepositoryItemEventArgs e)
        {
            EventHandler<LoadingDataRepositoryItemEventArgs> handler = Loading;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLoaded(LoadedDataRepositoryItemEventArgs e)
        {
            EventHandler<LoadedDataRepositoryItemEventArgs> handler = Loaded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRefreshing(CancelEventArgs e)
        {
            EventHandler<CancelEventArgs> handler = Refreshing;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnRefreshed(EventArgs e)
        {
            EventHandler<EventArgs> handler = Refreshed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}