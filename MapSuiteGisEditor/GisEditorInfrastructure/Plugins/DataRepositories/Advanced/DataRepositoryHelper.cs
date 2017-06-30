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
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
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
    }
}
