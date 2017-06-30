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
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ShapeFileExporter : FileExporter
    {
        public ShapeFileExporter()
        {
            IdentifierCore = typeof(ShapeFileFeatureLayer);
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
                DbfColumnType columnType = DbfColumnType.Character;
                try
                {
                    columnType = (DbfColumnType)Enum.Parse(typeof(DbfColumnType), column.TypeName);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                int length = column.MaxLength;
                if (length > 254)
                {
                    length = 254;
                    columnType = DbfColumnType.Memo;
                }
                else if (length <= 0)
                {
                    length = 254;
                }

                DbfColumn dbfColumn = new DbfColumn(column.ColumnName, columnType, length, GetDecimalLength(columnType, column.MaxLength));
                return dbfColumn;
            });

            try
            {

                ConfigureFeatureLayerParameters parameters = new ConfigureFeatureLayerParameters();
                foreach (var column in dbfColumns)
                {
                    parameters.AddedColumns.Add(column);
                }
                foreach (var feature in group)
                {
                    var newFeature = feature;
                    if (!feature.IsValid())
                    {
                        newFeature = feature.MakeValid();
                    }
                    Feature validFeature = new Feature(newFeature.GetWellKnownBinary(), newFeature.Id, feature.ColumnValues);
                    //foreach (var item in feature.LinkColumnValues)
                    //{
                    //    validFeature.LinkColumnValues.Add(item.Key, item.Value);
                    //}
                    parameters.AddedFeatures.Add(validFeature);
                }
                parameters.LayerUri = new Uri(path);
                parameters.LongColumnTruncateMode = LongColumnTruncateMode.Truncate;
                parameters.MemoColumnConvertMode = MemoColumnConvertMode.ToCharacter;
                switch (group.Key)
                {
                    case ShapeFileType.Null:
                    case ShapeFileType.Multipatch:
                    default:
                        parameters.WellKnownType = WellKnownType.Invalid;
                        break;
                    case ShapeFileType.Point:
                    case ShapeFileType.PointZ:
                    case ShapeFileType.PointM:
                    case ShapeFileType.Multipoint:
                    case ShapeFileType.MultipointM:
                    case ShapeFileType.MultipointZ:
                        parameters.WellKnownType = WellKnownType.Point;
                        break;
                    case ShapeFileType.Polyline:
                    case ShapeFileType.PolylineZ:
                    case ShapeFileType.PolylineM:
                        parameters.WellKnownType = WellKnownType.Line;
                        break;
                    case ShapeFileType.Polygon:
                    case ShapeFileType.PolygonZ:
                    case ShapeFileType.PolygonM:
                        parameters.WellKnownType = WellKnownType.Polygon;
                        break;
                }
                parameters.CustomData["Columns"] = parameters.AddedColumns;
                parameters.CustomData["CustomizeColumnNames"] = true;
                parameters.CustomData["EditedColumns"] = info.CostomizedColumnNames;
                parameters.Proj4ProjectionParametersString = info.ProjectionWkt;
                var layerPlugin = GisEditor.LayerManager.GetActiveLayerPlugins<ShapeFileFeatureLayerPlugin>().FirstOrDefault();
                var layer = layerPlugin.CreateFeatureLayer(parameters);
                SavePrjFile(path, info.ProjectionWkt);
                RebuildDbf(path);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));

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