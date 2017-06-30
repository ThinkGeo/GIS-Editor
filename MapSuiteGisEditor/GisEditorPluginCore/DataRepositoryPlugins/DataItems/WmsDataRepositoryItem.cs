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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class WmsDataRepositoryItem : DataRepositoryItem, IStorableSettings
    {
        private bool isRoot;
        private ObservableCollection<string> layerNames;
        private string url;
        private string userName;
        private string password;
        private string parameters;
        private ObservableCollection<string> formats;
        private ObservableCollection<string> styles;
        private string selectedFormat;
        private string selectedStyle;
        private ObservableCollection<DataRepositoryItem> children;

        public WmsDataRepositoryItem()
            : this(null, null)
        {
        }

        public WmsDataRepositoryItem(string name, string url)
            : this(name, null, url, null, null, null, null, null, null, null)
        {
        }

        public WmsDataRepositoryItem(string name, ObservableCollection<string> layerNames, string url, string userName, string password, string parameters, ObservableCollection<string> formats, ObservableCollection<string> styles, string selectedFormat, string selectedStyle)
        {
            Name = name;
            isRoot = layerNames != null && layerNames.Count > 0;
            this.url = url;
            this.layerNames = layerNames ?? new ObservableCollection<string>();
            this.userName = userName;
            this.password = password;
            this.parameters = parameters;
            this.formats = formats ?? new ObservableCollection<string>();
            this.styles = styles ?? new ObservableCollection<string>();
            this.selectedFormat = selectedFormat;
            this.selectedStyle = selectedStyle;
            if (isRoot) Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_wms_node.png", UriKind.RelativeOrAbsolute));
            children = new ObservableCollection<DataRepositoryItem>();
            AddChildren();
            AddMenuItems();
        }

        public string Url
        {
            get { return url; }
        }

        public ObservableCollection<string> LayerNames
        {
            get { return layerNames; }
        }

        protected override bool IsLoadableCore
        {
            get { return !isRoot; }
        }

        //protected override ObservableCollection<DataRepositoryItem> GetChildrenCore()
        //{
        //    return children;
        //}

        protected override void LoadCore()
        {
            WmsRasterLayer layer = new WmsRasterLayer(new Uri(Url)) { Name = Name };
            layer.ActiveLayerNames.Add(Name);
            layer.InitializeProj4Projection(GisEditor.ActiveMap.DisplayProjectionParameters);
            var layers = new Layer[] { layer };
            GisEditor.ActiveMap.AddLayersBySettings(layers);
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadCoreDescription));
        }

        public StorableSettings GetSettings()
        {
            var storableSettings = new StorableSettings();
            storableSettings.GlobalSettings["Wms"] = ToXml().ToString();
            return storableSettings;
        }

        public void ApplySettings(StorableSettings settings)
        {
            if (settings.GlobalSettings.ContainsKey("Wms"))
            {
                try
                {
                    var xElement = XElement.Parse(settings.GlobalSettings["Wms"]);
                    FromXml(xElement);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        public SettingUserControl GetSettingsUI()
        {
            return null;
        }

        private XElement GetXElementByCollection(IEnumerable<string> items, string elementName)
        {
            XElement x = new XElement(elementName);
            foreach (var item in items)
            {
                x.Add(new XElement("Item", item));
            }
            return x;
        }

        private void AddToCollection(XElement xElement, string elementName, ObservableCollection<string> collection)
        {
            var element = xElement.Element(elementName);
            if (element != null && element.HasElements)
            {
                foreach (var item in element.Descendants("Item"))
                {
                    collection.Add(item.Value);
                }
            }
        }

        private string GetAttributeValue(XElement xElement, string attributeName)
        {
            var attribute = xElement.Attribute(attributeName);
            return attribute == null ? "" : attribute.Value;
        }

        private string GetElementValue(XElement xElement, string elementName)
        {
            var resultXElement = xElement.Element(elementName);
            return resultXElement == null ? "" : resultXElement.Value;
        }

        private void AddChildren()
        {
            if (layerNames != null)
            {
                children.Clear();
                foreach (var layerName in layerNames)
                {
                    children.Add(new WmsDataRepositoryItem(layerName, url));
                }
            }
        }

        private void AddMenuItems()
        {
            if (isRoot)
            {
                ContextMenu = new ContextMenu();
                MenuItem editSettingItem = new MenuItem();
                editSettingItem.Header = GisEditor.LanguageManager.GetStringResource("WmsDataRepositoryItemEditWmsHeader");
                editSettingItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_edit.png", UriKind.RelativeOrAbsolute)) };
                editSettingItem.Command = new RelayCommand(() =>
                {
                    WmsRasterLayerConfigWindow wmsWindow = new WmsRasterLayerConfigWindow();
                    wmsWindow.Title = GisEditor.LanguageManager.GetStringResource("WmsDataRepositoryItemEditWMSRasterLayerHeader");
                    wmsWindow.ViewModel.AddToDataRepositoryVisibility = Visibility.Collapsed;
                    wmsWindow.ViewModel.Name = Name;
                    wmsWindow.ViewModel.WmsServerUrl = Url;
                    if (wmsWindow.ShowDialog().GetValueOrDefault())
                    {
                        WmsDataRepositoryItem newItem = new WmsDataRepositoryItem(
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

                        WmsDataRepositoryPlugin wmsDataRepositoryPlugin = GisEditor.DataRepositoryManager.GetActiveDataRepositoryPlugins<WmsDataRepositoryPlugin>().FirstOrDefault();
                        if (wmsDataRepositoryPlugin != null && wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.Contains(this))
                        {
                            int index = wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.IndexOf(this);
                            wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.Insert(index, newItem);
                            wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.RemoveAt(index + 1);
                            GisEditor.UIManager.RefreshPlugins(new DataRepositoryUIRefreshArgs(new DataRepositoryPlugin[] { wmsDataRepositoryPlugin }));
                        }
                    }
                });

                MenuItem removeItem = new MenuItem();
                removeItem.Header = GisEditor.LanguageManager.GetStringResource("WmsDataRepositoryItemRemoveRepositoryHeader");
                removeItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_remove_item.png", UriKind.RelativeOrAbsolute)) };
                removeItem.Command = new RelayCommand(() =>
                {
                    WmsDataRepositoryPlugin wmsDataRepositoryPlugin = GisEditor.DataRepositoryManager.GetActiveDataRepositoryPlugins<WmsDataRepositoryPlugin>().FirstOrDefault();
                    if (wmsDataRepositoryPlugin != null && wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.Contains(this))
                    {
                        wmsDataRepositoryPlugin.RootDataRepositoryItem.Children.Remove(this);
                        GisEditor.UIManager.RefreshPlugins(new DataRepositoryUIRefreshArgs(new DataRepositoryPlugin[] { wmsDataRepositoryPlugin }));
                    }
                });

                ContextMenu.Items.Add(editSettingItem);
                ContextMenu.Items.Add(removeItem);
            }
        }

        private void FromXml(XElement xElement)
        {
            Name = GetAttributeValue(xElement, "Name");
            AddToCollection(xElement, "LayerNames", layerNames);
            isRoot = layerNames.Count > 0;
            if (isRoot) Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dr_wms_node.png", UriKind.RelativeOrAbsolute));
            url = GetElementValue(xElement, "URL");
            userName = GetElementValue(xElement, "UserName");
            password = GetElementValue(xElement, "Password");
            parameters = GetElementValue(xElement, "Parameters");
            selectedFormat = GetElementValue(xElement, "SelectedFormat");
            selectedStyle = GetElementValue(xElement, "SelectedStyle");
            AddToCollection(xElement, "Formats", formats);
            AddToCollection(xElement, "Styles", styles);

            AddChildren();
            AddMenuItems();
        }

        private XElement ToXml()
        {
            XElement xElement = new XElement("Wms", new XAttribute("Name", Name));
            if (layerNames.Count > 0)
                xElement.Add(GetXElementByCollection(layerNames, "LayerNames"));
            xElement.Add(new XElement("URL", url));
            if (!string.IsNullOrEmpty(userName)) xElement.Add(new XElement("UserName", userName));
            if (!string.IsNullOrEmpty(password)) xElement.Add(new XElement("Password", password));
            if (!string.IsNullOrEmpty(parameters)) xElement.Add(new XElement("Parameters", parameters));
            if (!string.IsNullOrEmpty(selectedFormat)) xElement.Add(new XElement("SelectedFormat", selectedFormat));
            if (!string.IsNullOrEmpty(selectedStyle)) xElement.Add(new XElement("SelectedStyle", selectedStyle));
            if (formats.Count > 0)
                xElement.Add(GetXElementByCollection(formats, "Formats"));
            if (styles.Count > 0)
                xElement.Add(GetXElementByCollection(styles, "Styles"));

            return xElement;
        }
    }
}
