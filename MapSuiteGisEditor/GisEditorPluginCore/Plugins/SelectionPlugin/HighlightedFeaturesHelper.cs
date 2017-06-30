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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class HighlightedFeaturesHelper
    {
        public static MenuItem GetHighlightedFeaturesMenuitem()
        {
            MenuItem highlightedFeatureMenuItem = new MenuItem();
            highlightedFeatureMenuItem.Header = GisEditor.LanguageManager.GetStringResource("HighlightedFeaturesHelperHighLightMenuHeaderContent");
            highlightedFeatureMenuItem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/zoom_to_selected.png", UriKind.RelativeOrAbsolute)) };

            //IEnumerable<FeatureLayer> layers = GisEditor.ActiveMap.GetFeatureLayers();
            Collection<FeatureLayer> layers = GisEditor.SelectionManager.GetSelectionOverlay().TargetFeatureLayers;
            Collection<FeatureLayer> selectedFeaturesLayers = new Collection<FeatureLayer>();
            foreach (FeatureLayer layer in layers)
            {
                Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                if (features.Count > 0) selectedFeaturesLayers.Add(layer);
            }
            MenuItem copyToMenuitem = new MenuItem();
            copyToMenuitem.Header = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryingRibbonGroupCopyToLabelText");

            MenuItem toNewLayerMenuitem = new MenuItem();
            toNewLayerMenuitem.Header = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryingRibbonGroupCopyToTemporaryHeaderText");
            toNewLayerMenuitem.Tag = selectedFeaturesLayers;
            toNewLayerMenuitem.Click += NewLayerMenuitem_Click;

            MenuItem toShapeFileMenuitem = new MenuItem();
            toShapeFileMenuitem.Header = "Shape File";
            toShapeFileMenuitem.Tag = selectedFeaturesLayers;
            toShapeFileMenuitem.Click += ShapeFileMenuitem_Click;

            copyToMenuitem.Items.Add(toNewLayerMenuitem);
            copyToMenuitem.Items.Add(toShapeFileMenuitem);

            if (CheckCopyToEditLayerIsAvailable(selectedFeaturesLayers))
            {
                MenuItem toEditLayerMenuitem = new MenuItem();
                toEditLayerMenuitem.Tag = selectedFeaturesLayers;
                toEditLayerMenuitem.Header = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryingRibbonGroupCopyToEditHeaderText");
                toEditLayerMenuitem.Click += ToEditLayerMenuitem_Click;
                copyToMenuitem.Items.Add(toEditLayerMenuitem);
            }

            InMemoryFeatureLayer[] inMemoryFeatureLayers = GisEditor.ActiveMap.GetFeatureLayers(false).OfType<InMemoryFeatureLayer>().ToArray();
            if (inMemoryFeatureLayers.Length > 0)
            {
                copyToMenuitem.Items.Add(new Separator());
            }
            foreach (InMemoryFeatureLayer layer in inMemoryFeatureLayers)
            {
                MenuItem layerMenuitem = new MenuItem();
                layerMenuitem.Header = layer.Name;
                layerMenuitem.Click += LayerMenuitem_Click;
                copyToMenuitem.Items.Add(layerMenuitem);
            }

            MenuItem saveToKmlMenuitem = new MenuItem();
            saveToKmlMenuitem.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperSaveasvectorKML");
            saveToKmlMenuitem.Click += GoogleEarthHelper.SaveToKmlMenuitemClick;

            MenuItem openWithGoogleMenuitem = new MenuItem();
            openWithGoogleMenuitem.IsEnabled = !string.IsNullOrEmpty(GoogleEarthHelper.GetGoogleEarthInstalledPath());
            openWithGoogleMenuitem.Tag = "GoogleEarth";
            openWithGoogleMenuitem.Header = GisEditor.LanguageManager.GetStringResource("GisEarthHelperOpeninGoogleEarth");
            openWithGoogleMenuitem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png", UriKind.RelativeOrAbsolute)) };
            openWithGoogleMenuitem.Click += GoogleEarthHelper.OpenWithGoogleMenuitemClick;

            MenuItem openWithGoogleProMenuitem = new MenuItem();
            openWithGoogleProMenuitem.IsEnabled = !string.IsNullOrEmpty(GoogleEarthHelper.GetGoogleEarthProInstalledPath());
            openWithGoogleProMenuitem.Tag = "GoogleEarthPro";
            openWithGoogleProMenuitem.Header = "Open in Google Earth Pro";
            openWithGoogleProMenuitem.Icon = new Image() { Width = 16, Height = 16, Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png", UriKind.RelativeOrAbsolute)) };
            openWithGoogleProMenuitem.Click += GoogleEarthHelper.OpenWithGoogleMenuitemClick;

            highlightedFeatureMenuItem.Items.Add(copyToMenuitem);
            highlightedFeatureMenuItem.Items.Add(saveToKmlMenuitem);
            highlightedFeatureMenuItem.Items.Add(openWithGoogleMenuitem);
            highlightedFeatureMenuItem.Items.Add(openWithGoogleProMenuitem);

            return highlightedFeatureMenuItem;
        }

        public static bool CheckCopyToEditLayerIsAvailable(Collection<FeatureLayer> selectedFeaturesLayers)
        {
            return GisEditor.ActiveMap != null && GisEditor.ActiveMap.FeatureLayerEditOverlay != null && GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer != null && selectedFeaturesLayers.Count > 0;// && (selectedFeaturesLayers.Count > 1 || selectedFeaturesLayers[0] != GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer);
        }

        public static void CopyToEditLayer(Collection<FeatureLayer> selectionLayers)
        {
            GisEditorEditInteractiveOverlay overlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (overlay != null)
            {
                FeatureLayer editingLayer = overlay.EditTargetLayer;
                Collection<FeatureLayer> sourceLayers = new Collection<FeatureLayer>(selectionLayers.Except(editingLayer).ToList());
                if (sourceLayers.Count > 1)
                {
                    ChooseExportLayerWindow chooseExportLayerWindow = new ChooseExportLayerWindow(sourceLayers, new Collection<FeatureLayer> { editingLayer });
                    chooseExportLayerWindow.SelectedTargetFeatureLayer = editingLayer;
                    if (chooseExportLayerWindow.ShowDialog().GetValueOrDefault())
                    {
                        AddSelectedFeaturesToEditingLayer(chooseExportLayerWindow.SelectedSourceFeatureLayer);
                    }
                    else return;
                }
                else if (selectionLayers.Count == 1)
                {
                    AddSelectedFeaturesToEditingLayer(selectionLayers[0]);
                }
                else if (selectionLayers.Count == 2 && sourceLayers.Count == 1)
                {
                    AddSelectedFeaturesToEditingLayer(sourceLayers[0]);
                }
            }
        }

        private static void CopyToExistingLayerRibbonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem ribbonMenuItem = sender as MenuItem;
            if (ribbonMenuItem != null)
            {
                InMemoryFeatureLayer inMemoryFeatureLayer = ribbonMenuItem.Tag as InMemoryFeatureLayer;
                if (inMemoryFeatureLayer != null)
                {
                    Collection<FeatureLayer> selectedFeatureLayers = new Collection<FeatureLayer>();
                    foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                    {
                        Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                        if (features.Count > 0) selectedFeatureLayers.Add(layer);
                    }

                    HighlightedFeaturesHelper.CopyToExistingLayer(selectedFeatureLayers, inMemoryFeatureLayer);
                }
            }
        }

        private static void ShapeFileMenuitem_Click(object sender, RoutedEventArgs e)
        {
            var overlay = GisEditor.SelectionManager.GetSelectionOverlay();
            Dictionary<FeatureLayer, GeoCollection<Feature>> group = overlay.GetSelectedFeaturesGroup();
            if (group.Count > 1)
            {
                ChooseExportLayerWindow chooseExportLayerWindow = new ChooseExportLayerWindow(overlay.TargetFeatureLayers, overlay.TargetFeatureLayers);
                if (chooseExportLayerWindow.ShowDialog().GetValueOrDefault())
                {
                    FeatureLayer sourceLayer = chooseExportLayerWindow.SelectedSourceFeatureLayer;
                    sourceLayer.Open();
                    var allColumns = sourceLayer.FeatureSource.GetColumns();
                    Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(sourceLayer);
                    FeatureLayerPlugin plugin = (FeatureLayerPlugin)GisEditor.LayerManager.GetLayerPlugins(sourceLayer.GetType()).FirstOrDefault();
                    ExportToShapeFile(features, allColumns, plugin, sourceLayer.FeatureSource.GetFirstFeaturesWellKnownType());
                }
            }
            else if (group.Count == 1)
            {
                FeatureLayer layer = group.First().Key;
                layer.Open();
                Collection<Feature> features = group.First().Value;
                var allColumns = layer.FeatureSource.GetColumns();
                FeatureLayerPlugin plugin = (FeatureLayerPlugin)GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault();
                ExportToShapeFile(features, allColumns, plugin, layer.FeatureSource.GetFirstFeaturesWellKnownType());
            }
        }

        private static void ExportToShapeFile(Collection<Feature> resultFeatures, Collection<FeatureSourceColumn> columns, FeatureLayerPlugin sourceLayerPlugin, WellKnownType type)
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
                        RefreshArgs refreshArgs = new RefreshArgs(null, "LoadToMapCore");
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

        private static void ToEditLayerMenuitem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Collection<FeatureLayer> selectionLayers = menuItem.Tag as Collection<FeatureLayer>;
            CopyToEditLayer(selectionLayers);
        }

        private static void AddSelectedFeaturesToEditingLayer(FeatureLayer sourceLayer)
        {
            GisEditorEditInteractiveOverlay overlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (overlay != null)
            {
                FeatureLayer editingLayer = overlay.EditTargetLayer;
                Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(sourceLayer);
                foreach (var feature in features)
                {
                    feature.Id = Guid.NewGuid().ToString();
                    overlay.NewFeatureIds.Add(feature.Id);
                    overlay.EditShapesLayer.InternalFeatures.Add(feature);
                }
                sourceLayer.SafeProcess(() =>
                {
                    Collection<FeatureSourceColumn> columns = sourceLayer.FeatureSource.GetColumns();
                    overlay.EditShapesLayer.Columns.Clear();
                    foreach (var column in columns)
                    {
                        overlay.EditShapesLayer.Columns.Add(column);
                    }
                });

                overlay.TakeSnapshot();
                overlay.Refresh();
                GisEditor.SelectionManager.ClearSelectedFeatures(sourceLayer);
            }
        }

        private static void NewLayerMenuitem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Collection<FeatureLayer> selectedFeaturesLayers = (Collection<FeatureLayer>)menuItem.Tag;
            CopyToNewLayer(selectedFeaturesLayers);
        }

        public static void CopyToExistingLayer(Collection<FeatureLayer>
            selectedFeaturesLayers, InMemoryFeatureLayer inMemoryFeatureLayer)
        {
            if (selectedFeaturesLayers.Count > 1)
            {
                var targetLayers = new Collection<FeatureLayer>() { inMemoryFeatureLayer };
                AddFeaturesToSelectedLayer(selectedFeaturesLayers, targetLayers);
                GisEditor.ActiveMap.Refresh(GisEditor.ActiveMap.ActiveOverlay);
                RefreshArgs refreshArgs = new RefreshArgs(GisEditor.ActiveMap, RefreshArgsDescription.AddLayerGroupCommandDescription);
                GisEditor.UIManager.BeginRefreshPlugins(refreshArgs);
                LayerOverlay overlay = GisEditor.ActiveMap.ActiveOverlay as LayerOverlay;
                if (overlay != null)
                {
                    overlay.Invalidate();
                }
            }
            else if (selectedFeaturesLayers.Count == 1)
            {
                AddFeatureToInMemoryFeatureLayer(inMemoryFeatureLayer, selectedFeaturesLayers[0]);
            }
        }

        public static void CopyToNewLayer(Collection<FeatureLayer> selectedFeaturesLayers)
        {
            Collection<FeatureLayer> existLayers = new Collection<FeatureLayer>();
            foreach (var item in GisEditor.ActiveMap.GetFeatureLayers().OfType<InMemoryFeatureLayer>())
            {
                existLayers.Add(item);
            }

            GetLayersParameters parameters = new GetLayersParameters();
            parameters.LayerUris.Add(new Uri("mem:Newlayer"));
            InMemoryFeatureLayerPlugin plugin = GisEditor.LayerManager.GetActiveLayerPlugins<InMemoryFeatureLayerPlugin>().FirstOrDefault();
            InMemoryFeatureLayer inMemoryFeatureLayer = null;
            if (plugin != null)
            {
                inMemoryFeatureLayer = plugin.GetLayers(parameters).OfType<InMemoryFeatureLayer>().FirstOrDefault();
                inMemoryFeatureLayer.Name = "New layer";
                existLayers.Insert(0, inMemoryFeatureLayer);
            }
            if (selectedFeaturesLayers.Count > 1)
            {
                AddFeaturesToSelectedLayer(selectedFeaturesLayers, existLayers);
                GisEditor.ActiveMap.Refresh(GisEditor.ActiveMap.ActiveOverlay);
                RefreshArgs refreshArgs = new RefreshArgs(GisEditor.ActiveMap, RefreshArgsDescription.AddLayerGroupCommandDescription);
                GisEditor.UIManager.BeginRefreshPlugins(refreshArgs);
                LayerOverlay overlay = GisEditor.ActiveMap.ActiveOverlay as LayerOverlay;
                if (overlay != null)
                {
                    overlay.Invalidate();
                }
            }
            else if (selectedFeaturesLayers.Count == 1)
            {
                AddFeatureToInMemoryFeatureLayer(null, selectedFeaturesLayers[0]);
            }
        }

        private static void LayerMenuitem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            string name = (string)menuItem.Header;

            InMemoryFeatureLayer layer = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                             .SelectMany(o => o.Layers).OfType<InMemoryFeatureLayer>()
                             .FirstOrDefault(l => l.Name.Equals(name, System.StringComparison.Ordinal));
            AddFeatureToInMemoryFeatureLayer(layer, null);
        }

        private static void AddFeatureToInMemoryFeatureLayer(InMemoryFeatureLayer inMemoryFeatureLayer, FeatureLayer sourceLayer)
        {
            if (inMemoryFeatureLayer == null)
            {
                string name = "ExportResults";
                string existingName = string.Empty;
                InMemoryFeatureLayer existingLayer = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                                 .SelectMany(o => o.Layers).OfType<InMemoryFeatureLayer>()
                                 .FirstOrDefault(l => l.Name.Equals(name, System.StringComparison.Ordinal));
                if (existingLayer != null)
                {
                    existingName = existingLayer.Name;
                }

                int index = 0;
                while (name.Equals(existingName))
                {
                    index++;
                    name = "ExportResults" + index;
                    existingLayer = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                                 .SelectMany(o => o.Layers).OfType<InMemoryFeatureLayer>()
                                 .FirstOrDefault(l => l.Name.Equals(name, System.StringComparison.Ordinal));
                    existingName = existingLayer != null ? existingLayer.Name : string.Empty;
                }

                GetLayersParameters parameters = new GetLayersParameters();
                parameters.LayerUris.Add(new Uri("mem:" + name));
                InMemoryFeatureLayerPlugin plugin = GisEditor.LayerManager.GetActiveLayerPlugins<InMemoryFeatureLayerPlugin>().FirstOrDefault();
                if (plugin != null)
                {
                    inMemoryFeatureLayer = plugin.GetLayers(parameters).OfType<InMemoryFeatureLayer>().FirstOrDefault();
                }
                GisEditor.ActiveMap.AddLayerToActiveOverlay(inMemoryFeatureLayer, TargetLayerOverlayType.Dynamic);
            }

            //if (sourceLayer != null)
            //{
            //    foreach (var item in sourceLayer.FeatureSource.LinkExpressions)
            //    {
            //        inMemoryFeatureLayer.FeatureSource.LinkExpressions.Add(item);
            //    }
            //    foreach (var item in sourceLayer.FeatureSource.LinkSources)
            //    {
            //        inMemoryFeatureLayer.FeatureSource.LinkSources.Add(item);
            //    }
            //}

            Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures();
            AddFeaturesToInMemoryFeatureLayer(inMemoryFeatureLayer, features);

            GisEditor.ActiveMap.Refresh(GisEditor.ActiveMap.ActiveOverlay);
            RefreshArgs refreshArgs = new RefreshArgs(GisEditor.ActiveMap, RefreshArgsDescription.AddLayerGroupCommandDescription);
            GisEditor.UIManager.BeginRefreshPlugins(refreshArgs);
            LayerOverlay overlay = GisEditor.ActiveMap.ActiveOverlay as LayerOverlay;
            if (overlay != null)
            {
                overlay.Invalidate();
            }
        }

        private static void AddFeaturesToInMemoryFeatureLayer(InMemoryFeatureLayer inMemoryFeatureLayer, Collection<Feature> features)
        {
            inMemoryFeatureLayer.SafeProcess(() =>
            {
                List<string> columnNames = inMemoryFeatureLayer.GetDistinctColumnNames().ToList();
                foreach (var columnName in features[0].ColumnValues.Keys)
                {
                    if (!columnNames.Contains(columnName))
                    {
                        inMemoryFeatureLayer.Columns.Add(new FeatureSourceColumn(columnName, DbfColumnType.Character.ToString(), 100));
                    }
                }

                foreach (Feature feature in features)
                {
                    inMemoryFeatureLayer.EditTools.BeginTransaction();
                    inMemoryFeatureLayer.EditTools.Add(feature);
                    inMemoryFeatureLayer.EditTools.CommitTransaction();
                }
            });
        }

        private static void AddFeaturesToShapeFileFeatureLayer(ShapeFileFeatureLayer shapeFileFeatureLayer, Collection<Feature> features)
        {
            shapeFileFeatureLayer.ReadWriteMode = GeoFileReadWriteMode.ReadWrite;

            shapeFileFeatureLayer.SafeProcess(() =>
            {
                List<string> columnNames = shapeFileFeatureLayer.GetDistinctColumnNames().ToList();
                shapeFileFeatureLayer.FeatureSource.BeginTransaction();
                foreach (var columnName in features[0].ColumnValues.Keys)
                {
                    if (!columnNames.Contains(columnName))
                    {
                        shapeFileFeatureLayer.FeatureSource.AddColumn(new FeatureSourceColumn(columnName, DbfColumnType.Character.ToString(), 100));
                    }
                }
                shapeFileFeatureLayer.FeatureSource.CommitTransaction();

                foreach (Feature feature in features)
                {
                    shapeFileFeatureLayer.EditTools.BeginTransaction();
                    shapeFileFeatureLayer.EditTools.Add(feature);
                    shapeFileFeatureLayer.EditTools.CommitTransaction();
                }
            });
        }

        private static void AddFeaturesToSelectedLayer(Collection<FeatureLayer> selectedFeaturesLayers, Collection<FeatureLayer> existLayers)
        {
            ChooseExportLayerWindow chooseExportLayerWindow = new ChooseExportLayerWindow(selectedFeaturesLayers, existLayers);
            if (chooseExportLayerWindow.ShowDialog().GetValueOrDefault())
            {
                FeatureLayer sourceLayer = chooseExportLayerWindow.SelectedSourceFeatureLayer;
                Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(sourceLayer);
                InMemoryFeatureLayer targetLayer = chooseExportLayerWindow.SelectedTargetFeatureLayer as InMemoryFeatureLayer;

                //foreach (var item in sourceLayer.FeatureSource.LinkSources)
                //{
                //    targetLayer.FeatureSource.LinkSources.Add(item);
                //}
                //foreach (var item in sourceLayer.FeatureSource.LinkExpressions)
                //{
                //    targetLayer.FeatureSource.LinkExpressions.Add(item);
                //}

                AddFeaturesToInMemoryFeatureLayer(targetLayer, features);
                if (GisEditor.ActiveMap.GetFeatureLayers().OfType<InMemoryFeatureLayer>().All(l => !l.Name.Equals(targetLayer.Name)))
                {
                    if (targetLayer.Name.Equals("New layer"))
                    {
                        string name = "ExportResults";
                        IEnumerable<string> existingNames = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                                          .SelectMany(o => o.Layers).OfType<InMemoryFeatureLayer>().Select(l => l.Name);
                        int index = 0;
                        while (existingNames.Contains(name))
                        {
                            index++;
                            name = "ExportResults" + index;
                        }
                        targetLayer.Name = name;
                    }
                    GisEditor.ActiveMap.AddLayerToActiveOverlay(targetLayer, TargetLayerOverlayType.Dynamic);
                }
            }
        }

    }
}
