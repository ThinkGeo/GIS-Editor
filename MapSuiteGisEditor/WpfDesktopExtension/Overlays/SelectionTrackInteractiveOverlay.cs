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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using ThinkGeo.Core;
using ThinkGeo.UI.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension.Properties;
using System.Windows.Media;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class SelectionTrackInteractiveOverlay : TrackInteractiveOverlay
    {
        private static readonly int tolerance = 5;
        private static readonly Random random = new Random();
        //If change this field value, please change the SelectionUIPlugin.FeatureIdColumnName, too.
        private static readonly string FeatureIdColumnName = "FeatureId";
        public static readonly string FeatureIdSeparator = "[TG]";

        [NonSerialized]
        private Collection<FeatureLayer> targetFeatureLayers;

        [NonSerialized]
        private InMemoryFeatureLayer highlightFeatureLayer;

        [NonSerialized]
        private InMemoryFeatureLayer standOutHighlightFeatureLayer;

        [NonSerialized]
        private LayerTile highlightTile;

        [NonSerialized]
        private SelectionMode selectionMode;

        [NonSerialized]
        private SpatialQueryMode spatialQueryMode;

        [NonSerialized]
        private Collection<FeatureLayer> filteredLayers;

        [NonSerialized]
        private bool isTracking;

        public SelectionTrackInteractiveOverlay()
            : this(new Collection<FeatureLayer>())
        { }

        public SelectionTrackInteractiveOverlay(IEnumerable<FeatureLayer> featureLayersForSelecting)
        {
            this.highlightTile = new LayerTile { IsAsync = false };
            this.highlightTile.SetValue(Canvas.ZIndexProperty, 0);
            this.InitializeHightlightFeatureLayer();
            this.InitializeStandOutHighlightFeatureLayer();
            this.targetFeatureLayers = new Collection<FeatureLayer>();
            this.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
            this.FilteredLayers = new Collection<FeatureLayer>();
            //this.RenderMode = RenderMode.DrawingVisual;
            foreach (var featureLayer in featureLayersForSelecting)
            {
                this.targetFeatureLayers.Add(featureLayer);
            }
        }

        public event EventHandler<EventArgs> FeatureSelected;

        protected virtual void OnFeatureSelected(object sender, EventArgs e)
        {
            EventHandler<EventArgs> handler = FeatureSelected;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        public GeoBrush FillColor
        {
            get
            {
                return this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.FillBrush;
            }
            set
            {
                SetHighlightFeatureLayerStyle(value, null, null);
            }
        }

        public GeoBrush OutlineColor
        {
            get
            {
                return this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.Brush;
            }
            set
            {
                SetHighlightFeatureLayerStyle(null, value, null);
            }
        }

        public float OutlineThickness
        {
            get
            {
                return this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.Width;
            }
            set
            {
                SetHighlightFeatureLayerStyle(null, null, value);
            }
        }

        public SpatialQueryMode SpatialQueryMode
        {
            get { return spatialQueryMode; }
            set { spatialQueryMode = value; }
        }

        public Collection<FeatureLayer> TargetFeatureLayers
        {
            get { return targetFeatureLayers; }
        }

        public override bool IsEmpty
        {
            get
            {
                bool isEmpty = TrackShapeLayer.InternalFeatures.Count == 0 && HighlightFeatureLayer.InternalFeatures.Count == 0;
                if (isEmpty)
                {
                    RefreshHighlightTile(new RectangleShape());
                    Children.Clear();
                }
                return isEmpty;
            }
        }

        public InMemoryFeatureLayer HighlightFeatureLayer
        {
            get { return highlightFeatureLayer; }
        }

        public InMemoryFeatureLayer StandOutHighlightFeatureLayer
        {
            get { return standOutHighlightFeatureLayer; }
        }

        public Collection<FeatureLayer> FilteredLayers
        {
            get { return filteredLayers; }
            set { filteredLayers = value; }
        }


        public void AddHighlightFeatures(IEnumerable<Feature> features)
        {
            AddHighlightFeatures(features, AddHighlightFeaturesMode.Reset);
        }

        public void AddHighlightFeatures(IEnumerable<Feature> features, AddHighlightFeaturesMode addHighlightFeaturesMode)
        {
            AddHighlightFeaturesCore(features, addHighlightFeaturesMode);
        }

        protected virtual void AddHighlightFeaturesCore(IEnumerable<Feature> featuresToAdd, AddHighlightFeaturesMode addHighlightFeaturesMode)
        {
            switch (addHighlightFeaturesMode)
            {
                case AddHighlightFeaturesMode.Reset:
                    highlightFeatureLayer.InternalFeatures.Clear();
                    goto case AddHighlightFeaturesMode.Add;

                case AddHighlightFeaturesMode.Add:
                    UsingFeatureLayer(highlightFeatureLayer, (layer) =>
                    {
                        featuresToAdd.ForEach(feature =>
                        {
                            Feature newFeature = CreateHighlightFeature(feature, (FeatureLayer)feature.Tag);
                            if (!layer.InternalFeatures.Contains(newFeature.Id))
                            {
                                layer.EditTools.Add(newFeature);
                            }
                        });
                    });
                    break;

                case AddHighlightFeaturesMode.FilterExisting:
                    if (featuresToAdd.Count() > 0)
                    {
                        UsingFeatureLayer(highlightFeatureLayer, (layer) =>
                        {
                            var tag = featuresToAdd.First().Tag;
                            var filteredFeatureIds = featuresToAdd.Select(f => f.Id);
                            var highlightFeatures = layer.InternalFeatures.Where(f => f.Tag == tag).ToList();

                            for (int i = highlightFeatures.Count - 1; i >= 0; i--)
                            {
                                var id = highlightFeatures[i].Id;
                                var index = id.IndexOf("|");
                                if (index >= 0) id = id.Substring(0, index);
                                if (!filteredFeatureIds.Contains(id))
                                {
                                    layer.EditTools.Delete(highlightFeatures[i].Id);
                                }
                            }
                        });
                    }
                    else highlightFeatureLayer.InternalFeatures.Clear();
                    break;

                default:
                    break;
            }
        }

        public string CreateHighlightFeatureId(Feature sourceFeature, FeatureLayer sourceFeatureLayer)
        {
            return CreateHighlightFeatureIdCore(sourceFeature, sourceFeatureLayer);
        }

        protected virtual string CreateHighlightFeatureIdCore(Feature sourceFeature, FeatureLayer sourceFeatureLayer)
        {
            if (sourceFeature == null) return string.Empty;

            int sourceLayerHashCode = 0;
            if (sourceFeatureLayer != null) sourceLayerHashCode = sourceFeatureLayer.GetHashCode();
            else sourceLayerHashCode = random.Next(9999);
            string newFeatureId = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", sourceFeature.Id, FeatureIdSeparator, sourceLayerHashCode);
            return newFeatureId;
        }

        public Feature CreateHighlightFeature(Feature sourceFeature, FeatureLayer sourceFeatureLayer)
        {
            if (sourceFeature == null) return null;
            return CreateHighlightFeatureCore(sourceFeature, sourceFeatureLayer);
        }

        protected virtual Feature CreateHighlightFeatureCore(Feature sourceFeature, FeatureLayer sourceFeatureLayer)
        {
            string newFeatureId = CreateHighlightFeatureId(sourceFeature, sourceFeatureLayer);
            try
            {
                sourceFeature = sourceFeature.MakeValidUsingSqlTypes();
            }
            catch { }

            Feature newFeature = new Feature(sourceFeature.GetWellKnownBinary(), newFeatureId, sourceFeature.ColumnValues);

            //LINK:
            //foreach (var item in sourceFeature.LinkColumnValues)
            //{
            //    newFeature.LinkColumnValues.Add(item.Key, item.Value);
            //}
            newFeature.Tag = sourceFeature.Tag;
            return newFeature;
        }

        public string GetOriginalFeatureId(Feature highlightFeature)
        {
            return GetOriginalFeatureIdCore(highlightFeature);
        }

        protected virtual string GetOriginalFeatureIdCore(Feature highlightFeature)
        {
            string featureId = highlightFeature.Id;
            int separaterIndex = featureId.LastIndexOf("[TG]");
            if (separaterIndex != -1)
            {
                featureId = featureId.Remove(separaterIndex);
            }

            return featureId;
        }

        public Dictionary<FeatureLayer, GeoCollection<Feature>> GetSelectedFeaturesGroup()
        {
            return GetSelectedFeaturesGroup(null);
        }

        public Dictionary<FeatureLayer, GeoCollection<Feature>> GetSelectedFeaturesGroup(FeatureLayer targetFeatureLayer)
        {
            return GetSelectedFeaturesGroupCore(targetFeatureLayer);
        }

        protected virtual Dictionary<FeatureLayer, GeoCollection<Feature>> GetSelectedFeaturesGroupCore(FeatureLayer targetFeatureLayer)
        {
            Dictionary<FeatureLayer, GeoCollection<Feature>> groupedFeatures = new Dictionary<FeatureLayer, GeoCollection<Feature>>();
            IEnumerable<IGrouping<FeatureLayer, Feature>> tmpGroupedFeatures = null;
            if (targetFeatureLayer != null)
            {
                tmpGroupedFeatures = HighlightFeatureLayer.InternalFeatures.Where(tmpFeature => tmpFeature.Tag == targetFeatureLayer)
                    .GroupBy(tmpFeature => (FeatureLayer)tmpFeature.Tag);
            }
            else
            {
                tmpGroupedFeatures = HighlightFeatureLayer.InternalFeatures.GroupBy(tmpFeature => (FeatureLayer)tmpFeature.Tag);
            }

            foreach (var item in tmpGroupedFeatures)
            {
                GeoCollection<Feature> tmpFeatures = new GeoCollection<Feature>();
                foreach (var feature in item)
                {
                    string featureId = GetOriginalFeatureId(feature);
                    if (!tmpFeatures.Contains(featureId))
                    {
                        Feature newFeature = new Feature(feature.GetWellKnownBinary(), featureId, feature.ColumnValues);
                        //LINK:
                        //foreach (var linkItem in feature.LinkColumnValues)
                        //{
                        //    newFeature.LinkColumnValues.Add(linkItem.Key, linkItem.Value);
                        //}
                        tmpFeatures.Add(featureId, newFeature);
                    }
                }

                groupedFeatures.Add(item.Key, tmpFeatures);
            }

            return groupedFeatures;
        }

        // ThinkGeo v14+ overlay drawing is asynchronous.
        protected override async Task DrawAsyncCore(RectangleShape targetExtent, OverlayRefreshType overlayRefreshType, CancellationToken cancellationToken)
        {
            await base.DrawAsyncCore(targetExtent, overlayRefreshType, cancellationToken).ConfigureAwait(false);

            if (!Children.Contains(highlightTile) && overlayRefreshType == OverlayRefreshType.Redraw)
            {
                Children.Add(highlightTile);
            }

            //bool zoomLevelChanged = CheckZoomLevelChanged(targetExtent);
            if (overlayRefreshType == OverlayRefreshType.Redraw && (!isTracking || Vertices.Count == 0))
            //if (overlayRefreshType == OverlayRefreshType.Redraw && (!isTracking || Vertices.Count == 0 || zoomLevelChanged))
            {
                RefreshHighlightTile(targetExtent);
            }
        }

        protected override InteractiveResult KeyDownCore(KeyEventInteractionArguments interactionArguments)
        {
            InteractiveResult result = base.KeyDownCore(interactionArguments);
            if (interactionArguments.Key ==  System.Windows.Input.Key.Escape)
            {
                CancelLastestTracking(this);
                if (TrackMode != TrackMode.None)
                {
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                }
            }
            return result;
        }

        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            //if (UnsafeHelper.IsKeyPressed(Keys.ControlKey))
            //{
            //    this.selectionMode = SelectionMode.Subtract;
            //}
            //else if (UnsafeHelper.IsKeyPressed(Keys.ShiftKey))
            //{
            //    this.selectionMode = SelectionMode.Added;
            //}
            //else
            //{
            //    this.selectionMode = SelectionMode.None;
            //}

            return base.MouseMoveCore(interactionArguments); ;
        }

        protected override void OnTrackStarting(TrackStartingTrackInteractiveOverlayEventArgs e)
        {
            isTracking = true;
            base.OnTrackStarting(e);
        }

        protected override void OnTrackEnded(TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            isTracking = false;
            base.OnTrackEnded(e);

            if (TrackShapeLayer.InternalFeatures.Count != 0)
            {
                Collection<Feature> features = new Collection<Feature>();
                Collection<ErrorFeatureInfo> errorInfo = new Collection<ErrorFeatureInfo>();
                Dictionary<string, Collection<Feature>> currentSelectedFeatures = new Dictionary<string, Collection<Feature>>();
                foreach (var trackedFeature in TrackShapeLayer.InternalFeatures)
                {
                    Feature currentTrackedFeature = trackedFeature;

                    if (!currentTrackedFeature.IsGeometryValid())
                    {
                        currentTrackedFeature = currentTrackedFeature.MakeValidUsingSqlTypes();
                    }
                    if (currentTrackedFeature.GetShape() != null && currentTrackedFeature.GetWellKnownType() == WellKnownType.Point)
                    {
                        PointShape trackedPoint = (PointShape)currentTrackedFeature.GetShape();
                        double resolution = MapArguments.CurrentResolution;
                        currentTrackedFeature = new Feature(new RectangleShape(trackedPoint.X - tolerance * resolution,
                            trackedPoint.Y + tolerance * resolution,
                            trackedPoint.X + tolerance * resolution,
                            trackedPoint.Y - tolerance * resolution).GetWellKnownBinary(), currentTrackedFeature.Id);
                    }
                    else if (currentTrackedFeature.GetShape() != null && currentTrackedFeature.GetWellKnownType() == WellKnownType.Line)
                    {
                        var toleranceDistance = MapUtils.GetResolutionFromScale(MapArguments.CurrentScale, GeographyUnit.Meter) * tolerance;
                        try
                        {
                            currentTrackedFeature = currentTrackedFeature.Buffer(toleranceDistance, MapArguments.MapUnit, DistanceUnit.Meter);
                        }
                        catch { }
                    }


                    foreach (var queryingFeatureLayer in TargetFeatureLayers.Where(l => FilteredLayers.Contains(l)))
                    {
                        IEnumerable<Feature> queriedFeatures = new Collection<Feature>();
                        if (currentTrackedFeature.GetShape() != null)
                        {
                            bool isClosed = false;
                            Monitor.Enter(queryingFeatureLayer);
                            if (!queryingFeatureLayer.IsOpen)
                            {
                                isClosed = true;
                                queryingFeatureLayer.Open();
                            }

                            try
                            {
                                Func<Feature, bool, Func<Feature, bool>, IEnumerable<Feature>> processSpatialQueryFunc
                                    = new Func<Feature, bool, Func<Feature, bool>, IEnumerable<Feature>>((tmpTrackedFeature, filter, spatialQueryFunc) =>
                                {
                                    var bbox = tmpTrackedFeature.GetBoundingBox();
                                    Collection<Feature> featuresInsideOfBBox = null;

                                    if (filter)
                                    {
                                        featuresInsideOfBBox = queryingFeatureLayer.QueryTools.GetFeaturesIntersecting(bbox, queryingFeatureLayer.GetDistinctColumnNames());
                                    }
                                    else
                                    {
                                        featuresInsideOfBBox = queryingFeatureLayer.QueryTools.GetAllFeatures(queryingFeatureLayer.GetDistinctColumnNames());
                                    }

                                    var featuresToProcessing = featuresInsideOfBBox.AsParallel().Where(featureToValidate =>
                                    {
                                        if (featureToValidate.IsGeometryValid()) return true;
                                        else
                                        {
                                            errorInfo.Add(new ErrorFeatureInfo { FeatureId = featureToValidate.Id, Message = "Invalid feature" });
                                            return false;
                                        }
                                    }).ToArray();

                                    var resultQueriedFeatures = new Collection<Feature>();
                                    foreach (var tmpFeature in featuresToProcessing)
                                    {
                                        var validFeature = MakeFeatureValidate(tmpFeature);

                                        if (spatialQueryFunc(validFeature))
                                        {
                                            resultQueriedFeatures.Add(validFeature);
                                        }
                                    }

                                    return resultQueriedFeatures.GetVisibleFeatures(queryingFeatureLayer.ZoomLevelSet, MapArguments.CurrentExtent, MapArguments.MapWidth, MapArguments.MapUnit);
                                });

                                currentTrackedFeature = currentTrackedFeature.MakeValidUsingSqlTypes();

                                switch (spatialQueryMode)
                                {
                                    case SpatialQueryMode.Touching:
                                        queriedFeatures = processSpatialQueryFunc(currentTrackedFeature, true, tmpFeature
                                            => currentTrackedFeature.GetShape().Intersects(tmpFeature));
                                        break;

                                    case SpatialQueryMode.CompletelyContained:
                                        queriedFeatures = processSpatialQueryFunc(currentTrackedFeature, true, tmpFeature
                                            => currentTrackedFeature.GetShape().Contains(tmpFeature));
                                        break;

                                    case SpatialQueryMode.Intersecting:
                                        queriedFeatures = processSpatialQueryFunc(currentTrackedFeature, true, tmpFeature
                                           => currentTrackedFeature.GetShape().Intersects(tmpFeature));
                                        break;

                                    case SpatialQueryMode.Nearest:
                                        queriedFeatures = queryingFeatureLayer.QueryTools.GetFeaturesNearestTo(currentTrackedFeature, GeographyUnit.DecimalDegree, 1, queryingFeatureLayer.GetDistinctColumnNames());
                                        break;

                                    case SpatialQueryMode.NotTouching:
                                        queriedFeatures = processSpatialQueryFunc(currentTrackedFeature, false, tmpFeature
                                            => tmpFeature.GetShape().IsDisjointed(currentTrackedFeature));
                                        break;
                                }
                            }
                            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message, "Invalid Features", MessageBoxButtons.OK, MessageBoxIcon.Error); }

                            if (isClosed)
                            {
                                queryingFeatureLayer.Close();
                            }

                            Monitor.Exit(queryingFeatureLayer);

                            foreach (var feature in queriedFeatures)
                            {
                                feature.Tag = queryingFeatureLayer;
                                features.Add(feature);
                            }
                        }
                    }
                }

                //if (errorInfo.Count > 0)
                //{
                //    System.Windows.Forms.MessageBox.Show(Resources.InvalidFeatures, "Invalid Features", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //}

                TrackShapeLayer.InternalFeatures.Clear();
                features = new Collection<Feature>(RenameFeatures(features).GroupBy(f => f.Id).Select(g => g.FirstOrDefault()).Where(f => f != null).ToList());
                switch (selectionMode)
                {
                    case SelectionMode.None:
                        UsingFeatureLayer(HighlightFeatureLayer, (layer) =>
                        {
                            layer.InternalFeatures.Clear();
                            foreach (var feature in features)
                            {
                                //if (!feature.ColumnValues.ContainsKey(featureIdColumnName))
                                //{

                                //    //string featureIdColumn = LayerPluginHelper.GetFeatureIdColumn(ownerFeatureLayer);
                                //    //if (feature.ColumnValues.ContainsKey(featureIdColumn))
                                //    //{
                                //    //    header = feature.ColumnValues[featureIdColumn];
                                //    //}

                                //    string featureId = feature.Id;

                                //    if (featureId.Contains(FeatureIdSeparator))
                                //    {
                                //        featureId = featureId.Split(new string[] { FeatureIdSeparator }, StringSplitOptions.RemoveEmptyEntries)[0];
                                //    }

                                //    feature.ColumnValues.Add(featureIdColumnName, featureId);
                                //}
                                layer.EditTools.Add(feature);
                            }
                        });
                        break;

                    case SelectionMode.Added:
                        UsingFeatureLayer(HighlightFeatureLayer, (layer) =>
                        {
                            ToggleExistingFeatures(features);
                            foreach (var feature in features)
                            {
                                if (!layer.InternalFeatures.Contains(feature))
                                {
                                    layer.EditTools.Add(feature);
                                }
                            }
                        });
                        break;

                    case SelectionMode.Subtract:
                        UsingFeatureLayer(HighlightFeatureLayer, (layer) =>
                        {
                            foreach (var feature in features)
                            {
                                if (layer.InternalFeatures.Contains(feature))
                                {
                                    layer.EditTools.Delete(feature.Id);
                                }
                            }
                        });
                        break;
                }
                this.selectionMode = SelectionMode.None;

                OnFeatureSelected(this, new EventArgs());
            }
        }

        private static Feature MakeFeatureValidate(Feature feature)
        {
            Feature validFeature = feature.MakeValidUsingSqlTypes();

            WellKnownType featureType = feature.GetWellKnownType();
            WellKnownType validatedType = validFeature.GetWellKnownType();

            Feature result = validFeature;

            if (validatedType != featureType
                && validatedType == WellKnownType.GeometryCollection)
            {
                GeometryCollectionShape geoCollectionShape = validFeature.GetShape() as GeometryCollectionShape;
                if (geoCollectionShape != null)
                {
                    BaseShape resultShape = null;
                    switch (featureType)
                    {
                        case WellKnownType.Point:
                        case WellKnownType.Multipoint:
                            Collection<PointShape> points = new Collection<PointShape>();
                            foreach (var shape in geoCollectionShape.Shapes)
                            {
                                PointShape point = shape as PointShape;
                                if (point != null) points.Add(point);
                            }
                            resultShape = new MultipointShape(points);
                            break;
                        case WellKnownType.Line:
                        case WellKnownType.Multiline:
                            Collection<LineShape> lines = new Collection<LineShape>();
                            foreach (var shape in geoCollectionShape.Shapes)
                            {
                                LineShape line = shape as LineShape;
                                if (line != null) lines.Add(line);
                            }
                            resultShape = new MultilineShape(lines);
                            break;
                        case WellKnownType.Polygon:
                        case WellKnownType.Multipolygon:
                            Collection<PolygonShape> polygons = new Collection<PolygonShape>();
                            foreach (var shape in geoCollectionShape.Shapes)
                            {
                                PolygonShape polygon = shape as PolygonShape;
                                if (polygon != null) polygons.Add(polygon);
                            }
                            resultShape = new MultipolygonShape(polygons);
                            break;
                        default:
                            break;
                    }

                    if (resultShape != null) result = new Feature(resultShape);
                }
            }

            return result;
        }

        private void SetHighlightFeatureLayerStyle(GeoBrush fillColor, GeoBrush outlineColor, float? outlineThickness)
        {
            this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            if (fillColor != null)
            {
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.FillBrush = fillColor;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle.InnerPen.Brush = fillColor;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle.FillBrush= fillColor;
            }

            if (outlineColor != null)
            {
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.Brush = outlineColor;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle.OuterPen.Brush = outlineColor;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle.OutlinePen.Brush = outlineColor;
            }

            if (outlineThickness != null)
            {
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.Width = (float)outlineThickness;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle.OuterPen.Width = (float)outlineThickness;
                this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle.OutlinePen.Width = (float)outlineThickness;
            }
        }

        private void InitializeHightlightFeatureLayer()
        {
            this.highlightFeatureLayer = new InMemoryFeatureLayer(); // { MaxRecordsToDraw = 5000 };

            GeoSolidBrush fillColor = new GeoSolidBrush(GeoColors.Transparent);
            GeoSolidBrush outerColor = new GeoSolidBrush(new GeoColor(255, GeoColors.Yellow));
            GeoSolidBrush innerColor = new GeoSolidBrush(new GeoColor(255, GeoColor.FromHtml("#B0EBFF")));
            GeoSolidBrush centerColor = new GeoSolidBrush(new GeoColor(0, GeoColor.FromHtml("#FFFFFF")));

            ZoomLevel zoomLevel = this.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01;
            zoomLevel.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            zoomLevel.DefaultAreaStyle.FillBrush = fillColor;
            zoomLevel.DefaultAreaStyle.OutlinePen.Brush = outerColor;
            zoomLevel.DefaultAreaStyle.OutlinePen.Width = 2.0f;

            zoomLevel.DefaultLineStyle.OuterPen.Brush = outerColor;
            zoomLevel.DefaultLineStyle.OuterPen.Width = 5.0f;
            zoomLevel.DefaultLineStyle.InnerPen.Brush = innerColor;
            zoomLevel.DefaultLineStyle.InnerPen.Width = 1.0f;
            zoomLevel.DefaultLineStyle.CenterPen.Brush = centerColor;
            zoomLevel.DefaultLineStyle.CenterPen.Width = 1.0f;

            zoomLevel.DefaultPointStyle.FillBrush = fillColor;
            zoomLevel.DefaultPointStyle.OutlinePen.Brush = outerColor;
            zoomLevel.DefaultPointStyle.OutlinePen.Width = 2.0f;

            zoomLevel.DefaultTextStyle.TextBrush = new GeoSolidBrush(GeoColors.Yellow);
            zoomLevel.DefaultTextStyle.TextColumnName = FeatureIdColumnName;
            zoomLevel.DefaultTextStyle.Font = new GeoFont("Arial", 7, DrawingFontStyles.Bold);

            highlightFeatureLayer.Open();
            var featureIdColumn = new FeatureSourceColumn(FeatureIdColumnName);
            if (!this.highlightFeatureLayer.Columns.Contains(featureIdColumn))
            {
                this.highlightFeatureLayer.Columns.Add(featureIdColumn);
                //this.highlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = TextStyles.CreateSimpleTextStyle(FeatureIdColumnName, "Arial", 7, DrawingFontStyles.Bold, GeoColor.FromArgb(255, 91, 91, 91));
            }
            highlightFeatureLayer.Close();
        }

        private void InitializeStandOutHighlightFeatureLayer()
        {
            standOutHighlightFeatureLayer = new InMemoryFeatureLayer();
            GeoSolidBrush outerColor = new GeoSolidBrush(GeoColor.FromHtml("#FF0000"));
            ZoomLevel zoomLevel = this.standOutHighlightFeatureLayer.ZoomLevelSet.ZoomLevel01;
            zoomLevel.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            zoomLevel.DefaultAreaStyle.OutlinePen.Brush = outerColor;
            zoomLevel.DefaultAreaStyle.OutlinePen.Width = 4.0f;
            zoomLevel.DefaultLineStyle.OuterPen.Brush = outerColor;
            zoomLevel.DefaultLineStyle.OuterPen.Width = 4.0f;
            zoomLevel.DefaultPointStyle.FillBrush = outerColor;
            zoomLevel.DefaultPointStyle.OutlinePen.Brush = outerColor;
            zoomLevel.DefaultPointStyle.OutlinePen.Width = 4.0f;
        }

        private bool IsMultiPointLayer(FeatureLayer queryingFeatureLayer)
        {
            bool result = false;

            ShapeFileFeatureLayer shpLayer = queryingFeatureLayer as ShapeFileFeatureLayer;
            if (shpLayer != null)
            {
                result = shpLayer.GetShapeFileType() == ShapeFileType.Multipoint;
            }

            return result;
        }

        private void ToggleExistingFeatures(Collection<Feature> features)
        {
            Collection<Feature> featuresToRemove = new Collection<Feature>();
            foreach (var feature in HighlightFeatureLayer.InternalFeatures)
            {
                if (features.Count(tmpFeature => tmpFeature.Id.Equals(feature.Id, StringComparison.Ordinal)) > 0)
                {
                    featuresToRemove.Add(feature);
                }
            }

            foreach (var feature in featuresToRemove)
            {
                HighlightFeatureLayer.InternalFeatures.Remove(feature.Id);
                Feature relatedFeature = features.FirstOrDefault(tmpFeature => tmpFeature.Id.Equals(feature.Id, StringComparison.Ordinal));
                if (relatedFeature.GetShape() != null)
                {
                    features.Remove(relatedFeature);
                }
            }
        }

        private Collection<Feature> RenameFeatures(Collection<Feature> features)
        {
            var results = new Collection<Feature>();
            foreach (var feature in features)
            {
                Feature newFeature = CreateHighlightFeature(feature, (FeatureLayer)feature.Tag);
                results.Add(newFeature);
            }

            return results;
        }


        private void RefreshHighlightTile(RectangleShape targetExtent)
        {
            //highlightTile.TargetExtent = targetExtent;
            //highlightTile.DrawingLayers.Clear();
            //highlightTile.DrawingLayers.Add(highlightFeatureLayer);
            //highlightTile.DrawingLayers.Add(standOutHighlightFeatureLayer);

            //if (MapArguments == null) return;

            //highlightTile.Width = MapArguments.MapWidth;
            //highlightTile.Height = MapArguments.MapHeight;
            //highlightTile.ZoomLevelIndex = MapArguments.GetSnappedZoomLevelIndex(targetExtent);

            //// v14: Render highlight tiles with the WPF DrawingVisualGeoCanvas.
            //var geoCanvas = new DrawingVisualGeoCanvas();
            //object nativeImage = new RenderTargetBitmap(
            //    (int)highlightTile.Width,
            //    (int)highlightTile.Height,
            //    geoCanvas.Dpi,
            //    geoCanvas.Dpi,
            //    PixelFormats.Pbgra32);

            //geoCanvas.BeginDrawing(nativeImage, targetExtent, MapArguments.MapUnit);
            //highlightTile.Draw(geoCanvas);
            //geoCanvas.EndDrawing();
            ////highlightTile.CommitDrawing(geoCanvas, GetImageSourceFromNativeImage(nativeImage));
            //highlightTile.CommitDrawing(geoCanvas, (ImageSource)nativeImage);
        }

        //private bool CheckZoomLevelChanged(RectangleShape targetExtent)
        //{
        //    bool zoomLevelChanged = false;
        //    if (MapArguments != null && PreviousExtent != null)
        //    {
        //        int targetZoomLevelIndex = MapArguments.GetSnappedZoomLevelIndex(targetExtent);
        //        int currentZoomLevelIndex = MapArguments.GetSnappedZoomLevelIndex(PreviousExtent);
        //        if (currentZoomLevelIndex != targetZoomLevelIndex)
        //        {
        //            zoomLevelChanged = true;
        //        }
        //    }

        //    return zoomLevelChanged;
        //}

        private static object GetImageSourceFromNativeImage(object nativeImage)
        {
            object imageSource = nativeImage;
            if (nativeImage is System.Drawing.Bitmap)
            {
                System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)nativeImage;
                MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);
                imageSource = memoryStream;
                bitmap.Dispose();
            }

            return imageSource;
        }

        private static void UsingFeatureLayer<T>(T layer, Action<T> action, bool requireEditing = true)
            where T : FeatureLayer
        {
            lock (layer)
            {
                layer.Open();

                bool inTrans = layer.EditTools.IsInTransaction;
                bool needToSwitchTrans = !inTrans && requireEditing;

                if (!inTrans)
                {
                    layer.EditTools.BeginTransaction();
                }

                action(layer);

                layer.EditTools.CommitTransaction();
                layer.Close();
            }
        }

        private static void CancelLastestTracking(TrackInteractiveOverlay trackOverlay)
        {
            if (trackOverlay != null && trackOverlay.TrackMode != TrackMode.None && trackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                trackOverlay.TrackShapeLayer.InternalFeatures.RemoveAt(trackOverlay.TrackShapeLayer.InternalFeatures.Count - 1);
                //trackOverlay.Refresh();
                if (trackOverlay.TrackMode == TrackMode.Polygon ||
                    trackOverlay.TrackMode == TrackMode.Line)
                {
                    trackOverlay.MouseDoubleClick(new InteractionArguments());
                }
                else
                {
                    trackOverlay.MouseUp(new InteractionArguments());
                }
            }
        }

        private class ErrorFeatureInfo
        {
            public string FeatureId { get; set; }

            public string Message { get; set; }
        }

        public enum SelectionMode
        {
            None = 0,
            Added = 1,
            Subtract = 2
        }
    }
}