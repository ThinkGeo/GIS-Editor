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
using System.Reflection;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class ConfigureFeatureLayerParameters
    {
        [Obfuscation]
        private Collection<Feature> addedFeatures;

        [Obfuscation]
        private Dictionary<string, Feature> updatedFeatures;

        [Obfuscation]
        private Collection<Feature> deletedFeatures;

        [Obfuscation]
        private Collection<FeatureSourceColumn> addedColumns;

        [Obfuscation]
        private Dictionary<string, FeatureSourceColumn> updatedColumns;

        [Obfuscation]
        private Collection<FeatureSourceColumn> deletedColumns;

        [Obfuscation]
        private Dictionary<string, object> customData;

        [Obfuscation]
        private Uri layerUri;

        [Obfuscation]
        private WellKnownType wellKnownType;

        [Obfuscation]
        private MemoColumnConvertMode memoColumnConvertMode;

        [Obfuscation]
        private LongColumnTruncateMode longColumnTruncateMode;

        [Obfuscation]
        private string proj4ProjectionParametersString;

        public ConfigureFeatureLayerParameters()
            : this(null)
        { }

        public ConfigureFeatureLayerParameters(Uri layerUri)
            : this(layerUri, new Collection<FeatureSourceColumn>())
        { }

        // Here we need keep the FeatureSourceColumn.
        // It will be used in two places.
        // 1, used to export, it we need convert it into intermediate column and create it.
        // 2, used to create, the columns will be exactly column type.
        // so in the CreateFeatureLayer method, we need to first see if the column is IntermediateColumn
        // , if it is, we use it, or else use the one on the featuresource.
        public ConfigureFeatureLayerParameters(Uri layerUri, IEnumerable<FeatureSourceColumn> featureSourceColumns)
        {
            this.layerUri = layerUri;
            this.customData = new Dictionary<string, object>();
            this.memoColumnConvertMode = MemoColumnConvertMode.None;
            this.longColumnTruncateMode = LongColumnTruncateMode.None;
            this.proj4ProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();

            this.addedFeatures = new Collection<Feature>();
            this.updatedFeatures = new Dictionary<string, Feature>();
            this.deletedFeatures = new Collection<Feature>();
            this.addedColumns = new Collection<FeatureSourceColumn>();
            this.updatedColumns = new Dictionary<string, FeatureSourceColumn>();
            this.deletedColumns = new Collection<FeatureSourceColumn>();
            foreach (var column in featureSourceColumns)
            {
                this.addedColumns.Add(column);
            }
        }

        public MemoColumnConvertMode MemoColumnConvertMode
        {
            get { return memoColumnConvertMode; }
            set { memoColumnConvertMode = value; }
        }

        public LongColumnTruncateMode LongColumnTruncateMode
        {
            get { return longColumnTruncateMode; }
            set { longColumnTruncateMode = value; }
        }

        public Collection<Feature> AddedFeatures
        {
            get { return addedFeatures; }
        }

        public Collection<Feature> DeletedFeatures
        {
            get { return deletedFeatures; }
        }

        public Dictionary<string, Feature> UpdatedFeatures
        {
            get { return updatedFeatures; }
        }

        public Uri LayerUri
        {
            get { return layerUri; }
            set { layerUri = value; }
        }

        public WellKnownType WellKnownType
        {
            get { return wellKnownType; }
            set { wellKnownType = value; }
        }

        public string Proj4ProjectionParametersString
        {
            get { return proj4ProjectionParametersString; }
            set { proj4ProjectionParametersString = value; }
        }

        public Collection<FeatureSourceColumn> AddedColumns
        {
            get
            {
                return addedColumns;
            }
        }

        public Dictionary<string, FeatureSourceColumn> UpdatedColumns
        {
            get { return updatedColumns; }
        }

        public Collection<FeatureSourceColumn> DeletedColumns
        {
            get { return deletedColumns; }
        }

        public Dictionary<string, object> CustomData
        {
            get { return customData; }
        }
    }
}
