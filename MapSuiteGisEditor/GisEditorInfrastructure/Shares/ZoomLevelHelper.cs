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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal static class ZoomLevelHelper
    {
        public static CompositeStyle GetCurrentDrawingStyle(ZoomLevel zoomLevel)
        {
            var allNonComponentStyle = zoomLevel.CustomStyles.SelectMany(tmpStyle =>
            {
                var componentStyle = tmpStyle as CompositeStyle;
                if (componentStyle != null)
                    return componentStyle.Styles;
                else return new ObservableCollection<Style>() { tmpStyle };
            });
            var currentDrawingStyle = allNonComponentStyle.Where(tmpStyle =>
!(tmpStyle is IconTextStyle || tmpStyle is TextFilterStyle)).LastOrDefault();
            var currentDrawingTextStyle = allNonComponentStyle.Where(tmpStyle => tmpStyle is IconTextStyle || tmpStyle is TextFilterStyle).LastOrDefault();
            var resultComponentStyle = new CompositeStyle();
            if (currentDrawingStyle != null)
                resultComponentStyle.Styles.Add(currentDrawingStyle);
            if (currentDrawingTextStyle != null)
                resultComponentStyle.Styles.Add(currentDrawingTextStyle);
            return resultComponentStyle;
        }

        public static bool CheckIsVisibleZoomLevel(LayerListItem zoomLevelEntity)
        {
            ZoomLevel zoomLevel = zoomLevelEntity.ConcreteObject as ZoomLevel;
            if (zoomLevel != null && GisEditor.ActiveMap != null)
            {
                int applyToZoomLevelIndex = (int)zoomLevel.ApplyUntilZoomLevel;
                double upperScale = zoomLevel.Scale;
                double lowerScale = GisEditor.ActiveMap.ZoomLevelSet.GetZoomLevels()[applyToZoomLevelIndex - 1].Scale;
                return GisEditor.ActiveMap.CurrentScale <= upperScale && GisEditor.ActiveMap.CurrentScale >= lowerScale;
            }
            else return false;
        }

        public static void ResetZoomLevelRange(ZoomLevel selectedZoomLevel, FeatureLayer featureLayer, int from, int to)
        {
            for (int i = 0; i < featureLayer.ZoomLevelSet.CustomZoomLevels.Count; i++)
            {
                var zoomLevel = featureLayer.ZoomLevelSet.CustomZoomLevels[i];
                if (i >= from - 1 && i <= to - 1)
                {
                    foreach (var style in selectedZoomLevel.CustomStyles)
                    {
                        if (!zoomLevel.CustomStyles.Contains(style))
                            zoomLevel.CustomStyles.Add(style);
                    }
                }
                else
                {
                    foreach (var style in selectedZoomLevel.CustomStyles)
                    {
                        if (zoomLevel.CustomStyles.Contains(style))
                            zoomLevel.CustomStyles.Remove(style);
                    }
                }
            }

            LayerListHelper.InvalidateTileOverlay();
            GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(selectedZoomLevel, RefreshArgsDescriptions.ResetZoomLevelRangeDescription));
        }

        public static void ModifySelectedStyles(Style selectedStyle, IEnumerable<Style> newStyles, int from, int to)
        {
            foreach (var style in newStyles)
            {
                ModifySelectedStyle(selectedStyle, style, from, to);
            }
        }

        public static void ModifySelectedStyle(Style selectedStyle, Style newStyle, int from, int to)
        {

            if (GisEditor.LayerListManager.SelectedLayerListItem != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject is Style)
            {
                if (selectedStyle != GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject)
                {
                    //The following check is for issue-7208 and issue-7213.
                    //When we double click on a RegexItem, we actually mean to edit the RegextItem's parent - the RegexStyle.
                    //But the SelectedEntity matches up with the RegextItem, not the RegextStyle, so we need to have this check.
                    //Anyways, this is quite complicated, jsut reconsider before you modify the following code.
                    if (!IsSubStyleSelected(selectedStyle) && selectedStyle != GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject)
                    {
                        selectedStyle = (Style)GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject;
                    }
                }

                FeatureLayer currentLayer = LayerListHelper.FindMapElementInTree<FeatureLayer>(GisEditor.LayerListManager.SelectedLayerListItem);
                ZoomLevel currentZoomLevel = LayerListHelper.FindMapElementInTree<ZoomLevel>(GisEditor.LayerListManager.SelectedLayerListItem);
                int originalFrom = GisEditor.ActiveMap.GetSnappedZoomLevelIndex(currentZoomLevel.Scale, false) + 1;
                int originalTo = (int)currentZoomLevel.ApplyUntilZoomLevel;

                Style nextSelectedStyle = newStyle.CloneDeep();
                ReplaceStyle(currentLayer, selectedStyle, newStyle);

                if (originalFrom != from || originalTo != to)
                {
                    for (int i = 0; i < currentLayer.ZoomLevelSet.CustomZoomLevels.Count; i++)
                    {
                        var zoomLevel = currentLayer.ZoomLevelSet.CustomZoomLevels[i];
                        if (i >= from - 1 && i <= to - 1)
                        {
                            if (!zoomLevel.CustomStyles.Contains(newStyle))
                                zoomLevel.CustomStyles.Add(newStyle);
                        }
                        else
                        {
                            if (zoomLevel.CustomStyles.Contains(newStyle))
                                zoomLevel.CustomStyles.Remove(newStyle);
                        }
                    }
                }
                GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject = newStyle;
                LayerListHelper.InvalidateTileOverlay();
                GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescriptions.ModifySelectedStyleDescription));
            }
        }

        public static void AddStylesToZoomLevels(IEnumerable<Style> styles, int from, int to, Collection<ZoomLevel> zoomLevels)
        {
            foreach (var style in styles)
            {
                AddStyleToZoomLevels(style, from, to, zoomLevels);
            }
        }

        public static void AddStyleToZoomLevels(Style style, int from, int to, Collection<ZoomLevel> zoomLevels)
        {
            for (int i = from - 1; i < to; i++)
            {
                zoomLevels[i].CustomStyles.Add(style);
            }
        }

        public static void ReplaceStyle(FeatureLayer featureLayer, Style oldStyle, Style newStyle)
        {
            foreach (var tmpZoomLevel in featureLayer.ZoomLevelSet.CustomZoomLevels)
            {
                var index = tmpZoomLevel.CustomStyles.IndexOf(oldStyle);
                if (index >= 0)
                {
                    tmpZoomLevel.CustomStyles.Insert(index, newStyle);
                    tmpZoomLevel.CustomStyles.Remove(oldStyle);
                }
            }
        }

        public static void ApplyStyles(IEnumerable<Style> newStyles, FeatureLayer currentFeatureLayer, int fromZoomLevel, int toZoomLevel, bool needsRefresh = true)
        {
            foreach (var newStyle in newStyles)
            {
                ApplyStyle(newStyle, currentFeatureLayer, fromZoomLevel, toZoomLevel, needsRefresh);
            }
        }

        public static void ApplyStyle(Style newStyle, FeatureLayer currentFeatureLayer, int fromZoomLevel, int toZoomLevel, bool needsRefresh = true)
        {
            currentFeatureLayer.ZoomLevelSet
                               .CustomZoomLevels
                               .Where(tmpLevel => tmpLevel.CustomStyles.Contains(newStyle))
                               .ForEach(tmpLevel => tmpLevel.CustomStyles.Remove(newStyle));

            if (GisEditor.LayerListManager.SelectedLayerListItem != null && GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject != null)
            {
                FeatureLayer featureLayer = null;
                ZoomLevel zoomLevel = null;
                Style currentStyle = null;
                if ((featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer) != null)
                {
                    AddStyleToZoomLevels(newStyle, fromZoomLevel, toZoomLevel, featureLayer.ZoomLevelSet.CustomZoomLevels);
                }
                else if ((zoomLevel = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as ZoomLevel) != null)
                {
                    var customZoomLevels = (GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject as FeatureLayer).ZoomLevelSet.CustomZoomLevels;
                    AddStyleToZoomLevels(newStyle, fromZoomLevel, toZoomLevel, customZoomLevels);
                }
                else if ((currentStyle = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as Style) != null)
                {
                    featureLayer = GisEditor.LayerListManager.SelectedLayerListItem.Parent.Parent.ConcreteObject as FeatureLayer;
                    ReplaceStyle(featureLayer, currentStyle, newStyle);
                }

                if (needsRefresh)
                {
                    LayerListHelper.InvalidateTileOverlay();
                    GisEditor.UIManager.InvokeRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescriptions.ApplyStyleDescription));
                }
            }
        }

        //sub style means the styles that are contained by other styles, for example: an area style that is contained by a regex style.
        private static bool IsSubStyleSelected(Style selectedStyle)
        {
            bool isSubStyleOfRegexSelected = selectedStyle is RegexStyle && GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject == selectedStyle;
            bool isSubStyleOfClassBreakStyleSelected = selectedStyle is ClassBreakStyle && GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject == selectedStyle;
            bool isSubStyleOfValueStyleSelected = selectedStyle is ValueStyle && GisEditor.LayerListManager.SelectedLayerListItem.Parent.ConcreteObject == selectedStyle;

            return isSubStyleOfRegexSelected || isSubStyleOfClassBreakStyleSelected || isSubStyleOfValueStyleSelected;
        }
    }
}