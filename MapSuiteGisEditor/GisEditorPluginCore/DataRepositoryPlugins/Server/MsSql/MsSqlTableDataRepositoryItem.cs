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
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class MsSqlTableDataRepositoryItem : DataRepositoryItem
    {
        private string idColumnName;
        private string tableName;
        private MsSql2008FeatureLayerInfo layerInfo;

        public MsSqlTableDataRepositoryItem()
        {
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/table.png", UriKind.RelativeOrAbsolute));

            MenuItem propertyItem = new MenuItem();
            propertyItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/properties.png", 16, 16);
            propertyItem.Header = "Properties";
            propertyItem.Command = new RelayCommand(() =>
            {
                LayerPlugin matchingLayerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>()
    .FirstOrDefault(tmpPlugin => tmpPlugin.Name.Equals(GisEditor.LanguageManager.GetStringResource("MsSql2008FeatureLayerPluginName"), StringComparison.OrdinalIgnoreCase));

                var newLayer = GetMsSql2008FeatureLayers().FirstOrDefault();
                if (newLayer != null)
                {
                    UserControl userControl = matchingLayerPlugin.GetPropertiesUI(newLayer);

                    Window propertiesDockWindow = new Window()
                    {
                        Content = userControl,
                        Title = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties"),
                        SizeToContent = SizeToContent.WidthAndHeight,
                        ResizeMode = System.Windows.ResizeMode.NoResize,
                        Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style
                    };

                    propertiesDockWindow.ShowDialog();
                }
            });
            ContextMenu.Items.Add(propertyItem);
        }

        public MsSql2008FeatureLayerInfo LayerInfo
        {
            get { return layerInfo; }
            set { layerInfo = value; }
        }

        public string TableName
        {
            get { return tableName; }
            set { tableName = value; }
        }

        public string SchemaName
        {
            get;
            set;
        }

        public string DatabaseName
        { get; set; }

        protected override bool IsLeafCore
        {
            get
            {
                return true;
            }
        }

        protected override bool IsLoadableCore
        {
            get
            {
                return true;
            }
        }

        public string IdColumnName
        {
            get { return idColumnName; }
            set { idColumnName = value; }
        }

        protected override void LoadCore()
        {
            var newLayers = GetMsSql2008FeatureLayers();

            if (newLayers != null && newLayers.Count > 0)
            {
                GisEditor.ActiveMap.AddLayersToActiveOverlay(newLayers);
                GisEditor.UIManager.RefreshPlugins();
            }
        }

        private Collection<MsSqlFeatureLayer> GetMsSql2008FeatureLayers()
        {
            if (LayerInfo != null)
            {
                MsSqlServerDataRepositoryItem serverItem = Parent.Parent.Parent.Parent.Parent as MsSqlServerDataRepositoryItem;
                if (serverItem != null)
                {
                    LayerInfo.TableName = TableName;
                    LayerInfo.DatabaseName = DatabaseName;
                    LayerInfo.SchemaName = SchemaName;
                    Collection<string> newColumnNames = LayerInfo.CollectColumnsFromTable();
                    FeatureIdColumnWindow featureIdColumnWindow = new FeatureIdColumnWindow(newColumnNames);
                    featureIdColumnWindow.Owner = Application.Current.MainWindow;
                    featureIdColumnWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    if (featureIdColumnWindow.ShowDialog().GetValueOrDefault())
                    {
                        GetLayersParameters layerParameters = new GetLayersParameters();
                        layerParameters.CustomData["TableName"] = TableName;
                        layerParameters.CustomData["SchemaName"] = SchemaName;
                        layerParameters.CustomData["DatabaseName"] = DatabaseName;
                        layerParameters.CustomData["IdColumn"] = featureIdColumnWindow.FeatureIdColumn;
                        layerParameters.CustomData["ServerName"] = serverItem.Name;
                        layerParameters.CustomData["UserName"] = serverItem.UserName;
                        layerParameters.CustomData["Password"] = serverItem.Password;

                        Collection<MsSqlFeatureLayer> newLayers = new Collection<MsSqlFeatureLayer>();
                        try
                        {
                            newLayers = GisEditor.LayerManager.GetLayers<MsSqlFeatureLayer>(layerParameters);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Unable to Load Layer(s)", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        if (newLayers != null && newLayers.Count > 0)
                        {
                            return newLayers;
                        }
                    }
                }
            }
            return new Collection<MsSqlFeatureLayer>();
        }
    }
}