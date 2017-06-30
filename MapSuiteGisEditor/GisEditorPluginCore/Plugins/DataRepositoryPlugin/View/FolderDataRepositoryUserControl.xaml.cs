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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for FolderChildrenList.xaml
    /// </summary>
    public partial class FolderDataRepositoryUserControl : UserControl
    {
        private static List<WpfMap> hookedMaps = new List<WpfMap>();
        private ICommand tmpRelayCommand;
        private bool isDesending;

        public FolderDataRepositoryUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRepositoryItem[] selectedItems = ChildrenList.SelectedItems.OfType<DataRepositoryItem>().ToArray();
            if (selectedItems.Length > 1)
            {
                DataRepositoryContentViewModel.Current.PlaceOnMapCommand = DataRepositoryHelper.GetPlaceMultipleFilesCommand(selectedItems);
            }
            else if (selectedItems.Length == 1)
            {
                DataRepositoryItem dataRepositoryItem = selectedItems[0];
                if (dataRepositoryItem != null && dataRepositoryItem.ContextMenu != null)
                {
                    DataRepositoryContentViewModel.Current.ContextMenuStackPanel = DataRepositoryContentUserControl.ConvertContextMenuToButton(dataRepositoryItem.ContextMenu);
                }

                if (dataRepositoryItem != null && dataRepositoryItem.IsLoadable)
                {
                    DataRepositoryContentViewModel.Current.PlaceOnMapCommand = ((MenuItem)selectedItems.First().ContextMenu.Items[0]).Command;
                }
                else DataRepositoryContentViewModel.Current.PlaceOnMapCommand = null;
            }
        }

        [Obfuscation]
        private void ListBoxItemContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectedItems = ChildrenList.SelectedItems.OfType<DataRepositoryItem>().ToArray();
            if (selectedItems.Length > 1)
            {
                DataRepositoryHelper.AddSelectedItemsToMap(selectedItems, sender, ref tmpRelayCommand);
            }

            var folderDataRepositoryItem = sender.GetDataContext<FolderDataRepositoryItem>();
            if (folderDataRepositoryItem != null)
            {
                var listItem = sender as ListBoxItem;
                if (listItem != null)
                {
                    // listItem.DataContext
                    foreach (MenuItem menu in listItem.ContextMenu.Items)
                    {
                        if (menu.Header.Equals("Rename"))
                            menu.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        [Obfuscation]
        private void ListBoxItemContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (tmpRelayCommand != null)
            {
                DataRepositoryHelper.RestoreFirstMenuItemCommand(sender, tmpRelayCommand);
            }

        }

        [Obfuscation]
        private void ListBoxItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                var currentDataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
                if (currentDataRepositoryItem != null)
                {
                    currentDataRepositoryItem.IsRenaming = true;
                }
            }
        }

        //this method is partially responsible for issue-6698
        [Obfuscation]
        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedItems = ChildrenList.SelectedItems.OfType<DataRepositoryItem>().ToArray();
            DataRepositoryContentViewModel.SelectedDataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
            if (GisEditor.ActiveMap != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (selectedItems.Contains(DataRepositoryContentViewModel.SelectedDataRepositoryItem)
                            && e.ButtonState == MouseButtonState.Pressed)
                        {
                            HookEventForMap();
                            //do not do drag and drop when renaming
                            if (!selectedItems.Any(data => data.IsRenaming))
                            {
                                DragDrop.DoDragDrop(ChildrenList, selectedItems, DragDropEffects.Move);
                            }
                        }
                    }
                ));
            }
        }

        [Obfuscation]
        private void ListBoxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        [Obfuscation]
        private void ListBoxItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var currentDataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
            if (currentDataRepositoryItem != null)
            {
                DataRepositoryContentViewModel.SelectedDataRepositoryItem = currentDataRepositoryItem;
                if (currentDataRepositoryItem.IsLoadable)
                {
                    DataRepositoryContentViewModel.Current.PlaceOnMapCommand = ((MenuItem)DataRepositoryContentViewModel.SelectedDataRepositoryItem.ContextMenu.Items[0]).Command;
                }
                else DataRepositoryContentViewModel.Current.PlaceOnMapCommand = null;
            }
        }

        [Obfuscation]
        private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
            if (dataRepositoryItem != null)
            {
                if (!dataRepositoryItem.IsLeaf)
                {
                    FolderDataRepositoryItem folderDataRepositoryItem = dataRepositoryItem as FolderDataRepositoryItem;
                    if (folderDataRepositoryItem != null && !Directory.Exists(folderDataRepositoryItem.FolderInfo.FullName))
                    {
                        System.Windows.Forms.MessageBox.Show(folderDataRepositoryItem.FolderInfo.FullName + " doesn't exist."
                            , "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        return;
                    }
                    if (dataRepositoryItem.Parent != null && !dataRepositoryItem.Parent.IsExpanded) dataRepositoryItem.Parent.IsExpanded = true;
                    dataRepositoryItem.Refresh();
                    dataRepositoryItem.IsExpanded = true;
                    dataRepositoryItem.IsSelected = true;
                    DataContext = dataRepositoryItem;
                }
                else if (dataRepositoryItem.IsLoadable)
                {
                    dataRepositoryItem.Load();
                }
            }
        }

        [Obfuscation]
        private void ListBoxItemGotFocus(object sender, RoutedEventArgs e)
        {
            DataRepositoryContentViewModel.SelectedDataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
        }

        private static void HookEventForMap()
        {
            if (!hookedMaps.Contains(GisEditor.ActiveMap))
            {
                hookedMaps.Add(GisEditor.ActiveMap);

                GisEditor.ActiveMap.Drop += (s, arg) =>
                {
                    var draggedItems = arg.Data.GetData(typeof(DataRepositoryItem[])) as DataRepositoryItem[];
                    if (draggedItems != null)
                    {
                        var selectedFileItems = draggedItems.OfType<FileDataRepositoryItem>().ToList();
                        DataRepositoryHelper.PlaceFilesOnMap(selectedFileItems);
                    }

                    arg.Handled = true;
                };
            }
        }

        [Obfuscation]
        private void RenameControl_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            string newName = e.NewText;
            e.IsCancelled = true;
            if (!string.IsNullOrEmpty(newName))
            {
                var selectedItem = ChildrenList.SelectedValue as DataRepositoryItem;
                if (selectedItem != null)
                {
                    selectedItem.IsRenaming = false;
                    var fileDataItem = selectedItem as FileDataRepositoryItem;
                    var folderDataItem = selectedItem as FolderDataRepositoryItem;
                    if (fileDataItem != null && fileDataItem.Rename(newName))
                    {
                        selectedItem.Name = newName;
                        e.IsCancelled = false;
                    }
                    else if (folderDataItem != null)
                    {
                        selectedItem.Name = newName;
                        e.IsCancelled = false;
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("FilderDataRepositeryControlNameOneCharecterText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK);
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
                    ObservableCollection<DataRepositoryItem> children = null;

                    var dataRepositoryItem = DataContext as DataRepositoryItem;
                    var dataRepositoryViewModel = DataContext as DataRepositoryContentViewModel;
                    if (dataRepositoryItem != null)
                        children = dataRepositoryItem.Children;
                    else if (dataRepositoryViewModel != null)
                        children = dataRepositoryViewModel.Children;
                    else
                        return;
                    DataRepositoryItem[] folderItems = children.Where(c => c.Category == "Folder" || c.Category == "File Folder").ToArray();
                    DataRepositoryItem[] fileItems = children.Where(c => !(c.Category == "Folder" || c.Category == "File Folder")).ToArray();

                    var headerText = (clickedColumn.Header as TextBlock).Text;

                    if (headerText.Equals(GisEditor.LanguageManager.GetStringResource("FolderDataRepositoryUserControlNameText")))
                    {
                        folderItems = !isDesending ? folderItems.OrderBy(v => v.Name).ToArray() : folderItems.OrderByDescending(v => v.Name).ToArray();
                        fileItems = !isDesending ? fileItems.OrderBy(v => v.Name).ToArray() : fileItems.OrderByDescending(v => v.Name).ToArray();
                    }
                    else if (headerText.Equals(GisEditor.LanguageManager.GetStringResource("FolderDataRepositoryUserControlTypeText")))
                    {
                        folderItems = !isDesending ? folderItems.OrderBy(v => v.Category).ToArray() : folderItems.OrderByDescending(v => v.Category).ToArray();
                        fileItems = !isDesending ? fileItems.OrderBy(v => v.Category).ToArray() : fileItems.OrderByDescending(v => v.Category).ToArray();
                    }
                    else if (headerText.Equals(GisEditor.LanguageManager.GetStringResource("CommonSizeText")))
                    {
                        folderItems = !isDesending ? folderItems.OrderBy(v => GetSize(v)).ToArray() : folderItems.OrderByDescending(v => GetSize(v)).ToArray();
                        fileItems = !isDesending ? fileItems.OrderBy(v => GetSize(v)).ToArray() : fileItems.OrderByDescending(v => GetSize(v)).ToArray();
                    }

                    children.Clear();
                    if (isDesending)
                    {
                        foreach (var item in fileItems.Concat(folderItems))
                        {
                            children.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in folderItems.Concat(fileItems))
                        {
                            children.Add(item);
                        }
                    }

                    isDesending = !isDesending;

                }
            }
        }

        private long GetSize(DataRepositoryItem dataRepositoryItem)
        {
            long size = 0;
            if (dataRepositoryItem != null && dataRepositoryItem.CustomData.ContainsKey("Size"))
            {
                try
                {
                    size = (long)dataRepositoryItem.CustomData["Size"];
                }
                catch (Exception)
                {
                    size = 0;
                }
            }
            return size;
        }
    }
}
