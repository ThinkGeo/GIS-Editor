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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MergeTaskPlugin : GeoTaskPlugin
    {
        private string outputPathFileName;
        private string wkt;
        private List<FeatureSource> featureSources;
        private FeatureSourceColumn[] columns;
        private bool isCanceled;

        public MergeTaskPlugin()
        { }

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

        public List<FeatureSource> FeatureSources
        {
            get { return featureSources; }
            set { featureSources = value; }
        }

        public FeatureSourceColumn[] Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("MergeTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            if (featureSources.All(f => f is ShapeFileFeatureSource))
            {
                ProcessWithShapeFilesOnly();
            }
            else
            {
                var sourceFeatures = GetSourceFeatures();
                var resultFeatures = MergeFeatures(sourceFeatures, columns);
                if (!isCanceled)
                {
                    var info = new FileExportInfo(resultFeatures, columns, outputPathFileName, wkt);
                    Export(info);
                }
            }
        }

        private void ProcessWithShapeFilesOnly()
        {
            int count = featureSources.Sum(f =>
            {
                if (!f.IsOpen) f.Open();
                return f.GetCount();
            });

            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            var shapeFileFeatureSources = featureSources.OfType<ShapeFileFeatureSource>().ToList();
            var shapeFileType = shapeFileFeatureSources.First().GetShapeFileType();

            var currentProgress = 0;
            var shapeFileHelper = new ShapeFileHelper(shapeFileType, OutputPathFileName, columns, wkt);
            foreach (var featureSource in shapeFileFeatureSources)
            {
                shapeFileHelper.ForEachFeatures(featureSource, f =>
                {
                    if (f.GetWellKnownBinary() != null)
                    {
                        foreach (var featureColumn in columns)
                        {
                            if (!f.ColumnValues.Keys.Contains(featureColumn.ColumnName))
                            {
                                f.ColumnValues.Add(featureColumn.ColumnName, "0");
                            }
                        }

                        shapeFileHelper.Add(new Feature(f.GetWellKnownBinary(), Guid.NewGuid().ToString(), f.ColumnValues));
                    }

                    currentProgress++;
                    var progressPercentage = currentProgress * 100 / count;
                    args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
                    args.Current = currentProgress;
                    args.UpperBound = count;
                    OnUpdatingProgress(args);
                    isCanceled = args.TaskState == TaskState.Canceled;
                    return isCanceled;
                });
            }

            shapeFileHelper.Commit();
        }

        private int GetDecimalLength(DbfColumnType dbfColumnType, int maxLength)
        {
            if (dbfColumnType == DbfColumnType.Float)
            {
                return maxLength < 4 ? maxLength : 4;
            }
            else
            {
                return 0;
            }
        }

        private Collection<Feature> GetSourceFeatures()
        {
            var sourceFeatures = featureSources.SelectMany(featureSource =>
            {
                featureSource.Open();
                var allFeatures = featureSource.GetAllFeatures(featureSource.GetDistinctColumnNames());
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
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);

            foreach (Feature feature in sourceFeatures)
            {
                var current = id + 1;
                var progressPercentage = current * 100 / count;
                args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
                args.Current = current;
                args.UpperBound = count;
                OnUpdatingProgress(args);
                isCanceled = args.TaskState == TaskState.Canceled;
                if (isCanceled) break;

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
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                }
                id++;
            }
            return features;
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