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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DissolveTaskPlugin : GeoTaskPlugin
    {
        private Collection<FeatureSourceColumn> featureColumns;
        private string outputPathFileName;
        private string wkt;
        private IEnumerable<string> matchColumns;
        private IEnumerable<OperatorPair> operatorPairs;
        private FeatureSource featureSource;
        private bool isCanceled;

        public DissolveTaskPlugin()
        {
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public string Wkt
        {
            get { return wkt; }
            set { wkt = value; }
        }

        public IEnumerable<string> MatchColumns
        {
            get { return matchColumns; }
            set { matchColumns = value; }
        }

        public IEnumerable<OperatorPair> OperatorPairs
        {
            get { return operatorPairs; }
            set { operatorPairs = value; }
        }

        public FeatureSource FeatureSource
        {
            get { return featureSource; }
            set { featureSource = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("DissolveTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            Dissolve();
        }

        //private IEnumerable<string> GetMatchColumnsFromString(string matchColumnsInString)
        //{
        //    string[] columns = matchColumnsInString.Split(',');
        //    return columns;
        //}

        private void Dissolve()
        {
            Collection<Feature> dissolvedFeatures = new Collection<Feature>();

            // get features to dissolve.
            Collection<Feature> featuresToDissolve = GetFeaturesToDissolve();

            // group features.
            var featureGroupByShapeType = featuresToDissolve.GroupBy(f =>
            {
                return f.GetShape().GetType();
            });

            // collect export columns.
            var filteredFeatureColumns = new Collection<FeatureSourceColumn>();
            foreach (var matchColumn in this.matchColumns)
            {
                var tmpColumn = featureColumns.FirstOrDefault(f => f.ColumnName.Equals(matchColumn, StringComparison.Ordinal));
                if (tmpColumn != null) filteredFeatureColumns.Add(tmpColumn);
            }

            foreach (var extraColumn in this.operatorPairs)
            {
                var tmpColumn = featureColumns.FirstOrDefault(f => f.ColumnName.Equals(extraColumn.ColumnName, StringComparison.Ordinal));
                if (tmpColumn != null) filteredFeatureColumns.Add(tmpColumn);
            }

            filteredFeatureColumns.Add(new FeatureSourceColumn("Count", "Integer", 8));

            Action<IGrouping<Type, Feature>, Action<IEnumerable<Feature>, Dictionary<string, string>>> dissolveAction
                = new Action<IGrouping<Type, Feature>, Action<IEnumerable<Feature>, Dictionary<string, string>>>
                    ((groupByType, finalProcessAction) =>
                    {
                        //CancelTask(this.CancellationSource.Token, content, parameter);
                        var groupByColumns = GroupByColumns(groupByType, this.matchColumns);
                        groupByColumns.ForEach(groupFeature =>
                        {
                            //CancelTask(this.CancellationSource.Token, content, parameter);
                            Dictionary<string, string> newColumnValues = new Dictionary<string, string>();
                            foreach (var tmpMatchColumn in this.matchColumns)
                            {
                                newColumnValues.Add(tmpMatchColumn, groupFeature.First().ColumnValues[tmpMatchColumn]);
                            }

                            foreach (var operatorPair in this.operatorPairs)
                            {
                                //CancelTask(this.CancellationSource.Token, content, parameter);
                                CollectNewColumnValues(groupFeature, newColumnValues, operatorPair);
                                var value = newColumnValues[operatorPair.ColumnName];
                                var resultColumn = filteredFeatureColumns.FirstOrDefault(c => c.ColumnName.Equals(operatorPair.ColumnName));
                                if (resultColumn != null)
                                {
                                    if (resultColumn.MaxLength < value.Length)
                                    {
                                        var indexOf = filteredFeatureColumns.IndexOf(resultColumn);
                                        filteredFeatureColumns.RemoveAt(indexOf);
                                        var newColumn = new FeatureSourceColumn(resultColumn.ColumnName, resultColumn.TypeName, value.Length);
                                        filteredFeatureColumns.Insert(indexOf, newColumn);
                                    }
                                }
                            }

                            newColumnValues.Add("Count", groupFeature.Count().ToString());

                            try
                            {
                                if (finalProcessAction != null)
                                {
                                    finalProcessAction(groupFeature, newColumnValues);
                                }
                            }
                            catch (Exception e)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                                throw e;
                            }
                        });
                    });

            foreach (var groupByType in featureGroupByShapeType)
            {
                try
                {
                    //CancelTask(this.CancellationSource.Token, content, parameter);
                    if (groupByType.Key.IsSubclassOf(typeof(AreaBaseShape)))
                    {
                        dissolveAction(groupByType, (tmpFeatures, tmpColumnValues) =>
                        {
                            //MultipolygonShape dissolveShape = AreaBaseShape.Union(GetValidFeatures(tmpFeatures));
                            List<AreaBaseShape> areaShapes = GetValidFeatures(tmpFeatures)
                                .Select(f => f.GetShape())
                                .OfType<AreaBaseShape>()
                                .ToList();

                            MultipolygonShape dissolveShape = (MultipolygonShape)SqlTypesGeometryHelper.Union(areaShapes);

                            if (dissolveShape != null)
                            {
                                Feature dissolveFeature = new Feature(dissolveShape, tmpColumnValues);
                                dissolvedFeatures.Add(dissolveFeature);
                            }
                        });
                    }
                    else if (groupByType.Key.IsSubclassOf(typeof(LineBaseShape)))
                    {
                        dissolveAction(groupByType, (tmpFeatures, tmpColumnValues) =>
                        {
                            MultilineShape dissolveShape = new MultilineShape();
                            tmpFeatures.ForEach(tmpFeature =>
                            {
                                BaseShape tmpShape = tmpFeature.GetShape();
                                LineShape tmpLine = tmpShape as LineShape;
                                MultilineShape tmpMLine = tmpShape as MultilineShape;
                                if (tmpLine != null)
                                {
                                    dissolveShape.Lines.Add(tmpLine);
                                }
                                else if (tmpMLine != null)
                                {
                                    tmpMLine.Lines.ForEach(tmpLineInMLine => dissolveShape.Lines.Add(tmpLineInMLine));
                                }
                            });

                            if (dissolveShape.Lines.Count > 0)
                            {
                                dissolvedFeatures.Add(new Feature(dissolveShape, tmpColumnValues));
                            }
                        });
                    }
                    else if (groupByType.Key.IsSubclassOf(typeof(PointBaseShape)))
                    {
                        dissolveAction(groupByType, (tmpFeatures, tmpColumnValues) =>
                        {
                            MultipointShape multipointShape = new MultipointShape();
                            tmpFeatures.ForEach(tmpFeature =>
                            {
                                BaseShape tmpShape = tmpFeature.GetShape();
                                PointShape tmpPoint = tmpShape as PointShape;
                                MultipointShape tmpMPoint = tmpShape as MultipointShape;
                                if (tmpPoint != null)
                                {
                                    multipointShape.Points.Add(tmpPoint);
                                }
                                else if (tmpMPoint != null)
                                {
                                    tmpMPoint.Points.ForEach(tmpPointInMPointShape => multipointShape.Points.Add(tmpPointInMPointShape));
                                }
                            });
                            dissolvedFeatures.Add(new Feature(multipointShape, tmpColumnValues));
                        });
                    }
                    if (isCanceled)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    var errorEventArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    errorEventArgs.Error = new ExceptionInfo(string.Format(CultureInfo.InvariantCulture, "Feature id: {0}, {1}"
                        , groupByType.FirstOrDefault().Id, ex.Message)
                        , ex.StackTrace
                        , ex.Source);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    errorEventArgs.Message = groupByType.FirstOrDefault().Id;
                    OnUpdatingProgress(errorEventArgs);
                    continue;
                }
            }
            if (!isCanceled)
            {
                string saveFolder = Path.GetDirectoryName(outputPathFileName);
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                var info = new FileExportInfo(dissolvedFeatures, filteredFeatureColumns, outputPathFileName, Wkt);
                Export(info);
            }
        }

        private static IEnumerable<IGrouping<string, Feature>> GroupByColumns(IGrouping<Type, Feature> groupByType, IEnumerable<string> matchColumns)
        {
            return groupByType.GroupBy(f =>
            {
                string matchColumnString = String.Empty;
                foreach (var columnName in matchColumns)
                {
                    if (f.ColumnValues.ContainsKey(columnName))
                    {
                        matchColumnString += f.ColumnValues[columnName];
                    }
                    else
                    {
                        matchColumnString += String.Empty;
                    }

                    matchColumnString += ",";
                }
                matchColumnString = matchColumnString.TrimEnd(',');
                return matchColumnString;
            });
        }

        private Collection<Feature> GetFeaturesToDissolve()
        {
            featureSource.Open();
            featureColumns = featureSource.GetColumns();
            var allFeatures = featureSource.GetAllFeatures(featureSource.GetDistinctColumnNames());
            featureSource.Close();

            return allFeatures;
        }

        private static Feature CollectNewColumnValues(IGrouping<string, Feature> groupFeature, Dictionary<string, string> newColumnValues, OperatorPair operatorPair)
        {
            Feature feature = new Feature();
            switch (operatorPair.Operator)
            {
                case DissolveOperatorMode.First:
                    feature = groupFeature.FirstOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(operatorPair.ColumnName, feature.ColumnValues[operatorPair.ColumnName]);
                    }
                    break;

                case DissolveOperatorMode.Last:
                    feature = groupFeature.LastOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(operatorPair.ColumnName, feature.ColumnValues[operatorPair.ColumnName]);
                    }
                    break;

                case DissolveOperatorMode.Count:
                    newColumnValues.Add(operatorPair.ColumnName, groupFeature.Count().ToString());
                    break;

                case DissolveOperatorMode.Sum:
                    DissolveNumbers(groupFeature
                        , newColumnValues
                        , operatorPair
                        , rs => rs.Sum()
                        , rs => rs.Sum());
                    break;

                case DissolveOperatorMode.Average:
                    DissolveNumbers(groupFeature
                    , newColumnValues
                    , operatorPair
                    , rs => rs.Average());
                    break;

                case DissolveOperatorMode.Min:
                    DissolveNumbers(groupFeature
                        , newColumnValues
                        , operatorPair
                        , rs => rs.Min()
                        , rs => rs.Min());
                    break;

                case DissolveOperatorMode.Max:
                    DissolveNumbers(groupFeature
                        , newColumnValues
                        , operatorPair
                        , rs => rs.Max()
                        , rs => rs.Max());
                    break;
            }
            return feature;
        }

        private static void DissolveNumbers(IEnumerable<Feature> groupFeature
            , Dictionary<string, string> newColumnValues
            , OperatorPair operatorPair
            , Func<IEnumerable<double>, double> processDouble
            , Func<IEnumerable<int>, int> processInt = null)
        {
            if (operatorPair.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
            {
                var parseResults = groupFeature.Select(f => GetIntColumnValue(operatorPair, f)).ToArray();
                if (parseResults.All(r => r.IsFailed))
                {
                    newColumnValues.Add(operatorPair.ColumnName, string.Empty);
                }
                else
                {
                    if (processInt != null)
                    {
                        int intSum = processInt(parseResults.Where(r => !r.IsFailed).Select(r => r.Result));
                        newColumnValues.Add(operatorPair.ColumnName, intSum.ToString());
                    }
                    else
                    {
                        double doubleSum = processDouble(parseResults.Where(r => !r.IsFailed)
                            .Select(r => (double)r.Result));
                        newColumnValues.Add(operatorPair.ColumnName, doubleSum.ToString());
                    }
                }
            }
            else if (operatorPair.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase)
                || operatorPair.ColumnType.Equals("String", StringComparison.OrdinalIgnoreCase))
            {
                var parseResults = groupFeature.Select(f => GetDoubleColumnValue(operatorPair, f)).ToArray();
                if (parseResults.All(r => r.IsFailed))
                {
                    newColumnValues.Add(operatorPair.ColumnName, string.Empty);
                }
                else
                {
                    double doubleSum = processDouble(parseResults.Where(r => !r.IsFailed).Select(r => r.Result));
                    newColumnValues.Add(operatorPair.ColumnName, doubleSum.ToString());
                }
            }
        }

        private static ParseResult<double> GetDoubleColumnValue(OperatorPair operatorPair, Feature feature)
        {
            double currentValue = 0;
            bool isFailed = true;
            if (feature.ColumnValues.ContainsKey(operatorPair.ColumnName))
            {
                isFailed = !Double.TryParse(feature.ColumnValues[operatorPair.ColumnName], out currentValue);
            }
            return new ParseResult<double>(currentValue, isFailed);
        }

        private static ParseResult<int> GetIntColumnValue(OperatorPair operatorPair, Feature feature)
        {
            int currentValue = 0;
            bool isFailed = true;
            if (feature.ColumnValues.ContainsKey(operatorPair.ColumnName))
            {
                isFailed = !Int32.TryParse(feature.ColumnValues[operatorPair.ColumnName], out currentValue);
            }
            return new ParseResult<int>(currentValue, isFailed);
        }

        private static void RemoveRelatedFiles(string shapeFilePath)
        {
            Action<string, string> deleteFileIfExists = new Action<string, string>((shpPath, ext) =>
            {
                string actualPath = Path.ChangeExtension(shpPath, ext);
                if (File.Exists(actualPath))
                {
                    File.Delete(actualPath);
                }
            });

            deleteFileIfExists(shapeFilePath, ".shp");
            deleteFileIfExists(shapeFilePath, ".shx");
            deleteFileIfExists(shapeFilePath, ".dbf");
            deleteFileIfExists(shapeFilePath, ".prj");
            deleteFileIfExists(shapeFilePath, ".idx");
            deleteFileIfExists(shapeFilePath, ".ids");
        }

        private Collection<Feature> GetValidFeatures(IEnumerable<Feature> features)
        {
            ConcurrentQueue<Feature> validResult = new ConcurrentQueue<Feature>();
            Parallel.ForEach(features, tmpFeature =>
            {
                if (tmpFeature.CanMakeValid)
                {
                    tmpFeature = tmpFeature.MakeValid();
                    validResult.Enqueue(tmpFeature);
                }
                else
                {
                    var validateResult = tmpFeature.GetShape().Validate(ShapeValidationMode.Simple);
                    if (validateResult.IsValid)
                    {
                        validResult.Enqueue(tmpFeature);
                    }
                    else
                    {
                        HandleExceptionFromInvalidFeature(tmpFeature.Id, validateResult.ValidationErrors);
                    }
                }
            });
            Collection<Feature> validFeatures = new Collection<Feature>();
            foreach (var feature in validResult)
            {
                validFeatures.Add(feature);
            }
            return validFeatures;
        }

        private void HandleExceptionFromInvalidFeature(string id, string message)
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Error);
            args.Error = new ExceptionInfo(message, string.Empty, string.Empty);
            args.Message = id;

            OnUpdatingProgress(args);
            isCanceled = args.TaskState == TaskState.Canceled;
        }

        public class ParseResult<T>
        {
            public ParseResult()
            { }

            public ParseResult(T result)
                : this(result, false)
            { }

            public ParseResult(T result, bool isFailed)
            {
                this.Result = result;
                this.IsFailed = isFailed;
            }

            public T Result { get; set; }

            public bool IsFailed { get; set; }
        }
    }
}