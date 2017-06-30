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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public class AddLayersParameters
    {
        private string proj4ProjectionParameters;
        private Collection<Layer> layersToAdd;
        private Collection<Type> excludedLayerTypesToZoomTo;

        public AddLayersParameters()
        {
            layersToAdd = new Collection<Layer>();
            excludedLayerTypesToZoomTo = new Collection<Type>();
            TileSize = 256;

            IsMaxRecordsToDrawEnabled = false;
            MaxRecordsToDraw = 12000;
            TileSize = 256;
            IsCacheEnabled = true;
            ZoomToExtentAutomatically = false;
            TargetLayerOverlayType = TargetLayerOverlayType.Static;
            DrawingQuality = DrawingQuality.HighQuality;
            TileType = TileType.HybridTile;
        }

        public string Proj4ProjectionParameters
        {
            get { return proj4ProjectionParameters; }
            set { proj4ProjectionParameters = value; }
        }

        public Action<AddLayersParameters> LayersAdded { get; set; }

        public Action<AddLayersParameters> LayersAdding { get; set; }

        public Collection<Layer> LayersToAdd { get { return layersToAdd; } }

        public int TileSize { get; set; }

        public bool IsCacheEnabled { get; set; }

        public bool IsMaxRecordsToDrawEnabled { get; set; }

        public bool ZoomToExtentAutomatically { get; set; }

        public bool ZoomToExtentOfFirstAutomatically { get; set; }

        public Collection<Type> ExcludedLayerTypesToZoomTo { get { return excludedLayerTypesToZoomTo; } }

        public int MaxRecordsToDraw { get; set; }

        public TargetLayerOverlayType TargetLayerOverlayType { get; set; }

        public DrawingQuality DrawingQuality { get; set; }

        public TileType TileType { get; set; }
    }
}