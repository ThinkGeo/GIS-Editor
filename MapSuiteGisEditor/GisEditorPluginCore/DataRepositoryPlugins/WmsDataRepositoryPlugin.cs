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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class WmsDataRepositoryPlugin : DataRepositoryPlugin
    {
        public WmsDataRepositoryPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("WmsDataRepositeryPluginWmsServersName");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_wms_node.png", UriKind.RelativeOrAbsolute));
            Content = GetDataRepositoryContentUserControl();
            Index = DataRepositoryOrder.Wms;
            InitContextMenu();
        }

        protected override DataRepositoryItem CreateDataRepositoryItemCore()
        {
            WmsDataRepositoryItem wmsDataItem = null;
            WmsRasterLayerConfigWindow wmsWindow = new WmsRasterLayerConfigWindow();
            wmsWindow.ViewModel.AddToDataRepositoryVisibility = Visibility.Collapsed;
            if (wmsWindow.ShowDialog().GetValueOrDefault())
            {
                var names = RootDataRepositoryItem.Children.Select(c => c.Name).ToList();
                if (names.Contains(wmsWindow.ViewModel.Name))
                {
                    MessageBox.Show(string.Format(CultureInfo.InvariantCulture, GisEditor.LanguageManager.GetStringResource("WmsDataRepositeryPluginWmsAddedText"), wmsWindow.ViewModel.Name), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
                }
                else
                {
                    wmsDataItem = new WmsDataRepositoryItem(
                        wmsWindow.ViewModel.Name,
                        new ObservableCollection<string>(wmsWindow.ViewModel.AvailableLayers.Select(l => l.Name)),
                        wmsWindow.ViewModel.WmsServerUrl,
                        wmsWindow.ViewModel.UserName,
                        wmsWindow.ViewModel.Password,
                        wmsWindow.ViewModel.Parameters,
                        wmsWindow.ViewModel.Formats,
                        wmsWindow.ViewModel.Styles,
                        wmsWindow.ViewModel.SelectedFormat,
                        wmsWindow.ViewModel.SelectedStyle);
                }
            }
            return wmsDataItem;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            var allWms = settings.GlobalSettings.Where(s => s.Key.Contains("Wms")).ToArray();
            if (allWms.Length > 0)
            {
                RootDataRepositoryItem.Children.Clear();
                foreach (var item in allWms)
                {
                    WmsDataRepositoryItem wmsDataItem = new WmsDataRepositoryItem();
                    var tmpSettings = new StorableSettings();
                    tmpSettings.GlobalSettings["Wms"] = item.Value;
                    wmsDataItem.ApplySettings(tmpSettings);
                    RootDataRepositoryItem.Children.Add(wmsDataItem);
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            int index = 0;
            foreach (var item in RootDataRepositoryItem.Children.OfType<WmsDataRepositoryItem>())
            {
                settings.GlobalSettings["Wms" + (++index)] = item.GetSettings().GlobalSettings["Wms"];
            }
            return settings;
        }

        private void InitContextMenu()
        {
            MenuItem addItem = new MenuItem();
            addItem.Header = GisEditor.LanguageManager.GetStringResource("WmsDataRepositeryPluginAddNewWMSServerHeader");
            addItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_add_data.png", UriKind.RelativeOrAbsolute)) };
            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(addItem);
        }

        private UserControl GetDataRepositoryContentUserControl()
        {
            DataRepositoryContentUserControl userControl = new DataRepositoryContentUserControl();

            string header1 = GisEditor.LanguageManager.GetStringResource("FolderDataRepositoryUserControlTypeText");
            DataRepositoryGridColumn column1 = new DataRepositoryGridColumn(header1, 70, di => di.Category);

            string header2 = "Url";
            DataRepositoryGridColumn column2 = new DataRepositoryGridColumn(header2, 100);
            column2.CellContentConvertHandler = di =>
            {
                WmsDataRepositoryItem wmsDataRepositoryItem = di as WmsDataRepositoryItem;
                if (wmsDataRepositoryItem != null)
                {
                    return wmsDataRepositoryItem.Url;
                }
                else return Binding.DoNothing;
            };

            userControl.Columns.Add(column1);
            userControl.Columns.Add(column2);

            return userControl;
        }
    }
}
