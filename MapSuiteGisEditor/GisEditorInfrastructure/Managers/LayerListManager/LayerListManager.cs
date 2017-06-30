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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LayerListManager : Manager
    {
        private static LinearGradientBrush defaultBackground;
        private LayerListItem selectedLayerListItem;
        private Collection<LayerListItem> selectedLayerListItems;
        private Dictionary<string, LayerListItem> allMapElementsEntity;
        private Dictionary<string, Dictionary<string, Visibility>> states;
        private string[] defaultFeatureLayerMenuItemNames = new string[] { "ViewData", "--",
            "Moveup", "Movedown", "Movetotop", "Movetobottom", "--",
            "ZoomToExtent","Selectlayer", "Rename", "Remove", "--", "EditLayer","Rebuild","--",
            "AddStyle", "CurrentStyling", "--",
            "Exportascode","RebuildIndex", "LayerProjection", "Properties", "ExporttoFile" };

        private string[] defaultNonFeatureLayerMenuItemNames = new string[] {
            "Moveup", "Movedown", "Movetotop", "Movetobottom", "--",
            "ZoomToExtent", "Rename", "Remove", "--",
            "Transparency"};

        public event EventHandler<SelectedLayerListItemChangedLayerListManagerEventArgs> SelectedLayerListItemChanged;
        public event EventHandler<GettingLayerListItemContextMenuItemsEventArgs> GettingLayerListItemContextMenuItems;
        public event EventHandler<GottenLayerListItemContextMenuItemsEventArgs> GottenLayerListItemContextMenuItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerListManager" /> class.
        /// </summary>
        public LayerListManager()
        {
            selectedLayerListItems = new Collection<LayerListItem>();
            allMapElementsEntity = new Dictionary<string, LayerListItem>();
            states = new Dictionary<string, Dictionary<string, Visibility>>();
        }

        /// <summary>
        /// Gets or sets the selected layer list item.
        /// </summary>
        /// <value>
        /// The selected layer list item.
        /// </value>
        public LayerListItem SelectedLayerListItem
        {
            get { return selectedLayerListItem; }
            set
            {
                if (selectedLayerListItem != value)
                {
                    SelectedLayerListItemChangedLayerListManagerEventArgs e = new SelectedLayerListItemChangedLayerListManagerEventArgs(value, selectedLayerListItem);
                    if (selectedLayerListItem != null)
                    {
                        selectedLayerListItem.HighlightBackgroundBrush = selectedLayerListItem.BackgroundBrush;
                        selectedLayerListItem.IsSelected = false;
                    }
                    selectedLayerListItem = value;
                    if (selectedLayerListItem != null && selectedLayerListItem.HighlightBackgroundBrush == null)
                    {
                        foreach (var item in SelectedLayerListItems)
                        {
                            item.HighlightBackgroundBrush = item.BackgroundBrush;
                        }
                        SelectedLayerListItems.Clear();
                    }
                    OnSelectedLayerListItemChanged(e);
                }
            }
        }

        /// <summary>
        /// Gets the selected layer list items.
        /// </summary>
        /// <value>
        /// The selected layer list items.
        /// </value>
        public Collection<LayerListItem> SelectedLayerListItems
        {
            get { return selectedLayerListItems; }
        }

        /// <summary>
        /// Gets the root layer list item.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        /// <returns></returns>
        public LayerListItem GetRootLayerListItem(GisEditorWpfMap wpfMap)
        {
            return GetRootLayerListItemCore(wpfMap);
        }

        /// <summary>
        /// Gets the root layer list item core.
        /// </summary>
        /// <param name="wpfMap">The WPF map.</param>
        /// <returns></returns>
        protected virtual LayerListItem GetRootLayerListItemCore(GisEditorWpfMap wpfMap)
        {
            if (wpfMap != null) return GetLayerListItemForMap(wpfMap);
            else return null;
        }

        /// <summary>
        /// Gets the settings core.
        /// </summary>
        /// <returns></returns>
        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            settings.ProjectSettings.Add("Setting", SaveProjectSettingInternal().ToString());
            return settings;
        }

        /// <summary>
        /// Applies the settings core.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("Setting"))
            {
                try
                {
                    LoadProjectSettingInternal(XElement.Parse(settings.ProjectSettings["Setting"]));
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        protected virtual void OnSelectedLayerListItemChanged(SelectedLayerListItemChangedLayerListManagerEventArgs e)
        {
            EventHandler<SelectedLayerListItemChangedLayerListManagerEventArgs> handler = SelectedLayerListItemChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnGettingLayerListItemContextMenuItems(GettingLayerListItemContextMenuItemsEventArgs e)
        {
            EventHandler<GettingLayerListItemContextMenuItemsEventArgs> handler = GettingLayerListItemContextMenuItems;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnGottenLayerListItemContextMenuItems(GottenLayerListItemContextMenuItemsEventArgs e)
        {
            EventHandler<GottenLayerListItemContextMenuItemsEventArgs> handler = GottenLayerListItemContextMenuItems;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public ObservableCollection<MenuItem> GetLayerListContextMenuItems(LayerListItem layerListItem)
        {
            ObservableCollection<MenuItem> menuItems = null;

            GettingLayerListItemContextMenuItemsEventArgs gettingLayerListItemContextMenuItemsEventArgs = new GettingLayerListItemContextMenuItemsEventArgs(null);
            OnGettingLayerListItemContextMenuItems(gettingLayerListItemContextMenuItemsEventArgs);
            if (!gettingLayerListItemContextMenuItemsEventArgs.Cancel)
            {
                menuItems = GetLayerListContextMenuItemsCore(layerListItem);

                GottenLayerListItemContextMenuItemsEventArgs gottenLayerListItemContextMenuItemsEventArgs = new GottenLayerListItemContextMenuItemsEventArgs(menuItems);
                OnGottenLayerListItemContextMenuItems(gottenLayerListItemContextMenuItemsEventArgs);

                IEnumerable<MenuItem> resultMenuItems = gettingLayerListItemContextMenuItemsEventArgs.MenuItems.Concat(gottenLayerListItemContextMenuItemsEventArgs.MenuItems);
                if (layerListItem.ConcreteObject is Layer)
                {
                    menuItems = OrderMenuItems(new ObservableCollection<MenuItem>(resultMenuItems), (Layer)layerListItem.ConcreteObject);
                }
                else
                {
                    menuItems = new ObservableCollection<MenuItem>(resultMenuItems);
                }
            }
            else
            {
                menuItems = gettingLayerListItemContextMenuItemsEventArgs.MenuItems;
            }

            if (menuItems != null)
            {
                MenuItem menuItem = menuItems.FirstOrDefault(m => m.Header.Equals(GisEditor.LanguageManager.GetStringResource("MapElementsListPluginProperties")));
                if (menuItem != null)
                {
                    menuItems.Remove(menuItem);
                    menuItems.Add(menuItem);
                }
            }

            return menuItems;
        }

        protected virtual ObservableCollection<MenuItem> GetLayerListContextMenuItemsCore(LayerListItem layerListItem)
        {
            ObservableCollection<MenuItem> menuItems = new ObservableCollection<MenuItem>();
            var args = new GetLayerListItemContextMenuParameters(layerListItem);
            foreach (var item in layerListItem.ContextMenuItems)
            {
                menuItems.Add(item);
            }
            if (layerListItem.ConcreteObject is Layer)
            {
                var layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layerListItem.ConcreteObject.GetType()).FirstOrDefault();
                if (layerPlugin != null)
                {
                    foreach (var item in layerPlugin.GetLayerListItemContextMenuItems(args))
                    {
                        menuItems.Add(item);
                    }
                }
            }
            else if (layerListItem.ConcreteObject is Styles.Style)
            {
                var stylePlugin = GisEditor.StyleManager.GetStylePluginByStyle((Styles.Style)layerListItem.ConcreteObject);
                if (stylePlugin != null)
                {
                    foreach (var item in stylePlugin.GetLayerListItemContextMenuItems(args))
                    {
                        menuItems.Add(item);
                    }
                }
                Styles.Style style = (Styles.Style)layerListItem.ConcreteObject;
                if (style is CompositeStyle)
                {
                    CompositeStyle compositeStyle = (CompositeStyle)style;
                    if (compositeStyle.Styles.Count == 1 && !(compositeStyle.Styles[0] is FilterStyle) && compositeStyle.Styles[0].Filters != null && compositeStyle.Styles[0].Filters.Count > 0)
                    {
                        MenuItem menuItem = GetFilteredDataMenuItem(layerListItem);
                        menuItems.Add(menuItem);

                        MenuItem exportMenuItem = GetExportFilterMenuItem(layerListItem);
                        menuItems.Add(exportMenuItem);

                        MenuItem zoomtoMenuItem = GetZoomToFilteredDataMenuItem(layerListItem);
                        menuItems.Add(zoomtoMenuItem);
                    }
                }
                else if (!(style is FilterStyle) && style.Filters != null && style.Filters.Count > 0)
                {
                    MenuItem menuItem = GetFilteredDataMenuItem(layerListItem);
                    menuItems.Add(menuItem);

                    MenuItem zoomtoMenuItem = GetZoomToFilteredDataMenuItem(layerListItem);
                    menuItems.Add(zoomtoMenuItem);
                }
            }

            foreach (var uiPlugin in GisEditor.UIManager.GetActiveUIPlugins())
            {
                var tmpItems = uiPlugin.GetLayerListItemContextMenuItems(args);
                if (tmpItems != null && tmpItems.Count > 0)
                {
                    menuItems.Add(new MenuItem { Header = "--" });
                    foreach (var tmItem in tmpItems)
                    {
                        menuItems.Add(tmItem);
                    }
                }
            }

            return menuItems;
        }

        private MenuItem GetZoomToFilteredDataMenuItem(LayerListItem layerListItem)
        {
            MenuItem zoomToDataMenuItem = new MenuItem();
            zoomToDataMenuItem.Header = "Zoom to filtered data";
            zoomToDataMenuItem.Click += new System.Windows.RoutedEventHandler(ZoomToDataMenuItem_Click);
            zoomToDataMenuItem.Tag = layerListItem;
            zoomToDataMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/zoomto.png", UriKind.RelativeOrAbsolute)) };

            return zoomToDataMenuItem;
        }

        private MenuItem GetExportFilterMenuItem(LayerListItem layerListItem)
        {
            MenuItem exportMenuItem = new MenuItem();
            exportMenuItem.Header = "Export";
            exportMenuItem.Click += ExportMenuItem_Click;
            exportMenuItem.Tag = layerListItem;
            exportMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/Export.png", UriKind.RelativeOrAbsolute)) };

            return exportMenuItem;
        }

        private void ExportMenuItem_Click(object sender, RoutedEventArgs e)
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
                selectedLayer.Open();
                RectangleShape bbox = selectedLayer.GetBoundingBox();
                Collection<string> filters = ((Styles.Style)layerListItem.ConcreteObject).Filters;

                Collection<string> returningColumnNames = ((Styles.Style)layerListItem.ConcreteObject).GetRequiredColumnNames();
                returningColumnNames = GetRequiredColumnNamesForLink(returningColumnNames, selectedLayer.FeatureSource);

                Collection<FeatureSourceColumn> featureColumns = selectedLayer.FeatureSource.GetColumns();
                IEnumerable<string> allColumns = featureColumns.Select(c => c.ColumnName);

                ////LINK:
                //Collection<Feature> allDataFeatures = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width, GisEditor.ActiveMap.Height, allColumns, filters);

                ////LINK:
                //Collection<Feature> features = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width, GisEditor.ActiveMap.Height, returningColumnNames, filters);
                Collection<Feature> features = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width, GisEditor.ActiveMap.Height, returningColumnNames);

                ////LINK:
                //List<string> linkSourceNameStarts = GetLinkSourceNameStarts(selectedLayer.FeatureSource.LinkSources);
                //if (filters != null && filters.Count() > 0 && CheckHasLinkColumns(returningColumnNames, filters, linkSourceNameStarts))
                //{
                //    List<Feature> resultFeatureList = features.Where(f => CheckIsValidLinkFeature(f, linkSourceNameStarts)).ToList();
                //    features = new Collection<Feature>(resultFeatureList);
                //}

                Collection<Feature> resultFeatures = features; // new Collection<Feature>(allDataFeatures.Where(f => features.Any(t => t.Id.Equals(f.Id))).ToList());

                FeatureLayerPlugin layerPlugin = (FeatureLayerPlugin)GisEditor.LayerManager.GetLayerPlugins(selectedLayer.GetType()).FirstOrDefault();

                ExportToShapeFile(resultFeatures, featureColumns, layerPlugin, selectedLayer.FeatureSource.GetFirstFeaturesWellKnownType());
            }
        }

        private void ExportToShapeFile(Collection<Feature> resultFeatures, Collection<FeatureSourceColumn> columns, FeatureLayerPlugin sourceLayerPlugin, WellKnownType type)
        {
            int count = resultFeatures.Count;

            if (count > 0)
            {
                FeatureLayerPlugin targetLayerPlugin = (FeatureLayerPlugin)GisEditor.LayerManager.GetLayerPlugins(typeof(ShapeFileFeatureLayer)).FirstOrDefault();
                FeatureLayer resultLayer = null;

                if (targetLayerPlugin != null)
                {
                    GetLayersParameters getLayerParameters = new GetLayersParameters();
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
                System.Windows.Forms.MessageBox.Show("There is no features to export.", "Export");
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

        private MenuItem GetFilteredDataMenuItem(LayerListItem layerListItem)
        {
            MenuItem viewDataMenuItem = new MenuItem();
            viewDataMenuItem.Header = "Show filtered data";
            viewDataMenuItem.Click += new System.Windows.RoutedEventHandler(ViewDataMenuItem_Click);
            viewDataMenuItem.Tag = layerListItem;
            viewDataMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png", UriKind.RelativeOrAbsolute)) };

            return viewDataMenuItem;
        }

        private void ZoomToDataMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
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
                selectedLayer.Open();
                RectangleShape bbox = selectedLayer.GetBoundingBox();
                Collection<string> filters = ((Styles.Style)layerListItem.ConcreteObject).Filters;

                Collection<string> returningColumnNames = ((Styles.Style)layerListItem.ConcreteObject).GetRequiredColumnNames();
                returningColumnNames = GetRequiredColumnNamesForLink(returningColumnNames, selectedLayer.FeatureSource);

//                IEnumerable<string> allColumns = selectedLayer.FeatureSource.GetColumns().Select(c => c.ColumnName);
//                Collection<Feature> allDataFeatures = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width,
//GisEditor.ActiveMap.Height, allColumns, filters);

                Collection<Feature> features = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width,
GisEditor.ActiveMap.Height, returningColumnNames);

                //List<string> linkSourceNameStarts = GetLinkSourceNameStarts(selectedLayer.FeatureSource.LinkSources);
                //if (filters != null && filters.Count() > 0 && CheckHasLinkColumns(returningColumnNames, filters, linkSourceNameStarts))
                //{
                //    List<Feature> resultFeatureList = features.Where(f => CheckIsValidLinkFeature(f, linkSourceNameStarts)).ToList();
                //    features = new Collection<Feature>(resultFeatureList);
                //}

                Collection<Feature> resultFeatures = features;//new Collection<Feature>(allDataFeatures.Where(f => features.Any(t => t.Id.Equals(f.Id))).ToList());

                if (resultFeatures.Count > 0)
                {
                    RectangleShape boundingBox = ExtentHelper.GetBoundingBoxOfItems(resultFeatures);
                    GisEditor.ActiveMap.CurrentExtent = boundingBox;
                    GisEditor.ActiveMap.Refresh();
                }
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
                selectedLayer.Open();
                RectangleShape bbox = selectedLayer.GetBoundingBox();
                Collection<string> filters = ((Styles.Style)layerListItem.ConcreteObject).Filters;

                Collection<string> returningColumnNames = ((Styles.Style)layerListItem.ConcreteObject).GetRequiredColumnNames();
                returningColumnNames = GetRequiredColumnNamesForLink(returningColumnNames, selectedLayer.FeatureSource);

//                IEnumerable<string> allColumns = selectedLayer.FeatureSource.GetColumns().Select(c => c.ColumnName);
//                Collection<Feature> allDataFeatures = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width,
//GisEditor.ActiveMap.Height, allColumns, filters);

                Collection<Feature> features = selectedLayer.FeatureSource.GetFeaturesForDrawing(bbox, GisEditor.ActiveMap.Width,
GisEditor.ActiveMap.Height, returningColumnNames);

                //List<string> linkSourceNameStarts = GetLinkSourceNameStarts(selectedLayer.FeatureSource.LinkSources);
                //if (filters != null && filters.Count() > 0 && CheckHasLinkColumns(returningColumnNames, filters, linkSourceNameStarts))
                //{
                //    List<Feature> resultFeatureList = features.Where(f => CheckIsValidLinkFeature(f, linkSourceNameStarts)).ToList();
                //    features = new Collection<Feature>(resultFeatureList);
                //}

                Collection<Feature> resultFeatures = features;// new Collection<Feature>(allDataFeatures.Where(f => features.Any(t => t.Id.Equals(f.Id))).ToList());

                ShowFilteredData(selectedLayer, resultFeatures, string.Format("{0} {1}", selectedLayer.Name, layerListItem.Name));
            }
        }

        private Collection<string> GetRequiredColumnNamesForLink(IEnumerable<string> returningColumnNames, FeatureSource featureSource)
        {
            //IEnumerable<string> requiredLinkColumnNames = GetRequiredLinkColumnNames(featureSource);
            //IEnumerable<string> tempColumnNames = returningColumnNames
            //    .Where(c => !requiredLinkColumnNames.Contains(c.ToUpperInvariant()))
            //    .Concat(requiredLinkColumnNames)
            //    .Distinct();

            Collection<string> resultColumnNames = new Collection<string>();
            returningColumnNames.ForEach(columnName => resultColumnNames.Add(columnName));
            return resultColumnNames;
        }

        //private static Collection<string> GetRequiredLinkColumnNames(FeatureSource featureSource)
        //{
        //    string startFlag = "FEATURE";
        //    Collection<string> requiredLinkColumnNames = new Collection<string>();
        //    if (featureSource.LinkExpressions != null)
        //    {
        //        foreach (var exp in featureSource.LinkExpressions)
        //        {
        //            LinkExpression linkExp = LinkExpression.Parse(exp);
        //            if (startFlag.Equals(linkExp.LeftKey, StringComparison.OrdinalIgnoreCase))
        //            {
        //                requiredLinkColumnNames.Add(linkExp.LeftValue);
        //            }

        //            if (startFlag.Equals(linkExp.RightKey, StringComparison.OrdinalIgnoreCase))
        //            {
        //                requiredLinkColumnNames.Add(linkExp.RightValue);
        //            }
        //        }
        //    }

        //    return requiredLinkColumnNames;
        //}

        //private static bool CheckIsValidLinkFeature(Feature feature, IEnumerable<string> linkSourceNameStarts)
        //{
        //    return feature.LinkColumnValues
        //        .Where(l => linkSourceNameStarts.Any(linkName => l.Key.StartsWith(linkName, StringComparison.OrdinalIgnoreCase))).Count() > 0;
        //}

        private static bool CheckHasLinkColumns(IEnumerable<string> returningColumnNames, IEnumerable<string> filters, IEnumerable<string> linkSourceNameStarts)
        {
            bool hasLinkColumns = false;

            if (returningColumnNames != null)
            {
                hasLinkColumns = returningColumnNames != null
                    && returningColumnNames.Any(s => s.Contains("."))
                    && returningColumnNames.Any(s => linkSourceNameStarts.Any(linkName => s.StartsWith(linkName, StringComparison.OrdinalIgnoreCase) || s.ToUpperInvariant
                        ().Contains("[" + linkName.ToUpperInvariant())));
            }

            if (!hasLinkColumns && filters != null)
            {
                hasLinkColumns = filters.Any(f => linkSourceNameStarts.Any(linkName => f.ToUpperInvariant().Contains(linkName.ToUpperInvariant())));
            }

            return hasLinkColumns;
        }

        //private List<string> GetLinkSourceNameStarts(IEnumerable<LinkSource> linkSources)
        //{
        //    return GetFlatLinkSources(linkSources).Select(l => l.Name + ".").ToList();
        //}

        //internal IEnumerable<LinkSource> GetFlatLinkSources(IEnumerable<LinkSource> linkSources)
        //{
        //    return GetChildren(linkSources);
        //}

        //private static IEnumerable<LinkSource> GetChildren(IEnumerable<LinkSource> linkSources)
        //{
        //    return linkSources.Concat(linkSources.SelectMany(i => GetChildren(i.LinkSources)));
        //}

        private static void ShowFilteredData(FeatureLayer featureLayer, Collection<Feature> features, string title = "")
        {
            Collection<FeatureLayer> layers = new Collection<FeatureLayer>();

            Collection<FeatureSourceColumn> addColumns = new Collection<FeatureSourceColumn>();
            featureLayer.SafeProcess(() =>
            {
                addColumns = featureLayer.FeatureSource.GetColumns(GettingColumnsType.FeatureSourceOnly);
            });
            InMemoryFeatureLayer layer = new InMemoryFeatureLayer(addColumns, features);
            layer.Name = featureLayer.Name;
            layer.Open();

            foreach (var column in addColumns)
            {
                layer.FeatureSource.SetColumnAlias(column.ColumnName, featureLayer.FeatureSource.GetColumnAlias(column.ColumnName));
            }
            foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
            {
                layer.ZoomLevelSet.CustomZoomLevels.Add(zoomLevel);
            }
            //foreach (var expression in featureLayer.FeatureSource.LinkExpressions)
            //{
            //    layer.FeatureSource.LinkExpressions.Add(expression);
            //}
            //foreach (var linkSource in featureLayer.FeatureSource.LinkSources)
            //{
            //    layer.FeatureSource.LinkSources.Add(linkSource);
            //}
            layers.Add(layer);

            DataViewerUserControl content = new DataViewerUserControl(layer, layers);
            content.IsHighlightFeatureEnabled = false;
            content.Title = title;
            content.ShowDock();
        }

        private ObservableCollection<MenuItem> OrderMenuItems(ObservableCollection<MenuItem> menuItems, Layer layer)
        {
            ObservableCollection<MenuItem> newMenuItems = new ObservableCollection<MenuItem>();
            string[] orders = layer is FeatureLayer ? defaultFeatureLayerMenuItemNames : defaultNonFeatureLayerMenuItemNames;
            foreach (var name in orders)
            {
                if (name != "--")
                {
                    MenuItem resultMenuItem = menuItems.FirstOrDefault(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                    if (resultMenuItem != null)
                    {
                        newMenuItems.Add(resultMenuItem);
                        menuItems.Remove(resultMenuItem);
                    }
                }
                else if (newMenuItems.Count > 0 && !newMenuItems.LastOrDefault().Header.Equals("--"))
                {
                    newMenuItems.Add(new MenuItem { Header = name });
                }
            }
            if (layer is FeatureLayer)
            {
                MenuItem transparentMenuItem = menuItems.FirstOrDefault(m => m.Name.Equals("Transparency"));
                MenuItem currentStylingMenuItem = newMenuItems.FirstOrDefault(m => m.Name.Equals("CurrentStyling"));
                if (transparentMenuItem != null && currentStylingMenuItem != null)
                {
                    int index = currentStylingMenuItem.Items.Count > 0 ? 1 : 0;
                    if (currentStylingMenuItem.Items.Count > 1) index = 2;
                    currentStylingMenuItem.Items.Insert(index, transparentMenuItem);
                    menuItems.Remove(transparentMenuItem);
                }
            }

            int currentIndex = newMenuItems.Count - 1;
            foreach (var item in menuItems)
            {
                if (newMenuItems[currentIndex].Header == "--" && item.Header == "--") continue;

                newMenuItems.Add(item);
                currentIndex++;
            }

            return newMenuItems;
        }

        private LayerListItem GetLayerListItemForMap(GisEditorWpfMap wpfMap)
        {
            var mapElementItem = new LayerListItem();
            LayerListItem overlayListItem = null;
            foreach (var overlay in wpfMap.Overlays.Reverse().Concat(wpfMap.InteractiveOverlays))
            {
                overlayListItem = GetLayerListItemFromUIPlugins(overlay);
                if (overlayListItem != null)
                {
                    overlayListItem.Parent = mapElementItem;
                    LayerOverlay layerOverlay = overlay as LayerOverlay;
                    if (layerOverlay != null)
                    {
                        foreach (var layer in layerOverlay.Layers.Reverse())
                        {
                            var layerListItem = GetLayerListItemForLayer(layer);
                            if (layerListItem != null)
                            {
                                if (SelectedLayerListItem != null && layer == SelectedLayerListItem.ConcreteObject)
                                {
                                    layerListItem.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
                                    SelectedLayerListItem = layerListItem;
                                }
                                if (wpfMap.FeatureLayerEditOverlay != null && wpfMap.FeatureLayerEditOverlay.EditTargetLayer == layer && layer.IsVisible)
                                {
                                    layerListItem.WarningImages.Add(
                                        new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/sketchTools.png", UriKind.Absolute)), Width = 14, Height = 14, ToolTip = "In Editing", Name = "InEditing" });
                                }
                                layerListItem.Parent = overlayListItem;
                                overlayListItem.Children.Add(layerListItem);
                            }
                        }
                    }
                    mapElementItem.Children.Add(overlayListItem);
                }
            }
            return mapElementItem;
        }

        private LayerListItem GetLayerListItemForOverlay(Overlay overlay)
        {
            var overlayListItem = new LayerListItem();
            overlayListItem.ConcreteObject = overlay;
            overlayListItem.CheckBoxVisibility = Visibility.Visible;
            overlayListItem.ChildrenContainerVisibility = Visibility.Visible;
            overlayListItem.IsChecked = overlay.IsVisible;
            overlayListItem.Name = overlay.Name;
            overlayListItem.HighlightBackgroundBrush = GetDefaultLayerGroupBackground();
            overlayListItem.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OverlayItemPropertyChanged);
            if (selectedLayerListItem != null && overlay == selectedLayerListItem.ConcreteObject)
            {
                overlayListItem.HighlightBackgroundBrush = new SolidColorBrush(Colors.LightBlue);
            }
            return overlayListItem;
        }

        private LayerListItem GetLayerListItemForLayer(Layer layer)
        {
            var layerListItem = GisEditor.LayerManager.GetLayerListItem(layer);
            if (layerListItem == null) layerListItem = GetLayerListItemFromUIPlugins(layer);
            LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault();
            if (layerPlugin != null)
            {
                bool isDataSourceAvailable = layerPlugin.DataSourceResolveTool.IsDataSourceAvailable(layer);
                if (!isDataSourceAvailable)
                {
                    string noIndexToolTip = "This layer's data source is unavailable, please right-click to resolve it.";
                    layerListItem.WarningImages.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/NoIndexFile.png", UriKind.Absolute)), Width = 14, Height = 14, ToolTip = noIndexToolTip });
                }
            }
            return layerListItem;
        }

        private static LayerListItem GetLayerListItemFromUIPlugins(object concreteObject)
        {
            foreach (var uiPlugin in GisEditor.UIManager.GetUIPlugins())
            {
                var layerListItem = uiPlugin.GetLayerListItem(concreteObject);
                if (layerListItem != null)
                    return layerListItem;
            }
            return null;
        }

        private void LoadProjectSettingInternal(XElement projectSettings)
        {
            states = new Dictionary<string, Dictionary<string, Visibility>>();
            foreach (var mapXElement in projectSettings.Descendants("Map"))
            {
                Dictionary<string, Visibility> mapElementsVisibilities = new Dictionary<string, Visibility>();
                foreach (var overlayXElement in mapXElement.Descendants("Overlay"))
                {
                    AddState(mapElementsVisibilities, overlayXElement);
                    foreach (var layerXElement in overlayXElement.Descendants("Layer"))
                    {
                        AddState(mapElementsVisibilities, layerXElement);
                    }
                }
                states.Add(mapXElement.Attribute("Name").Value, mapElementsVisibilities);
            }
        }

        private XElement SaveProjectSettingInternal()
        {
            XElement expandStatusElement = new XElement("ExpandStatus");
            foreach (var item in allMapElementsEntity)
            {
                XElement mapXElement = new XElement("Map", new XAttribute("Name", item.Key));
                foreach (var overlayEntity in item.Value.Children)
                {
                    XElement overlayXElement = new XElement("Overlay", new XAttribute("Name", overlayEntity.Name), overlayEntity.ChildrenContainerVisibility.ToString());
                    foreach (var layerEntity in overlayEntity.Children)
                    {
                        XElement layerXElement = new XElement("Layer", new XAttribute("Name", layerEntity.Name), layerEntity.ChildrenContainerVisibility.ToString());
                        overlayXElement.Add(layerXElement);
                    }
                    mapXElement.Add(overlayXElement);
                }
                if (mapXElement.HasElements)
                {
                    expandStatusElement.Add(mapXElement);
                }
            }
            return expandStatusElement;
        }

        private static void AddState(Dictionary<string, Visibility> mapElementsVisibilities, XElement xElement)
        {
            var nameAttribute = xElement.Attribute("Name");
            if (nameAttribute != null)
            {
                var visibility = Visibility.Collapsed;
                if (Enum.TryParse<Visibility>(xElement.Value, out visibility))
                {
                    mapElementsVisibilities[nameAttribute.Value] = visibility;
                }
            }
        }

        private void OverlayItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var overlayViewModel = sender as LayerListItem;
            if (e.PropertyName == "IsChecked" && SelectedLayerListItems.Contains(overlayViewModel))
            {
                foreach (var item in SelectedLayerListItems)
                {
                    if (item.IsChecked != overlayViewModel.IsChecked)
                    {
                        item.IsChecked = overlayViewModel.IsChecked;
                    }
                }
            }
        }

        private static LinearGradientBrush GetDefaultLayerGroupBackground()
        {
            if (defaultBackground == null)
            {
                GradientStopCollection gradientStopCollection = new GradientStopCollection();
                gradientStopCollection.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCECED4"), 0));
                gradientStopCollection.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFEDEEF4"), 0.8));

                defaultBackground = new LinearGradientBrush(gradientStopCollection, new Point(0, 0), new Point(0, 1));
            }
            return defaultBackground;
        }
    }

    //internal class LinkExpression
    //{
    //    private static readonly string[] operators = new string[] { "equals", "==", "=" };

    //    protected LinkExpression(string expression, LinkSource linkSource)
    //    {
    //        if (!expression.ToLowerInvariant().Contains("equals") && !expression.ToLowerInvariant().Contains("="))
    //        {
    //            throw new ArgumentException("Link expression \"" + expression + "\" is invalid.", "equals");
    //        }

    //        this.Expression = expression;
    //        this.LinkSource = linkSource;

    //        Operator = operators.First(o => Expression.Contains(o));
    //        Expression = Expression.Replace(Operator, Operator.ToLowerInvariant());
    //        string[] leftRight = Expression.Split(new string[] { Operator }, StringSplitOptions.RemoveEmptyEntries);
    //        string[] left = leftRight[0].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
    //        string[] right = leftRight[1].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
    //        if (left.Length == 2)
    //        {
    //            LeftKey = left[0].Trim();
    //            LeftValue = left[1].Trim();
    //        }
    //        else
    //        {
    //            throw new ArgumentException("Link expression \"" + expression + "\" is invalid.", leftRight[0]);
    //        }
    //        if (right.Length == 2)
    //        {
    //            RightKey = right[0].Trim();
    //            RightValue = right[1].Trim();
    //        }
    //        else
    //        {
    //            throw new ArgumentException("Link expression \"" + expression + "\" is invalid.", leftRight[1]);
    //        }
    //    }

    //    public string Expression { get; set; }

    //    public LinkSource LinkSource { get; set; }

    //    public string LeftKey { get; set; }

    //    public string LeftValue { get; set; }

    //    public string RightKey { get; set; }

    //    public string RightValue { get; set; }

    //    public string Operator { get; set; }

    //    public IEnumerable<KeyValuePair<string, string>> GetColumnNames()
    //    {
    //        yield return new KeyValuePair<string, string>(LeftKey, LeftValue);
    //        yield return new KeyValuePair<string, string>(RightKey, RightValue);
    //    }

    //    public static LinkExpression Parse(string expression, LinkSource linkSource = null)
    //    {
    //        LinkExpression linkExpression = new LinkExpression(expression, linkSource);
    //        return linkExpression;
    //    }

    //    private static string FormatInvariant(string str, params object[] parameters)
    //    {
    //        return string.Format(CultureInfo.InvariantCulture, str, parameters);
    //    }

    //    public string GetFormattedExpression()
    //    {
    //        // case 1: 
    //        // feature.sub_trafk equals tracts.tra_pk
    //        // join tracts_item in tracts_output on feature.ColumnValues[""sub_trafk""] equals tracts_item.tracts_item[""tra_pk""]

    //        // case 2:
    //        // leases.lse_ownfk equals owners1.own_pk
    //        // join owners1_item in owners1.Records on leases_item[""lse_ownfk""] equals owners1_item[""own_pk""]
    //        string p0 = FormatInvariant("{0}_item", RightKey);
    //        string p1 = string.Empty;
    //        if (LinkSource.LinkSources.Count > 0)
    //        {
    //            p1 = FormatInvariant("{0}_output", RightKey);
    //        }
    //        else
    //        {
    //            p1 = FormatInvariant(@"{0}.Records", RightKey);
    //        }

    //        string p2 = string.Empty;
    //        if (LeftKey.Equals("FEATURE", StringComparison.OrdinalIgnoreCase))
    //        {
    //            p2 = FormatInvariant(@"{0}.ColumnValues[""{1}""]", LeftKey, LeftValue);
    //        }
    //        else
    //        {
    //            p2 = FormatInvariant(@"{0}_item[""{1}""]", LeftKey, LeftValue);
    //        }

    //        string p3 = string.Empty;
    //        if (LinkSource.LinkSources.Count > 0)
    //        {
    //            p3 = FormatInvariant(@"{0}_item.{0}_item[""{1}""]", RightKey, RightValue);
    //        }
    //        else
    //        {
    //            p3 = FormatInvariant(@"{0}_item[""{1}""]", RightKey, RightValue);
    //        }

    //        string template = FormatInvariant("join {0} in {1} on {2} equals {3}", p0, p1, p2, p3);
    //        return template;
    //    }
    //}
}