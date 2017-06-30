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
    public class SimplifyTaskPlugin : GeoTaskPlugin
    {
        private DistanceUnitToStringConverter converter = new DistanceUnitToStringConverter();
        private FeatureSource featureSource;
        private bool preserveTopology;
        private string selectedDistanceUnit;
        private double simplificationTolerance;
        private string outputPathFileName;
        private GeographyUnit mapUnit;
        private string displayProjectionParameters;
        private bool isCanceled;

        public SimplifyTaskPlugin()
        { }

        public FeatureSource FeatureSource
        {
            get { return featureSource; }
            set { featureSource = value; }
        }

        public bool PreserveTopology
        {
            get { return preserveTopology; }
            set { preserveTopology = value; }
        }

        public string SelectedDistanceUnit
        {
            get { return selectedDistanceUnit; }
            set { selectedDistanceUnit = value; }
        }

        public double SimplificationTolerance
        {
            get { return simplificationTolerance; }
            set { simplificationTolerance = value; }
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public GeographyUnit MapUnit
        {
            get { return mapUnit; }
            set { mapUnit = value; }
        }

        public string DisplayProjectionParameters
        {
            get { return displayProjectionParameters; }
            set { displayProjectionParameters = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("SimplifyTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            if (FeatureSource is ShapeFileFeatureSource)
            {
                SimplifyShapeFile();
            }
            else
            {
                SimplifyAllFeatures();
            }
        }

        private void SimplifyShapeFile()
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            var currentSource = (ShapeFileFeatureSource)FeatureSource;
            if (!currentSource.IsOpen) currentSource.Open();

            var canceled = false;
            var shapeFileType = currentSource.GetShapeFileType();
            var projectionWkt = Proj4Projection.ConvertProj4ToPrj(DisplayProjectionParameters);
            var helper = new ShapeFileHelper(shapeFileType, outputPathFileName, currentSource.GetColumns(), projectionWkt);
            try
            {
                helper.ForEachFeatures(currentSource, (f, currentProgress, upperBound, percentage) =>
                {
                    try
                    {
                        if (f.GetWellKnownBinary() != null)
                        {

                            SimplificationType simType = SimplificationType.DouglasPeucker;
                            if (preserveTopology)
                            {
                                simType = SimplificationType.TopologyPreserving;
                            }

                            var shape = f.GetShape();
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
                                Feature feature = new Feature(simplifiedShape);
                                foreach (var item in f.ColumnValues)
                                {
                                    feature.ColumnValues[item.Key] = item.Value;
                                }
                                helper.Add(feature);
                            }
                        }

                        args = new UpdatingTaskProgressEventArgs(TaskState.Updating, percentage);
                        args.Current = currentProgress;
                        args.UpperBound = upperBound;
                        OnUpdatingProgress(args);

                        canceled = args.TaskState == TaskState.Canceled;
                        return canceled;
                    }
                    catch(Exception e)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                        return false;
                    }
                });
            }
            finally
            {
                helper.Commit();
            }
        }

        private void SimplifyAllFeatures()
        {
            UpdatingTaskProgressEventArgs args = null;

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
                    args = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    args.Message = feature.Id;
                    args.Error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    OnUpdatingProgress(args);
                    continue;
                }

                var progressPercentage = index * 100 / count;
                args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
                args.Current = index;
                args.UpperBound = count;
                OnUpdatingProgress(args);
                isCanceled = args.TaskState == TaskState.Canceled;
                if (isCanceled) break;
                index++;
            }
            if (!isCanceled)
            {
                args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
                args.Message = "Creating File";
                OnUpdatingProgress(args);

                FileExportInfo info = new FileExportInfo(simplifiedFeatures, GetColumns(), outputPathFileName, displayProjectionParameters);
                Export(info);
            }
            //args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Completed);
            //args.Message = "Finished";
            //OnUpdatingProgress(args);
        }

        private IEnumerable<Feature> GetFeaturesFromTempFile()
        {
            featureSource.Open();
            var allFeatures = featureSource.GetAllFeatures(featureSource.GetDistinctColumnNames());
            featureSource.Close();

            return allFeatures.Where(f =>
            {
                var shape = f.GetShape();
                return shape is AreaBaseShape || shape is LineBaseShape;
            });
        }

        //private void Output(IEnumerable<Feature> bufferedFeatures)
        //{
        //    try
        //    {
        //        FileExportInfo info = new FileExportInfo(bufferedFeatures, GetColumns(), outputPathFileName, displayProjectionParameters);
        //        ShpFileExporter exporter = new ShpFileExporter();
        //        exporter.ExportToFile(info);
        //    }
        //    catch (Exception ex)
        //    {
        //        UpdatingTaskProgressEventArgs e = new UpdatingTaskProgressEventArgs(TaskState.Canceled);
        //        e.Error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
        //        OnUpdatingProgress(e);
        //    }
        //}

        private IEnumerable<FeatureSourceColumn> GetColumns()
        {
            featureSource.Open();
            var columns = featureSource.GetColumns();
            featureSource.Close();

            return columns;
        }
    }
}