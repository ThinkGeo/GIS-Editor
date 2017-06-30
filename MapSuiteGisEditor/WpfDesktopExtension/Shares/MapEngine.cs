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
using System.Drawing;
using System.IO;
using System.Reflection;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Portable;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite
{
    /// <summary>This class is for generating maps.</summary>
    /// <remarks>
    /// The MapEngine class is similar to a Map Control in Map Suite Desktop Edition or Web
    /// Edition.
    /// </remarks>
    [Serializable]
    public class MapEngine
    {
        private bool showLogo;
        private RectangleShape currentExtent;
        private GeoBrush backgroundFillBrush;
        private GeoCanvas canvas;
        private Collection<SimpleCandidate> labeledFeaturesInLayers;
        private GeoCollection<Layer> staticLayers;
        private GeoCollection<Layer> dynamicLayers;
        private GeoCollection<AdornmentLayer> adornmentLayers;

        /// <summary>This event is raised before Layers are drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<LayersDrawingEventArgs> LayersDrawing;

        /// <summary>This event is raised after Layers are drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<LayersDrawnEventArgs> LayersDrawn;

        /// <summary>This event is raised before a Layer is drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<LayerDrawingEventArgs> LayerDrawing;

        /// <summary>This event is raised after a Layer is drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<LayerDrawnEventArgs> LayerDrawn;

        /// <summary>This event is raised before AdornmentLayers are drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<AdornmentLayersDrawingEventArgs> AdornmentLayersDrawing;

        /// <summary>This event is raised after AdornmentLayers are drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<AdornmentLayersDrawnEventArgs> AdornmentLayersDrawn;

        /// <summary>This event is raised before an AdornmentLayer is drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<AdornmentLayerDrawingEventArgs> AdornmentLayerDrawing;

        /// <summary>This event is raised after an AdornmentLayer is drawn.</summary>
        [field: NonSerialized]
        public event EventHandler<AdornmentLayerDrawnEventArgs> AdornmentLayerDrawn;

        /// <summary>Create a new instance of the <strong>MapEngine</strong>.</summary>
        public MapEngine()
        {
            backgroundFillBrush = new GeoSolidBrush(GeoColor.StandardColors.Transparent);
            labeledFeaturesInLayers = new Collection<SimpleCandidate>();
            staticLayers = new GeoCollection<Layer>();
            dynamicLayers = new GeoCollection<Layer>();
            adornmentLayers = new GeoCollection<AdornmentLayer>();
            canvas = GeoCanvas.CreatePlatformGeoCanvas();
            currentExtent = new RectangleShape();
        }

        /// <summary>
        /// This event is raised before AdornmentLayers are drawn.
        /// </summary>
        /// <param name="e">The AdornmentLayersDrawingEventArgs passed for the event raised.</param>
        protected virtual void OnAdornmentLayersDrawing(AdornmentLayersDrawingEventArgs e)
        {
            EventHandler<AdornmentLayersDrawingEventArgs> handler = AdornmentLayersDrawing;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised after AdornmentLayers are drawn.
        /// </summary>
        /// <param name="e">The AdornmentLayersDrawnEventArgs passed for the event raised.</param>
        protected virtual void OnAdornmentLayersDrawn(AdornmentLayersDrawnEventArgs e)
        {
            EventHandler<AdornmentLayersDrawnEventArgs> handler = AdornmentLayersDrawn;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised before an AdornmentLayer is drawn.
        /// </summary>
        /// <param name="e">The AdornmentLayerDrawingEventArgs passed for the event raised.</param>
        protected virtual void OnAdornmentLayerDrawing(AdornmentLayerDrawingEventArgs e)
        {
            EventHandler<AdornmentLayerDrawingEventArgs> handler = AdornmentLayerDrawing;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised after an AdornmentLayer is drawn.
        /// </summary>
        /// <param name="e">The AdornmentLayerDrawnEventArgs passed for the event raised.</param>
        protected virtual void OnAdornmentLayerDrawn(AdornmentLayerDrawnEventArgs e)
        {
            EventHandler<AdornmentLayerDrawnEventArgs> handler = AdornmentLayerDrawn;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised before Layers are drawn.
        /// </summary>
        /// <param name="e">The LayersDrawingEventArgs passed for the event raised.</param>
        protected virtual void OnLayersDrawing(LayersDrawingEventArgs e)
        {
            EventHandler<LayersDrawingEventArgs> handler = LayersDrawing;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised after Layers are drawn.
        /// </summary>
        /// <param name="e">The LayersDrawnEventArgs passed for the event raised.</param>
        protected virtual void OnLayersDrawn(LayersDrawnEventArgs e)
        {
            EventHandler<LayersDrawnEventArgs> handler = LayersDrawn;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised before a Layer is drawn.
        /// </summary>
        /// <param name="e">The LayerDrawingEventArgs passed for the event raised.</param>
        protected virtual void OnLayerDrawing(LayerDrawingEventArgs e)
        {
            EventHandler<LayerDrawingEventArgs> handler = LayerDrawing;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This event is raised after a Layer is drawn.
        /// </summary>
        /// <param name="e">The LayerDrawnEventArgs passed for the event raised.</param>
        protected virtual void OnLayerDrawn(LayerDrawnEventArgs e)
        {
            EventHandler<LayerDrawnEventArgs> handler = LayerDrawn;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>Gets and sets the <strong>GeoCanvas</strong> used to draw the Layers.</summary>
        public GeoCanvas Canvas
        {
            get { return canvas; }
            set { canvas = value; }
        }

        /// <summary>This property specifies whether the logo is shown on the Map or not.</summary>
        public bool ShowLogo
        {
            get { return showLogo; }
            set { showLogo = value; }
        }

        /// <summary>
        /// This property holds a collection of AdornmentLayers to be drawn on the
        /// <strong>MapEngine</strong>.
        /// </summary>
        /// <remarks>
        /// This collection of Layers <strong>StaticLayers</strong> will be drawn when
        /// calling the <strong>DrawAdornmentLayers</strong> API.
        /// </remarks>
        public GeoCollection<AdornmentLayer> AdornmentLayers
        {
            get { return adornmentLayers; }
        }

        /// <summary>This property gets or sets the current extent of the MapEngine.</summary>
        /// <remarks>
        /// The current extent is the rectangle that is currently being shown on the
        /// MapEngine.
        /// </remarks>
        public RectangleShape CurrentExtent
        {
            get { return currentExtent; }
            set { currentExtent = value; }
        }

        /// <summary>
        /// This property holds a group of Layers to be drawn on the
        /// <strong>MapEngine</strong>.
        /// </summary>
        /// <remarks>
        /// This collection of Layers <strong>StaticLayers</strong> will be drawn when
        /// calling the <strong>DrawStaticLayers</strong> API.
        /// </remarks>
        public GeoCollection<Layer> StaticLayers
        {
            get { return staticLayers; }
        }

        /// <summary>
        /// This property holds a group of Layers to be drawn on the
        /// <strong>MapEngine</strong>.
        /// </summary>
        /// <remarks>
        /// This collection of Layers <strong>StaticLayers</strong> will be drawn when
        /// calling the <strong>DrawDynamicLayers</strong> API.
        /// </remarks>
        public GeoCollection<Layer> DynamicLayers
        {
            get { return dynamicLayers; }
        }

        /// <summary>
        /// Gets or sets the <strong>GeoBrush</strong> for the background of the
        /// <strong>MapEngine</strong>.
        /// </summary>
        public GeoBrush BackgroundFillBrush
        {
            get { return backgroundFillBrush; }
            set { backgroundFillBrush = value; }
        }

        /// <summary>
        /// Finds a feature layer by key (specified in the "name" parameter) within the collection of
        /// StaticLayers.
        /// </summary>
        /// <returns>The corresponding FeatureLayer with the specified key in the MapControl.</returns>
        /// <param name="name">The key to find the final result feature layer.</param>
        public FeatureLayer FindStaticFeatureLayer(string name)
        {
            FeatureLayer featureLayer = null;

            if (staticLayers.Contains(name))
            {
                featureLayer = staticLayers[name] as FeatureLayer;
            }
            return featureLayer;
        }

        /// <summary>
        /// Find the raster layer by key (specified in the "name" parameter) within the collection of
        /// StaticLayers.
        /// </summary>
        /// <returns>The corresponding RasterLayer with the passing specified in the MapControl.</returns>
        /// <param name="name">The key to find the final result raster layer.</param>
        public RasterLayer FindStaticRasterLayer(string name)
        {
            RasterLayer rasterLayer = null;

            if (staticLayers.Contains(name))
            {
                rasterLayer = staticLayers[name] as RasterLayer;
            }

            return rasterLayer;
        }

        /// <summary>
        /// Find the feature layer by key (specified in the "name" parameter) within the collection of
        /// DynamicLayers.
        /// </summary>
        /// <returns>The corresponding FeatureLayer with the specified key in the MapControl.</returns>
        /// <param name="name">The key to find the final result feature layer.</param>
        public FeatureLayer FindDynamicFeatureLayer(string name)
        {
            FeatureLayer featureLayer = null;

            if (dynamicLayers.Contains(name))
            {
                featureLayer = dynamicLayers[name] as FeatureLayer;
            }

            return featureLayer;
        }

        /// <summary>
        /// Find the raster layer by key (specified in the "name" parameter) within the collection of
        /// DynamicLayers.
        /// </summary>
        /// <returns>The corresponding RasterLayer with the specified key in the MapControl.</returns>
        /// <param name="name">The key to find the final result raster layer.</param>
        public RasterLayer FindDynamicRasterLayer(string name)
        {
            RasterLayer rasterLayer = null;

            if (dynamicLayers.Contains(name))
            {
                rasterLayer = dynamicLayers[name] as RasterLayer;
            }

            return rasterLayer;
        }

        // This function takes a height and width in screen coordinates, then
        // looks at the current extent and returns an adjusted world rectangle
        // so that the ratio to height and width in screen and world coordinates match.
        /// <summary>
        /// This method returns an adjusted extent based on the ratio of the screen width and
        /// height.
        /// </summary>
        /// <returns>
        /// This method returns an adjusted extent based on the ratio of the screen width and
        /// height.
        /// </returns>
        /// <remarks>
        /// This function is used because the extent to draw must be the rame ratio as the screen
        /// width and height. If they are not, then the image drawn will be stretched or compressed.
        /// We always adjust the extent upwards to ensure that no matter how we adjust it,
        /// the original extent will fit within the new extent. This ensures that everything
        /// you wanted to see in the first extent is visible and maybe a bit more.
        /// </remarks>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public RectangleShape GetDrawingExtent(float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return GetDrawingExtent(currentExtent, screenWidth, screenHeight);
        }

        /// <summary>
        /// This method returns an adjusted extent based on the ratio of the screen width and
        /// height.
        /// </summary>
        /// <returns>
        /// This method returns an adjusted extent based on the ratio of the screen width and
        /// height.
        /// </returns>
        /// <remarks>
        /// This function is used because the extent to draw must be the rame ratio as the screen
        /// width and height. If they are not, then the image drawn will be stretched or compressed.
        /// We always adjust the extent upwards to ensure that no matter how we adjust it,
        /// the original extent will fit within the new extent. This ensures that everything
        /// you wanted to see in the first extent is visible and maybe a bit more.
        ///
        /// This function takes a height and width in screen coordinates, then
        /// looks at a world extent passed, and returns an adjusted world rectangle
        /// so that the ratio to height and width in screen and world coordinates match.
        /// </remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to adjust for drawing.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape GetDrawingExtent(RectangleShape worldExtent, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckObjectIsNotNull(worldExtent, "worldExtent");
            ValidatorHelper.CheckShapeIsValidForOperation(worldExtent);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.GetDrawingExtent(worldExtent, screenWidth, screenHeight);
        }

        //  This function allows you to open all of the layers (either static or dynamic)
        //  and close their FeatureSources.  This is handy in Map Suite Web Edition, where in between
        //  web requests we need to close all connections because we will get serialized.
        /// <summary>
        /// This API allows you to open all of the layers (either static or dynamic).
        /// </summary>
        public void OpenAllLayers()
        {
            foreach (Layer layer in staticLayers)
            {
                if (!layer.IsOpen) { layer.Open(); }
            }

            foreach (Layer layer in dynamicLayers)
            {
                if (!layer.IsOpen) { layer.Open(); }
            }

            foreach (Layer layer in adornmentLayers)
            {
                if (!layer.IsOpen) { layer.Open(); }
            }
        }

        //  This function allows you to close all of the layers (either static or dynamic)
        //  and close their FeatureSources.  This is handy in Map Suite Web Edition, where in between
        //  web requests we need to close all connections because we will get serialized.
        /// <summary>
        /// This API allows you close all of the layers (either static or dynamic).
        /// </summary>
        public void CloseAllLayers()
        {
            foreach (Layer layer in staticLayers)
            {
                if (layer.IsOpen) { layer.Close(); }
            }

            foreach (Layer layer in dynamicLayers)
            {
                if (layer.IsOpen) { layer.Close(); }
            }

            foreach (Layer layer in adornmentLayers)
            {
                if (layer.IsOpen) { layer.Close(); }
            }
        }

        /// <summary>This is a static function that allows you to pass in a world rectangle, a world point to center on, and a height
        /// and width in screen units.  The function will center the rectangle based on the point, then adjust the rectangle's
        /// ratio based on the height and width in screen coordinates.
        ///</summary>
        /// <overloads>This overload allows you to pass in a world point as the center.</overloads>
        /// <returns>This method returns an adjusted extent centered on a point.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the current extent you want to center.</param>
        /// <param name="worldPoint">This parameter is the world point you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape CenterAt(RectangleShape worldExtent, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckShapeIsValidForOperation(worldPoint);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.CenterAt(worldExtent, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This is a function that allows you to pass a world point to center on and a height
        /// and width in screen units.  The function will update the current extent by centering on the point and
        /// adjusting its ratio based on the height and width in screen coordinates.
        /// </summary>
        /// <returns>None.</returns>
        /// <remarks>This API will update the <strong>CurrentExtent</strong>.</remarks>
        /// <param name="worldPoint">This parameter is the world point you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void CenterAt(PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckShapeIsValidForOperation(worldPoint);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = CenterAt(currentExtent, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This is a static function that allows you to pass in a world rectangle, a world point to center on, and a height
        /// and width in screen units.  The function will center the rectangle based on the point, then adjust the rectangle's
        /// ratio based on the height and width in screen coordinates.
        ///</summary>
        /// <overloads>This overload allows you to pass in a world point as the center.</overloads>
        /// <returns>This method returns an adjusted extent centered on a point.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the current extent you want to center.</param>
        /// <param name="centerFeature">This parameter is the world point feature you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape CenterAt(RectangleShape worldExtent, Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.CenterAt(worldExtent, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This is a function that allows you to pass in a feature to center on, as well as a height
        /// and width in screen units.  The function will center the CurrentExtent based on the specified feature
        /// and adjust its ratio based on the height and width in screen coordinates.
        ///</summary>
        /// <returns>None.</returns>
        /// <remarks>This API will update the <strong>CurrentExtent</strong>.</remarks>
        /// <param name="centerFeature">This parameter is the world point feature you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void CenterAt(Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = CenterAt(currentExtent, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This is a static function that allows you to pass in a world rectangle, a point in screen coordinates
        /// to center on, and a height and width in screen units.  The function will center the rectangle based on the
        /// screen point, then adjust the rectangle's ratio based on the height and width in screen coordinates.
        ///</summary>
        /// <overloads>This overload allows you to pass in a screen point as the center.</overloads>
        /// <returns>This method returns an adjusted extent centered on a point.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the current extent you want to center.</param>
        /// <param name="screenX">This parameter is the X coordinate on the screen to center to.</param>
        /// <param name="screenY">This parameter is the Y coordinate on the screen to center to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape CenterAt(RectangleShape worldExtent, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.CenterAt(worldExtent, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This is a function that allows you to pass a screen point to center on and a height
        /// and width in screen units.  The function will update the current extent by centering on the point and
        /// adjusting its ratio based on the height and width in screen coordinates.
        ///</summary>
        /// <overloads>This overload allows you to pass in a screen point as the center.</overloads>
        /// <remarks>This API will update the <strong>CurrentExtent</strong>.</remarks>
        /// <param name="screenX">This parameter is the X coordinate on the screen to center on.</param>
        /// <param name="screenY">This parameter is the Y coordinate on the screen to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void CenterAt(float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = CenterAt(currentExtent, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method returns the number of pixels between two world points.</summary>
        /// <returns>This method returns the number of pixels between two world points.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="worldPoint1">This parameter is the first point -- the one you want to measure from.</param>
        /// <param name="worldPoint2">This parameter is the second point -- the one you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static float GetScreenDistanceBetweenTwoWorldPoints(RectangleShape worldExtent, PointShape worldPoint1, PointShape worldPoint2, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckObjectIsNotNull(worldPoint1, "worldPoint1");
            ValidatorHelper.CheckObjectIsNotNull(worldPoint2, "worldPoint2");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.GetScreenDistanceBetweenTwoWorldPoints(worldExtent, worldPoint1, worldPoint2, screenWidth, screenHeight);
        }

        /// <summary>This method returns the number of pixels between two world points using the CurrentExtent as reference.</summary>
        /// <returns>This method returns the number of pixels between two world points.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldPoint1">This parameter is the first point -- the one you want to measure from.</param>
        /// <param name="worldPoint2">This parameter is the second point -- the one you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public float GetScreenDistanceBetweenTwoWorldPoints(PointShape worldPoint1, PointShape worldPoint2, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckObjectIsNotNull(worldPoint1, "worldPoint1");
            ValidatorHelper.CheckObjectIsNotNull(worldPoint2, "worldPoint2");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return GetScreenDistanceBetweenTwoWorldPoints(currentExtent, worldPoint1, worldPoint2, screenWidth, screenHeight);
        }

        /// <summary>This method returns the number of pixels between two world points using the CurrentExtent as reference.</summary>
        /// <returns>This method returns the number of pixels between two world points.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldPointFeature1">This parameter is the first pointFeture -- the one you want to measure from.</param>
        /// <param name="worldPointFeature2">This parameter is the second pointFeature -- the one you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public float GetScreenDistanceBetweenTwoWorldPoints(Feature worldPointFeature1, Feature worldPointFeature2, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return GetScreenDistanceBetweenTwoWorldPoints(currentExtent, worldPointFeature1, worldPointFeature2, screenWidth, screenHeight);
        }

        /// <summary>This method returns the number of pixels between two world points.</summary>
        /// <returns>This method returns the number of pixels between two world points.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="worldPointFeature1">This parameter is the first point Feature -- the one you want to measure from.</param>
        /// <param name="worldPointFeature2">This parameter is the second point Feature -- the one you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static float GetScreenDistanceBetweenTwoWorldPoints(RectangleShape worldExtent, Feature worldPointFeature1, Feature worldPointFeature2, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.GetScreenDistanceBetweenTwoWorldPoints(worldExtent, worldPointFeature1, worldPointFeature2, screenWidth, screenHeight);
        }

        /// <summary>This method returns the distance in world units between two screen points.</summary>
        /// <overloads>This overload allows you to pass in ScreenPointF as the points.</overloads>
        /// <returns>This method returns the distance in wold units between two screen points.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="screenPoint1">This is the screen point you want to measure from.</param>
        /// <param name="screenPoint2">This is the screen point you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        /// <param name="worldExtentUnit">This is the geographic unit of the world extent rectangle.</param>
        /// <param name="distanceUnit">This is the geographic unit you want the result to show in.</param>
        public static double GetWorldDistanceBetweenTwoScreenPoints(RectangleShape worldExtent, ScreenPointF screenPoint1, ScreenPointF screenPoint2, float screenWidth, float screenHeight, GeographyUnit worldExtentUnit, DistanceUnit distanceUnit)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");
            ValidatorHelper.CheckDistanceUnitIsValid(distanceUnit, "distanceUnit");

            return ExtentHelper.GetWorldDistanceBetweenTwoScreenPoints(worldExtent, screenPoint1, screenPoint2, screenWidth, screenHeight, worldExtentUnit, distanceUnit);
        }

        /// <summary>This method returns the distance in world units between two screen points by using the CurrentExtent as a reference.</summary>
        /// <overloads>This overload allows you to pass in ScreenPointF as the points.</overloads>
        /// <returns>This method returns the distance in world units between two screen points.</returns>
        /// <remarks>None</remarks>
        /// <param name="screenPoint1">This is the screen point you want to measure from.</param>
        /// <param name="screenPoint2">This is the screen point you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <param name="distanceUnit">This is the geographic unit you want the result to show in.</param>
        public double GetWorldDistanceBetweenTwoScreenPoints(ScreenPointF screenPoint1, ScreenPointF screenPoint2, float screenWidth, float screenHeight, GeographyUnit mapUnit, DistanceUnit distanceUnit)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckDistanceUnitIsValid(distanceUnit, "distanceUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return GetWorldDistanceBetweenTwoScreenPoints(currentExtent, screenPoint1, screenPoint2, screenWidth, screenHeight, mapUnit, distanceUnit);
        }

        /// <summary>This method returns the distance in wold units between two screen points.</summary>
        /// <overloads>This overload allows you to pass in the X &amp; Y for each point.</overloads>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="screenPoint1X">This parameter is the X of the point you want to measure from.</param>
        /// <param name="screenPoint1Y">This parameter is the Y of the point you want to measure from.</param>
        /// <param name="screenPoint2X">This parameter is the X of the point you want to measure to.</param>
        /// <param name="screenPoint2Y">This parameter is the Y of the point you want to measure to.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        /// <param name="worldExtentUnit">This is the geographic unit of the world extent you passed in.</param>
        /// <param name="distanceUnit">This is the geographic unit you want the result to show in.</param>
        public static double GetWorldDistanceBetweenTwoScreenPoints(RectangleShape worldExtent, float screenPoint1X, float screenPoint1Y, float screenPoint2X, float screenPoint2Y, float screenWidth, float screenHeight, GeographyUnit worldExtentUnit, DistanceUnit distanceUnit)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");
            ValidatorHelper.CheckDistanceUnitIsValid(distanceUnit, "distanceUnit");

            return ExtentHelper.GetWorldDistanceBetweenTwoScreenPoints(worldExtent, screenPoint1X, screenPoint1Y, screenPoint2X, screenPoint2Y, screenWidth, screenHeight, worldExtentUnit, distanceUnit);
        }

        /// <summary>Get the current Scale responding to the <strong>CurrentExtent</strong>.</summary>
        /// <returns>The calculated scale based on the CurrentExtent.</returns>
        /// <param name="screenWidth">
        /// This parameter specifies the screen width responding to the
        /// <strong>CurrentExtent</strong>.
        /// </param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        public double GetCurrentScale(float screenWidth, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return GetCurrentScale(currentExtent, screenWidth, mapUnit);
        }

        /// <summary>
        /// This Static API is used to calculate the scale based on the specified worldExtent
        /// and its corresponding ScreenWidth and MapUnit.
        /// </summary>
        /// <param name="worldExtent">
        /// This parameter specifies the worldExtent used to calculate the current
        /// scale.
        /// </param>
        /// <param name="screenWidth">This parameter specifies the screenWidth corresponding to the worldExtent.</param>
        /// <param name="mapUnit">
        /// This parameter specifies the unit for the extent, the result will be different if
        /// choose DecimalDegree as Unit and Meter as Unit.
        /// </param>
        public static double GetCurrentScale(RectangleShape worldExtent, float screenWidth, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");

            return ExtentHelper.GetScale(worldExtent, screenWidth, mapUnit);
        }

        /// <summary>
        /// This API gets the BoundingBox of a group of BaseShapes.
        /// </summary>
        /// <param name="shapes">The target group of BaseShapes to get the BoundingBox for.</param>
        /// <returns>The BoundingBox that contains all the shapes you passed in.</returns>
        public static RectangleShape GetBoundingBoxOfItems(IEnumerable<BaseShape> shapes)
        {
            ValidatorHelper.CheckObjectIsNotNull(shapes, "shapes");

            return ExtentHelper.GetBoundingBoxOfItems(shapes);
        }

        /// <summary>
        /// This API gets the BoundingBox of a group of Features.
        /// </summary>
        /// <param name="features">The target group of Features to get the BoundingBox for.</param>
        /// <returns>The BoundingBox that contains all the features you passed in.</returns>
        public static RectangleShape GetBoundingBoxOfItems(IEnumerable<Feature> features)
        {
            ValidatorHelper.CheckObjectIsNotNull(features, "features");

            return ExtentHelper.GetBoundingBoxOfItems(features);
        }

        ///// <summary>
        ///// Gets the composite CurrentExtent of all layers included in
        ///// <strong>StaticLayers</strong> and <strong>DynamicLayers, then adjusts it
        ///// according to the specified ScreenWidth and ScreenHeight.</strong>.
        ///// </summary>
        ///// <remarks>
        ///// If you have two layers, one justified up and to the right and the other
        ///// down and to the left, the full extent will combine all of the current
        ///// extents to make one composite extent.
        ///// </remarks>
        ///// <returns>The full extent to make the map at the largest view.</returns>
        ///// <param name="screenWidth">This parameter specifies the width of the Map.</param>
        ///// <param name="screenHeight">This parameter specifies the height of the Map.</param>
        //public RectangleShape GetFullExtent(float screenWidth, float screenHeight)
        //{
        //    ValidatorHelper.CheckIfInputValueIsBiggerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
        //    ValidatorHelper.CheckIfInputValueIsBiggerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

        //    RectangleShape boundingBox = GetFullExtent();
        //    if (boundingBox != null)
        //    {
        //        boundingBox = GetDrawingExtent(boundingBox, screenWidth, screenHeight);
        //    }
        //    return boundingBox;
        //}

        // TODO comment out this method. after we know how to define it , then add it to mapengine and desktop.
        ///// <summary>
        ///// Gets the composite CurrentExtent of all layers included in
        ///// <strong>StaticLayers</strong> and <strong>DynamicLayers</strong>.
        ///// </summary>
        ///// <remarks>
        ///// If you have two layers, one justified up and to the right and the other
        ///// down and to the left, the full extent will combine all of the current
        ///// extents to make one composite extent.
        ///// </remarks>
        ///// <returns>The full extent to make the map at the largest view.</returns>
        //public RectangleShape GetFullExtent()
        //{
        //    RectangleShape boundingBox = null;

        //    foreach (Layer layer in staticLayers)
        //    {
        //        if (layer.HasBoundingBox)
        //        {
        //            if (!layer.IsOpen) { layer.Open(); }
        //            if (boundingBox == null)
        //            {
        //                boundingBox = layer.GetBoundingBox();
        //            }
        //            else
        //            {
        //                boundingBox.ExpandToInclude(layer.GetBoundingBox());
        //            }
        //        }
        //    }

        //    foreach (Layer layer in dynamicLayers)
        //    {
        //        if (layer.HasBoundingBox)
        //        {
        //            if (!layer.IsOpen) { layer.Open(); }
        //            if (boundingBox == null)
        //            {
        //                boundingBox = layer.GetBoundingBox();
        //            }
        //            else
        //            {
        //                boundingBox.ExpandToInclude(layer.GetBoundingBox());
        //            }
        //        }
        //    }

        //    //TODO wheter need to close all the layers.

        //    return boundingBox;
        //}

        //TODO: On the threading front the idea we had was to put different layers
        //  to draw on different threads.  For example we draw layer 1 on thread 1
        // and layer two on thread 2.  Once thread one is free then we draw layer three.
        //  We need to consider labels and other factors when doing this.
        /// <summary>
        /// Draw a group of layers on the specified "background" image.
        /// </summary>
        /// <param name="layers">This parameter specifies the target layers to be drawn.</param>
        /// <param name="image">This parameter specifies the "background" image of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the target layers on the specified "background" image.</returns>
        public GeoImage Draw(IEnumerable<Layer> layers, GeoImage image, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(layers, "layers");
            ValidatorHelper.CheckObjectIsNotNull(image, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            Draw(layers, image.NativeImage, mapUnit, false);
            return image;
        }

        /// <summary>
        /// Draw a group of layers and return a new image with the specified width and height.
        /// </summary>
        /// <param name="layers">This parameter specifies the target layers to be drawn.</param>
        /// <param name="width">This parameter specifies the width of the returning image.</param>
        /// <param name="height">This parameter specifies the height of the returning image.</param>
        /// <returns>The resulting image after drawing the target layers based on the specified width and height.</returns>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        public GeoImage Draw(IEnumerable<Layer> layers, int width, int height, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(layers, "layers");
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(layers, width, height, mapUnit, false);
        }

        /// <summary>
        /// Draw a group of static layers on the specified "background" image.
        /// </summary>
        /// <param name="image">This parameter specifies the "background" image of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of static layers on the specified "background" image.</returns>
        public GeoImage DrawStaticLayers(GeoImage image, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(image, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            Draw(staticLayers, image.NativeImage, mapUnit, true);
            return image;
        }

        /// <summary>
        /// Draw a group of dynamic layers on the specified "background" image.
        /// </summary>
        /// <param name="image">This parameter specifies the "background" image of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of dynamic layers on the specified "background" image.</returns>
        public GeoImage DrawDynamicLayers(GeoImage image, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(image, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            Draw(dynamicLayers, image, mapUnit, false);
            return image;
        }

        /// <summary>
        /// Draw a group of AdornmentLayers on the specified "background" image.
        /// </summary>
        /// <param name="image">This parameter specifies the "background" image of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of AdornmentLayers on the specified "background" image.</returns>
        public GeoImage DrawAdornmentLayers(GeoImage image, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(image, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckObjectIsNotNull(image, "gdiPlusBitmap");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            Draw(adornmentLayers, image.NativeImage, mapUnit, false);
            return image;
        }

        /// <summary>
        /// Draw a group of static layers and return a new image with the specified width and height.
        /// </summary>
        /// <param name="width">This parameter specifies the width of the returning image.</param>
        /// <param name="height">This parameter specifies the height of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of static layers based on the specified width and height.</returns>
        public GeoImage DrawStaticLayers(int width, int height, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(staticLayers, width, height, mapUnit, true);
        }

        /// <summary>
        /// Draw a group of dynamic layers and return a new image with the specified width and height.
        /// </summary>
        /// <param name="width">This parameter specifies the width of the returning image.</param>
        /// <param name="height">This parameter specifies the height of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of dynamic layers based on the specified width and height.</returns>
        public GeoImage DrawDynamicLayers(int width, int height, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(dynamicLayers, width, height, mapUnit, false);
        }

        /// <summary>
        /// Draw a group of AdornmentLayers and return a new image with the specified width and height.
        /// </summary>
        /// <param name="width">This parameter specifies the width of the returning image.</param>
        /// <param name="height">This parameter specifies the height of the returning image.</param>
        /// <param name="mapUnit">This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting image after drawing the group of AdornmentLayers based on the specified width and height.</returns>
        public GeoImage DrawAdornmentLayers(int width, int height, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(adornmentLayers, width, height, mapUnit);
        }

        /// <summary>
        /// This method updates the CurrentExtent that is zoomed in by the percentage provided.
        /// </summary>
        /// <returns>
        /// This method updates the CurrentExtent that is zoomed in by the percentage provided.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        public void ZoomIn(int percentage)
        {
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomIn(currentExtent, percentage);
        }

        /// <summary>
        /// This method returns a new extent that is zoomed in by the percentage
        /// provided.
        /// </summary>
        /// <returns>
        /// This method returns a new extent that is zoomed in by the percentage
        /// provided.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        public static RectangleShape ZoomIn(RectangleShape worldExtent, int percentage)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomIn(worldExtent, percentage);
        }

        /// <summary>This method returns an extent that is centered and zoomed in.</summary>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <returns>This method returns an extent that is centered and zoomed in.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <param name="worldExtent">This parameter is the world extent that you want centered and zoomed.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="worldPoint">This parameter is the world point you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width in screen coordinates.</param>
        /// <param name="screenHeight">This parameter is the height in screen coordinates.</param>
        public static RectangleShape ZoomIntoCenter(RectangleShape worldExtent, int percentage, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomIntoCenter(worldExtent, percentage, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>
        /// This method returns a new extent that is zoomed in by the percentage
        /// provided.
        /// </summary>
        /// <returns>
        /// This method returns a new extent that is zoomed in by the percentage
        /// provided.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="centerFeature">This parameter is the world point feature you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width in screen coordinates.</param>
        /// <param name="screenHeight">This parameter is the height in screen coordinates.</param>
        public static RectangleShape ZoomIntoCenter(RectangleShape worldExtent, int percentage, Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomIntoCenter(worldExtent, percentage, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This method will update the CurrentExtent by using the ZoomIntoCenter operation.</summary>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <returns>None.</returns>
        /// <remarks>
        /// The CurrentExtent will be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="worldPoint">This parameter is the world point you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width in screen coordinates.</param>
        /// <param name="screenHeight">This parameter is the height in screen coordinates.</param>
        public void ZoomIntoCenter(int percentage, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomIntoCenter(currentExtent, percentage, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This method returns an extent that is centered and zoomed in.</summary>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <returns>This method returns an extent that is centered and zoomed in.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="centerFeature">This parameter is the world point you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width in screen coordinates.</param>
        /// <param name="screenHeight">This parameter is the height in screen coordinates.</param>
        public void ZoomIntoCenter(int percentage, Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomIntoCenter(currentExtent, percentage, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This method returns an extent that is centered and zoomed in.</summary>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a screen X &amp; Y.
        /// </overloads>
        /// <returns>This method returns an extent that is centered and zoomed in.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to center and zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="screenX">This parameter is the screen X you want to center on.</param>
        /// <param name="screenY">This parameter is the screen Y you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape ZoomIntoCenter(RectangleShape worldExtent, int percentage, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenX, "screenX", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenY, "screenY", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomIntoCenter(worldExtent, percentage, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method updates the CurrentExtent based on a calculated rectangle that is centered and zoomed in.</summary>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a screen X &amp; Y.
        /// </overloads>
        /// <returns>None.</returns>
        /// <remarks>
        /// The CurrentExtent will be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom in.</param>
        /// <param name="screenX">This parameter is the screen X you want to center on.</param>
        /// <param name="screenY">This parameter is the screen Y you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void ZoomIntoCenter(int percentage, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsInRange(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue, 100, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenX, "screenX", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenY, "screenY", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomIntoCenter(currentExtent, percentage, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>
        /// This method returns a new extent that is zoomed out by the percentage
        /// provided.
        /// </summary>
        /// <returns>
        /// This method returns a new extent that is zoomed out by the percentage
        /// provided.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        public static RectangleShape ZoomOut(RectangleShape worldExtent, int percentage)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomOut(worldExtent, percentage);
        }

        /// <summary>
        /// This method will update the CurrentExtent by using the ZoomOut operation.
        /// </summary>
        /// <returns>None.</returns>
        /// <remarks>None</remarks>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom.</param>
        public void ZoomOut(int percentage)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomOut(currentExtent, percentage);
        }

        /// <summary>This method returns an extent that is centered and zoomed out.</summary>
        /// <returns>This method returns an extent that is centered and zoomed out.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <param name="worldExtent">This parameter is the world extent you want to center and zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="worldPoint">This parameter is the world point you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape ZoomOutToCenter(RectangleShape worldExtent, int percentage, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomOutToCenter(worldExtent, percentage, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This method returns an extent that is centered and zoomed out.</summary>
        /// <returns>This method returns an extent that is centered and zoomed out.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a feature.
        /// </overloads>
        /// <param name="worldExtent">This parameter is the world extent you want to center and zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="centerFeature">This parameter is the feature you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape ZoomOutToCenter(RectangleShape worldExtent, int percentage, Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomOutToCenter(worldExtent, percentage, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This method updates the CurrentExtent by using the ZoomOutToCenter operation.</summary>
        /// <returns>None.</returns>
        /// <remarks>
        /// The CurrentExtent will be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="worldPoint">This parameter is the world point you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void ZoomOutToCenter(int percentage, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomOutToCenter(currentExtent, percentage, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This method updates the CurrentExtent by using the ZoomOutToCenter operation.</summary>
        /// <returns>None.</returns>
        /// <remarks>
        /// The CurrentExtent will be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a world point.
        /// </overloads>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="centerFeature">This parameter is the world point Feature you want the extent to be centered on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void ZoomOutToCenter(int percentage, Feature centerFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomOutToCenter(currentExtent, percentage, centerFeature, screenWidth, screenHeight);
        }

        /// <summary>This method returns an extent that is centered and zoomed out.</summary>
        /// <returns>This method returns an extent that is centered and zoomed out.</returns>
        /// <remarks>
        /// The resulting rectangle will already be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a screenX and screenY.
        /// </overloads>
        /// <param name="worldExtent">This parameter is the world extent you want to center and zoom.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="screenX">This parameter is the screen X you want to center on.</param>
        /// <param name="screenY">This parameter is the screen Y you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static RectangleShape ZoomOutToCenter(RectangleShape worldExtent, int percentage, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ZoomOutToCenter(worldExtent, percentage, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method updates the CurrentExtent by using the ZoomOutToCenter operation.</summary>
        /// <returns>None.</returns>
        /// <remarks>
        /// The CurrentExtent will  be adjusted for the ratio of the screen. You
        /// do not need to call GetDrawingExtent afterwards.
        /// </remarks>
        /// <overloads>
        /// This overload allows you to pass in the height and width in screen coordinates,
        /// as well as a screenX and screenY.
        /// </overloads>
        /// <param name="percentage">This parameter is the percentage by which you want to zoom out.</param>
        /// <param name="screenX">This parameter is the screen X you want to center on.</param>
        /// <param name="screenY">This parameter is the screen Y you want to center on.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public void ZoomOutToCenter(int percentage, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomOutToCenter(currentExtent, percentage, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method returns a panned extent.</summary>
        /// <overloads>
        /// This overload allows you to pass in a direction and a percentage by which you want to
        /// pan.
        /// </overloads>
        /// <returns>This method returns a panned extent.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to pan.</param>
        /// <param name="direction">This parameter is the direction you want to pan.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to pan.</param>
        public static RectangleShape Pan(RectangleShape worldExtent, PanDirection direction, int percentage)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckObjectIsNotNull(direction, "direction");
            ValidatorHelper.CheckPanDirectionIsValid(direction, "direction");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.IncludeValue);

            return ExtentHelper.Pan(worldExtent, direction, percentage);
        }

        /// <summary>Update the CurrentExtent by using a panning operation.</summary>
        /// <overloads>
        /// This overload allows you to pass in a direction and a percentage by which you want to
        /// pan.
        /// </overloads>
        /// <returns>None.</returns>
        /// <remarks>None</remarks>
        /// <param name="panDirection">This parameter is the direction you want to pan.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to pan.</param>
        public void Pan(PanDirection panDirection, int percentage)
        {
            ValidatorHelper.CheckPanDirectionIsValid(panDirection, "direction");
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = Pan(currentExtent, panDirection, percentage);
        }

        /// <summary>This method returns a panned extent.</summary>
        /// <overloads>
        /// This overload allows you to pass in an angle and a percentage by which you want to
        /// pan.
        /// </overloads>
        /// <returns>This method returns a panned extent.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want to pan.</param>
        /// <param name="degree">This parameter is the angle in degrees in which you want to pan.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to pan.</param>
        public static RectangleShape Pan(RectangleShape worldExtent, float degree, int percentage)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsInRange(degree, "degree", 0, RangeCheckingInclusion.IncludeValue, 360, RangeCheckingInclusion.IncludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.IncludeValue);

            return ExtentHelper.Pan(worldExtent, degree, percentage);
        }

        /// <summary>This method updates the CurrentExtent by using a panning operation.</summary>
        /// <overloads>
        /// This overload allows you to pass in an angle and a percentage by which you want to
        /// pan and update the CurrentExtent.
        /// </overloads>
        /// <returns>None.</returns>
        /// <remarks>None</remarks>
        /// <param name="degree">This parameter is the angle in degrees in which you want to pan.</param>
        /// <param name="percentage">This parameter is the percentage by which you want to pan.</param>
        public void Pan(float degree, int percentage)
        {
            ValidatorHelper.CheckInputValueIsInRange(degree, "degree", 0, RangeCheckingInclusion.IncludeValue, 360, RangeCheckingInclusion.IncludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(percentage, "percentage", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = Pan(currentExtent, degree, percentage);
        }

        /// <summary>This method returns screen coordinates from the specified world coordinates, based on the CurrentExtent.</summary>
        /// <returns>This method returns screen coordinates from the specified world coordinates, based on the CurrentExtent.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldX">This parameter is the world point X you want converted to a screen point.</param>
        /// <param name="worldY">This parameter is the world point Y you want converted to a screen point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen for the CurrentExtent.</param>
        /// <param name="screenHeight">This parameter is the height of the screen for the CurrentExtent.</param>
        public ScreenPointF ToScreenCoordinate(double worldX, double worldY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return ToScreenCoordinate(currentExtent, worldX, worldY, screenWidth, screenHeight);
        }

        /// <summary>This method returns screen coordinates from the specified world coordinates, based on the CurrentExtent.</summary>
        /// <returns>This method returns screen coordinates from the specified world coordinates, based on the CurrentExtent.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldPoint">This parameter is the world point you want converted to a screen point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen for the CurrentExtent.</param>
        /// <param name="screenHeight">This parameter is the height of the screen for the CurrentExtent.</param>
        public ScreenPointF ToScreenCoordinate(PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return ToScreenCoordinate(currentExtent, worldPoint.X, worldPoint.Y, screenWidth, screenHeight);
        }

        /// <summary>This method returns screen coordinates from the specified world coordinate pointFeature, based on the CurrentExtent.</summary>
        /// <returns>This method returns screen coordinates from the specified world coordinate pointFeature, based on the CurrentExtent.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldPointFeature">This parameter is the world coordinate pointFeature you want converted to a screen point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen for the CurrentExtent.</param>
        /// <param name="screenHeight">This parameter is the height of the screen for the CurrentExtent.</param>
        public ScreenPointF ToScreenCoordinate(Feature worldPointFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return ToScreenCoordinate(currentExtent, worldPointFeature, screenWidth, screenHeight);
        }

        /// <summary>This method returns world coordinates from screen coordinates, based on the CurrentExtent.</summary>
        /// <returns>This method returns world coordinates from screen coordinates, based on the CurrentExtent.</returns>
        /// <remarks>None</remarks>
        /// <param name="screenX">
        /// This parameter is the X of the point you want converted to world
        /// coordinates.
        /// </param>
        /// <param name="screenY">
        /// This parameter is the Y of the point you want converted to world
        /// coordinates.
        /// </param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public PointShape ToWorldCoordinate(float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return ToWorldCoordinate(currentExtent, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method returns world coordinates from screen coordinates, based on the CurrentExtent.</summary>
        /// <returns>This method returns world coordinates from screen coordinates, based on the CurrentExtent.</returns>
        /// <remarks>None</remarks>
        /// <param name="screenPoint"> This parameter is the point you want converted to world coordinates.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public PointShape ToWorldCoordinate(ScreenPointF screenPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return ToWorldCoordinate(screenPoint.X, screenPoint.Y, screenWidth, screenHeight);
        }

        /// <summary>This method returns screen coordinates from world coordinates.</summary>
        /// <returns>This method returns screen coordinates from world coordinates.</returns>
        /// <remarks>None</remarks>
        /// <overloads>This overload allows you to pass in world X &amp; Y coordinates.</overloads>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="worldX">This parameter is the world X you want converted to screen points.</param>
        /// <param name="worldY">This parameter is the world Y you want converted to screen points.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static ScreenPointF ToScreenCoordinate(RectangleShape worldExtent, double worldX, double worldY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ToScreenCoordinate(worldExtent, worldX, worldY, screenWidth, screenHeight);
        }

        /// <summary>This method returns screen coordinates from world coordinates.</summary>
        /// <returns>This method returns screen coordinates from world coordinates.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="worldPoint">This parameter is the world point you want converted to a screen point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static ScreenPointF ToScreenCoordinate(RectangleShape worldExtent, PointShape worldPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckObjectIsNotNull(worldPoint, "worldPoint");
            ValidatorHelper.CheckShapeIsValidForOperation(worldPoint);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ToScreenCoordinate(worldExtent, worldPoint, screenWidth, screenHeight);
        }

        /// <summary>This method returns screen coordinates from world coordinates.</summary>
        /// <returns>This method returns screen coordinates from world coordinates.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="worldPointFeature">This parameter is the world point feature you want converted to a screen point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static ScreenPointF ToScreenCoordinate(RectangleShape worldExtent, Feature worldPointFeature, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckFeatureIsValid(worldPointFeature);
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ToScreenCoordinate(worldExtent, worldPointFeature, screenWidth, screenHeight);
        }

        /// <summary>This method returns world coordinates from screen coordinates.</summary>
        /// <returns>This method returns world coordinates from screen coordinates.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="screenX">
        /// This parameter is the X coordinate of the point you want converted to world
        /// coordinates.
        /// </param>
        /// <param name="screenY">
        /// This parameter is the Y coordinate of the point you want converted to world
        /// coordinates.
        /// </param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static PointShape ToWorldCoordinate(RectangleShape worldExtent, float screenX, float screenY, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ToWorldCoordinate(worldExtent, screenX, screenY, screenWidth, screenHeight);
        }

        /// <summary>This method returns world coordinates from screen coordinates.</summary>
        /// <returns>This method returns world coordinates from screen coordinates.</returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent.</param>
        /// <param name="screenPoint">This parameter is the screen point you want converted to a world point.</param>
        /// <param name="screenWidth">This parameter is the width of the screen.</param>
        /// <param name="screenHeight">This parameter is the height of the screen.</param>
        public static PointShape ToWorldCoordinate(RectangleShape worldExtent, ScreenPointF screenPoint, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckInputValueIsLargerThan(screenWidth, "screenWidth", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(screenHeight, "screenHeight", 0, RangeCheckingInclusion.ExcludeValue);

            return ExtentHelper.ToWorldCoordinate(worldExtent, screenPoint, screenWidth, screenHeight);
        }

        /// <summary>
        /// This method returns an extent that is snapped to a zoom level in the provided
        /// zoom level set.
        /// </summary>
        /// <returns>
        /// This method returns an extent that is snapped to a zoom level in the provided
        /// zoom level set.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtent">This parameter is the world extent you want snapped.</param>
        /// <param name="worldExtentUnit">This parameter is the geographic unit of the world extent parameter.</param>
        /// <param name="screenWidth">This parameter is the screen width.</param>
        /// <param name="screenHeight">This parameter is the screen height.</param>
        /// <param name="zoomLevelSet">This parameter is the set of zoom levels you want to snap to.</param>
        public static RectangleShape SnapToZoomLevel(RectangleShape worldExtent, GeographyUnit worldExtentUnit, float screenWidth, float screenHeight, ZoomLevelSet zoomLevelSet)
        {
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");

            return ExtentHelper.GetSnappedExtent(worldExtent, worldExtentUnit, screenWidth, screenHeight, zoomLevelSet);
        }

        /// <summary>
        /// This method updates the CurrentExtent by snapping to a zoom level in the provided
        /// zoom level set.
        /// </summary>
        /// <returns>
        /// This method updates the CurrentExtent by snapping to a zoom level in the provided
        /// zoom level set.
        /// </returns>
        /// <remarks>None</remarks>
        /// <param name="worldExtentUnit">This parameter is the geographic unit of the CurrentExtent.</param>
        /// <param name="screenWidth">This parameter is the screen width.</param>
        /// <param name="screenHeight">This parameter is the screen height.</param>
        /// <param name="zoomLevelSet">This parameter is the set of zoom levels you want to snap to.</param>
        public void SnapToZoomLevel(GeographyUnit worldExtentUnit, float screenWidth, float screenHeight, ZoomLevelSet zoomLevelSet)
        {
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = SnapToZoomLevel(currentExtent, worldExtentUnit, screenWidth, screenHeight, zoomLevelSet);
        }

        /// <summary>This method returns a extent that has been zoomed into a certain scale.</summary>
        /// <returns>This method returns a extent that has been zoomed into a certain scale.</returns>
        /// <remarks>None</remarks>
        /// <param name="targetScale">This parameter is the scale you want to zoom into.</param>
        /// <param name="worldExtent">This parameter is the world extent you want zoomed into the scale.</param>
        /// <param name="worldExtentUnit">This parameter is the geographic unit of the world extent parameter.</param>
        /// <param name="screenWidth">This parameter is the screen width.</param>
        /// <param name="screenHeight">This parameter is the screen height.</param>
        public static RectangleShape ZoomToScale(double targetScale, RectangleShape worldExtent, GeographyUnit worldExtentUnit, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");
            ValidatorHelper.CheckExtentIsValid(worldExtent, "worldExtent");

            return ExtentHelper.ZoomToScale(targetScale, worldExtent, worldExtentUnit, screenWidth, screenHeight);
        }

        /// <summary>This method updates the CurrentExtent by zooming to a certain scale.</summary>
        /// <remarks>None</remarks>
        /// <param name="targetScale">This parameter is the scale you want to zoom into.</param>
        /// <param name="worldExtentUnit">This parameter is the geographic unit of the CurrentExtent.</param>
        /// <param name="screenWidth">This parameter is the screen width.</param>
        /// <param name="screenHeight">This parameter is the screen height.</param>
        public void ZoomToScale(double targetScale, GeographyUnit worldExtentUnit, float screenWidth, float screenHeight)
        {
            ValidatorHelper.CheckGeographyUnitIsValid(worldExtentUnit, "worldExtentUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            currentExtent = ZoomToScale(targetScale, currentExtent, worldExtentUnit, screenWidth, screenHeight);
        }

        /// <summary>
        /// Get the current MapSuiteCore.dll file version.
        /// </summary>
        /// <returns>A string representing the file version of MapSuiteCore.dll.</returns>
        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            IFileVersionInfo info = PclSystem.FileVersionInfoFactory.Create(PclSystem.Assembly.GetLocation(assembly));

            return info.FileVersion;
        }

        /// <summary>
        /// This method is a static API to get information about a group of passed-in features with the specified
        /// returningColumns, in the format of a DataTable.
        /// </summary>
        /// <param name="features">This parameter specifies the target features.</param>
        /// <param name="returningColumnNames">This parameter specifies the returning columnNames for the features.</param>
        /// <returns>A DateTable of information about those passed-in features and the returning columnNames.</returns>
        public static GeoDataTable LoadDataTable(Collection<Feature> features, IEnumerable<string> returningColumnNames)
        {
            ValidatorHelper.CheckObjectIsNotNull(returningColumnNames, "returningColumnNames");

            GeoDataTable dataTable = null;

            if (features != null)
            {
                dataTable = new GeoDataTable();
                foreach (string columnName in returningColumnNames)
                {
                    dataTable.Columns.Add(columnName);
                }

                foreach (Feature feature in features)
                {
                    Dictionary<string, object> dataRow = new Dictionary<string, object>();

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        if (feature.ColumnValues.ContainsKey(dataTable.Columns[i]))
                        {
                            dataRow.Add(dataTable.Columns[i], feature.ColumnValues[dataTable.Columns[i]]);
                        }
                        else
                        {
                            dataRow.Add(dataTable.Columns[i], string.Empty);
                        }
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Draw a group of layers on the specified "background" bitmap.
        /// </summary>
        /// <param name = "layers" > This parameter specifies the target layers to be drawn.</param>
        /// <param name = "gdiPlusBitmap" > This parameter specifies the "background" bitmap of the returning bitmap.</param>
        /// <param name = "mapUnit" > This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting bitmap after drawing the target layers on the specified "background" bitmap.</returns>
        public Bitmap Draw(IEnumerable<Layer> layers, Bitmap gdiPlusBitmap, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(layers, gdiPlusBitmap, mapUnit, false);
        }

        /// <summary>
        /// Draw a group of static layers on the specified "background" bitmap.
        /// </summary>
        /// <param name = "gdiPlusBitmap" > This parameter specifies the "background" bitmap of the returning bitmap.</param>
        /// <param name = "mapUnit" > This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting bitmap after drawing the group of static layers on the specified "background" bitmap.</returns>
        public Bitmap DrawStaticLayers(Bitmap gdiPlusBitmap, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "gdiPlusBitmap");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(staticLayers, gdiPlusBitmap, mapUnit, true);
        }

        /// <summary>
        /// Draw a group of dynamic layers on the specified "background" bitmap.
        /// </summary>
        /// <param name = "gdiPlusBitmap" > This parameter specifies the "background" bitmap of the returning bitmap.</param>
        /// <param name = "mapUnit" > This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting bitmap after drawing the group of dynamic layers on the specified "background" bitmap.</returns>
        public Bitmap DrawDynamicLayers(Bitmap gdiPlusBitmap, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "gdiPlusBitmap");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(dynamicLayers, gdiPlusBitmap, mapUnit, false);
        }

        /// <summary>
        /// Draw a group of AdornmentLayers on the specified "background" bitmap.
        /// </summary>
        /// <param name = "gdiPlusBitmap" > This parameter specifies the "background" bitmap of the returning bitmap.</param>
        /// <param name = "mapUnit" > This parameter specifies the MapUnit used in the current map.</param>
        /// <returns>The resulting bitmap after drawing the group of AdornmentLayers on the specified "background" bitmap.</returns>
        public Bitmap DrawAdornmentLayers(Bitmap gdiPlusBitmap, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "gdiPlusBitmap");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            return Draw(adornmentLayers, gdiPlusBitmap, mapUnit);
        }

        private Bitmap Draw(IEnumerable<AdornmentLayer> adornmentLayers, Bitmap gdiPlusBitmap, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "gdiPlusBitmap");
            ValidatorHelper.CheckObjectIsNotNull(adornmentLayers, "adornmentLayers");
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            if (!(canvas is PlatformGeoCanvas)) { throw new ArgumentException("The GeoCanvas isn't right."); }

            AdornmentLayersDrawingEventArgs layersDrawingEventArgs = new AdornmentLayersDrawingEventArgs(adornmentLayers);
            OnAdornmentLayersDrawing(layersDrawingEventArgs);

            canvas.BeginDrawing(gdiPlusBitmap, currentExtent, mapUnit);
            foreach (AdornmentLayer adornmentLayer in adornmentLayers)
            {
                DrawOneAdornmentLayer(adornmentLayer);
            }
            canvas.EndDrawing();

            AdornmentLayersDrawnEventArgs layerslDrawnEventArgs = new AdornmentLayersDrawnEventArgs(adornmentLayers);
            OnAdornmentLayersDrawn(layerslDrawnEventArgs);

            return gdiPlusBitmap;
        }

        private Bitmap Draw(IEnumerable<Layer> layers, Bitmap gdiPlusBitmap, GeographyUnit mapUnit, bool isToDrawBackground)
        {
            ValidatorHelper.CheckObjectIsNotNull(layers, "layers");
            ValidatorHelper.CheckObjectIsNotNull(gdiPlusBitmap, "gdiPlusBitmap");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            labeledFeaturesInLayers.Clear();

            if (!(canvas is PlatformGeoCanvas)) { throw new ArgumentException("The GeoCanvas isn't right."); }
            ZoomLevelSet zoomLevelSet = new ZoomLevelSet();
            if (mapUnit != GeographyUnit.DecimalDegree)
            {
                zoomLevelSet = new SphericalMercatorZoomLevelSet();
            }
            RectangleShape snappedExtent = ExtentHelper.SnapToZoomLevel(currentExtent, mapUnit, gdiPlusBitmap.Width, gdiPlusBitmap.Height, zoomLevelSet);
            currentExtent = snappedExtent;

            if (isToDrawBackground)
            {
                canvas.BeginDrawing(gdiPlusBitmap, snappedExtent, mapUnit);
                canvas.Clear(backgroundFillBrush);
                canvas.EndDrawing();
            }

            LayersDrawingEventArgs layersDrawingEventArgs = new LayersDrawingEventArgs(layers, snappedExtent, gdiPlusBitmap);
            OnLayersDrawing(layersDrawingEventArgs);

            if (!layersDrawingEventArgs.Cancel)
            {
                foreach (Layer layer in layers)
                {
                    //TODO whether should move beginDrawing out of foreach???
                    canvas.BeginDrawing(gdiPlusBitmap, snappedExtent, mapUnit);
                    DrawOneLayer(layer, gdiPlusBitmap);
                    canvas.EndDrawing();
                }
            }

            canvas.BeginDrawing(gdiPlusBitmap, snappedExtent, mapUnit);
            DrawLogo();
            canvas.EndDrawing();

            LayersDrawnEventArgs layerslDrawnEventArgs = new LayersDrawnEventArgs(layers, snappedExtent, gdiPlusBitmap);
            OnLayersDrawn(layerslDrawnEventArgs);

            return gdiPlusBitmap;
        }

        private GeoImage Draw(IEnumerable<AdornmentLayer> adornmentLayers, int width, int height, GeographyUnit mapUnit)
        {
            ValidatorHelper.CheckObjectIsNotNull(adornmentLayers, "adornmentLayers");
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            GeoImage returningGeoImage = new GeoImage(width, height);
            AdornmentLayersDrawingEventArgs layersDrawingEventArgs = new AdornmentLayersDrawingEventArgs(adornmentLayers);
            OnAdornmentLayersDrawing(layersDrawingEventArgs);

            canvas.BeginDrawing(returningGeoImage, currentExtent, mapUnit);
            foreach (AdornmentLayer adornmentLayer in adornmentLayers)
            {
                DrawOneAdornmentLayer(adornmentLayer);
            }
            canvas.EndDrawing();

            AdornmentLayersDrawnEventArgs layerslDrawnEventArgs = new AdornmentLayersDrawnEventArgs(adornmentLayers);
            OnAdornmentLayersDrawn(layerslDrawnEventArgs);

            return returningGeoImage;
        }

        private GeoImage Draw(IEnumerable<Layer> layers, int width, int height, GeographyUnit mapUnit, bool isToDrawBackground)
        {
            ValidatorHelper.CheckObjectIsNotNull(layers, "layers");
            ValidatorHelper.CheckInputValueIsLargerThan(width, "width", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckInputValueIsLargerThan(height, "height", 0, RangeCheckingInclusion.ExcludeValue);
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            GeoImage tempGeoImage = new GeoImage(width, height);
            GeoImage returningGeoImage;
            try
            {
                labeledFeaturesInLayers.Clear();
                if (isToDrawBackground)
                {
                    canvas.BeginDrawing(tempGeoImage, currentExtent, mapUnit);
                    canvas.Clear(backgroundFillBrush);
                    canvas.EndDrawing();
                }

                LayersDrawingEventArgs layersDrawingEventArgs = new LayersDrawingEventArgs(layers, currentExtent, tempGeoImage);
                OnLayersDrawing(layersDrawingEventArgs);

                if (!layersDrawingEventArgs.Cancel)
                {
                    foreach (Layer layer in layers)
                    {
                        canvas.BeginDrawing(tempGeoImage, currentExtent, mapUnit);
                        DrawOneLayer(layer, tempGeoImage);
                        canvas.EndDrawing();
                    }
                }

                canvas.BeginDrawing(tempGeoImage, currentExtent, mapUnit);
                DrawLogo();
                canvas.EndDrawing();

                LayersDrawnEventArgs layerslDrawnEventArgs = new LayersDrawnEventArgs(layers, currentExtent, tempGeoImage);
                OnLayersDrawn(layerslDrawnEventArgs);

                returningGeoImage = new GeoImage(PclSystem.Current.Resolve<INativeImage>().Clone(tempGeoImage.NativeImage));
            }
            finally
            {
                tempGeoImage.Dispose();
            }

            return returningGeoImage;
        }

        private void Draw(IEnumerable<Layer> layers, object image, GeographyUnit mapUnit, bool isToDrawBackground)
        {
            ValidatorHelper.CheckObjectIsNotNull(layers, "layers");
            ValidatorHelper.CheckObjectIsNotNull(image, "image");
            ValidatorHelper.CheckGeographyUnitIsValid(mapUnit, "mapUnit");
            ValidatorHelper.CheckExtentIsValid(currentExtent, "currentExtent");

            labeledFeaturesInLayers.Clear();

            ZoomLevelSet zoomLevelSet = new ZoomLevelSet();
            if (mapUnit != GeographyUnit.DecimalDegree)
            {
                zoomLevelSet = new SphericalMercatorZoomLevelSet();
            }

            float width = PclSystem.Current.Resolve<INativeImage>().GetWidth(image);
            float height = PclSystem.Current.Resolve<INativeImage>().GetHeight(image);
            RectangleShape snappedExtent = ExtentHelper.GetSnappedExtent(currentExtent, mapUnit, width, height, zoomLevelSet);
            currentExtent = snappedExtent;

            if (isToDrawBackground)
            {
                canvas.BeginDrawing(image, currentExtent, mapUnit);
                canvas.Clear(backgroundFillBrush);
                canvas.EndDrawing();
            }

            LayersDrawingEventArgs layersDrawingEventArgs = new LayersDrawingEventArgs(layers, currentExtent, image);
            OnLayersDrawing(layersDrawingEventArgs);

            if (!layersDrawingEventArgs.Cancel)
            {
                foreach (Layer layer in layers)
                {
                    canvas.BeginDrawing(image, currentExtent, mapUnit);
                    DrawOneLayer(layer, image);
                    canvas.EndDrawing();
                }
            }

            canvas.BeginDrawing(image, currentExtent, mapUnit);
            DrawLogo();
            canvas.EndDrawing();

            LayersDrawnEventArgs layerslDrawnEventArgs = new LayersDrawnEventArgs(layers, currentExtent, image);
            OnLayersDrawn(layerslDrawnEventArgs);
        }

        private void DrawLogo()
        {
            if (ShowLogo)
            {
                Stream stream = new MemoryStream();
                GeoImage logoImage = null;
                try
                {
                    logoImage = new GeoImage("You logo path");

                    float logoWidth = 100;
                    float logoHeight = 50;

                    canvas.DrawWorldImage(logoImage, currentExtent.LowerRightPoint.X, currentExtent.LowerRightPoint.Y, logoWidth, logoHeight, DrawingLevel.LevelFour, -logoWidth / 2 - 2, -logoHeight / 2 - 2, 0);
                }
                finally
                {
                    stream.Dispose();
                    logoImage?.Dispose();
                }
            }
        }

        private void DrawOneLayer(Layer layer, Object nativeImage)
        {
            LayerDrawingEventArgs layerDrawingEventArgs = new LayerDrawingEventArgs(layer, currentExtent, nativeImage);
            OnLayerDrawing(layerDrawingEventArgs);

            if (!layerDrawingEventArgs.Cancel)
            {
                layer.Draw(canvas, labeledFeaturesInLayers);
            }

            LayerDrawnEventArgs layerDrawnEventArgs = new LayerDrawnEventArgs(layer, currentExtent, nativeImage);
            OnLayerDrawn(layerDrawnEventArgs);
        }

        private void DrawOneAdornmentLayer(AdornmentLayer adornmentLayer)
        {
            AdornmentLayerDrawingEventArgs layerDrawingEventArgs = new AdornmentLayerDrawingEventArgs(adornmentLayer);
            OnAdornmentLayerDrawing(layerDrawingEventArgs);

            if (adornmentLayer.IsVisible)
            {
                adornmentLayer.Draw(canvas, labeledFeaturesInLayers);
            }
            AdornmentLayerDrawnEventArgs layerDrawnEventArgs = new AdornmentLayerDrawnEventArgs(adornmentLayer);
            OnAdornmentLayerDrawn(layerDrawnEventArgs);
        }

    }
}