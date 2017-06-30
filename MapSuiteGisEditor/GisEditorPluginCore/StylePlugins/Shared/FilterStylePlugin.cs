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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FilterStylePlugin : StylePlugin
    {
        public FilterStylePlugin()
            : base()
        {
            Name = GisEditor.LanguageManager.GetStringResource("FilterStylePluginName");
            Description = GisEditor.LanguageManager.GetStringResource("FilterStylePluginDescription");
            Index = StylePluginOrder.FilterStyle;
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png", UriKind.RelativeOrAbsolute));
            StyleCategories = StyleCategories.Composite;
        }

        protected override Style GetDefaultStyleCore()
        {
            return new FilterStyle();
        }

        //protected override StyleEditResult EditStyleCore(Style style, StyleArguments arguments)
        //{
        //    return StylePluginHelper.CustomizeStyle<FilterStyle>(style, arguments);
        //}

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);

            if (parameters.LayerListItem.ConcreteObject is FilterStyle)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "--";
                menuItems.Add(menuItem);

                MenuItem viewDataMenuItem = new MenuItem();
                viewDataMenuItem.Header = "View filtered data";
                viewDataMenuItem.Click += ViewDataMenuItem_Click;
                viewDataMenuItem.Tag = parameters.LayerListItem;
                viewDataMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png", UriKind.RelativeOrAbsolute)) };
                menuItems.Add(viewDataMenuItem);

                MenuItem zoomToFilterMenuItem = new MenuItem();
                zoomToFilterMenuItem.Header = GisEditor.LanguageManager.GetStringResource("FilterStylePluginZoomtofilter");
                zoomToFilterMenuItem.Tag = parameters.LayerListItem;
                zoomToFilterMenuItem.Click += ZoomToFilterMenuItem_Click;
                zoomToFilterMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/zoomto.png", UriKind.RelativeOrAbsolute)) };
                menuItems.Add(zoomToFilterMenuItem);

                MenuItem exportItem = new MenuItem();
                exportItem.Header = "Export";
                exportItem.Tag = parameters.LayerListItem;
                exportItem.Click += ExportItem_Click;
                exportItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Export.png", UriKind.RelativeOrAbsolute)) };
                menuItems.Add(exportItem);
            }

            return menuItems;
        }

        private void ExportItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            LayerListItem layerListItem = (LayerListItem)menuItem.Tag;
            LayerListItem featureLayerItem = layerListItem;
            while (!(featureLayerItem.ConcreteObject is FeatureLayer))
            {
                featureLayerItem = featureLayerItem.Parent;
            }
            GisEditor.LayerListManager.SelectedLayerListItem = featureLayerItem;
            FeatureLayer selectedLayer = featureLayerItem.ConcreteObject as FeatureLayer;
            if (selectedLayer != null)
            {
                Collection<Feature> resultFeatures = new Collection<Feature>();
                selectedLayer.SafeProcess(() =>
                {
                    resultFeatures = selectedLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                });

                FilterStyle filterStyle = (FilterStyle)layerListItem.ConcreteObject;
                foreach (var condition in filterStyle.Conditions)
                {
                    resultFeatures = condition.GetMatchingFeatures(resultFeatures);
                }

                if (resultFeatures.Count > 0)
                {
                    Collection<FeatureSourceColumn> columns = selectedLayer.FeatureSource.GetColumns();

                    FeatureLayerPlugin sourcePlugin = GisEditor.LayerManager.GetLayerPlugins(selectedLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                    if (sourcePlugin != null)
                    {
                        WellKnownType type = selectedLayer.FeatureSource.GetFirstFeaturesWellKnownType();
                        ExportToShapeFile(resultFeatures, columns, sourcePlugin, type);
                    }
                }
            }
        }

        private void ExportToShapeFile(Collection<Feature> resultFeatures, Collection<FeatureSourceColumn> columns, FeatureLayerPlugin sourceLayerPlugin, WellKnownType type)
        {
            int count = resultFeatures.Count;
            if (count > 0)
            {
                ShapeFileFeatureLayerPlugin targetLayerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<ShapeFileFeatureLayerPlugin>().FirstOrDefault();
                FeatureLayer resultLayer = null;
                GetLayersParameters getLayerParameters = new GetLayersParameters();

                if (targetLayerPlugin != null)
                {
                    ConfigureFeatureLayerParameters parameters = targetLayerPlugin.GetCreateFeatureLayerParameters(columns);
                    if (parameters != null && sourceLayerPlugin != null)
                    {
                        bool needColumns = false;
                        Collection<string> tempColumns = new Collection<string>();
                        if (parameters.CustomData.ContainsKey("Columns"))
                        {
                            tempColumns = parameters.CustomData["Columns"] as Collection<string>;
                        }
                        else
                        {
                            needColumns = true;
                        }

                        var featureColumns = columns.Where(c => needColumns || tempColumns.Contains(c.ColumnName));
                        if (targetLayerPlugin.CanCreateFeatureLayerWithSourceColumns(sourceLayerPlugin))
                        {
                            foreach (var item in featureColumns)
                            {
                                FeatureSourceColumn column = new FeatureSourceColumn(item.ColumnName, item.TypeName, item.MaxLength);
                                if (column.TypeName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    column.TypeName = "Character";
                                }
                                parameters.AddedColumns.Add(column);
                            }
                        }
                        else
                        {
                            var geoColumns = sourceLayerPlugin.GetIntermediateColumns(featureColumns);
                            foreach (var item in geoColumns)
                            {
                                if (item.TypeName.Equals("c", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    item.TypeName = "Character";
                                }
                                parameters.AddedColumns.Add(item);
                            }
                        }


                        parameters.WellKnownType = type;
                        //parameters.CustomData["SourceLayer"] = featureLayer;

                        getLayerParameters.LayerUris.Add(parameters.LayerUri);
                        foreach (var item in parameters.CustomData)
                        {
                            getLayerParameters.CustomData[item.Key] = item.Value;
                        }

                        Proj4Projection proj4 = new Proj4Projection();
                        proj4.InternalProjectionParametersString = parameters.Proj4ProjectionParametersString;
                        proj4.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                        proj4.SyncProjectionParametersString();
                        proj4.Open();

                        foreach (var item in resultFeatures)
                        {
                            Feature feature = proj4.ConvertToInternalProjection(item);
                            parameters.AddedFeatures.Add(feature);
                        }
                        if (parameters.MemoColumnConvertMode == MemoColumnConvertMode.ToCharacter)
                        {
                            foreach (var item in parameters.AddedColumns.Where(c => c.TypeName.Equals("Memo", StringComparison.InvariantCultureIgnoreCase)).ToList())
                            {
                                //parameters.AddedColumns.Remove(item);
                                item.TypeName = "Character";
                                item.MaxLength = 254;
                                DbfColumn tmpDbfColumn = item as DbfColumn;
                                if (tmpDbfColumn != null)
                                {
                                    tmpDbfColumn.ColumnType = DbfColumnType.Character;
                                    tmpDbfColumn.Length = 254;
                                }
                            }
                        }
                        resultLayer = targetLayerPlugin.CreateFeatureLayer(parameters);

                        resultLayer.FeatureSource.Projection = proj4;
                        resultLayer = targetLayerPlugin.GetLayers(getLayerParameters).FirstOrDefault() as FeatureLayer;
                    }
                }

                if (resultLayer != null)
                {
                    GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.YesNo);
                    messageBox.Owner = Application.Current.MainWindow;
                    messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    messageBox.Title = GisEditor.LanguageManager.GetStringResource("NavigatePluginAddToMap");
                    messageBox.Message = GisEditor.LanguageManager.GetStringResource("DoYouWantToAddToMap");
                    messageBox.ErrorMessage = string.Empty;
                    if (messageBox.ShowDialog().Value)
                    {
                        GisEditor.ActiveMap.AddLayerToActiveOverlay(resultLayer);
                        GisEditor.ActiveMap.RefreshActiveOverlay();
                        RefreshArgs refreshArgs = new RefreshArgs(this, "LoadToMapCore");
                        InvokeRefreshPlugins(GisEditor.UIManager, refreshArgs);
                        GisEditor.ActiveMap.Refresh();
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("There's no features in this layer.", "Export File");
            }
        }

        private static void InvokeRefreshPlugins(UIPluginManager uiPluginManager, RefreshArgs refreshArgs = null)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    uiPluginManager.RefreshPlugins(refreshArgs);
                });
            }
        }

        private void ViewDataMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            LayerListItem layerListItem = (LayerListItem)menuItem.Tag;
            LayerListItem tempItem = layerListItem;
            while (!(tempItem.ConcreteObject is FeatureLayer))
            {
                tempItem = tempItem.Parent;
            }
            FeatureLayer selectedLayer = tempItem.ConcreteObject as FeatureLayer;
            if (selectedLayer != null)
            {
                FilterStyle filterStyle = (FilterStyle)layerListItem.ConcreteObject;
                FilterStyleViewModel.ShowFilteredData(selectedLayer, filterStyle.Conditions, string.Format("{0} {1}", selectedLayer.Name, layerListItem.Name));
            }
        }

        private void ZoomToFilterMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            LayerListItem layerListItem = (LayerListItem)menuItem.Tag;
            LayerListItem tempItem = layerListItem;
            while (!(tempItem.ConcreteObject is FeatureLayer))
            {
                tempItem = tempItem.Parent;
            }
            FeatureLayer selectedLayer = tempItem.ConcreteObject as FeatureLayer;
            if (selectedLayer != null)
            {
                Collection<Feature> resultFeatures = new Collection<Feature>();
                selectedLayer.SafeProcess(() =>
                {
                    resultFeatures = selectedLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                });

                FilterStyle filterStyle = (FilterStyle)layerListItem.ConcreteObject;
                foreach (var condition in filterStyle.Conditions)
                {
                    resultFeatures = condition.GetMatchingFeatures(resultFeatures);
                }
                if (resultFeatures.Count > 0)
                {
                    RectangleShape boundingBox = ExtentHelper.GetBoundingBoxOfItems(resultFeatures);
                    GisEditor.ActiveMap.CurrentExtent = boundingBox;
                    GisEditor.ActiveMap.Refresh();
                }
            }
        }

        protected override StyleLayerListItem GetStyleLayerListItemCore(Style style)
        {
            var filterStyle = style as FilterStyle;
            if (filterStyle != null) return new FilterStyleItem(filterStyle);
            return null;
        }
    }
}