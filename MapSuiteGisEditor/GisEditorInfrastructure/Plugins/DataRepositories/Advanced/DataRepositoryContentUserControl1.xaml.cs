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
using System.Windows.Media;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for DataRepositoryContentUserControl1.xaml
    /// </summary>
    public partial class DataRepositoryContentUserControl : UserControl
    {
        private static List<WpfMap> hookedMaps = new List<WpfMap>();
        private ICommand tmpRelayCommand;
        private bool isDesending;
        private Collection<GridViewColumn> columns;

        public event EventHandler<DataRepositoryItemMouseDoubleClickUserControlEventArgs> DataRepositoryItemMouseDoubleClick;

        public DataRepositoryContentUserControl()
        {
            InitializeComponent();
            columns = new Collection<GridViewColumn>();
            Loaded += DataRepositoryContentUserControl_Loaded;
            MouseUp += DataRepositoryContentUserControl_MouseUp;
        }

        public Collection<GridViewColumn> Columns
        {
            get { return columns; }
        }

        protected virtual void OnDataRepositoryItemMouseDoubleClick(DataRepositoryItemMouseDoubleClickUserControlEventArgs e)
        {
            EventHandler<DataRepositoryItemMouseDoubleClickUserControlEventArgs> handler = DataRepositoryItemMouseDoubleClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void DataRepositoryContentUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (GridViewColumn column in columns)
            {
                if (!GridViewList.Columns.Contains(column))
                {
                    GridViewList.Columns.Add(column);
                }
            }
        }

        [Obfuscation]
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRepositoryItem[] selectedItems = ChildrenList.SelectedItems.OfType<DataRepositoryItem>().ToArray();
            if (selectedItems.Length > 1)
            {
                DataRepositoryContentViewModel.Current.PlaceOnMapCommand = DataRepositoryHelper.GetPlaceMultipleFilesCommand(selectedItems);

                DataRepositoryItem dataRepositoryItem = selectedItems[0];
                DataRepositoryContentViewModel.Current.ContextMenuStackPanel = DataRepositoryContentUserControl.ConvertContextMenuToButton(dataRepositoryItem.ContextMenu, DataRepositoryContentViewModel.Current.PlaceOnMapCommand);
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

            DataRepositoryItem dataRepositoryItem = sender.GetDataContext<DataRepositoryItem>();
            if (dataRepositoryItem != null && !dataRepositoryItem.CanRename)
            {
                var listItem = sender as ListBoxItem;
                if (listItem != null)
                {
                    // listItem.DataContext
                    foreach (MenuItem menu in listItem.ContextMenu.Items)
                    {
                        if (menu.Header.Equals("Rename")) menu.Visibility = Visibility.Collapsed;
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
        private void DataRepositoryContentUserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DataRepositoryItem[] selectedItems = ChildrenList.SelectedItems.OfType<DataRepositoryItem>().ToArray();
            foreach (var item in selectedItems)
            {
                item.IsRenaming = false;
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
                    DataRepositoryItemMouseDoubleClickUserControlEventArgs args = new DataRepositoryItemMouseDoubleClickUserControlEventArgs();
                    args.SelectedDataRepositoryItem = dataRepositoryItem;
                    OnDataRepositoryItemMouseDoubleClick(args);
                    if (args.Handled) return;

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
                    if (draggedItems != null && draggedItems.Count() > 0)
                    {
                        DataRepositoryItem dataRepositoryItem = draggedItems.First();

                        while (dataRepositoryItem.Parent != null)
                        {
                            dataRepositoryItem = dataRepositoryItem.Parent;
                        }
                        if (dataRepositoryItem.SourcePlugin != null && dataRepositoryItem.SourcePlugin.CanDropOnMap)
                        {
                            dataRepositoryItem.SourcePlugin.DropOnMap(draggedItems);
                        }
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
                    if (selectedItem.CanRename && selectedItem.Rename(newName))
                    {
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

        public static StackPanel ConvertContextMenuToButton(ContextMenu menu, ICommand placeOnMapCommand = null)
        {
            StackPanel sp = new StackPanel();
            sp.HorizontalAlignment = HorizontalAlignment.Left;
            sp.Orientation = Orientation.Horizontal;

            MenuItem[] menuItems = menu.Items.OfType<MenuItem>().ToArray();
            if (menuItems.Length > 0)
            {
                Grid seperator = new Grid();
                seperator.Width = 1;
                seperator.Height = 18;
                seperator.Background = new SolidColorBrush(Colors.Gray);
                seperator.Margin = new Thickness(2, 0, 2, 0);
                sp.Children.Add(seperator);

                foreach (MenuItem item in menuItems)
                {
                    Button button = new Button();
                    if (item.Header == GisEditor.LanguageManager.GetStringResource("DataRepositoryItemPlaceMapHeader") && placeOnMapCommand != null)
                    {
                        button.Command = placeOnMapCommand;
                    }
                    else
                    {
                        button.Command = item.Command;
                    }
                    button.ToolTip = item.Header;
                    ToolTipService.SetShowOnDisabled(button, true);

                    Border border = new Border();
                    border.Width = 22;
                    border.Height = 22;
                    border.BorderThickness = new Thickness(1);
                    Image image = item.Icon as Image;
                    if (image != null)
                    {
                        border.Child = new Image() { Width = 16, Height = 16, Source = image.Source };
                    }
                    button.Content = border;
                    sp.Children.Add(button);
                }
            }

            return sp;
        }
    }
}
