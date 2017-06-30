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


using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class LayerExtension
    {
        public static T Cast<T>(this Layer layer) where T : Layer
        {
            return layer as T;
        }

        public static string GetInternalProj4ProjectionParameters(this Layer layer)
        {
            string proj4Parameter = string.Empty;

            if (layer != null)
            {
                RasterLayer rasterLayer = layer as RasterLayer;
                FeatureLayer featureLayer = layer as FeatureLayer;
                if (rasterLayer != null)
                {
                    Proj4Projection projection = rasterLayer.ImageSource.Projection as Proj4Projection;
                    if (projection != null)
                    {
                        proj4Parameter = projection.InternalProjectionParametersString;
                    }
                    if (string.IsNullOrEmpty(proj4Parameter))
                    {
                        proj4Parameter = Proj4Projection.GetEpsgParametersString(4326);
                    }
                }
                else if (featureLayer != null)
                {
                    Proj4Projection projection = (featureLayer.FeatureSource.Projection) as Proj4Projection;
                    if (projection == null)
                    {
                        string projection4326String = Proj4Projection.GetEpsgParametersString(4326);
                        projection = new Proj4Projection(projection4326String, projection4326String);
                        projection.SyncProjectionParametersString();
                        featureLayer.FeatureSource.Projection = projection;
                        projection.Open();
                    }

                    proj4Parameter = projection.InternalProjectionParametersString;
                }
            }

            return proj4Parameter;
        }
    }
}
