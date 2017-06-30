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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;
using ThinkGeo.MapSuite.WpfDesktopEdition.Extension;

namespace SplitTaskPlugin
{
    public class SplitTaskPlugin : LongRunningTaskPlugin
    {
        private string outputPath;
        private string wkt;
        private string splitColumnName;
        private string layerName;
        private FeatureSource featureSource;
        private Dictionary<string, string> exportConfigs;
        private bool overwriteOutputFiles;

        public SplitTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool parametersValid = TryParseParameters(parameters);

            try
            {
                if (parametersValid)
                {
                    Split();
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
                "FeatureSourceInString",
                "SplitColumnName",
                "ExportConfigs",
                "LayerName",
                "OverwriteOutputFiles",
            };

            bool allExist = parameterNames.All(name => parameters.ContainsKey(name));

            if (allExist)
            {
                outputPath = parameters[parameterNames[0]];
                wkt = parameters[parameterNames[1]];
                featureSource = (FeatureSource)GetObjectFromString(parameters[parameterNames[2]]);
                splitColumnName = parameters[parameterNames[3]];
                exportConfigs = (Dictionary<string, string>)GetObjectFromString(parameters[parameterNames[4]]);
                layerName = parameters[parameterNames[5]];
                overwriteOutputFiles = parameters[parameterNames[6]] == bool.TrueString ? true : false;
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

        private void Split()
        {
            try
            {
                var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);

                featureSource.Open();
                Collection<Feature> allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
                var columns = featureSource.GetColumns();
                featureSource.Close();

                var featuresGroups = allFeatures.GroupBy(tmpFeature
                    => tmpFeature.ColumnValues[splitColumnName]);

                int i = 0;
                int count = exportConfigs.Count;
                exportConfigs.ForEach(config =>
                {
                    string folderName = outputPath;
                    string fileName = config.Value;
                    if (String.IsNullOrEmpty(fileName))
                    {
                        fileName = String.Format(CultureInfo.InvariantCulture, "{0}_{1}.shp", layerName, config.Key);
                    }

                    if (!fileName.EndsWith(".shp", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".shp";
                    }

                    string finalShapeFilePath = Path.Combine(folderName, fileName);

                    if (File.Exists(finalShapeFilePath) && overwriteOutputFiles)
                    {
                        //CloseExistingLayersAndCollectOverlaysToRefresh(this, finalShapeFilePath);
                        RemoveShapeFile(finalShapeFilePath);
                    }

                    //this.OutputShapeFileNames.Add(finalShapeFilePath);
                    var featureGroup = featuresGroups.FirstOrDefault(group => group.Key == config.Key);
                    if (featureGroup != null)
                    {
                        ShpFileExporter exporter = new ShpFileExporter();
                        exporter.ExportToFile(new FileExportInfo(featureGroup, columns, finalShapeFilePath, wkt));
                    }

                    args.Message = String.Format(CultureInfo.InvariantCulture, "Building... ({0}/{1})", ++i, count);
                    OnUpdatingProgress(args);
                });
            }
            catch (Exception ex)
            {
                //HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
            }
            finally
            {
                //this.IsBusy = false;
                //this.BusyContent = String.Empty;
                //this.CurrentThread = null;
            }
        }

        private void RemoveShapeFile(string shpPath)
        {
            if (!string.IsNullOrEmpty(shpPath))
            {
                string dir = Path.GetDirectoryName(shpPath);
                string[] files = Directory.GetFiles(dir, Path.GetFileNameWithoutExtension(shpPath) + ".*");
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
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
