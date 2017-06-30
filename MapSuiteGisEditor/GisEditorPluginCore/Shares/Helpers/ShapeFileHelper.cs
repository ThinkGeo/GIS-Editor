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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class ShapeFileHelper
    {
        private int capabilityToFlush = 5000;
        private FeatureLayer featureLayer;
        private int currentAddedCount;

        public ShapeFileHelper(ShapeFileType shapeFileType, string pathFileName, IEnumerable<FeatureSourceColumn> columns, string projectionWkt)
        {
            Initialize(shapeFileType, pathFileName, ConvertToDbfColumns(columns), projectionWkt);
        }

        public ShapeFileHelper(ShapeFileType shapeFileType, string pathFileName, IEnumerable<DbfColumn> columns, string projectionWkt)
        {
            Initialize(shapeFileType, pathFileName, columns, projectionWkt);
        }

        public int CapabilityToFlush
        {
            get { return capabilityToFlush; }
            set { capabilityToFlush = value; }
        }

        public void Add(Feature feature)
        {
            feature = feature.MakeValidUsingSqlTypes();
            if (!featureLayer.IsOpen)
            {
                featureLayer.Open();
            }

            if (!featureLayer.EditTools.IsInTransaction)
            {
                featureLayer.EditTools.BeginTransaction();
            }

            try
            {
                currentAddedCount++;
                featureLayer.EditTools.Add(feature);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (currentAddedCount > capabilityToFlush)
                {
                    currentAddedCount = 0;
                    featureLayer.EditTools.CommitTransaction();
                }
            }
        }

        public void Commit()
        {
            try
            {
                if (!featureLayer.IsOpen) featureLayer.Open();

                if (featureLayer.EditTools.IsInTransaction)
                {
                    currentAddedCount = 0;
                    featureLayer.EditTools.CommitTransaction();
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (featureLayer.IsOpen) featureLayer.Close();
            }
        }

        public void ForEachFeatures(ShapeFileFeatureSource featureSource, Func<Feature, bool> process)
        {
            if (!featureSource.IsOpen) featureSource.Open();
            int currentFeatureCount = featureSource.GetCount();
            for (int i = 0; i < currentFeatureCount; i++)
            {
                var currentFeature = featureSource.GetFeatureById((i + 1).ToString(), featureSource.GetDistinctColumnNames());
                if (process != null)
                {
                    var canceled = process(currentFeature);
                    if (canceled) break;
                }
            }
        }

        public void ForEachFeatures(ShapeFileFeatureSource featureSource, Func<Feature, int, int, int, bool> process)
        {
            if (!featureSource.IsOpen) featureSource.Open();

            int currentProgress = 0;
            int currentFeatureCount = featureSource.GetCount();
            for (int i = 0; i < currentFeatureCount; i++)
            {
                var currentFeature = featureSource.GetFeatureById((i + 1).ToString(), featureSource.GetDistinctColumnNames());
                if (process != null)
                {
                    currentProgress++;
                    var currentPercentage = currentProgress * 100 / currentFeatureCount;
                    var canceled = process(currentFeature, currentProgress, currentFeatureCount, currentPercentage);
                    if (canceled) break;
                }
            }
        }

        private void Initialize(ShapeFileType shapeFileType, string pathFileName, IEnumerable<DbfColumn> columns, string projectionWkt)
        {
            string[] suffixes = { ".shp", ".shx", ".ids", ".idx", ".dbf", ".prj" };
            foreach (var suffix in suffixes)
            {
                string fileToRemove = Path.ChangeExtension(pathFileName, suffix);
                if (File.Exists(fileToRemove))
                {
                    File.Delete(fileToRemove);
                }
            }

            if (!string.IsNullOrEmpty(projectionWkt))
            {
                File.WriteAllText(Path.ChangeExtension(pathFileName, ".prj"), projectionWkt);
            }

            ShapeFileFeatureLayer.CreateShapeFile(shapeFileType, pathFileName, columns);
            featureLayer = new ShapeFileFeatureLayer(pathFileName, GeoFileReadWriteMode.ReadWrite);
        }

        private static Collection<DbfColumn> ConvertToDbfColumns(IEnumerable<FeatureSourceColumn> featureSourceColumns)
        {
            Collection<DbfColumn> dbfColumns = new Collection<DbfColumn>();
            foreach (var column in featureSourceColumns)
            {
                DbfColumnType columnType = (DbfColumnType)Enum.Parse(typeof(DbfColumnType), column.TypeName);
                DbfColumn dbfColumn = new DbfColumn(column.ColumnName, columnType, column.MaxLength, GetDecimalLength(columnType, column.MaxLength));
                dbfColumns.Add(dbfColumn);
            }
            return dbfColumns;
        }

        private static int GetDecimalLength(DbfColumnType dbfColumnType, int maxLength)
        {
            if (dbfColumnType == DbfColumnType.Float)
            {
                return maxLength < 4 ? maxLength : 4;
            }
            else
            {
                return 0;
            }
        }
    }
}