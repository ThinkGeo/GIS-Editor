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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public partial class GisEditorEditInteractiveOverlay : InteractiveOverlay
    {
        private const string existingFeatureColumnName = "state";
        private const string existingFeatureColumnValue = "selected";
        private const string highlightControlPointKey = "HighlightControlPoint";
        private const int snappingRangeTolerance = 2;
        private const int editOverlayZIndex = 601;
        private const GeographyUnit mapUnit = GeographyUnit.Meter;

        [NonSerialized]
        private Collection<string> newFeatureIds;

        [NonSerialized]
        private TrackResultProcessMode trackResultProcessMode;

        [NonSerialized]
        private TranslateTransform translateTransform;

        [NonSerialized]
        private InMemoryFeatureLayer editShapesLayer;

        [NonSerialized]
        private InMemoryFeatureLayer associateControlPointsLayer;

        [NonSerialized]
        private InMemoryFeatureLayer reshapeControlPointsLayer;

        [NonSerialized]
        private InMemoryFeatureLayer snappingPointsLayer;

        [NonSerialized]
        private InMemoryFeatureLayer snappingToleranceLayer;

        [NonSerialized]
        private LayerTile tile;

        [NonSerialized]
        private double snappingDistance;

        [NonSerialized]
        private SnappingDistanceUnit snappingDistanceUnit;

        [NonSerialized]
        private ObservableCollection<FeatureLayer> snappingLayers;

        [NonSerialized]
        private FeatureLayer editTargetLayer;

        [NonSerialized]
        private bool isPointLayer = false;

        [NonSerialized]
        private bool canDrag;

        [NonSerialized]
        private bool canReshape;

        [NonSerialized]
        private bool requestMouseDownOneTime;

        [NonSerialized]
        private bool canRotate;

        [NonSerialized]
        private bool canResize;

        [NonSerialized]
        private Collection<SimpleCandidate> editsInProcessSimpleCandidate;

        [NonSerialized]
        private LineShape editsInProcess;

        [NonSerialized]
        private InMemoryFeatureLayer editCandidatesLayer;

        public GisEditorEditInteractiveOverlay()
            : base()
        {
            newFeatureIds = new Collection<string>();
            snappingLayers = new ObservableCollection<FeatureLayer>();
            snappingLayers.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && ParentMap != null)
                {
                    ParentMap.Refresh(this);
                }
            };
            editSnapshots = new Collection<EditSnapshot>();
            OverlayCanvas.SetValue(Canvas.ZIndexProperty, editOverlayZIndex);

            editShapesLayer = new InMemoryFeatureLayer();
            associateControlPointsLayer = new InMemoryFeatureLayer();
            reshapeControlPointsLayer = new InMemoryFeatureLayer();
            snappingPointsLayer = new InMemoryFeatureLayer();
            snappingToleranceLayer = new InMemoryFeatureLayer();
            editsInProcessSimpleCandidate = new Collection<SimpleCandidate>();
            editCandidatesLayer = new InMemoryFeatureLayer();

            translateTransform = new TranslateTransform();
            OverlayCanvas.RenderTransform = translateTransform;
            RenderMode = RenderMode.DrawingVisual;
            CanRotate = true;
            CanResize = true;

            SetDefaultStyle();
            tile = GetLayerTile();
            OverlayCanvas.Children.Add(tile);
            SnappingDistance = 10;
            SnappingDistanceUnit = SnappingDistanceUnit.Pixel;
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.KeyDown += new System.Windows.Input.KeyEventHandler(MainWindow_KeyDown);
            }
            TakeSnapshot();
        }

        public Collection<string> NewFeatureIds
        {
            get { return newFeatureIds; }
        }

        public TrackResultProcessMode TrackResultProcessMode
        {
            get { return trackResultProcessMode; }
            set { trackResultProcessMode = value; }
        }

        public FeatureLayer EditTargetLayer
        {
            get
            {
                return editTargetLayer;
            }
            set
            {
                if (editTargetLayer != value)
                {
                    editTargetLayer = value;
                    editingFeature = default(Feature);
                    var shapeFileFeatureLayer = editTargetLayer as ShapeFileFeatureLayer;
                    if (shapeFileFeatureLayer != null)
                    {
                        shapeFileFeatureLayer.SafeProcess(() =>
                        {
                            isPointLayer = shapeFileFeatureLayer.GetShapeFileType() == ShapeFileType.Point;
                        });
                    }
                }
            }
        }

        public bool CanRotate
        {
            get { return canRotate; }
            set { canRotate = value; }
        }

        public bool CanResize
        {
            get { return canResize; }
            set { canResize = value; }
        }

        public bool CanCancel
        {
            get
            {
                if ((EditTargetLayer == null && EditShapesLayer == null) || EditShapesLayer.InternalFeatures.Count == 0) return false;
                else return CanFoward || CanRollback;

                //else if (EditTargetLayer == null && EditShapesLayer != null) return editSnapshots.Count >= 1 && EditShapesLayer.InternalFeatures.Count > 0;
                //else return editSnapshots.Count >= 1 && (EditTargetLayer.FeatureIdsToExclude.Count > 0 || EditShapesLayer.InternalFeatures.Count > 0);
            }
        }

        [Obsolete("This property is obsoleted, please use the property EditShapesLayer's CustomStyles instead. This property is obsolete and may be removed in or after version 9.0.")]
        public GeoBrush EditShapesLayerFillColor { get; set; }

        [Obsolete("This property is obsoleted, please use the property EditShapesLayer's CustomStyles instead. This property is obsolete and may be removed in or after version 9.0.")]
        public GeoBrush EditShapesLayerOutlineColor { get; set; }

        public InMemoryFeatureLayer EditShapesLayer
        {
            get { return editShapesLayer; }
        }

        public InMemoryFeatureLayer EditCandidatesLayer
        {
            get { return editCandidatesLayer; }
        }

        public ObservableCollection<FeatureLayer> SnappingLayers
        {
            get { return snappingLayers; }
        }

        public double SnappingDistance
        {
            get { return snappingDistance; }
            set
            {
                snappingDistance = value;
            }
        }

        public SnappingDistanceUnit SnappingDistanceUnit
        {
            get { return snappingDistanceUnit; }
            set { snappingDistanceUnit = value; }
        }

        public InMemoryFeatureLayer ReshapeControlPointsLayer { get { return reshapeControlPointsLayer; } }

        public InMemoryFeatureLayer AssociateControlPointsLayer { get { return associateControlPointsLayer; } }

        public bool CanDrag
        {
            get { return canDrag; }
            set { canDrag = value; }
        }

        public bool CanReshape
        {
            get { return canReshape; }
            set { canReshape = value; }
        }

        public bool RequestMouseDownOneTime
        {
            get { return requestMouseDownOneTime; }
            set { requestMouseDownOneTime = value; }
        }

        public override bool IsEmpty
        {
            get
            {
                //bool isEmpty = editShapesLayer.InternalFeatures.Count == 0 && SnappingLayer == null;
                bool isEmpty = editShapesLayer.InternalFeatures.Count == 0 && SnappingLayers.Count == 0;

                if (isEmpty)
                {
                    foreach (Wpf.Tile currentTile in OverlayCanvas.Children)
                    {
                        currentTile.Dispose();
                    }

                    OverlayCanvas.Children.Clear();
                }

                return isEmpty;
            }
        }

        public Collection<Feature> GetEditingFeaturesInterseting()
        {
            return GetEditingFeaturesInterseting(new PointShape(worldX, worldY));
        }

        public Collection<Feature> GetEditingFeaturesInterseting(BaseShape baseShape)
        {
            BaseShape searchArea = baseShape;
            if (baseShape.GetWellKnownType() == WellKnownType.Point)
            {
                PointShape worldCoordinate = (PointShape)baseShape;
                double searchTorlerence = 15 * MapArguments.CurrentResolution;
                searchArea = new RectangleShape(worldCoordinate.X - searchTorlerence,
                    worldCoordinate.Y + searchTorlerence, worldCoordinate.X + searchTorlerence,
                    worldCoordinate.Y - searchTorlerence);
            }

            Collection<Feature> features = new Collection<Feature>();
            lock (EditShapesLayer)
            {
                if (!EditShapesLayer.IsOpen) EditShapesLayer.Open();
                var intersectingFeatures = EditShapesLayer.QueryTools.GetFeaturesIntersecting(searchArea, EditShapesLayer.GetDistinctColumnNames());
                foreach (var feature in intersectingFeatures)
                {
                    features.Add(feature);
                }
            }

            return features;
        }

        public bool CheckIsInHitArea(Vertex hitPoint)
        {
            double resolution = MapArguments.CurrentResolution;
            double searchingTolerance = resolution * 14 * searchingToleranceRatio;
            RectangleShape searchingArea = new RectangleShape(hitPoint.X - searchingTolerance, hitPoint.Y + searchingTolerance, hitPoint.X + searchingTolerance, hitPoint.Y - searchingTolerance);

            if (!EditShapesLayer.IsOpen) EditShapesLayer.Open();
            var features = EditShapesLayer.QueryTools.GetFeaturesIntersecting(searchingArea, EditShapesLayer.GetDistinctColumnNames());
            return features.Count != 0;
        }

        protected override void DrawCore(RectangleShape targetExtent, OverlayRefreshType overlayRefreshType)
        {
            if (overlayRefreshType == OverlayRefreshType.Pan)
            {
                if (PreviousExtent != null)
                {
                    double resolution = MapArguments.CurrentResolution;
                    double worldOffsetX = targetExtent.UpperLeftPoint.X - PreviousExtent.UpperLeftPoint.X;
                    double worldOffsetY = targetExtent.UpperLeftPoint.Y - PreviousExtent.UpperLeftPoint.Y;
                    double screenOffsetX = worldOffsetX / resolution;
                    double screenOffsetY = worldOffsetY / resolution;

                    translateTransform.X -= screenOffsetX;
                    translateTransform.Y += screenOffsetY;
                }
            }
            else
            {
                translateTransform.X = 0;
                translateTransform.Y = 0;

                snappingPointsLayer.InternalFeatures.Clear();

                if (OverlayCanvas.Children.Count == 0)
                {
                    OverlayCanvas.Children.Add(tile);
                }

                tile.TargetExtent = targetExtent;
                tile.Width = MapArguments.ActualWidth;
                tile.Height = MapArguments.ActualHeight;
                tile.ZoomLevelIndex = MapArguments.GetSnappedZoomLevelIndex(targetExtent);
                RedrawTile(tile);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && tile != null)
            {
                tile.Dispose();
            }
        }

        private void RedrawTile(LayerTile layerTile)
        {
            int tileSW = (int)MapArguments.ActualWidth;
            int tileSH = (int)MapArguments.ActualHeight;

            GeoCanvas geoCanvas = null;
            object nativeImage = null;

            if (RenderMode == RenderMode.DrawingVisual)
            {
                geoCanvas = new DrawingVisualGeoCanvas();
                nativeImage = new RenderTargetBitmap(tileSW, tileSH, geoCanvas.Dpi, geoCanvas.Dpi, PixelFormats.Pbgra32);
            }
            else
            {
                nativeImage = new System.Drawing.Bitmap(tileSW, tileSH);
                geoCanvas = new PlatformGeoCanvas();
            }

            geoCanvas.BeginDrawing(nativeImage, layerTile.TargetExtent, MapArguments.MapUnit);
            layerTile.Draw(geoCanvas);
            geoCanvas.EndDrawing();
            layerTile.CommitDrawing(geoCanvas, GetImageSourceFromNativeImage(nativeImage));
        }

        private LayerTile GetLayerTile()
        {
            LayerTile layerTile = new LayerTile();
            layerTile.SetValue(Canvas.NameProperty, "DefaultLayerTile");
            layerTile.IsAsync = false;
            layerTile.DrawingLayers.Add(editCandidatesLayer);
            layerTile.DrawingLayers.Add(EditShapesLayer);

            //layerTile.DrawingLayers.Add(snappingRangeLayer);
            layerTile.DrawingLayers.Add(snappingPointsLayer);
            layerTile.DrawingLayers.Add(ReshapeControlPointsLayer);
            layerTile.DrawingLayers.Add(AssociateControlPointsLayer);
            layerTile.DrawingLayers.Add(snappingToleranceLayer);

            return layerTile;
        }

        private void SetDefaultStyle()
        {
            var defaultPointStyle = PointStyles.CreateSimpleCircleStyle(GeoColor.FromArgb(102, 0, 0, 255), 10, GeoColor.FromArgb(100, 0, 0, 255), 2);
            var defaultLineStyle = LineStyles.CreateSimpleLineStyle(GeoColor.FromArgb(100, 0, 0, 255), 2, true);
            var defaultAreaStyle = AreaStyles.CreateSimpleAreaStyle(GeoColor.FromArgb(102, GeoColor.FromHtml("#EFFBD6")), GeoColor.FromArgb(255, 0, 0, 255), 2);

            editShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = defaultPointStyle;
            editShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = defaultLineStyle;
            editShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = defaultAreaStyle;
            editShapesLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            ValueStyle valueStyle = new ValueStyle();
            valueStyle.ColumnName = existingFeatureColumnName;
            valueStyle.ValueItems.Add(new ValueItem(string.Empty, PointStyles.CreateSimpleSquareStyle(GeoColor.StandardColors.White, 8, GeoColor.StandardColors.Black)));
            valueStyle.ValueItems.Add(new ValueItem(existingFeatureColumnValue, PointStyles.CreateSimpleSquareStyle(GeoColor.StandardColors.Orange, 8, GeoColor.StandardColors.Black)));

            reshapeControlPointsLayer.Open();
            reshapeControlPointsLayer.Columns.Add(new FeatureSourceColumn(existingFeatureColumnName));
            reshapeControlPointsLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(valueStyle);
            reshapeControlPointsLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            associateControlPointsLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = new PointStyle(GetGeoImageFromResource("/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Resources/resize.png"));
            associateControlPointsLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            snappingPointsLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(new PeakStyle());
            snappingPointsLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            snappingToleranceLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            snappingToleranceLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = new AreaStyle(new GeoPen(GeoColor.SimpleColors.Black, 1));

            var unselectedPointStyle = PointStyles.CreateSimpleCircleStyle(GeoColor.StandardColors.LightGray, 10, GeoColor.StandardColors.LightGray, 2);
            var unselectedLineStyle = LineStyles.CreateSimpleLineStyle(GeoColor.StandardColors.LightGray, 3, true);
            var unselectedAreaStyle = AreaStyles.CreateSimpleAreaStyle(GeoColor.FromArgb(0, GeoColor.FromHtml("#EFFBD6")), GeoColor.StandardColors.LightGray, 3);

            editCandidatesLayer.Open();
            editCandidatesLayer.Columns.Add(new FeatureSourceColumn(existingFeatureColumnName));
            editCandidatesLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(unselectedAreaStyle);
            editCandidatesLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(unselectedLineStyle);
            editCandidatesLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(unselectedPointStyle);
            editCandidatesLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(valueStyle);
            editCandidatesLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
        }

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

        private static GeoImage GetGeoImageFromResource(string path)
        {
            var stream = Application.GetResourceStream(new Uri(path, UriKind.RelativeOrAbsolute)).Stream;
            GeoImage image = new GeoImage(stream);
            return image;
        }

        private Collection<Feature> GetTargetFeaturesInterseting(BaseShape baseShape)
        {
            BaseShape searchArea = baseShape;
            if (baseShape.GetWellKnownType() == WellKnownType.Point)
            {
                PointShape worldCoordinate = (PointShape)baseShape;
                double searchTorlerence = 15 * MapArguments.CurrentResolution;
                searchArea = new RectangleShape(worldCoordinate.X - searchTorlerence,
                    worldCoordinate.Y + searchTorlerence, worldCoordinate.X + searchTorlerence,
                    worldCoordinate.Y - searchTorlerence);
            }

            Collection<Feature> features = new Collection<Feature>();
            lock (EditTargetLayer)
            {
                if (!EditTargetLayer.IsOpen) EditTargetLayer.Open();

                //var excludeFeatures = new Collection<string>();
                //foreach (var item in editTargetLayer.FeatureIdsToExclude)
                //{
                //    excludeFeatures.Add(item);
                //}

                editTargetLayer.FeatureIdsToExclude.Clear();
                foreach (var item in newFeatureIds)
                {
                    editTargetLayer.FeatureIdsToExclude.Add(item);
                }

                EditTargetLayer.SafeProcess(() =>
                {
                    var intersectingFeatures = EditTargetLayer.QueryTools.GetFeaturesIntersecting(searchArea, EditTargetLayer.GetDistinctColumnNames());
                    foreach (var feature in intersectingFeatures)
                    {
                        try
                        {
                            features.Add(feature);
                        }
                        catch
                        {
                            features.Add(SqlTypesGeometryHelper.MakeValid(feature));
                        }
                    }
                });
            }

            foreach (var item in editShapesLayer.InternalFeatures)
            {
                if (item.GetShape().Intersects(searchArea) && !features.Any(f => f.GetWellKnownText() == item.GetWellKnownText()))
                {
                    features.Add(item);
                }
            }

            foreach (var item in editCandidatesLayer.InternalFeatures)
            {
                if (item.GetShape().Intersects(searchArea) && !features.Any(f => f.GetWellKnownText() == item.GetWellKnownText()))
                {
                    features.Add(item);
                }
            }

            return features;
        }
    }
}