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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    public class DataRepositoryContentViewModel : ViewModelBase
    {
        private static DataRepositoryItem selectedViewModel;
        private static DataRepositoryContentViewModel current;
        private static string selectedItem = "";
        private StackPanel contextMenuStackPanel;

        private string searchText;
        private Visibility searchResultVisibility;
        private Visibility contentVisibility;
        private ObservableCollection<DataRepositoryItem> children;
        private DataRepositoryItem searchResult;
        private DataRepositoryItem currentPluginItemViewModel;
        private ICommand addDataCommand;
        private ICommand placeOnMapCommand;
        private ObservedCommand removeCommand;

        [NonSerialized]
        private ListBox searchContent;
        [NonSerialized]
        private RelayCommand refreshCommand;
        [NonSerialized]
        private RelayCommand searchCommand;

        public DataRepositoryContentViewModel()
        {
            searchResult = new DataRepositoryItem();
            children = new ObservableCollection<DataRepositoryItem>();
            searchText = string.Empty;

            searchResultVisibility = Visibility.Collapsed;
            var dataPlugins = GisEditor.DataRepositoryManager.GetActiveDataRepositoryPlugins<DataRepositoryPlugin>();

            foreach (var dataPlugin in dataPlugins)
            {
                Children.Add(dataPlugin.RootDataRepositoryItem);
            }
            if (Children.Count > 0)
            {
                Children[0].IsSelected = true;
            }
        }

        public static DataRepositoryContentViewModel Current
        {
            get
            {
                if (current == null) current = new DataRepositoryContentViewModel();
                return current;
            }
        }

        public static DataRepositoryItem SelectedDataRepositoryItem
        {
            get { return selectedViewModel; }
            set
            {
                selectedViewModel = value;
            }
        }

        public StackPanel ContextMenuStackPanel
        {
            get
            {
                return contextMenuStackPanel;
            }
            set
            {
                contextMenuStackPanel = value;
                RaisePropertyChanged(() => ContextMenuStackPanel);
            }
        }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                RaisePropertyChanged(() => SearchText);
            }
        }

        public DataRepositoryItem SearchResult
        {
            get { return searchResult; }
        }

        public Visibility SearchResultVisibility
        {
            get { return searchResultVisibility; }
            set
            {
                searchResultVisibility = value;
                RaisePropertyChanged(() => SearchResultVisibility);
                ContentVisibility = SearchResultVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility ContentVisibility
        {
            get { return contentVisibility; }
            set
            {
                contentVisibility = value;
                RaisePropertyChanged(() => ContentVisibility);
            }
        }

        public ObservableCollection<DataRepositoryItem> Children
        {
            get { return children; }
        }

        public ListBox SearchContent
        {
            get { return searchContent; }
            set { searchContent = value; }
        }

        public DataRepositoryItem CurrentPluginItemViewModel
        {
            get { return currentPluginItemViewModel; }
            set
            {
                currentPluginItemViewModel = value;
                RaisePropertyChanged(() => CurrentPluginItemViewModel);
            }
        }

        public ICommand AddDataCommand
        {
            get
            {
                return addDataCommand;
            }
            set
            {
                addDataCommand = value;
                RaisePropertyChanged(() => AddDataCommand);
            }
        }

        public ICommand PlaceOnMapCommand
        {
            get
            {
                return placeOnMapCommand;
            }
            set
            {
                placeOnMapCommand = value;
                RaisePropertyChanged(() => PlaceOnMapCommand);
            }
        }

        public ObservedCommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new ObservedCommand(() =>
                    {
                        var parent = selectedViewModel.Parent;
                        var currentSelectedViewModel = selectedViewModel;
                        parent.Children.Remove(currentSelectedViewModel);
                    }, () =>
                        selectedViewModel != null
                        && selectedViewModel.CanRemove);
                }
                return removeCommand;
            }
        }

        public RelayCommand RefreshCommand
        {
            get
            {
                if (refreshCommand == null)
                {
                    refreshCommand = new RelayCommand(() =>
                    {
                        selectedViewModel.Refresh();
                    });
                }
                return refreshCommand;
            }
        }

        public RelayCommand SearchCommand
        {
            get
            {
                if (searchCommand == null)
                {
                    searchCommand = new RelayCommand(() =>
                    {
                        if (SelectedDataRepositoryItem != null)
                        {
                            var tmp = SelectedDataRepositoryItem;
                            var dataRepositoryPlugin = tmp.SourcePlugin;
                            searchResult.Children.Clear();
                            if (tmp.Children.Count == 0 && dataRepositoryPlugin == null)
                            {
                                tmp = tmp.Parent;
                            }
                            foreach (var item in tmp.GetSearchResult(new string[] { SearchText }))
                            {
                                searchResult.Children.Add(item);
                            }
                            SelectedDataRepositoryItem = tmp;
                            SearchResultVisibility = Visibility.Visible;
                        }
                    });
                }
                return searchCommand;
            }
        }

        public XElement GetSettings()
        {
            XElement treeViewStatusXElement = new XElement("TreeViewStatus");
            foreach (var item in GisEditor.DataRepositoryManager.ExpandedFolders)
            {
                treeViewStatusXElement.Add(new XElement("Item", item));
            }
            treeViewStatusXElement.Add(new XElement("SelectedItem", GisEditor.DataRepositoryManager.CurrentSelectedItem));
            return treeViewStatusXElement;
        }

        public void ApplySettings(XElement state)
        {
            if (state != null)
            {
                GisEditor.DataRepositoryManager.ExpandedFolders.Clear();
                foreach (var item in state.Descendants("Item").Select(i => i.Value))
                {
                    GisEditor.DataRepositoryManager.ExpandedFolders.Add(item);
                }
                var selectedItemXEl = state.Element("SelectedItem");
                if (selectedItemXEl != null)
                {
                    selectedItem = selectedItemXEl.Value;
                }
                foreach (var item in Children)
                {
                    RestoreExpandStatus(item);
                }
            }
        }

        public ObservableCollection<DataRepositoryItem> SearchChildren(IEnumerable<string> keywords)
        {
            ObservableCollection<DataRepositoryItem> resultItems = new ObservableCollection<DataRepositoryItem>();
            foreach (var item in Children)
            {
                foreach (var results in item.GetSearchResult(keywords))
                {
                    resultItems.Add(results);
                }
            }

            return resultItems;
        }

        public static void RestoreExpandStatus(DataRepositoryItem viewModel)
        {
            Collection<string> expandStatus = GisEditor.DataRepositoryManager.ExpandedFolders;
            if (viewModel == null) return;
            viewModel.IsExpanded = expandStatus.Contains(viewModel.Id);
            if (viewModel.Id.Equals(selectedItem)) viewModel.IsSelected = true;
            RestoreChildrenExpandStatus(viewModel.Children, expandStatus);
        }

        public static void RestoreChildrenExpandStatus(ObservableCollection<DataRepositoryItem> treeViewChildren, Collection<string> expandStatus)
        {
            foreach (var item in treeViewChildren)
            {
                item.IsSelected = selectedItem.Equals(item.Id);
                item.IsExpanded = expandStatus.Contains(item.Id);
                RestoreChildrenExpandStatus(item.Children, expandStatus);
            }
        }
    }
}
