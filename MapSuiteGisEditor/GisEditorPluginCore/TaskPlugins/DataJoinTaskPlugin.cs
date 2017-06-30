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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DataJoinTaskPlugin : TaskPlugin
    {
        public string Delimiter { get; set; }

        public string OutputPathFileName { get; set; }

        public bool IsIncludeAllFeatures { get; set; }

        public int CodePage { get; set; }

        public string DisplayProjectionParameters { get; set; }

        public List<Feature> Features { get; set; }

        public ShapeFileFeatureSource ShapeFileFeatureSource { get; set; }

        public Dictionary<string, string> InvalidColumns { get; set; }

        public ObservableCollection<MatchCondition> MatchConditions { get; set; }

        public FeatureSourceColumn Condition1LayerColumn { get; set; }

        public FeatureSourceColumn DelimitedFileCondition1Column { get; set; }

        public FeatureSourceColumn Condition3LayerColumn { get; set; }

        public FeatureSourceColumn Condition2LayerColumn { get; set; }

        public FeatureSourceColumn DelimitedFileCondition2Column { get; set; }

        public FeatureSourceColumn DelimitedFileCondition3Column { get; set; }

        public ObservableCollection<FeatureSourceColumn> SourceLayerColumns { get; set; }

        public ObservableCollection<FeatureSourceColumn> IncludedColumnsList { get; set; }

        public DataJoinAdapter DataJoinAdapter { get; set; }

        public string CsvFilePath { get; set; }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("DataJoinTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            if (ShapeFileFeatureSource != null)
            {
                DataJoinShapeFile();
            }
            else
            {
                CreateShapeFile(IncludedColumnsList, OutputPathFileName, Encoding.GetEncoding(CodePage), CsvFilePath
                    , Features, IsIncludeAllFeatures, MatchConditions, OnUpdatingProgress, InvalidColumns);
            }
        }

        private void DataJoinShapeFile()
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            ShapeFileFeatureSource currentSource = ShapeFileFeatureSource;
            if (!currentSource.IsOpen) currentSource.Open();
            var index = 0;
            var count = currentSource.GetAllFeatures(ReturningColumnsType.AllColumns).Count;

            Collection<DbfColumn> includeColumns = new Collection<DbfColumn>();

            RemoveUnduplicateColumn(IncludedColumnsList);

            foreach (var column in IncludedColumnsList)
            {
                DbfColumnType tmpDbfColumnType = DbfColumnType.Character;
                if (Enum.TryParse(column.TypeName, out tmpDbfColumnType))
                {
                    DbfColumn dbfColumn = new DbfColumn(column.ColumnName, tmpDbfColumnType, column.MaxLength, 0);
                    includeColumns.Add(dbfColumn);
                }
            }

            ShapeFileType shapeFileType = GetShapeFileType(currentSource.GetAllFeatures(ReturningColumnsType.AllColumns).FirstOrDefault());
            var projectionWkt = Proj4Projection.ConvertProj4ToPrj(DisplayProjectionParameters);
            var dataTable = DataJoinAdapter.ReadDataToDataGrid(CsvFilePath, Delimiter);
            var featureRows = dataTable.Rows;

            var helper = new ShapeFileHelper(shapeFileType, OutputPathFileName, includeColumns, projectionWkt);
            helper.ForEachFeatures(currentSource, (f, currentProgress, upperBound, percentage) =>
            {
                try
                {
                    bool canceled = false;
                    if (f.GetWellKnownBinary() != null)
                    {
                        index++;
                        try
                        {
                            var matchedDataRow = featureRows.Cast<DataRow>().FirstOrDefault(r => MatchConditions.All(tmpCondition => f.ColumnValues[tmpCondition.SelectedLayerColumn.ColumnName]
                                        == r[tmpCondition.SelectedDelimitedColumn.ColumnName].ToString()));

                            if (matchedDataRow != null)
                            {
                                SetFeatureColumnValues(f, matchedDataRow, IncludedColumnsList, InvalidColumns);
                                helper.Add(f);
                            }
                            else if (IsIncludeAllFeatures)
                            {
                                helper.Add(f);
                            }

                            if (UpdateProgress(OnUpdatingProgress, index, count)) canceled = true;
                        }
                        catch (Exception ex)
                        {
                            var errorEventArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                            errorEventArgs.Error = new ExceptionInfo(string.Format(CultureInfo.InvariantCulture, "Feature id: {0}, {1}"
                                , f.Id, ex.Message)
                                , ex.StackTrace
                                , ex.Source);
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            errorEventArgs.Message = f.Id;
                            OnUpdatingProgress(errorEventArgs);
                        }
                    }

                    args = new UpdatingTaskProgressEventArgs(TaskState.Updating, percentage);
                    args.Current = currentProgress;
                    args.UpperBound = upperBound;
                    OnUpdatingProgress(args);

                    canceled = args.TaskState == TaskState.Canceled;
                    return canceled;
                }
                catch
                {
                    return false;
                }
            });

            helper.Commit();

            SavePrjFile(OutputPathFileName, DisplayProjectionParameters);
        }

        private void CreateShapeFile(ObservableCollection<FeatureSourceColumn> includedColumnsList
            , string OutputPath, Encoding ShapeFileEncoding, string csvFilePath
            , List<Feature> features
            , bool isIncludeAllFeatures
            , IEnumerable<MatchCondition> matchConditions
            , Action<UpdatingTaskProgressEventArgs> updateAction
            , Dictionary<string, string> invalidColumns)
        {
            Collection<DbfColumn> includeColumns = new Collection<DbfColumn>();

            RemoveUnduplicateColumn(includedColumnsList);

            foreach (var column in includedColumnsList)
            {
                DbfColumnType tmpDbfColumnType = DbfColumnType.Character;
                if (Enum.TryParse(column.TypeName, out tmpDbfColumnType))
                {
                    DbfColumn dbfColumn = new DbfColumn(column.ColumnName, tmpDbfColumnType, column.MaxLength, 0);
                    includeColumns.Add(dbfColumn);
                }
            }

            ShapeFileType shapeFileType = GetShapeFileType(features.FirstOrDefault());

            if (shapeFileType != ShapeFileType.Null)
            {
                ShapeFileFeatureLayer.CreateShapeFile(shapeFileType,
                OutputPath, includeColumns, ShapeFileEncoding, OverwriteMode.Overwrite);

                var dataTable = DataJoinAdapter.ReadDataToDataGrid(csvFilePath, Delimiter);
                var featureRows = dataTable.Rows;
                var index = 0;
                var count = features.Count;

                ShapeFileFeatureLayer newShapeFileFeatureLayer = new ShapeFileFeatureLayer(OutputPath, GeoFileReadWriteMode.ReadWrite);
                newShapeFileFeatureLayer.SafeProcess(() =>
                {
                    newShapeFileFeatureLayer.EditTools.BeginTransaction();

                    foreach (var feature in features)
                    {
                        index++;
                        try
                        {
                            var matchedDataRow = featureRows.Cast<DataRow>().FirstOrDefault(r => matchConditions.All(tmpCondition => feature.ColumnValues[tmpCondition.SelectedLayerColumn.ColumnName]
                                        == r[tmpCondition.SelectedDelimitedColumn.ColumnName].ToString()));

                            if (matchedDataRow != null)
                            {
                                SetFeatureColumnValues(feature, matchedDataRow, includedColumnsList, invalidColumns);
                                newShapeFileFeatureLayer.EditTools.Add(feature);
                            }
                            else if (isIncludeAllFeatures)
                            {
                                newShapeFileFeatureLayer.EditTools.Add(feature);
                            }

                            if (UpdateProgress(updateAction, index, count)) break;
                        }
                        catch (Exception ex)
                        {
                            var errorEventArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                            errorEventArgs.Error = new ExceptionInfo(string.Format(CultureInfo.InvariantCulture, "Feature id: {0}, {1}"
                                , feature.Id, ex.Message)
                                , ex.StackTrace
                                , ex.Source);
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            errorEventArgs.Message = feature.Id;
                            updateAction(errorEventArgs);
                        }
                    }
                    newShapeFileFeatureLayer.EditTools.CommitTransaction();
                });

                SavePrjFile(OutputPath, DisplayProjectionParameters);
            }
        }

        private static void RemoveUnduplicateColumn(ObservableCollection<FeatureSourceColumn> IncludedColumnsList)
        {
            Collection<FeatureSourceColumn> tmpColumns = new Collection<FeatureSourceColumn>();
            Collection<FeatureSourceColumn> duplicateColumns = new Collection<FeatureSourceColumn>();
            foreach (var item in IncludedColumnsList)
            {
                if (!tmpColumns.Select(f => f.ColumnName).Contains(item.ColumnName))
                    tmpColumns.Add(item);
                else
                {
                    duplicateColumns.Add(item);
                }
            }
            if (duplicateColumns.Count > 0)
            {
                foreach (var item in duplicateColumns)
                {
                    IncludedColumnsList.Remove(item);
                }
            }
        }

        private static bool UpdateProgress(Action<UpdatingTaskProgressEventArgs> updateAction, int index, int count)
        {
            var progressPercentage = index * 100 / count;
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
            args.Current = index;
            args.UpperBound = count;
            if (updateAction != null) updateAction(args);
            var isCanceled = args.TaskState == TaskState.Canceled;
            return isCanceled;
        }

        private static ShapeFileType GetShapeFileType(Feature feature)
        {
            ShapeFileType shapeFileType = ShapeFileType.Null;
            if (feature != null)
            {
                switch (feature.GetWellKnownType())
                {
                    case WellKnownType.Point:
                        shapeFileType = ShapeFileType.Point;
                        break;
                    case WellKnownType.Multipoint:
                        shapeFileType = ShapeFileType.Multipoint;
                        break;
                    case WellKnownType.Line:
                    case WellKnownType.Multiline:
                        shapeFileType = ShapeFileType.Polyline;
                        break;
                    case WellKnownType.Polygon:
                    case WellKnownType.Multipolygon:
                        shapeFileType = ShapeFileType.Polygon;
                        break;
                }
            }
            return shapeFileType;
        }

        private static void SetFeatureColumnValues(Feature feature, DataRow rowItem
            , IEnumerable<FeatureSourceColumn> IncludedColumnsList, Dictionary<string, string> invalidColumns)
        {
            foreach (var column in IncludedColumnsList.Where(tmpColumn => tmpColumn is DataJoinFeatureSourceColumn))
            {
                if (invalidColumns.ContainsKey(column.ColumnName))
                    feature.ColumnValues[column.ColumnName] = rowItem[invalidColumns[column.ColumnName]].ToString();
                else
                    feature.ColumnValues[column.ColumnName] = rowItem[column.ColumnName].ToString();
            }
        }

        private static void SavePrjFile(string shapeFileName, string parameters)
        {
            string wkt = Proj4Projection.ConvertProj4ToPrj(parameters);
            string prjPath = Path.ChangeExtension(shapeFileName, "prj");
            if (!File.Exists(prjPath))
            {
                File.WriteAllText(prjPath, wkt);
            }
            else
            {
                File.Delete(prjPath);
                File.WriteAllText(prjPath, wkt);
            }
        }
    }
}