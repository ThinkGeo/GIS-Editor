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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PostgreViewDataRepositoryItem : DataRepositoryItem
    {
        private string idColumnName;
        private string viewName;

        public PostgreViewDataRepositoryItem()
        {
            Icon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/dataview.png", UriKind.RelativeOrAbsolute));

            MenuItem propertyItem = new MenuItem();
            propertyItem.Icon = DataRepositoryHelper.GetMenuIcon("/GisEditorPluginCore;component/Images/properties.png", 16, 16);
            propertyItem.Header = "Properties";
            propertyItem.Command = new RelayCommand(() =>
            {
                LayerPlugin matchingLayerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>()
    .FirstOrDefault(tmpPlugin => tmpPlugin.Name.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase));

                PostgreSqlFeatureLayer newLayer = GetPostgreSqlFeatureLayers().FirstOrDefault();
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

        public string ViewName
        {
            get { return viewName; }
            set { viewName = value; }
        }

        public string SchemaName { get; set; }

        public string DatabaseName { get; set; }

        protected override bool IsLeafCore
        {
            get { return true; }
        }

        protected override bool IsLoadableCore
        {
            get { return true; }
        }

        public string IdColumnName
        {
            get { return idColumnName; }
            set { idColumnName = value; }
        }

        protected override void LoadCore()
        {
            Collection<PostgreSqlFeatureLayer> newLayers = GetPostgreSqlFeatureLayers();
            if (newLayers.Count > 0)
            {
                GisEditor.ActiveMap.AddLayersToActiveOverlay(newLayers);
                GisEditor.UIManager.RefreshPlugins();
            }
        }

        private Collection<PostgreSqlFeatureLayer> GetPostgreSqlFeatureLayers()
        {
            PostgreSchemaDataRepositoryItem schemaItem = Parent.Parent as PostgreSchemaDataRepositoryItem;
            if (schemaItem != null)
            {
                DatabaseDataRepositoryItem databaseItem = schemaItem.Parent.Parent as DatabaseDataRepositoryItem;
                if (databaseItem != null)
                {
                    PostgreServerDataRepositoryItem serverItem = databaseItem.Parent as PostgreServerDataRepositoryItem;
                    if (serverItem != null)
                    {
                        string connectionString = PostgreServerDataRepositoryPlugin.GetConnectionString(serverItem, databaseItem.Name);
                        if (string.IsNullOrEmpty(idColumnName))
                        {
                            try
                            {
                                PostgreSqlFeatureSource featureSource = new PostgreSqlFeatureSource(connectionString, Name, "oid");
                                featureSource.SchemaName = schemaItem.SchemaName;
                                featureSource.Open();
                                List<string> newColumnNames = featureSource.GetColumns().Select(c => c.ColumnName).ToList();
                                featureSource.Close();
                                FeatureIdColumnWindow featureIdColumnWindow = new FeatureIdColumnWindow(newColumnNames);
                                featureIdColumnWindow.Owner = Application.Current.MainWindow;
                                featureIdColumnWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                if (featureIdColumnWindow.ShowDialog().Value)
                                {
                                    IdColumnName = featureIdColumnWindow.FeatureIdColumn;
                                }
                            }
                            catch { }
                        }

                        if (string.IsNullOrEmpty(IdColumnName)) return new Collection<PostgreSqlFeatureLayer>();

                        string[] tableInfo = viewName.Split(':');
                        string url = "postgreSqlFeatureLayer:{0}|{1}|{2}|{3}";
                        url = String.Format(CultureInfo.InvariantCulture, url, tableInfo[1], tableInfo[0], connectionString, idColumnName);
                        Uri layerUri = new Uri(url);

                        GetLayersParameters layerParameters = new GetLayersParameters();
                        layerParameters.LayerUris.Add(layerUri);
                        Collection<PostgreSqlFeatureLayer> newLayers = GisEditor.LayerManager.GetLayers<PostgreSqlFeatureLayer>(layerParameters);
                        if (newLayers.Count > 0)
                        {
                            return newLayers;
                        }
                    }
                }
            }
            return new Collection<PostgreSqlFeatureLayer>();
        }
    }
}