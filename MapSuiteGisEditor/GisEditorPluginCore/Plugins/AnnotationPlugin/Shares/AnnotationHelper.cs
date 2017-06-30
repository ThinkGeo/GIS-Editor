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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class AnnotationHelper
    {
        private const int tolerance = 5;

        internal static AnnotationViewModel ViewModel { get; set; }

        //internal static void ActiveMap_MapClick(object sender, MapClickWpfMapEventArgs e)
        internal static void ActiveMap_MapClick(object sender, MapMouseClickInteractiveOverlayEventArgs e)
        {
            if (ViewModel.IsInModifyMode)
            {
                ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.Open();
                RectangleShape clickBuffer = GetClickBuffer(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY);

                #region save feature ids to exclude temporary.

                var tmpFeatureIdsToExclude = new Collection<string>();
                foreach (var id in ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude)
                {
                    tmpFeatureIdsToExclude.Add(id);
                }
                ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();

                #endregion save feature ids to exclude temporary.

                var foundFeature = ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.QueryTools
                    .GetFeaturesIntersecting(clickBuffer, ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.GetDistinctColumnNames()).FirstOrDefault(tmpFeature => !tmpFeatureIdsToExclude.Contains(tmpFeature.Id));

                if (foundFeature == default(Feature))
                {
                    PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas();
                    foundFeature = ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures
                        .Where(tmpFeature => tmpFeature.ColumnValues.ContainsKey(AnnotationTrackInteractiveOverlay.AnnotationTextColumnName)
                            && !String.IsNullOrEmpty(tmpFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName]))
                        .FirstOrDefault(textFeature =>
                        {
                            if (tmpFeatureIdsToExclude.Contains(textFeature.Id)) return false;

                            TextStyle textStyle = ViewModel.CurrentAnnotationOverlay
                                .GetSpecificTextStyle(textFeature.ColumnValues[AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName]);

                            DrawingRectangleF textArea = geoCanvas.MeasureText(textFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName]
                                , textStyle.Font);

                            PointShape textScreenPoint = GisEditor.ActiveMap.ToScreenCoordinate((PointShape)textFeature.GetShape());

                            double left = textScreenPoint.X;
                            double top = textScreenPoint.Y;
                            double right = textScreenPoint.X + textArea.Width;
                            double bottom = textScreenPoint.Y + textArea.Height;

                            string placementString = textStyle.PointPlacement.ToString();
                            if (placementString.Contains("Left"))
                            {
                                left = textScreenPoint.X - textArea.Width;
                            }

                            if (placementString.Contains("Upper"))
                            {
                                top = textScreenPoint.Y - textArea.Height;
                            }

                            PointShape upperLeft = GisEditor.ActiveMap.ToWorldCoordinate(new PointShape(left, top));
                            PointShape lowerRight = GisEditor.ActiveMap.ToWorldCoordinate(new PointShape(right, bottom));

                            RectangleShape textWorldArea = new RectangleShape(upperLeft, lowerRight);
                            return textWorldArea.Intersects(new PointShape(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY));
                        });
                }

                #region restore feature ids to exclude

                foreach (var id in tmpFeatureIdsToExclude)
                {
                    ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Add(id);
                }

                #endregion restore feature ids to exclude

                ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.Close();

                if (foundFeature != default(Feature))
                {
                    var isShiftDown = Keyboard.Modifiers == ModifierKeys.Shift;
                    if (!isShiftDown)
                    {
                        CommitEdit(false);
                    }

                    bool isEditing = true;
                    if (!ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Contains(foundFeature.Id))
                    {
                        ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Add(foundFeature.Id);
                    }
                    else
                    {
                        isEditing = false;
                        if (isShiftDown)
                        {
                            ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Remove(foundFeature.Id);
                            if (ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Contains(foundFeature.Id))
                            {
                                ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Remove(foundFeature.Id);
                            }
                        }
                    }

                    if (isEditing)
                    {
                        SetAnnotationToEditMode(foundFeature);
                    }

                    ViewModel.CurrentEditOverlay.CalculateAllControlPoints();
                    ViewModel.CurrentAnnotationOverlay.Refresh();
                    ViewModel.CurrentEditOverlay.Refresh();
                    ViewModel.SyncUIState();
                    ViewModel.TakeSnapshot();
                }
                else
                {
                    if (ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Count > 0
                        || (MarkerHelper.CurrentMarkerOverlay != null && MarkerHelper.CurrentMarkerOverlay.Markers.Count > 0))
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => CommitEdit()));
                    }
                }
            }
        }

        internal static void SetAnnotationToEditMode(Feature foundFeature)
        {
            var shape = foundFeature.GetShape();

            //if it's area, line or point
            if (!(shape is PointBaseShape)
                || (string.IsNullOrEmpty(foundFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName])))
            {
                ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Add(foundFeature.Id, foundFeature);
            }
            //if it's text annotation
            else
            {
                string styleValue = String.Empty;
                if (foundFeature.ColumnValues.ContainsKey(AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName))
                {
                    styleValue = foundFeature.ColumnValues[AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName];
                }

                MarkerHelper.AddMarker((PointShape)shape,
                                       foundFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName],
                                       foundFeature.Id, styleValue);
            }
        }

        internal static void CommitEdit(bool takeSnapshot = true)
        {
            if (ViewModel.IsInModifyMode)
            {
                CommitNonTextAnnotationEdit();
                CommitTextAnnotationEdit();

                ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();
                ViewModel.CurrentAnnotationOverlay.Refresh();

                ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Clear();
                ViewModel.CurrentEditOverlay.CalculateAllControlPoints();
                ViewModel.CurrentEditOverlay.Refresh();

                ViewModel.SyncStylePreview();
                ViewModel.SyncUIState();

                if (takeSnapshot)
                {
                    ViewModel.TakeSnapshot();
                }
            }
        }

        internal static void EditAnnotationStyle<T>(AnnotaionStyleType annotationStyleType) where T : Style
        {
            AreaStyle areaStyle = (AreaStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<AreaStyle>(AnnotaionStyleType.LayerStyle, false)).CloneDeep();
            areaStyle.Name = "Annotation Area Style";
            LineStyle lineStyle = (LineStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<LineStyle>(AnnotaionStyleType.LayerStyle, false)).CloneDeep();
            lineStyle.Name = "Annotation Line Style";
            PointStyle pointStyle = (PointStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<PointStyle>(AnnotaionStyleType.LayerStyle, false)).CloneDeep();
            pointStyle.Name = "Annotation Point Style";
            TextStyle textStyle = (TextStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<TextStyle>(AnnotaionStyleType.LayerStyle, false)).CloneDeep();
            textStyle.Name = "Annotation Text Style";
            PointStyle fileLinkPointStyle = (PointStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<PointStyle>(AnnotaionStyleType.FileLinkStyle, false)).CloneDeep();
            fileLinkPointStyle.Name = "File Link Point Style";
            TextStyle fileLinkTextStyle = (TextStyle)(ViewModel.CurrentAnnotationOverlay.GetLatestStyle<TextStyle>(AnnotaionStyleType.FileLinkStyle, false)).CloneDeep();
            fileLinkTextStyle.Name = "File Link Text Style";

            CompositeStyle compositeStyle = new CompositeStyle();
            compositeStyle.Styles.Add(areaStyle);
            compositeStyle.Styles.Add(lineStyle);
            compositeStyle.Styles.Add(pointStyle);
            compositeStyle.Styles.Add(textStyle);
            compositeStyle.Styles.Add(fileLinkPointStyle);
            compositeStyle.Styles.Add(fileLinkTextStyle);

            StyleBuilderArguments styleArguments = new StyleBuilderArguments();
            styleArguments.IsSubStyleReadonly = true;
            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
            styleArguments.AvailableStyleCategories = StyleCategories.Area | StyleCategories.Line | StyleCategories.Point;
            styleArguments.StyleToEdit = compositeStyle;
            styleArguments.FeatureLayer = ViewModel.CurrentAnnotationOverlay.TrackShapeLayer;
            styleArguments.AppliedCallback = (result) =>
            {
                var AnnotationStyle = result.CompositeStyle;
                ViewModel.AnnotationPreview = AnnotationStyle.GetPreviewImage(32, 32);
            };

            var resultStyle = GisEditor.StyleManager.EditStyle(styleArguments);
            if (resultStyle != null && resultStyle.CompositeStyle != null)
            {
                var annotationStyle = resultStyle.CompositeStyle;
                foreach (var item in annotationStyle.Styles)
                {
                    if (item.Name == "Annotation Area Style"
                        || item.Name == "Annotation Line Style"
                        || item.Name == "Annotation Point Style"
                        || item.Name == "Annotation Text Style")
                    {
                        ViewModel.CurrentAnnotationOverlay.ChangeAppliedStyle(item, AnnotaionStyleType.LayerStyle);
                    }
                    else if (item.Name == "File Link Point Style" || item.Name == "File Link Text Style")
                    {
                        ViewModel.CurrentAnnotationOverlay.ChangeAppliedStyle(item, AnnotaionStyleType.FileLinkStyle);
                    }
                }
                ViewModel.AnnotationPreview = annotationStyle.GetPreviewImage(32, 32);
            }
        }

        private static bool CheckFeatureIsMatchedStyleType<T>(Feature tmpFeature) where T : Style
        {
            if (CheckTypeIsEqualOrSubclass<T, PointStyle>())
            {
                return tmpFeature.GetWellKnownType() == WellKnownType.Point;
            }
            if (CheckTypeIsEqualOrSubclass<T, LineStyle>())
            {
                return tmpFeature.GetWellKnownType() == WellKnownType.Line;
            }
            if (CheckTypeIsEqualOrSubclass<T, AreaStyle>())
            {
                return tmpFeature.GetWellKnownType() == WellKnownType.Polygon;
            }
            return false;
        }

        private static void CommitTextAnnotationEdit()
        {
            if (MarkerHelper.CurrentMarkerOverlay != null && MarkerHelper.CurrentMarkerOverlay.Markers.Count != 0)
            {
                var featuresToBeReplaced = GetTextAnnotations();

                var newFeatures = featuresToBeReplaced.Select(f =>
                {
                    var marker = MarkerHelper.CurrentMarkerOverlay.Markers.First(m => m.Tag.Equals(f.Id));

                    Feature newFeature = new Feature(marker.Position.X, marker.Position.Y, f.Id);

                    foreach (var pair in f.ColumnValues)
                    {
                        newFeature.ColumnValues.Add(pair.Key, pair.Value);
                    }

                    newFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName] = ((TextBox)marker.Content).Text;

                    return newFeature;
                });

                foreach (var feature in featuresToBeReplaced)
                {
                    ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Remove(feature);
                }

                foreach (var feature in newFeatures)
                {
                    ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Add(feature.Id, feature);
                }

                MarkerHelper.CurrentMarkerOverlay.Markers.Clear();
                MarkerHelper.CurrentMarkerOverlay.Refresh();
            }
        }

        private static Feature[] GetTextAnnotations()
        {
            var featuresToBeReplaced
                    = ViewModel.CurrentAnnotationOverlay
                        .TrackShapeLayer.InternalFeatures
                        .Where(f => ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Contains(f.Id)
                                    && f.GetShape() is PointBaseShape
                                    && f.ColumnValues.ContainsKey(AnnotationTrackInteractiveOverlay.AnnotationTextColumnName)
                                    && !string.IsNullOrEmpty(f.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName]))
                                    .ToArray();
            return featuresToBeReplaced;
        }

        private static void CommitNonTextAnnotationEdit()
        {
            if (ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures.Count > 0)
            {
                foreach (var feature in ViewModel.CurrentEditOverlay.EditShapesLayer.InternalFeatures)
                {
                    var featureToBeReplaced = ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.FirstOrDefault(f => f.Id == feature.Id);
                    if (featureToBeReplaced != null)
                    {
                        ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Remove(featureToBeReplaced);
                        ViewModel.CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Add(feature.Id, feature);
                    }
                }
            }
        }

        private static bool CheckTypeIsEqualOrSubclass<T, K>()
        {
            return typeof(T) == typeof(K) || typeof(T).IsSubclassOf(typeof(K));
        }

        private static RectangleShape GetClickBuffer(double x, double y)
        {
            double resolution = GisEditor.ActiveMap.CurrentResolution;
            double worldTolerance = tolerance * resolution;
            double left = x - worldTolerance;
            double right = x + worldTolerance;
            double top = y + worldTolerance;
            double bottom = y - worldTolerance;
            return new RectangleShape(left, top, right, bottom);
        }
    }
}