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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal class LayerListHelper
    {
        private static readonly string upIconPackUri = "/GisEditorPluginCore;component/Images/up.png";
        private static bool stopRefresh;

        public static Collection<Layer> AddDropFilesToActiveMap(DragEventArgs e, bool refreshPlugins = true)
        {
            Collection<Layer> layersToAdd = new Collection<Layer>();
            var dropFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropFiles != null)
            {
                var dropFileGroups = dropFiles.GroupBy(tmpFile => Path.GetExtension(tmpFile).ToUpperInvariant());
                var unsupportedExtensions = new StringBuilder();
                foreach (var fileGroup in dropFileGroups)
                {
                    var ext = fileGroup.Key;
                    var matchingLayerProvider = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>()
                        .FirstOrDefault(tmpPlugin => tmpPlugin.ExtensionFilter.ToUpperInvariant().Contains(ext));

                    if (matchingLayerProvider != null)
                    {
                        Collection<Layer> layers = new Collection<Layer>();
                        var getLayersParameters = new GetLayersParameters();
                        foreach (var item in fileGroup)
                        {
                            getLayersParameters.LayerUris.Add(new Uri(item));
                        }
                        try
                        {
                            layers = matchingLayerProvider.GetLayers(getLayersParameters);
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                        finally
                        {
                            foreach (var tmpLayer in layers)
                            {
                                layersToAdd.Add(tmpLayer);
                            }
                        }
                    }
                    else
                    {
                        unsupportedExtensions.AppendFormat(CultureInfo.InvariantCulture, "{0}, ", fileGroup.Key);
                    }
                }

                if (layersToAdd.Count > 0 && GisEditor.ActiveMap != null)
                {
                    GisEditor.ActiveMap.AddLayersBySettings(layersToAdd, true);
                    if (refreshPlugins)
                    {
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(null, RefreshArgsDescription.MapDropDescription));
                    }
                }

                if (unsupportedExtensions.Length > 0 && Application.Current != null)
                {
                    System.Windows.Forms.MessageBox.Show(String.Format(CultureInfo.InvariantCulture, "The following data type is not supported:\r\n\r\n {0}", unsupportedExtensions.ToString().Trim(' ', ','))
                        , "Unsupported data type error"
                        , System.Windows.Forms.MessageBoxButtons.OK
                        , System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
            return layersToAdd;
        }

        public static LayerListItem CreateLayerListItemForMeasureOverlay(LayerListItem overlayEntity)
        {
            var measureOverlay = overlayEntity.ConcreteObject as MeasureTrackInteractiveOverlay;
            if (measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0)
            {
                overlayEntity.Name = "Measurements";
                overlayEntity.ChildrenContainerVisibility = Visibility.Visible;
                overlayEntity.ContextMenuItems.Clear();
                overlayEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetZoomToExtentMenuItem());
                overlayEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetRemoveOverlayMenuItem());
                overlayEntity.PropertyChanged += new PropertyChangedEventHandler(OverlayEntity_PropertyChanged);
                overlayEntity.SideImage = new Image { Source = new BitmapImage(new Uri(upIconPackUri, UriKind.Relative)) };
                overlayEntity.IsChecked = measureOverlay.ShapeLayer.MapShapes.Any(m => m.Value.ZoomLevels.ZoomLevel01.IsActive);

                foreach (var subEntity in CollectSubEntities(measureOverlay))
                {
                    subEntity.Parent = overlayEntity;
                    overlayEntity.Children.Add(subEntity);
                }
            }
            else overlayEntity = null;

            return overlayEntity;
        }

        public static void RefreshInteractiveOverlays(LayerListItem layerListItem)
        {
            var editOverlay = GisEditor.ActiveMap.InteractiveOverlays
                .OfType<GisEditorEditInteractiveOverlay>().FirstOrDefault();

            if (editOverlay != null)
            {
                if (layerListItem.ConcreteObject is FeatureLayer && editOverlay.EditTargetLayer == layerListItem.ConcreteObject)
                {
                    ClearEditOverlay(editOverlay);
                }
                else if (layerListItem.ConcreteObject is LayerOverlay && ((LayerOverlay)layerListItem.ConcreteObject).Layers.Contains((Layer)editOverlay.EditTargetLayer))
                {
                    ClearEditOverlay(editOverlay);
                }
            }

            if (layerListItem.ConcreteObject is LayerOverlay)
            {
                foreach (var item in ((LayerOverlay)layerListItem.ConcreteObject).Layers.OfType<FeatureLayer>())
                {
                    GisEditor.SelectionManager.ClearSelectedFeatures(item);
                }
            }
            else if (layerListItem.ConcreteObject is FeatureLayer)
            {
                GisEditor.SelectionManager.ClearSelectedFeatures((FeatureLayer)layerListItem.ConcreteObject);
            }
        }

        public static T FindMapElementInLayerList<T>(LayerListItem layerListItem) where T : class
        {
            var resultViewModel = FindItemInLayerList<T>(layerListItem);
            if (resultViewModel != null) return resultViewModel.ConcreteObject as T;
            else return null;
        }

        public static LayerListItem FindItemInLayerList<T>(LayerListItem layerListItem) where T : class
        {
            return FindViewModelInTree(layerListItem, new Func<object, bool>((actualMapElement) => actualMapElement is T));
        }

        internal static void AddStyle(Styles.Style style, FeatureLayer layer)
        {
            var styleProvider = GisEditor.StyleManager.GetStylePluginByStyle(style);
            if (styleProvider == null) return;
            Styles.Style csvStyle = styleProvider.GetDefaultStyle();
            StyleBuilderArguments arguments = new StyleBuilderArguments();
            arguments.AvailableUIElements = StyleBuilderUIElements.ZoomLevelPicker | StyleBuilderUIElements.StyleList;
            arguments.FeatureLayer = layer;
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null)
            {
                switch (featureLayerPlugin.GetFeatureSimpleShapeType(layer))
                {
                    case SimpleShapeType.Point:
                        arguments.AvailableStyleCategories = StyleCategories.Point | StyleCategories.Label | StyleCategories.Composite;
                        break;
                    case SimpleShapeType.Line:
                        arguments.AvailableStyleCategories = StyleCategories.Line | StyleCategories.Label | StyleCategories.Composite;
                        break;
                    case SimpleShapeType.Area:
                        arguments.AvailableStyleCategories = StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                        break;
                }
            }
            arguments.AppliedCallback = args =>
            {
                if (args.CompositeStyle != null)
                {
                    ZoomLevelHelper.ApplyStyle(args.CompositeStyle, layer, args.FromZoomLevelIndex, args.ToZoomLevelIndex);
                }
            };

            arguments.StyleToEdit = new CompositeStyle(new Styles.Style[] { csvStyle }) { Name = styleProvider.Name };
            arguments.FillRequiredColumnNames();
            var resultStyle = GisEditor.StyleManager.EditStyle(arguments);
            if (!resultStyle.Canceled)
            {
                ZoomLevelHelper.ApplyStyle(resultStyle.CompositeStyle, layer, resultStyle.FromZoomLevelIndex, resultStyle.ToZoomLevelIndex);
            }
        }

        public static void EditStyle(LayerListItem selectedLayerListItem)
        {
            var componentStyleItem = LayerListHelper.FindItemInLayerList<CompositeStyle>(selectedLayerListItem) as StyleLayerListItem;
            if (componentStyleItem != null)
            {
                var componentStyle = componentStyleItem.ConcreteObject as CompositeStyle;
                var styleArguments = new StyleBuilderArguments();
                styleArguments.FeatureLayer = componentStyleItem.Parent.ConcreteObject as FeatureLayer;
                var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(styleArguments.FeatureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (featureLayerPlugin != null)
                {

                    styleArguments.AvailableStyleCategories = StylePluginHelper.GetStyleCategoriesByFeatureLayer(styleArguments.FeatureLayer);
                    int from = 0;
                    int to = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count;
                    if (!string.IsNullOrEmpty(componentStyleItem.ZoomLevelRange))
                    {
                        var array = componentStyleItem.ZoomLevelRange.Split(" to ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (array.Length == 2)
                        {
                            int.TryParse(array[0].Replace("(", "").Trim(), out from);
                            int.TryParse(array[1].Replace(")", "").Trim(), out to);
                        }
                    }

                    styleArguments.FromZoomLevelIndex = from;
                    styleArguments.ToZoomLevelIndex = to;
                    styleArguments.AppliedCallback = new Action<StyleBuilderResult>((styleResults) =>
                    {
                        if (!styleResults.Canceled)
                        {
                            var resultStyle = styleResults.CompositeStyle as CompositeStyle;
                            var count = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels().Count;
                            for (int i = 0; i < count; i++)
                            {
                                var customStyles = styleArguments.FeatureLayer.ZoomLevelSet.CustomZoomLevels[i].CustomStyles;
                                if (i >= styleResults.FromZoomLevelIndex - 1 && i < styleResults.ToZoomLevelIndex)
                                {
                                    if (!customStyles.Contains(componentStyle)) customStyles.Add(componentStyle);
                                    componentStyle.Styles.Clear();
                                    componentStyle.Name = resultStyle.Name;
                                    foreach (var item in resultStyle.Styles)
                                    {
                                        componentStyle.Styles.Add(item);
                                    }
                                }
                                else customStyles.Remove(componentStyle);
                            }
                            foreach (var overlay in GisEditor.ActiveMap.GetOverlaysContaining(styleArguments.FeatureLayer))
                            {
                                if (overlay.MapArguments != null)
                                {
                                    overlay.Invalidate();
                                }
                            }
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(styleResults, RefreshArgsDescription.EditStyleDescription));
                        }
                    });
                    var styleItems = new Collection<StyleLayerListItem>();
                    foreach (var style in componentStyle.Styles)
                    {
                        var item = GisEditor.StyleManager.GetStyleLayerListItem(style);
                        if (item != null)
                            styleItems.Add(item);
                    }

                    var clonedStyleItems = new Collection<StyleLayerListItem>();
                    var clonedCompositeStyle = componentStyle.CloneDeep() as CompositeStyle;
                    styleArguments.StyleToEdit = clonedCompositeStyle;

                    foreach (var style in clonedCompositeStyle.Styles)
                    {
                        var item = GisEditor.StyleManager.GetStyleLayerListItem(style);
                        if (item != null) clonedStyleItems.Add(item);
                    }

                    object selectedClonedObject = FindSelectedObject(styleItems.ToList(), clonedStyleItems.ToList(), selectedLayerListItem.ConcreteObject);
                    styleArguments.FillRequiredColumnNames();
                    styleArguments.SelectedConcreteObject = selectedClonedObject;
                    var styleBuilder = GisEditor.StyleManager.GetStyleBuiderUI(styleArguments);
                    if (styleBuilder.ShowDialog().GetValueOrDefault())
                        styleArguments.AppliedCallback(styleBuilder.StyleBuilderResult);
                }
            }
        }

        public static void ReplaceFromLibrary()
        {
            StyleLibraryWindow library = new StyleLibraryWindow();
            if (library.ShowDialog().GetValueOrDefault())
            {
                if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
                var styleItem = GisEditor.LayerListManager.SelectedLayerListItem;
                TileOverlay containingOverlay = null;
                var compositeStyle = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as CompositeStyle;
                if (compositeStyle != null)
                {
                    FeatureLayer currentFeatureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer;
                    if (currentFeatureLayer != null)
                    {
                        foreach (var zoomLevel in currentFeatureLayer.ZoomLevelSet.CustomZoomLevels)
                        {
                            var index = zoomLevel.CustomStyles.IndexOf(compositeStyle);
                            if (index >= 0)
                            {
                                zoomLevel.CustomStyles.RemoveAt(index);
                                zoomLevel.CustomStyles.Insert(index, library.Result.CompositeStyle);
                            }
                        }
                        containingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Parent.ConcreteObject as TileOverlay;
                    }
                }
                else if (styleItem.ConcreteObject is Styles.Style && styleItem.Parent.ConcreteObject is Styles.Style)
                {
                    var index = styleItem.Parent.Children.IndexOf(styleItem);
                    styleItem.Parent.Children.RemoveAt(index);
                    var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                    foreach (var item in compositeStyleItem.Children)
                    {
                        styleItem.Parent.Children.Insert(index, item);
                        index++;
                    }
                    ((StyleLayerListItem)styleItem.Parent).UpdateConcreteObject();
                    containingOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                }
                else
                {
                    styleItem.Children.Clear();
                    var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                    foreach (var item in compositeStyleItem.Children)
                    {
                        styleItem.Children.Add(item);
                    }
                    ((StyleLayerListItem)styleItem).UpdateConcreteObject();
                    containingOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                }
                if (containingOverlay != null)
                {
                    containingOverlay.Invalidate();
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(containingOverlay, RefreshArgsDescription.ReplaceFromLibraryDescription));
                }
            }
        }

        public static void InsertFromLibrary()
        {
            StyleLibraryWindow library = new StyleLibraryWindow();
            if (library.ShowDialog().GetValueOrDefault())
            {
                var styleItem = GisEditor.LayerListManager.SelectedLayerListItem as StyleLayerListItem;
                if (styleItem != null)
                {
                    TileOverlay containingOverlay = null;
                    var compositeStyle = styleItem.ConcreteObject as CompositeStyle;
                    var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(library.Result.CompositeStyle);
                    if (compositeStyle != null)
                    {
                        foreach (var item in compositeStyleItem.Children.Reverse())
                        {
                            styleItem.Children.Insert(0, item);
                        }
                        styleItem.UpdateConcreteObject();
                        containingOverlay = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Parent.ConcreteObject as TileOverlay;
                    }
                    else if (styleItem.ConcreteObject is Styles.Style && styleItem.Parent.ConcreteObject is Styles.Style)
                    {
                        var index = styleItem.Parent.Children.IndexOf(styleItem);
                        foreach (var item in compositeStyleItem.Children)
                        {
                            index++;
                            styleItem.Parent.Children.Insert(index, item);
                        }
                        ((StyleLayerListItem)styleItem.Parent).UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    else
                    {
                        foreach (var item in compositeStyleItem.Children.Reverse())
                        {
                            styleItem.Children.Insert(0, item);
                        }
                        styleItem.UpdateConcreteObject();
                        containingOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
                    }
                    if (containingOverlay != null)
                    {
                        containingOverlay.Invalidate();
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(containingOverlay, RefreshArgsDescription.InsertFromLibraryDescription));
                    }
                }
            }
        }

        public static void RefreshCache()
        {
            TileOverlay tileOverlay = LayerListHelper.FindMapElementInLayerList<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
            if (tileOverlay != null && tileOverlay.TileCache != null)
            {
                tileOverlay.Invalidate();
            }
        }

        private static LayerListItem FindViewModelInTree(LayerListItem layerListItem, Func<object, bool> match)
        {
            if (match(layerListItem.ConcreteObject))
            {
                return layerListItem;
            }
            else if (layerListItem.Parent != null)
            {
                return FindViewModelInTree(layerListItem.Parent, match);
            }
            return layerListItem;
        }

        private static void ClearEditOverlay(GisEditorEditInteractiveOverlay editOverlay)
        {
            editOverlay.EditTargetLayer.FeatureIdsToExclude.Clear();

            editOverlay.EditShapesLayer.InternalFeatures.Clear();
            editOverlay.AssociateControlPointsLayer.InternalFeatures.Clear();
            editOverlay.ReshapeControlPointsLayer.InternalFeatures.Clear();

            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.AssociateControlPointsLayer.BuildIndex();
            editOverlay.ReshapeControlPointsLayer.BuildIndex();

            GisEditor.ActiveMap.Refresh(editOverlay);
        }

        private static object FindSelectedObject(List<StyleLayerListItem> styleItems, List<StyleLayerListItem> clonedStyleItems, object selectedObject)
        {
            for (int i = 0; i < styleItems.Count; i++)
            {
                if (styleItems[i].ConcreteObject == selectedObject)
                {
                    return clonedStyleItems[i].ConcreteObject;
                }
                else
                {
                    var item = FindSelectedObject(styleItems[i].Children.OfType<StyleLayerListItem>().ToList(), clonedStyleItems[i].Children.OfType<StyleLayerListItem>().ToList(), selectedObject);
                    if (item != null) return item;
                }
            }
            return null;
        }


        private static void OverlayEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsChecked", StringComparison.Ordinal) && !stopRefresh)
            {
                LayerListItem currentInstance = sender as LayerListItem;
                if (currentInstance != null)
                {
                    var trackOverlay = currentInstance.ConcreteObject as MeasureTrackInteractiveOverlay;
                    if (trackOverlay != null)
                    {
                        bool needRefresh = false;
                        if (currentInstance.IsChecked)
                        {
                            if (trackOverlay.ShapeLayer.MapShapes.Count > 0)
                            {
                                needRefresh = true;
                                foreach (var item in trackOverlay.ShapeLayer.MapShapes)
                                {
                                    item.Value.ZoomLevels.ZoomLevel01.IsActive = true;
                                }

                                StopRefreshProcess(() =>
                                {
                                    currentInstance.Children.ForEach(tmpEntity => tmpEntity.IsChecked = true);
                                });
                            }
                        }
                        else
                        {
                            needRefresh = true;
                            foreach (var item in trackOverlay.ShapeLayer.MapShapes)
                            {
                                item.Value.ZoomLevels.ZoomLevel01.IsActive = false;
                            }

                            StopRefreshProcess(() =>
                            {
                                currentInstance.Children.ForEach(tmpEntity => tmpEntity.IsChecked = false);
                            });
                        }

                        if (needRefresh && !stopRefresh) trackOverlay.Refresh();
                    }
                }
            }
        }

        private static void SubEntity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as LayerListItem;
            if (e.PropertyName.Equals("IsChecked", StringComparison.Ordinal) && entity != null)
            {
                if ((entity.ConcreteObject as MapShape).ZoomLevels.ZoomLevel01.IsActive != entity.IsChecked)
                {
                    (entity.ConcreteObject as MapShape).ZoomLevels.ZoomLevel01.IsActive = entity.IsChecked;
                    if (GisEditor.ActiveMap != null)
                        GisEditor.ActiveMap.Refresh(entity.Parent.ConcreteObject as MeasureTrackInteractiveOverlay);
                }
            }

            if (e.PropertyName.Equals("Header", StringComparison.Ordinal))
            {
                (entity.ConcreteObject as MapShape).Feature.ColumnValues["DisplayName"] = entity.Name;
            }
        }

        private static IEnumerable<LayerListItem> CollectSubEntities(MeasureTrackInteractiveOverlay trackOverlay)
        {
            foreach (var item in trackOverlay.ShapeLayer.MapShapes)
            {
                var mapShape = item.Value;
                if (!mapShape.Feature.ColumnValues.ContainsKey("DisplayName"))
                    mapShape.Feature.ColumnValues["DisplayName"] = mapShape.Feature.Id;
                var subEntity = new LayerListItem
                {
                    Name = mapShape.Feature.ColumnValues["DisplayName"],
                    ConcreteObject = mapShape,
                    ExpandButtonVisibility = Visibility.Collapsed,
                    HighlightBackgroundBrush = new SolidColorBrush(Colors.White)
                };

                subEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetRenameMenuItem());
                subEntity.ContextMenuItems.Add(LayerListMenuItemHelper.GetRemoveFeatureMenuItem());
                StopRefreshProcess(() =>
                {
                    subEntity.IsChecked = mapShape.ZoomLevels.ZoomLevel01.IsActive;
                });

                var wkt = mapShape.Feature.GetWellKnownType();
                Styles.Style drawingStyle = null;
                if (wkt == WellKnownType.Line || wkt == WellKnownType.Multiline)
                    drawingStyle = mapShape.ZoomLevels.ZoomLevel01.CustomStyles.OfType<LineStyle>().FirstOrDefault();
                else
                    drawingStyle = mapShape.ZoomLevels.ZoomLevel01.CustomStyles.OfType<AreaStyle>().FirstOrDefault();
                subEntity.PreviewImage = new Image { Source = drawingStyle.GetPreviewImage(26, 26) };

                subEntity.PropertyChanged += new PropertyChangedEventHandler(SubEntity_PropertyChanged);

                yield return subEntity;
            }
        }

        private static void StopRefreshProcess(Action action)
        {
            stopRefresh = true;
            if (action != null) action();
            stopRefresh = false;
        }

    }
}
