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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class BasReader
    {
        private BinaryReader basReader;

        public BasReader(string basFile, FileAccess access)
        {
            Validator.CheckTobinBasFileName(basFile);

            basReader = new BinaryReader(new FileStream(basFile, FileMode.Open, access));
        }

        public Collection<string> GetColumns()
        {
            Collection<string> allColumns = new Collection<string>();

            basReader.BaseStream.Seek(0, SeekOrigin.Begin);
            BasEntity firstRecord = BasEntityParser.Parse(basReader);
            if (firstRecord != null)
            {
                foreach (var column in Enum.GetValues(typeof(BasDefaultColumns)))
                {
                    allColumns.Add(column.ToString());
                }
                foreach (var column in firstRecord.HeaderResult.VariableData.Keys)
                {
                    allColumns.Add(column);
                }
            }

            return allColumns;
        }

        public BasFeatureEntity GetFeatureEntityByOffset(long offset)
        {
            BasFeatureEntity feature = null;

            basReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            BasEntity entity = BasEntityParser.Parse(basReader);

            if (entity != null)
            {
                feature = BasEntityToFeatureEntityConverter.Convert(entity);
            }

            return feature;
        }

        public Collection<BasFeatureEntity> GetAllFeatureEntities()
        {
            Collection<BasFeatureEntity> features = new Collection<BasFeatureEntity>();

            basReader.BaseStream.Seek(0, SeekOrigin.Begin);
            while (basReader.BaseStream.Position != -1 && basReader.BaseStream.Length - basReader.BaseStream.Position > 80)
            {
                BasEntity entity = BasEntityParser.Parse(basReader);

                if (entity != null)
                {
                    features.Add(BasEntityToFeatureEntityConverter.Convert(entity));
                }
            }
            return features;
        }

        public void Close()
        {
            if (basReader != null)
            {
                basReader.Close();
                basReader = null;
            }
        }
    }
}