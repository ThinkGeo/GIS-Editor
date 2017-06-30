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
using System.IO;
using System.Runtime.Serialization;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [DataContract]
    [Serializable]
    public class CSVInfoModel
    {
        private string csvFileName;
        private string delimiter;
        private DelimitedSpatialColumnsType mappingType;
        private string longitudeColumnName;
        private string latitudeColumnName;
        private string wktColumnName;

        public CSVInfoModel()
            : this(null, null)
        { }

        public CSVInfoModel(string csvFileName, string delimiter)
        {
            this.csvFileName = csvFileName;
            this.delimiter = delimiter;
        }

        [DataMember]
        public string CSVFileName
        {
            get { return csvFileName; }
            set { csvFileName = value; }
        }

        [DataMember]
        public string Delimiter
        {
            get { return delimiter; }
            set { delimiter = value; }
        }

        [DataMember]
        public DelimitedSpatialColumnsType MappingType
        {
            get { return mappingType; }
            set { mappingType = value; }
        }

        [DataMember]
        public string LongitudeColumnName
        {
            get { return longitudeColumnName; }
            set { longitudeColumnName = value; }
        }

        public string LatitudeColumnName
        {
            get { return latitudeColumnName; }
            set { latitudeColumnName = value; }
        }

        [DataMember]
        public string WktColumnName
        {
            get { return wktColumnName; }
            set { wktColumnName = value; }
        }

        public static CSVInfoModel FromCSVModel(CSVModel model)
        {
            CSVInfoModel entity = new CSVInfoModel();
            if (model != null)
            {
                entity.CSVFileName = model.CsvFeatureLayer.DelimitedPathFilename;
                entity.Delimiter = model.Delimiter;
                entity.LatitudeColumnName = model.LatitudeColumnName;
                entity.LongitudeColumnName = model.LongitudeColumnName;
                entity.MappingType = model.MappingType;
                entity.WktColumnName = model.WktColumnName;
                return entity;
            }
            return entity;
        }

        public static CSVInfoModel FromConfig(string configFileName)
        {
            CSVInfoModel entity = new CSVInfoModel();
            if (File.Exists(configFileName))
            {
                using (StreamReader sr = new StreamReader(configFileName))
                {
                    entity.Delimiter = sr.ReadLine();
                    DelimitedSpatialColumnsType mappingType = DelimitedSpatialColumnsType.XAndY;
                    Enum.TryParse<DelimitedSpatialColumnsType>(sr.ReadLine(), out mappingType);
                    entity.MappingType = mappingType;
                    entity.LongitudeColumnName = sr.ReadLine();
                    entity.LatitudeColumnName = sr.ReadLine();
                    entity.WktColumnName = sr.ReadLine();
                    entity.CSVFileName = configFileName.Remove(configFileName.LastIndexOf('.'));
                }
            }

            return entity;
        }

        public CSVInfoModel CloneDeep()
        {
            DataContractSerializer serializer = new DataContractSerializer(GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Seek(0, SeekOrigin.Begin);
                return serializer.ReadObject(ms) as CSVInfoModel;
            }
        }
    }
}