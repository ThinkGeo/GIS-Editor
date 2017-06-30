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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public partial class GisEditorEditInteractiveOverlay : InteractiveOverlay
    {
        private const int clickPointTolerance = 14;

        public event EventHandler<FeatureClickedGisEditorEditInteractiveOverlayEventArgs> FeatureClicked;

        public event EventHandler<FeatureTrackEndedGisEditorEditInteractiveOverlayEventArgs> FeatureTrackEnded;

        [Obfuscation(Exclude = true)]
        private InMemoryFeatureSource cachedFeatureSourceInCurrentExtent;

        private enum EditMode
        {
            None = 0,
            Drag = 1,
            Reshape = 2,
            ResizeOrRotate = 3,
            MultiSelect = 4
        }

        private bool CanProcessMouseEvents
        {
            get { return IsEnabled && TrackResultProcessMode == TrackResultProcessMode.None; }
        }

        private class EditSnapshot
        {
            private GeoCollection<Feature> internalFeatures;
            private GeoCollection<Feature> reshapeFeatures;
            private GeoCollection<Feature> associateFeatures;
            private GeoCollection<Feature> unselectedFeatures;

            public EditSnapshot()
            {
                internalFeatures = new GeoCollection<Feature>();
                unselectedFeatures = new GeoCollection<Feature>();
                reshapeFeatures = new GeoCollection<Feature>();
                associateFeatures = new GeoCollection<Feature>();
            }

            public Feature EditingFeature { get; set; }

            public Feature ControlFeature { get; set; }

            public PointShape OriginalPosition { get; set; }

            public EditMode CurrentEditMode { get; set; }

            public GeoCollection<Feature> InternalFeatures { get { return internalFeatures; } }

            public GeoCollection<Feature> UnselectedFeatures { get { return unselectedFeatures; } }

            public GeoCollection<Feature> ReshapeFeatures { get { return reshapeFeatures; } }

            public GeoCollection<Feature> AssociateFeatures { get { return associateFeatures; } }
        }

        private const double searchingToleranceRatio = 1;
        private const int maxSnapshootCount = 50;
        private const string selectedFeatureIdColumn = "Id";

        [NonSerialized]
        private Rectangle trackShape;

        [NonSerialized]
        private Point trackStartScreenPoint;

        [NonSerialized]
        private StateController<EditSnapshot> history = new StateController<EditSnapshot>();

        [NonSerialized]
        private Feature editingFeature;

        [NonSerialized]
        private Feature controlFeature;

        [NonSerialized]
        private PointShape originalPosition;

        [NonSerialized]
        private EditMode currentEditMode;

        [NonSerialized]
        private double currentSearchingTolerance;

        [NonSerialized]
        private Collection<EditSnapshot> editSnapshots;

        [NonSerialized]
        private bool isEnabled;

        [NonSerialized]
        private int polygonIndex = int.MaxValue;

        [NonSerialized]
        private int innerRingIndexForMultipolygon = int.MaxValue;

        [NonSerialized]
        private int outerRingVertexIndexForMultipolygon = int.MaxValue;

        [NonSerialized]
        private int innerRingVertexIndexForMultiPolygon = int.MaxValue;

        [NonSerialized]
        private int outerRingVertexIndex = int.MaxValue;

        [NonSerialized]
        private int innerRingVertexIndex = int.MaxValue;

        [NonSerialized]
        private int innerRingIndex = int.MaxValue;

        [NonSerialized]
        private bool isMovingVertex = false;

        [Obfuscation(Exclude = true)]
        private GisEditorWpfMap parentMap;

        [NonSerialized]
        private int matchIndex = 0;

        [NonSerialized]
        private bool indexFound = false;

        [NonSerialized]
        private double worldX;

        [NonSerialized]
        private double worldY;

        [NonSerialized]
        private bool isEdited;

        public bool IsEdited
        {
            get { return isEdited; }
            set { isEdited = value; }
        }

        public GisEditorWpfMap ParentMap
        {
            get { return parentMap; }
            set { parentMap = value; }
        }

        public bool CanRollback
        {
            get
            {
                return history.CanRollBack;
            }
        }

        public bool CanFoward
        {
            get
            {
                return history.CanForward;
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; }
        }

        public PointShape CurrentWorldCoordinate
        {
            get { return new PointShape(worldX, worldY); }
        }

        public void Rollback()
        {
            if (CanRollback)
            {
                LoadSnapshot(history.RollBack());
                if (MapArguments != null)
                {
                    Refresh();
                }
            }
        }

        public void Forward()
        {
            if (CanFoward)
            {
                LoadSnapshot(history.Forward());
                if (MapArguments != null)
                {
                    Refresh();
                }
            }
        }

        public void Cancel()
        {
            history.Clear();
            isEdited = false;
            LoadSnapshot(new EditSnapshot()
            {
                ControlFeature = new Feature(),
                CurrentEditMode = EditMode.None,
                EditingFeature = new Feature(),
                OriginalPosition = new PointShape()
            });
            Refresh();
        }

        public void TakeSnapshot()
        {
            EditSnapshot currentSnapshot = SaveSnapshot();
            history.Add(currentSnapshot);
            if (!isEdited)
            {
                Task task = new Task(() =>
                {
                    isEdited = CheckSaveEditIsEnable();
                });
                task.Start();
            }
        }

        private bool CheckSaveEditIsEnable()
        {
            bool result = false;
            if (EditTargetLayer != null)
            {
                if (EditShapesLayer.InternalFeatures.Count != EditTargetLayer.FeatureIdsToExclude.Count)
                {
                    result = true;
                }
                else if (!EditShapesLayer.InternalFeatures.Any(i => EditTargetLayer.FeatureIdsToExclude.Contains(i.Id)))
                {
                    result = true;
                }
                else
                {
                    List<string> tempIds = EditTargetLayer.FeatureIdsToExclude.ToList();
                    EditTargetLayer.FeatureIdsToExclude.Clear();
                    Collection<Feature> features = null;
                    EditTargetLayer.SafeProcess(() =>
                    {
                        features = EditTargetLayer.FeatureSource.GetFeaturesByIds(tempIds, ReturningColumnsType.AllColumns);
                    });
                    tempIds.ForEach(t =>
                    {
                        EditTargetLayer.FeatureIdsToExclude.Add(t);
                    });
                    if (features != null && !features.Any(f => EditShapesLayer.InternalFeatures.Contains(f.Id) && EditShapesLayer.InternalFeatures[f.Id].GetWellKnownText() == f.GetWellKnownText()))
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        public void ClearSnapshots()
        {
            history.Clear();
            isEdited = false;
        }

        public void RemoveVertex(PointShape targetPointShape)
        {
            RemoveVertexCore(targetPointShape);
        }

        public void RemoveFeatures()
        {
            editShapesLayer.InternalFeatures.Clear();
            editShapesLayer.BuildIndex();
            editingFeature = new Feature();
            ClearVertexControlPoints();
            TakeSnapshot();
            Refresh();
        }

        protected virtual void OnFeatureTrackEnded(Collection<Feature> features)
        {
            EventHandler<FeatureTrackEndedGisEditorEditInteractiveOverlayEventArgs> handler = FeatureTrackEnded;
            if (handler != null)
            {
                var eventArgs = new FeatureTrackEndedGisEditorEditInteractiveOverlayEventArgs() { SelectedFeatures = features };
                handler(this, eventArgs);
            }
        }

        protected virtual Feature OnClickedFeatureFetch(InteractionArguments e)
        {
            EventHandler<FeatureClickedGisEditorEditInteractiveOverlayEventArgs> handler = FeatureClicked;
            Feature editingFeature = null;
            if (handler != null)
            {
                FeatureClickedGisEditorEditInteractiveOverlayEventArgs newE = new FeatureClickedGisEditorEditInteractiveOverlayEventArgs { Arguments = e };
                handler(this, newE);
                if (newE.ClickedFeature != null)
                {
                    editingFeature = newE.ClickedFeature;
                }
            }

            return editingFeature;
        }

        protected override InteractiveResult MouseDownCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseDownCore(interactionArguments);
            if (!CanProcessMouseEvents || interactionArguments.MouseButton == MapMouseButton.Right) return result;

            originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            RectangleShape searchingArea = GetSearchingArea(interactionArguments);
            currentSearchingTolerance = interactionArguments.SearchingTolerance * searchingToleranceRatio;

            if (!editShapesLayer.IsOpen) editShapesLayer.Open();
            if (!reshapeControlPointsLayer.IsOpen) reshapeControlPointsLayer.Open();
            if (!associateControlPointsLayer.IsOpen) associateControlPointsLayer.Open();

            Collection<Feature> focusedFeatures = null;
            if ((focusedFeatures = reshapeControlPointsLayer.QueryTools.GetFeaturesInsideBoundingBox(searchingArea, reshapeControlPointsLayer.GetDistinctColumnNames())).Count != 0)
            {
                controlFeature = focusedFeatures.OrderBy(f => f.GetShape().GetDistanceTo(searchingArea.GetCenterPoint(), GeographyUnit.Meter, DistanceUnit.Meter)).First();
                controlFeature.ColumnValues[selectedFeatureIdColumn] = editingFeature.Id;
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                currentEditMode = EditMode.None;
                TakeSnapshot();
                currentEditMode = EditMode.Reshape;
            }
            else if (AddVertex(originalPosition))
            {
                CalculateVertexControlPoints(editingFeature);
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                currentEditMode = EditMode.None;
                TakeSnapshot();
                currentEditMode = EditMode.Reshape;
            }
            else if ((focusedFeatures = associateControlPointsLayer.QueryTools.GetFeaturesInsideBoundingBox(searchingArea, associateControlPointsLayer.GetDistinctColumnNames())).Count != 0)
            {
                controlFeature = focusedFeatures.First();
                controlFeature.ColumnValues[selectedFeatureIdColumn] = editingFeature.Id;
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                currentEditMode = EditMode.None;
                TakeSnapshot();
                currentEditMode = EditMode.ResizeOrRotate;
            }
            else if ((focusedFeatures = editShapesLayer.QueryTools.GetFeaturesIntersecting(searchingArea, editShapesLayer.GetDistinctColumnNames())).Count != 0)
            {
                editingFeature = focusedFeatures.First();
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                currentEditMode = EditMode.None;
                TakeSnapshot();
                currentEditMode = EditMode.Drag;
            }
            else
            {
                if (!reshapeControlPointsLayer.IsOpen) reshapeControlPointsLayer.Open();
                currentEditMode = EditMode.MultiSelect;

                trackStartScreenPoint = new Point(interactionArguments.ScreenX, interactionArguments.ScreenY);

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

                ParentMap.ToolsGrid.Children.Add(trackShape);
            }
            return result;
        }

        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            worldX = interactionArguments.WorldX;
            worldY = interactionArguments.WorldY;
            if (RequestMouseDownOneTime)
            {
                MouseDown(interactionArguments);
                RequestMouseDownOneTime = false;
            }

            InteractiveResult result = base.MouseMoveCore(interactionArguments);

            //if (ParentMap.TrackOverlay.TrackMode != TrackMode.None
            //    && ParentMap.TrackOverlay.MouseDownCount < 1
            //    && SnappingLayers.Count > 0)
            //{
            //    lock (OverlayCanvas.Children)
            //    {
            //        Point currentPosition = new Point(interactionArguments.WorldX, interactionArguments.WorldY);

            //        var snappingCircle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
            //        if (snappingCircle == null)
            //        {
            //            snappingCircle = new System.Windows.Shapes.Ellipse();
            //            snappingCircle.IsHitTestVisible = false;
            //            snappingCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            //            snappingCircle.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //            snappingCircle.Stroke = new SolidColorBrush(Colors.Black);
            //            snappingCircle.StrokeThickness = 1;
            //            OverlayCanvas.Children.Add(snappingCircle);
            //        }

            //        var snappingDistance = SnappingDistance;
            //        var snappingDistanceUnit = SnappingDistanceUnit;
            //        var snappingScreenPoint = ExtentHelper.ToScreenCoordinate(MapArguments.CurrentExtent, currentPosition.X, currentPosition.Y, (float)MapArguments.ActualWidth, (float)MapArguments.ActualHeight);

            //        try
            //        {
            //            SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, new Vertex(currentPosition.X, currentPosition.Y));
            //            var snappingArea = new PointShape(currentPosition.X, currentPosition.Y)
            //                .Buffer(calc.Distance, MapArguments.MapUnit, calc.DistanceUnit)
            //                .GetBoundingBox();

            //            var snappingScreenSize = Math.Max(snappingArea.Width, snappingArea.Height) / MapArguments.CurrentResolution;
            //            snappingCircle.Width = snappingScreenSize;
            //            snappingCircle.Height = snappingScreenSize;
            //            snappingCircle.Margin = new System.Windows.Thickness(snappingScreenPoint.X - snappingScreenSize * .5, snappingScreenPoint.Y - snappingScreenSize * .5, 0, 0);
            //        }
            //        catch
            //        { }
            //    }
            //}
            //else
            //{
            //    lock (OverlayCanvas.Children)
            //    {
            //        var snappingCircle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
            //        if (snappingCircle != null) OverlayCanvas.Children.Remove(snappingCircle);
            //    }
            //}

            if (!CanProcessMouseEvents) return result;

            currentSearchingTolerance = interactionArguments.SearchingTolerance * searchingToleranceRatio;
            PointShape targetControlPoint = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            if (currentEditMode == EditMode.Drag)
            {
                if (CanDrag || (isPointLayer && CanReshape && EditShapesLayer.InternalFeatures.Count == 1))
                {
                    ParentMap.Cursor = System.Windows.Input.Cursors.Cross;
                    ClearVertexControlPoints();
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                    result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;

                    double offsetX = interactionArguments.WorldX - originalPosition.X;
                    double offsetY = interactionArguments.WorldY - originalPosition.Y;
                    originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);

                    //if a point is in edit mode
                    if (isPointLayer)
                    {
                        Snap();//this method changes originalPosition's value.
                    }

                    if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        foreach (var key in EditShapesLayer.InternalFeatures.GetKeys())
                        {
                            // move all shapes.
                            Feature currentFeature = EditShapesLayer.InternalFeatures[key];
                            BaseShape newShape = BaseShape.TranslateByOffset(currentFeature.GetShape(), offsetX, offsetY, GeographyUnit.Meter, DistanceUnit.Meter);
                            EditShapesLayer.InternalFeatures[key] = new Feature(newShape.GetWellKnownBinary(), currentFeature.Id, currentFeature.ColumnValues);
                        }
                        editingFeature = EditShapesLayer.InternalFeatures.Where(tmpFeature => tmpFeature.Id.Equals(editingFeature.Id)).FirstOrDefault();
                    }
                    else
                    {
                        string currentKey = "";
                        foreach (var key in EditShapesLayer.InternalFeatures.GetKeys())
                        {
                            Feature currentFeature = EditShapesLayer.InternalFeatures[key];
                            if (currentFeature.Id == editingFeature.Id)
                            {
                                currentKey = key;
                                break;
                            }
                        }

                        BaseShape newShape = BaseShape.TranslateByOffset(editingFeature.GetShape(), offsetX, offsetY, GeographyUnit.Meter, DistanceUnit.Meter);
                        editingFeature = new Feature(newShape.GetWellKnownBinary(), editingFeature.Id, editingFeature.ColumnValues);
                        EditShapesLayer.InternalFeatures[currentKey] = editingFeature;
                    }
                }
            }
            else if (currentEditMode == EditMode.Reshape)
            {
                if (CanReshape)
                {
                    isMovingVertex = true;

                    associateControlPointsLayer.InternalFeatures.Clear();
                    originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);

                    Snap();

                    PointShape sourceControlPoint = (PointShape)controlFeature.GetShape();
                    Feature newShape = MoveVertex(editingFeature, sourceControlPoint, originalPosition);

                    UpdateFeature(newShape.GetWellKnownBinary());

                    if (!indexFound)
                    {
                        Parallel.For(0, reshapeControlPointsLayer.InternalFeatures.Count, (i, loopState) =>
                        {
                            Feature feature = reshapeControlPointsLayer.InternalFeatures[i];
                            PointShape pointShape = feature.GetShape() as PointShape;
                            if (pointShape != null &&
                                pointShape.X == sourceControlPoint.X &&
                                pointShape.Y == sourceControlPoint.Y)
                            {
                                matchIndex = i;
                                indexFound = true;

                                loopState.Stop();
                            }
                        });
                    }

                    Feature newFeature = new Feature(originalPosition);
                    newFeature.ColumnValues.Add(existingFeatureColumnName, existingFeatureColumnValue);
                    newFeature.ColumnValues.Add(selectedFeatureIdColumn, editingFeature.Id);
                    if (reshapeControlPointsLayer.InternalFeatures.Count > matchIndex)
                    {
                        reshapeControlPointsLayer.InternalFeatures[matchIndex] = newFeature;
                    }

                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                    result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                }
            }
            else if (currentEditMode == EditMode.ResizeOrRotate)
            {
                if (CanRotate || CanResize)
                {
                    ClearVertexControlPoints();
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                    result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                }
                originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
                Feature newFeature = editingFeature;
                if (CanResize)
                {
                    newFeature = ResizeFeature(newFeature, (PointShape)controlFeature.GetShape(), originalPosition);
                }

                if (CanRotate)
                {
                    newFeature = RotateFeature(newFeature, (PointShape)controlFeature.GetShape(), originalPosition);
                }

                controlFeature = new Feature(originalPosition.GetWellKnownBinary(), GetControlFeatureId(controlFeature), controlFeature.ColumnValues);
                UpdateFeature(newFeature.GetWellKnownBinary());
                CalculateAssociateControlPoints();
            }
            else if (currentEditMode == EditMode.MultiSelect && trackShape != null)
            {
                Point currentPosition = new Point(interactionArguments.ScreenX, interactionArguments.ScreenY);

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

            return result;
        }

        protected override InteractiveResult MouseUpCore(InteractionArguments interactionArguments)
        {
            isMovingVertex = false;
            indexFound = false;
            editsInProcess = null;

            polygonIndex = int.MaxValue;
            innerRingIndexForMultipolygon = int.MaxValue;
            outerRingVertexIndexForMultipolygon = int.MaxValue;
            innerRingVertexIndexForMultiPolygon = int.MaxValue;

            outerRingVertexIndex = int.MaxValue;
            innerRingVertexIndex = int.MaxValue;
            innerRingIndex = int.MaxValue;

            if (cachedFeatureSourceInCurrentExtent != null)
            {
                if (cachedFeatureSourceInCurrentExtent.IsOpen) cachedFeatureSourceInCurrentExtent.Close();
                cachedFeatureSourceInCurrentExtent.InternalFeatures.Clear();
                cachedFeatureSourceInCurrentExtent = null;
            }

            InteractiveResult result = base.MouseUpCore(interactionArguments);
            if (!CanProcessMouseEvents) return result;

            originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            currentSearchingTolerance = interactionArguments.SearchingTolerance * searchingToleranceRatio;

            if (currentEditMode != EditMode.None
                && currentEditMode != EditMode.MultiSelect)
            {
                InteractiveOverlayHelper.ResetInProcessInteractiveOverlayImageSource(this);
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                currentEditMode = EditMode.None;

                if (CanResize || CanRotate)
                {
                    CalculateAssociateControlPoints(editingFeature);
                }
                else if (CanReshape)
                {
                    ClearVertexControlPoints();
                    CalculateReshapeControlPoints(editingFeature);
                }

                TakeSnapshot();
                editShapesLayer.BuildIndex();
            }
            else if (currentEditMode == EditMode.MultiSelect)
            {
                Point currentScreenPoint = new Point(interactionArguments.ScreenX, interactionArguments.ScreenY);
                if (Math.Abs(trackStartScreenPoint.X - interactionArguments.ScreenX) > 0 && Math.Abs(trackStartScreenPoint.Y - interactionArguments.ScreenY) > 0)
                {
                    PointShape startPointInDegree = ParentMap.ToWorldCoordinate(new PointShape(trackStartScreenPoint.X, trackStartScreenPoint.Y));
                    PointShape endPointInDegree = ParentMap.ToWorldCoordinate(new PointShape(currentScreenPoint.X, currentScreenPoint.Y));
                    double minX = startPointInDegree.X < endPointInDegree.X ? startPointInDegree.X : endPointInDegree.X;
                    double maxX = startPointInDegree.X < endPointInDegree.X ? endPointInDegree.X : startPointInDegree.X;
                    double minY = startPointInDegree.Y < endPointInDegree.Y ? startPointInDegree.Y : endPointInDegree.Y;
                    double maxY = startPointInDegree.Y < endPointInDegree.Y ? endPointInDegree.Y : startPointInDegree.Y;

                    var features = GetTargetFeaturesInterseting(new RectangleShape(minX, maxY, maxX, minY));
                    OnFeatureTrackEnded(features);
                }

                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                currentEditMode = EditMode.None;

                ParentMap.ToolsGrid.Children.Remove(trackShape);
                trackShape = null;
            }

            //Remove the snapping tolerance feature.
            snappingToleranceLayer.InternalFeatures.Clear();
            ParentMap.Cursor = System.Windows.Input.Cursors.Arrow;
            return result;
        }

        protected override InteractiveResult MouseClickCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseDoubleClickCore(interactionArguments);
            if (!CanProcessMouseEvents || interactionArguments.MouseButton == MapMouseButton.Right) return result;

            currentSearchingTolerance = interactionArguments.SearchingTolerance * searchingToleranceRatio;
            originalPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);

            Snap();

            RectangleShape searchingArea = GetSearchingArea(interactionArguments);

            Monitor.Enter(editShapesLayer);
            Monitor.Enter(reshapeControlPointsLayer);

            try
            {
                if (!editShapesLayer.IsOpen) editShapesLayer.Open();
                if (!reshapeControlPointsLayer.IsOpen) reshapeControlPointsLayer.Open();

                Collection<Feature> focusedFeatures = null;

                if ((focusedFeatures = reshapeControlPointsLayer.QueryTools.GetFeaturesInsideBoundingBox(searchingArea, reshapeControlPointsLayer.GetDistinctColumnNames())).Count != 0)
                {
                    ClearVertexControlPoints();
                    SetHighlightControlPoint(interactionArguments);
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                    result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                }
                else if ((focusedFeatures = editShapesLayer.QueryTools.GetFeaturesInsideBoundingBox(searchingArea, editShapesLayer.GetDistinctColumnNames())).Count != 0)
                {
                    SelectFeatureOfEditShapesLayer(interactionArguments, result, focusedFeatures);
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                }
                else
                {
                    currentEditMode = EditMode.None;
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                    result.DrawThisOverlay = InteractiveOverlayDrawType.DoNotDraw;
                    SelectFeatureOfEditShapesLayer(interactionArguments, result, focusedFeatures);
                }
                return result;
            }
            finally
            {
                Monitor.Exit(editShapesLayer);
                Monitor.Exit(reshapeControlPointsLayer);
            }
        }

        protected override InteractiveResult ManipulationStartedCore(InteractionArguments interactionArguments)
        {
            return MouseDown(interactionArguments);
        }

        protected override InteractiveResult ManipulationDeltaCore(InteractionArguments interactionArguments)
        {
            return MouseMove(interactionArguments);
        }

        protected override InteractiveResult ManipulationCompletedCore(InteractionArguments interactionArguments)
        {
            return MouseUp(interactionArguments);
        }

        private void Snap()
        {
            if (SnappingLayers.Count != 0)
            {
                foreach (var snappingLayer in SnappingLayers)
                {
                    lock (snappingLayer)
                    {
                        var snappingPoint = GetSnappingPointFromLayer(snappingLayer);

                        if (snappingPoint != null)
                        {
                            originalPosition = snappingPoint;
                        }
                        else
                        {
                            snappingPoint = GetSnappingPointFromLayer(editShapesLayer);
                            if (snappingPoint != null)
                            {
                                originalPosition = snappingPoint;
                            }
                        }
                    }
                }

                ShowSnappingArea();
            }
        }

        private PointShape GetSnappingPointFromLayer(FeatureLayer layer)
        {
            if (!layer.IsOpen) layer.Open();
            var boundingBox = ParentMap.CurrentExtent;
            var screenWidth = ParentMap.ActualWidth;

            if (cachedFeatureSourceInCurrentExtent == null)
            {
                var idsToExcludeBackup = new Collection<string>(layer.FeatureIdsToExclude);
                layer.FeatureIdsToExclude.Clear();

                var featuresToCache = layer.QueryTools.GetFeaturesInsideBoundingBox(MapArguments.CurrentExtent, ReturningColumnsType.NoColumns);
                cachedFeatureSourceInCurrentExtent = new InMemoryFeatureSource(new Collection<FeatureSourceColumn>());
                cachedFeatureSourceInCurrentExtent.Open();
                cachedFeatureSourceInCurrentExtent.BeginTransaction();
                foreach (var featureForDrawing in featuresToCache)
                {
                    cachedFeatureSourceInCurrentExtent.AddFeature(featureForDrawing);
                }
                cachedFeatureSourceInCurrentExtent.CommitTransaction();

                if (idsToExcludeBackup.Count > 0)
                {
                    foreach (var tmpId in idsToExcludeBackup)
                    {
                        layer.FeatureIdsToExclude.Add(tmpId);
                    }
                }
            }

            if (!cachedFeatureSourceInCurrentExtent.IsOpen) cachedFeatureSourceInCurrentExtent.Open();

            SnappingAdapter calc = SnappingAdapter.Convert(SnappingDistance, SnappingDistanceUnit, MapArguments, originalPosition);
            var featuresWithinDistance = cachedFeatureSourceInCurrentExtent.GetFeaturesWithinDistanceOf(originalPosition, MapArguments.MapUnit, calc.DistanceUnit, calc.Distance, cachedFeatureSourceInCurrentExtent.GetDistinctColumnNames());

            var snappingFeature = featuresWithinDistance.FirstOrDefault();
            if (snappingFeature != null && snappingFeature.GetShape() != null)
            {
                var snappingPoint = GetSnappingPointFromFeature(snappingFeature, originalPosition);
                return snappingPoint;
            }

            return null;
        }

        private void ShowSnappingArea()
        {
            //generate the snapping tolerance feature.
            if (SnappingLayers.Count > 0)
            {
                SnappingAdapter calc = SnappingAdapter.Convert(SnappingDistance, SnappingDistanceUnit, MapArguments, originalPosition);
                Feature snappingToleranceFeature = new Feature(originalPosition.Buffer(calc.Distance, MapArguments.MapUnit, calc.DistanceUnit));
                snappingToleranceLayer.InternalFeatures.Clear();
                snappingToleranceLayer.InternalFeatures.Add(snappingToleranceFeature);
            }
        }

        private PointShape GetSnappingPointFromFeature(Feature snappingFeature, PointShape originPoint)
        {
            var snappingShape = snappingFeature.GetShape();
            if (snappingShape != null)
            {
                PointShape snappingPoint = GetSnappingPoint(snappingShape, originPoint);
                if (snappingPoint != null)
                {
                    return snappingPoint;
                }
            }

            return originPoint;
        }

        private PointShape GetSnappingPoint(BaseShape targetShape, PointShape sourcePoint)
        {
            SnappingAdapter calc = SnappingAdapter.Convert(SnappingDistance, SnappingDistanceUnit, MapArguments, sourcePoint);
            return GetSnappingPoint(targetShape, sourcePoint, MapArguments.MapUnit, calc.Distance, calc.DistanceUnit);
        }

        public static PointShape GetSnappingPoint(BaseShape targetShape, PointShape sourcePoint, GeographyUnit mapUnit, double withinDistance, DistanceUnit distanceUnit)
        {
            WellKnownType wkType = targetShape.GetWellKnownType();
            var snappingRect = sourcePoint.Buffer(withinDistance, 4, mapUnit, distanceUnit).GetBoundingBox();
            var findClosestPointFromASetOfPoints = new Func<PointShape, IEnumerable<PointShape>, PointShape>((centerPoint, sourcePoints) =>
            {
                Collection<PointShape> tmpPoints = new Collection<PointShape>();
                foreach (var point in sourcePoints)
                {
                    if (snappingRect.Contains(point)) tmpPoints.Add(point);
                }
                return GetClosestPointFrom(tmpPoints, centerPoint);
            });

            PointShape targetPoint = sourcePoint;
            if (wkType == WellKnownType.Point || wkType == WellKnownType.Multipoint)
            {
                targetPoint = (PointShape)targetShape;
            }
            else if (wkType == WellKnownType.Line)
            {
                targetPoint = findClosestPointFromASetOfPoints(sourcePoint, GetPointsFromLine((LineShape)targetShape));
            }
            else if (wkType == WellKnownType.Multiline)
            {
                var pointsInLines = ((MultilineShape)targetShape).Lines.SelectMany(tmpLine => GetPointsFromLine(tmpLine));
                targetPoint = findClosestPointFromASetOfPoints(sourcePoint, pointsInLines);
            }
            else if (wkType == WellKnownType.Polygon)
            {
                targetPoint = findClosestPointFromASetOfPoints(sourcePoint, GetPointsFromPolygon((PolygonShape)targetShape));
            }
            else if (wkType == WellKnownType.Multipolygon)
            {
                targetPoint = findClosestPointFromASetOfPoints(sourcePoint, GetPointsFromMultiPolygon((MultipolygonShape)targetShape));
            }

            if (sourcePoint != targetPoint) return GetSnappedPointWithinDistance(sourcePoint, targetPoint, withinDistance, distanceUnit, mapUnit);
            else return sourcePoint;
        }

        private static PointShape GetClosestPointFrom(IEnumerable<PointShape> targetPoints, PointShape sourcePoint)
        {
            return targetPoints.OrderBy(tmpPoint => Math.Pow(Math.Pow(tmpPoint.X - sourcePoint.X, 2) + Math.Pow(tmpPoint.Y - sourcePoint.Y, 2), .5)).FirstOrDefault();
        }

        private static PointShape GetSnappedPointWithinDistance(PointShape sourcePoint, PointShape targetPoint, double withinDistance, DistanceUnit distanceUnit, GeographyUnit mapUnit)
        {
            if (targetPoint == null) return sourcePoint;

            double distance = sourcePoint.GetDistanceTo(targetPoint, mapUnit, distanceUnit);
            if (distance < withinDistance) return targetPoint;
            else return sourcePoint;
        }

        private static IEnumerable<PointShape> GetPointsFromMultiPolygon(MultipolygonShape multipolygonShape)
        {
            foreach (var polygonShape in multipolygonShape.Polygons)
            {
                foreach (var point in GetPointsFromPolygon(polygonShape))
                {
                    yield return point;
                }
            }
        }

        private static IEnumerable<PointShape> GetPointsFromPolygon(PolygonShape polygonShape)
        {
            foreach (var point in GetPointsFromRing(polygonShape.OuterRing))
            {
                yield return point;
            }
            foreach (var innerRing in polygonShape.InnerRings)
            {
                foreach (var point in GetPointsFromRing(innerRing))
                {
                    yield return point;
                }
            }
        }

        private static IEnumerable<PointShape> GetPointsFromRing(RingShape ringShape)
        {
            foreach (var vertex in ringShape.Vertices)
            {
                yield return new PointShape(vertex);
            }
        }

        private static IEnumerable<PointShape> GetPointsFromLine(LineShape lineShape)
        {
            foreach (var vertex in lineShape.Vertices)
            {
                yield return new PointShape(vertex);
            }
        }

        private void SetHighlightControlPoint(InteractionArguments interactionArguments)
        {
            PointShape currentWorldPosition = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            RectangleShape searchArea = GetSearchingArea(interactionArguments);
            var tmpControl = reshapeControlPointsLayer.InternalFeatures.AsParallel()
                .Where(f => f.GetShape().IsWithin(searchArea))
                .OrderBy(f => f.GetShape().GetDistanceTo(currentWorldPosition, GeographyUnit.Meter, DistanceUnit.Meter))
                .FirstOrDefault();
            if (tmpControl != null && tmpControl.GetShape() != null) tmpControl.ColumnValues.Add(existingFeatureColumnName, existingFeatureColumnValue);
        }

        private void SelectFeatureOfEditShapesLayer(InteractionArguments interactionArguments, InteractiveResult result, Collection<Feature> focusedFeatures)
        {
            Feature tmpEditingFeature = OnClickedFeatureFetch(interactionArguments);
            if (tmpEditingFeature != null)
                editingFeature = tmpEditingFeature;
        }

        protected override void LoadStateCore(byte[] state)
        {
            base.LoadStateCore(state);
            ClearSnapshots();
            TakeSnapshot();
        }

        public void ClearVertexControlPoints()
        {
            reshapeControlPointsLayer.InternalFeatures.Clear();
            associateControlPointsLayer.InternalFeatures.Clear();
        }

        public void CalculateVertexControlPoints()
        {
            CalculateVertexControlPoints(editingFeature);
        }

        public void CalculateVertexControlPoints(Feature feature)
        {
            ClearVertexControlPoints();
            CalculateReshapeControlPoints(feature);
            CalculateAssociateControlPoints(feature);
        }

        protected virtual IEnumerable<Feature> CalculateVertexControlPointsCore(Feature feature)
        {
            WellKnownType wellKnowType = feature.GetWellKnownType();
            IEnumerable<Feature> returnValues = new Collection<Feature>();
            switch (wellKnowType)
            {
                case WellKnownType.Multipoint:
                    returnValues = CaculateVertexControlPointsForMultipointTypeFeature(feature);
                    break;

                case WellKnownType.Line:
                    returnValues = CaculateVertexControlPointsForLineTypeFeature(feature);
                    break;

                case WellKnownType.Multiline:
                    returnValues = CaculateVertexControlPointsForMultilineTypeFeature(feature);
                    break;

                case WellKnownType.Polygon:
                    returnValues = CaculateVertexControlPointsForPolygonTypeFeature(feature);
                    break;

                case WellKnownType.Multipolygon:
                    returnValues = CaculateVertexControlPointsForMultipolygonTypeFeature(feature);
                    break;

                case WellKnownType.Point:
                case WellKnownType.Invalid:
                default:
                    break;
            }

            foreach (var pointFeature in returnValues)
            {
                pointFeature.ColumnValues.Add(selectedFeatureIdColumn, feature.Id);
            }

            return returnValues;
        }

        public void CalculateReshapeControlPoints()
        {
            CalculateReshapeControlPoints(editingFeature);
        }

        public void CalculateReshapeControlPoints(Feature feature)
        {
            if (feature == null) return;
            IEnumerable<Feature> controlPoints = CalculateVertexControlPointsCore(feature);
            foreach (var controlPoint in controlPoints)
            {
                reshapeControlPointsLayer.InternalFeatures.Add(controlPoint);
            }
        }

        public void CalculateAssociateControlPoints()
        {
            CalculateAssociateControlPoints(editingFeature);
        }

        public void CalculateAssociateControlPoints(Feature feature)
        {
            if (feature == null || feature.GetWellKnownBinary() == null) return;
            WellKnownType wktype = feature.GetWellKnownType();
            if (wktype != WellKnownType.Invalid && wktype != WellKnownType.Point)
            {
                PointShape lowerRightPoint = feature.GetBoundingBox().LowerRightPoint;
                Feature associateFeature = new Feature(lowerRightPoint.X + currentSearchingTolerance, lowerRightPoint.Y - currentSearchingTolerance);
                associateFeature.ColumnValues.Add(selectedFeatureIdColumn, feature.Id);
                associateControlPointsLayer.InternalFeatures.Add(associateFeature);
            }
        }

        public bool CanAddVertex(PointShape targetPosition)
        {
            var targetWorldPosition = ParentMap.ToWorldCoordinate(targetPosition);
            double searchingTolerance = clickPointTolerance * Math.Max(ParentMap.CurrentExtent.Width / ParentMap.ActualWidth, ParentMap.CurrentExtent.Height / ParentMap.ActualHeight);
            RectangleShape searchingArea = new RectangleShape(targetWorldPosition.X - searchingTolerance, targetWorldPosition.Y + searchingTolerance, targetWorldPosition.X + searchingTolerance, targetWorldPosition.Y - searchingTolerance);
            foreach (var feature in EditShapesLayer.InternalFeatures)
            {
                var shape = feature.GetShape();
                if (shape is LineShape)
                {
                    var lineShape = shape as LineShape;
                    for (int i = 0; i < lineShape.Vertices.Count - 1; i++)
                    {
                        LineShape currentLine = new LineShape(new Vertex[] { lineShape.Vertices[i], lineShape.Vertices[i + 1] });
                        if (searchingArea.Intersects(currentLine))
                            return true;
                    }
                }
                else if (shape is MultilineShape)
                {
                    var lineShape = shape as MultilineShape;
                    foreach (var line in lineShape.Lines)
                    {
                        for (int i = 0; i < line.Vertices.Count - 1; i++)
                        {
                            LineShape currentLine = new LineShape(new Vertex[] { line.Vertices[i], line.Vertices[i + 1] });
                            if (searchingArea.Intersects(currentLine))
                                return true;
                        }
                    }
                }
                else if (shape is PolygonShape)
                {
                    var polygonShape = shape as PolygonShape;
                    for (int i = 0; i < polygonShape.OuterRing.Vertices.Count - 1; i++)
                    {
                        LineShape currentLine = new LineShape(new Vertex[] { polygonShape.OuterRing.Vertices[i], polygonShape.OuterRing.Vertices[i + 1] });
                        if (searchingArea.Intersects(currentLine))
                            return true;
                    }

                    foreach (var innerRing in polygonShape.InnerRings)
                    {
                        for (int i = 0; i < innerRing.Vertices.Count - 1; i++)
                        {
                            LineShape currentLine = new LineShape(new Vertex[] { innerRing.Vertices[i], innerRing.Vertices[i + 1] });
                            if (searchingArea.Intersects(currentLine))
                                return true;
                        }
                    }
                }
                else if (shape is MultipolygonShape)
                {
                    var multiPolygonShape = shape as MultipolygonShape;
                    foreach (var polygon in multiPolygonShape.Polygons)
                    {
                        for (int i = 0; i < polygon.OuterRing.Vertices.Count - 1; i++)
                        {
                            LineShape currentLine = new LineShape(new Vertex[] { polygon.OuterRing.Vertices[i], polygon.OuterRing.Vertices[i + 1] });
                            if (searchingArea.Intersects(currentLine))
                                return true;
                        }

                        foreach (var innerRing in polygon.InnerRings)
                        {
                            for (int i = 0; i < innerRing.Vertices.Count - 1; i++)
                            {
                                LineShape currentLine = new LineShape(new Vertex[] { innerRing.Vertices[i], innerRing.Vertices[i + 1] });
                                if (searchingArea.Intersects(currentLine))
                                    return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public bool CanRemoveVertex(PointShape targetPosition)
        {
            var targetWorldPosition = ParentMap.ToWorldCoordinate(targetPosition);
            double searchingTolerance = clickPointTolerance * Math.Max(ParentMap.CurrentExtent.Width / ParentMap.ActualWidth, ParentMap.CurrentExtent.Height / ParentMap.ActualHeight);
            RectangleShape searchingArea = new RectangleShape(targetWorldPosition.X - searchingTolerance, targetWorldPosition.Y + searchingTolerance, targetWorldPosition.X + searchingTolerance, targetWorldPosition.Y - searchingTolerance);
            foreach (var feature in ReshapeControlPointsLayer.InternalFeatures)
            {
                var pointShape = feature.GetShape() as PointShape;
                if (searchingArea.Contains(pointShape))
                {
                    return true;
                }
            }
            return false;
        }

        private byte[] RemoveOnePointFromMultipointFeature(PointShape removedPoint, ref bool isSuccess)
        {
            WellKnownType wellKnowType = editingFeature.GetWellKnownType();
            Vertex removedVertex = new Vertex(removedPoint);
            switch (wellKnowType)
            {
                case WellKnownType.Multipoint:
                    MultipointShape multipoint = (MultipointShape)editingFeature.GetShape();
                    isSuccess = multipoint.RemoveVertex(removedVertex);
                    return multipoint.GetWellKnownBinary();
                case WellKnownType.Line:
                    LineShape line = (LineShape)editingFeature.GetShape();
                    isSuccess = line.RemoveVertex(removedVertex);
                    return line.GetWellKnownBinary();
                case WellKnownType.Multiline:
                    MultilineShape multiline = (MultilineShape)editingFeature.GetShape();
                    isSuccess = multiline.RemoveVertex(removedVertex);
                    return multiline.GetWellKnownBinary();
                case WellKnownType.Polygon:
                    PolygonShape polygon = (PolygonShape)editingFeature.GetShape();
                    isSuccess = polygon.RemoveVertex(removedVertex);
                    return polygon.GetWellKnownBinary();
                case WellKnownType.Multipolygon:
                    MultipolygonShape multipolygon = (MultipolygonShape)editingFeature.GetShape();
                    isSuccess = multipolygon.RemoveVertex(removedVertex);
                    return multipolygon.GetWellKnownBinary();
                default:
                    return editingFeature.GetWellKnownBinary();
            }
        }

        private void UpdateFeature(byte[] wkb)
        {
            try
            {
                Feature tempFeature = new Feature(wkb, editingFeature.Id, editingFeature.ColumnValues);
                var isValid = SqlTypesGeometryHelper.IsValid(tempFeature);

                if (!isValid) { tempFeature = SqlTypesGeometryHelper.MakeValid(tempFeature); }

                if (SqlTypesGeometryHelper.IsValid(tempFeature))
                {
                    editingFeature = tempFeature;
                    foreach (string key in EditShapesLayer.InternalFeatures.GetKeys())
                    {
                        if (string.Equals(EditShapesLayer.InternalFeatures[key].Id, editingFeature.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            EditShapesLayer.InternalFeatures[key] = editingFeature;
                            break;
                        }
                    }

                    EditShapesLayer.BuildIndex();
                }
            }
            catch
            { }
        }

        private static IEnumerable<Feature> CaculateVertexControlPointsForMultipointTypeFeature(Feature multipointFeature)
        {
            Collection<Feature> returnValues = new Collection<Feature>();

            MultipointShape multiPointShape = multipointFeature.GetShape() as MultipointShape;
            for (int k = 0; k < multiPointShape.Points.Count; k++)
            {
                PointShape pointShape = new PointShape(multiPointShape.Points[k].X, multiPointShape.Points[k].Y);
                returnValues.Add(new Feature(new Vertex(pointShape)));
            }

            return returnValues;
        }

        private static IEnumerable<Feature> CaculateVertexControlPointsForLineTypeFeature(Feature LineFeature)
        {
            Collection<Feature> returnValues = new Collection<Feature>();

            LineShape lineShape = LineFeature.GetShape() as LineShape;
            for (int k = 0; k < lineShape.Vertices.Count; k++)
            {
                Feature reshapeFeature = new Feature(lineShape.Vertices[k]);
                returnValues.Add(reshapeFeature);
            }

            return returnValues;
        }

        private static IEnumerable<Feature> CaculateVertexControlPointsForMultilineTypeFeature(Feature multiLineFeature)
        {
            Collection<Feature> returnValues = new Collection<Feature>();

            MultilineShape multiLineShape = multiLineFeature.GetShape() as MultilineShape;
            for (int j = 0; j < multiLineShape.Lines.Count; j++)
            {
                LineShape lineShape = multiLineShape.Lines[j];
                for (int k = 0; k < lineShape.Vertices.Count; k++)
                {
                    Feature reshapeFeature = new Feature(lineShape.Vertices[k]);
                    returnValues.Add(reshapeFeature);
                }
            }

            return returnValues;
        }

        private static IEnumerable<Feature> CaculateVertexControlPointsForPolygonTypeFeature(Feature polygonFeature)
        {
            Collection<Feature> returnValues = new Collection<Feature>();

            PolygonShape polygonShape = polygonFeature.GetShape() as PolygonShape;
            RingShape outRing = polygonShape.OuterRing;
            for (int k = 0; k < outRing.Vertices.Count; k++)
            {
                Feature reshapeFeature = new Feature(outRing.Vertices[k]);
                returnValues.Add(reshapeFeature);
            }

            for (int j = 0; j < polygonShape.InnerRings.Count; j++)
            {
                RingShape innerRing = polygonShape.InnerRings[j];
                for (int k = 0; k < innerRing.Vertices.Count; k++)
                {
                    Feature reshapeFeature = new Feature(innerRing.Vertices[k]);
                    returnValues.Add(reshapeFeature);
                }
            }

            return returnValues;
        }

        private static IEnumerable<Feature> CaculateVertexControlPointsForMultipolygonTypeFeature(Feature multipolygonFeature)
        {
            ConcurrentBag<Feature> returnValues = new ConcurrentBag<Feature>();

            MultipolygonShape multiPolygonShape = multipolygonFeature.GetShape() as MultipolygonShape;

            Parallel.For(0, multiPolygonShape.Polygons.Count, (i) =>
            {
                PolygonShape polygonShape = multiPolygonShape.Polygons[i];
                RingShape outRing = polygonShape.OuterRing;
                for (int k = 0; k < outRing.Vertices.Count; k++)
                {
                    Feature reshapeFeature = new Feature(outRing.Vertices[k]);
                    returnValues.Add(reshapeFeature);
                }

                for (int j = 0; j < polygonShape.InnerRings.Count; j++)
                {
                    RingShape innerRing = polygonShape.InnerRings[j];
                    for (int k = 0; k < innerRing.Vertices.Count; k++)
                    {
                        Feature reshapeFeature = new Feature(innerRing.Vertices[k]);
                        returnValues.Add(reshapeFeature);
                    }
                }
            });

            //for (int i = 0; i < multiPolygonShape.Polygons.Count; i++)
            //{
            //    PolygonShape polygonShape = multiPolygonShape.Polygons[i];
            //    RingShape outRing = polygonShape.OuterRing;
            //    for (int k = 0; k < outRing.Vertices.Count; k++)
            //    {
            //        Feature reshapeFeature = new Feature(outRing.Vertices[k]);
            //        returnValues.Add(reshapeFeature);
            //    }

            //    for (int j = 0; j < polygonShape.InnerRings.Count; j++)
            //    {
            //        RingShape innerRing = polygonShape.InnerRings[j];
            //        for (int k = 0; k < innerRing.Vertices.Count; k++)
            //        {
            //            Feature reshapeFeature = new Feature(innerRing.Vertices[k]);
            //            returnValues.Add(reshapeFeature);
            //        }
            //    }
            //}

            return returnValues;
        }

        private static RectangleShape GetSearchingArea(InteractionArguments interactionArguments)
        {
            PointShape targetWorldPoint = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            double searchingTolerance = interactionArguments.SearchingTolerance * searchingToleranceRatio;
            return new RectangleShape(targetWorldPoint.X - searchingTolerance, targetWorldPoint.Y + searchingTolerance, targetWorldPoint.X + searchingTolerance, targetWorldPoint.Y - searchingTolerance);
        }

        private Feature MoveVertex(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            Feature returnFeature = new Feature();
            controlFeature = new Feature(targetControlPoint.GetWellKnownBinary(), GetControlFeatureId(controlFeature), controlFeature.ColumnValues);
            controlFeature.ColumnValues[existingFeatureColumnName] = existingFeatureColumnValue;
            WellKnownType wellKnowType = sourceFeature.GetWellKnownType();
            switch (wellKnowType)
            {
                case WellKnownType.Multipoint:
                    returnFeature = MoveVertexForMultipointTypeFeature(sourceFeature, sourceControlPoint, targetControlPoint);
                    break;

                case WellKnownType.Line:
                    returnFeature = MoveVertexForLineTypeFeature(sourceFeature, sourceControlPoint, targetControlPoint);
                    break;

                case WellKnownType.Multiline:
                    returnFeature = MoveVertexForMultiLineTypeFeature(sourceFeature, sourceControlPoint, targetControlPoint);
                    break;

                case WellKnownType.Polygon:
                    returnFeature = MoveVertexForPolygonTypeFeature(sourceFeature, sourceControlPoint, targetControlPoint);
                    break;

                case WellKnownType.Multipolygon:
                    returnFeature = MoveVertexForMultipolygonTypeFeature(sourceFeature, sourceControlPoint, targetControlPoint);
                    break;

                case WellKnownType.Point:
                case WellKnownType.Invalid:
                default:
                    break;
            }

            //foreach (string featureKey in reshapeControlPointsLayer.InternalFeatures.GetKeys())
            //{
            //    Feature feature = reshapeControlPointsLayer.InternalFeatures[featureKey];
            //    if (feature.ColumnValues != null)
            //    {
            //        if (feature.ColumnValues.ContainsKey(existingFeatureColumnName) && feature.ColumnValues[existingFeatureColumnName] == existingFeatureColumnValue)
            //        {
            //            reshapeControlPointsLayer.InternalFeatures[featureKey] = new Feature(targetControlPoint.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
            //        }
            //    }
            //}

            return returnFeature;
        }

        private Feature MoveVertexForPolygonTypeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            PolygonShape originalShape = sourceFeature.GetShape() as PolygonShape;
            originalShape.Id = sourceFeature.Id;

            if (isMovingVertex
                && (outerRingVertexIndex != int.MaxValue || innerRingIndex != int.MaxValue || innerRingVertexIndex != int.MaxValue)
                && (outerRingVertexIndex != originalShape.OuterRing.Vertices.Count - 1 || (innerRingIndex != int.MaxValue && innerRingVertexIndex != originalShape.InnerRings[innerRingIndex].Vertices.Count - 1)))
            {
                if (outerRingVertexIndex != int.MaxValue)
                {
                    originalShape.OuterRing.Vertices[outerRingVertexIndex] = new Vertex(targetControlPoint);
                    UpdateEditsInProcessIfExists(targetControlPoint);
                }
                else if (innerRingIndex != int.MaxValue && innerRingVertexIndex != int.MaxValue)
                {
                    originalShape.InnerRings[innerRingIndex].Vertices[innerRingVertexIndex] = new Vertex(targetControlPoint);
                    UpdateEditsInProcessIfExists(targetControlPoint);
                }
            }
            else
            {
                for (int i = 0; i < originalShape.OuterRing.Vertices.Count; i++)
                {
                    Vertex currentVertex = originalShape.OuterRing.Vertices[i];
                    if (currentVertex.X == sourceControlPoint.X && currentVertex.Y == sourceControlPoint.Y)
                    {
                        originalShape.OuterRing.Vertices[i] = new Vertex(targetControlPoint);
                        UpdateEditsInProcessForPolygonOuterRing(targetControlPoint, originalShape, i);

                        outerRingVertexIndex = i;
                        if (i != 0 && i != originalShape.OuterRing.Vertices.Count - 1)
                        {
                            break;
                        }
                    }
                }

                for (int i = 0; i < originalShape.InnerRings.Count; i++)
                {
                    for (int j = 0; j < originalShape.InnerRings[i].Vertices.Count; j++)
                    {
                        Vertex currentVertex = originalShape.InnerRings[i].Vertices[j];

                        if (currentVertex.X == sourceControlPoint.X && currentVertex.Y == sourceControlPoint.Y)
                        {
                            originalShape.InnerRings[i].Vertices[j] = new Vertex(targetControlPoint);
                            UpdateEditsInProcessForPolygonInnerRing(originalShape, i, j);

                            innerRingIndex = i;
                            innerRingVertexIndex = j;
                            if (j != 0 && j != originalShape.InnerRings[i].Vertices.Count - 1)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return new Feature(originalShape, sourceFeature.ColumnValues);
        }

        private void UpdateEditsInProcessIfExists(PointShape targetControlPoint)
        {
            if (editsInProcess != null && editsInProcess.Vertices.Count == 3)
            {
                editsInProcess.Vertices[1] = new Vertex(targetControlPoint);
            }
        }

        private void UpdateEditsInProcessForPolygonInnerRing(PolygonShape originalShape, int i, int j)
        {
            editsInProcess = new LineShape();
            if (j > 0)
            {
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[j - 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[originalShape.InnerRings[i].Vertices.Count - 1]);
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[originalShape.InnerRings[i].Vertices.Count - 2]);
            }

            editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[j]);

            if (i < originalShape.InnerRings[i].Vertices.Count - 1)
            {
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[j + 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[0]);
                editsInProcess.Vertices.Add(originalShape.InnerRings[i].Vertices[1]);
            }
        }

        private void UpdateEditsInProcessForPolygonOuterRing(PointShape targetControlPoint, PolygonShape originalShape, int i)
        {
            editsInProcess = new LineShape();
            if (i > 0)
            {
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[i - 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[originalShape.OuterRing.Vertices.Count - 1]);
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[originalShape.OuterRing.Vertices.Count - 2]);
            }

            editsInProcess.Vertices.Add(new Vertex(targetControlPoint));

            if (i < originalShape.OuterRing.Vertices.Count - 1)
            {
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[i + 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[0]);
                editsInProcess.Vertices.Add(originalShape.OuterRing.Vertices[1]);
            }
        }

        private Feature MoveVertexForLineTypeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            LineShape originalShape = sourceFeature.GetShape() as LineShape;
            originalShape.Id = sourceFeature.Id;

            for (int i = 0; i < originalShape.Vertices.Count; i++)
            {
                if (originalShape.Vertices[i].X == sourceControlPoint.X && originalShape.Vertices[i].Y == sourceControlPoint.Y)
                {
                    originalShape.Vertices[i] = new Vertex(targetControlPoint);
                    editsInProcess = new LineShape();
                    if (i > 0)
                    {
                        editsInProcess.Vertices.Add(originalShape.Vertices[i - 1]);
                    }

                    editsInProcess.Vertices.Add(originalShape.Vertices[i]);

                    if (i < originalShape.Vertices.Count - 1)
                    {
                        editsInProcess.Vertices.Add(originalShape.Vertices[i + 1]);
                    }
                }
            }

            if (editsInProcess != null && editsInProcess.Vertices.Count <= 1) editsInProcess = null;
            return new Feature(originalShape, sourceFeature.ColumnValues);
        }

        private Feature MoveVertexForMultiLineTypeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            MultilineShape originalShape = sourceFeature.GetShape() as MultilineShape;
            originalShape.Id = sourceFeature.Id;

            for (int i = 0; i < originalShape.Lines.Count; i++)
            {
                for (int j = 0; j < originalShape.Lines[i].Vertices.Count; j++)
                {
                    Vertex currentVertex = originalShape.Lines[i].Vertices[j];
                    if (currentVertex.X == sourceControlPoint.X && currentVertex.Y == sourceControlPoint.Y)
                    {
                        originalShape.Lines[i].Vertices[j] = new Vertex(targetControlPoint);
                        editsInProcess = new LineShape();
                        if (j > 0)
                        {
                            editsInProcess.Vertices.Add(originalShape.Lines[i].Vertices[j - 1]);
                        }

                        editsInProcess.Vertices.Add(originalShape.Lines[i].Vertices[j]);

                        if (j < originalShape.Lines[i].Vertices.Count - 1)
                        {
                            editsInProcess.Vertices.Add(originalShape.Lines[i].Vertices[j + 1]);
                        }
                    }
                }
            }

            return new Feature(originalShape, sourceFeature.ColumnValues);
        }

        private Feature MoveVertexForMultipolygonTypeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            MultipolygonShape originalShape = sourceFeature.GetShape() as MultipolygonShape;
            originalShape.Id = sourceFeature.Id;

            if (isMovingVertex
                && polygonIndex != int.MaxValue
                && ((outerRingVertexIndexForMultipolygon != int.MaxValue && outerRingVertexIndexForMultipolygon != originalShape.Polygons[polygonIndex].OuterRing.Vertices.Count - 1) || (innerRingIndexForMultipolygon != int.MaxValue && innerRingIndexForMultipolygon != originalShape.Polygons[polygonIndex].InnerRings.Count - 1)))
            {
                if (outerRingVertexIndexForMultipolygon != int.MaxValue
                    && outerRingVertexIndexForMultipolygon < originalShape.Polygons[polygonIndex].OuterRing.Vertices.Count)
                {
                    originalShape.Polygons[polygonIndex].OuterRing.Vertices[outerRingVertexIndexForMultipolygon] = new Vertex(targetControlPoint);
                    UpdateEditsInProcessForMultipolygonOuterRing(targetControlPoint, originalShape, polygonIndex, outerRingVertexIndexForMultipolygon);
                }
                else if (innerRingVertexIndexForMultiPolygon != int.MaxValue
                    && innerRingVertexIndexForMultiPolygon < originalShape.Polygons[polygonIndex].InnerRings[innerRingIndexForMultipolygon].Vertices.Count)
                {
                    originalShape.Polygons[polygonIndex].InnerRings[innerRingIndexForMultipolygon].Vertices[innerRingVertexIndexForMultiPolygon] = new Vertex(targetControlPoint);
                    UpdateEditsInProcessForMultipolygonInnerRing(targetControlPoint, originalShape, polygonIndex, innerRingIndexForMultipolygon, innerRingVertexIndexForMultiPolygon);
                }
            }
            else
            {
                for (int j = 0; j < originalShape.Polygons.Count; j++)
                {
                    for (int i = 0; i < originalShape.Polygons[j].OuterRing.Vertices.Count; i++)
                    {
                        Vertex currentVertex = originalShape.Polygons[j].OuterRing.Vertices[i];

                        if (currentVertex.X == sourceControlPoint.X && currentVertex.Y == sourceControlPoint.Y)
                        {
                            originalShape.Polygons[j].OuterRing.Vertices[i] = new Vertex(targetControlPoint);
                            UpdateEditsInProcessForMultipolygonOuterRing(targetControlPoint, originalShape, j, i);

                            polygonIndex = j;
                            outerRingVertexIndexForMultipolygon = i;

                            if (i != 0 && i != originalShape.Polygons[j].OuterRing.Vertices.Count - 1)
                            {
                                break;
                            }
                        }
                    }

                    for (int k = 0; k < originalShape.Polygons[j].InnerRings.Count; k++)
                    {
                        for (int i = 0; i < originalShape.Polygons[j].InnerRings[k].Vertices.Count; i++)
                        {
                            Vertex currentVertex = originalShape.Polygons[j].InnerRings[k].Vertices[i];

                            if (currentVertex.X == sourceControlPoint.X && currentVertex.Y == sourceControlPoint.Y)
                            {
                                originalShape.Polygons[j].InnerRings[k].Vertices[i] = new Vertex(targetControlPoint);
                                UpdateEditsInProcessForMultipolygonInnerRing(targetControlPoint, originalShape, j, k, i);

                                polygonIndex = j;
                                innerRingIndexForMultipolygon = k;
                                innerRingVertexIndexForMultiPolygon = i;

                                if (i != 0 && i != originalShape.Polygons[j].InnerRings[k].Vertices.Count - 1)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return new Feature(originalShape, sourceFeature.ColumnValues);
        }

        private void UpdateEditsInProcessForMultipolygonInnerRing(PointShape targetControlPoint, MultipolygonShape originalShape, int j, int k, int i)
        {
            editsInProcess = new LineShape();
            if (i > 0)
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].InnerRings[k].Vertices[i - 1]);
            }

            editsInProcess.Vertices.Add(new Vertex(targetControlPoint));

            if (i < originalShape.Polygons[j].InnerRings[k].Vertices.Count - 1)
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].InnerRings[k].Vertices[i + 1]);
            }
        }

        private void UpdateEditsInProcessForMultipolygonOuterRing(PointShape targetControlPoint, MultipolygonShape originalShape, int j, int i)
        {
            editsInProcess = new LineShape();
            if (i > 0)
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[i - 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[originalShape.Polygons[j].OuterRing.Vertices.Count - 1]);
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[originalShape.Polygons[j].OuterRing.Vertices.Count - 2]);
            }

            editsInProcess.Vertices.Add(new Vertex(targetControlPoint));

            if (i < originalShape.Polygons[j].OuterRing.Vertices.Count - 1)
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[i + 1]);
            }
            else
            {
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[0]);
                editsInProcess.Vertices.Add(originalShape.Polygons[j].OuterRing.Vertices[1]);
            }
        }

        private static Feature MoveVertexForMultipointTypeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            MultipointShape originalShape = sourceFeature.GetShape() as MultipointShape;
            originalShape.Id = sourceFeature.Id;

            for (int i = 0; i < originalShape.Points.Count; i++)
            {
                PointShape currentPoint = originalShape.Points[i];

                if (currentPoint.X == sourceControlPoint.X && currentPoint.Y == sourceControlPoint.Y)
                {
                    originalShape.Points[i].X = targetControlPoint.X;
                    originalShape.Points[i].Y = targetControlPoint.Y;
                }
            }

            return new Feature(originalShape, sourceFeature.ColumnValues);
        }

        private Feature ResizeFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            PointShape centerPointShape = sourceFeature.GetShape().GetCenterPoint();
            targetControlPoint = new PointShape(targetControlPoint.X, targetControlPoint.Y);

            double currentDistance = GetDistance(targetControlPoint, centerPointShape);
            double referenceDistance = GetDistance(centerPointShape, sourceControlPoint);
            double scale = currentDistance / referenceDistance;

            BaseShape baseShape = BaseShape.ScaleTo(sourceFeature.GetShape(), scale);
            baseShape.Id = sourceFeature.Id;

            Feature returnFeature = new Feature(baseShape, sourceFeature.ColumnValues);
            return returnFeature;
        }

        public bool AddVertex(PointShape targetPointShape)
        {
            bool result = false;
            if (reshapeControlPointsLayer.InternalFeatures.Count == 0) return result;

            double searchingTolerance = clickPointTolerance * Math.Max(ParentMap.CurrentExtent.Width / ParentMap.ActualWidth, ParentMap.CurrentExtent.Height / ParentMap.ActualHeight);
            foreach (string key in editShapesLayer.InternalFeatures.GetKeys())
            {
                Feature currentFeature = AddVertex(editShapesLayer.InternalFeatures[key], targetPointShape, searchingTolerance);
                if (currentFeature.GetShape() != null)
                {
                    editingFeature = currentFeature;
                    editShapesLayer.InternalFeatures[key] = editingFeature;
                    result = true;
                    break;
                }
            }

            return result;
        }

        private Feature AddVertex(Feature targetFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            WellKnownType wellKnowType = targetFeature.GetWellKnownType();
            switch (wellKnowType)
            {
                case WellKnownType.Line:
                    returnFeature = AddVertexToLineFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multiline:
                    returnFeature = AddVertexToMultilineFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Polygon:
                    returnFeature = AddVertexToPolygonFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multipolygon:
                    returnFeature = AddVertexToMultipolygonFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multipoint:
                case WellKnownType.Point:
                case WellKnownType.Invalid:
                default:
                    break;
            }

            return returnFeature;
        }

        private Feature AddVertexToLineFeature(Feature lineFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();
            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            LineShape targetLineShape = lineFeature.GetShape() as LineShape;

            for (int i = 0; i < targetLineShape.Vertices.Count - 1; i++)
            {
                LineShape currentLine = new LineShape(new Vertex[] { targetLineShape.Vertices[i], targetLineShape.Vertices[i + 1] });
                if (searchingArea.Intersects(currentLine))
                {
                    Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                    targetLineShape.Vertices.Insert(i + 1, vertexToBeAdded);
                    controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                    controlFeature.ColumnValues[selectedFeatureIdColumn] = lineFeature.Id;
                    returnFeature = new Feature(targetLineShape.GetWellKnownBinary(), lineFeature.Id, lineFeature.ColumnValues);
                    break;
                }
            }

            return returnFeature;
        }

        private Feature AddVertexToMultilineFeature(Feature multilineFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            MultilineShape targetMultilineShape = multilineFeature.GetShape() as MultilineShape;

            foreach (LineShape targetLineShape in targetMultilineShape.Lines)
            {
                for (int i = 0; i < targetLineShape.Vertices.Count - 1; i++)
                {
                    LineShape currentLine = new LineShape(new Vertex[] { targetLineShape.Vertices[i], targetLineShape.Vertices[i + 1] });
                    if (searchingArea.Intersects(currentLine))
                    {
                        Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                        targetLineShape.Vertices.Insert(i + 1, vertexToBeAdded);
                        controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                        controlFeature.ColumnValues[selectedFeatureIdColumn] = multilineFeature.Id;
                        return new Feature(targetMultilineShape.GetWellKnownBinary(), multilineFeature.Id, multilineFeature.ColumnValues);
                    }
                }
            }

            return returnFeature;
        }

        private Feature AddVertexToPolygonFeature(Feature polygonFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            PolygonShape targetPolygonShape = polygonFeature.GetShape() as PolygonShape;

            RingShape outerRing = targetPolygonShape.OuterRing;
            for (int i = 0; i < outerRing.Vertices.Count - 1; i++)
            {
                LineShape currentLine = new LineShape(new Vertex[] { outerRing.Vertices[i], outerRing.Vertices[i + 1] });
                if (searchingArea.Intersects(currentLine))
                {
                    Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                    outerRing.Vertices.Insert(i + 1, vertexToBeAdded);
                    controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                    controlFeature.ColumnValues[selectedFeatureIdColumn] = polygonFeature.Id;
                    return new Feature(targetPolygonShape.GetWellKnownBinary(), polygonFeature.Id, polygonFeature.ColumnValues);
                }
            }

            for (int i = 0; i < targetPolygonShape.InnerRings.Count; i++)
            {
                RingShape innerRing = targetPolygonShape.InnerRings[i];
                for (int j = 0; j < innerRing.Vertices.Count - 1; j++)
                {
                    LineShape currentLine = new LineShape(new Vertex[] { innerRing.Vertices[j], innerRing.Vertices[j + 1] });
                    if (searchingArea.Intersects(currentLine))
                    {
                        Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                        innerRing.Vertices.Insert(j + 1, vertexToBeAdded);
                        controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                        controlFeature.ColumnValues[selectedFeatureIdColumn] = polygonFeature.Id;
                        return new Feature(targetPolygonShape.GetWellKnownBinary(), polygonFeature.Id, polygonFeature.ColumnValues);
                    }
                }
            }

            return returnFeature;
        }

        private Feature AddVertexToMultipolygonFeature(Feature multipolygonFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            MultipolygonShape targetMultipolygonShape = multipolygonFeature.GetShape() as MultipolygonShape;

            foreach (PolygonShape targetPolygonShape in targetMultipolygonShape.Polygons)
            {
                RingShape outerRing = targetPolygonShape.OuterRing;
                for (int i = 0; i < outerRing.Vertices.Count - 1; i++)
                {
                    LineShape currentLine = new LineShape(new Vertex[] { outerRing.Vertices[i], outerRing.Vertices[i + 1] });
                    if (searchingArea.Intersects(currentLine))
                    {
                        Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                        outerRing.Vertices.Insert(i + 1, vertexToBeAdded);
                        controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                        controlFeature.ColumnValues[selectedFeatureIdColumn] = multipolygonFeature.Id;
                        returnFeature = new Feature(targetMultipolygonShape.GetWellKnownBinary(), multipolygonFeature.Id, multipolygonFeature.ColumnValues);
                        return returnFeature;
                    }
                }

                for (int i = 0; i < targetPolygonShape.InnerRings.Count; i++)
                {
                    RingShape innerRing = targetPolygonShape.InnerRings[i];
                    for (int j = 0; j < innerRing.Vertices.Count - 1; j++)
                    {
                        LineShape currentLine = new LineShape(new Vertex[] { innerRing.Vertices[j], innerRing.Vertices[j + 1] });
                        if (searchingArea.Intersects(currentLine))
                        {
                            Vertex vertexToBeAdded = new Vertex(currentLine.GetClosestPointTo(targetPointShape, mapUnit));
                            innerRing.Vertices.Insert(j + 1, vertexToBeAdded);
                            controlFeature = new Feature(vertexToBeAdded.X, vertexToBeAdded.Y, GetControlFeatureId(controlFeature));
                            controlFeature.ColumnValues[selectedFeatureIdColumn] = multipolygonFeature.Id;
                            returnFeature = new Feature(targetMultipolygonShape.GetWellKnownBinary(), multipolygonFeature.Id, multipolygonFeature.ColumnValues);
                            return returnFeature;
                        }
                    }
                }
            }

            return returnFeature;
        }

        private Feature RemoveVertexFromPolygonFeature(Feature polygonFeature, PointShape targetPointShape, double searchingTolerance)
        {
            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            PolygonShape targetPolygonShape = polygonFeature.GetShape() as PolygonShape;

            RingShape outerRing = targetPolygonShape.OuterRing;
            for (int i = 0; i < outerRing.Vertices.Count - 1; i++)
            {
                if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(outerRing.Vertices[i].X, outerRing.Vertices[i].Y)) && outerRing.Vertices.Count > 4)
                {
                    outerRing.Vertices.RemoveAt(i);
                    if (i == 0)
                    {
                        outerRing.Vertices.RemoveAt(outerRing.Vertices.Count - 1);
                        outerRing.Vertices.Insert(0, outerRing.Vertices[outerRing.Vertices.Count - 1]);
                    }
                    return new Feature(targetPolygonShape.GetWellKnownBinary(), polygonFeature.Id, polygonFeature.ColumnValues);
                }
            }

            for (int i = 0; i < targetPolygonShape.InnerRings.Count; i++)
            {
                RingShape innerRing = targetPolygonShape.InnerRings[i];
                for (int j = 0; j < innerRing.Vertices.Count - 1; j++)
                {
                    LineShape currentLine = new LineShape(new Vertex[] { innerRing.Vertices[j], innerRing.Vertices[j + 1] });
                    if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(innerRing.Vertices[i].X, innerRing.Vertices[i].Y)) && innerRing.Vertices.Count > 4)
                    {
                        innerRing.Vertices.RemoveAt(j + 1);
                        if (i == 0)
                        {
                            innerRing.Vertices.RemoveAt(innerRing.Vertices.Count - 1);
                            innerRing.Vertices.Insert(0, innerRing.Vertices[innerRing.Vertices.Count - 1]);
                        }
                        return new Feature(targetPolygonShape.GetWellKnownBinary(), polygonFeature.Id, polygonFeature.ColumnValues);
                    }
                }
            }

            return new Feature();
        }

        private Feature RemoveVertexFromMultiPolygonFeature(Feature multipolygonFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            MultipolygonShape targetMultipolygonShape = multipolygonFeature.GetShape() as MultipolygonShape;

            foreach (PolygonShape targetPolygonShape in targetMultipolygonShape.Polygons)
            {
                RingShape outerRing = targetPolygonShape.OuterRing;
                for (int i = 0; i < outerRing.Vertices.Count - 1; i++)
                {
                    if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(outerRing.Vertices[i].X, outerRing.Vertices[i].Y)) && outerRing.Vertices.Count > 4)
                    {
                        outerRing.Vertices.RemoveAt(i);
                        if (i == 0)
                        {
                            outerRing.Vertices.RemoveAt(outerRing.Vertices.Count - 1);
                            outerRing.Vertices.Insert(0, outerRing.Vertices[outerRing.Vertices.Count - 1]);
                        }
                        return new Feature(targetMultipolygonShape.GetWellKnownBinary(), multipolygonFeature.Id, multipolygonFeature.ColumnValues);
                    }
                }

                for (int i = 0; i < targetPolygonShape.InnerRings.Count; i++)
                {
                    RingShape innerRing = targetPolygonShape.InnerRings[i];
                    for (int j = 0; j < innerRing.Vertices.Count - 1; j++)
                    {
                        if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(innerRing.Vertices[i].X, innerRing.Vertices[i].Y)) && innerRing.Vertices.Count > 4)
                        {
                            innerRing.Vertices.RemoveAt(i);
                            if (i == 0)
                            {
                                innerRing.Vertices.RemoveAt(innerRing.Vertices.Count - 1);
                                innerRing.Vertices.Insert(0, innerRing.Vertices[innerRing.Vertices.Count - 1]);
                            }
                            return new Feature(targetMultipolygonShape.GetWellKnownBinary(), multipolygonFeature.Id, multipolygonFeature.ColumnValues);
                        }
                    }
                }
            }

            return returnFeature;
        }

        private Feature RemoveVertexToMultilineFeature(Feature multilineFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            MultilineShape targetMultilineShape = multilineFeature.GetShape() as MultilineShape;

            foreach (LineShape targetLineShape in targetMultilineShape.Lines)
            {
                for (int i = 0; i <= targetLineShape.Vertices.Count - 1; i++)
                {
                    if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(targetLineShape.Vertices[i].X, targetLineShape.Vertices[i].Y)) && targetLineShape.Vertices.Count > 2)
                    {
                        targetLineShape.Vertices.RemoveAt(i);
                        returnFeature = new Feature(targetMultilineShape.GetWellKnownBinary(), multilineFeature.Id, multilineFeature.ColumnValues);
                        break;
                    }
                }
            }

            return returnFeature;
        }

        private Feature RemoveVertexToLineFeature(Feature lineFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();
            RectangleShape searchingArea = new RectangleShape(targetPointShape.X - searchingTolerance, targetPointShape.Y + searchingTolerance, targetPointShape.X + searchingTolerance, targetPointShape.Y - searchingTolerance);
            LineShape targetLineShape = lineFeature.GetShape() as LineShape;

            for (int i = 0; i <= targetLineShape.Vertices.Count - 1; i++)
            {
                if (searchingArea.Contains(targetPointShape) && searchingArea.Contains(new PointShape(targetLineShape.Vertices[i].X, targetLineShape.Vertices[i].Y)) && targetLineShape.Vertices.Count > 2)
                {
                    targetLineShape.Vertices.RemoveAt(i);
                    returnFeature = new Feature(targetLineShape.GetWellKnownBinary(), lineFeature.Id, lineFeature.ColumnValues);
                    break;
                }
            }

            return returnFeature;
        }

        private void RemoveVertexCore(PointShape targetPointShape)
        {
            if (ReshapeControlPointsLayer.InternalFeatures.Count != 0)
            {
                double searchingTolerance = clickPointTolerance * Math.Max(ParentMap.CurrentExtent.Width / ParentMap.ActualWidth, ParentMap.CurrentExtent.Height / ParentMap.ActualHeight);
                foreach (string key in EditShapesLayer.InternalFeatures.GetKeys())
                {
                    Feature currentFeature = RemoveVertex(EditShapesLayer.InternalFeatures[key], targetPointShape, searchingTolerance);
                    if (currentFeature.GetShape() != null)
                    {
                        editingFeature = currentFeature;
                        EditShapesLayer.InternalFeatures[key] = currentFeature;
                        break;
                    }
                }
            }
        }

        private Feature RemoveVertex(Feature targetFeature, PointShape targetPointShape, double searchingTolerance)
        {
            Feature returnFeature = new Feature();

            WellKnownType wellKnowType = targetFeature.GetWellKnownType();
            switch (wellKnowType)
            {
                case WellKnownType.Line:
                    returnFeature = RemoveVertexToLineFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multiline:
                    returnFeature = RemoveVertexToMultilineFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Polygon:
                    returnFeature = RemoveVertexFromPolygonFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multipolygon:
                    returnFeature = RemoveVertexFromMultiPolygonFeature(targetFeature, targetPointShape, searchingTolerance);
                    break;

                case WellKnownType.Multipoint:
                case WellKnownType.Point:
                case WellKnownType.Invalid:
                default:
                    break;
            }
            return returnFeature;
        }

        private string GetControlFeatureId(Feature controlFeature)
        {
            return controlFeature != null ? controlFeature.Id : Guid.NewGuid().ToString();
        }

        private Feature RotateFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            PointShape centerPointShape = sourceFeature.GetShape().GetCenterPoint();
            float rotateAngle = GetRotatingAngle(targetControlPoint, sourceControlPoint, centerPointShape);

            BaseShape baseShape = BaseShape.Rotate(sourceFeature, centerPointShape, rotateAngle);
            Feature returnFeature = new Feature(baseShape.GetWellKnownBinary(), editingFeature.Id, sourceFeature.ColumnValues);
            return returnFeature;
        }

        private EditSnapshot SaveSnapshot()
        {
            EditSnapshot snapshot = new EditSnapshot
            {
                ControlFeature = controlFeature,
                CurrentEditMode = currentEditMode,
                EditingFeature = editingFeature,
                OriginalPosition = originalPosition
            };
            snapshot.InternalFeatures.LoadFrom<Feature>(EditShapesLayer.InternalFeatures);
            snapshot.UnselectedFeatures.LoadFrom<Feature>(EditCandidatesLayer.InternalFeatures);
            snapshot.ReshapeFeatures.LoadFrom<Feature>(ReshapeControlPointsLayer.InternalFeatures);
            return snapshot;
        }

        private void LoadSnapshot(EditSnapshot snapshoot)
        {
            controlFeature = snapshoot.ControlFeature;
            currentEditMode = snapshoot.CurrentEditMode;
            editingFeature = snapshoot.EditingFeature;
            originalPosition = snapshoot.OriginalPosition;
            EditShapesLayer.InternalFeatures.LoadFrom<Feature>(snapshoot.InternalFeatures);
            EditShapesLayer.BuildIndex();
            EditCandidatesLayer.InternalFeatures.LoadFrom<Feature>(snapshoot.UnselectedFeatures);
            EditCandidatesLayer.BuildIndex();
            ReshapeControlPointsLayer.InternalFeatures.LoadFrom<Feature>(snapshoot.ReshapeFeatures);
            AssociateControlPointsLayer.InternalFeatures.LoadFrom<Feature>(snapshoot.AssociateFeatures);
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && EditShapesLayer != null && EditShapesLayer.InternalFeatures.Count > 0)
            {
                Feature reshapeControlPoint = reshapeControlPointsLayer.InternalFeatures.FirstOrDefault(f => f.ColumnValues.ContainsKey(existingFeatureColumnName) && f.ColumnValues[existingFeatureColumnName] == existingFeatureColumnValue);
                if (reshapeControlPoint != null && reshapeControlPoint.GetShape() != null)
                {
                    bool isSuccess = false;
                    byte[] wkb = RemoveOnePointFromMultipointFeature((PointShape)reshapeControlPoint.GetShape(), ref isSuccess);
                    if (isSuccess)
                    {
                        UpdateFeature(wkb);
                        CalculateVertexControlPoints();
                        TakeSnapshot();
                        Refresh();
                    }
                }
                else
                {
                    RemoveFeatures();
                }
            }
        }

        private static float GetRotatingAngle(PointShape currentPosition, PointShape referencePointShape, PointShape centerPointShape)
        {
            float resultAngle;

            double angle0 = Math.Atan2(referencePointShape.Y - centerPointShape.Y, referencePointShape.X - centerPointShape.X);
            double angle1 = Math.Atan2(currentPosition.Y - centerPointShape.Y, currentPosition.X - centerPointShape.X);
            double angle = angle1 - angle0;
            angle = angle * 180 / Math.PI;
            angle = angle - Math.Floor(angle / 360) * 360;
            if (angle < 0) { angle = angle + 360; }

            resultAngle = (float)angle;
            return resultAngle;
        }

        private static double GetDistance(PointShape fromPoint, PointShape toPoint)
        {
            double horizenDistance = Math.Abs((fromPoint.X - toPoint.X));
            double verticalDistance = Math.Abs((fromPoint.Y - toPoint.Y));
            return Math.Sqrt(Math.Pow(horizenDistance, 2) + Math.Pow(verticalDistance, 2));
        }

        //#region ISwitchable
        //public bool IsActive
        //{
        //    get { return IsEnabled; }
        //}
        //public void Disactive()
        //{
        //    IsEnabled = false;
        //}
        //#endregion ISwitchable
    }
}