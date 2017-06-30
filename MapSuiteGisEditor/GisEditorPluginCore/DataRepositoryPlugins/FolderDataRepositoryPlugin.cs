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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FolderDataRepositoryPlugin : DataRepositoryPlugin
    {
        private const string dataFolderName = "Sample Data";
        private string dataFolderPath;
        private DataRepositoryItem rootDataRepositoryItem;

        public FolderDataRepositoryPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("FolderDataRepositoryPluginName");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_datafolders_parent.png", UriKind.RelativeOrAbsolute));
            Content = GetDataRepositoryContentUserControl();
            CanRefreshDynamically = true;
            InitializeContextMenu();
            Index = DataRepositoryOrder.Folder;
            dataFolderPath = Path.Combine(FolderHelper.GetGisEditorFolder(), dataFolderName);
        }

        protected override bool CanDropOnMapCore
        {
            get { return true; }
        }

        protected override void DropOnMapCore(IEnumerable<DataRepositoryItem> dataRepositoryItems)
        {
            DataRepositoryHelper.PlaceFilesOnMap(dataRepositoryItems.OfType<FileDataRepositoryItem>());
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            XElement xElement = new XElement("Items");
            foreach (var item in RootDataRepositoryItem.Children.OfType<FolderDataRepositoryItem>())
            {
                var itemXElement = new XElement("Item", item.FolderInfo.FullName);
                itemXElement.SetAttributeValue("DisplayName", item.Name);
                xElement.Add(itemXElement);
            }

            if (xElement.HasElements)
            {
                settings.GlobalSettings["Folders"] = xElement.ToString();
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("Folders"))
            {
                try
                {
                    RootDataRepositoryItem.Children.Clear();
                    XElement xElement = XElement.Parse(settings.GlobalSettings["Folders"]);
                    foreach (var item in xElement.Descendants("Item"))
                    {
                        if (Directory.Exists(item.Value))
                        {
                            var dataRepositoryItem = new FolderDataRepositoryItem(item.Value, true);
                            dataRepositoryItem.Parent = RootDataRepositoryItem;
                            RootDataRepositoryItem.Children.Add(dataRepositoryItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }

            if (Directory.Exists(dataFolderPath) && RootDataRepositoryItem.Children.All(i => !((FolderDataRepositoryItem)i).FolderInfo.FullName.Equals(dataFolderPath, StringComparison.InvariantCultureIgnoreCase)))
            {
                RootDataRepositoryItem.Children.Add(new FolderDataRepositoryItem(dataFolderPath, true));
            }
        }

        protected override DataRepositoryItem CreateDataRepositoryItemCore()
        {
            FolderDataRepositoryItem dataItem = null;
            FolderHelper.OpenFolderBrowserDialog((tmpDialog, tmpResult) =>
            {
                if (tmpResult == System.Windows.Forms.DialogResult.OK)
                {
                    var folderPaths = RootDataRepositoryItem.Children.Select(c => ((FolderDataRepositoryItem)c).FolderInfo.FullName).ToList();
                    if (folderPaths.Contains(tmpDialog.SelectedPath))
                    {
                        MessageBox.Show(string.Format(CultureInfo.InvariantCulture, GisEditor.LanguageManager.GetStringResource("DataRepositoryAddDataFolderWarningLabel"), tmpDialog.SelectedPath), "Alert");
                    }
                    else
                    {
                        dataItem = new FolderDataRepositoryItem(tmpDialog.SelectedPath, true);
                        dataItem.Parent = RootDataRepositoryItem;
                    }
                }
            });
            return dataItem;
        }

        private void InitializeContextMenu()
        {
            ContextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            menuItem.Header = GisEditor.LanguageManager.GetStringResource("DataRepositoryAddNewDataFolderMenuItemLabel");
            menuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_add_data.png", UriKind.RelativeOrAbsolute)) };
            ContextMenu.Items.Add(menuItem);
        }

        protected override DataRepositoryItem RootDataRepositoryItemCore
        {
            get
            {
                return rootDataRepositoryItem ?? (rootDataRepositoryItem = DataRepositoryHelper.CreateRootDataRepositoryItem(this));
            }
        }

        private UserControl GetDataRepositoryContentUserControl()
        {
            DataRepositoryContentUserControl userControl = new DataRepositoryContentUserControl();
            userControl.DataRepositoryItemMouseDoubleClick += UserControl_DataRepositoryItemMouseDoubleClick;

            string header1 = GisEditor.LanguageManager.GetStringResource("FolderDataRepositoryUserControlTypeText");
            DataRepositoryGridColumn column1 = new DataRepositoryGridColumn(header1, 70, di => di.Category);

            string header2 = GisEditor.LanguageManager.GetStringResource("CommonSizeText");
            DataRepositoryGridColumn column2 = new DataRepositoryGridColumn(header2, 100);
            column2.CellContentConvertHandler = di =>
            {
                FileDataRepositoryItem fileDataRepositoryItem = di as FileDataRepositoryItem;
                if (fileDataRepositoryItem != null)
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0:N0} KB"
                         , (int)Math.Ceiling((double)fileDataRepositoryItem.FileInfo.Length / 1024d));
                }
                else return Binding.DoNothing;
            };

            userControl.Columns.Add(column1);
            userControl.Columns.Add(column2);

            return userControl;
        }

        private void UserControl_DataRepositoryItemMouseDoubleClick(object sender, ThinkGeo.MapSuite.GisEditor.DataRepositoryItemMouseDoubleClickUserControlEventArgs e)
        {
            FolderDataRepositoryItem folderDataRepositoryItem = e.SelectedDataRepositoryItem as FolderDataRepositoryItem;
            if (folderDataRepositoryItem != null && !Directory.Exists(folderDataRepositoryItem.FolderInfo.FullName))
            {
                System.Windows.Forms.MessageBox.Show(folderDataRepositoryItem.FolderInfo.FullName + " doesn't exist."
                    , "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                e.Handled = true;
            }
        }
    }
}
