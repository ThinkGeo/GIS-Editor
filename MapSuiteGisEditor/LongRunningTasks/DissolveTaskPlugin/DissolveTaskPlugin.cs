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
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using DissolveWizard;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;
using ThinkGeo.MapSuite.WpfDesktopEdition.Extension;

namespace LongRunningTaskPlugins
{
    public class DissolveTaskPlugin : LongRunningTaskPlugin
    {
        private IEnumerable<string> matchColumns;
        private IEnumerable<OperatorPair> operatorPairs;
        private string outputPath;
        private string Wkt;
        private Collection<FeatureSourceColumn> featureColumns;
        private FeatureSource featureSource;

        public DissolveTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool parametersValid = TryParseParameters(parameters);
            if (parametersValid)
            {
                Dissolve();
            }
        }

        private bool TryParseParameters(Dictionary<string, string> parameters)
        {
            string[] parameterNames = 
            {
                "OutputPath",
                "Wkt", 
                "MatchColumns", 
                "OperatorPairs",
                "FeatureSourceXml"
            };

            bool allExist = parameterNames.All(name => parameters.ContainsKey(name));

            if (allExist)
            {
                outputPath = parameters[parameterNames[0]];
                Wkt = parameters[parameterNames[1]];
                matchColumns = GetMatchColumnsFromString(parameters[parameterNames[2]]);
                operatorPairs = GetOperatorPairsFromString(parameters[parameterNames[3]]);
                featureSource = GetFeatureSourceFromString(parameters[parameterNames[4]]);
            }

            return allExist;
        }

        private FeatureSource GetFeatureSourceFromString(string xml)
        {
            GeoSerializer serializer = new GeoSerializer();
            return (FeatureSource)serializer.Deserialize(xml);
        }

        private IEnumerable<OperatorPair> GetOperatorPairsFromString(string operatorPairsInString)
        {
            byte[] byteArray = Convert.FromBase64String(operatorPairsInString);

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                return (IEnumerable<OperatorPair>)formatter.Deserialize(stream);
            }
        }

        private IEnumerable<string> GetMatchColumnsFromString(string matchColumnsInString)
        {
            string[] columns = matchColumnsInString.Split(',');
            return columns;
        }

        private void Dissolve()
        {
            try
            {
                Collection<Feature> dissolvedFeatures = new Collection<Feature>();
                // get features to dissolve.
                Collection<Feature> featuresToDissolve = GetFeaturesToDissolve();
                // group features.
                var featureGroupByShapeType = featuresToDissolve.GroupBy(f =>
                {
                    return f.GetShape().GetType();
                });

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
                                }

                                newColumnValues.Add("Count", groupFeature.Count().ToString());

                                try
                                {
                                    if (finalProcessAction != null)
                                    {
                                        finalProcessAction(groupFeature, newColumnValues);
                                    }
                                }
                                catch { }
                            });
                        });

                featureGroupByShapeType.ForEach(groupByType =>
                {
                    //CancelTask(this.CancellationSource.Token, content, parameter);
                    if (groupByType.Key.IsSubclassOf(typeof(AreaBaseShape)))
                    {
                        dissolveAction(groupByType, (tmpFeatures, tmpColumnValues) =>
                        {
                            MultipolygonShape dissolveShape = AreaBaseShape.Union(GetValidFeatures(tmpFeatures));
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
                            //MultilineShape dissolveShape = LineBaseShape.Union(tmpFeatures);
                            //Feature dissolveFeature = new Feature(dissolveShape, tmpColumnValues);
                            //dissolveFeatures.Add(dissolveFeature);
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

                string saveFolder = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }

                ShpFileExporter exporter = new ShpFileExporter();
                exporter.ExportToFile(new FileExportInfo(dissolvedFeatures, filteredFeatureColumns, outputPath, Wkt));
            }
            catch (Exception e)
            {

            }
            finally
            {
                var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                args.Message = "Finished";

                OnUpdatingProgress(args);
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
                matchColumnString.TrimEnd(',');
                return matchColumnString;
            });
        }

        private Collection<Feature> GetFeaturesToDissolve()
        {
            featureSource.Open();
            featureColumns = featureSource.GetColumns();
            var allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
            featureSource.Close();

            return allFeatures;
        }

        private static Feature CollectNewColumnValues(IGrouping<string, Feature> groupFeature, Dictionary<string, string> newColumnValues, OperatorPair operatorPair)
        {
            var getIntColumnValueFunc = new Func<Feature, int>(f =>
            {
                int currentValue = 0;
                if (f.ColumnValues.ContainsKey(operatorPair.ColumnName))
                {
                    Int32.TryParse(f.ColumnValues[operatorPair.ColumnName], out currentValue);
                }
                return currentValue;
            });

            var getDoubleColumnValueFunc = new Func<Feature, double>(f =>
            {
                double currentValue = 0;
                if (f.ColumnValues.ContainsKey(operatorPair.ColumnName))
                {
                    Double.TryParse(f.ColumnValues[operatorPair.ColumnName], out currentValue);
                }
                return currentValue;
            });

            Feature feature = new Feature();
            switch (operatorPair.Operator)
            {
                case OperatorMode.First:
                    feature = groupFeature.FirstOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(operatorPair.ColumnName, feature.ColumnValues[operatorPair.ColumnName]);
                    }
                    break;
                case OperatorMode.Last:
                    feature = groupFeature.LastOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(operatorPair.ColumnName, feature.ColumnValues[operatorPair.ColumnName]);
                    }
                    break;
                case OperatorMode.Count:
                    newColumnValues.Add(operatorPair.ColumnName, groupFeature.Count().ToString());
                    break;
                case OperatorMode.Sum:
                    if (operatorPair.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        int intSum = groupFeature.Sum(getIntColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, intSum.ToString());
                    }
                    else if (operatorPair.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double doubleSum = groupFeature.Sum(getDoubleColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, doubleSum.ToString());
                    }
                    break;
                case OperatorMode.Average:
                    if (operatorPair.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase) || operatorPair.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double averageValue = groupFeature.Average(getDoubleColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, averageValue.ToString());
                    }
                    break;
                case OperatorMode.Min:
                    if (operatorPair.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        int intMin = groupFeature.Min(getIntColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, intMin.ToString());
                    }
                    else if (operatorPair.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double doubleMin = groupFeature.Min(getDoubleColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, doubleMin.ToString());
                    }
                    break;
                case OperatorMode.Max:
                    if (operatorPair.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        int intMin = groupFeature.Max(getIntColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, intMin.ToString());
                    }
                    else if (operatorPair.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double doubleMin = groupFeature.Max(getDoubleColumnValueFunc);
                        newColumnValues.Add(operatorPair.ColumnName, doubleMin.ToString());
                    }
                    break;
            }
            return feature;
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
            Collection<Feature> validFeatures = new Collection<Feature>();
            Parallel.ForEach(features, tmpFeature =>
            {
                var validateResult = tmpFeature.GetShape().Validate(ShapeValidationMode.Simple);
                if (validateResult.IsValid)
                {
                    validFeatures.Add(tmpFeature);
                }
                else
                {
                    HandleExceptionFromInvalidFeature(tmpFeature.Id, validateResult.ValidationErrors);
                }
            });
            return validFeatures;
        }

        private void HandleExceptionFromInvalidFeature(string id, string message)
        {
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
            args.ExceptionInfo = new LongRunningTaskExceptionInfo(message, null);
            args.Message = id;

            OnUpdatingProgress(args);
        }
    }
}
