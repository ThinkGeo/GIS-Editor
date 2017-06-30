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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class RasterLayerExtension
    {
        public static void InitializeProj4Projection(this RasterLayer rasterLayer, string internalProj4ProjectionParameter)
        {
            Proj4Projection projection = new Proj4Projection();
            projection.InternalProjectionParametersString = internalProj4ProjectionParameter;
            projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            rasterLayer.ImageSource.Projection = projection;
        }

        public static String GetInternalProj4ProjectionParameter(this RasterLayer rasterLayer)
        {
            Proj4Projection projection = rasterLayer.ImageSource.Projection as Proj4Projection;
            if (projection != null)
            {
                return projection.InternalProjectionParametersString;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}