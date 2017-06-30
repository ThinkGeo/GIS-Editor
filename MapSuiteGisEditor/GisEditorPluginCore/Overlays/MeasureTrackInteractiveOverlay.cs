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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MeasureTrackInteractiveOverlay : TrackInteractiveOverlay
    {
        private const string valueStyleColumnName = "Value_Style_Key";
        private const string measureResultColumnName = "Result";
        private const string memoColumnName = "Memo";
        private static readonly string nameTemplate = GisEditor.LanguageManager.GetStringResource("MeasureTrackInteractiveOverlayMeasurementText");

        public event EventHandler<EventArgs> FeatureDragged;

        public event EventHandler<EventArgs> FeatureDragging;

        private Feature selectedMeasure;

        private Feature selectPoint;

        [NonSerialized]
        private const string valueStyleColumnValue = "default";

        [NonSerialized]
        private StateController<Collection<MapShape>> stateController;

        [Obfuscation(Exclude = true)]
        private static DistanceUnit distanceUnit;

        [Obfuscation(Exclude = true)]
        private static AreaUnit areaUnit;

        [Obfuscation(Exclude = true)]
        private GisEditorWpfMap parentMap;

        [Obfuscation(Exclude = true)]
        private MapShapeLayer shapeLayer;

        [NonSerialized]
        private TextBlock textBlock;

        [NonSerialized]
        private MeasuringInMode measuringMode;

        [NonSerialized]
        private MeasureCustomeMode measureCustomeMode;

        public MeasureTrackInteractiveOverlay()
        {
            measuringMode = MeasuringInMode.DecimalDegree;
            measureCustomeMode = MeasureCustomeMode.Select;
            shapeLayer = new MapShapeLayer();
            textBlock = new TextBlock { Visibility = Visibility.Collapsed };
            OverlayCanvas.Children.Add(textBlock);
            PolygonTrackMode = PolygonTrackMode.LineOnly;
            RenderMode = RenderMode.DrawingVisual;
            InitializeColumns(TrackShapeLayer);
            InitializeColumns(TrackShapesInProcessLayer, false);

            SetStylesForInMemoryFeatureLayer(TrackShapeLayer);
            SetStylesForInMemoryFeatureLayer(TrackShapesInProcessLayer);
            stateController = new StateController<Collection<MapShape>>();
            stateController.Add(new Collection<MapShape>());
        }

        public static string AreaColumnName { get { return "Area"; } }

        public static string DistanceColumnName { get { return "Distance"; } }

        public static string UnitColumnName { get { return "Unit"; } }

        public bool CanRollback
        {
            get { return stateController.CanRollBack; }
        }

        public StateController<Collection<MapShape>> History
        {
            get { return stateController; }
        }

        public bool CanForward
        {
            get { return stateController.CanForward; }
        }

        public override bool IsEmpty
        {
            get { return false; }
        }

        public CompositeStyle MeasurementStyle
        {
            get { return MeasureSetting.Instance.MeasurementStyle; }
            set
            {
                MeasureSetting.Instance.MeasurementStyle = value;
                IconTextStyle textStyle = value.Styles.OfType<IconTextStyle>().FirstOrDefault();
                if (textStyle != null)
                {
                    textStyle.PolygonLabelingLocationMode = PolygonLabelingLocationMode.BoundingBoxCenter;
                    textStyle.SplineType = SplineType.StandardSplining;
                    textStyle.TextColumnName = measureResultColumnName;
                }
            }
        }

        public static DistanceUnit DistanceUnit
        {
            get { return distanceUnit; }
            set { distanceUnit = value; }
        }

        public static AreaUnit AreaUnit
        {
            get { return areaUnit; }
            set { areaUnit = value; }
        }

        public GisEditorWpfMap ParentMap
        {
            get { return parentMap; }
            set { parentMap = value; }
        }

        public MapShapeLayer ShapeLayer
        {
            get { return shapeLayer; }
        }

        public MeasuringInMode MeasuringMode
        {
            get { return measuringMode; }
            set { measuringMode = value; }
        }

        public MeasureCustomeMode MeasureCustomeMode
        {
            get { return measureCustomeMode; }
            set { measureCustomeMode = value; }
        }

        public void Rollback()
        {
            RollbackCore();
        }

        protected override InteractiveResult MouseDownCore(InteractionArguments interactionArguments)
        {
            if (TrackMode == TrackMode.Custom
                && measureCustomeMode == MeasureCustomeMode.Move)
            {
                selectedMeasure = GetDragFeatureByPoint(interactionArguments);

                if (selectedMeasure != null)
                {
                    TrackShapeLayer.InternalFeatures.Add(selectedMeasure);
                    shapeLayer.MapShapes.Remove(selectedMeasure.Id);
                }
            }
            return base.MouseDownCore(interactionArguments);
        }

        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            var arguments = base.MouseMoveCore(interactionArguments);

            if (TrackMode == TrackMode.Custom
                && measureCustomeMode == MeasureCustomeMode.Move
                && selectedMeasure != null
                && selectPoint != null)
            {
                var pointShape = selectPoint.GetShape() as PointShape;
                var drageFeature = DragFeature(selectedMeasure, pointShape, new PointShape(interactionArguments.WorldX, interactionArguments.WorldY));

                TrackShapeLayer.InternalFeatures.Clear();
                TrackShapeLayer.InternalFeatures.Add(drageFeature);

                arguments.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                arguments.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
            }

            if (Vertices.Count == 0)
            {
                textBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                Canvas.SetLeft(textBlock, interactionArguments.ScreenX);
                Canvas.SetTop(textBlock, interactionArguments.ScreenY);
            }
            return arguments;
        }

        protected override InteractiveResult MouseUpCore(InteractionArguments interactionArguments)
        {
            if (selectedMeasure != null)
            {
                Canvas.SetLeft(textBlock, interactionArguments.ScreenX);
                Canvas.SetTop(textBlock, interactionArguments.ScreenY);

                textBlock.Visibility = Visibility.Visible;

                MeasureLastFeatureInLayer();

                var feature = TrackShapeLayer.InternalFeatures.LastOrDefault();
                shapeLayer.MapShapes.Add(selectedMeasure.Id, GetMapShape(feature));
                TrackShapeLayer.InternalFeatures.Clear();

                selectedMeasure = null;
                selectPoint = null;

                TakeSnapshot();
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.OnTrackEndedDescription));
            }

            return base.MouseUpCore(interactionArguments);
        }

        protected virtual void RollbackCore()
        {
            if (CanRollback)
            {
                var mapShapes = stateController.RollBack();
                shapeLayer.MapShapes.Clear();
                foreach (var mapShape in mapShapes)
                {
                    shapeLayer.MapShapes[mapShape.Feature.Id] = mapShape;
                }
                Refresh();
            }
        }

        public void Forward()
        {
            ForwardCore();
        }

        protected virtual void ForwardCore()
        {
            if (CanForward)
            {
                var mapShapes = stateController.Forward();
                shapeLayer.MapShapes.Clear();
                foreach (var mapShape in mapShapes)
                {
                    shapeLayer.MapShapes[mapShape.Feature.Id] = mapShape;
                }
                Refresh();
            }
        }

        protected override void OnDrawing(DrawingOverlayEventArgs e)
        {
            MeasureLastFeatureInLayer();
            base.OnDrawing(e);
        }

        protected override InteractiveResult MouseClickCore(InteractionArguments interactionArguments)
        {
            if (TrackMode == TrackMode.Custom
                && measureCustomeMode != MeasureCustomeMode.Move)
            {
                SelectAFeature(interactionArguments);
            }
            return base.MouseClickCore(interactionArguments);
        }

        protected Feature DragFeature(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            return DragFeatureCore(sourceFeature, sourceControlPoint, targetControlPoint);
        }

        protected virtual Feature DragFeatureCore(Feature sourceFeature, PointShape sourceControlPoint, PointShape targetControlPoint)
        {
            FeatureDraggingEditInteractiveOverlayEventArgs featureDraggingEditInteractiveOverlayEventArgs = new FeatureDraggingEditInteractiveOverlayEventArgs(sourceFeature, false, sourceControlPoint, targetControlPoint);
            OnFeatureDragging(featureDraggingEditInteractiveOverlayEventArgs);

            if (featureDraggingEditInteractiveOverlayEventArgs.Cancel)
            {
                return sourceFeature;
            }

            double offsetDistanceX = targetControlPoint.X - sourceControlPoint.X;
            double offsetDistanceY = targetControlPoint.Y - sourceControlPoint.Y;

            BaseShape baseShape = BaseShape.TranslateByOffset(sourceFeature.GetShape(), offsetDistanceX, offsetDistanceY, GeographyUnit.Meter, DistanceUnit.Meter);
            baseShape.Id = sourceFeature.Id;

            Feature returnFeature = new Feature(baseShape, sourceFeature.ColumnValues);
            OnFeatureDragged(new FeatureDraggedEditInteractiveOverlayEventArgs(returnFeature));

            return returnFeature;
        }

        protected virtual void OnFeatureDragging(FeatureDraggingEditInteractiveOverlayEventArgs e)
        {
            EventHandler<EventArgs> handler = FeatureDragging;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnFeatureDragged(FeatureDraggedEditInteractiveOverlayEventArgs e)
        {
            EventHandler<EventArgs> handler = FeatureDragged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected override void OnTrackStarting(TrackStartingTrackInteractiveOverlayEventArgs e)
        {
            textBlock.Visibility = Visibility.Visible;
            base.OnTrackStarting(e);
        }

        protected override void OnTrackEnded(TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            textBlock.Visibility = Visibility.Collapsed;
            base.OnTrackEnded(e);
            bool hasArea = MeasureLastFeatureInLayer();

            if (hasArea)
            {
                var lastTrackingFeature = TrackShapeLayer.InternalFeatures.LastOrDefault();
                if (lastTrackingFeature != null)
                {
                    lastTrackingFeature.Id = NewMeasurementName(lastTrackingFeature.ColumnValues["Result"]);

                    lastTrackingFeature.ColumnValues["DisplayName"] = lastTrackingFeature.Id;
                    var mapShape = GetMapShape(lastTrackingFeature);

                    ShapeLayer.MapShapes.Add(lastTrackingFeature.Id, mapShape);
                }

                TakeSnapshot();
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.OnTrackEndedDescription));
            }
            TrackShapeLayer.InternalFeatures.Clear();
        }

        protected override void DrawTileCore(GeoCanvas geoCanvas)
        {
            base.DrawTileCore(geoCanvas);
            LayerTile layerTile = OverlayCanvas.Children.OfType<LayerTile>().FirstOrDefault(tmpTile
                => tmpTile.GetValue(FrameworkElement.NameProperty).Equals("DefaultLayerTile"));

            if (layerTile != null)
            {
                layerTile.DrawingLayers.Insert(0, ShapeLayer);
                layerTile.Draw(geoCanvas);
            }
        }

        protected override InteractiveResult KeyDownCore(KeyEventInteractionArguments interactionArguments)
        {
            InteractiveResult result = base.KeyDownCore(interactionArguments);
            if (interactionArguments.Key == System.Windows.Forms.Keys.Escape.ToString())
            {
                CancelLastestTracking();
                textBlock.Visibility = Visibility.Collapsed;
                if (TrackMode != TrackMode.None)
                {
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                }
            }
            return result;
        }

        protected override RectangleShape GetBoundingBoxCore()
        {
            RectangleShape extent = ExtentHelper.GetBoundingBoxOfItems(ShapeLayer.MapShapes.Select(m => m.Value.Feature));
            return extent;
        }

        private MapShape GetMapShape(Feature lastTrackingFeature)
        {
            var mapShape = new MapShape(lastTrackingFeature);
            foreach (var item in MeasurementStyle.Styles)
            {
                mapShape.ZoomLevels.ZoomLevel01.CustomStyles.Add(item);
            }
            mapShape.ZoomLevels.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            return mapShape;
        }

        private void SetStylesForInMemoryFeatureLayer(InMemoryFeatureLayer featureLayer)
        {
            featureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
            featureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
            featureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
            featureLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;
            foreach (var item in MeasurementStyle.Styles)
            {
                featureLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(item);
            }
            featureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
        }

        private void InitializeColumns(InMemoryFeatureLayer featureLayer, bool trackMeasureResult = true)
        {
            featureLayer.SafeProcess(() =>
            {
                if (trackMeasureResult)
                {
                    featureLayer.Columns.Add(new FeatureSourceColumn(measureResultColumnName, DbfColumnType.Float.ToString(), 0));
                }
                featureLayer.Columns.Add(new FeatureSourceColumn(memoColumnName));
                featureLayer.Columns.Add(new FeatureSourceColumn(valueStyleColumnName));
                featureLayer.Columns.Add(new FeatureSourceColumn(AreaColumnName));
                featureLayer.Columns.Add(new FeatureSourceColumn(DistanceColumnName));
                featureLayer.Columns.Add(new FeatureSourceColumn(UnitColumnName));
            });
        }

        private void TakeSnapshot()
        {
            Collection<MapShape> features = new Collection<MapShape>();
            foreach (var mapShape in shapeLayer.MapShapes)
            {
                features.Add(mapShape.Value);
            }
            stateController.Add(features);
        }

        private bool MeasureLastFeatureInLayer()
        {
            foreach (var targetLayer in new[] { TrackShapeLayer, TrackShapesInProcessLayer })
            {
                if (targetLayer.InternalFeatures.Count > 0)
                {
                    Feature feature = targetLayer.InternalFeatures[targetLayer.InternalFeatures.Count - 1];
                    if (!feature.ColumnValues.ContainsKey(measureResultColumnName))
                    {
                        bool hasArea = MeasureOneFeature(feature);
                        if (!hasArea) return false;
                        Feature greatCircleFeature = GetGreatCircle(feature);
                        if (greatCircleFeature != null)
                        {
                            targetLayer.InternalFeatures[targetLayer.InternalFeatures.Count - 1] = greatCircleFeature;
                        }
                    }
                    else
                    {
                        bool hasArea = MeasureOneFeature(feature);
                        if (!hasArea) return false;
                    }
                }
            }
            return true;
        }

        private bool MeasureOneFeature(Feature feature)
        {
            switch (TrackMode)
            {
                case TrackMode.Rectangle:
                case TrackMode.Square:
                case TrackMode.Ellipse:
                case TrackMode.Circle:
                case TrackMode.Polygon:
                    bool hasArea = SetTextForArea(feature);
                    if (!hasArea) return false;
                    break;
                case TrackMode.Line:
                    SetTextForLine(feature);
                    break;
                case TrackMode.Custom:
                    BaseShape baseShape = feature.GetShape();
                    if (baseShape is AreaBaseShape)
                    {
                        hasArea = SetTextForArea(feature);
                        if (!hasArea) return false;
                    }
                    else if (baseShape is LineBaseShape)
                    {
                        SetTextForLine(feature);
                    }
                    break;
                case TrackMode.Point:
                    break;
            }
            feature.ColumnValues[valueStyleColumnName] = valueStyleColumnValue;
            textBlock.Text = feature.ColumnValues[measureResultColumnName];

            return true;
        }

        private void SetTextForLine(Feature feature)
        {
            double length = 0;
            string textValue = string.Empty;

            try
            {
                if (measuringMode == MeasuringInMode.DecimalDegree)
                {
                    Feature tempFeature = GetProjectedFeature(feature);
                    length = ((LineBaseShape)tempFeature.GetShape()).GetLength(GeographyUnit.DecimalDegree, DistanceUnit);
                }
                else
                {
                    length = ((LineBaseShape)feature.GetShape()).GetLength(MapArguments.MapUnit, DistanceUnit);
                }
                textValue = string.Format(CultureInfo.InvariantCulture, "{0:N3} {1}", length, GisEditorWpfMapExtension.GetAbbreviateDistanceUnit(DistanceUnit));
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                textValue = "Error";
            }
            finally
            {
                feature.ColumnValues[DistanceColumnName] = length.ToString(CultureInfo.InvariantCulture);
                feature.ColumnValues[UnitColumnName] = DistanceUnit.ToString();
                feature.ColumnValues[measureResultColumnName] = textValue;
            }
        }

        private bool SetTextForArea(Feature feature)
        {
            double area = 0;
            if (measuringMode == MeasuringInMode.DecimalDegree)
            {
                Feature tmpFeature = GetProjectedFeature(feature);
                area = ((AreaBaseShape)tmpFeature.GetShape()).GetArea(GeographyUnit.DecimalDegree, AreaUnit);
            }
            else
            {
                area = ((AreaBaseShape)feature.GetShape()).GetArea(MapArguments.MapUnit, AreaUnit);
            }

            double compareArea = Conversion.ConvertMeasureUnits(area, AreaUnit, AreaUnit.SquareMeters);
            if (compareArea < 3)
            {
                return false;
            }

            string textValue = string.Format("{0:N3} {1}", area, GetAbbreviateAreaUnit(AreaUnit));
            feature.ColumnValues[AreaColumnName] = area.ToString(CultureInfo.InvariantCulture);
            feature.ColumnValues[UnitColumnName] = AreaUnit.ToString();
            feature.ColumnValues[measureResultColumnName] = textValue;

            return true;
        }

        private static Feature GetProjectedFeature(Feature feature)
        {
            Proj4Projection projection = new Proj4Projection();
            projection.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
            projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            projection.SyncProjectionParametersString();

            Feature tmpFeature = new Feature(feature.GetWellKnownBinary());
            if (projection.CanProject())
            {
                projection.Open();
                tmpFeature = projection.ConvertToInternalProjection(tmpFeature);
                projection.Close();
            }
            return tmpFeature;
        }

        private static string GetAbbreviateAreaUnit(AreaUnit unit)
        {
            switch (unit)
            {
                case AreaUnit.SquareMeters:
                    return "m2";
                case AreaUnit.Acres:
                    return "ac.";
                case AreaUnit.Hectares:
                    return "ha";
                case AreaUnit.SquareFeet:
                    return "sq. ft.";
                case AreaUnit.SquareKilometers:
                    return "sq. km";
                case AreaUnit.SquareMiles:
                    return "sq. mi.";
                case AreaUnit.SquareUsSurveyFeet:
                    return "sq. us-ft.";
                case AreaUnit.SquareYards:
                    return "sq. yd.";
                default:
                    return "unknown.";
            }
        }

        private Feature GetGreatCircle(Feature feature)
        {
            LineShape line = feature.GetShape() as LineShape;
            if (line != null)
            {
                MultilineShape greatCircle = new MultilineShape();
                for (int i = 0; i < line.Vertices.Count; i++)
                {
                    if (i != line.Vertices.Count - 1)
                    {
                        PointShape point = new PointShape(line.Vertices[i]);
                        MultilineShape multiline = point.GreatCircle(new PointShape(line.Vertices[i + 1]));
                        if (multiline != null)
                        {
                            foreach (var l in multiline.Lines) greatCircle.Lines.Add(l);
                        }
                    }
                }

                if (greatCircle.Lines.Count > 0)
                {
                    Feature wrapperFeature = new Feature(greatCircle);
                    foreach (var columnValue in feature.ColumnValues)
                    {
                        wrapperFeature.ColumnValues.Add(columnValue.Key, columnValue.Value);
                    }

                    return wrapperFeature;
                }
            }

            return null;
        }

        private Tuple<bool, Feature> IsFoundFeatureMeasured(Feature foundFeature)
        {
            bool isMeasured = false;
            Feature feature = null;

            if (foundFeature != null)
            {
                string uniqueKey = foundFeature.Tag.ToString();
                foreach (MapShape mapShape in ShapeLayer.MapShapes.Values)
                {
                    if (mapShape.Feature.Tag != null && uniqueKey.Equals(mapShape.Feature.Tag.ToString()))
                    {
                        isMeasured = true;
                        feature = mapShape.Feature;
                        break;
                    }
                }
            }

            return new Tuple<bool, Feature>(isMeasured, feature);
        }

        private Feature GetDragFeatureByPoint(InteractionArguments interactionArguments)
        {
            Feature result = null;
            var point = new Feature(interactionArguments.WorldX, interactionArguments.WorldY);

            foreach (var shape in ShapeLayer.MapShapes.Reverse())
            {
                if (shape.Value.Feature.Intersects(point.Buffer(10, GisEditor.ActiveMap.MapUnit, DistanceUnit.Meter)))
                {
                    result = shape.Value.Feature;
                    selectPoint = point;
                    break;
                }
            }

            return result;
        }

        private Feature FindFeatureByPoint(double x, double y)
        {
            Feature result = null;

            IEnumerable<FeatureLayer> allFeatureLayers = null;
            string currentProj4 = String.Empty;

            if (ParentMap != null)
            {
                allFeatureLayers = ParentMap.GetFeatureLayers(true).Reverse();
                currentProj4 = ParentMap.DisplayProjectionParameters;
            }
            else allFeatureLayers = new Collection<FeatureLayer>();

            GeographyUnit currentUnit = GeographyUnit.DecimalDegree;
            if (!string.IsNullOrEmpty(currentProj4))
            {
                currentUnit = GisEditorWpfMap.GetGeographyUnit(currentProj4);
            }

            foreach (var featureLayer in allFeatureLayers)
            {
                Collection<Feature> featuresInDistance = null;
                featureLayer.SafeProcess(() =>
                {
                    featuresInDistance = AscendingSearch(x, y, currentUnit, featureLayer, 1, 20);
                });
                //featureLayer.Open();
                //var featuresInDistance = AscendingSearch(x, y, currentUnit, featureLayer, 1, 20);
                //featureLayer.Close();

                if (featuresInDistance != null && featuresInDistance.Count > 0)
                {
                    if (featuresInDistance.Count > 1)
                    {
                        result = featuresInDistance[0];
                        for (int i = 0; i < featuresInDistance.Count - 1; i++)
                        {
                            result = result.GetIntersection(featuresInDistance[i + 1]);
                        }
                    }
                    else
                    {
                        result = featuresInDistance[0];
                    }
                    result.Tag = Convert.ToBase64String(result.GetWellKnownBinary());
                    break;
                }
            }

            return result;
        }

        private static Collection<Feature> AscendingSearch(double x, double y, GeographyUnit currentUnit, FeatureLayer featureLayer, double lowerScale, double upperScale)
        {
            Collection<Feature> results = null;

            if (currentUnit == GeographyUnit.DecimalDegree)
            {
                if (Math.Round(y, 4) > 90 || Math.Round(y, 4) < -90 || Math.Round(x, 4) > 180 || Math.Round(x, 4) < -180)
                {
                    return null;
                }
            }

            for (double i = lowerScale; i < upperScale; i++)
            {
                var featuresInDistance = featureLayer.FeatureSource
                                                     .GetFeaturesWithinDistanceOf(new Feature(x, y),
                                                      currentUnit,
                                                      DistanceUnit.Meter, lowerScale,
                                                      ReturningColumnsType.NoColumns);

                if (featuresInDistance.Count > 0)
                {
                    results = featuresInDistance;
                    break;
                }
            }

            return results;
        }

        private void CancelLastestTracking()
        {
            if (TrackMode != TrackMode.None && TrackShapeLayer.InternalFeatures.Count > 0)
            {
                Vertices.Clear();
                TrackShapeLayer.InternalFeatures.RemoveAt(TrackShapeLayer.InternalFeatures.Count - 1);
                TrackShapesInProcessLayer.InternalFeatures.Clear();
                Refresh();
                if (TrackMode == TrackMode.Polygon || TrackMode == TrackMode.Line)
                {
                    MouseDoubleClick(new InteractionArguments());
                }
                else
                {
                    MouseUp(new InteractionArguments());
                }
            }
        }

        private string NewMeasurementName(string result)
        {
            int index = 1;
            string featureName = string.Format(CultureInfo.InvariantCulture, nameTemplate, index) + " ";
            while (ShapeLayer.MapShapes.Any(f => f.Key.StartsWith(featureName, StringComparison.Ordinal)))
            {
                featureName = string.Format(CultureInfo.InvariantCulture, nameTemplate, ++index);
            }
            return string.Format(featureName + " ({0})", result);
        }

        private void SelectAFeature(InteractionArguments interactionArguments)
        {
            Feature foundFeature = FindFeatureByPoint(interactionArguments.WorldX, interactionArguments.WorldY);
            if (foundFeature != null)
            {
                var result = IsFoundFeatureMeasured(foundFeature);
                if (!result.Item1)
                {
                    MeasureOneFeature(foundFeature);
                    foundFeature.Id = NewMeasurementName(foundFeature.ColumnValues["Result"]);
                    foundFeature.ColumnValues["DisplayName"] = foundFeature.Id;
                    var mapShape = GetMapShape(foundFeature);
                    ShapeLayer.MapShapes.Add(foundFeature.Id, mapShape);
                    TakeSnapshot();
                    MeasureLastFeatureInLayer();
                    Refresh();
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.MouseClickCoreDescription));
                }
                else if (ShapeLayer.MapShapes.ContainsKey(result.Item2.Id))
                {
                    ShapeLayer.MapShapes.Remove(result.Item2.Id);
                    Refresh();
                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.MouseClickCoreDescription));
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("MeasureTrackInteractiveOverlayNoFeaturesText"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        public static CompositeStyle GetInitialCompositeStyle()
        {
            AreaStyle measurementAreaStyle = new AreaStyle(new GeoPen(GeoColor.SimpleColors.Black, 2), new GeoSolidBrush(GeoColor.StandardColors.Transparent));
            measurementAreaStyle.Name = "Measurement Area Style";
            LineStyle measurementLineStyle = new LineStyle(new GeoPen(GeoColor.SimpleColors.Black, 2), new GeoPen(GeoColor.StandardColors.Transparent));
            measurementLineStyle.Name = "Measurement Line Style";
            IconTextStyle measurementPointStyle = new IconTextStyle();
            measurementPointStyle.Name = "Measurement Text Style";
            measurementPointStyle.TextColumnName = measureResultColumnName;
            measurementPointStyle.Font = new GeoFont("Arial", 12);
            measurementPointStyle.TextSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Black);
            measurementPointStyle.HaloPen = new GeoPen(GeoColor.StandardColors.White, 3);
            measurementPointStyle.BestPlacement = false;
            measurementPointStyle.DuplicateRule = LabelDuplicateRule.UnlimitedDuplicateLabels;
            measurementPointStyle.OverlappingRule = LabelOverlappingRule.AllowOverlapping;
            measurementPointStyle.PolygonLabelingLocationMode = PolygonLabelingLocationMode.BoundingBoxCenter;
            measurementPointStyle.ForceLineCarriage = true;
            measurementPointStyle.SuppressPartialLabels = false;
            measurementPointStyle.SplineType = SplineType.StandardSplining;
            measurementPointStyle.TextLineSegmentRatio = 10000;
            measurementPointStyle.AllowLineCarriage = true;
            measurementPointStyle.IsHaloEnabled = true;

            CompositeStyle measurementStyle = new CompositeStyle();
            measurementStyle.Styles.Add(measurementAreaStyle);
            measurementStyle.Styles.Add(measurementLineStyle);
            measurementStyle.Styles.Add(measurementPointStyle);
            return measurementStyle;
        }
    }
}