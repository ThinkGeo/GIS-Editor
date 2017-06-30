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
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DatabaseTreeViewModel : ViewModelBase
    {
        private static DataRepositoryItem selectedViewModel;
        private Visibility contentVisibility;
        private ObservableCollection<DataRepositoryItem> children;
        private DataRepositoryItem currentPluginItemViewModel;

        [NonSerialized]
        private ListBox searchContent;

        private string selectedItem = "";

        public DatabaseTreeViewModel(MsSql2008FeatureLayerInfo model)
        {
            children = new ObservableCollection<DataRepositoryItem>();
            MsSqlServerDataRepositoryItem serverItem = MsSqlServerDataRepositoryPlugin.GetMsSqlServerDataRepositoryItem(model);
            Children.Add(serverItem);
            if (Children.Count > 0)
            {
                Children[0].IsSelected = true;
            }
        }

        public static DataRepositoryItem SelectedDataRepositoryItem
        {
            get { return selectedViewModel; }
            set { selectedViewModel = value; }
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

        internal void RestoreExpandStatus(DataRepositoryItem viewModel, Collection<string> expandStatus)
        {
            if (viewModel == null) return;
            viewModel.IsExpanded = expandStatus.Contains(viewModel.Id);
            if (viewModel.Id.Equals(selectedItem)) viewModel.IsSelected = true;
            RestoreChildrenExpandStatus(viewModel.Children, expandStatus);
        }

        private void RestoreChildrenExpandStatus(ObservableCollection<DataRepositoryItem> treeViewChildren, Collection<string> expandStatus)
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