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
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class FileGeodatabaseExporter : FileExporter
    {
        public FileGeodatabaseExporter()
        {
            IdentifierCore = typeof(FileGeoDatabaseFeatureLayer);
        }

        protected override void ExportToFileCore(FileExportInfo info)
        {
            if (info.FeaturesToExport.Count() == 0)
            {
                throw new OperationCanceledException("Feature count is zero.");
            }
            var groupedFeatures = info.FeaturesToExport.GroupBy(f =>
            {
                return GetShapeFileType(f.GetShape());
            });

            foreach (var group in groupedFeatures)
            {
                if (group.Key != ShapeFileType.Null) Export(group, info);
            }
        }

        private static ShapeFileType GetShapeFileType(BaseShape shape)
        {
            if (shape is MultipointShape)
            {
                return ShapeFileType.Multipoint;
            }
            if (shape is PointShape)
            {
                return ShapeFileType.Point;
            }

            if (shape is AreaBaseShape)
            {
                return ShapeFileType.Polygon;
            }
            else if (shape is LineBaseShape)
            {
                return ShapeFileType.Polyline;
            }
            else
            {
                return ShapeFileType.Null;
            }
        }

        private void Export(IGrouping<ShapeFileType, Feature> group, FileExportInfo info)
        {
            string path = info.Path;
            if (File.Exists(path))
            {
                if (info.Overwrite)
                {
                    string[] suffixes = { ".shp", ".shx", ".ids", ".idx", ".dbf", ".prj" };
                    foreach (var suffix in suffixes)
                    {
                        string fileToRemove = Path.ChangeExtension(path, suffix);
                        if (File.Exists(fileToRemove))
                        {
                            File.Delete(fileToRemove);
                        }
                    }
                }
                else
                {
                    string dir = Path.GetDirectoryName(path);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    string extension = Path.GetExtension(path);

                    path = Path.Combine(dir, fileName + group.Key.ToString() + extension);
                }
            }

            var dbfColumns = info.Columns.Select(column =>
            {
                DbfColumnType columnType = (DbfColumnType)Enum.Parse(typeof(DbfColumnType), column.TypeName);
                DbfColumn dbfColumn = new DbfColumn(column.ColumnName, columnType, column.MaxLength, GetDecimalLength(columnType, column.MaxLength));
                return dbfColumn;
            });

            ShapeFileFeatureLayer.CreateShapeFile(group.Key, path, dbfColumns);
            ShapeFileFeatureLayer layer = new ShapeFileFeatureLayer(path, GeoFileReadWriteMode.ReadWrite);
            try
            {
                layer.Open();
                layer.EditTools.BeginTransaction();
                foreach (var feature in group)
                {
                    bool isValid = true;
                    var newFeature = feature;
                    if (!feature.IsValid())
                    {
                        if (feature.CanMakeValid) newFeature = feature.MakeValid();
                        else isValid = false;
                    }
                    if (isValid)
                    {
                        var featureSourceColumns = layer.FeatureSource.GetColumns();
                        var tempColumnNames = featureSourceColumns.Select(column => column.ColumnName);

                        var validColumns = GeoDbf.GetValidColumnNames(tempColumnNames);
                        Dictionary<string, string> columnValues = new Dictionary<string, string>();
                        for (int i = 0; i < validColumns.Count(); i++)
                        {
                            var columnName = dbfColumns.ElementAt(i).ColumnName;
                            if (newFeature.ColumnValues.ContainsKey(columnName))
                            {
                                columnValues.Add(validColumns.ElementAt(i), newFeature.ColumnValues[columnName]);
                            }
                        }
                        Feature validFeature = new Feature(newFeature.GetWellKnownBinary(), newFeature.Id, columnValues);
                        layer.EditTools.Add(validFeature);
                    }
                }
                layer.EditTools.CommitTransaction();
                layer.Close();

                SavePrjFile(path, info.ProjectionWkt);
                RebuildDbf(path);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                if (layer.EditTools.IsInTransaction)
                {
                    layer.EditTools.CommitTransaction();
                }

                if (layer.IsOpen)
                {
                    layer.Close();
                }

                string[] suffixes = { ".shp", ".shx", ".ids", ".idx", ".dbf", ".prj" };
                foreach (var suffix in suffixes)
                {
                    string fileToRemove = Path.ChangeExtension(path, suffix);
                    if (File.Exists(fileToRemove))
                    {
                        File.Delete(fileToRemove);
                    }
                }
                throw new OperationCanceledException("Shapefile generates failed.", ex);
            }
        }

        private void RebuildDbf(string path)
        {
            string dbfPath = Path.ChangeExtension(path, ".dbf");

            if (File.Exists(dbfPath))
            {
                File.SetAttributes(dbfPath, FileAttributes.Normal);

                using (GeoDbf geoDbf = new GeoDbf(dbfPath, GeoFileReadWriteMode.ReadWrite))
                {
                    geoDbf.Open();
                    int columnNumber = -1;
                    for (int i = 1; i <= geoDbf.ColumnCount; i++)
                    {
                        string columnName = geoDbf.GetColumnName(i);
                        if (columnName.Equals("RECID", StringComparison.OrdinalIgnoreCase))
                        {
                            columnNumber = i;
                            break;
                        }
                    }

                    if (columnNumber > -1)
                    {
                        for (int i = 1; i <= geoDbf.RecordCount; i++)
                        {
                            geoDbf.WriteField(i, columnNumber, i);
                        }
                    }
                }
            }
        }

        private int GetDecimalLength(DbfColumnType dbfColumnType, int maxLength)
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

        private void SavePrjFile(string shpPath, string wkt)
        {
            string prjPath = Path.ChangeExtension(shpPath, "prj");
            if (!File.Exists(prjPath) && !string.IsNullOrEmpty(wkt))
            {
                File.WriteAllText(prjPath, wkt);
            }
        }
    }
}
