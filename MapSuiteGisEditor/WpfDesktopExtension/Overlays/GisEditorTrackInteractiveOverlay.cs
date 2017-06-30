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
using System.Windows.Forms;
using System.Windows.Media;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class GisEditorTrackInteractiveOverlay : TrackInteractiveOverlay
    {
        private const string inTrackingFeatureKey = "InTrackingFeature";

        #region Boundary Adjustment

        [NonSerialized]
        private bool isShiftKeyDown;
        [NonSerialized]
        private bool isDirty;
        [NonSerialized]
        private bool needAddVertex;
        [NonSerialized]
        private double snappingDistance;
        [NonSerialized]
        private GisEditorEditInteractiveOverlay editOverlay;

        [NonSerialized]
        private Collection<Vertex> boundaryVertices;
        [NonSerialized]
        private List<Vertex> searchingVertices;
        [NonSerialized]
        private Collection<FeatureLayer> snappingLayers;
        [NonSerialized]
        private GeographyUnit geographyUnit;
        [NonSerialized]
        private SnappingDistanceUnit snappingDistanceUnit;
        [NonSerialized]
        private PointShape firstPoint;

        public GisEditorEditInteractiveOverlay EditOverlay
        {
            get { return editOverlay; }
            set { editOverlay = value; }
        }

        public SnappingDistanceUnit SnappingDistanceUnit
        {
            get { return snappingDistanceUnit; }
            set { snappingDistanceUnit = value; }
        }

        public double SnappingDistance
        {
            get { return snappingDistance; }
            set { snappingDistance = value; }
        }

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        public Collection<FeatureLayer> SnappingLayers
        {
            get { return snappingLayers; }
            set { snappingLayers = value; }
        }

        public GeographyUnit GeographyUnit
        {
            get { return geographyUnit; }
            set { geographyUnit = value; }
        }

        #endregion

        public GisEditorTrackInteractiveOverlay()
        {
            PolygonTrackMode = PolygonTrackMode.LineOnly;

            #region Boundary Adjustment
            searchingVertices = new List<Vertex>();
            boundaryVertices = new Collection<Vertex>();
            snappingLayers = new Collection<FeatureLayer>();
            isShiftKeyDown = false;
            needAddVertex = true;
            #endregion
        }

        #region Boudary Adjustment

        protected override InteractiveResult MouseDoubleClickCore(InteractionArguments interactionArguments)
        {
            if (isShiftKeyDown)
                AddSnapedVertex(interactionArguments.CurrentExtent, true);

            var interactionResult = base.MouseDoubleClickCore(interactionArguments);
            return interactionResult;
        }

        #endregion

        //protected override void OnTrackStarted(TrackStartedTrackInteractiveOverlayEventArgs e)
        //{
        //    base.OnTrackStarted(e);

        //    if (!isShiftKeyDown)
        //    {
        //        PointShape snappedPoint = GetSnappingPoint(e.StartedVertex, new Feature());
        //        if (snappedPoint != null) e.StartedVertex = new Vertex(snappedPoint);
        //    }
        //}

        protected override void OnVertexAdding(VertexAddingTrackInteractiveOverlayEventArgs e)
        {
            base.OnVertexAdding(e);

            if (MouseDownCount == 1)
            {
                if (firstPoint == null) firstPoint = GetSnappingPoint(e.AddingVertex, e.AffectedFeature, false);
                //if (firstPoint != null) e.AddingVertex = new Vertex(firstPoint);

            }
            else
            {
                PointShape snappedPoint = GetSnappingPoint(e.AddingVertex, e.AffectedFeature, false);
                if (snappedPoint != null) e.AddingVertex = new Vertex(snappedPoint);
                firstPoint = null;
            }
        }

        protected override void OnMouseMoved(MouseMovedTrackInteractiveOverlayEventArgs e)
        {
            base.OnMouseMoved(e);

            var trackShape = GetTrackingShape();

            if (!isShiftKeyDown && trackShape != null && (TrackMode == TrackMode.Polygon || TrackMode == TrackMode.Line)
                && editOverlay != null && editOverlay.SnappingLayers.Count > 0)
            {
                lock (OverlayCanvas.Children)
                {
                    var snappingCircle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                    if (snappingCircle == null)
                    {
                        snappingCircle = new System.Windows.Shapes.Ellipse();
                        snappingCircle.IsHitTestVisible = false;
                        snappingCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        snappingCircle.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        snappingCircle.Stroke = new SolidColorBrush(Colors.Black);
                        snappingCircle.StrokeThickness = 1;
                        OverlayCanvas.Children.Add(snappingCircle);
                    }

                    var snappingDistance = editOverlay.SnappingDistance;
                    var snappingDistanceUnit = editOverlay.SnappingDistanceUnit;
                    var snappingScreenPoint = ExtentHelper.ToScreenCoordinate(MapArguments.CurrentExtent, e.MovedVertex.X, e.MovedVertex.Y, (float)MapArguments.ActualWidth, (float)MapArguments.ActualHeight);

                    try
                    {
                        SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, e.MovedVertex);
                        var snappingArea = new PointShape(e.MovedVertex.X, e.MovedVertex.Y)
                            .Buffer(calc.Distance, editOverlay.MapArguments.MapUnit, calc.DistanceUnit)
                            .GetBoundingBox();

                        var snappingScreenSize = Math.Max(snappingArea.Width, snappingArea.Height) / MapArguments.CurrentResolution;
                        snappingCircle.Width = snappingScreenSize;
                        snappingCircle.Height = snappingScreenSize;
                        snappingCircle.Margin = new System.Windows.Thickness(snappingScreenPoint.X - snappingScreenSize * .5, snappingScreenPoint.Y - snappingScreenSize * .5, 0, 0);
                    }
                    catch
                    { }
                }

                //PointShape snappedPoint = GetSnappingPoint(e.MovedVertex, e.AffectedFeature);
                //if (snappedPoint != null)
                //{
                //    e.MovedVertex = new Vertex(snappedPoint);
                //    lock (OverlayCanvas.Children)
                //    {
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

                //        var snappingDistance = editOverlay.SnappingDistance;
                //        var snappingDistanceUnit = editOverlay.SnappingDistanceUnit;
                //        var snappingScreenPoint = ExtentHelper.ToScreenCoordinate(MapArguments.CurrentExtent, snappedPoint, (float)MapArguments.ActualWidth, (float)MapArguments.ActualHeight);

                //        SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, e.MovedVertex);
                //        var snappingArea = snappedPoint.Buffer(calc.Distance, editOverlay.MapArguments.MapUnit, calc.DistanceUnit).GetBoundingBox();
                //        var snappingScreenSize = Math.Max(snappingArea.Width, snappingArea.Height) / MapArguments.CurrentResolution;
                //        snappingCircle.Width = snappingScreenSize;
                //        snappingCircle.Height = snappingScreenSize;
                //        snappingCircle.Margin = new System.Windows.Thickness(snappingScreenPoint.X - snappingScreenSize * .5, snappingScreenPoint.Y - snappingScreenSize * .5, 0, 0);
                //    }
                //}
            }
        }

        protected override void OnTrackEnded(TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            base.OnTrackEnded(e);
            Vertices.Clear();

            #region Boudary Adjustment

            if (isShiftKeyDown) MouseDownCount = 0;

            #endregion

            OverlayCanvas.Children.Clear();
        }

        protected override InteractiveResult KeyDownCore(KeyEventInteractionArguments interactionArguments)
        {
            #region Boudary Adjustment

            isShiftKeyDown = interactionArguments.IsShiftKeyPressed;

            #endregion

            var result = base.KeyDownCore(interactionArguments);

            if (interactionArguments.Key.Equals(Keys.Escape.ToString(), StringComparison.Ordinal))
            {
                MouseDownCount = 0;
                Vertices.Clear();
                TrackShapeLayer.InternalFeatures.Clear();
                TrackShapesInProcessLayer.InternalFeatures.Clear();
                result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
            }

            return result;
        }

        #region Boudary Adjustment

        protected override InteractiveResult KeyUpCore(KeyEventInteractionArguments interactionArguments)
        {
            isShiftKeyDown = interactionArguments.IsShiftKeyPressed;
            return base.KeyUpCore(interactionArguments);
        }

        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            if (isShiftKeyDown)
            {
                var circle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                if (circle != null)
                    OverlayCanvas.Children.Remove(circle);
            }

            if (IsDirty && TrackMode != TrackMode.None)
            {
                CollectionVertices(interactionArguments);
            }

            UpdateArguments(interactionArguments);

            if (TrackMode != TrackMode.None
              && MouseDownCount < 1
              && SnappingLayers.Count > 0)
            {
                lock (OverlayCanvas.Children)
                {
                    Vertex currentPosition = new Vertex(interactionArguments.WorldX, interactionArguments.WorldY);

                    var snappingCircle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                    if (snappingCircle == null)
                    {
                        snappingCircle = new System.Windows.Shapes.Ellipse();
                        snappingCircle.IsHitTestVisible = false;
                        snappingCircle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        snappingCircle.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        snappingCircle.Stroke = new SolidColorBrush(Colors.Black);
                        snappingCircle.StrokeThickness = 1;
                        OverlayCanvas.Children.Add(snappingCircle);
                    }

                    var snappingDistance = SnappingDistance;
                    var snappingDistanceUnit = SnappingDistanceUnit;
                    var snappingScreenPoint = ExtentHelper.ToScreenCoordinate(MapArguments.CurrentExtent, currentPosition.X, currentPosition.Y, (float)MapArguments.ActualWidth, (float)MapArguments.ActualHeight);

                    try
                    {
                        SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, currentPosition);
                        var snappingArea = new PointShape(currentPosition.X, currentPosition.Y)
                            .Buffer(calc.Distance, MapArguments.MapUnit, calc.DistanceUnit)
                            .GetBoundingBox();

                        var snappingScreenSize = Math.Max(snappingArea.Width, snappingArea.Height) / MapArguments.CurrentResolution;
                        snappingCircle.Width = snappingScreenSize;
                        snappingCircle.Height = snappingScreenSize;
                        snappingCircle.Margin = new System.Windows.Thickness(snappingScreenPoint.X - snappingScreenSize * .5, snappingScreenPoint.Y - snappingScreenSize * .5, 0, 0);
                    }
                    catch
                    { }
                }
            }
            else
            {
                lock (OverlayCanvas.Children)
                {
                    var circle = OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                    if (circle != null)
                        OverlayCanvas.Children.Remove(circle);
                }
            }


            var interactiveResult = base.MouseMoveCore(interactionArguments);
            return interactiveResult;
        }

        protected override void OnVertexAdded(VertexAddedTrackInteractiveOverlayEventArgs e)
        {
            if (isShiftKeyDown)
                AddSnapedVertex(MapArguments.CurrentExtent);
            base.OnVertexAdded(e);
        }

        #endregion

        protected override InteractiveResult MouseDownCore(InteractionArguments interactionArguments)
        {
            #region Boudary Adjustment

            //UpdateArguments(interactionArguments);

            if (isShiftKeyDown && Vertices.Count == 0)
            {
                TrackShapeLayer.Open();
                TrackShapeLayer.Clear();
            }

            #endregion

            InteractiveResult interactiveResult = base.MouseDownCore(interactionArguments);

            if (interactionArguments.MouseButton == MapMouseButton.Right && TrackMode != TrackMode.None)
            {
                interactiveResult.DrawThisOverlay = InteractiveOverlayDrawType.DoNotDraw;
                interactiveResult = MouseClickCore(interactionArguments);
                interactiveResult.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
            }

            return interactiveResult;
        }

        public void RemoveLastVertex()
        {
            if (TrackMode == TrackMode.Line)
            {
                if (Vertices.Count < 3)
                {
                    CleanTrackingShapes();
                }
                else
                {
                    Vertices.RemoveAt(Vertices.Count - 2);
                    MouseDownCount--;
                }
            }
            else if (TrackMode == TrackMode.Freehand || TrackMode == TrackMode.Polygon)
            {
                if (Vertices.Count < 5)
                {
                    CleanTrackingShapes();
                }
                else
                {
                    Vertices.RemoveAt(Vertices.Count - 3);
                    MouseDownCount--;
                }
            }

            DrawCore(MapArguments.CurrentExtent, OverlayRefreshType.Redraw);
        }

        protected override InteractiveResult MouseClickCore(InteractionArguments interactionArguments)
        {
            UpdateArguments(interactionArguments);
            InteractiveResult result = base.MouseClickCore(interactionArguments);

            return result;
        }

        private void CleanTrackingShapes()
        {
            Vertices.Clear();
            TrackShapesInProcessLayer.InternalFeatures.Clear();
            int index = TrackShapeLayer.InternalFeatures.Count - 1;
            if (index >= 0 && TrackShapeLayer.InternalFeatures.Count > index)
            {
                TrackShapeLayer.InternalFeatures.RemoveAt(index);
            }
            MouseDownCount = 0;
            EndTracking();
        }

        private PointShape GetSnappingPoint(Vertex originVertex, Feature affectedFeature, bool preventSameVertex = true)
        {
            PolygonShape affectedPolygon = affectedFeature.GetShape() as PolygonShape;
            Collection<PointShape> tmpSnappingPoints = new Collection<PointShape>();

            if (editOverlay != null && snappingLayers.Count != 0)
            {
                //we only snaps to selected features when the selected features are from the EditTargetLayer
                IEnumerable<FeatureLayer> currentSnappingLayers = snappingLayers;
                if (editOverlay.SnappingLayers.Any(snappingLayer => snappingLayer == editOverlay.EditTargetLayer))
                {
                    currentSnappingLayers = currentSnappingLayers.Concat(new FeatureLayer[] { editOverlay.EditShapesLayer });
                }

                foreach (var snappingLayer in currentSnappingLayers)
                {
                    lock (snappingLayer)
                    {
                        if (!snappingLayer.IsOpen)
                        {
                            snappingLayer.Open();
                        }

                        var boundingBox = MapArguments.CurrentExtent;
                        var screenWidth = MapArguments.ActualWidth;
                        Feature snappingFeature = null;
                        try
                        {
                            SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, originVertex);
                            snappingFeature = snappingLayer.QueryTools.
                                GetFeaturesWithinDistanceOf(new Feature(originVertex), MapArguments.MapUnit, calc.DistanceUnit, calc.Distance, ReturningColumnsType.NoColumns).FirstOrDefault(f => f.Id != affectedFeature.Id);
                        }
                        catch { }
                        BaseShape snappingShape = null;
                        if (snappingFeature != null && (snappingShape = snappingFeature.GetShape()) != null)
                        {
                            SnappingAdapter calc = SnappingAdapter.Convert(snappingDistance, snappingDistanceUnit, MapArguments, originVertex);
                            PointShape snappingPoint = GisEditorEditInteractiveOverlay.GetSnappingPoint(snappingShape, new PointShape(originVertex), MapArguments.MapUnit, calc.Distance, calc.DistanceUnit);

                            if (preventSameVertex && (affectedPolygon != null && !affectedPolygon.OuterRing.Vertices.Any(v => v.X == snappingPoint.X && v.Y == snappingPoint.Y)))
                            {
                                return snappingPoint;
                            }
                            else
                            {
                                return snappingPoint;
                            }
                        }
                    }
                }
            }

            PointShape originPoint = new PointShape(originVertex);

            return originPoint;
        }

        #region Boudary Adjustment

        private void AddSnapedVertex(RectangleShape currentExtent, bool needRemoveVertex = false)
        {
            if (TrackShapeLayer.InternalFeatures.Count > 0)
            {
                var trackShape = TrackShapeLayer.InternalFeatures.Last().GetShape();

                if (TrackMode == TrackMode.Polygon && trackShape is PolygonShape && Vertices.Count > 3)
                {
                    if (!needAddVertex)
                    {
                        needAddVertex = true;
                        return;
                    }

                    var trackPolygon = trackShape as PolygonShape;
                    var vertex1 = trackPolygon.OuterRing.Vertices[trackPolygon.OuterRing.Vertices.Count - 3];
                    var vertex2 = trackPolygon.OuterRing.Vertices[trackPolygon.OuterRing.Vertices.Count - 2];

                    GenerateResultShapeByVertex(currentExtent, vertex1, vertex2, needRemoveVertex);
                }
                else if (TrackMode == TrackMode.Line && trackShape is LineShape
                    && (Vertices.Count > 2 || (Vertices.Count > 1 && needRemoveVertex)))
                {
                    var trackLine = trackShape as LineShape;
                    var vertex1 = trackLine.Vertices[trackLine.Vertices.Count - 2];
                    var vertex2 = trackLine.Vertices[trackLine.Vertices.Count - 1];
                    if (vertex1 != vertex2)
                    {
                        GenerateResultLineByVertex(currentExtent, vertex1, vertex2, needRemoveVertex);
                    }
                    else
                    {
                        boundaryVertices.Clear();
                        boundaryVertices.Add(Vertices.First());
                        boundaryVertices.Add(Vertices.Last());
                    }
                }
            }
        }

        private void GenerateResultLineByVertex(RectangleShape currentExtent, Vertex vertex1, Vertex vertex2, bool needRemoveVertex = false)
        {
            var snappingLayer = snappingLayers.FirstOrDefault();
            if (snappingLayer != null)
            {
                snappingLayer.Open();

                Collection<Feature> features = snappingLayer.QueryTools.GetFeaturesInsideBoundingBox(currentExtent, ReturningColumnsType.NoColumns);

                bool found = false;

                foreach (var item in features)
                {
                    BaseShape shape = item.GetShape();

                    var multipolygon = shape as MultipolygonShape;
                    var polygonShape = shape as PolygonShape;
                    var multiline = shape as MultilineShape;

                    if (multipolygon != null)
                    {
                        foreach (var p in multipolygon.Polygons)
                        {
                            if (p.OuterRing.Vertices.Contains(vertex1) && p.OuterRing.Vertices.Contains(vertex2))
                            {
                                GenerateResultLine(vertex1, vertex2, p.OuterRing.Vertices, needRemoveVertex);
                                found = true;
                                break;
                            }
                        }

                        if (found) break;
                    }
                    else if (polygonShape != null)
                    {
                        if (polygonShape.OuterRing.Vertices.Contains(vertex1) && polygonShape.OuterRing.Vertices.Contains(vertex2))
                        {
                            GenerateResultLine(vertex1, vertex2, polygonShape.OuterRing.Vertices, needRemoveVertex);
                            found = true;
                            break;
                        }
                    }
                    else if (multiline != null)
                    {
                        foreach (var l in multiline.Lines)
                        {
                            if (l.Vertices.Contains(vertex1) && l.Vertices.Contains(vertex2))
                            {
                                GenerateResultLine(vertex1, vertex2, l.Vertices, needRemoveVertex);
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void GenerateResultLine(Vertex vertex1, Vertex vertex2, Collection<Vertex> vertices, bool needRemoveVertex = false)
        {
            LineShape line = new LineShape(vertices);
            try
            {
                LineShape resultLine = GetLineOnALine(line, vertex1, vertex2, GeographyUnit);
                if (resultLine != null && resultLine.Vertices.Count > 0)
                {
                    if (!IsEqual(resultLine.Vertices[0].X, vertex1.X) || !IsEqual(resultLine.Vertices[0].Y, vertex1.Y))
                    {
                        resultLine.ReversePoints();
                    }

                    foreach (var insertVertex in resultLine.Vertices)
                    {
                        int count = Vertices.Count;

                        Vertices.Insert(count - 2, insertVertex);
                        MouseDownCount++;
                    }

                    if (needRemoveVertex)
                    {
                        Vertices.RemoveAt(Vertices.Count - 2);
                        Vertices.RemoveAt(Vertices.Count - 1);
                        MouseDownCount -= 2;

                        boundaryVertices.Clear();
                        boundaryVertices.Add(Vertices.First());
                        boundaryVertices.Add(Vertices.Last());
                    }
                }
            }
            catch
            { }
        }

        private void UpdateArguments(InteractionArguments interactionArguments)
        {
            if (TrackMode != TrackMode.None)
            {
                PointShape point = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);

                if (searchingVertices.Count < 1) return;

                try
                {
                    Vertex vertex = GetClosestVertex(searchingVertices, point);

                    interactionArguments.WorldX = vertex.X;
                    interactionArguments.WorldY = vertex.Y;

                    ScreenPointF screen = ExtentHelper.ToScreenCoordinate(interactionArguments.CurrentExtent, new PointShape(vertex), interactionArguments.MapWidth, interactionArguments.MapHeight);
                    interactionArguments.ScreenX = screen.X;
                    interactionArguments.ScreenY = screen.Y;
                }
                catch
                { }
            }
        }

        private Vertex GetClosestVertex(List<Vertex> vertices, PointShape point)
        {
            Vertex result = vertices.FirstOrDefault();
            if (vertices.Count > 0)
            {
                result = vertices.OrderBy(v => (v.X - point.X) * (v.X - point.X) + (v.Y - point.Y) * (v.Y - point.Y)).First();
            }

            if (result != default(Vertex))
            {
                double distanceInMeter = point.GetDistanceTo(new PointShape(result), MapArguments.MapUnit, DistanceUnit.Meter);
                SnappingAdapter calc = SnappingAdapter.Convert(SnappingDistance, SnappingDistanceUnit, MapArguments, point);
                double radiusInMeter = Conversion.ConvertMeasureUnits(calc.Distance, calc.DistanceUnit, DistanceUnit.Meter);
                if (distanceInMeter > radiusInMeter)
                {
                    result = new Vertex(point);
                }
            }

            return result;
        }

        private void CollectionVertices(InteractionArguments interactionArguments)
        {
            searchingVertices = new List<Vertex>();
            var snappingLayer = snappingLayers.FirstOrDefault();
            if (snappingLayer != null)
            {
                snappingLayer.Open();

                Collection<Feature> features = snappingLayer.QueryTools.GetFeaturesInsideBoundingBox(interactionArguments.CurrentExtent, ReturningColumnsType.NoColumns);
                foreach (var item in features)
                {
                    BaseShape shape = item.GetShape();

                    if (shape is PointShape)
                    {
                        searchingVertices.Add(new Vertex((PointShape)shape));
                    }
                    else if (shape is MultipointShape)
                    {
                        searchingVertices.AddRange(((MultipointShape)shape).Points.Select(p => new Vertex(p)));
                    }
                    else if (shape is PolygonShape)
                    {
                        searchingVertices.AddRange((shape as PolygonShape).OuterRing.Vertices);
                    }
                    else if (shape is MultipolygonShape)
                    {
                        foreach (var polygon in (shape as MultipolygonShape).Polygons)
                        {
                            searchingVertices.AddRange(polygon.OuterRing.Vertices);
                        }
                    }
                    else if (shape is LineShape)
                    {
                        searchingVertices.AddRange(((LineShape)shape).Vertices);
                    }
                    else if (shape is MultilineShape)
                    {
                        foreach (var line in (shape as MultilineShape).Lines)
                        {
                            searchingVertices.AddRange(line.Vertices);
                        }
                    }
                }

                isDirty = false;
            }
        }

        private List<Vertex> GetAllVertices(Collection<Vertex> vertices)
        {
            List<Vertex> allVertices = new List<Vertex>(vertices);
            for (int i = 0; i < vertices.Count; i++)
            {
                if (i != vertices.Count - 1)
                {
                    LineShape lineShape = new LineShape();
                    lineShape.Vertices.Add(vertices[i]);
                    lineShape.Vertices.Add(vertices[i + 1]);
                    PointShape lineCenterPoint = lineShape.GetCenterPoint();
                    allVertices.Add(new Vertex(lineCenterPoint.X, lineCenterPoint.Y));
                }
            }
            return allVertices;
        }

        private static bool IsEqual(double sourceValue, double targetValue)
        {
            bool returnValue = false;
            double decision = Math.Pow(10, -6);

            if (sourceValue != 0 && targetValue != 0)
            {
                double tempSource = Math.Abs(1 - sourceValue / targetValue);
                double tempTarget = Math.Abs(1 - targetValue / sourceValue);

                if (Math.Abs(tempSource) <= decision && Math.Abs(tempTarget) <= decision)
                {
                    returnValue = true;
                }
            }
            else
            {
                double absValue = Math.Abs(sourceValue - targetValue);
                if (absValue < decision)
                {
                    returnValue = true;
                }
            }
            return returnValue;
        }

        private void AddCustomVertex(Vertex trackVertex)
        {
            Feature currentFeature = GetCurrentFeature();
            VertexAddingTrackInteractiveOverlayEventArgs vertexAddingTrackInteractiveOverlayEventArgs = new VertexAddingTrackInteractiveOverlayEventArgs(trackVertex, currentFeature, false);
            OnVertexAdding(vertexAddingTrackInteractiveOverlayEventArgs);
            if (vertexAddingTrackInteractiveOverlayEventArgs.Cancel)
            {
                return;
            }
            Vertices.Add(vertexAddingTrackInteractiveOverlayEventArgs.AddingVertex);
            currentFeature = GetCurrentFeature();
            OnVertexAdded(new VertexAddedTrackInteractiveOverlayEventArgs(vertexAddingTrackInteractiveOverlayEventArgs.AddingVertex, currentFeature));
            if (boundaryVertices.Count > 0)
            {
                if (Vertices.Count == 1)
                {
                    Vertices.Add(boundaryVertices.First());
                    Vertices.Add(trackVertex);
                    Vertices.Add(boundaryVertices.Last());

                    OnTrackStarted(new TrackStartedTrackInteractiveOverlayEventArgs(trackVertex));
                }
                else
                {
                    Vertices.RemoveAt(Vertices.Count - 2);
                    Vertices.Add(trackVertex);
                    Vertices.Add(boundaryVertices.Last());
                }
            }
        }

        private Feature GetCurrentFeature()
        {
            Feature currentFeature;

            switch (TrackMode)
            {
                case TrackMode.Custom:
                    if (Vertices.Count >= 2)
                    {
                        BaseShape baseShape = GetTrackingShape();
                        if (baseShape != null)
                        {
                            currentFeature = new Feature(baseShape);
                        }
                        else
                        {
                            currentFeature = new Feature();
                        }
                    }
                    else
                    {
                        currentFeature = new Feature();
                    }
                    break;
                default:
                    currentFeature = new Feature();
                    break;
            }

            return currentFeature;
        }

        private void GenerateResultShape(Vertex vertex1, Vertex vertex2, Collection<Vertex> vertices, bool needRemoveVertex = false)
        {
            LineShape line = new LineShape(vertices);
            try
            {
                LineShape resultLine = GetLineOnALine(line, vertex1, vertex2, GeographyUnit);
                if (resultLine != null && resultLine.Vertices.Count > 0)
                {
                    if (!IsEqual(resultLine.Vertices[0].X, vertex1.X) || !IsEqual(resultLine.Vertices[0].Y, vertex1.Y))
                    {
                        resultLine.ReversePoints();
                    }

                    var tempVertex = resultLine.Vertices.ToList();
                    if (Vertices.Count == 4)
                    {
                        tempVertex.Reverse();

                        if (needRemoveVertex) needAddVertex = false;
                    }

                    foreach (var insertVertex in tempVertex)
                    {
                        int count = Vertices.Count;

                        Vertices.Insert(count - 3, insertVertex);
                        MouseDownCount++;
                    }

                    if (needRemoveVertex)
                    {
                        Vertices.RemoveAt(Vertices.Count - 3);
                        Vertices.RemoveAt(Vertices.Count - 1);
                        MouseDownCount -= 2;
                    }
                }
            }
            catch
            { }
        }

        private void GenerateResultShapeByVertex(RectangleShape currentExtent, Vertex vertex1, Vertex vertex2, bool needRemoveVertex = false)
        {
            var snappingLayer = snappingLayers.FirstOrDefault();
            if (snappingLayer != null)
            {
                snappingLayer.Open();
                Collection<Feature> features = snappingLayer.QueryTools.GetFeaturesInsideBoundingBox(currentExtent, ReturningColumnsType.NoColumns);

                bool found = false;

                foreach (var item in features)
                {
                    BaseShape shape = item.GetShape();

                    if (shape is PolygonShape)
                    {
                        PolygonShape polygon = shape as PolygonShape;

                        if (polygon.OuterRing.Vertices.Contains(vertex1) && polygon.OuterRing.Vertices.Contains(vertex2))
                        {
                            GenerateResultShape(vertex1, vertex2, polygon.OuterRing.Vertices, needRemoveVertex);
                            break;
                        }
                    }
                    else if (shape is MultipolygonShape)
                    {
                        MultipolygonShape multipolygon = shape as MultipolygonShape;

                        if (multipolygon != null)
                        {
                            foreach (var p in multipolygon.Polygons)
                            {
                                if (p.OuterRing.Vertices.Contains(vertex1) && p.OuterRing.Vertices.Contains(vertex2))
                                {
                                    GenerateResultShape(vertex1, vertex2, p.OuterRing.Vertices, needRemoveVertex);
                                    found = true;
                                    break;
                                }
                            }

                            if (found) break;
                        }
                    }
                }
            }
        }

        private LineShape GetLineOnALine(LineShape line, Vertex vertex1, Vertex vertex2, GeographyUnit unit)
        {
            LineShape result1 = new LineShape();
            LineShape result2 = new LineShape();

            int index1 = line.Vertices.IndexOf(vertex1);
            int index2 = line.Vertices.IndexOf(vertex2);

            if (index1 < index2)
            {
                for (int i = index1; i <= index2; i++)
                {
                    result1.Vertices.Add(line.Vertices[i]);
                }

                for (int i = index1; i >= 0; i--)
                {
                    result2.Vertices.Add(line.Vertices[i]);
                }

                for (int i = line.Vertices.Count - 2; i >= index2; i--)
                {
                    result2.Vertices.Add(line.Vertices[i]);
                }
            }
            else
            {
                for (int i = index2; i <= index1; i++)
                {
                    result1.Vertices.Add(line.Vertices[i]);
                }

                for (int i = index2; i >= 0; i--)
                {
                    result2.Vertices.Add(line.Vertices[i]);
                }

                for (int i = line.Vertices.Count - 2; i >= index1; i--)
                {
                    result2.Vertices.Add(line.Vertices[i]);
                }
            }

            double length1 = result1.GetLength(unit, DistanceUnit.Meter);
            double length2 = result2.GetLength(unit, DistanceUnit.Meter);

            return length1 > length2 ? result2 : result1;
        }

        private void UpdateCustomVertex(int index, Vertex trackVertex)
        {
            Vertices[index] = trackVertex;
            Feature currentFeature = GetCurrentFeature();
            OnMouseMoved(new MouseMovedTrackInteractiveOverlayEventArgs(trackVertex, currentFeature));
        }

        #endregion
    }
}