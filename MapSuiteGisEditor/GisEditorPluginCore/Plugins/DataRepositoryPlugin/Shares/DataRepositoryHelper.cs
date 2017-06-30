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


using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class DataRepositoryHelper
    {
        public static DataRepositoryPlugin GetSourcePlugin(this DataRepositoryItem dataRepositoryItem)
        {
            var rootDataRepsitory = dataRepositoryItem.GetRootDataRepositoryItem();
            return rootDataRepsitory.SourcePlugin;
        }

        public static DataRepositoryItem CreateRootDataRepositoryItem(DataRepositoryPlugin dataRepositoryPlugin)
        {
            DataRepositoryItem dataRepositoryItem = new DataRepositoryItem();
            dataRepositoryItem.SourcePlugin = dataRepositoryPlugin;
            dataRepositoryItem.Name = dataRepositoryPlugin.Name;
            dataRepositoryItem.Icon = dataRepositoryPlugin.SmallIcon;

            var addDataCommand = new RelayCommand(() =>
            {
                var dataItem = dataRepositoryPlugin.CreateDataRepositoryItem();
                if (dataItem != null)
                {
                    dataRepositoryItem.Children.Add(dataItem);
                    GisEditor.InfrastructureManager.SaveSettings(GisEditor.DataRepositoryManager.GetPlugins());
                }
            });
            if (dataRepositoryPlugin.ContextMenu != null && dataRepositoryPlugin.ContextMenu.HasItems)
            {
                ((MenuItem)dataRepositoryPlugin.ContextMenu.Items[0]).Command = addDataCommand;
            }
            dataRepositoryItem.ContextMenu = dataRepositoryPlugin.ContextMenu;
            dataRepositoryItem.Content = dataRepositoryPlugin.Content;
            return dataRepositoryItem;
        }

        public static Image GetMenuIcon(string uri, int width, int height)
        {
            var image = new Image();
            image.Width = width;
            image.Height = height;
            image.Source = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
            return image;
        }

        public static void RestoreFirstMenuItemCommand(object sender, ICommand tmpRelayCommand)
        {
            var firstMenuItem = sender.GetDataContext<DataRepositoryItem>().ContextMenu.Items[0] as MenuItem;
            firstMenuItem.Command = tmpRelayCommand;
            tmpRelayCommand = null;
        }

        public static void AddSelectedItemsToMap(IEnumerable<DataRepositoryItem> selectedItems, object sender, ref ICommand tmpRelayCommand)
        {
            var firstMenuItem = sender.GetDataContext<DataRepositoryItem>().ContextMenu.Items[0] as MenuItem;
            tmpRelayCommand = firstMenuItem.Command;
            var newCommand = GetPlaceMultipleFilesCommand(selectedItems);
            firstMenuItem.Command = newCommand;
        }

        public static RelayCommand GetPlaceMultipleFilesCommand(IEnumerable<DataRepositoryItem> selectedItems)
        {
            var newCommand = new RelayCommand(() =>
            {
                DataRepositoryItem dataRepositoryItem = selectedItems.FirstOrDefault();
                if (dataRepositoryItem != null)
                {
                    DataRepositoryItem rootItem = dataRepositoryItem.GetRootDataRepositoryItem();
                    rootItem.SourcePlugin.DropOnMap(selectedItems);
                }
            });
            return newCommand;
        }

        internal static void PlaceFilesOnMap(IEnumerable<string> allFiles)
        {
            List<Layer> resultsLayers = new List<Layer>();
            var groupedFileDataItems = allFiles.GroupBy(item => Path.GetExtension(item));
            foreach (var fileDataItems in groupedFileDataItems)
            {
                var extension = fileDataItems.Key.ToUpperInvariant();
                var matchingLayerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>()
 .FirstOrDefault(tmpPlugin => tmpPlugin.ExtensionFilter.ToUpperInvariant().Contains(extension));
                if (matchingLayerPlugin != null)
                {
                    var getLayersParameters = new GetLayersParameters();
                    foreach (var item in fileDataItems)
                    {
                        getLayersParameters.LayerUris.Add(new Uri(item));
                    }
                    var layers = matchingLayerPlugin.GetLayers(getLayersParameters);
                    resultsLayers.AddRange(layers);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataRepositoryCannotFindLayerProviderWarningLabel"), GisEditor.LanguageManager.GetStringResource("DataRepositoryWarningLabel"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }

            }
            if (resultsLayers.Count > 0)
            {
                GisEditor.ActiveMap.AddLayersBySettings(resultsLayers, true);
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(resultsLayers, RefreshArgsDescription.PlaceFilesOnMapDescription));
            }
        }

        internal static void PlaceFilesOnMap(FileDataRepositoryItem fileDataItem)
        {
            PlaceFilesOnMap(new string[] { fileDataItem.FileInfo.FullName });
        }

        internal static void PlaceFilesOnMap(IEnumerable<FileDataRepositoryItem> allFileDataItems)
        {
            var allFiles = allFileDataItems.Select(item => item.FileInfo.FullName);
            PlaceFilesOnMap(allFiles);
        }
    }
}
