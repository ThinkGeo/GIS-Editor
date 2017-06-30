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
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;

namespace MergeTaskPlugin
{
    public class MergeTaskPlugin : LongRunningTaskPlugin
    {
        private string outputPath;
        private string Wkt;
        private FeatureSource[] featureSources;
        private FeatureSourceColumn[] columns;

        public MergeTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool parametersValid = TryParseParameters(parameters);

            try
            {
                if (parametersValid)
                {
                    var sourceFeatures = GetSourceFeatures();
                    var resultFeatures = MergeFeatures(sourceFeatures, columns);

                    ShpFileExporter shpFileExporter = new ShpFileExporter();
                    FileExportInfo fileInfor = new FileExportInfo(resultFeatures, columns, outputPath, Wkt);
                    shpFileExporter.ExportToFile(fileInfor);
                }
            }
            catch (Exception ex)
            {
                var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
                args.ExceptionInfo = new LongRunningTaskExceptionInfo(ex.Source, ex.StackTrace);
                OnUpdatingProgress(args);
            }
            finally
            {
                var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                args.Message = "Finished";
                OnUpdatingProgress(args);
            }
        }

        private bool TryParseParameters(Dictionary<string, string> parameters)
        {
            string[] parameterNames = new string[] 
            {
                "OutputPath",
                "Wkt",
                "FeatureSourcesString",
                "ColumnsString"
            };

            bool allExist = parameterNames.All(name => parameters.ContainsKey(name));

            if (allExist)
            {
                outputPath = parameters[parameterNames[0]];
                Wkt = parameters[parameterNames[1]];
                featureSources = (FeatureSource[])GetObjectFromString(parameters[parameterNames[2]]);
                columns = (FeatureSourceColumn[])GetObjectFromString(parameters[parameterNames[3]]);
            }

            return allExist;
        }

        private object GetObjectFromString(string objectInString)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(objectInString)))
            {
                return formatter.Deserialize(stream);
            }
        }

        private Collection<Feature> GetSourceFeatures()
        {
            var sourceFeatures = featureSources.SelectMany(featureSource =>
            {
                featureSource.Open();
                var allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
                featureSource.Close();

                return allFeatures;
            });

            return new Collection<Feature>(sourceFeatures.ToList());
        }

        private Collection<Feature> MergeFeatures(Collection<Feature> sourceFeatures, IEnumerable<FeatureSourceColumn> featureColumns)
        {
            Collection<Feature> features = new Collection<Feature>();
            int id = 0;
            int count = sourceFeatures.Count;
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);

            foreach (Feature feature in sourceFeatures)
            {
                args.Current = (id + 1) * 100 / count;
                OnUpdatingProgress(args);

                foreach (FeatureSourceColumn featureColumn in featureColumns)
                {
                    if (!feature.ColumnValues.Keys.Contains(featureColumn.ColumnName))
                        feature.ColumnValues.Add(featureColumn.ColumnName, "0");
                }
                try
                {
                    if (!feature.GetShape().Validate(ShapeValidationMode.Simple).IsValid)
                    {
                        throw new Exception("This feature is invalid.");
                    }
                    features.Add(new Feature(feature.GetWellKnownBinary(), id.ToString(), feature.ColumnValues));
                }
                catch (Exception ex)
                {
                    HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                }
                id++;
            }
            return features;
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
