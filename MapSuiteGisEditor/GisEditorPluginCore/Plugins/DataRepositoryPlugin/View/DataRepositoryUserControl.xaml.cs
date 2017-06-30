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
using System.Linq;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DataRepositoryUserControl.xaml
    /// </summary>
    [Obfuscation]
    internal partial class DataRepositoryUserControl : UserControl
    {
        private static readonly string alertMessage = "The folder \"{0}\" doesn't exist. " + Environment.NewLine + "Please re-add it or select its parent folder to refresh.";
        private static LinearGradientBrush highlightBrush;
        private static SolidColorBrush transparentBrush;
        private static SolidColorBrush borderBrush;

        private DataRepositoryItem selectedTreeViewItem;

        static DataRepositoryUserControl()
        {
            transparentBrush = new SolidColorBrush(Colors.Transparent);
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 183, 0));

            highlightBrush = new LinearGradientBrush();
            highlightBrush.StartPoint = new Point(0, 0);
            highlightBrush.EndPoint = new Point(0, 1);
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(254, 251, 244), 0));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 231, 206), 0.19));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 222, 184), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 206, 107), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 222, 154), 0.79));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 235, 170), 1));
        }

        public DataRepositoryUserControl()
        {
            InitializeComponent();
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("DataRepositoryHelp", HelpButtonMode.IconOnly);
        }

        [Obfuscation]
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Restore CurrentItemViewModel after reload Data Repository Plugin
            if (DataRepositoryContentViewModel.Current.CurrentPluginItemViewModel == null && selectedTreeViewItem != null && DataRepositoryContentViewModel.Current.SearchResultVisibility != Visibility.Visible)
            {
                TreeView_SelectedItemChanged(sender, new RoutedPropertyChangedEventArgs<object>(null, selectedTreeViewItem));
            }
        }

        [Obfuscation]
        private void TreeViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            HideRenameMenuItemInTreeView(sender, Visibility.Visible);
        }

        [Obfuscation]
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!string.IsNullOrEmpty(DataRepositoryContentViewModel.Current.SearchText) && e.Key == Key.Enter)
            {
                DataRepositoryContentViewModel.Current.SearchCommand.Execute(null);
            }
        }

        [Obfuscation]
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DataRepositoryContentViewModel.Current.SearchResultVisibility = Visibility.Collapsed;
            if (e.NewValue != null && (selectedTreeViewItem = e.NewValue as DataRepositoryItem) != null)
            {
                FolderDataRepositoryItem folderItem = selectedTreeViewItem as FolderDataRepositoryItem;
                if (folderItem != null && !Directory.Exists(folderItem.FolderInfo.FullName))
                {
                    System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.InvariantCulture, alertMessage,
                        folderItem.FolderInfo.FullName), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                DataRepositoryContentViewModel.SelectedDataRepositoryItem = selectedTreeViewItem;
                if (selectedTreeViewItem.ContextMenu != null)
                {
                    DataRepositoryContentViewModel.Current.ContextMenuStackPanel = DataRepositoryContentUserControl.ConvertContextMenuToButton(selectedTreeViewItem.ContextMenu);
                }
                else
                {
                    DataRepositoryContentViewModel.Current.ContextMenuStackPanel = null;
                }
                if (!selectedTreeViewItem.IsLeaf)
                {
                    var viewModelContainingContent = selectedTreeViewItem.GetRootDataRepositoryItem();
                    DataRepositoryContentViewModel.Current.CurrentPluginItemViewModel = viewModelContainingContent;
                    if (viewModelContainingContent.ContextMenu != null && viewModelContainingContent.ContextMenu.HasItems)
                    {
                        DataRepositoryContentViewModel.Current.AddDataCommand = ((MenuItem)viewModelContainingContent.ContextMenu.Items[0]).Command;
                    }
                    if (DataRepositoryContentViewModel.Current.CurrentPluginItemViewModel.Content != null)
                    {
                        if (!DataRepositoryContentViewModel.SelectedDataRepositoryItem.Id.Equals("Data Folders", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            var sourcePlugin = DataRepositoryContentViewModel.SelectedDataRepositoryItem.GetSourcePlugin();
                            if (sourcePlugin != null && sourcePlugin.CanRefreshDynamically)
                            {
                                DataRepositoryContentViewModel.SelectedDataRepositoryItem.Refresh();
                            }
                        }
                        DataRepositoryContentViewModel.Current.CurrentPluginItemViewModel.Content.DataContext = DataRepositoryContentViewModel.SelectedDataRepositoryItem;
                    }
                }
            }
        }

        [Obfuscation]
        private void TreeViewItem_RightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var currentViewModel = sender.GetDataContext<DataRepositoryItem>();
            if (currentViewModel != null)
                currentViewModel.IsSelected = true;
        }

        [Obfuscation]
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (DataRepositoryContentViewModel.Current.SearchResultVisibility == Visibility.Visible)
            {
                DataRepositoryContentViewModel.Current.SearchResultVisibility = Visibility.Collapsed;
            }
        }

        [Obfuscation]
        private void RenameControl_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            if (!e.OldText.Equals(e.NewText))
            {
                Rename(e.NewText);
            }
            else e.IsCancelled = true;
        }

        [Obfuscation]
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataRepositoryItem currentItem = sender.GetDataContext<DataRepositoryItem>();
            if (currentItem != null && currentItem.IsLoadable)
            {
                currentItem.Load();
            }
        }

        private static void HideRenameMenuItemInTreeView(object sender, Visibility visibility)
        {
            var currentDataRepositoryItem = sender.GetDataContext<FolderDataRepositoryItem>();
            if (currentDataRepositoryItem != null)
            {
                if (currentDataRepositoryItem.ContextMenu.Items.Count > 2)
                {
                    var menuItem = currentDataRepositoryItem.ContextMenu.Items[2] as MenuItem;
                    if (menuItem.Header.ToString().Contains("Rename"))
                    {
                        menuItem.Visibility = visibility;
                    }
                }
            }
        }

        private void Rename(string newName)
        {
            var folderDataItem = DataRepositoryTree.SelectedValue as FolderDataRepositoryItem;
            if (folderDataItem != null)
            {
                folderDataItem.Rename(newName);
                folderDataItem.Name = newName;
            }
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = borderBrush;
            ((Border)sender).Background = highlightBrush;
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = transparentBrush;
            ((Border)sender).Background = transparentBrush;
        }
    }
}