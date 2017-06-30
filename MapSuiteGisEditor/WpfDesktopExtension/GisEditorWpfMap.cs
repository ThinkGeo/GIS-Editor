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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public sealed partial class GisEditorWpfMap : WpfMap
    {
        private const int panPercentage = 10;
        private const string overlayNamePattern = "(?<=Layer Group) \\d+";

        public event EventHandler<DisplayProjectionParametersChangedGisEditorWpfMapEventArgs> DisplayProjectionParametersChanged;

        public event EventHandler<AddingLayersToActiveOverlayEventArgs> AddingLayersToActiveOverlay;

        public event EventHandler<AddedLayersToActiveOverlayEventArgs> AddedLayersToActiveOverlay;

        #region Used by LandPro

        // Used by LandPro.
        public event EventHandler<IdentifyingFeaturesMapEventArgs> IdentifyingFeatures;

        // Used by LandPro.
        public event EventHandler<IdentifiedFeaturesMapEventArgs> IdentifiedFeatures;

        // Used by LandPro.
        public void OnIdentifyingFeatures(IdentifyingFeaturesMapEventArgs e)
        {
            EventHandler<IdentifyingFeaturesMapEventArgs> handler = IdentifyingFeatures;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Used by LandPro.
        public void OnIdentifiedFeatures(IdentifiedFeaturesMapEventArgs e)
        {
            EventHandler<IdentifiedFeaturesMapEventArgs> handler = IdentifiedFeatures;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion Used by LandPro

        [NonSerialized]
        private bool isMapLoaded;

        [NonSerialized]
        private bool isBusy;

        [Obfuscation(Exclude = true)]
        private string version;

        [Obfuscation(Exclude = true)]
        private string displayProjectionParameters;

        [NonSerialized]
        private Cursor previousCursor;

        [NonSerialized]
        private bool isMapStateChanged;

        [NonSerialized]
        private ProgressBar progressBar;

        [NonSerialized]
        private TextBlock progressText;

        [NonSerialized]
        private Grid progressGrid;

        [Obfuscation(Exclude = true)]
        private AdornmentOverlay fixedAdornmentOverlay;

        [Obfuscation(Exclude = true)]
        private SelectionTrackInteractiveOverlay selectOverlay;

        [NonSerialized]
        private Canvas fixedAdornmentCanvas;

        [NonSerialized]
        private Layer activeLayer;

        [Obfuscation(Exclude = true)]
        private Overlay activeOverlay;

        [Obfuscation(Exclude = true)]
        private ExtentInteractiveOverlay extentOverlay;

        [Obfuscation(Exclude = true)]
        private string name;

        [NonSerialized]
        private Dictionary<BingMapsOverlay, string> bingMapsApplicationIds;

        [NonSerialized]
        private Collection<RectangleShape> mapNextExtents;

        [NonSerialized]
        private DispatcherTimer raiseCurrentExtentChangedTimer;

        [NonSerialized]
        private Canvas mapCanvas;

        [NonSerialized]
        private Image cancelImage;

        [NonSerialized]
        private PointShape mouseDownPoint;

        static GisEditorWpfMap()
        {
        }

        public GisEditorWpfMap()
            : this(string.Empty)
        { }

        public GisEditorWpfMap(string name)
            : base()
        {
            ResetMapVersion();
            mapNextExtents = new Collection<RectangleShape>();
            bingMapsApplicationIds = new Dictionary<BingMapsOverlay, string>();
            ZoomLevelSet = new GoogleMapsZoomLevelSet();
            foreach (var item in ZoomLevelSet.GetZoomLevels())
            {
                item.Scale = Math.Round(item.Scale, 6);
                ZoomLevelSet.CustomZoomLevels.Add(item);
            }
            for (int i = 0; i < 5; i++)
            {
                var scale = ZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale * 0.5;
                var zoomLevel = new ZoomLevel(Math.Round(scale, 6));
                ZoomLevelSet.CustomZoomLevels.Add(zoomLevel);
            }
            MinimumScale = ZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale;
            FlowDirection = FlowDirection.LeftToRight;
            Name = name;
            ContextMenu = new ContextMenu();
            InitializeProgressBar();
            InitializeNotifyLegendChanging();
            ExtentOverlay.MapMouseDown += new EventHandler<MapMouseDownInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseDown);
            ExtentOverlay.MapMouseUp += new EventHandler<MapMouseUpInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseUp);
            TrackOverlay = new GisEditorTrackInteractiveOverlay();
            DisplayProjectionParameters = Proj4Projection.GetEpsgParametersString(4326);

            MapTools.PanZoomBar.IsEnabled = false;
            MapTools.Logo.IsEnabled = false;

            MapTools.Add(new AdornmentLogo() { IsEnabled = false });
            Loaded += MapLoaded;

            ScaleLineAdornmentLayer defaultScaleLineAdornmentLayer = new ScaleLineAdornmentLayer
            {
                Location = AdornmentLocation.LowerLeft,
                XOffsetInPixel = 10,
                YOffsetInPixel = -10,
                Name = "Scale 1",
                UnitSystem = ScaleLineUnitSystem.ImperialAndMetric
            };

            fixedAdornmentOverlay = new AdornmentOverlay();
            fixedAdornmentOverlay.Layers.Add(defaultScaleLineAdornmentLayer);

            selectOverlay = new SelectionTrackInteractiveOverlay();
            InteractiveOverlays.Insert(0, selectOverlay);
            KeyDown += new KeyEventHandler(GisEditorWpfMap_KeyDown);
            GCCollectionMode = GCCollectionMode.Optimized;
        }

        protected override void OnCurrentExtentChanged(CurrentExtentChangedWpfMapEventArgs e)
        {
            if (raiseCurrentExtentChangedTimer == null)
            {
                raiseCurrentExtentChangedTimer = new DispatcherTimer();
                raiseCurrentExtentChangedTimer.Interval = TimeSpan.FromMilliseconds(500);
                raiseCurrentExtentChangedTimer.Tick += (s, a) =>
                {
                    base.OnCurrentExtentChanged(e);
                    raiseCurrentExtentChangedTimer.Stop();
                };
            }

            if (raiseCurrentExtentChangedTimer.IsEnabled)
            {
                raiseCurrentExtentChangedTimer.Stop();
            }

            raiseCurrentExtentChangedTimer.Start();
        }

        /// <summary>
        /// This static property gets or sets the layer that is currently activated.
        /// </summary>
        /// <remarks>
        /// When the layer is activated, it indicates that the layer is selected.
        /// You need to set this property when you think this layer is activated.
        /// This is the only way to communicate with the plugings.
        ///
        /// For example, treeview plugin and a select plugin.
        /// When one layer is selected in the treeview plugin,
        /// and the select plugin is enabled,
        /// we can highlight the features only only in this layer.
        /// </remarks>
        public Layer ActiveLayer { get { return activeLayer; } set { activeLayer = value; } }

        /// <summary>
        /// This static property gets or sets the overlays that is currently activated.
        /// </summary>
        public Overlay ActiveOverlay { get { return activeOverlay; } set { activeOverlay = value; } }

        public GisEditorEditInteractiveOverlay FeatureLayerEditOverlay
        {
            get { return InteractiveOverlays.OfType<GisEditorEditInteractiveOverlay>().FirstOrDefault(); }
        }

        public Canvas MapCanvas
        {
            get { return mapCanvas; }
        }

        public bool IsBusy
        {
            get { return isBusy; }
        }

        public bool IsMapLoaded
        {
            get { return isMapLoaded; }
            set { isMapLoaded = value; }
        }

        public bool IsMapStateChanged
        {
            get { return isMapStateChanged; }
            set { isMapStateChanged = value; }
        }

        public AdornmentOverlay FixedAdornmentOverlay { get { return fixedAdornmentOverlay; } }

        public SelectionTrackInteractiveOverlay SelectionOverlay { get { return selectOverlay; } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ToolsGrid.Children.Add(progressGrid);
            fixedAdornmentCanvas = (Canvas)GetTemplateChild("AdornmentCanvas");
            mapCanvas = EventCanvas;
        }

        public string DisplayProjectionParameters
        {
            get { return displayProjectionParameters; }
            set
            {
                string oldDisplayProjectionParameters = displayProjectionParameters;
                if (oldDisplayProjectionParameters == null
                    || !oldDisplayProjectionParameters.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    displayProjectionParameters = value;
                    OnDisplayProjectionParametersChanged(oldDisplayProjectionParameters, displayProjectionParameters);
                }
            }
        }

        private Collection<RectangleShape> MapNextExtents
        {
            get { return mapNextExtents; }
        }

        public Collection<FeatureLayer> GetFeatureLayers()
        {
            return GetFeatureLayers(false);
        }

        public Collection<FeatureLayer> GetFeatureLayers(bool visibleLayersOnly)
        {
            IEnumerable<FeatureLayer> featureLayers = new Collection<FeatureLayer>();
            featureLayers = Overlays.OfType<LayerOverlay>()
                .Where(lo => visibleLayersOnly ? lo.IsVisible : true)
                .SelectMany(overlay => overlay.Layers.OfType<FeatureLayer>()
                    .Where(l => visibleLayersOnly ? l.IsVisible : true));

            return new Collection<FeatureLayer>(featureLayers.ToList());
        }

        [Obsolete("This method will be obsoleted soon. Please use AddLayerToActiveOverlay(Layer) instead")]
        public void AddLayersToActiveOverlay(Layer layer)
        {
            AddLayerToActiveOverlay(layer);
        }

        public void AddLayerToActiveOverlay(Layer layer)
        {
            AddLayersToActiveOverlay(new Collection<Layer>() { layer }, TargetLayerOverlayType.Static);
        }

        public void AddLayerToActiveOverlay(Layer layer, TileType tileType)
        {
            AddLayersToActiveOverlay(new Collection<Layer> { layer }, tileType);
        }

        [Obsolete("This method will be obsoleted soon. Please use AddLayerToActiveOverlay(Layer, TargetLayerOverlayType) instead")]
        public void AddLayersToActiveOverlay(Layer layer, bool isDynamic)
        {
            AddLayerToActiveOverlay(layer, isDynamic ? TargetLayerOverlayType.Dynamic : TargetLayerOverlayType.Static);
        }

        [Obsolete("This method will be obsoleted soon. Please use AddLayerToActiveOverlay(Layers, TargetLayerOverlayType) instead")]
        public void AddLayersToActiveOverlay(IEnumerable<Layer> layers, bool isDynamic)
        {
            AddLayersToActiveOverlay(layers, isDynamic ? TargetLayerOverlayType.Dynamic : TargetLayerOverlayType.Static);
        }

        public void AddLayerToActiveOverlay(Layer layer, TargetLayerOverlayType targetLayerOverlayType)
        {
            AddLayersToActiveOverlay(new Collection<Layer> { layer }, targetLayerOverlayType);
        }

        public void AddLayersToActiveOverlay(IEnumerable<Layer> layers)
        {
            AddLayersToActiveOverlay(layers, TargetLayerOverlayType.Static);
        }

        public void AddLayersToActiveOverlay(IEnumerable<Layer> layers, TileType tileType)
        {
            AddLayersParameters parameters = new AddLayersParameters();
            parameters.TileType = tileType;
            foreach (var layer in layers.Where(l => l != null))
            {
                parameters.LayersToAdd.Add(layer);
            }

            AddLayersToActiveOverlay(parameters);
        }

        public void AddLayersToActiveOverlay(IEnumerable<Layer> layers, TargetLayerOverlayType targetLayerOverlayType)
        {
            AddLayersParameters parameters = new AddLayersParameters();
            parameters.TargetLayerOverlayType = targetLayerOverlayType;
            foreach (var layer in layers.Where(l => l != null))
            {
                parameters.LayersToAdd.Add(layer);
            }

            AddLayersToActiveOverlay(parameters);
        }

        public void AddLayersToActiveOverlay(AddLayersParameters parameters)
        {
            if (parameters.LayersToAdd.Count() == 0) return;

            OnAddingLayersToActiveOverlay(parameters);
            bool noOverlaysExist = Overlays.Count == 0;
            bool layerOverlaysExist = Overlays.OfType<LayerOverlay>().Count() != 0;
            bool noLayersInLayerOverlays = Overlays.OfType<LayerOverlay>().SelectMany(tmpoverlay => tmpoverlay.Layers).Count() == 0;
            bool baseOverlaysExist = Overlays.Any(overlay => overlay is TileOverlay && !(overlay is LayerOverlay));
            bool isFirstLayerAfterBase = baseOverlaysExist && noLayersInLayerOverlays;

            bool isFirstLayer = noOverlaysExist || (layerOverlaysExist && noLayersInLayerOverlays && !baseOverlaysExist);
            RectangleShape worldExtent = null;

            if (isFirstLayer)
            {
                string proj4ProjectionParameters = parameters.Proj4ProjectionParameters;
                if (string.IsNullOrEmpty(proj4ProjectionParameters))
                {
                    Layer layerToAdd = parameters.LayersToAdd.FirstOrDefault();
                    proj4ProjectionParameters = layerToAdd.GetInternalProj4ProjectionParameters();
                }
                if (string.IsNullOrEmpty(proj4ProjectionParameters)) return;

                //Step 1 Set projection(first time add).
                foreach (var featureLayer in parameters.LayersToAdd.Where(f => f != null))
                {
                    Proj4ProjectionInfo projectionInfo = featureLayer.GetProj4ProjectionInfo();
                    if (projectionInfo != null)
                    {
                        projectionInfo.ExternalProjectionParametersString = proj4ProjectionParameters;
                        projectionInfo.SyncProjectionParametersString();
                        if (projectionInfo.IsOpen)
                        {
                            projectionInfo.Close();
                            projectionInfo.Open();
                        }
                    }
                }

                //Step 2  Set world extent by layers bounding box, projection parameters and so on.(first time add)
                MapUnit = GisEditorWpfMap.GetGeographyUnit(proj4ProjectionParameters);
                DisplayProjectionParameters = proj4ProjectionParameters;
                worldExtent = GetLayersBoundingBox(parameters.LayersToAdd);
                CurrentExtent = worldExtent;
            }

            if (worldExtent == null) worldExtent = GetLayersBoundingBox(parameters.LayersToAdd);

            //step 3
            AddLayersToLayerOverlay(parameters);
            OnAddedLayersToActiveOverlay(parameters);

            if (parameters.LayersAdded != null)
            {
                parameters.LayersAdded(parameters);
            }
            bool newLayersInSight = parameters.LayersToAdd.Any(layer =>
            {
                if (layer.HasBoundingBox)
                {
                    if (!layer.IsOpen) layer.Open();
                    FeatureLayer featureLayer = layer as FeatureLayer;
                    if (featureLayer != null && !featureLayer.FeatureSource.CanGetBoundingBoxQuickly())
                    {
                        return true;
                    }
                    RectangleShape layerBoundingBox = layer.GetBoundingBox();
                    return layerBoundingBox.Intersects(CurrentExtent);
                }
                else
                {
                    //if the layer does not have any bounding box, then we treat the layer as it is in sight.
                    //just in case we may miss any raster layers.
                    return true;
                }
            });

            RefreshCachesAndZoomToExtent(isFirstLayer, isFirstLayerAfterBase, worldExtent, newLayersInSight, parameters);
        }

        protected void OnAddingLayersToActiveOverlay(AddLayersParameters parameters)
        {
            EventHandler<AddingLayersToActiveOverlayEventArgs> handler = AddingLayersToActiveOverlay;
            if (handler != null)
            {
                handler(this, new AddingLayersToActiveOverlayEventArgs(parameters));
            }
        }

        protected void OnAddedLayersToActiveOverlay(AddLayersParameters parameters)
        {
            EventHandler<AddedLayersToActiveOverlayEventArgs> handler = AddedLayersToActiveOverlay;
            if (handler != null)
            {
                handler(this, new AddedLayersToActiveOverlayEventArgs(parameters));
            }
        }

        private void RefreshCachesAndZoomToExtent(bool isFirstAdded, bool isFirstAddedAfterBase, RectangleShape worldExtent, bool needToRedraw, AddLayersParameters parameters)
        {
            bool useCache = parameters.IsCacheEnabled;
            bool zoomToExtentOfNewlyAddedLayer = parameters.ZoomToExtentAutomatically;
            bool zoomToExtentOfFirstNewlyAddedLayer = parameters.ZoomToExtentOfFirstAutomatically;
            if (ActiveOverlay != null)
            {
                if (ActiveOverlay is TileOverlay)
                {
                    TileOverlay tileOverlay = (TileOverlay)ActiveOverlay;
                    if (useCache) tileOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                    else tileOverlay.RefreshCache(RefreshCacheMode.DisableCache);
                }

                if (worldExtent != null && (isFirstAdded || zoomToExtentOfNewlyAddedLayer || (isFirstAddedAfterBase && zoomToExtentOfFirstNewlyAddedLayer)))
                {
                    if (!parameters.LayersToAdd.Any(l => parameters.ExcludedLayerTypesToZoomTo.Any(e => l.GetType() == e || l.GetType().IsSubclassOf(e))))
                    {
                        //Avoid the extent is very small.
                        if (worldExtent.Width != new RectangleShape().Width)
                        {
                            CurrentExtent = worldExtent;
                        }
                    }
                }

                if (isFirstAdded || needToRedraw)
                {
                    Refresh(TileOverlayExtension.RefreshBufferTime, RequestDrawingBufferTimeType.ResetDelay);
                }
            }
        }

        private void AddLayersToLayerOverlay(AddLayersParameters arguments)
        {
            if (arguments.TargetLayerOverlayType == TargetLayerOverlayType.Dynamic)
            {
                AddLayersToDynamicLayerOverlay(arguments);
            }
            else
            {
                AddLayersToStaticLayerOverlay(arguments);
            }
        }

        private void AddLayersToDynamicLayerOverlay(AddLayersParameters arguments)
        {
            IEnumerable<Layer> layers = arguments.LayersToAdd;
            foreach (var layer in layers)
            {
                LayerOverlay layerOverlay = null;
                if (ActiveOverlay != null && ActiveOverlay is DynamicLayerOverlay)
                {
                    layerOverlay = (DynamicLayerOverlay)ActiveOverlay;
                    layerOverlay.Layers.Add(layer);
                }
                else
                {
                    layerOverlay = Overlays.OfType<DynamicLayerOverlay>().FirstOrDefault();
                    if (layerOverlay == null)
                    {
                        layerOverlay = new DynamicLayerOverlay();
                        layerOverlay.Name = "Dynamic Layer Group";
                        layerOverlay.DrawingQuality = DrawingQuality.HighQuality;
                        layerOverlay.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                        layerOverlay.Layers.Add(layer);
                        Overlays.Add(layerOverlay);
                    }
                    ActiveOverlay = layerOverlay;
                }
            }
        }

        private LayerOverlay GetActivateLayerOverlay(AddLayersParameters arguments)
        {
            LayerOverlay layerOverlay = ActiveOverlay as LayerOverlay;
            if (layerOverlay == null
                || ActiveOverlay is NoaaWeatherOverlay
                || ActiveOverlay is DynamicLayerOverlay
                || layerOverlay.TileType != arguments.TileType)
            {
                layerOverlay = new GisEditorLayerOverlay
                {
                    Name = GetLayerOverlayName(),
                    LockLayerMode = LockLayerMode.Lock,
                    TileBuffer = 0,
                    TileType = arguments.TileType,
                    DrawingExceptionMode = DrawingExceptionMode.DrawException,
                    TileWidth = arguments.TileSize,
                    TileHeight = arguments.TileSize,
                    DrawingQuality = arguments.DrawingQuality
                };
                layerOverlay.RefreshCache(arguments.IsCacheEnabled);

                Overlays.Add(layerOverlay);

                Refresh(layerOverlay, TileOverlayExtension.RefreshBufferTime, RequestDrawingBufferTimeType.ResetDelay);
                ActiveOverlay = layerOverlay;
            }

            return layerOverlay;
        }

        private string GetLayerOverlayName()
        {
            string overlayName = "Layer Group {0}";
            int maxValue = 0;
            foreach (var layerOverlay in Overlays.OfType<LayerOverlay>())
            {
                string match = System.Text.RegularExpressions.Regex.Match(layerOverlay.Name, overlayNamePattern).Value;
                int currentValue = 0;
                if (!string.IsNullOrEmpty(match) && int.TryParse(match, out currentValue))
                {
                    maxValue = maxValue > currentValue ? maxValue : currentValue;
                }
            }
            return string.Format(CultureInfo.InvariantCulture, overlayName, maxValue + 1);
        }

        private void AddLayersToStaticLayerOverlay(AddLayersParameters arguments)
        {
            IEnumerable<Layer> layers = arguments.LayersToAdd;
            LayerOverlay layerOverlay = GetActivateLayerOverlay(arguments);
            if (layers.Count() > 0) layerOverlay.IsVisible = true;

            foreach (var layer in layers)
            {
                bool isLayerValid = true;

                if (layer is ShapeFileFeatureLayer)
                {
                    ShapeFileFeatureLayer shapeFileFeatureLayer = (ShapeFileFeatureLayer)layer;
                    if (arguments.IsMaxRecordsToDrawEnabled)
                    {
                        shapeFileFeatureLayer.MaxRecordsToDraw = arguments.MaxRecordsToDraw;
                    }
                    else
                    {
                        shapeFileFeatureLayer.MaxRecordsToDraw = 0;
                    }
                }
                else if (layer is RasterLayer)
                {
                    RasterLayer rasterLayer = (RasterLayer)layer;
                    string proj4 = string.Empty;
                    Proj4Projection projection = rasterLayer.ImageSource.Projection as Proj4Projection;
                    if (projection != null)
                    {
                        proj4 = projection.InternalProjectionParametersString;
                    }

                    if (string.IsNullOrEmpty(proj4))//|| ManagedProj4ProjectionExtension.CanProject(proj4, DisplayProjectionParameters)
                    {
                        isLayerValid = false;
                        string errorMessage = "The raster layer {0} has a projection that does not match with the map's current projection, it won't be added to the map.";
                        System.Windows.Forms.MessageBox.Show(String.Format(CultureInfo.InvariantCulture, errorMessage, rasterLayer.Name), "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                }

                if (isLayerValid)
                {
                    lock (layerOverlay.Layers)
                    {
                        layerOverlay.Layers.Add(layer);
                    }
                }

                if (layerOverlay != null)
                {
                    FileBitmapTileCache tileCache = layerOverlay.TileCache as FileBitmapTileCache;
                    if (arguments.IsCacheEnabled)
                    {
                        layerOverlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                    }
                    else
                    {
                        layerOverlay.RefreshCache(RefreshCacheMode.DisableCache);
                    }
                }
            }
        }

        public void RefreshActiveOverlay()
        {
            if (ActiveOverlay != null)
            {
                TileOverlay overlay = ActiveOverlay as TileOverlay;
                if (overlay != null)
                {
                    overlay.Invalidate();
                }
            }
        }

        public RectangleShape GetLayersBoundingBox(IEnumerable<Layer> layers)
        {
            RectangleShape worldExtent = null;
            try
            {
                layers.ForEach(l =>
                {
                    Monitor.Enter(l);
                    l.Open();

                    //To avoid this scenario: the layer opens and doesn't close in method GetLayers of LayerPlugin, and then GisEditor will set its projection, after that
                    //, the above code "l.Open();" cannot open its projection.
                    if (l is FeatureLayer)
                    {
                        FeatureLayer featureLayer = (FeatureLayer)l;
                        if (featureLayer.FeatureSource.Projection != null && !featureLayer.FeatureSource.Projection.IsOpen)
                        {
                            featureLayer.FeatureSource.Projection.Open();
                        }
                    }
                    else if (l is RasterLayer)
                    {
                        RasterLayer rasterLayer = (RasterLayer)l;
                        if (rasterLayer.ImageSource.Projection != null && !rasterLayer.ImageSource.Projection.IsOpen)
                        {
                            rasterLayer.ImageSource.Projection.Open();
                        }
                    }
                });
                if (layers.Any(l =>
                {
                    if (!l.HasBoundingBox) return false;
                    else
                    {
                        FeatureLayer featureLayer = l as FeatureLayer;
                        if (featureLayer != null && featureLayer.FeatureSource.CanGetCountQuickly()) return featureLayer.QueryTools.GetCount() > 0;
                        else return true;
                    }
                }))
                {
                    foreach (Layer layer in layers)
                    {
                        RectangleShape layerExtent = GetVisibleBoundingBox(layer);
                        if (worldExtent == null)
                        {
                            worldExtent = layerExtent;
                        }
                        else if (layerExtent != null)
                        {
                            worldExtent.ExpandToInclude(layerExtent);
                        }
                    }
                }
            }
            finally
            {
                layers.ForEach(l => Monitor.Exit(l));
            }
            return worldExtent;
        }

        private RectangleShape GetVisibleBoundingBox(Layer layer)
        {
            if (layer == null) return null;

            FeatureLayer featureLayer = layer as FeatureLayer;
            RectangleShape extent = new RectangleShape(-180, 90, 180, -90); ;
            if (featureLayer != null)
            {
                if (featureLayer.FeatureSource.CanGetBoundingBoxQuickly())
                {
                    extent = featureLayer.GetBoundingBox();
                }

                if (featureLayer.ZoomLevelSet.CustomZoomLevels.Count == 0)
                {
                    CompositeStyle compositeStyle = new CompositeStyle();
                    compositeStyle.Name = featureLayer.Name + "Style";

                    compositeStyle.Styles.Add(GetDefaultAreaStyle());
                    compositeStyle.Styles.Add(GetDefaultLineStyle());
                    compositeStyle.Styles.Add(GetDefaultPointStyle());

                    foreach (var zoomLevel in this.ZoomLevelSet.GetZoomLevels())
                    {
                        ZoomLevel newZoomLevel = new ZoomLevel(zoomLevel.Scale);
                        newZoomLevel.CustomStyles.Add(compositeStyle);
                        featureLayer.ZoomLevelSet.CustomZoomLevels.Add(newZoomLevel);
                    }
                }

                double layerVisibleUpperScale = (from zoom in featureLayer.ZoomLevelSet.CustomZoomLevels
                                                 where zoom.CustomStyles.Count > 0
                                                 select zoom.Scale).Max();
                double layerVisibleLowerScale = (from zoom in featureLayer.ZoomLevelSet.CustomZoomLevels
                                                 where zoom.CustomStyles.Count > 0
                                                 select zoom.Scale).Min();
                layerVisibleLowerScale = layerVisibleLowerScale * 0.5; // 50% of min scale.

                double layerUpperScale = MapUtils.GetScale(MapUnit, extent, ActualWidth, ActualHeight);
                if (layerVisibleUpperScale < layerUpperScale)
                {
                    extent = MapUtils.CalculateExtent(extent.GetCenterPoint(), layerVisibleUpperScale, MapUnit, ActualWidth, ActualHeight);
                }
                else if (layerVisibleLowerScale > layerUpperScale)
                {
                    // if layer scale is smaller than 50% of min scale, we shouldn't zoom to this scale.
                    extent = null;
                }
            }
            else
            {
                extent = layer.GetBoundingBox();
            }

            return extent;
        }

        private PointStyle GetDefaultPointStyle()
        {
            PointStyle defaultPointStyle = new PointStyle();
            defaultPointStyle.Name = "Point Style";
            defaultPointStyle.SymbolType = PointSymbolType.Circle;
            defaultPointStyle.SymbolSize = 6;
            defaultPointStyle.SymbolSolidBrush = new GeoSolidBrush(GeoColor.FromHtml("#FF4500"));
            defaultPointStyle.SymbolPen = new GeoPen(GeoColor.StandardColors.Black, 1);
            return defaultPointStyle;
        }

        private AreaStyle GetDefaultAreaStyle()
        {
            AreaStyle defaultAreaStyle = new AreaStyle();
            defaultAreaStyle.Name = "Area Style";
            defaultAreaStyle.FillSolidBrush = new GeoSolidBrush(GeoColor.FromHtml("#C0C0C0"));
            defaultAreaStyle.OutlinePen = new GeoPen(GeoColor.FromHtml("#808080"), 1);
            return defaultAreaStyle;
        }

        private LineStyle GetDefaultLineStyle()
        {
            LineStyle lineStyle = new LineStyle(new GeoPen(GeoColor.SimpleColors.Black, 1), new GeoPen(GeoColor.StandardColors.Transparent, 1), new GeoPen(GeoColor.StandardColors.Transparent, 1));
            lineStyle.Name = "Line Style";
            return lineStyle;
        }

        public int GetSnappedZoomLevelIndex(double scale, bool includeTemporaryZoomLevel)
        {
            if (includeTemporaryZoomLevel)
            {
                return GetSnappedZoomLevelIndex(scale);
            }
            else
            {
                if (scale > MaximumScale)
                {
                    scale = MaximumScale;
                }
                else if (scale < MinimumScale)
                {
                    scale = MinimumScale;
                }
                List<double> scales = ZoomLevelSet.GetZoomLevels().Where(z => z.GetType() == typeof(ZoomLevel)).Select(z => z.Scale).ToList();
                return MapUtils.GetSnappedZoomLevelIndex(scale, new Collection<double>(scales));
            }
        }

        public IEnumerable<LayerOverlay> GetOverlaysContaining(Layer layer)
        {
            return from overlay in Overlays.OfType<LayerOverlay>()
                   where overlay.Layers.Contains(layer)
                   select overlay;
        }

        protected override void DrawCore(RectangleShape targetExtent, OverlayRefreshType overlayRefreshType)
        {
            if (fixedAdornmentCanvas != null && !fixedAdornmentCanvas.Children.Contains(fixedAdornmentOverlay.OverlayCanvas))
            {
                Canvas.SetZIndex(fixedAdornmentOverlay.OverlayCanvas, 999);
                fixedAdornmentCanvas.Children.Add(fixedAdornmentOverlay.OverlayCanvas);
            }

            base.DrawCore(targetExtent, overlayRefreshType);

            var arguments = GetMapArguments();
            if (overlayRefreshType == OverlayRefreshType.Redraw)
            {
                fixedAdornmentOverlay.Initialize(arguments);
                fixedAdornmentOverlay.Draw(targetExtent, overlayRefreshType);
            }
        }

        protected override void ZoomToPreviousExtentCore()
        {
            mapNextExtents.Add(CurrentExtent);
            base.ZoomToPreviousExtentCore();
        }

        public void ZoomToNextExtent()
        {
            if (MapNextExtents.Count > 0)
            {
                RectangleShape nextExtent = (RectangleShape)MapNextExtents[MapNextExtents.Count - 1].CloneDeep();
                Draw(nextExtent, OverlayRefreshType.Redraw);

                MapNextExtents.RemoveAt(MapNextExtents.Count - 1);
            }
        }

        public RectangleShape GetClosestExtent(RectangleShape extent, double screenWidth, double screenHeight)
        {
            double scale = GetClosestScale(extent, screenWidth);
            PointShape center = extent.GetCenterPoint();
            return GetRectangle(center, scale, screenWidth, screenHeight);
        }

        private double GetClosestScale(RectangleShape extent, double screenWidth)
        {
            double resolution = extent.Width / screenWidth;
            double scale = MapUtils.GetScaleFromResolution(resolution, MapUnit);

            if (scale > MaximumScale)
            {
                scale = MaximumScale;
            }
            else if (scale < MinimumScale)
            {
                scale = MinimumScale;
            }

            return MapUtils.GetClosestScale(scale, ZoomLevelSet);
        }

        public static GeographyUnit GetGeographyUnit(string projectionString)
        {
            Proj4Projection proj4 = new Proj4Projection();
            proj4.ExternalProjectionParametersString = projectionString;
            return proj4.GetExternalGeographyUnit();
        }

        private RectangleShape GetRectangle(PointShape center, double scale)
        {
            double resolution = MapUtils.GetResolutionFromScale(scale, MapUnit);
            double left = center.X - resolution * ActualWidth * .5;
            double top = center.Y + resolution * ActualHeight * .5;
            double right = center.X + resolution * ActualWidth * .5;
            double bottom = center.Y - resolution * ActualHeight * .5;
            return GetRectangle(center, scale, ActualWidth, ActualHeight);
        }

        private RectangleShape GetRectangle(PointShape center, double scale, double screenWidth, double screenHeight)
        {
            double resolution = MapUtils.GetResolutionFromScale(scale, MapUnit);
            double left = center.X - resolution * screenWidth * .5;
            double top = center.Y + resolution * screenHeight * .5;
            double right = center.X + resolution * screenWidth * .5;
            double bottom = center.Y - resolution * screenHeight * .5;
            return new RectangleShape(left, top, right, bottom);
        }

        private double GetZoomLevelScale(int index)
        {
            double scale;
            if (ZoomLevelSet.CustomZoomLevels.Count == 0)
            {
                Collection<ZoomLevel> zoomLevels = ZoomLevelSet.GetZoomLevels();
                if (zoomLevels.Count <= index)
                {
                    scale = zoomLevels.Min(z => z.Scale);
                }
                else
                {
                    scale = zoomLevels[index].Scale;
                }
            }
            else
            {
                if (index >= ZoomLevelSet.CustomZoomLevels.Count)
                {
                    index = ZoomLevelSet.CustomZoomLevels.Count - 1;
                }

                scale = ZoomLevelSet.CustomZoomLevels[index].Scale;
            }

            return scale;
        }

        private static int GetDrawingProgress(TileOverlay tileOverlay)
        {
            Canvas drawingCanvas = ((Canvas)tileOverlay.OverlayCanvas.Children[0]);
            int total = drawingCanvas.Children.Count;
            int drawn = 0;
            foreach (Wpf.Tile tile in drawingCanvas.Children)
            {
                if (tile.IsOpened) drawn++;
            }

            if (total != 0)
            {
                return drawn * 100 / total;
            }
            else
            {
                return 0;
            }
        }

        public MapArguments GetMapArguments()
        {
            MapArguments mapArgs = null;
            if (InteractiveOverlays.Count > 0 && InteractiveOverlays[0].MapArguments != null)
                mapArgs = InteractiveOverlays[0].MapArguments;
            else
                mapArgs = new MapArguments();
            mapArgs.ActualHeight = ActualHeight;
            mapArgs.ActualWidth = ActualWidth;
            mapArgs.CurrentExtent = (RectangleShape)CurrentExtent.CloneDeep();
            mapArgs.CurrentResolution = CurrentResolution;
            mapArgs.CurrentScale = CurrentScale;
            mapArgs.MapUnit = MapUnit;
            mapArgs.MaximumScale = MaximumScale;
            mapArgs.MinimumScale = MinimumScale;
            mapArgs.MaxExtent = MaxExtent;
            mapArgs.ZoomLevelScales.Clear();
            foreach (var zoomLevel in ZoomLevelSet.GetZoomLevels())
            {
                mapArgs.ZoomLevelScales.Add(zoomLevel.Scale);
            }

            return mapArgs;
        }

        private void RemovedUnExistsShapeFileLayers()
        {
            var excludedLayers = (from layerOverlay in Overlays.OfType<LayerOverlay>()
                                  from layer in layerOverlay.Layers.OfType<ShapeFileFeatureLayer>()
                                  where !File.Exists(layer.ShapePathFilename)
                                  select layer).ToArray();
            Collection<LayerOverlay> excludedLayerOverlays = new Collection<LayerOverlay>();
            foreach (LayerOverlay layerOverlay in Overlays.OfType<LayerOverlay>())
            {
                foreach (var item in excludedLayers)
                {
                    layerOverlay.Layers.Remove(item);
                }
                if (layerOverlay.Layers.Count == 0)
                    excludedLayerOverlays.Add(layerOverlay);
            }

            foreach (var overlay in excludedLayerOverlays)
            {
                Overlays.Remove(overlay);
            }
        }

        private void Overlays_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Overlay overlay in e.NewItems)
                    {
                        overlay.Drawn += overlay_Drawn;
                        TileOverlay tileOverlay = overlay as TileOverlay;
                        if (tileOverlay != null && tileOverlay.TileType != TileType.SingleTile)
                        {
                            tileOverlay.DrawTilesProgressChanged +=
                                new EventHandler<DrawTilesProgressChangedTileOverlayEventArgs>(tileOverlay_DrawTilesProgressChanged);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Overlay overlay in e.OldItems)
                    {
                        overlay.Drawn -= overlay_Drawn;
                        TileOverlay tileOverlay = overlay as TileOverlay;
                        if (tileOverlay != null && tileOverlay.TileType != TileType.SingleTile)
                        {
                            tileOverlay.DrawTilesProgressChanged -=
                                new EventHandler<DrawTilesProgressChangedTileOverlayEventArgs>(tileOverlay_DrawTilesProgressChanged);
                        }
                    }

                    if (Overlays.Count == 0)
                    {
                        SetProgressVisible();
                        progressBar.Value = 100;
                        progressText.Text = String.Format(CultureInfo.InvariantCulture, "{0} %", 100);
                    }

                    break;

                default:
                    break;
            }
        }

        private void tileOverlay_DrawTilesProgressChanged(object sender, DrawTilesProgressChangedTileOverlayEventArgs e)
        {
            TileOverlay currentTileOverlay = (TileOverlay)sender;

            //only those overlays that are in MultiTile mode and visible takes credit
            var tileOverlays = Overlays
                                .OfType<TileOverlay>()
                                .Where((to => to.TileType != TileType.SingleTile && to.IsVisible));

            int total = tileOverlays.Count() * 100;
            int progress = 0;
            foreach (TileOverlay tileOverlay in tileOverlays)
            {
                LayerOverlay layerOverlay = tileOverlay as LayerOverlay;
                if (layerOverlay != null && layerOverlay.Layers.Count == 0)
                {
                    progress += 100;
                }
                else
                {
                    progress += GetDrawingProgress(tileOverlay);
                }
            }

            if (total != 0)
            {
                int currentProgress = progress * 100 / total;
                SetProgressVisible(currentProgress != 100);
                progressBar.Value = currentProgress;
                progressText.Text = String.Format(CultureInfo.InvariantCulture, "{0} %  {1}", currentProgress, currentTileOverlay.Name);
            }
        }

        private void overlay_Drawn(object sender, DrawnOverlayEventArgs e)
        {
            IsMapStateChanged = true;
        }

        private void InitializeProgressBar()
        {
            Overlays.CollectionChanged += Overlays_CollectionChanged;

            progressBar = new ProgressBar();
            progressBar.Opacity = .8;
            progressBar.SetValue(Canvas.ZIndexProperty, 1000);

            progressText = new TextBlock();
            progressText.Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 64, 64));
            progressText.FontSize = 12;
            progressText.VerticalAlignment = VerticalAlignment.Center;
            progressText.FontWeight = FontWeights.Bold;
            progressText.FontFamily = new FontFamily("ARIAL");
            //progressText.Effect = new DropShadowEffect() { Color = Colors.Black, BlurRadius = 1, Opacity = .8, ShadowDepth = 1 };
            progressText.SetValue(Canvas.ZIndexProperty, 1001);
            progressText.Opacity = .8;
            progressText.Margin = new Thickness(5, 1, 0, 0);

            cancelImage = new Image();
            cancelImage.MouseDown += new MouseButtonEventHandler(CancelImage_MouseDown);
            cancelImage.Stretch = Stretch.None;
            cancelImage.Cursor = Cursors.Arrow;
            cancelImage.Width = 16;
            cancelImage.Height = 16;
            cancelImage.HorizontalAlignment = HorizontalAlignment.Right;
            cancelImage.Source = new BitmapImage(new Uri("/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Images/Redraw_Cancel_Button.png", UriKind.Relative));
            Grid.SetColumn(cancelImage, 2);

            progressGrid = new Grid();
            progressGrid.SnapsToDevicePixels = true;
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition());
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5) });
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(14) });
            progressGrid.Width = 150;
            progressGrid.Height = 16;
            progressGrid.Margin = new Thickness(10);
            progressGrid.HorizontalAlignment = HorizontalAlignment.Center;
            progressGrid.VerticalAlignment = VerticalAlignment.Bottom;
            progressGrid.Children.Add(progressBar);
            progressGrid.Children.Add(progressText);
            progressGrid.Children.Add(cancelImage);
            SetProgressVisible();
        }

        private void CancelImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Overlays.OfType<TileOverlay>().ForEach(tempOverlay => { tempOverlay.Close(); });
        }

        private void SetProgressVisible(bool visible = false)
        {
            progressGrid.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            isBusy = visible;
        }

        private void InitializeNotifyLegendChanging()
        {
            AdornmentOverlay.Drawn += new EventHandler<DrawnOverlayEventArgs>(AdornmentOverlay_Drawn);
        }

        private void AdornmentOverlay_Drawn(object sender, DrawnOverlayEventArgs e)
        {
            IsMapStateChanged = true;
        }

        private void ExtentOverlay_MapMouseUp(object sender, MapMouseUpInteractiveOverlayEventArgs e)
        {
            if (e.InteractionArguments.MouseButton == MapMouseButton.Right && ExtentOverlay.ExtentChangedType == ExtentChangedType.TrackZoomIn)
            {
                IEnumerable<ZoomLevel> deleteZoomLevels = ZoomLevelSet.CustomZoomLevels.Where(c => c is PreciseZoomLevel).ToArray();
                foreach (var item in deleteZoomLevels)
                {
                    ZoomLevelSet.CustomZoomLevels.Remove(item);
                }

                PointShape mouseUpPoint = new PointShape(e.InteractionArguments.ScreenX, e.InteractionArguments.ScreenY);
                double trackWidth = Math.Abs(mouseUpPoint.X - mouseDownPoint.X);
                double trackHeight = e.InteractionArguments.MapHeight * trackWidth / e.InteractionArguments.MapWidth;

                if (mouseUpPoint.Y < mouseDownPoint.Y)
                {
                    mouseUpPoint = new PointShape(mouseUpPoint.X, mouseDownPoint.Y - trackHeight);
                }
                else
                {
                    mouseUpPoint = new PointShape(mouseUpPoint.X, mouseDownPoint.Y + trackHeight);
                }

                PointShape mouseDownWorldPoint = ToWorldCoordinate(mouseDownPoint);
                PointShape mouseUpWorldPoint = ToWorldCoordinate(mouseUpPoint);

                double minX = Math.Min(mouseUpWorldPoint.X, mouseDownWorldPoint.X);
                double minY = Math.Min(mouseUpWorldPoint.Y, mouseDownWorldPoint.Y);
                double maxX = Math.Max(mouseUpWorldPoint.X, mouseDownWorldPoint.X);
                double maxY = Math.Max(mouseUpWorldPoint.Y, mouseDownWorldPoint.Y);
                RectangleShape boundingBox = new RectangleShape(minX, maxY, maxX, minY);
                //double scale = MapUtils.GetScale(MapUnit, boundingBox, e.InteractionArguments.MapWidth, e.InteractionArguments.MapHeight);
                double scale = ExtentHelper.GetScale(boundingBox, e.InteractionArguments.MapWidth, MapUnit);
                PreciseZoomLevel zoomLevel = new PreciseZoomLevel(scale);

                int index = 0;
                if (scale < ZoomLevelSet.CustomZoomLevels.Last().Scale)
                {
                    index = ZoomLevelSet.CustomZoomLevels.Count;
                }
                else
                {
                    foreach (var item in ZoomLevelSet.CustomZoomLevels)
                    {
                        if (item.Scale < scale)
                        {
                            index = ZoomLevelSet.CustomZoomLevels.IndexOf(item);
                            break;
                        }
                    }
                }

                ZoomLevelSet.CustomZoomLevels.Insert(index, zoomLevel);
                ExtentOverlay.MapArguments.ZoomLevelScales.Clear();
                foreach (var tempScale in ZoomLevelSet.CustomZoomLevels.Select(z => z.Scale))
                {
                    ExtentOverlay.MapArguments.ZoomLevelScales.Add(tempScale);
                }
            }

            if (previousCursor != null)
            {
                Cursor = previousCursor;
                previousCursor = null;
            }
        }

        private void ExtentOverlay_MapMouseDown(object sender, MapMouseDownInteractiveOverlayEventArgs e)
        {
            mouseDownPoint = new PointShape(e.InteractionArguments.ScreenX, e.InteractionArguments.ScreenY);
            if (Keyboard.IsKeyDown(Key.Space))
            {
                previousCursor = Cursor;
                Cursor = GisEditorCursors.Grab;
            }
        }

        private void OnDisplayProjectionParametersChanged(string oldProjectionParameters, string newProjectionParameters)
        {
            EventHandler<DisplayProjectionParametersChangedGisEditorWpfMapEventArgs> handler = DisplayProjectionParametersChanged;
            if (handler != null)
            {
                handler(this, new DisplayProjectionParametersChangedGisEditorWpfMapEventArgs(oldProjectionParameters, newProjectionParameters));
            }
        }

        private void GisEditorWpfMap_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    Pan(PanDirection.Left, panPercentage);
                    break;

                case Key.Right:
                    Pan(PanDirection.Right, panPercentage);
                    break;

                case Key.Up:
                    Pan(PanDirection.Up, panPercentage);
                    break;

                case Key.Down:
                    Pan(PanDirection.Down, panPercentage);
                    break;
            }
        }

        [OnGeoserializing]
        private void OnSerializingInternal()
        {
            extentOverlay = ExtentOverlay;
            name = Name;
            var bingMapsOverlays = Overlays.OfType<BingMapsOverlay>().Where(o => !string.IsNullOrEmpty(o.ApplicationId)).ToList();
            if (bingMapsOverlays.Count > 0)
            {
                bingMapsApplicationIds.Clear();
                foreach (var bingMapsOverlay in bingMapsOverlays)
                {
                    bingMapsApplicationIds.Add(bingMapsOverlay, bingMapsOverlay.ApplicationId);
                    bingMapsOverlay.ApplicationId = string.Empty;
                }
            }
        }

        [OnGeoserialized]
        private void OnSerializedInternal()
        {
            var bingMapsOverlays = Overlays.OfType<BingMapsOverlay>().ToArray();
            foreach (var bingMapsOverlay in bingMapsOverlays)
            {
                string applicationId = string.Empty;
                if (bingMapsApplicationIds.ContainsKey(bingMapsOverlay))
                { applicationId = bingMapsApplicationIds[bingMapsOverlay]; }

                bingMapsOverlay.ApplicationId = applicationId;
            }
            bingMapsApplicationIds.Clear();
        }

        [OnGeodeserialized]
        private void OnDeserializedInternal()
        {
            Name = name;
            MapTools.PanZoomBar.IsEnabled = false;
            MapTools.Logo.IsEnabled = false;

            if (extentOverlay != null)
            {
                extentOverlay.MapMouseDown -= new EventHandler<MapMouseDownInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseDown);
                extentOverlay.MapMouseDown += new EventHandler<MapMouseDownInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseDown);
                extentOverlay.MapMouseUp -= new EventHandler<MapMouseUpInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseUp);
                extentOverlay.MapMouseUp += new EventHandler<MapMouseUpInteractiveOverlayEventArgs>(ExtentOverlay_MapMouseUp);
                ExtentOverlay = extentOverlay;
            }

            if (Overlays.Count > 0)
            {
                Overlays.CollectionChanged -= Overlays_CollectionChanged;
                Overlays.CollectionChanged += Overlays_CollectionChanged;
                var tmpOverlays = new Dictionary<string, Overlay>();
                foreach (var key in Overlays.GetKeys())
                {
                    tmpOverlays.Add(key, Overlays[key]);
                }
                Overlays.Clear();
                foreach (var tmpOverlay in tmpOverlays)
                {
                    Overlays.Add(tmpOverlay.Key, tmpOverlay.Value);
                }
            }

            FixWrongIndexFilePathIssue();
            FixZoomLevelIssue();
            FixBaseMaps();
            FixLoadingTiffLibrary();
            FixUnExistCaches();
            FixUnClosedInstance();
            ResetMapVersion();
        }

        private void FixWrongIndexFilePathIssue()
        {
            Collection<FeatureLayer> layers = GetFeatureLayers(false);
            foreach (var featureLayer in layers.OfType<ShapeFileFeatureLayer>())
            {
                string indexFilePath = featureLayer.IndexPathFilename;
                if (!File.Exists(indexFilePath)
                    && indexFilePath.Contains("_tmp.idx"))
                {
                    indexFilePath = indexFilePath.Replace("_tmp.idx", ".idx");
                    string indexFileFolder = Path.GetDirectoryName(indexFilePath);
                    string shapeFileFolder = Path.GetDirectoryName(featureLayer.ShapePathFilename);
                    if (File.Exists(indexFilePath)
                        && indexFileFolder.Equals(shapeFileFolder))
                    {
                        featureLayer.IndexPathFilename = indexFilePath;
                    }
                }
            }
        }

        private void FixUnClosedInstance()
        {
            Collection<FeatureLayer> layers = GetFeatureLayers(false);
            foreach (var featureLayer in layers.Where(l => l.IsVisible))
            {
                featureLayer.Close();
                featureLayer.SafeProcess(() =>
                {
                    if (featureLayer.FeatureSource != null && featureLayer.FeatureSource.IsOpen)
                    {
                        featureLayer.FeatureSource.Close();
                    }
                    if (featureLayer.FeatureSource.Projection != null && featureLayer.FeatureSource.Projection.IsOpen)
                    {
                        featureLayer.FeatureSource.Projection.Close();
                    }
                });
            }
        }

        private void FixUnExistCaches()
        {
            foreach (var overlay in Overlays.OfType<TileOverlay>())
            {
                FileBitmapTileCache tileCache = overlay.TileCache as FileBitmapTileCache;
                if (tileCache != null && !Directory.Exists(tileCache.CacheDirectory))
                {
                    overlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                }
            }
        }

        /// <summary>
        /// Fix Loading Tiff Library
        /// </summary>
        private void FixLoadingTiffLibrary()
        {
            if (System.Environment.OSVersion.Version.Major <= 5)
            {
                foreach (var tiffRasterLayer in Overlays.OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<GeoTiffRasterLayer>())
                {
                    //tiffRasterLayer.LibraryType = GeoTiffLibraryType.ManagedLibTiff;
                }
            }
        }

        /// <summary>
        /// Fix CompositeStyles for featureLayer
        /// </summary>
        private void FixCompositeStyles()
        {
            foreach (var featureLayer in Overlays.OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<FeatureLayer>())
            {
                var replacedStyles = new Collection<CompositeStyle>();

                foreach (var zoomLevel in featureLayer.ZoomLevelSet.GetZoomLevels())
                {
                    for (int i = 0; i < zoomLevel.CustomStyles.Count; i++)
                    {
                        var style = zoomLevel.CustomStyles[i];

                        if (!(style is CompositeStyle))
                        {
                            var comopsiteStyle = replacedStyles.Where(s => s.Styles[0] == style).FirstOrDefault();

                            if (comopsiteStyle == null)
                            {
                                comopsiteStyle = new CompositeStyle(style);
                                replacedStyles.Add(comopsiteStyle);
                            }

                            var index = zoomLevel.CustomStyles.IndexOf(style);
                            zoomLevel.CustomStyles.Remove(style);
                            zoomLevel.CustomStyles.Insert(index, comopsiteStyle);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Just fix Bing maps and world map kit
        /// </summary>
        private void FixBaseMaps()
        {
            var oldWmkOverlay = Overlays.OfType<WorldMapKitWmsWpfOverlay>().FirstOrDefault();
            if (oldWmkOverlay != null)
            {
                var newWmkOverlay = new WorldMapKitMapOverlay(oldWmkOverlay.ClientId, oldWmkOverlay.PrivateKey);
                newWmkOverlay.TileType = oldWmkOverlay.TileType;
                newWmkOverlay.Name = oldWmkOverlay.Name;
                newWmkOverlay.DrawingExceptionMode = oldWmkOverlay.DrawingExceptionMode;
                newWmkOverlay.TileBuffer = oldWmkOverlay.TileBuffer;
                newWmkOverlay.TileCache = oldWmkOverlay.TileCache;
                newWmkOverlay.Projection = (Layers.WorldMapKitProjection)oldWmkOverlay.Projection;
                newWmkOverlay.MapArguments = oldWmkOverlay.MapArguments;

                var index = Overlays.IndexOf(oldWmkOverlay);
                Overlays.Insert(index, newWmkOverlay);
                Overlays.RemoveAt(index + 1);
            }

            var oldBingMapsTileOverlay = Overlays.OfType<BingMapsTileOverlay>().FirstOrDefault();
            if (oldBingMapsTileOverlay != null)
            {
                var newBingMapsOverlay = new BingMapsOverlay(oldBingMapsTileOverlay.ApplicationId, oldBingMapsTileOverlay.MapStyle);
                newBingMapsOverlay.Logo = null;
                newBingMapsOverlay.Name = oldBingMapsTileOverlay.Name;
                newBingMapsOverlay.TileType = oldBingMapsTileOverlay.TileType;
                newBingMapsOverlay.DrawingExceptionMode = oldBingMapsTileOverlay.DrawingExceptionMode;
                newBingMapsOverlay.TileCache = oldBingMapsTileOverlay.TileCache;
                newBingMapsOverlay.MapArguments = oldBingMapsTileOverlay.MapArguments;

                var index = Overlays.IndexOf(newBingMapsOverlay);
                Overlays.Insert(index, newBingMapsOverlay);
                Overlays.RemoveAt(index + 1);
            }

            var oldBingMapsOverlays = Overlays.OfType<BingMapsOverlay>();
            foreach (var bingMapsOverlay in oldBingMapsOverlays)
            {
                bingMapsOverlay.Logo = null;
            }
        }

        private void FixZoomLevelIssue()
        {
            var allFeatureLayers = GetFeatureLayers(false);
            foreach (var featureLayer in allFeatureLayers)
            {
                if (ZoomLevelSet.CustomZoomLevels.Count > featureLayer.ZoomLevelSet.CustomZoomLevels.Count)
                {
                    var minScale = featureLayer.ZoomLevelSet.CustomZoomLevels[featureLayer.ZoomLevelSet.CustomZoomLevels.Count - 1].Scale;
                    var missingZoomLevels = ZoomLevelSet.CustomZoomLevels.Skip(featureLayer.ZoomLevelSet.CustomZoomLevels.Count).ToArray();
                    foreach (var missingZoomLevel in missingZoomLevels)
                    {
                        featureLayer.ZoomLevelSet.CustomZoomLevels.Add(missingZoomLevel);
                    }
                }

                var customZoomLevels = new Collection<ZoomLevel>();
                foreach (var item in featureLayer.ZoomLevelSet.CustomZoomLevels.Where(z => z.CustomStyles.Count > 0 && z.ApplyUntilZoomLevel != ApplyUntilZoomLevel.None))
                {
                    customZoomLevels.Add(item);
                }
                if (customZoomLevels.Count == 0) continue;
                featureLayer.ZoomLevelSet.CustomZoomLevels.Clear();
                foreach (var item in ZoomLevelSet.GetZoomLevels())
                {
                    featureLayer.ZoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(item.Scale));
                }
                foreach (var item in customZoomLevels)
                {
                    var from = GetSnappedZoomLevelIndex(item.Scale) + 1;
                    var to = (int)item.ApplyUntilZoomLevel;
                    for (int i = from - 1; i < to; i++)
                    {
                        foreach (var style in item.CustomStyles)
                        {
                            featureLayer.ZoomLevelSet.CustomZoomLevels[i].CustomStyles.Add(style);
                        }
                    }
                }
            }
        }

        private void MapLoaded(object sender, RoutedEventArgs e)
        {
            var senderMap = sender as GisEditorWpfMap;
            senderMap.Focus();
            if (!senderMap.IsMapLoaded)
            {
                senderMap.CurrentExtent = new RectangleShape(0, 1, 1, 0);
                senderMap.IsMapLoaded = true;
            }

            senderMap.Loaded -= MapLoaded;
        }

        private void ResetMapVersion()
        {
            string mapSuiteCorePathFileName = typeof(BaseShape).Assembly.Location;
            version = FileVersionInfo.GetVersionInfo(mapSuiteCorePathFileName).FileVersion;
        }
    }
}