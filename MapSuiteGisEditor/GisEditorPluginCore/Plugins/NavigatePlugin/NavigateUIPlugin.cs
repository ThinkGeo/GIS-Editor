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


using Microsoft.Windows.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NavigateUIPlugin : UIPlugin
    {
        private static readonly string searchEntriesKey = "SearchEntries";

        private Point originPosition;
        private bool isHighPriorityMouseOperation;
        private Point mouseDownCoordinate;
        private Dictionary<WpfMap, Cursor> previousCursors;
        private Dictionary<WpfMap, SwitcherMode> previousSwitchMode;
        private Dictionary<WpfMap, bool> previousExtentOverlayEnabled;
        private Point trackStartScreenPoint;
        private RibbonEntry navigateEntry;

        //private RibbonEntry geoprocessingEntry;
        //private RibbonEntry dataEntry;
        private RibbonEntry adornmentEntry;

        private RibbonEntry adornmentHelpEntry;
        private SearchPlaceUserControl searchPlaceUserControl;

        [NonSerialized]
        private NavigateRibbonGroup navigateGroup;

        //[NonSerialized]
        //private GeoProcessingGroup geoprocessingGroup;
        //[NonSerialized]
        //private DataRibbonGroup dataRibbonGroup;
        [NonSerialized]
        private AdornmentRibbonGroup adornmentGroup;

        [NonSerialized]
        private RibbonGroup adornmentHelpRibbonGroup;

        [NonSerialized]
        private Rectangle trackShape;

        public NavigateUIPlugin()
        {
            previousCursors = new Dictionary<WpfMap, Cursor>();
            previousSwitchMode = new Dictionary<WpfMap, SwitcherMode>();
            previousExtentOverlayEnabled = new Dictionary<WpfMap, bool>();
            Index = UIPluginOrder.NavigatePlugin;
            Description = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginDescription");
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/base_globe_32.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/base_globe_32.png", UriKind.RelativeOrAbsolute));

            navigateGroup = new NavigateRibbonGroup();
            navigateEntry = new RibbonEntry(navigateGroup, RibbonTabOrder.Home, "HomeRibbonTabHeader");

            //geoprocessingGroup = new GeoProcessingGroup();
            //geoprocessingEntry = new RibbonEntry(geoprocessingGroup, RibbonTabOrder.Tools, "ToolsRibbonTabHeader");

            //dataRibbonGroup = new DataRibbonGroup();
            //dataEntry = new RibbonEntry(dataRibbonGroup, RibbonTabOrder.Tools, "ToolsRibbonTabHeader");

            adornmentGroup = new AdornmentRibbonGroup();
            adornmentEntry = new RibbonEntry(adornmentGroup, RibbonTabOrder.Adornments, "AdornmentsRibbonTabHeader");

            adornmentHelpRibbonGroup = new RibbonGroup();
            adornmentHelpRibbonGroup.Items.Add(HelpResourceHelper.GetHelpButton("AdornmentsHelp", HelpButtonMode.RibbonButton));
            adornmentHelpRibbonGroup.GroupSizeDefinitions.Add(new RibbonGroupSizeDefinition() { IsCollapsed = false });
            adornmentHelpRibbonGroup.SetResourceReference(RibbonGroup.HeaderProperty, "HelpHeader");
            adornmentHelpEntry = new RibbonEntry(adornmentHelpRibbonGroup, RibbonTabOrder.Adornments, "AdornmentsRibbonTabHeader");
            searchPlaceUserControl = new SearchPlaceUserControl();
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (!RibbonEntries.Contains(navigateEntry)) RibbonEntries.Add(navigateEntry);
            //if (!RibbonEntries.Contains(geoprocessingEntry)) RibbonEntries.Add(geoprocessingEntry);
            //if (!RibbonEntries.Contains(dataEntry)) RibbonEntries.Add(dataEntry);
            if (!RibbonEntries.Contains(adornmentEntry)) RibbonEntries.Add(adornmentEntry);
            if (!RibbonEntries.Contains(adornmentHelpEntry)) RibbonEntries.Add(adornmentHelpEntry);

            GisEditor.ProjectManager.Closing -= new EventHandler<EventArgs>(ProjectManager_Closing);
            GisEditor.ProjectManager.Closing += new EventHandler<EventArgs>(ProjectManager_Closing);

            //add the search place dock window
            searchPlaceUserControl.Plotted += new EventHandler<PlotOnMapEventArgs>(SearchPlaceUserControl_Plotted);
            searchPlaceUserControl.ClearPlottedPlaces += ClearPlottedPlaces;
            searchPlaceUserControl.CanClearPlottedPlaces += CanClearPlottedPlaces;

            DockWindow placeSearchDockWindow = new DockWindow();
            placeSearchDockWindow.Title = "NavigateRibbonGroupPlaceSearchLabel";
            placeSearchDockWindow.Name = "PlaceSearch";
            placeSearchDockWindow.StartupMode = DockWindowStartupMode.Hide;

            placeSearchDockWindow.Content = searchPlaceUserControl;
            FeatureInfoWindow.Instance.StartupMode = DockWindowStartupMode.Hide;
            DockWindows.Add(FeatureInfoWindow.Instance);
            DockWindows.Add(placeSearchDockWindow);

            DockWindow findFeatureDockWindow = new DockWindow();
            findFeatureDockWindow.Title = "Find Feature";
            findFeatureDockWindow.Name = "FindFeature";
            findFeatureDockWindow.StartupMode = DockWindowStartupMode.Hide;
            findFeatureDockWindow.Content = FindFeatureUserControl.Instance;
            DockWindows.Add(findFeatureDockWindow);

            //the navigation group has button that shows up the place search window, that's why we pass the placeSearchDockWindow to it.
            navigateGroup.SearchDockWindow = placeSearchDockWindow;
        }

        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();

            SearchPlaceViewModel viewModel = searchPlaceUserControl.DataContext as SearchPlaceViewModel;
            if (viewModel != null)
            {
                string names = string.Empty;
                foreach (string name in viewModel.SearchEntriesModels.Where(s => s.IsChecked).Select(m => m.Name))
                {
                    names += name + "|";
                }
                if (!string.IsNullOrEmpty(names))
                {
                    settings.GlobalSettings[searchEntriesKey] = names;
                }
            }

            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            SearchPlaceViewModel viewModel = searchPlaceUserControl.DataContext as SearchPlaceViewModel;
            base.ApplySettingsCore(settings);
            foreach (var item in settings.GlobalSettings)
            {
                if (item.Key.Equals(searchEntriesKey))
                {
                    string[] names = item.Value.Split('|');

                    if (viewModel != null)
                    {
                        foreach (SearchEntriesModel model in viewModel.SearchEntriesModels)
                        {
                            if (names.Contains(model.Name))
                            {
                                model.IsChecked = true;
                            }
                            else
                            {
                                model.IsChecked = false;
                            }
                        }
                    }
                }
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            foreach (var map in GisEditor.DockWindowManager.DocumentWindows.Select((doc) => doc.Content as WpfMap))
            {
                var northArrowTool = map.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
                if (northArrowTool != null)
                {
                    map.MapTools.Remove(northArrowTool);
                }
            }
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            navigateGroup.SynchronizeState(currentMap);
            FindFeatureUserControl.Instance.RefreshFeatureLayers();

            if (refreshArgs != null)
            {
                Tuple<IEnumerable<Feature>, FeatureLayer> results = refreshArgs.Sender as Tuple<IEnumerable<Feature>, FeatureLayer>;
                if (results != null && refreshArgs.Description == "Identify")
                {
                    GisEditor.ActiveMap.ShowIdentifyFeaturesWindow(results.Item1, results.Item2);
                }
                else
                {
                    Collection<Feature> features = new Collection<Feature>();
                    var groups = currentMap.SelectionOverlay.GetSelectedFeaturesGroup();
                    foreach (var group in groups)
                    {
                        FeatureLayer layer = group.Key;
                        //update calculated records for feature layer.
                        if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(layer.FeatureSource.Id)
                            && CalculatedDbfColumn.CalculatedColumns[layer.FeatureSource.Id].Count > 0)
                        {
                            Collection<Feature> tempFeatures = group.Value;
                            CalculatedDbfColumn.UpdateCalculatedRecords(CalculatedDbfColumn.CalculatedColumns[layer.FeatureSource.Id], tempFeatures, GisEditor.ActiveMap.DisplayProjectionParameters);
                            foreach (var item in tempFeatures)
                            {
                                item.Tag = layer;
                                features.Add(item);
                            }
                        }
                    }

                    FeatureInfoWindow.Instance.Refresh(features);
                }
            }
            else
            {
                Collection<Feature> features = new Collection<Feature>();
                var groups = currentMap.SelectionOverlay.GetSelectedFeaturesGroup();
                foreach (var group in groups)
                {
                    FeatureLayer layer = group.Key;
                    //update calculated records for feature layer.
                    if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(layer.FeatureSource.Id)
                        && CalculatedDbfColumn.CalculatedColumns[layer.FeatureSource.Id].Count > 0)
                    {
                        Collection<Feature> tempFeatures = group.Value;
                        CalculatedDbfColumn.UpdateCalculatedRecords(CalculatedDbfColumn.CalculatedColumns[layer.FeatureSource.Id], tempFeatures, GisEditor.ActiveMap.DisplayProjectionParameters);
                        foreach (var item in tempFeatures)
                        {
                            item.Tag = layer;
                            features.Add(item);
                        }
                    }
                }

                FeatureInfoWindow.Instance.Refresh(features);
            }
        }

        protected override void AttachMapCore(GisEditorWpfMap wpfMap)
        {
            base.AttachMapCore(wpfMap);
            wpfMap.Cursor = GisEditorCursors.Pan;
            wpfMap.CurrentScaleChanged -= Map_CurrentScaleChanged;
            wpfMap.CurrentScaleChanged += Map_CurrentScaleChanged;
            wpfMap.ExtentOverlay.MapMouseClick -= IdentifyInteractiveOverlay_MapMouseClick;
            wpfMap.ExtentOverlay.MapMouseClick += IdentifyInteractiveOverlay_MapMouseClick;
            wpfMap.ExtentOverlay.MapMouseDown -= IdentifyInteractiveOverlay_MapMouseDown;
            wpfMap.ExtentOverlay.MapMouseDown += IdentifyInteractiveOverlay_MapMouseDown;
            wpfMap.ExtentOverlay.MapMouseMove -= IdentifyInteractiveOverlay_MapMouseMove;
            wpfMap.ExtentOverlay.MapMouseMove += IdentifyInteractiveOverlay_MapMouseMove;
            wpfMap.ExtentOverlay.MapMouseUp -= IdentifyInteractiveOverlay_MapMouseUp;
            wpfMap.ExtentOverlay.MapMouseUp += IdentifyInteractiveOverlay_MapMouseUp;
            wpfMap.DisplayProjectionParametersChanged -= WpfMap_DisplayProjectionParametersChanged;
            wpfMap.DisplayProjectionParametersChanged += WpfMap_DisplayProjectionParametersChanged;
            wpfMap.KeyDown -= Map_KeyDown;
            wpfMap.KeyDown += Map_KeyDown;
            wpfMap.KeyUp -= Map_KeyUp;
            wpfMap.KeyUp += Map_KeyUp;
            wpfMap.MouseDown -= Map_MouseDown;
            wpfMap.MouseDown += Map_MouseDown;
            wpfMap.ZoomLevelSetChanged -= WpfMap_ZoomLevelSetChanged;
            wpfMap.ZoomLevelSetChanged += WpfMap_ZoomLevelSetChanged;

            if (!wpfMap.MapTools.Any(t => t is NorthArrowMapTool))
            {
                wpfMap.MapTools.Add(new NorthArrowMapTool());
            }

            SwitcherPanZoomBarMapTool panZoom = wpfMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (panZoom != null)
            {
                panZoom.SwitcherModeChanged -= PanZoom_SwitcherModeChanged;
                panZoom.SwitcherModeChanged += PanZoom_SwitcherModeChanged;
                panZoom.GlobeButtonClick -= PanZoom_GlobeButtonClick;
                panZoom.GlobeButtonClick += PanZoom_GlobeButtonClick;
                PanZoom_SwitcherModeChanged(this, new SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs { NewSwitcherMode = SwitcherMode.Pan });
                panZoom.SwitcherMode = SwitcherMode.Pan;
            }
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            wpfMap.CurrentScaleChanged -= Map_CurrentScaleChanged;
            wpfMap.ExtentOverlay.MapMouseClick -= IdentifyInteractiveOverlay_MapMouseClick;
            wpfMap.ExtentOverlay.MapMouseDown -= IdentifyInteractiveOverlay_MapMouseDown;
            wpfMap.KeyDown -= Map_KeyDown;
            wpfMap.KeyUp -= Map_KeyUp;
            wpfMap.MouseDown -= Map_MouseDown;
            wpfMap.ZoomLevelSetChanged -= WpfMap_ZoomLevelSetChanged;

            NorthArrowMapTool northArrowMapTool = wpfMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
            if (northArrowMapTool != null)
            {
                wpfMap.MapTools.Remove(northArrowMapTool);
            }

            SwitcherPanZoomBarMapTool panZoom = wpfMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (panZoom != null)
            {
                panZoom.SwitcherModeChanged -= PanZoom_SwitcherModeChanged;
                panZoom.GlobeButtonClick -= PanZoom_GlobeButtonClick;
            }
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            MenuItem identifyMenuItem = new MenuItem();
            identifyMenuItem.Header = GisEditor.LanguageManager.GetStringResource("GenaralMenuItemIdentifyFeaturesHeader");
            identifyMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/identify.png", UriKind.RelativeOrAbsolute)) };
            identifyMenuItem.Click += new RoutedEventHandler(IdentifyMenuItem_Click);

            MenuItem validateItem = new MenuItem();
            validateItem.Header = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginValidatefeatureText");
            validateItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/validate.png", UriKind.RelativeOrAbsolute)) };
            validateItem.Tag = parameters;
            validateItem.Click += new RoutedEventHandler(ValidateItem_Click);

            MenuItem clearFeaturesMenuItem = new MenuItem();
            clearFeaturesMenuItem.Header = GisEditor.LanguageManager.GetStringResource("NavigatePluginClearSelectedFeaturesHeader");
            clearFeaturesMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/Clear.png", UriKind.RelativeOrAbsolute)), Width = 20, Height = 20 };
            clearFeaturesMenuItem.Click += new RoutedEventHandler(ClearFeaturesMenuItem_Click);

            Collection<MenuItem> menuItems = new Collection<MenuItem>();
            menuItems.Add(identifyMenuItem);
            //menuItems.Add(validateItem);
            menuItems.Add(clearFeaturesMenuItem);

            MenuItem plotPointMenuItem = new MenuItem();
            plotPointMenuItem.Header = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginPlotPointHereText");
            plotPointMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/plotpoint32.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            plotPointMenuItem.Click += new RoutedEventHandler(PlotPointMenuItemClick);
            menuItems.Add(plotPointMenuItem);

            MenuItem quickFilterMenuItem = new MenuItem();
            quickFilterMenuItem.Header = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginQuickFilterText");
            quickFilterMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/FilterStyle.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            quickFilterMenuItem.Click += new RoutedEventHandler(QuickFilterMenuItem);
            menuItems.Add(quickFilterMenuItem);

            SimpleMarkerOverlay currentMarkerOverlay = CurrentOverlays.PlottedMarkerOverlay;
            if (currentMarkerOverlay.Markers.Count > 0)
            {
                MenuItem clearPlottedMarkersMenuItem = new MenuItem();
                clearPlottedMarkersMenuItem.Header = "Clear plot points";
                clearPlottedMarkersMenuItem.Icon = new Image()
                {
                    Width = 16,
                    Height = 16,
                    Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/clearcache.png", UriKind.RelativeOrAbsolute))
                };
                clearPlottedMarkersMenuItem.Click += (s, e) =>
                {
                    PopupOverlay currentPopupOverlay = CurrentOverlays.PopupOverlay;
                    IEnumerable<Popup> plottedPopups = currentMarkerOverlay.Markers.Select(m => m.Tag).OfType<Popup>().ToList();
                    CurrentOverlays.PlottedMarkerOverlay.Markers.Clear();
                    foreach (var popup in plottedPopups)
                    {
                        currentPopupOverlay.Popups.Remove(popup);
                    }

                    GisEditor.ActiveMap.Refresh(currentMarkerOverlay);
                    GisEditor.ActiveMap.Refresh(currentPopupOverlay);
                    e.Handled = true;
                };
                menuItems.Add(clearPlottedMarkersMenuItem);
            }
            return menuItems;
        }

        private void PlotPointMenuItemClick(object sender, RoutedEventArgs e)
        {
            var mouseWorldCoordinate = GisEditor.ActiveMap.ToWorldCoordinate(mouseDownCoordinate);
            PlotPointViewModel.PlotPoint(new Vertex(mouseWorldCoordinate));
        }

        private void QuickFilterMenuItem(object sender, RoutedEventArgs e)
        {
            IdentifyMenuItem_Click(sender, e);

            var selectedFeature = GisEditor.SelectionManager.GetSelectedFeatures().FirstOrDefault();
            if (selectedFeature != null)
            {
                string columnName = selectedFeature.ColumnValues.Keys.First();
                if (!string.IsNullOrEmpty(columnName))
                {
                    string columnValue = selectedFeature.ColumnValues[columnName];
                    var selectLayer = GisEditor.SelectionManager.GetSelectedFeaturesLayer(selectedFeature);
                    FeatureInfoWindow.Instance.featureInfoControl.ViewModel.SelectedEntity = new FeatureViewModel(selectedFeature, selectLayer);
                    FeatureInfoWindow.Instance.featureInfoControl.AddQuickFilterStyle(columnName, columnValue);
                }
            }
        }

        private void ValidateItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem validateItem = (MenuItem)sender;
            GetMapContextMenuParameters parameters = (GetMapContextMenuParameters)validateItem.Tag;
            Collection<Feature> features = new Collection<Feature>();
            foreach (FeatureLayer featureLayer in GisEditor.ActiveMap.SelectionOverlay.FilteredLayers)
            {
                featureLayer.Open();
                ShapeFileFeatureLayer shpLayer = featureLayer as ShapeFileFeatureLayer;
                List<string> tempIds = null;
                if (shpLayer != null && shpLayer.FeatureIdsToExclude.Count > 0)
                {
                    tempIds = shpLayer.FeatureIdsToExclude.ToList();
                    shpLayer.FeatureIdsToExclude.Clear();
                }
                Collection<Feature> returnFeatures = featureLayer.QueryTools.GetFeaturesIntersecting(parameters.WorldCoordinates, ReturningColumnsType.NoColumns);
                if (returnFeatures.Count > 0)
                {
                    foreach (var item in returnFeatures)
                    {
                        features.Add(item);
                    }
                    break;
                }
                if (tempIds != null && tempIds.Count > 0)
                {
                    foreach (var item in tempIds)
                    {
                        shpLayer.FeatureIdsToExclude.Add(item);
                    }
                }
            }
            Feature feature = features.FirstOrDefault();

            if (feature != null)
            {
                //ShapeValidationResult result = feature.GetShape().Validate(ShapeValidationMode.Simple);
                if (SqlTypesGeometryHelper.IsValid(feature))
                {
                    MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NavigateUIPluginShapeisValidText"), "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string reason = SqlTypesGeometryHelper.GetInvalidReason(feature);
                    string message = string.Format(CultureInfo.InvariantCulture, "The clicked feature is invalid.\r\n\r\nFeature id: {0}\r\nReason: {1}", feature.Id, reason);
                    MessageBox.Show(message, "warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ProjectManager_Closing(object sender, EventArgs e)
        {
            var maps = GisEditor.GetMaps();
            foreach (var overlay in maps.SelectMany(m => m.Overlays))
            {
                overlay.Close();
            }

            Collection<string> physicalPaths = new Collection<string>();
            foreach (var map in maps)
            {
                if (map == null) continue;
                foreach (var layerGroup in map.GetFeatureLayers().GroupBy(l => l.GetType()))
                {
                    LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layerGroup.Key).FirstOrDefault();
                    if (layerPlugin != null)
                    {
                        foreach (var layer in layerGroup)
                        {
                            Uri layerUri = layerPlugin.GetUri(layer);
                            if (layerUri != null && layerUri.Scheme.ToUpper().Equals("FILE"))
                            {
                                physicalPaths.Add(layerUri.LocalPath);
                            }
                        }
                    }
                }
            }

            var directory = FolderHelper.GetCurrentProjectTaskResultFolder();

            if (!System.IO.Directory.Exists(directory)) return;

            string[] files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (!physicalPaths.Contains(System.IO.Path.GetFileNameWithoutExtension(file)))
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        }
                    }
                }
            }

            files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            if (files.Length < 1)
            {
                try
                {
                    System.IO.Directory.Delete(directory, true);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            }
        }

        private void ClearFeaturesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GisEditor.SelectionManager.ClearSelectedFeatures();
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.ClearFeaturesMenuItemClickDescription));
        }

        private void WpfMap_DisplayProjectionParametersChanged(object sender, DisplayProjectionParametersChangedGisEditorWpfMapEventArgs e)
        {
            GisEditorWpfMapExtension.ReprojectMap(sender as GisEditorWpfMap, e.OldProjectionParameters, e.NewProjectionParameters);
        }

        private void WpfMap_ZoomLevelSetChanged(object sender, ZoomLevelSetChangedWpfMapEventArgs e)
        {
            navigateGroup.ViewModel.SysnchCurrentZoomLevels(sender as GisEditorWpfMap);
        }

        private void IdentifyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var mouseWorldCoordinate = GisEditor.ActiveMap.ToWorldCoordinate(mouseDownCoordinate);
            GisEditor.ActiveMap.ShowIdentifyFeaturesWindow(mouseWorldCoordinate);
        }

        private void Map_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                mouseDownCoordinate = e.GetPosition((WpfMap)sender);
            }
        }

        private void Map_KeyUp(object sender, KeyEventArgs e)
        {
            if (GisEditor.ActiveMap != null)
            {
                if (e.Key == Key.LeftCtrl)
                {
                    SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                    if (previousSwitchMode.ContainsKey(GisEditor.ActiveMap))
                    {
                        panZoomBar.SwitcherMode = previousSwitchMode[GisEditor.ActiveMap];
                        previousSwitchMode.Clear();
                    }
                }

                if (e.Key == Key.Space || e.Key == Key.LeftShift || e.Key == Key.RightShift)
                {
                    GisEditor.ActiveMap.PreviewMouseDown -= new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                    GisEditor.ActiveMap.PreviewMouseMove -= new MouseEventHandler(ActiveMapDrag_MouseMove);
                    GisEditor.ActiveMap.PreviewMouseUp -= new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                    WpfMap extendedMap = GisEditor.ActiveMap;
                    if (extendedMap.ExtentOverlay != null && previousExtentOverlayEnabled.ContainsKey(extendedMap))
                    {
                        extendedMap.ExtentOverlay.OverlayCanvas.IsEnabled = previousExtentOverlayEnabled[extendedMap];
                    }

                    if (previousCursors.ContainsKey(extendedMap))
                    {
                        extendedMap.Cursor = previousCursors[extendedMap];
                        previousCursors.Remove(extendedMap);
                    }

                    GisEditor.ActiveMap.Refresh(GisEditor.SelectionManager.GetSelectionOverlay());
                    EndHighPriorityMouseOperation();
                }
            }
        }

        private void Map_KeyDown(object sender, KeyEventArgs e)
        {
            if (GisEditor.ActiveMap != null && !e.IsRepeat)
            {
                if (e.Key == Key.LeftCtrl)
                {
                    SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                    previousSwitchMode[GisEditor.ActiveMap] = panZoomBar.SwitcherMode;
                    panZoomBar.SwitcherMode = SwitcherMode.Identify;
                    e.Handled = true;
                }

                if (e.Key == Key.Space)
                {
                    GisEditor.ActiveMap.PreviewMouseDown -= new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                    GisEditor.ActiveMap.PreviewMouseMove -= new MouseEventHandler(ActiveMapDrag_MouseMove);
                    GisEditor.ActiveMap.PreviewMouseUp -= new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                    GisEditor.ActiveMap.PreviewMouseDown += new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                    GisEditor.ActiveMap.PreviewMouseMove += new MouseEventHandler(ActiveMapDrag_MouseMove);
                    GisEditor.ActiveMap.PreviewMouseUp += new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                    var extendedMap = GisEditor.ActiveMap;
                    previousCursors[extendedMap] = extendedMap.Cursor;
                    extendedMap.Cursor = GisEditorCursors.Pan;

                    e.Handled = true;
                }
                else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                {
                    var extendedMap = GisEditor.ActiveMap;
                    if (extendedMap.ExtentOverlay != null && GisEditor.SelectionManager.GetSelectionOverlay().TrackMode == TrackMode.None && !GisEditor.ActiveMap.FeatureLayerEditOverlay.IsEnabled)
                    {
                        GisEditor.ActiveMap.PreviewMouseDown -= new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                        GisEditor.ActiveMap.PreviewMouseMove -= new MouseEventHandler(ActiveMapDrag_MouseMove);
                        GisEditor.ActiveMap.PreviewMouseUp -= new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                        GisEditor.ActiveMap.PreviewMouseDown += new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                        GisEditor.ActiveMap.PreviewMouseMove += new MouseEventHandler(ActiveMapDrag_MouseMove);
                        GisEditor.ActiveMap.PreviewMouseUp += new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                        previousCursors[extendedMap] = extendedMap.Cursor;
                        previousExtentOverlayEnabled[extendedMap] = extendedMap.ExtentOverlay.OverlayCanvas.IsEnabled;
                        extendedMap.ExtentOverlay.OverlayCanvas.IsEnabled = true;
                        extendedMap.Cursor = GisEditorCursors.TrackZoom;

                        e.Handled = true;
                    }
                }
            }
        }

        private void CanClearPlottedPlaces(object sender, CanExecuteRoutedEventArgs e)
        {
            if (GisEditor.ActiveMap != null)
            {
                e.CanExecute = GisEditor.ActiveMap.Overlays.Contains("PopupOverlay") && CurrentOverlays.PopupOverlay.Popups.Count > 0;
            }
        }

        private void ClearPlottedPlaces(object sender, ExecutedRoutedEventArgs e)
        {
            if (GisEditor.ActiveMap.Overlays.Contains("PopupOverlay"))
            {
                var popupOverlay = CurrentOverlays.PopupOverlay;
                if (popupOverlay != null)
                {
                    popupOverlay.Popups.Clear();
                    popupOverlay.Refresh();
                }
            }
        }

        private void SearchPlaceUserControl_Plotted(object sender, PlotOnMapEventArgs e)
        {
            PointShape pointShape = e.Result.CentroidPoint;
            if (pointShape == null && e.Result.BoundingBox != null)
            {
                pointShape = e.Result.BoundingBox.GetCenterPoint();
            }

            if (pointShape != null)
            {
                string internalProjection = Proj4Projection.GetEpsgParametersString(4326);
                if (!string.IsNullOrEmpty(e.Result.InternalProjection))
                {
                    internalProjection = e.Result.InternalProjection;
                }

                Proj4Projection proj4 = new Proj4Projection();
                proj4.InternalProjectionParametersString = internalProjection;
                proj4.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                proj4.Open();

                PointShape position = (PointShape)proj4.ConvertToExternalProjection(pointShape);
                ClosablePopup popup = new ClosablePopup(position);
                popup.ParentMap = GisEditor.ActiveMap;
                popup.WorkingContent = e.Result.PlaceName;
                if (CurrentOverlays.PopupOverlay.Popups.All(p => p.Position.X != popup.Position.X && p.Position.Y != popup.Position.Y))
                {
                    CurrentOverlays.PopupOverlay.Popups.Add(popup);
                }
                if (e.Result.BoundingBox != null)
                {
                    RectangleShape extent = proj4.ConvertToExternalProjection(e.Result.BoundingBox);
                    GisEditor.ActiveMap.CurrentExtent = extent;
                    GisEditor.ActiveMap.Refresh();
                }
                else if (pointShape != null)
                {
                    GisEditor.ActiveMap.CenterAt((PointShape)proj4.ConvertToExternalProjection(pointShape));
                }
                proj4.Close();
            }
        }

        private void Map_CurrentScaleChanged(object sender, CurrentScaleChangedWpfMapEventArgs e)
        {
            WpfMap currentMap = (WpfMap)sender;
            double currentScale = currentMap.CurrentScale;
            var zoomLevels = currentMap.ZoomLevelSet.GetZoomLevels();
            foreach (ZoomLevelItemViewModel levelEntity in navigateGroup.ViewModel.CurrentZoomLevels)
            {
                if (levelEntity.ScaleIndex < zoomLevels.Count)
                {
                    //if (Math.Abs(zoomLevels[levelEntity.ScaleIndex].Scale - currentScale) < 1) levelEntity.IsChecked = true;
                    if (!(zoomLevels[levelEntity.ScaleIndex] is PreciseZoomLevel) && zoomLevels[levelEntity.ScaleIndex].Scale == currentScale) levelEntity.IsChecked = true;
                    else levelEntity.IsChecked = false;
                }
            }
        }

        private void ShowIdentifyHint()
        {
            if (MapHelper.ShowHintWindow("ShowIdentifyFeaturesHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginIdentifyWindowTitle");
                var description = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginIdentifyWindowDescription");
                var steps = new Collection<String>()
                            {
                                GisEditor.LanguageManager.GetStringResource("NavigateUIPluginIdentifyWindowStep1Select"),
                                GisEditor.LanguageManager.GetStringResource("NavigateUIPluginIdentifyWindowStep2Hold"),
                            };
                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Identify Multiples.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowIdentifyFeaturesHint", !result);
            }
        }

        private void ShowTrackZoomInHint()
        {
            if (MapHelper.ShowHintWindow("ShowTrackZoomInHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintWindowTitle");
                var description = GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintWindowDescription");
                var steps = new Collection<String>()
                {
                      GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintStep1Select"),
                      GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintStep2Press"),
                      GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintStep3Drag"),
                      GisEditor.LanguageManager.GetStringResource("NavigateUIPluginTrackZoomInHintStep4Release")
                };
                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Track Zoom with Shift.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowTrackZoomInHint", !result);
            }
        }

        private void PanZoom_SwitcherModeChanged(object sender, SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs e)
        {
            var extentOverlay = CurrentOverlays.ExtentOverlay;
            if (e.NewSwitcherMode != SwitcherMode.None)
            {
                GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(extentOverlay);
            }

            var oldPanMode = extentOverlay.PanMode;
            var oldLeftClickDragKey = extentOverlay.LeftClickDragKey;
            var newCursor = GisEditorCursors.Normal;
            switch (e.NewSwitcherMode)
            {
                case SwitcherMode.TrackZoom:
                    extentOverlay.PanMode = MapPanMode.Default;
                    extentOverlay.LeftClickDragKey = System.Windows.Forms.Keys.None;
                    newCursor = GisEditorCursors.TrackZoom;
                    GisEditor.ActiveMap.ExtentOverlay.OverlayCanvas.IsEnabled = true;
                    break;

                case SwitcherMode.Identify:
                    extentOverlay.PanMode = MapPanMode.Disabled;
                    extentOverlay.LeftClickDragKey = System.Windows.Forms.Keys.ShiftKey;
                    newCursor = GisEditorCursors.Identify;
                    GisEditor.ActiveMap.ExtentOverlay.OverlayCanvas.IsEnabled = true;
                    break;

                case SwitcherMode.None:
                    extentOverlay.PanMode = MapPanMode.Disabled;
                    extentOverlay.LeftClickDragKey = System.Windows.Forms.Keys.None;
                    newCursor = GisEditorCursors.Normal;
                    GisEditor.ActiveMap.ExtentOverlay.OverlayCanvas.IsEnabled = false;
                    break;

                case SwitcherMode.Pan:
                default:
                    extentOverlay.PanMode = MapPanMode.Default;
                    extentOverlay.LeftClickDragKey = System.Windows.Forms.Keys.ShiftKey;
                    GisEditor.ActiveMap.ExtentOverlay.OverlayCanvas.IsEnabled = true;
                    newCursor = GisEditorCursors.Pan;
                    break;
            }

            GisEditor.ActiveMap.Cursor = newCursor;
            if (oldPanMode != extentOverlay.PanMode || oldLeftClickDragKey != extentOverlay.LeftClickDragKey)
            {
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.PanZoomSwitcherModeChangedDescription));
            }
            if (e.IsSwitchedByMouse)
            {
                switch (e.NewSwitcherMode)
                {
                    case SwitcherMode.Pan:
                        ShowPanningHint();
                        break;

                    case SwitcherMode.TrackZoom:
                        ShowTrackZoomInHint();
                        break;

                    case SwitcherMode.Identify:
                        ShowIdentifyHint();
                        break;
                }
            }
        }

        private void ShowPanningHint()
        {
            if (MapHelper.ShowHintWindow("ShowPanningHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("PanningHintDialogSubTitle");
                var description = GisEditor.LanguageManager.GetStringResource("PanningHintDialogDescription");
                Collection<string> steps = new Collection<string>
                    {
                        GisEditor.LanguageManager.GetStringResource("PanningHintDialogFirstTip"),
                        GisEditor.LanguageManager.GetStringResource("PanningHintDialogSecondTip"),
                        GisEditor.LanguageManager.GetStringResource("PanningHintDialogTHirdTip"),
                        GisEditor.LanguageManager.GetStringResource("PanningHintDialogFourthTip")
                    };

                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/SpacebarPanning.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowPanningHint", !result);
            }
        }

        private void IdentifyInteractiveOverlay_MapMouseClick(object sender, MapMouseClickInteractiveOverlayEventArgs e)
        {
            if (e.InteractionArguments.MouseButton == MapMouseButton.Left)
            {
                SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                if (panZoomBar != null && panZoomBar.SwitcherMode == SwitcherMode.Identify)
                {
                    GisEditor.ActiveMap.ShowIdentifyFeaturesWindow(new PointShape(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY));
                    GisEditor.ActiveMap.Focus();
                }
            }
        }

        private void IdentifyInteractiveOverlay_MapMouseDown(object sender, MapMouseDownInteractiveOverlayEventArgs e)
        {
            SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();

            if (((e.InteractionArguments.MouseButton == MapMouseButton.Left)
                          || (e.InteractionArguments.MouseButton == MapMouseButton.Right)) && panZoomBar != null && panZoomBar.SwitcherMode == SwitcherMode.Identify)
            {
                trackStartScreenPoint = new Point(e.InteractionArguments.ScreenX, e.InteractionArguments.ScreenY);

                trackShape = new Rectangle();
                trackShape.SetValue(Grid.VerticalAlignmentProperty, VerticalAlignment.Top);
                trackShape.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                trackShape.SetValue(Grid.MarginProperty, new Thickness(trackStartScreenPoint.X, trackStartScreenPoint.Y, 0, 0));
                trackShape.SetValue(Grid.ZIndexProperty, 701);
                trackShape.Width = 1;
                trackShape.Height = 1;
                trackShape.Fill = new SolidColorBrush(Colors.White);
                trackShape.Stroke = new SolidColorBrush(Colors.Red);
                trackShape.Opacity = .5;

                GisEditor.ActiveMap.ToolsGrid.Children.Add(trackShape);
            }
        }

        private void IdentifyInteractiveOverlay_MapMouseMove(object sender, MapMouseMoveInteractiveOverlayEventArgs e)
        {
            SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();

            if (trackShape != null && panZoomBar != null && panZoomBar.SwitcherMode == SwitcherMode.Identify)
            {
                Point currentPosition = new Point(e.InteractionArguments.ScreenX, e.InteractionArguments.ScreenY);

                if (currentPosition.X < trackStartScreenPoint.X)
                {
                    trackShape.SetValue(Grid.MarginProperty, new Thickness(currentPosition.X, trackStartScreenPoint.Y, 0, 0));
                }

                if (currentPosition.Y < trackStartScreenPoint.Y)
                {
                    trackShape.SetValue(Grid.MarginProperty, new Thickness(trackStartScreenPoint.X, currentPosition.Y, 0, 0));
                }

                if (currentPosition.Y < trackStartScreenPoint.Y && currentPosition.X < trackStartScreenPoint.X)
                {
                    trackShape.SetValue(Grid.MarginProperty, new Thickness(currentPosition.X, currentPosition.Y, 0, 0));
                }

                trackShape.Width = Math.Abs(currentPosition.X - trackStartScreenPoint.X);
                trackShape.Height = Math.Abs(currentPosition.Y - trackStartScreenPoint.Y);
            }
        }

        private void PanZoom_GlobeButtonClick(object sender, GlobeButtonClickPanZoomBarMapToolEventArgs e)
        {
            Collection<RectangleShape> rectangles = new Collection<RectangleShape>();
            foreach (var overlay in GisEditor.ActiveMap.Overlays)
            {
                if (overlay is WorldMapKitMapOverlay || overlay is OpenStreetMapOverlay || overlay is BingMapsOverlay)
                {
                    continue;
                }
                RectangleShape rectangleShape = overlay.GetBoundingBox();
                if (rectangleShape != null)
                {
                    rectangles.Add(rectangleShape);
                }
            }
            if (rectangles.Count > 0)
            {
                e.NewExtent = ExtentHelper.GetBoundingBoxOfItems(rectangles);
            }
            else
            {
                Overlay baseOverlay = GisEditor.ActiveMap.Overlays.FirstOrDefault(overlay => overlay is WorldMapKitMapOverlay || overlay is OpenStreetMapOverlay || overlay is BingMapsOverlay);
                if (baseOverlay != null)
                    e.NewExtent = baseOverlay.GetBoundingBox();
                else
                    e.NewExtent = GisEditor.ActiveMap.MaxExtent;
            }
        }

        private void IdentifyInteractiveOverlay_MapMouseUp(object sender, MapMouseUpInteractiveOverlayEventArgs e)
        {
            SwitcherPanZoomBarMapTool panZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();

            if ((panZoomBar != null && panZoomBar.SwitcherMode == SwitcherMode.Identify))
            {
                Point currentScreenPoint = new Point(e.InteractionArguments.ScreenX, e.InteractionArguments.ScreenY);

                if (Math.Abs(trackStartScreenPoint.X - e.InteractionArguments.WorldX) > 0 && Math.Abs(trackStartScreenPoint.Y - e.InteractionArguments.WorldY) > 0)
                {
                    PointShape startPointInDegree = GisEditor.ActiveMap.ToWorldCoordinate(new PointShape(trackStartScreenPoint.X, trackStartScreenPoint.Y));
                    PointShape endPointInDegree = GisEditor.ActiveMap.ToWorldCoordinate(new PointShape(currentScreenPoint.X, currentScreenPoint.Y));
                    double minX = startPointInDegree.X < endPointInDegree.X ? startPointInDegree.X : endPointInDegree.X;
                    double maxX = startPointInDegree.X < endPointInDegree.X ? endPointInDegree.X : startPointInDegree.X;
                    double minY = startPointInDegree.Y < endPointInDegree.Y ? startPointInDegree.Y : endPointInDegree.Y;
                    double maxY = startPointInDegree.Y < endPointInDegree.Y ? endPointInDegree.Y : startPointInDegree.Y;
                    GisEditor.ActiveMap.ShowIdentifyFeaturesWindow(new RectangleShape(minX, maxY, maxX, minY));
                    GisEditor.ActiveMap.Focus();
                }

                GisEditor.ActiveMap.ToolsGrid.Children.Remove(trackShape);
                trackShape = null;

                if (previousSwitchMode.ContainsKey(GisEditor.ActiveMap))
                {
                    panZoomBar.SwitcherMode = previousSwitchMode[GisEditor.ActiveMap];
                    previousSwitchMode.Clear();
                }
            }

            var selectedFeatures = GisEditor.SelectionManager.GetSelectedFeatures();
            if (selectedFeatures.Count > 0)
            {
                var selectLayer = GisEditor.SelectionManager.GetSelectedFeaturesLayer(selectedFeatures.FirstOrDefault());

                foreach (var feature in selectedFeatures)
                {
                    if (!feature.ColumnValues.ContainsKey(SelectionUIPlugin.FeatureIdColumnName))
                    {
                        string featureId = feature.Id;

                        string featureIdColumn = LayerPluginHelper.GetFeatureIdColumn(selectLayer);
                        if (feature.ColumnValues.ContainsKey(featureIdColumn))
                        {
                            featureId = feature.ColumnValues[featureIdColumn];
                        }
                        //else if (feature.LinkColumnValues.ContainsKey(featureIdColumn))
                        //{
                        //    featureId = string.Join(Environment.NewLine, feature.LinkColumnValues[featureIdColumn].Select(f => f.Value));
                        //}

                        if (featureId.Contains(SelectionTrackInteractiveOverlay.FeatureIdSeparator))
                        {
                            featureId = featureId.Split(new string[] { SelectionTrackInteractiveOverlay.FeatureIdSeparator }, StringSplitOptions.RemoveEmptyEntries)[0];
                        }

                        feature.ColumnValues.Add(SelectionUIPlugin.FeatureIdColumnName, featureId);
                    }
                }

                GisEditor.ActiveMap.SelectionOverlay.Refresh();
            }
        }

        #region drag events

        private void EndHighPriorityMouseOperation()
        {
            if (isHighPriorityMouseOperation)
            {
                isHighPriorityMouseOperation = false;
                GisEditor.ActiveMap.Refresh(GisEditor.ActiveMap.InteractiveOverlays);
            }
        }

        private void ActiveMapDrag_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndHighPriorityMouseOperation();
            e.Handled = true;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                WpfMap currentMap = (WpfMap)sender;
                if (currentMap.ExtentOverlay != null)
                {
                    Point currentPosition = e.GetPosition(GisEditor.ActiveMap);
                    InteractionArguments arguments = CollectMouseEventArguments(currentPosition, currentMap);
                    InteractiveResult result = currentMap.ExtentOverlay.MouseUp(arguments);
                    if (result.NewCurrentExtent != null)
                    {
                        currentMap.CurrentExtent = result.NewCurrentExtent;
                        currentMap.Refresh();
                    }
                }
            }
        }

        private void ActiveMapDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighPriorityMouseOperation)
            {
                Point currentPosition = e.GetPosition(GisEditor.ActiveMap);
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    WpfMap currentMap = (WpfMap)sender;
                    if (currentMap.ExtentOverlay != null)
                    {
                        InteractionArguments arguments = CollectMouseEventArguments(currentPosition, currentMap);
                        currentMap.ExtentOverlay.MouseMove(arguments);
                    }
                }
                else
                {
                    double currentResolution = GisEditor.ActiveMap.CurrentResolution;
                    double offsetScreenX = currentPosition.X - originPosition.X;
                    double offsetScreenY = currentPosition.Y - originPosition.Y;

                    GisEditor.ActiveMap.Pan(-offsetScreenX, offsetScreenY);
                    GisEditor.ActiveMap.Cursor = GisEditorCursors.Grab;
                    originPosition = currentPosition;
                    e.Handled = true;
                }
            }
        }

        private void ActiveMapDrag_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            originPosition = e.GetPosition(GisEditor.ActiveMap);
            isHighPriorityMouseOperation = true;
            e.Handled = true;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                WpfMap currentMap = (WpfMap)sender;
                if (currentMap.ExtentOverlay != null)
                {
                    InteractionArguments arguments = CollectMouseEventArguments(originPosition, currentMap);
                    arguments.MouseButton = MapMouseButton.Left;
                    currentMap.ExtentOverlay.MouseDown(arguments);
                }
            }
        }

        private InteractionArguments CollectMouseEventArguments(Point currentScreenPoint, WpfMap wpfMap)
        {
            PointShape currentWorldPoint = wpfMap.ToWorldCoordinate(currentScreenPoint);
            InteractionArguments arguments = new InteractionArguments();
            arguments.CurrentExtent = wpfMap.CurrentExtent;
            arguments.MapHeight = (int)wpfMap.ActualHeight;
            arguments.MapWidth = (int)wpfMap.ActualWidth;
            arguments.MapUnit = wpfMap.MapUnit;
            arguments.MouseWheelDelta = 0;
            arguments.Scale = wpfMap.CurrentScale;
            arguments.ScreenX = (float)currentScreenPoint.X;
            arguments.ScreenY = (float)currentScreenPoint.Y;
            arguments.WorldX = currentWorldPoint.X;
            arguments.WorldY = currentWorldPoint.Y;
            return arguments;
        }

        #endregion drag events
    }
}