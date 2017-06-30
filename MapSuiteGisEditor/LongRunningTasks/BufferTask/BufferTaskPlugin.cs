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
using System.Threading;
using GdalWrapper;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;

namespace LongRunningTasks
{
    public class BufferTaskPlugin : LongRunningTaskPlugin
    {
        private string[] parameterNames;
        private Collection<FeatureSourceColumn> columns;

        public BufferTaskPlugin()
        {
            parameterNames = new string[] 
            { 
                "FeatureSourceInString",//0
                "Distance",//1
                "Smoothness",//2
                "CapStyle",//3
                "MapUnit",//4
                "DistanceUnit",//5
                "OutPutPath",//6
                "DisplayProjectionParameters"//7
            };
        }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool parametersValid = parameterNames.All(name => parameters.ContainsKey(name));

            if (parametersValid)
            {
                double distance = 0;
                int smoothness = 0;
                BufferCapType capType = BufferCapType.Butt;
                GeographyUnit mapUnit = GeographyUnit.Unknown;
                DistanceUnit distanceUnit = DistanceUnit.Feet;

                var featuresToBuffer = GetFeaturesToBuffer(parameters[parameterNames[0]]);
                double.TryParse(parameters[parameterNames[1]], out distance);
                int.TryParse(parameters[parameterNames[2]], out smoothness);
                Enum.TryParse<BufferCapType>(parameters[parameterNames[3]], out capType);
                Enum.TryParse<GeographyUnit>(parameters[parameterNames[4]], out mapUnit);
                Enum.TryParse<DistanceUnit>(parameters[parameterNames[5]], out distanceUnit);

                int bufferdFeaturesCount = 0;
                Collection<Feature> bufferedFeatures = new Collection<Feature>();
                foreach (Feature feature in featuresToBuffer)
                {
                    try
                    {
                        BaseShape shape = feature.GetShape();
                        MultipolygonShape bufferedShape = shape.Buffer(distance, smoothness, capType, mapUnit, distanceUnit);
                        Feature bufferedFeature = new Feature(bufferedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
                        bufferedFeature.Tag = feature.Tag;
                        bufferedFeatures.Add(bufferedFeature);
                    }
                    catch (Exception ex)
                    {
                        UpdatingProgressLongRunningTaskPluginEventArgs errorEventArgs = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
                        errorEventArgs.ExceptionInfo = new LongRunningTaskExceptionInfo(ex.Message, ex.StackTrace);
                        errorEventArgs.Message = feature.Id;
                        OnUpdatingProgress(errorEventArgs);

                        continue;
                    }

                    Interlocked.Increment(ref bufferdFeaturesCount);
                    OnUpdatingProgress(new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating) { Current = bufferdFeaturesCount * 100 / featuresToBuffer.Count });
                }

                OnUpdatingProgress(new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating) { Message = "Creating File" });

                string outputPath = parameters[parameterNames[6]];
                string projection = parameters[parameterNames[7]];
                Output(bufferedFeatures, outputPath, projection);

                OnUpdatingProgress(new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating) { Message = "Finished" });
            }
        }

        private Collection<Feature> GetFeaturesToBuffer(string featureSourceInString)
        {
            FeatureSource featureSource = (FeatureSource)GetObjectFromString(featureSourceInString);

            featureSource.Open();
            var allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
            columns = featureSource.GetColumns();
            featureSource.Close();

            return allFeatures;
        }

        private object GetObjectFromString(string objectInString)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(objectInString)))
            {
                return formatter.Deserialize(stream);
            }
        }

        private void Output(IEnumerable<Feature> bufferedFeatures, string path, string projection)
        {
            ShpFileExporter exporter = new ShpFileExporter();
            string projectionInWKT = GdalHelper.Proj4ToWkt(projection);
            FileExportInfo info = new FileExportInfo(bufferedFeatures, columns, path, projectionInWKT);
            exporter.ExportToFile(info);
        }
    }
}
