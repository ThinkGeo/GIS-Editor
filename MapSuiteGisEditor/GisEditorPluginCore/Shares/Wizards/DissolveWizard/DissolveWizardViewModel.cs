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
using System.Linq;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DissolveWizardViewModel : WizardViewModel<DissolveWizardShareObject>
    {
        private DissolveWizardShareObject sharedObject;

        public DissolveWizardViewModel()
        {
            sharedObject = new DissolveWizardShareObject();
            Title = GisEditor.LanguageManager.GetStringResource("DissolveWizardViewModelDissolveWizardTitle");
            HelpKey = "DissolveWizardHelp";
            TargetObject = sharedObject;

            Add(new DissolveWizardChooseLayerStep());
            Add(new DissolveWizardChooseColumnStep());
            Add(new ChooseDataStep());
            Add(new DissolveWizardSaveResultStep());
        }

        #region for unit test

        // Tuple
        // item1: columnName,
        // item2: columnType,
        // item3: OperatorMode.
        public static Collection<Feature> Dissolve(IEnumerable<string> dissolveColumnNames
            , IEnumerable<Tuple<string, string, DissolveOperatorMode>> pairedColumnStrategies
            , IEnumerable<Feature> featuresToDissovle)
        {
            var featureGroupByShapeType = featuresToDissovle.GroupBy(f =>
            {
                return f.GetShape().GetType();
            });

            Collection<Feature> dissolveFeatures = new Collection<Feature>();
            Action<IGrouping<Type, Feature>, Action<IEnumerable<Feature>, Dictionary<string, string>>> dissolveAction
                = new Action<IGrouping<Type, Feature>, Action<IEnumerable<Feature>, Dictionary<string, string>>>
                    ((groupByType, finalProcessAction) =>
                    {
                        var groupByColumns = GroupByColumns(groupByType, dissolveColumnNames);
                        groupByColumns.ForEach(groupFeature =>
                        {
                            Dictionary<string, string> newColumnValues = new Dictionary<string, string>();
                            foreach (var tmpMatchColumn in dissolveColumnNames)
                            {
                                newColumnValues.Add(tmpMatchColumn, groupFeature.First().ColumnValues[tmpMatchColumn]);
                            }

                            foreach (var operatorPair in pairedColumnStrategies)
                            {
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
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            }
                        });
                    });

            featureGroupByShapeType.ForEach(groupByType =>
            {
                if (groupByType.Key.IsSubclassOf(typeof(AreaBaseShape)))
                {
                    dissolveAction(groupByType, (tmpFeatures, tmpColumnValues) =>
                    {
                        MultipolygonShape dissolveShape = AreaBaseShape.Union(GetValidFeatures(tmpFeatures));
                        if (dissolveShape != null)
                        {
                            Feature dissolveFeature = new Feature(dissolveShape, tmpColumnValues);
                            dissolveFeatures.Add(dissolveFeature);
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
                            dissolveFeatures.Add(new Feature(dissolveShape, tmpColumnValues));
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
                        dissolveFeatures.Add(new Feature(multipointShape, tmpColumnValues));
                    });
                }
            });

            return dissolveFeatures;
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

        // Tuple
        // item1: columnName,
        // item2: columnType,
        // item3: OperatorMode.
        private static Feature CollectNewColumnValues(IGrouping<string, Feature> groupFeature, Dictionary<string, string> newColumnValues, Tuple<string, string, DissolveOperatorMode> operatorPair)
        {
            PairStrategy pairStrategy = new PairStrategy
            {
                ColumnName = operatorPair.Item1,
                ColumnType = operatorPair.Item2,
                Operator = operatorPair.Item3
            };

            var getIntColumnValueFunc = new Func<Feature, int>(f =>
            {
                int currentValue = 0;
                if (f.ColumnValues.ContainsKey(pairStrategy.ColumnName))
                {
                    Int32.TryParse(f.ColumnValues[pairStrategy.ColumnName], out currentValue);
                }
                return currentValue;
            });

            var getDoubleColumnValueFunc = new Func<Feature, double>(f =>
            {
                double currentValue = 0;
                if (f.ColumnValues.ContainsKey(pairStrategy.ColumnName))
                {
                    Double.TryParse(f.ColumnValues[pairStrategy.ColumnName], out currentValue);
                }
                return currentValue;
            });

            Feature feature = new Feature();
            switch (pairStrategy.Operator)
            {
                case DissolveOperatorMode.First:
                    feature = groupFeature.FirstOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(pairStrategy.ColumnName, feature.ColumnValues[pairStrategy.ColumnName]);
                    }
                    break;
                case DissolveOperatorMode.Last:
                    feature = groupFeature.LastOrDefault();
                    if (feature.GetShape() != null)
                    {
                        newColumnValues.Add(pairStrategy.ColumnName, feature.ColumnValues[pairStrategy.ColumnName]);
                    }
                    break;
                case DissolveOperatorMode.Count:
                    newColumnValues.Add(pairStrategy.ColumnName, groupFeature.Count().ToString());
                    break;
                case DissolveOperatorMode.Sum:
                    if (pairStrategy.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        int intSum = groupFeature.Sum(getIntColumnValueFunc);
                        newColumnValues.Add(pairStrategy.ColumnName, intSum.ToString());
                    }
                    else if (pairStrategy.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double doubleSum = groupFeature.Sum(getDoubleColumnValueFunc);
                        newColumnValues.Add(pairStrategy.ColumnName, doubleSum.ToString());
                    }
                    break;
                case DissolveOperatorMode.Average:
                    if (pairStrategy.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase) || pairStrategy.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double averageValue = groupFeature.Average(getDoubleColumnValueFunc);
                        newColumnValues.Add(pairStrategy.ColumnName, averageValue.ToString());
                    }
                    break;
                case DissolveOperatorMode.Min:
                    if (pairStrategy.ColumnType.Equals("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        int intMin = groupFeature.Min(getIntColumnValueFunc);
                        newColumnValues.Add(pairStrategy.ColumnName, intMin.ToString());
                    }
                    else if (pairStrategy.ColumnType.Equals("Double", StringComparison.OrdinalIgnoreCase))
                    {
                        double doubleMin = groupFeature.Sum(getDoubleColumnValueFunc);
                        newColumnValues.Add(pairStrategy.ColumnName, doubleMin.ToString());
                    }
                    break;
            }
            return feature;
        }

        private static Collection<Feature> GetValidFeatures(IEnumerable<Feature> features)
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

        private static void HandleExceptionFromInvalidFeature(string featureId, string errorMessage, Dictionary<string, ErrorInfo> errorRecord = null)
        {
            if (errorRecord != null && !errorRecord.ContainsKey(featureId + errorMessage))
                errorRecord.Add(featureId + errorMessage, new ErrorInfo { ID = featureId, ErrorMessage = errorMessage });
        }

        private struct PairStrategy
        {
            public string ColumnName { get; set; }

            public string ColumnType { get; set; }

            public DissolveOperatorMode Operator { get; set; }
        }

        #endregion for unit test
    }
}