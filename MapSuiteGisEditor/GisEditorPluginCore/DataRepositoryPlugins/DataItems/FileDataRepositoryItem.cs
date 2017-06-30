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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Windows;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FileDataRepositoryItem : DataRepositoryItem
    {
        private static string notExistLabel = GisEditor.LanguageManager.GetStringResource("DataRepositoryNotExistedLabel");
        private const string fileTypeFormat = "{0} File";

        private FileInfo fileInfo;
        private LayerPlugin matchingLayerPlugin;

        public FileDataRepositoryItem(string filePath)
        {
            fileInfo = new FileInfo(filePath);
            Name = fileInfo.Name;
            CustomData["Size"] = fileInfo.Length;

            matchingLayerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>()
    .FirstOrDefault(tmpPlugin => tmpPlugin.ExtensionFilter.ToUpperInvariant().Contains(FileInfo.Extension.ToUpperInvariant()));

            string imageUri = matchingLayerPlugin != null ? ((BitmapImage)matchingLayerPlugin.SmallIcon).UriSource.OriginalString : "/GisEditorPluginCore;component/Images/dr_fileicon_raster.png";
            Icon = new BitmapImage(new Uri(imageUri, UriKind.RelativeOrAbsolute));

            AddMenuItems();
        }

        protected override bool CanRenameCore
        {
            get { return true; }
        }

        public FileInfo FileInfo
        {
            get { return fileInfo; }
        }

        protected override string CategoryCore
        {
            get
            {
                if (FileInfo.Extension.Equals(".SHP", StringComparison.OrdinalIgnoreCase)) return GetShapeFileType(FileInfo.FullName);
                return string.Format(CultureInfo.InvariantCulture, fileTypeFormat, FileInfo.Extension.Remove(0, 1).ToUpperInvariant());
            }
        }

        protected override bool IsLoadableCore
        {
            get { return true; }
        }

        protected override void LoadCore()
        {
            if (matchingLayerPlugin == null || !matchingLayerPlugin.IsActive)
            {
                System.Windows.Forms.MessageBox.Show("The relevant layer plugin has been disabled, please enable it in Plugin Manager first", "Layer Plugin");
                return;
            }
            if (GisEditor.ActiveMap != null && matchingLayerPlugin != null)
            {
                if (File.Exists(fileInfo.FullName))
                {
                    var getLayersParameters = new GetLayersParameters();
                    getLayersParameters.LayerUris.Add(new Uri(fileInfo.FullName));
                    var layers = matchingLayerPlugin.GetLayers(getLayersParameters);
                    if (layers.Count > 0)
                    {
                        GisEditor.ActiveMap.AddLayersBySettings(layers, true);
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadCoreDescription));
                    }
                }
                else ShowFileNotFoundMessage();
            }
        }

        protected override string IdCore
        {
            get { return fileInfo.FullName; }
        }

        protected override bool IsLeafCore
        {
            get { return true; }
        }

        private void AddMenuItems()
        {
            if (CanRename)
            {
                MenuItem renameFileItem = new MenuItem();
                renameFileItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryRenameMenuItemLabel");
                renameFileItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/rename.png", 16, 16);
                renameFileItem.Command = new RelayCommand(() => DataRepositoryContentViewModel.SelectedDataRepositoryItem.IsRenaming = true);
                ContextMenu.Items.Add(renameFileItem);
            }

            if (fileInfo.Extension.Equals(".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                MenuItem editConfigItem = new MenuItem();
                editConfigItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryEditColumnMappingMenuItemLabel");
                editConfigItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/dr_edit.png", 16, 16);
                editConfigItem.Command = new RelayCommand(() =>
                {
                    try
                    {
                        var configFilePath = fileInfo.FullName + ".config";
                        CSVInfoModel csvInfoModel = File.Exists(configFilePath) ? CSVInfoModel.FromConfig(configFilePath) : new CSVInfoModel(fileInfo.FullName, ",");
                        CSVConfigWindow csvConfigWindow = new CSVConfigWindow(new CSVViewModel(new Collection<CSVInfoModel> { csvInfoModel }));
                        csvConfigWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("DataRepositoryWarningLabel"));
                    }
                });
                ContextMenu.Items.Add(editConfigItem);
            }

            MenuItem showInWindowsExplorerItem = new MenuItem();
            showInWindowsExplorerItem.Header = GisEditor.LanguageManager.GetStringResource("ShowInWindowsExplorerMenuItemLabel");
            showInWindowsExplorerItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/windows explorer.png", 16, 16);
            showInWindowsExplorerItem.Command = new ObservedCommand(() => ProcessUtils.OpenPath(fileInfo.FullName), () => fileInfo != null && File.Exists(fileInfo.FullName));
            ContextMenu.Items.Add(showInWindowsExplorerItem);

            MenuItem propertyItem = new MenuItem();
            propertyItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/properties.png", 16, 16);
            propertyItem.Header = "Properties";
            propertyItem.Command = new RelayCommand(() =>
            {
                GetLayersParameters getLayersParameters = new GetLayersParameters();
                getLayersParameters.LayerUris.Add(new Uri(fileInfo.FullName));
                Layer layer = matchingLayerPlugin.GetLayers(getLayersParameters).FirstOrDefault();
                UserControl userControl = matchingLayerPlugin.GetPropertiesUI(layer);

                Window propertiesDockWindow = new Window()
                {
                    Content = userControl,
                    Title = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties"),
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = System.Windows.ResizeMode.NoResize,
                    Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style
                };

                propertiesDockWindow.ShowDialog();
            });
            ContextMenu.Items.Add(propertyItem);
        }

        protected override bool RenameCore(string newName)
        {
            bool isAllAccessable = false;
            if (File.Exists(FileInfo.FullName))
            {
                try
                {
                    newName = Path.GetFileNameWithoutExtension(newName);
                    var relatedFiles = GetRelatedFiles();
                    if (relatedFiles != null && relatedFiles.Count > 0)
                    {
                        isAllAccessable = relatedFiles.All(file => !file.IsInUse());
                        if (isAllAccessable)
                        {
                            foreach (var relatedFile in relatedFiles)
                            {
                                relatedFile.MoveTo(Path.Combine(relatedFile.DirectoryName,
                                                                newName + relatedFile.Extension));
                            }
                            var newFileInfo = relatedFiles.FirstOrDefault(relatedFile => relatedFile.Extension == FileInfo.Extension);
                            fileInfo = newFileInfo;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataRepositoryCannotAccessLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryWarningLabel"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            else ShowFileNotFoundMessage();
            return isAllAccessable;
        }

        private void ShowFileNotFoundMessage()
        {
            System.Windows.Forms.MessageBox.Show(fileInfo.FullName + GisEditor.LanguageManager.GetStringResource("DataRepositoryDoesntexistLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryWarningLabel"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
        }

        private static string GetWorldFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            string newExtension = String.Concat(extension.Remove(extension.Length - 2, 1), 'w');
            return Path.ChangeExtension(fileName, newExtension);
        }

        private string GetShapeFileType(String pathFileName)
        {
            if (File.Exists(pathFileName))
            {
                return ShapeFileFeatureLayerExtension.GetShapeFileType(pathFileName).ToString();
            }
            return notExistLabel;
        }

        private List<FileInfo> GetRelatedFiles()
        {
            List<FileInfo> results = null;

            string[] suffixes;
            string extension = FileInfo.Extension;

            if (extension.EndsWith("shp", StringComparison.OrdinalIgnoreCase))
            {
                suffixes = new[] { "shp", "idx", "ids", "dbf", "shx", "prj" };
            }
            else if (extension.EndsWith("grd", StringComparison.OrdinalIgnoreCase))
            {
                suffixes = new[] { "grd" };
            }
            else
            {
                suffixes = new[] { extension.Replace(".", string.Empty), Path.GetExtension(GetWorldFileName(fileInfo.FullName)).Replace(".", string.Empty) };
            }

            if (suffixes.Length > 0)
            {
                var files = suffixes.Select(suf =>
                {
                    string relatedFileName = Path.ChangeExtension(FileInfo.FullName, suf);
                    return File.Exists(relatedFileName) ? new FileInfo(relatedFileName) : null;
                }).Where(f => f != null);

                results = new List<FileInfo>(files);
            }

            return results;
        }
    }
}