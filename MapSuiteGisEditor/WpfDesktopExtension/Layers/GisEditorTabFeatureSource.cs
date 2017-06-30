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


using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class GisEditorTabFeatureLayer : TabFeatureLayer
    {
        public GisEditorTabFeatureLayer()
            : this(string.Empty)
        { }

        public GisEditorTabFeatureLayer(string tabPathFilename)
            : this(tabPathFilename, GeoFileReadWriteMode.Read)
        { }

        public GisEditorTabFeatureLayer(string tabPathFilename, GeoFileReadWriteMode readWriteMode)
            : base(tabPathFilename, readWriteMode)
        {
            FeatureSource = new GisEditorTabFeatureSource(tabPathFilename, readWriteMode);
        }
    }

    [Serializable]
    public class GisEditorTabFeatureSource : TabFeatureSource
    {
        public GisEditorTabFeatureSource(string tabPathFilename, GeoFileReadWriteMode readWriteMode)
            : base(tabPathFilename, readWriteMode)
        { }

        protected override Collection<Feature> GetFeaturesInsideBoundingBoxCore(RectangleShape boundingBox, IEnumerable<string> returningColumnNames)
        {
            var foundFeatures = base.GetFeaturesInsideBoundingBoxCore(boundingBox, returningColumnNames);
            return MakeFeaturesValid(foundFeatures);
        }

        protected override Collection<Feature> GetFeaturesByIdsCore(IEnumerable<string> ids, IEnumerable<string> returningColumnNames)
        {
            var foundFeatures = base.GetFeaturesByIdsCore(ids, returningColumnNames);
            return MakeFeaturesValid(foundFeatures);
        }

        private static Collection<Feature> MakeFeaturesValid(Collection<Feature> foundFeatures)
        {
            Collection<Feature> validFeatures = new Collection<Feature>();
            foreach (var feature in foundFeatures)
            {
                if (feature != null)
                {
                    var stGemetry = SqlGeometry.STGeomFromWKB(new SqlBytes(feature.GetWellKnownBinary()), 0).MakeValid();
                    validFeatures.Add(new Feature(stGemetry.STAsBinary().Value, feature.Id, feature.ColumnValues));
                }
            }
            return validFeatures;
        }
    }
}