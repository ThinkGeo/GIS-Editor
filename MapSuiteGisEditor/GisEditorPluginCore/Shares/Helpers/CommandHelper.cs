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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Windows.Documents;
using System.Diagnostics;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class CommandHelper
    {
        private static readonly string overlayNamePattern = "(?<=Layer Group) \\d+";

        private static bool multiselect;
        private static bool isOpen = false;
        private static ObservedCommand addStyleCommand;
        private static ObservedCommand addLayerGroupCommand;
        private static ObservedCommand openViewDataWindowCommand;
        private static ObservedCommand openDataRepositoryWindowCommand;
        private static ObservedCommand reprojectionCommand;
        private static ObservedCommand printCommand;
        private static RelayCommand<string> rebuildDbf;
        private static RelayCommand<string> copyToClipboardCommand;
        private static ObservedCommand openFindFeaturesWindowCommand;
        private static ObservedCommand zoomToFullExtentCommand;
        private static ObservedCommand zoomToPreviousExtentCommand;
        private static ObservedCommand zoomInCommand;
        private static ObservedCommand zoomOutCommand;
        private static ObservedCommand openZoomLevelConfigWindowCommand;
        private static ObservedCommand openPlotPointWindowCommand;
        private static RelayCommand exportCommand;
        private static RelayCommand<BookmarkViewModel> zoomToBookmarkCommand;
        private static ObservedCommand zoomToSelectFeaturesCommand;
        private static ObservedCommand zoomToNextExtentCommand;
        private static ObservedCommand<bool> addNewLayersCommand;
        private static ObservedCommand removeAllLayersCommand;
        private static ObservedCommand refreshAllLayersCommand;
        private static ObservedCommand hideAllLayersCommand;
        private static ObservedCommand showAllLayersCommand;
        private static ObservedCommand<string> createNewLayerCommand;
        private static ObservedCommand openViewColumnsWindowCommand;
        private static RelayCommand<Hyperlink> gotoUriCommand;

        public static RelayCommand<Hyperlink> GotoUriCommand
        {
            get
            {
                if (gotoUriCommand == null)
                {
                    gotoUriCommand = new RelayCommand<Hyperlink>(sender =>
                    {
                        try
                        {
                            Hyperlink l = sender as Hyperlink;

                            if (l.NavigateUri.IsFile)
                            {
                                Process.Start(l.NavigateUri.AbsolutePath);
                            }
                            else
                            {
                                Process.Start(l.NavigateUri.AbsoluteUri);
                            }
                        }
                        catch { }
                    });
                }
                return gotoUriCommand;
            }
        }

        static CommandHelper()
        {
            multiselect = true;
        }

        public static ObservedCommand ShowAllLayersCommand
        {
            get
            {
                if (showAllLayersCommand == null)
                {
                    showAllLayersCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.Overlays.ForEach(o => o.IsVisible = true);
                        SetLayersVisible(true);
                    }, () => CheckMapIsNotNull() && GisEditor.ActiveMap.Overlays.Count > 0);
                }
                return showAllLayersCommand;
            }
        }

        public static ObservedCommand HideAllLayersCommand
        {
            get
            {
                if (hideAllLayersCommand == null)
                {
                    hideAllLayersCommand = new ObservedCommand(() =>
                    {
                        SetLayersVisible(false);
                    }, () => CheckMapIsNotNull() && GisEditor.ActiveMap.Overlays.Count > 0);
                }
                return hideAllLayersCommand;
            }
        }

        public static ObservedCommand RefreshAllLayersCommand
        {
            get
            {
                if (refreshAllLayersCommand == null)
                {
                    refreshAllLayersCommand = new ObservedCommand(() =>
                    {
                        foreach (var overlay in GisEditor.ActiveMap.Overlays)
                        {
                            TileOverlay tileOverlay = overlay as TileOverlay;
                            if (tileOverlay != null) tileOverlay.Invalidate(false);
                            else overlay.RefreshWithBufferSettings();
                        }
                    }, () => CheckMapIsNotNull() && GisEditor.ActiveMap.Overlays.Count > 0);
                }
                return refreshAllLayersCommand;
            }
        }

        public static ObservedCommand<string> CreateNewLayerCommand
        {
            get
            {
                if (createNewLayerCommand == null)
                {
                    createNewLayerCommand = new ObservedCommand<string>(pluginName =>
                    {
                        FeatureLayerPlugin layerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<FeatureLayerPlugin>()
                            .FirstOrDefault(p => p.Name.Equals(pluginName));

                        if (layerPlugin == null)
                        {
                            layerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<ShapeFileFeatureLayerPlugin>().FirstOrDefault();
                        }

                        if (layerPlugin != null)
                        {
                            ConfigureFeatureLayerParameters parameters = layerPlugin.GetConfigureFeatureLayerParameters();
                            if (parameters != null)
                            {
                                FeatureLayer featureLayer = layerPlugin.CreateFeatureLayer(parameters);
                                string message = string.Format(CultureInfo.InvariantCulture, "Add created {0} to current map?", layerPlugin.Name);
                                MessageBoxResult result = MessageBox.Show(message, "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                                if (result == MessageBoxResult.Yes)
                                {
                                    GetLayersParameters getLayersParameters = new GetLayersParameters();
                                    getLayersParameters.LayerUris.Add(parameters.LayerUri);
                                    foreach (var item in parameters.CustomData)
                                    {
                                        getLayersParameters.CustomData[item.Key] = item.Value;
                                    }
                                    Collection<Layer> layers = layerPlugin.GetLayers(getLayersParameters);

                                    if (layers.Count > 0)
                                    {
                                        if (layers[0] is FeatureLayer)
                                        {
                                            EditorUIPlugin.UpdateCalculatedRecords((FeatureLayer)layers[0], parameters.AddedColumns.Concat(parameters.UpdatedColumns.Values), false);
                                        }
                                        GisEditor.ActiveMap.AddLayersBySettings(layers);
                                        GisEditor.UIManager.BeginRefreshPlugins();
                                    }
                                }
                            }
                        }
                    }, pluginName => GisEditor.ActiveMap != null);
                }
                return createNewLayerCommand;
            }
        }

        public static ObservedCommand RemoveAllLayersCommand
        {
            get
            {
                if (removeAllLayersCommand == null)
                {
                    removeAllLayersCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.Overlays.Clear();
                        GisEditor.ActiveMap.ActiveLayer = null;
                        GisEditor.ActiveMap.ActiveOverlay = null;
                        GisEditor.ActiveMap.Refresh();
                        GisEditor.UIManager.RefreshPlugins();
                    }, () => CheckMapIsNotNull() && GisEditor.ActiveMap.Overlays.Count > 0);
                }
                return removeAllLayersCommand;
            }
        }

        internal static bool Multiselect
        {
            get { return multiselect; }
            set { multiselect = value; }
        }

        public static ObservedCommand<bool> AddNewLayersCommand
        {
            get
            {
                if (addNewLayersCommand == null)
                {
                    addNewLayersCommand = new ObservedCommand<bool>((multiselect) =>
                    {
                        ObservableCollection<LayerPlugin> supportedLayerProviders = new ObservableCollection<LayerPlugin>();

                        var contentUIPlugin = GisEditor.UIManager.GetUIPlugins().OfType<ContentUIPlugin>().FirstOrDefault();

                        if (contentUIPlugin != null)
                            contentUIPlugin.OnLayerPluginDropDownOpening(supportedLayerProviders);

                        if (GisEditor.LayerManager != null)
                        {
                            foreach (var provider in GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>())
                            {
                                supportedLayerProviders.Add(provider);
                            }
                        }

                        if (contentUIPlugin != null)
                            contentUIPlugin.OnLayerPluginDropDownOpened(supportedLayerProviders);

                        AddNewLayers(supportedLayerProviders, multiselect);
                    }, CheckMapIsNotNull);
                }
                return addNewLayersCommand;
            }
        }

        public static ObservedCommand AddStyleCommand
        {
            get
            {
                if (addStyleCommand == null)
                {
                    addStyleCommand = new ObservedCommand(() =>
                    {
                        if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                        var featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
                        if (featureLayer == null)
                        {
                            featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer;
                        }
                        if (featureLayer != null)
                        {
                            var styleArguments = new StyleBuilderArguments();
                            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                            switch (featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer))
                            {
                                case SimpleShapeType.Point:
                                    styleArguments.AvailableStyleCategories = StyleCategories.Point | StyleCategories.Label | StyleCategories.Composite;
                                    break;

                                case SimpleShapeType.Line:
                                    styleArguments.AvailableStyleCategories = StyleCategories.Line | StyleCategories.Label | StyleCategories.Composite;
                                    break;

                                case SimpleShapeType.Area:
                                    styleArguments.AvailableStyleCategories = StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                                    break;
                            }
                            var componentStyle = new CompositeStyle();
                            styleArguments.StyleToEdit = componentStyle;
                            styleArguments.FeatureLayer = featureLayer;
                            styleArguments.FromZoomLevelIndex = 1;
                            styleArguments.ToZoomLevelIndex = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels().Count;
                            styleArguments.AppliedCallback = new Action<StyleBuilderResult>((styleResult) =>
                            {
                                if (!styleResult.Canceled)
                                {
                                    foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
                                    {
                                        zoomLevel.CustomStyles.Remove(componentStyle);
                                    }
                                    ZoomLevelHelper.AddStyleToZoomLevels(styleResult.CompositeStyle, styleResult.FromZoomLevelIndex, styleResult.ToZoomLevelIndex, featureLayer.ZoomLevelSet.CustomZoomLevels);
                                    LayerListHelper.RefreshCache();
                                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(featureLayer, RefreshArgsDescription.AddStyleCommandDescription));
                                    componentStyle = styleResult.CompositeStyle as CompositeStyle;
                                }
                            });

                            styleArguments.FillRequiredColumnNames();
                            var styleResults = GisEditor.StyleManager.EditStyle(styleArguments);
                            styleArguments.AppliedCallback(styleResults);
                        }
                    }, () => GisEditor.LayerListManager.SelectedLayerListItem != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is FeatureLayer);
                }
                return addStyleCommand;
            }
        }

        public static RelayCommand<BookmarkViewModel> ZoomToBookmarkCommand
        {
            get
            {
                if (zoomToBookmarkCommand == null)
                {
                    zoomToBookmarkCommand = new RelayCommand<BookmarkViewModel>(tmpBookmark =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            var targetCenter = tmpBookmark.Center;
                            if (!tmpBookmark.InternalProj4Projection.Equals(GisEditor.ActiveMap.DisplayProjectionParameters))
                            {
                                Proj4Projection projection = new Proj4Projection();
                                projection.InternalProjectionParametersString = tmpBookmark.InternalProj4Projection;
                                projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                                projection.Open();
                                targetCenter = (PointShape)projection.ConvertToExternalProjection(targetCenter);
                                projection.Close();
                            }
                            GisEditor.ActiveMap.ZoomTo(targetCenter, tmpBookmark.Scale);
                        }
                    });
                }
                return zoomToBookmarkCommand;
            }
        }

        public static ObservedCommand PrintCommand
        {
            get
            {
                if (printCommand == null)
                {
                    printCommand = new ObservedCommand(() =>
                    {
                        PrintMapWindow printMapWindow = new PrintMapWindow();
                        printMapWindow.ShowDialog();
                    }, () => PrintMapViewModel.CanUsePrint);
                }
                return CommandHelper.printCommand;
            }
        }

        public static RelayCommand<string> RebuildDbf
        {
            get
            {
                if (rebuildDbf == null)
                {
                    rebuildDbf = new RelayCommand<string>((path) =>
                    {
                        string dbfPath = Path.ChangeExtension(path, ".dbf");

                        if (File.Exists(dbfPath))
                        {
                            File.SetAttributes(dbfPath, FileAttributes.Normal);

                            using (GeoDbf geoDbf = new GeoDbf(dbfPath, GeoFileReadWriteMode.ReadWrite))
                            {
                                geoDbf.Open();
                                int columnNumber = -1;
                                for (int i = 1; i <= geoDbf.ColumnCount; i++)
                                {
                                    string columnName = geoDbf.GetColumnName(i);
                                    if (columnName.Equals("RECID", StringComparison.OrdinalIgnoreCase))
                                    {
                                        columnNumber = i;
                                        break;
                                    }
                                }

                                if (columnNumber > -1)
                                {
                                    for (int i = 1; i <= geoDbf.RecordCount; i++)
                                    {
                                        geoDbf.WriteField(i, columnNumber, i);
                                    }
                                }
                            }
                        }
                    });
                }
                return rebuildDbf;
            }
        }

        public static ObservedCommand OpenZoomLevelConfigWindowCommand
        {
            get
            {
                if (openZoomLevelConfigWindowCommand == null)
                {
                    openZoomLevelConfigWindowCommand = new ObservedCommand(() =>
                    {
                        ZoomLevelSetConfigurationWindow configWindow = new ZoomLevelSetConfigurationWindow();
                        configWindow.ApplyAction = ApplyNewZoomLevelSet;
                        if (configWindow.ShowDialog().GetValueOrDefault())
                        {
                            var viewModel = configWindow.DataContext as ZoomLevelConfigurationViewModel;
                            if (viewModel != null) ApplyNewZoomLevelSet(viewModel.ZoomLevelSetViewModel.Select(z => z.Scale).ToList());
                        }
                    }, CheckMapIsNotNull);
                }
                return openZoomLevelConfigWindowCommand;
            }
        }

        public static ObservedCommand OpenPlotPointWindowCommand
        {
            get
            {
                if (openPlotPointWindowCommand == null)
                {
                    openPlotPointWindowCommand = new ObservedCommand(() =>
                    {
                        PlotPointWindow plotPointWindow = new PlotPointWindow();
                        plotPointWindow.ShowDialog();
                    }, CheckMapIsNotNull);
                }
                return openPlotPointWindowCommand;
            }
        }

        public static ObservedCommand ZoomOutCommand
        {
            get
            {
                if (zoomOutCommand == null)
                {
                    zoomOutCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.ZoomOut();
                    }, CheckMapIsNotNull);
                }
                return zoomOutCommand;
            }
        }

        public static ObservedCommand ZoomInCommand
        {
            get
            {
                if (zoomInCommand == null)
                {
                    zoomInCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.ZoomIn();
                    }, CheckMapIsNotNull);
                }
                return zoomInCommand;
            }
        }

        public static ObservedCommand ZoomToPreviousExtentCommand
        {
            get
            {
                if (zoomToPreviousExtentCommand == null)
                {
                    zoomToPreviousExtentCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.ZoomToPreviousExtent();
                    }, CheckMapIsNotNull);
                }
                return zoomToPreviousExtentCommand;
            }
        }

        public static ObservedCommand ZoomToNextExtentCommand
        {
            get
            {
                if (zoomToNextExtentCommand == null)
                {
                    zoomToNextExtentCommand = new ObservedCommand(() =>
                    {
                        GisEditor.ActiveMap.ZoomToNextExtent();
                    }, CheckMapIsNotNull);
                }
                return zoomToNextExtentCommand;
            }
        }

        public static ObservedCommand ZoomToFullExtentCommand
        {
            get
            {
                if (zoomToFullExtentCommand == null)
                {
                    zoomToFullExtentCommand = new ObservedCommand(() =>
                    {
                        double left = double.MaxValue;
                        double right = double.MinValue;
                        double top = double.MinValue;
                        double bottom = double.MaxValue;
                        RectangleShape fullExtent = GisEditor.ActiveMap.MaxExtent;

                        foreach (Overlay overlay in GisEditor.ActiveMap.Overlays)
                        {
                            if (overlay is WorldMapKitMapOverlay || overlay is OpenStreetMapOverlay || overlay is BingMapsOverlay)
                            {
                                continue;
                            }
                            RectangleShape bbox = overlay.GetBoundingBox();
                            if (bbox != null)
                            {
                                left = left < bbox.LowerLeftPoint.X ? left : bbox.LowerLeftPoint.X;
                                right = right > bbox.LowerRightPoint.X ? right : bbox.LowerRightPoint.X;
                                top = top > bbox.UpperLeftPoint.Y ? top : bbox.UpperLeftPoint.Y;
                                bottom = bottom < bbox.LowerRightPoint.Y ? bottom : bbox.LowerRightPoint.Y;
                            }
                        }

                        if (left < right && top > bottom)
                        {
                            fullExtent = new RectangleShape(left, top, right, bottom);
                        }
                        else
                        {
                            Overlay baseOverlay = GisEditor.ActiveMap.Overlays.FirstOrDefault(overlay => overlay is WorldMapKitMapOverlay || overlay is OpenStreetMapOverlay || overlay is BingMapsOverlay);
                            if (baseOverlay != null)
                                fullExtent = baseOverlay.GetBoundingBox();
                        }

                        GisEditor.ActiveMap.CurrentExtent = fullExtent;
                        GisEditor.ActiveMap.Refresh();
                    }, CheckMapIsNotNull);
                }
                return zoomToFullExtentCommand;
            }
        }

        public static ObservedCommand ZoomToSelectFeaturesCommand
        {
            get
            {
                if (zoomToSelectFeaturesCommand == null)
                {
                    zoomToSelectFeaturesCommand = new ObservedCommand(() =>
                    {
                        RectangleShape extent = ExtentHelper.GetBoundingBoxOfItems(GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures);
                        GisEditor.ActiveMap.CurrentExtent = extent;
                        GisEditor.ActiveMap.Refresh();
                    }, () => GisEditor.SelectionManager.GetSelectionOverlay() != null && GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures.Count > 0);
                }
                return zoomToSelectFeaturesCommand;
            }
        }

        public static ObservedCommand OpenFindFeaturesWindowCommand
        {
            get
            {
                if (openFindFeaturesWindowCommand == null)
                {
                    openFindFeaturesWindowCommand = new ObservedCommand(() => QueryFeatureLayerWindow.OpenQuery()
                        , () => GisEditor.ActiveMap != null && !isOpen);
                }
                return openFindFeaturesWindowCommand;
            }
        }

        public static ObservedCommand ReprojectionCommand
        {
            get
            {
                if (reprojectionCommand == null)
                {
                    reprojectionCommand = new ObservedCommand(() =>
                    {
                        string description = GisEditor.LanguageManager.GetStringResource("selectAProjectionForAllLayersDescription");
                        ProjectionWindow projectionWindow = new ProjectionWindow(GisEditor.ActiveMap.DisplayProjectionParameters
                            , description
                            , "");
                        if (projectionWindow.ShowDialog().GetValueOrDefault())
                        {
                            string selectedProj4Parameter = projectionWindow.Proj4ProjectionParameters;
                            if (!string.IsNullOrEmpty(selectedProj4Parameter))
                            {
                                string originalProj4String = GisEditor.ActiveMap.DisplayProjectionParameters;
                                try
                                {
                                    GisEditor.ActiveMap.DisplayProjectionParameters = selectedProj4Parameter;
                                }
                                catch (Exception ex)
                                {
                                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                    GisEditor.ActiveMap.DisplayProjectionParameters = originalProj4String;
                                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("CommandHelperCannotReprojectText")
                                        , GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                                }
                            }

                            //else
                            //{
                            //    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("CommandHelperPleaseSelectProjectionText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                            //}
                        }
                    }, CheckMapIsNotNull);
                }
                return reprojectionCommand;
            }
        }

        public static RelayCommand<string> CopyToClipboardCommand
        {
            get
            {
                if (copyToClipboardCommand == null)
                {
                    copyToClipboardCommand = new RelayCommand<string>(message =>
                    {
                        if (!string.IsNullOrEmpty(message))
                        {
                            Clipboard.SetDataObject(message);
                        }
                    });
                }
                return copyToClipboardCommand;
            }
        }

        public static ObservedCommand OpenViewColumnsWindowCommand
        {
            get
            {
                if (openViewColumnsWindowCommand == null)
                {
                    openViewColumnsWindowCommand = new ObservedCommand(() =>
                    {
                        FeatureLayer selectedLayer = GisEditor.ActiveMap.ActiveLayer as FeatureLayer;
                        if (selectedLayer != null)
                        {
                            ViewColumnsWindow window = new ViewColumnsWindow(selectedLayer);
                            window.Owner = Application.Current.MainWindow;
                            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            if (window.ShowDialog().GetValueOrDefault())
                            {
                                Dictionary<string, string> aliasNames = window.AliasNames;
                                foreach (var item in aliasNames)
                                {
                                    selectedLayer.FeatureSource.SetColumnAlias(item.Key, item.Value, () =>
                                    {
                                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("CommandHelperDuplicateColumnAliasText"));
                                    });
                                }
                            }
                        }
                    }, () => GisEditor.ActiveMap != null && GisEditor.ActiveMap.ActiveLayer != null && GisEditor.ActiveMap.ActiveLayer is FeatureLayer);
                }
                return openViewColumnsWindowCommand;
            }
        }

        public static ObservedCommand OpenViewDataWindowCommand
        {
            get
            {
                if (openViewDataWindowCommand == null)
                {
                    openViewDataWindowCommand = new ObservedCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl();
                        content.ShowDock();
                    }, () => GisEditor.ActiveMap != null && GisEditor.ActiveMap.GetFeatureLayers(true).Count() > 0);
                }
                return openViewDataWindowCommand;
            }
        }

        public static ObservedCommand OpenDataRepositoryWindowCommand
        {
            get
            {
                if (openDataRepositoryWindowCommand == null)
                {
                    openDataRepositoryWindowCommand = new ObservedCommand(() =>
                    {
                        var dataRepositoryUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<DataRepositoryUIPlugin>().FirstOrDefault();
                        if (dataRepositoryUIPlugin != null && dataRepositoryUIPlugin.DockWindows.Count > 0)
                        {
                            dataRepositoryUIPlugin.DockWindows.First().Show(DockWindowPosition.Right);
                        }
                    }, () => { return CheckMapIsNotNull() && CheckDataRepositoryUIPluginIsExist(); });
                }
                return openDataRepositoryWindowCommand;
            }
        }

        public static ObservedCommand AddLayerGroupCommand
        {
            get
            {
                if (addLayerGroupCommand == null)
                {
                    addLayerGroupCommand = new ObservedCommand(() =>
                    {
                        LayerOverlay emptyLayerOverlay = new GisEditorLayerOverlay
                        {
                            LockLayerMode = LockLayerMode.Lock,
                            Name = GetLayerOverlayName(),
                            TileBuffer = 1,
                            TileType = TileType.HybridTile,
                            DrawingExceptionMode = DrawingExceptionMode.DrawException,
                            TileWidth = Singleton<ContentSetting>.Instance.TileSize,
                            TileHeight = Singleton<ContentSetting>.Instance.TileSize,
                            DrawingQuality = Singleton<ContentSetting>.Instance.HighQuality ? DrawingQuality.HighQuality : DrawingQuality.HighSpeed
                        };

                        emptyLayerOverlay.RefreshCache(Singleton<ContentSetting>.Instance.UseCache);

                        if (GisEditor.ActiveMap != null)
                        {
                            DynamicLayerOverlay dynamicLayerOverlay = GisEditor.ActiveMap.Overlays.FirstOrDefault(o => o is DynamicLayerOverlay) as DynamicLayerOverlay;
                            if (dynamicLayerOverlay != null)
                            {
                                int index = GisEditor.ActiveMap.Overlays.IndexOf(dynamicLayerOverlay);
                                GisEditor.ActiveMap.Overlays.Insert(index, emptyLayerOverlay);
                            }
                            else GisEditor.ActiveMap.Overlays.Add(emptyLayerOverlay);
                            GisEditor.ActiveMap.ActiveOverlay = emptyLayerOverlay;
                            GisEditor.ActiveMap.Refresh(emptyLayerOverlay);
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.ActiveMap, RefreshArgsDescription.AddLayerGroupCommandDescription));
                        }
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return addLayerGroupCommand;
            }
        }

        public static RelayCommand ExportCommand
        {
            get
            {
                if (exportCommand == null)
                {
                    exportCommand = new RelayCommand(() =>
                        {
                            PngExportStrategy pngExportStrategy = new PngExportStrategy();
                            pngExportStrategy.Export();
                        });
                }

                return exportCommand;
            }
        }

        private static void AddNewLayers(IEnumerable<LayerPlugin> fileLayerPlugins, bool multiselect)
        {
            if (fileLayerPlugins.Count() > 0)
            {
                StringBuilder filterStringBuilder = new StringBuilder();
                StringBuilder allFilesFilterStringBuilder = new StringBuilder();

                foreach (var fileLayerPlugin in fileLayerPlugins)
                {
                    string[] array = fileLayerPlugin.ExtensionFilter.Split('|');
                    if (array != null && array.Length >= 2)
                    {
                        allFilesFilterStringBuilder.Append(array[1] + ";");
                    }
                }

                filterStringBuilder.Append("All Supported Formats|" + allFilesFilterStringBuilder.ToString());

                foreach (var fileLayerPlugin in fileLayerPlugins)
                {
                    if (!string.IsNullOrEmpty(fileLayerPlugin.ExtensionFilter))
                        filterStringBuilder.Append("|" + fileLayerPlugin.ExtensionFilter);
                }

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = multiselect;
                openFileDialog.Filter = filterStringBuilder.ToString();
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    try
                    {
                        AddToDataRepository(openFileDialog.FileNames[0]);
                        DataRepositoryHelper.PlaceFilesOnMap(openFileDialog.FileNames);
                    }
                    catch (Exception ex)
                    {
                        //SendExceptionMessage("Warning", ex.Message);
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        MessageBox.Show(ex.Message, "Unable to Open File(s)");
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("LayerPluginManagerNotFoundText")
                    , GisEditor.LanguageManager.GetStringResource("LayerPluginManagerNotFoundCaption")
                    , System.Windows.Forms.MessageBoxButtons.OK
                    , System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        public static void AddToDataRepository(string filePath)
        {
            if (Singleton<ContentSetting>.Instance.IsShowAddDataRepositoryDialog)
            {
                var folderPlugin = GisEditor.DataRepositoryManager.GetDataRepositoryPlugins().OfType<FolderDataRepositoryPlugin>().FirstOrDefault();
                var folderPath = Path.GetDirectoryName(filePath);
                var existingFolders = folderPlugin.RootDataRepositoryItem.Children.OfType<FolderDataRepositoryItem>().Select(f => f.FolderInfo.FullName);
                if (!existingFolders.Contains(folderPath))
                {
                    var addToDataRepositoryWindow = new AddToDataRepositoryWindow();
                    if (addToDataRepositoryWindow.ShowDialog().GetValueOrDefault())
                    {
                        folderPlugin.RootDataRepositoryItem.Children.Add(new FolderDataRepositoryItem(folderPath, true));
                    }
                }
            }
        }

        public static void CloseFindFeaturesWindow()
        {
            QueryFeatureLayerWindow.CloseQuery();
        }

        private static bool CheckDataRepositoryUIPluginIsExist()
        {
            return GisEditor.UIManager.GetActiveUIPlugins<DataRepositoryUIPlugin>().FirstOrDefault() != null;
        }

        public static bool CheckMapIsNotNull()
        {
            return GisEditor.ActiveMap != null;
        }

        public static bool CheckMapIsNotNull<T>(T parameter)
        {
            return GisEditor.ActiveMap != null;
        }

        private static string GetLayerOverlayName()
        {
            string overlayName = "Layer Group {0}";
            int maxValue = 0;
            foreach (var layerOverlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>())
            {
                string match = Regex.Match(layerOverlay.Name, overlayNamePattern).Value;
                int currentValue = 0;
                if (!string.IsNullOrEmpty(match) && int.TryParse(match, out currentValue))
                {
                    maxValue = maxValue > currentValue ? maxValue : currentValue;
                }
            }
            return string.Format(CultureInfo.InvariantCulture, overlayName, maxValue + 1);
        }

        private static void ChangeAllLayersZoomLevelSet(GisEditorWpfMap map, IEnumerable<ZoomLevel> zoomLevels)
        {
            var zoomLevelSet = new ZoomLevelSet();
            map.ZoomLevelSet.CustomZoomLevels.Clear();
            foreach (var item in zoomLevels)
            {
                zoomLevelSet.CustomZoomLevels.Add(item);
            }
            map.ZoomLevelSet = zoomLevelSet;
            map.MinimumScale = map.ZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale;
            var allFeatureLayers = map.GetFeatureLayers();
            foreach (var featureLayer in allFeatureLayers)
            {
                var originalZoomLevels = featureLayer.ZoomLevelSet.CustomZoomLevels.ToList();
                featureLayer.ZoomLevelSet.CustomZoomLevels.Clear();
                foreach (var item in zoomLevels)
                {
                    var max = item.Scale * 2;
                    var min = item.Scale * 0.5;
                    ZoomLevel newZoomLevel = null;
                    if (item is PreciseZoomLevel)
                    {
                        newZoomLevel = new PreciseZoomLevel(item.Scale);
                    }
                    else
                    {
                        newZoomLevel = new ZoomLevel(item.Scale);
                    }
                    foreach (var style in item.CustomStyles)
                    {
                        newZoomLevel.CustomStyles.Add(style);
                    }

                    var styles = originalZoomLevels.Where(z => z.Scale < max && z.Scale > min).SelectMany(z => z.CustomStyles);
                    foreach (var style in styles)
                    {
                        if (!newZoomLevel.CustomStyles.Contains(style)) newZoomLevel.CustomStyles.Add(style);
                    }
                    featureLayer.ZoomLevelSet.CustomZoomLevels.Add(newZoomLevel);
                }
            }
            foreach (var layerOverlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>())
            {
                if (layerOverlay.MapArguments != null)
                {
                    // layerOverlay.Refresh();
                    layerOverlay.RefreshWithBufferSettings();
                }
            }
        }

        public static void ApplyNewZoomLevelSet(IEnumerable<double> scales)
        {
            ApplyNewZoomLevelSet(scales.Select(s => new ZoomLevel(s)));
        }

        public static void ApplyNewZoomLevelSet(IEnumerable<ZoomLevel> zoomLevels)
        {
            foreach (var map in GisEditor.DockWindowManager.DocumentWindows.Select(d => d.Content).OfType<GisEditorWpfMap>())
            {
                ChangeAllLayersZoomLevelSet(map, zoomLevels);
                var panZoomBar = map.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                if (panZoomBar != null) panZoomBar.OnApplyTemplate();
            }
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.DockWindowManager.DocumentWindows, RefreshArgsDescription.ApplyNewZoomLevelSetDescription));
            GisEditor.ActiveMap.Refresh();
        }

        private static void SetLayersVisible(bool isVisible)
        {
            foreach (var layer in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>().SelectMany(o => o.Layers).ToList())
            {
                layer.IsVisible = isVisible;
            }

            RefreshAllLayersCommand.Execute(null);
            GisEditor.UIManager.RefreshPlugins();
        }
    }
}
