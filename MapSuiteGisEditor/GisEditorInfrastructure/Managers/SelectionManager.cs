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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    public class SelectionManager : Manager
    {
        /// <summary>
        /// Gets the selection overlay.
        /// </summary>
        /// <returns></returns>
        public SelectionTrackInteractiveOverlay GetSelectionOverlay()
        {
            return GetSelectionOverlayCore();
        }

        /// <summary>
        /// Gets the selection overlay core.
        /// </summary>
        /// <returns></returns>
        protected virtual SelectionTrackInteractiveOverlay GetSelectionOverlayCore()
        {
            SelectionTrackInteractiveOverlay selectionOverlay = null;
            if (GisEditor.ActiveMap != null)
            {
                selectionOverlay = GisEditor.ActiveMap.SelectionOverlay;
            }

            return selectionOverlay;
        }

        /// <summary>
        /// Gets the selected features layer.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns></returns>
        public FeatureLayer GetSelectedFeaturesLayer(Feature feature)
        {
            return GetSelectedFeaturesLayerCore(feature);
        }

        /// <summary>
        /// Gets the selected features layer core.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns></returns>
        protected virtual FeatureLayer GetSelectedFeaturesLayerCore(Feature feature)
        {
            var segments = feature.Id.Split(new string[] { SelectionTrackInteractiveOverlay.FeatureIdSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
            {
                string hashCode = segments[1];
                return GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>()
                    .SelectMany(o => o.Layers)
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.GetHashCode().ToString(CultureInfo.InvariantCulture).Equals(hashCode));
            }
            else return null;
        }

        /// <summary>
        /// Gets the selected features.
        /// </summary>
        /// <param name="featureLayer">The feature layer.</param>
        /// <returns></returns>
        public Collection<Feature> GetSelectedFeatures(FeatureLayer featureLayer)
        {
            return GetSelectedFeatures(new FeatureLayer[] { featureLayer });
        }

        /// <summary>
        /// Gets the selected features.
        /// </summary>
        /// <param name="featureLayers">The feature layers.</param>
        /// <returns></returns>
        public Collection<Feature> GetSelectedFeatures(IEnumerable<FeatureLayer> featureLayers)
        {
            var features = GetSelectedFeatures();
            return new Collection<Feature>(features.Where(f => featureLayers.Any(l => l != null && l.Equals(f.Tag))).ToArray());
        }

        /// <summary>
        /// Gets the selected features.
        /// </summary>
        /// <returns></returns>
        public Collection<Feature> GetSelectedFeatures()
        {
            return GetSelectedFeaturesCore();
        }

        /// <summary>
        /// Gets the selected features core.
        /// </summary>
        /// <returns></returns>
        protected virtual Collection<Feature> GetSelectedFeaturesCore()
        {
            Collection<Feature> selectedFeatures = new Collection<Feature>();
            if (GetSelectionOverlay() != null)
            {
                GetSelectionOverlay().HighlightFeatureLayer.InternalFeatures.ForEach(f => selectedFeatures.Add(f));
            }

            return selectedFeatures;
        }

        /// <summary>
        /// Clears the selected features.
        /// </summary>
        public void ClearSelectedFeatures()
        {
            foreach (var item in GetSelectionOverlay().TargetFeatureLayers)
            {
                ClearSelectedFeatures(item);
            }
        }

        /// <summary>
        /// Clears the selected features.
        /// </summary>
        /// <param name="featureLayer">The feature layer.</param>
        public void ClearSelectedFeatures(FeatureLayer featureLayer)
        {
            ClearSelectedFeaturesCore(featureLayer);
        }

        /// <summary>
        /// Clears the selected features core.
        /// </summary>
        /// <param name="featureLayer">The feature layer.</param>
        protected virtual void ClearSelectedFeaturesCore(FeatureLayer featureLayer)
        {
            if (GisEditor.SelectionManager.GetSelectedFeatures().Count > 0)
            {
                var overlay = GisEditor.SelectionManager.GetSelectionOverlay();
                var featuresToRemove = overlay.HighlightFeatureLayer.InternalFeatures.Where(feature => feature.Tag == featureLayer).ToArray();
                foreach (var feature in featuresToRemove)
                {
                    overlay.HighlightFeatureLayer.InternalFeatures.Remove(feature);
                }
                overlay.StandOutHighlightFeatureLayer.InternalFeatures.Clear();

                overlay.HighlightFeatureLayer.BuildIndex();
                GisEditor.ActiveMap.Refresh(overlay);
            }
        }
    }
}