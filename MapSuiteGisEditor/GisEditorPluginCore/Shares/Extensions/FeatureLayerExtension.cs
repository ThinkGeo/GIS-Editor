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
using System.Linq;
using ThinkGeo.Core;

using ThinkGeo.UI.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class FeatureLayerExtension
    {
        public static void AddBlankZoomLevels(this FeatureLayer featureLayer)
        {
            var zoomScales = GisEditor.ActiveMap?.ZoomScales;
            if (zoomScales == null) return;

            for (int i = 0; i < zoomScales.Count; i++)
            {
                featureLayer.ZoomLevelSet.CustomZoomLevels.Add(new ZoomLevel(zoomScales[i]));
            }
        }

        // Used by LandPro.
        public static void SetLayerAccess(this FeatureLayer featureLayer, LayerAccessMode layerAccessMode)
        {
            ShapeFileFeatureLayer shapeFileFeatureLayer = featureLayer as ShapeFileFeatureLayer;
            if (shapeFileFeatureLayer != null)
            {
                FileAccess readWriteMode = FileAccess.Read;
                switch (layerAccessMode)
                {
                    case LayerAccessMode.Write:
                    case LayerAccessMode.ReadWrite:
                        readWriteMode = FileAccess.ReadWrite;
                        break;
                    case LayerAccessMode.Read:
                    default:
                        readWriteMode = FileAccess.Read;
                        break;
                }
                shapeFileFeatureLayer.ReadWriteMode = readWriteMode;
            }
        }

        public static void ReOpen(this FeatureLayer featureLayer, Action reOpenAction = null)
        {
            bool isOpened = false;
            if (featureLayer.IsOpen)
            {
                isOpened = true;
                featureLayer.CloseAll();
            }

            if (reOpenAction != null)
            {
                reOpenAction();
            }

            if (isOpened)
            {
                featureLayer.Open();
            }
        }

        public static void CloseAll(this FeatureLayer featureLayer)
        {
            lock (featureLayer)
            {
                featureLayer.Close();

                if (featureLayer.FeatureSource.IsOpen) featureLayer.FeatureSource.Close();

                if (featureLayer.FeatureSource.ProjectionConverter != null && featureLayer.FeatureSource.ProjectionConverter.IsOpen)
                {
                    featureLayer.FeatureSource.ProjectionConverter.Close();
                }
            }
        }

        public static void SafeProcess(this Layer layer, Action processAction)
        {
            lock (layer)
            {
                bool isClosed = false;
                if (!layer.IsOpen)
                {
                    layer.Open();
                    isClosed = true;
                }

                if (processAction != null) processAction();

                if (isClosed)
                {
                    layer.Close();
                }
            }
        }

        public static void SafeProcess(this LayerBase layer, Action processAction)
        {
            if (layer == null) return;
            lock (layer)
            {
                bool isClosed = false;
                var openableLayer = layer as Layer;
                if (openableLayer != null && !openableLayer.IsOpen)
                {
                    openableLayer.Open();
                    isClosed = true;
                }

                if (processAction != null) processAction();

                if (isClosed && openableLayer != null)
                {
                    openableLayer.Close();
                }
            }
        }

        public static void SafeProcess(this FeatureSource featureSource, Action processAction)
        {
            lock (featureSource)
            {
                bool isClosed = false;
                if (!featureSource.IsOpen)
                {
                    featureSource.Open();
                    isClosed = true;
                }

                if (processAction != null) processAction();

                if (isClosed)
                {
                    featureSource.Close();
                }
            }
        }

        public static void SafeProcess<T>(this FeatureSource featureSource, Action<T> processAction, T parameter)
        {
            lock (featureSource)
            {
                bool isClosed = false;
                if (!featureSource.IsOpen)
                {
                    featureSource.Open();
                    isClosed = true;
                }

                if (processAction != null) processAction(parameter);

                if (isClosed)
                {
                    featureSource.Close();
                }
            }
        }

        public static Feature MakeValidIfCan(this Feature feature)
        {
            if (feature.CanMakeValid())
                return feature.MakeValid();
            else return feature;
        }

        public static void CloseInOverlay(this FeatureLayer featureLayer)
        {
            featureLayer.CloseAll();

            LayerPlugin currentFeatureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault();
            if (currentFeatureLayerPlugin != null)
            {
                Uri currentFeatureLayerUri = currentFeatureLayerPlugin.GetUri(featureLayer);
                IEnumerable<LayerOverlay> layerOverlays = GisEditor.GetMaps().
                                    SelectMany(map => map.Overlays).
                                    OfType<LayerOverlay>().
                                    Where(layerOverlay =>
                                    {
                                        FeatureLayer matchingShpLayer = layerOverlay.Layers.OfType<FeatureLayer>().Where(tempLayer =>
                                        {
                                            LayerPlugin tempFeatureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(tempLayer.GetType()).FirstOrDefault();
                                            return tempFeatureLayerPlugin.GetUri(tempLayer) == currentFeatureLayerUri;
                                        }).FirstOrDefault();

                                        if (matchingShpLayer != null)
                                        {
                                            matchingShpLayer.CloseAll();
                                            return true;
                                        }
                                        return false;
                                    });

                foreach (var layerOverlay in layerOverlays)
                {
                    layerOverlay.Close();
                }
            }
            else
            {
                LayerOverlay layerOverlay = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>().Where(lo => lo.Layers.Contains(featureLayer)).FirstOrDefault();
                if (layerOverlay != null)  layerOverlay.Close();
            }
        }
    }
}
