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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SplitTaskPlugin : GeoTaskPlugin
    {
        private string outputPath;
        private string wkt;
        private FeatureSource featureSource;
        private string splitColumnName;
        private Dictionary<string, string> exportConfigs;
        private string layerName;
        private bool overwriteOutputFiles;
        private bool isCanceled;

        public SplitTaskPlugin()
        { }

        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        public string Wkt
        {
            get { return wkt; }
            set { wkt = value; }
        }

        public FeatureSource FeatureSource
        {
            get { return featureSource; }
            set { featureSource = value; }
        }

        public string SplitColumnName
        {
            get { return splitColumnName; }
            set { splitColumnName = value; }
        }

        public Dictionary<string, string> ExportConfigs
        {
            get { return exportConfigs; }
            set { exportConfigs = value; }
        }

        public string LayerName
        {
            get { return layerName; }
            set { layerName = value; }
        }

        public bool OverwriteOutputFiles
        {
            get { return overwriteOutputFiles; }
            set { overwriteOutputFiles = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("SplitTaksPlguinOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("SplitTaskPluginSplittingText") + " " + layerName;
        }

        protected override void RunCore()
        {
            Split();
        }

        private void Split()
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);

            featureSource.Open();
            Collection<Feature> allFeatures = featureSource.GetAllFeatures(featureSource.GetDistinctColumnNames());
            var columns = featureSource.GetColumns();
            featureSource.Close();

            var featuresGroups = allFeatures.GroupBy(tmpFeature
                => tmpFeature.ColumnValues[splitColumnName]).ToArray();

            int i = 0;
            int count = exportConfigs.Count;
            foreach (var config in exportConfigs)
            {
                try
                {
                    string folderName = outputPath;
                    string fileName = config.Value;
                    if (String.IsNullOrEmpty(fileName))
                    {
                        fileName = String.Format(CultureInfo.InvariantCulture, "{0}_{1}.shp", layerName, config.Key);
                    }

                    fileName = Path.ChangeExtension(fileName, ".shp");
                    string finalShapeFilePath = Path.Combine(folderName, fileName);
                    var featureGroup = featuresGroups.FirstOrDefault(group => group.Key == config.Key || (config.Key.Equals("<Blank>", StringComparison.Ordinal) && group.Key.Equals(string.Empty, StringComparison.Ordinal)));
                    if (featureGroup != null)
                    {
                        var info = new FileExportInfo(featureGroup, columns, finalShapeFilePath, wkt);
                        Export(info);
                    }

                    args.Current = ++i;
                    args.UpperBound = count;
                    args.Message = String.Format(CultureInfo.InvariantCulture, "Building... ({0}/{1})", args.Current, args.UpperBound);
                    OnUpdatingProgress(args);
                    isCanceled = args.TaskState == TaskState.Canceled;

                    if (isCanceled)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    var errorArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    errorArgs.Error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    OnUpdatingProgress(errorArgs);
                    continue;
                }
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
            var args = new UpdatingTaskProgressEventArgs(TaskState.Error);
            args.Error = new ExceptionInfo(message, string.Empty, string.Empty);
            args.Message = id;

            OnUpdatingProgress(args);
        }
    }
}