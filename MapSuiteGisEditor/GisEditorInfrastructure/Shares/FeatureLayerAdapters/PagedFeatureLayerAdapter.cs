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
using System.Data;
using System.Linq;
using System.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal class PagedFeatureLayerAdapter : FeatureLayerAdapter
    {
        public static readonly int BufferSize = 800;
        public static readonly int IncreaseSize = 200;

        private DataTable dataTable;
        private int rowCount;
        private FeatureLayer featureLayer;
        private Collection<string> uriColumnNames;

        public PagedFeatureLayerAdapter(FeatureLayer featureLayer, Collection<string> linkColumnNames)
            : base(featureLayer, linkColumnNames)
        {
            this.featureLayer = featureLayer;
            this.uriColumnNames = linkColumnNames;
        }

        protected override int GetCountCore()
        {
            if (rowCount == 0)
            {
                featureLayer.SafeProcess(() =>
                {
                    rowCount = featureLayer.FeatureSource.GetCount();
                });
            }
            return rowCount;
        }

        protected override DataTable GetDataTableCore()
        {
            int recordCount = GetCount();
            int actualRequestCount = recordCount >= BufferSize ? BufferSize : recordCount;
            if (actualRequestCount == 0)
            {
                return new DataTable();
            }
            dataTable = InitializeTableColumns();

            Collection<Feature> features = new Collection<Feature>();

            featureLayer.FeatureSource.SafeProcess(() =>
            {
                try
                {
                    features = featureLayer.FeatureSource.GetAllFeatures(featureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName).ToArray(), 0, actualRequestCount);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, ex);
                }
            });
            FillFeaturesToDataTable(features, dataTable);
            return dataTable;
        }

        protected override DataTable GetDataTableCore(DataTable currentDataTable, int length)
        {
            try
            {
                Monitor.Enter(featureLayer);
                featureLayer.Open();

                Collection<Feature> features = new Collection<Feature>();

                featureLayer.FeatureSource.SafeProcess(() =>
                {
                    features = featureLayer.FeatureSource.GetAllFeatures(featureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName).ToArray(), currentDataTable.Rows.Count, length);
                });
                FillFeaturesToDataTable(features, currentDataTable);
                return currentDataTable;
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                return currentDataTable;
            }
            finally
            {
                featureLayer.Close();
                Monitor.Exit(featureLayer);
            }
        }

        protected override DataTable GetDataTableCore(IEnumerable<string> featureIds)
        {
            DataTable resultDataTable = InitializeTableColumns();
            Collection<Feature> features = new Collection<Feature>();
            featureLayer.FeatureSource.SafeProcess(() =>
            {
                try
                {
                    //features = featureLayer.FeatureSource.GetFeaturesByIds(featureIds, featureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName).ToArray(), null);
                    features = featureLayer.FeatureSource.GetFeaturesByIds(featureIds, featureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName).ToArray());
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, ex);
                }
            });
            FillFeaturesToDataTable(features, resultDataTable);
            return resultDataTable;
        }

        private void FillFeaturesToDataTable(Collection<Feature> features, DataTable dataTable)
        {
            if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
            {
                CalculatedDbfColumn.UpdateCalculatedRecords(CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id], features, GisEditor.ActiveMap.DisplayProjectionParameters);
            }

            FillUriColumnNames(uriColumnNames, dataTable);

            foreach (var feature in features)
            {
                DataRow newRow = dataTable.NewRow();
                newRow[FeatureIdColumnName] = feature.Id;
                newRow[IsSelectedColumnName] = SelectedFeatures.Count > 0 && SelectedFeatures.ContainsKey(feature.Id);
                string tempPreviousValue = string.Empty;
                foreach (var item in feature.ColumnValues)
                {
                    if (!string.IsNullOrEmpty(item.Key) && dataTable.Columns.Contains(item.Key))
                    {
                        if (uriColumnNames.Contains(item.Key))
                        {
                            newRow[item.Key] = new Uri(item.Value, UriKind.RelativeOrAbsolute);
                        }
                        else
                        {
                            newRow[item.Key] = string.IsNullOrEmpty(item.Value) ? DBNull.Value : (object)item.Value;
                        }
                        int previousRowCount = tempPreviousValue.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Count();
                        int currentRowCount = item.Value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Count();
                        if (previousRowCount != currentRowCount)
                        {
                            EnableColumnVirtualization = false;
                        }
                        tempPreviousValue = item.Value;
                    }
                }
                //foreach (var linkColumnValue in feature.LinkColumnValues)
                //{
                //    newRow[linkColumnValue.Key] = string.Join(Environment.NewLine, linkColumnValue.Value.Select(v => v.Value));
                //}
                dataTable.Rows.Add(newRow);
            }
        }

        //private List<string> GetLinkSourceNames(LinkSource linkSource)
        //{
        //    List<string> names = new List<string>();
        //    names.Add(linkSource.Name.ToUpperInvariant());
        //    if (linkSource.LinkSources.Count > 0)
        //    {
        //        foreach (var item in linkSource.LinkSources)
        //        {
        //            names.AddRange(GetLinkSourceNames(item));
        //        }
        //    }
        //    return names;
        //}

        private DataTable InitializeTableColumns()
        {
            DataTable resultDataTable = new DataTable();
            resultDataTable.Columns.Add(new DataColumn(FeatureIdColumnName, typeof(string)));
            resultDataTable.Columns.Add(new DataColumn(IsSelectedColumnName, typeof(string)));
            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();
            featureLayer.FeatureSource.SafeProcess(() =>
            {
                columns = featureLayer.FeatureSource.GetColumns(GettingColumnsType.All);
            });

            //List<string> linkSourceNames = new List<string>();
            //foreach (var linkSource in featureLayer.FeatureSource.LinkSources)
            //{
            //    linkSourceNames.AddRange(GetLinkSourceNames(linkSource));
            //}

            if (CalculatedDbfColumn.CalculatedColumns.ContainsKey(featureLayer.FeatureSource.Id))
            {
                foreach (var item in CalculatedDbfColumn.CalculatedColumns[featureLayer.FeatureSource.Id])
                {
                    columns.Add(item);
                }
            }

            foreach (var item in columns)
            {
                if (!string.IsNullOrEmpty(item.ColumnName))
                {
                    if (resultDataTable.Columns.Contains(item.ColumnName))
                    {
                        throw new Exception("A duplicate column '" + item.ColumnName + "' is detected.");
                    }
                    //bool useStringType = false;
                    //int dotIndex = item.ColumnName.IndexOf(".");
                    //if (dotIndex >= 0)
                    //{
                    //    string potentialLinkSourceName = item.ColumnName.Substring(0, dotIndex).ToUpperInvariant();
                    //    useStringType = linkSourceNames.Contains(potentialLinkSourceName);
                    //}
                    //Type columnType = null;
                    //if (useStringType)
                    //{
                    //    columnType = typeof(string);
                    //}
                    //else
                    //{
                    //    columnType = GetColumnTypeFromDbfColumn(item.TypeName);
                    //}

                    Type columnType = GetColumnTypeFromDbfColumn(item.TypeName);
                    resultDataTable.Columns.Add(new DataColumn(item.ColumnName, columnType) { AllowDBNull = true });
                }
            }

            return resultDataTable;
        }

        private static Type GetColumnTypeFromDbfColumn(string columnType)
        {
            DbfColumnType dbfColumnType = DbfColumnType.Character;
            DbfColumnType tempColumnType = DbfColumnType.Character;
            if (Enum.TryParse<DbfColumnType>(columnType, out tempColumnType))
            {
                dbfColumnType = tempColumnType;
            }
            return GetColumnTypeFromDbfColumn(dbfColumnType);
        }

        private static Type GetColumnTypeFromDbfColumn(DbfColumnType columnType)
        {
            switch (columnType)
            {
                case DbfColumnType.Float:
                case DbfColumnType.Numeric:
                    return typeof(double);
                case DbfColumnType.Null:
                    return typeof(Nullable);
                case DbfColumnType.Date:

                //return typeof(DateTime);
                case DbfColumnType.Logical:
                case DbfColumnType.Character:
                case DbfColumnType.Memo:
                default:
                    return typeof(string);
            }
        }
    }
}