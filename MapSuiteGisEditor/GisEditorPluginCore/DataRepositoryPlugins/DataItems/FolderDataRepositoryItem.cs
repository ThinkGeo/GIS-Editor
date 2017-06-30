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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FolderDataRepositoryItem : DataRepositoryItem
    {
        private static IEnumerable<string> searchPatterns;

        private bool isRoot;
        private bool isRenamed;
        private bool isUpdating;
        private bool collectSubItems;
        private DirectoryInfo folderInfo;

        static FolderDataRepositoryItem()
        {
            var providers = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>();
            searchPatterns = providers.Where(p => !string.IsNullOrEmpty(p.ExtensionFilter)).SelectMany(tmpProvider =>
            {
                var tmpFilter = tmpProvider.ExtensionFilter;
                string[] filterPairs = tmpFilter.Split('|');
                Collection<string> resultPatterns = new Collection<string>();
                for (int i = 1; i < filterPairs.Length; i += 2)
                {
                    foreach (var singleFilter in filterPairs[i].Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        resultPatterns.Add(singleFilter);
                    }
                }

                return resultPatterns;
            }).Distinct();
        }

        public FolderDataRepositoryItem(string folderPath, bool isRoot, bool collectSubItems = false)
        {
            this.isRoot = isRoot;
            this.collectSubItems = collectSubItems;
            if (Directory.Exists(folderPath))
            {
                folderInfo = new DirectoryInfo(folderPath);
                CollectFolderChildren(folderPath);
                InitContextMenuItem();
                Name = folderInfo.Name;
                string uri = isRoot ? "/GisEditorPluginCore;component/Images/dr_datafolder.png" : "/GisEditorPluginCore;component/Images/dr_folder_closed.png";
                Icon = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
            }
        }

        protected override bool CanRenameCore
        {
            get { return true; }
        }

        protected override bool CanRemoveCore
        {
            get { return isRoot; }
        }

        protected override string CategoryCore
        {
            get { return "File Folder"; }
        }

        public DirectoryInfo FolderInfo
        {
            get { return folderInfo; }
        }

        public bool IsRenamed
        {
            get { return isRenamed; }
        }

        protected override string IdCore
        {
            get { return FolderInfo.FullName; }
        }

        protected override bool RenameCore(string newName)
        {
            Name = newName;
            return true;
        }

        protected override Collection<DataRepositoryItem> GetSearchResultCore(IEnumerable<string> keywords)
        {
            var result = new Collection<DataRepositoryItem>();
            foreach (var item in GetAllSubItemsFromFolder(folderInfo.FullName))
            {
                if (Directory.Exists(item))
                {
                    var folderItem = new FolderDataRepositoryItem(item, false);
                    foreach (var subItem in folderItem.GetSearchResult(keywords))
                    {
                        result.Add(subItem);
                    }
                }
                else
                {
                    string fileName = Path.GetFileName(item);
                    if (keywords.Any(keyWord => fileName.IndexOf(keyWord, StringComparison.OrdinalIgnoreCase) != -1))
                        result.Add(new FileDataRepositoryItem(item));
                }
            }

            return result;
        }

        private void InitRootContextMenu()
        {
            if (ContextMenu == null) ContextMenu = new ContextMenu();

            MenuItem showInWindowsExplorerItem = new MenuItem();
            showInWindowsExplorerItem.Header = GisEditor.LanguageManager.GetStringResource("ShowInWindowsExplorerMenuItemLabel");
            showInWindowsExplorerItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/windows explorer.png", 16, 16);
            showInWindowsExplorerItem.Command = new RelayCommand(ShowInWindowsExplorer, () => folderInfo != null && Directory.Exists(folderInfo.FullName));
            ContextMenu.Items.Add(showInWindowsExplorerItem);

            MenuItem removeFolderMenuItem = new MenuItem();
            removeFolderMenuItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryRemovefromRepositoryMenuItemLabel");
            removeFolderMenuItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/dr_remove_item.png", 16, 16);
            removeFolderMenuItem.Command = new RelayCommand(RemoveFolderDataRepositoryItem);
            ContextMenu.Items.Add(removeFolderMenuItem);

            if (CanRename)
            {
                MenuItem renameItem = new MenuItem();
                renameItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryRenameMenuItemLabel");
                renameItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/rename.png", 16, 16);
                renameItem.Command = new RelayCommand(() => DataRepositoryContentViewModel.SelectedDataRepositoryItem.IsRenaming = true);
                ContextMenu.Items.Add(renameItem);
            }
        }

        private void ShowInWindowsExplorer()
        {
            ProcessUtils.OpenPath(folderInfo.FullName);
        }

        private static void RemoveFolderDataRepositoryItem()
        {
            var senderItem = DataRepositoryContentViewModel.SelectedDataRepositoryItem;
            if (senderItem != null && senderItem.Parent != null)
            {
                if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataRepositoryRemoveDataFolderWarningLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryRemoveDataFolderWarningCaption"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    senderItem.Parent.Children.Remove(senderItem);
                    GisEditor.InfrastructureManager.SaveSettings(GisEditor.DataRepositoryManager.GetPlugins());
                }
            }
        }

        private void InitSubContextMenu()
        {
            if (ContextMenu == null) ContextMenu = new ContextMenu();

            if (CanRename)
            {
                MenuItem renameItem = new MenuItem();
                renameItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryRenameMenuItemLabel");
                renameItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/rename.png", 16, 16);
                renameItem.Command = new RelayCommand(() => DataRepositoryContentViewModel.SelectedDataRepositoryItem.IsRenaming = true);
                ContextMenu.Items.Add(renameItem);
            }

            MenuItem showInWindowsExplorerItem = new MenuItem();
            showInWindowsExplorerItem.Header = GisEditor.LanguageManager.GetStringResource("ShowInWindowsExplorerMenuItemLabel");
            showInWindowsExplorerItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/windows explorer.png", 16, 16);
            showInWindowsExplorerItem.Command = new ObservedCommand(() => ProcessUtils.OpenPath(folderInfo.FullName), () => FolderInfo != null && FolderInfo.Exists);
            ContextMenu.Items.Add(showInWindowsExplorerItem);
        }

        private void CollectFolderChildren(string folderPath)
        {
            string[] folders = { };
            try
            {
                folders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                foreach (var folder in folders)
                {
                    FolderDataRepositoryItem folderDataRepositoryItem = new FolderDataRepositoryItem(folder, false) { Parent = this };
                    Children.Add(folderDataRepositoryItem);
                }
            }
        }

        private void InitContextMenuItem()
        {
            if (isRoot)
                InitRootContextMenu();
            else
                InitSubContextMenu();
        }

        private DataRepositoryItem GetSelectedTreeView(DataRepositoryItem treeViewParent)
        {
            DataRepositoryItem selectedTreeView = null;

            foreach (var child in treeViewParent.Children)
            {
                if (child.IsSelected)
                {
                    selectedTreeView = child;
                    break;
                }

                if (child.Children.Count > 0) return GetSelectedTreeView(child);
            }

            return selectedTreeView;
        }

        internal static IEnumerable<string> GetAllSubItemsFromFolder(string folderPath)
        {
            foreach (var folder in Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly))
            {
                yield return folder;
            }
            foreach (var searchPattern in searchPatterns)
            {
                foreach (var file in Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(file);
                    if (!extension.Equals(".grd", StringComparison.OrdinalIgnoreCase)
                        || (extension.Equals(".grd", StringComparison.OrdinalIgnoreCase) && !File.Exists(Path.ChangeExtension(file, ".gri"))))
                    {
                        yield return file;
                    }
                }
            }
        }

        internal bool Rename(string newName)
        {
            bool succussed = false;
            string newLocation = Path.Combine(FolderInfo.Parent.FullName, newName);
            if (newLocation != FolderInfo.FullName)
            {
                //after MoveTo, FolderInfo.FullName will has a "\" in the end, it will cause some issue, for example,
                //newLocation is "D:\data", after MoveTo, FolderInfo.FullName is "D:\data\"
                try
                {
                    FolderInfo.MoveTo(newLocation);
                    folderInfo = new DirectoryInfo(newLocation);
                    isRenamed = true;
                    succussed = true;
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));

                    System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("DataRepositoryWarningLabel"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            return succussed;
        }

        protected override void RefreshCore()
        {
            try
            {
                RefreshLocalFiles(this);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshLocalFiles(FolderDataRepositoryItem folderItem)
        {
            if (folderItem != null && !isUpdating)
            {
                isUpdating = true;
                if (Directory.Exists(folderItem.FolderInfo.FullName))
                {
                    Children.Clear();
                    foreach (var item in GetAllSubItemsFromFolder(folderItem.FolderInfo.FullName))
                    {
                        DataRepositoryItem dataRepositoryItem;
                        if (File.Exists(item))
                            dataRepositoryItem = new FileDataRepositoryItem(item);
                        else
                            dataRepositoryItem = new FolderDataRepositoryItem(item, false);

                        dataRepositoryItem.Parent = this;
                        dataRepositoryItem.IsExpanded = GisEditor.DataRepositoryManager.ExpandedFolders.Contains(dataRepositoryItem.Id);
                        Children.Add(dataRepositoryItem);
                    }
                }
                else
                {
                    Parent.Refresh();
                }
                isUpdating = false;
            }
        }
    }
}