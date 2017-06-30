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
using System.IO;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class CsvFeatureLayer : FeatureLayer, IDisposable
    {
        [Obfuscation(Exclude = true)]
        private CsvFeatureSource featureSource;

        public CsvFeatureLayer() :
            this(string.Empty)
        { }

        public CsvFeatureLayer(string csvFileName)
            : this(csvFileName, string.Empty, string.Empty)
        { }

        public CsvFeatureLayer(string csvFileName, string wktColumnName, string delimiter)
        {
            Name = Path.GetFileNameWithoutExtension(csvFileName);
            featureSource = new CsvFeatureSource(csvFileName);
            FeatureSource = featureSource;

            CsvPathFileName = csvFileName;
            WktColumnName = wktColumnName;
            Delimiter = delimiter;
        }

        public string CsvPathFileName
        {
            get { return featureSource.CsvPathFileName; }
            set { featureSource.CsvPathFileName = value; }
        }

        public bool RequireIndex
        {
            get
            {
                return featureSource.RequireIndex;
            }
            set
            {
                featureSource.RequireIndex = value;
            }
        }

        public Encoding Encoding { get { return featureSource.Encoding; } set { featureSource.Encoding = value; } }

        public string Delimiter { get { return featureSource.Delimiter; } set { featureSource.Delimiter = value; } }

        public string LongitudeColumnName { get { return featureSource.LongitudeColumnName; } set { featureSource.LongitudeColumnName = value; } }

        public string LatitudeColumnName { get { return featureSource.LatitudeColumnName; } set { featureSource.LatitudeColumnName = value; } }

        public string WktColumnName { get { return featureSource.WktColumnName; } set { featureSource.WktColumnName = value; } }

        public CsvMappingType MappingType { get { return featureSource.MappingType; } set { featureSource.MappingType = value; } }

        public override bool HasBoundingBox
        {
            get
            {
                return true;
            }
        }

        public static void CreateCsvFile(string csvPathFileName, IEnumerable<string> databaseColumns, IEnumerable<Feature> features, string delimiter, OverwriteMode overwriteMode)
        {
            CreateCsvFile(csvPathFileName, databaseColumns, features, delimiter, overwriteMode, Encoding.Default);
        }

        public static void CreateCsvFile(string csvPathFileName, IEnumerable<string> databaseColumns, IEnumerable<Feature> features, string delimiter, OverwriteMode overwriteMode, Encoding encoding)
        {
            CsvFeatureSource.CreateCsvFile(csvPathFileName, databaseColumns, features, delimiter, overwriteMode, encoding);
        }

        [Obsolete("This method will not be used anymore.")]
        public void BuildConfigurationFile()
        {
            BuildConfigurationFileCore();
        }

        [Obsolete("This method will not be used anymore.")]
        protected virtual void BuildConfigurationFileCore()
        {
            featureSource.BuildConfigurationFile();
        }

        public void BuildIndexFile()
        {
            BuildIndexFileCore();
        }

        protected virtual void BuildIndexFileCore()
        {
            featureSource.BuildIndexFile();
        }

        [Obsolete("This method will not be used anymore.")]
        public void LoadConfiguration()
        {
            LoadConfigurationCore();
        }

        [Obsolete("This method will not be used anymore.")]
        protected virtual void LoadConfigurationCore()
        {
            featureSource.LoadConfiguration();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (featureSource != null) featureSource.Dispose();
            }
        }

        ~CsvFeatureLayer()
        {
            Dispose(false);
        }


        #region wrapper

        public DelimitedSpatialColumnsType SpatialColumnType
        {
            get { return (DelimitedSpatialColumnsType)MappingType; }
            set { MappingType = (CsvMappingType)value; }
        }

        public string WellKnownTextColumnName
        {
            get { return WktColumnName; }
            set { WktColumnName = value; }
        }

        public string DelimitedPathFilename
        {
            get { return CsvPathFileName; }
            set { CsvPathFileName = value; }
        }

        public string XColumnName
        {
            get { return LongitudeColumnName; }
            set { LongitudeColumnName = value; }
        }

        public string YColumnName
        {
            get { return LatitudeColumnName; }
            set { LatitudeColumnName = value; }
        }

        public static void CreateDelimitedFile(string csvPathFilename, Collection<string> csvColumns, string delimiter, OverwriteMode overwriteMode)
        {
            CreateCsvFile(csvPathFilename, csvColumns, new Collection<Feature>(), delimiter, overwriteMode);
        }

        #endregion
    }
}