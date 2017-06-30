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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FileExportInfo
    {
        private IEnumerable<Feature> featuresToExport;
        private string path;
        private IEnumerable<FeatureSourceColumn> columns;
        private string projectionInWKT;
        private bool overWriteIfExists;
        private Dictionary<string, string> costomizedColumnNames;

        public FileExportInfo(IEnumerable<Feature> features,
                              IEnumerable<FeatureSourceColumn> columns,
                              string path,
                              string projectionWkt,
            bool overwrite = true)
        {
            costomizedColumnNames = new Dictionary<string, string>();
            FeaturesToExport = features;
            Columns = columns;//.Where(c => Encoding.Default.GetByteCount(c.ColumnName) <= 10).ToArray();
            Path = path;
            ProjectionWkt = projectionWkt;
            Overwrite = overwrite;
        }

        public IEnumerable<Feature> FeaturesToExport
        {
            get { return featuresToExport; }
            set { featuresToExport = value; }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public IEnumerable<FeatureSourceColumn> Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public Dictionary<string, string> CostomizedColumnNames
        {
            get { return costomizedColumnNames; }
        }

        public string ProjectionWkt
        {
            get { return projectionInWKT; }
            set { projectionInWKT = value; }
        }

        public bool Overwrite
        {
            get { return overWriteIfExists; }
            set { overWriteIfExists = value; }
        }
    }
}