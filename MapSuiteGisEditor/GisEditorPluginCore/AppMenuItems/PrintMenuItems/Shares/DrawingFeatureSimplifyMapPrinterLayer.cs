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
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DrawingFeatureSimplifyMapPrinterLayer : SimplifyMapPrinterLayer
    {
        public DrawingFeatureSimplifyMapPrinterLayer()
        {
            DrawDescription = false;
            EnableClipping = false;
        }

        public InMemoryFeatureLayer FeatureLayer
        {
            get
            {
                if (this.Layers.Count > 0 && this.Layers[0] is InMemoryFeatureLayer)
                {
                    return this.Layers[0] as InMemoryFeatureLayer;
                }
                else return null;
            }
        }

        public Feature Feature
        {
            get
            {
                if (FeatureLayer != null)
                {
                    return FeatureLayer.InternalFeatures.FirstOrDefault();
                }
                else return null;
            }
        }

        public AreaStyle DefaultAreaStyle
        {
            get
            {
                if (FeatureLayer != null)
                {
                    return FeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle;
                }
                else return null;
            }
        }

        public LineStyle DefaultLineStyle
        {
            get
            {
                if (FeatureLayer != null)
                {
                    return FeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle;
                }
                else return null;
            }
        }

        public PointStyle DefaultPointStyle
        {
            get
            {
                if (FeatureLayer != null)
                {
                    return FeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle;
                }
                else return null;
            }
        }

        [OnGeodeserialized]
        private void OnDeserialized()
        {
            DrawDescription = false;
            EnableClipping = false;
        }
    }
}
