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
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal static class LayerListHelper
    {
        private static readonly string common = " Style";
        private static readonly string area = " Area Style";
        private static readonly string line = " Line Style";
        private static readonly string point = " Point Style";
        private static readonly string text = " Style";
        private static readonly string newValue = "...";

        public static StyleCategories GetStyleCategoriesByFeatureLayer(FeatureLayer featureLayer)
        {
            StyleCategories resultStyleCategories = StyleCategories.None;
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null)
            {
                var type = featureLayerPlugin.GetFeatureSimpleShapeType(featureLayer);
                switch (type)
                {
                    case SimpleShapeType.Point:
                        resultStyleCategories = StyleCategories.Point | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Line:
                        resultStyleCategories = StyleCategories.Line | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Area:
                        resultStyleCategories = StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                        break;

                    case SimpleShapeType.Unknown:
                    default:
                        resultStyleCategories = StyleCategories.Point | StyleCategories.Line | StyleCategories.Area | StyleCategories.Label | StyleCategories.Composite;
                        break;
                }
            }

            return resultStyleCategories;
        }

        public static string GetShortName(this StylePlugin stylePlugin)
        {
            if (stylePlugin.Name.Contains(area)
                || stylePlugin.Name.Contains(line)
                || stylePlugin.Name.Contains(point)
                || stylePlugin.Name.Contains(text))
            {
                return stylePlugin.Name.Replace(area, newValue).Replace(line, newValue).Replace(point, newValue).Replace(text, newValue);
            }
            else
            {
                return stylePlugin.Name.Replace(common, newValue);
            }
        }

        public static void AddStyle()
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
                styleArguments.ToZoomLevelIndex = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels().Where(z => z.GetType() == typeof(ZoomLevel)).Count();
                styleArguments.AppliedCallback = new Action<StyleBuilderResult>((styleResult) =>
                {
                    if (!styleResult.Canceled)
                    {
                        foreach (var zoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
                        {
                            zoomLevel.CustomStyles.Remove(componentStyle);
                        }

                        for (int i = styleResult.FromZoomLevelIndex - 1; i < styleResult.ToZoomLevelIndex; i++)
                        {
                            featureLayer.ZoomLevelSet.CustomZoomLevels[i].CustomStyles.Add(styleResult.CompositeStyle);
                        }

                        LayerListHelper.InvalidateTileOverlay();
                        GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(featureLayer, RefreshArgsDescriptions.AddStyleCommandDescription));
                        componentStyle = styleResult.CompositeStyle as CompositeStyle;
                    }
                });

                styleArguments.FillRequiredColumnNames();
                var styleResults = GisEditor.StyleManager.EditStyle(styleArguments);
                styleArguments.AppliedCallback(styleResults);
            }
        }

        public static void InvalidateTileOverlay()
        {
            TileOverlay tileOverlay = LayerListHelper.FindMapElementInTree<TileOverlay>(GisEditor.LayerListManager.SelectedLayerListItem);
            if (tileOverlay != null)
            {
                tileOverlay.Invalidate();
            }
        }

        public static T FindMapElementInTree<T>(LayerListItem layerListItem) where T : class
        {
            var resultViewModel = FindViewModelInTree<T>(layerListItem);
            if (resultViewModel != null) return resultViewModel.ConcreteObject as T;
            else return null;
        }

        public static LayerListItem FindViewModelInTree<T>(LayerListItem layerListItem) where T : class
        {
            return FindViewModelInTree(layerListItem, new Func<object, bool>((actualMapElement) => actualMapElement is T));
        }

        public static void SafeProcess(this Layer layer, Action processAction)
        {
            lock (layer)
            {
                bool isClosed = false;
                if (!layer.IsOpen)
                {
                    layer.Open();
                    isClosed = true;
                }

                if (processAction != null) processAction();

                if (isClosed)
                {
                    layer.Close();
                }
            }
        }

        public static void InvokeRefreshPlugins(this UIPluginManager uiPluginManager, RefreshArgs refreshArgs = null)
        {
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    uiPluginManager.RefreshPlugins(refreshArgs);
                });
            }
        }

        public static List<LayerListItem> CollectCompositeStyleLayerListItem(FeatureLayer featureLayer)
        {
            var resultZoomLevels = GetZoomLevelsAccordingToSacle(featureLayer);

            return resultZoomLevels
                .OrderByDescending(zoomLevel => zoomLevel.Scale)
                .SelectMany(zoomLevel => GetStyleLayerListItem(zoomLevel)).ToList();
        }

        public static Collection<ZoomLevel> GetZoomLevelsAccordingToSacle(FeatureLayer featureLayer)
        {
            List<ZoomLevel> allLevels = featureLayer.ZoomLevelSet.CustomZoomLevels.Where(z => z.GetType() == typeof(ZoomLevel)).ToList();

            var notEmptyLevels = allLevels.Where(zoomLevel => zoomLevel.CustomStyles.Count != 0).OrderBy(zoomLevel => zoomLevel.Scale);

            var group = new Dictionary<Styles.Style, Collection<ZoomLevel>>();

            foreach (var zoomLevel in notEmptyLevels)
            {
                foreach (var item in zoomLevel.CustomStyles)
                {
                    if (group.ContainsKey(item))
                    {
                        if (!group[item].Contains(zoomLevel)) group[item].Add(zoomLevel);
                    }
                    else group[item] = new Collection<ZoomLevel> { zoomLevel };
                }
            }

            var resultZoomLevels = new Collection<ZoomLevel>();
            foreach (var item in group)
            {
                double fromScale = item.Value.Last().Scale;
                int to = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(item.Value.First().Scale, false) + 1;

                var targetZoomLevel = resultZoomLevels.FirstOrDefault(z => z.Scale == fromScale && z.ApplyUntilZoomLevel == (ApplyUntilZoomLevel)to);
                if (targetZoomLevel == null)
                {
                    ZoomLevel zoomLevel = new ZoomLevel(fromScale);
                    zoomLevel.CustomStyles.Add(item.Key);
                    zoomLevel.ApplyUntilZoomLevel = (ApplyUntilZoomLevel)to;
                    resultZoomLevels.Add(zoomLevel);
                }
                else targetZoomLevel.CustomStyles.Add(item.Key);
            }
            return resultZoomLevels;
        }

        public static bool CheckIsValid(this Style style)
        {
            AreaStyle areaStyle = style as AreaStyle;
            LineStyle lineStyle = style as LineStyle;
            PointStyle pointStyle = style as PointStyle;
            TextStyle textStyle = style as TextStyle;
            DotDensityStyle dotDensityStyle = style as DotDensityStyle;
            ClassBreakStyle classBreakStyle = style as ClassBreakStyle;
            RegexStyle regexStyle = style as RegexStyle;
            FilterStyle filterStyle = style as FilterStyle;
            CompositeStyle componentStyle = style as CompositeStyle;

            bool isStyleValid = style.IsActive && !string.IsNullOrEmpty(style.Name);

            if (areaStyle != null)
            {
                isStyleValid &= (!areaStyle.FillSolidBrush.Color.IsTransparent
                    || !areaStyle.OutlinePen.Color.IsTransparent
                    || areaStyle.Advanced.FillCustomBrush != null);
            }
            else if (lineStyle != null)
            {
                isStyleValid &= (!lineStyle.CenterPen.Color.IsTransparent
                    || !lineStyle.OuterPen.Color.IsTransparent
                    || !lineStyle.InnerPen.Color.IsTransparent);
            }
            else if (pointStyle != null)
            {
                switch (pointStyle.PointType)
                {
                    case PointType.Symbol:
                        isStyleValid &= (!pointStyle.SymbolPen.Color.IsTransparent
                            || pointStyle.Image != null
                            || !pointStyle.SymbolSolidBrush.Color.IsTransparent
                            || pointStyle.Advanced.CustomBrush != null);
                        break;

                    case PointType.Bitmap:
                        isStyleValid &= pointStyle.Image != null;
                        break;

                    case PointType.Character:
                        isStyleValid &= pointStyle.CharacterFont != null
                            && (!pointStyle.CharacterSolidBrush.Color.IsTransparent
                            || pointStyle.Advanced.CustomBrush != null);
                        break;
                    default:
                        break;
                }
            }
            else if (textStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(textStyle.TextColumnName)
                    && (!textStyle.HaloPen.Color.IsTransparent
                    || !textStyle.TextSolidBrush.Color.IsTransparent
                    || textStyle.Advanced.TextCustomBrush != null);
            }
            else if (dotDensityStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(dotDensityStyle.ColumnName)
                    && (dotDensityStyle.CustomPointStyle != null
                    && CheckIsValid(dotDensityStyle.CustomPointStyle)
                    && dotDensityStyle.PointToValueRatio != 0);
            }
            else if (classBreakStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(classBreakStyle.ColumnName)
                    && classBreakStyle.ClassBreaks.Count != 0;
            }
            else if (regexStyle != null)
            {
                isStyleValid &= !string.IsNullOrEmpty(regexStyle.ColumnName)
                    && regexStyle.RegexItems.Count != 0;
            }
            else if (filterStyle != null)
            {
                isStyleValid &= filterStyle.Conditions.Count > 0;
            }
            else if (componentStyle != null)
            {
                isStyleValid = true;
            }
            return isStyleValid;
        }

        public static void EditStyle(LayerListItem selectedLayerListItem)
        {
            var componentStyleItem = LayerListHelper.FindViewModelInTree<CompositeStyle>(selectedLayerListItem) as StyleLayerListItem;
            if (componentStyleItem != null)
            {
                var componentStyle = componentStyleItem.ConcreteObject as CompositeStyle;
                var styleArguments = new StyleBuilderArguments();
                styleArguments.FeatureLayer = componentStyleItem.Parent.ConcreteObject as FeatureLayer;
                var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(styleArguments.FeatureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (featureLayerPlugin != null)
                {
                    styleArguments.AvailableStyleCategories = GetStyleCategoriesByFeatureLayer(styleArguments.FeatureLayer);
                    int from = 1;
                    int to = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Where(z => z.GetType() == typeof(ZoomLevel)).Count();
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
                            var count = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels().Where(z => z.GetType() == typeof(ZoomLevel)).Count();
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
                            if (styleArguments.FeatureLayer.IsVisible)
                            {
                                foreach (var overlay in GisEditor.ActiveMap.GetOverlaysContaining(styleArguments.FeatureLayer))
                                {
                                    overlay.Invalidate();
                                }
                            }
                            GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(styleResults, RefreshArgsDescriptions.EditStyleDescription));
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
                    //styleBuilder.StyleBuilderArguments = styleArguments;
                    //var styleResult = GisEditor.StyleManager.EditStyle(styleArguments, selectedClonedObject);
                    if (styleBuilder.ShowDialog().GetValueOrDefault())
                        styleArguments.AppliedCallback(styleBuilder.StyleBuilderResult);
                }
            }
        }

        private static IEnumerable<LayerListItem> GetStyleLayerListItem(ZoomLevel zoomLevel)
        {
            var result = new Collection<LayerListItem>();
            for (int i = 0; i < zoomLevel.CustomStyles.Count; i++)
            {
                Styles.Style style = zoomLevel.CustomStyles[i];
                if (!style.CheckIsValid())
                {
                    zoomLevel.CustomStyles.Remove(style);
                    i = i - 1;
                }
            }
            var from = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(zoomLevel.Scale, false) + 1;
            var to = (int)zoomLevel.ApplyUntilZoomLevel;
            var zoomLevelText = string.Format("  ({0} to {1})", from, to);
            var currentZoomLevelIndex = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(GisEditor.ActiveMap.CurrentScale, false) + 1;
            foreach (var style in zoomLevel.CustomStyles.Reverse())
            {
                var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
                if (styleItem != null)
                {
                    CreateComponentStyleEntity(styleItem);
                    styleItem.ZoomLevelRange = zoomLevelText;
                    if (from <= currentZoomLevelIndex && to >= currentZoomLevelIndex)
                    {
                        styleItem.FontWeight = System.Windows.FontWeights.Bold;
                    }
                    result.Add(styleItem);
                }
            }
            return result;
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

        private static void CreateComponentStyleEntity(StyleLayerListItem styleItem)
        {
            var bitmapSource = styleItem.GetPreviewSource(23, 23);
            styleItem.PreviewImage = new Image { Source = bitmapSource };
            styleItem.IsExpanded = false;

            styleItem.DoubleClicked = () =>
            {
                GisEditor.LayerListManager.SelectedLayerListItem = styleItem;
                GisEditor.ActiveMap.ActiveLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as Layer;
                EditStyle(styleItem);
            };

            AddMenuItems(styleItem);
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetSaveStyleMenuItem());

            foreach (var item in styleItem.Children.OfType<StyleLayerListItem>())
            {
                AddSubEntitiesToComponentStyleEntity(item);
            }
        }

        private static void AddSubEntitiesToComponentStyleEntity(StyleLayerListItem item)
        {
            //var subEntity = new LayerListItem();
            //subEntity.Parent = componentStyleEntity;
            //subEntity.ConcreteObject = item;
            //subEntity.Name = item.Name;
            //item.CheckBoxVisibility = System.Windows.Visibility.Collapsed;
            item.PreviewImage = new Image { Source = item.GetPreviewSource(23, 23) };
            AddMenuItems(item);
            item.DoubleClicked = () =>
            {
                GisEditor.LayerListManager.SelectedLayerListItem = item;
                EditStyle(item);
            };
            if (item.Children.Count > 0)
            {
                AddComplicatedStyleInnerStyles(item);
            }
            //componentStyleEntity.Children.Add(subEntity);
        }

        private static void AddComplicatedStyleInnerStyles(StyleLayerListItem item)
        {
            bool isValueStyle = item.ConcreteObject is ValueStyle;
            foreach (var innerStyleItem in item.Children.OfType<StyleLayerListItem>())
            {
                innerStyleItem.DoubleClicked = () =>
                {
                    GisEditor.LayerListManager.SelectedLayerListItem = innerStyleItem;
                    EditStyle(innerStyleItem);
                };
                innerStyleItem.PreviewImage = new Image { Source = innerStyleItem.GetPreviewSource(23, 23) };
                if (isValueStyle)
                {
                    innerStyleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetZoomToExtentMenuItem());
                }

                AddMenuItems(innerStyleItem, false);
                foreach (var subItem in innerStyleItem.Children.OfType<StyleLayerListItem>())
                {
                    AddSubEntitiesToComponentStyleEntity(subItem);
                }
            }
        }

        private static void AddMenuItems(StyleLayerListItem styleItem, bool isMovementEnabled = true)
        {
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Up, isMovementEnabled));
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.Down, isMovementEnabled));
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToTop, isMovementEnabled));
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetMovementMenuItem(MovementAction.ToBottom, isMovementEnabled));
            styleItem.ContextMenuItems.Add(new MenuItem() { Header = "--" });
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetInsertFromLibraryMenuItem());
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetReplaceFromLibraryMenuItem());
            styleItem.ContextMenuItems.Add(new MenuItem() { Header = "--" });
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetDuplicateMenuItem());
            if (styleItem.CanRename)
            {
                styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRenameMenuItem());
            }
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetEditStyleMenuItem());
            styleItem.ContextMenuItems.Add(LayerListMenuItemHelper.GetRemoveStyleMenuItem());
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
    }
}
