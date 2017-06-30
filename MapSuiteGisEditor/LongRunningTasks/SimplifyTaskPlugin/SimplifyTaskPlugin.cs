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
using GdalWrapper;
using GISEditorConverters;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;

namespace LongRunningTaskPlugins
{
    public class SimplifyTaskPlugin : LongRunningTaskPlugin
    {
        private DistanceUnitToStringConverter converter = new DistanceUnitToStringConverter();

        //private string tempShapeFilePath;
        private bool preserveTopology;
        private string selectedDistanceUnit;
        private double simplificationTolerance;
        private string outputPath;
        private GeographyUnit mapUnit;
        private string displayProjectionParameters;
        private FeatureSource featureSource;

        public SimplifyTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool allParametersValid = ParseParameters(parameters);

            if (allParametersValid)
            {
                UpdatingProgressLongRunningTaskPluginEventArgs args = null;

                var features = GetFeaturesFromTempFile().ToArray();
                int index = 1;
                int count = features.Count();

                SimplificationType simType = SimplificationType.DouglasPeucker;
                if (preserveTopology)
                {
                    simType = SimplificationType.TopologyPreserving;
                }

                Collection<Feature> simplifiedFeatures = new Collection<Feature>();
                foreach (Feature feature in features)
                {
                    try
                    {
                        var shape = feature.GetShape();
                        var areaShape = shape as AreaBaseShape;
                        var lineShape = shape as LineBaseShape;
                        BaseShape simplifiedShape = null;

                        if (areaShape != null)
                        {
                            if (selectedDistanceUnit == "Decimal Degrees")
                            {
                                simplifiedShape = areaShape.Simplify(simplificationTolerance, simType);
                            }
                            else
                            {
                                simplifiedShape = areaShape.Simplify(mapUnit, simplificationTolerance, (DistanceUnit)converter.ConvertBack(selectedDistanceUnit, null, null, null), simType);
                            }
                        }
                        else if (lineShape != null)
                        {
                            if (selectedDistanceUnit == "Decimal Degrees")
                            {
                                simplifiedShape = lineShape.Simplify(simplificationTolerance, simType);
                            }
                            else
                            {
                                simplifiedShape = lineShape.Simplify(mapUnit, simplificationTolerance, (DistanceUnit)converter.ConvertBack(selectedDistanceUnit, null, null, null), simType);
                            }
                        }

                        if (simplifiedShape != null)
                        {
                            Feature newFeature = new Feature(simplifiedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
                            newFeature.Tag = feature.Tag;
                            simplifiedFeatures.Add(newFeature);
                        }
                    }
                    catch (Exception ex)
                    {
                        args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
                        args.Message = feature.Id;
                        args.ExceptionInfo = new LongRunningTaskExceptionInfo(ex.Message, ex.StackTrace);

                        OnUpdatingProgress(args);
                    }

                    args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                    args.Current = index * 100 / count;

                    OnUpdatingProgress(args);

                    index++;
                }

                args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                args.Message = "Creating File";
                OnUpdatingProgress(args);

                Output(simplifiedFeatures);

                args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                args.Message = "Finished";
                OnUpdatingProgress(args);
            }
            else
            {
                //report error and return.
            }
        }

        private bool ParseParameters(Dictionary<string, string> parameters)
        {
            bool areAllParametersValid = false;

            string[] parameterNames = { "FeatureSourceInString", "PreserveTopology", "SelectedDistanceUnit", "SimplificationTolerance", "OutputPath", "MapUnit", "DisplayProjectionParameters" };
            bool allParametersExist = parameterNames.All(parameterName => parameters.ContainsKey(parameterName));

            if (allParametersExist)
            {
                featureSource = (FeatureSource)GetObjectFromString(parameters[parameterNames[0]]);
                preserveTopology = parameters[parameterNames[1]] == "true" ? true : false;
                selectedDistanceUnit = parameters[parameterNames[2]];
                bool isToleranceValid = double.TryParse(parameters[parameterNames[3]], out simplificationTolerance);
                outputPath = parameters[parameterNames[4]];
                bool isUnitValid = Enum.TryParse<GeographyUnit>(parameters[parameterNames[5]], out mapUnit);
                displayProjectionParameters = parameters[parameterNames[6]];

                areAllParametersValid = allParametersExist && isToleranceValid && isUnitValid;
            }

            return areAllParametersValid;
        }

        private object GetObjectFromString(string objectInString)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(objectInString)))
            {
                return formatter.Deserialize(stream);
            }
        }

        private IEnumerable<Feature> GetFeaturesFromTempFile()
        {
            featureSource.Open();
            var allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
            featureSource.Close();

            return allFeatures.Where(f =>
            {
                var shape = f.GetShape();
                return shape is AreaBaseShape || shape is LineBaseShape;
            });
        }

        private void Output(IEnumerable<Feature> bufferedFeatures)
        {
            string projectionInWKT = GdalHelper.Proj4ToWkt(displayProjectionParameters);
            FileExportInfo info = new FileExportInfo(bufferedFeatures, GetColumns(), outputPath, projectionInWKT);

            ShpFileExporter exporter = new ShpFileExporter();
            exporter.ExportToFile(info);
        }

        private IEnumerable<FeatureSourceColumn> GetColumns()
        {
            featureSource.Open();
            var columns = featureSource.GetColumns();
            featureSource.Close();

            return columns;
        }
    }
}
