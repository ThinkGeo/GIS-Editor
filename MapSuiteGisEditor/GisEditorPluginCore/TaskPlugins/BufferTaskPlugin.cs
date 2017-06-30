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
using System.Runtime.Serialization;
using System.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [DataContract]
    public class BufferTaskPlugin : GeoTaskPlugin
    {
        [NonSerialized]
        private Collection<FeatureSourceColumn> columns;

        private Collection<Feature> featuresToBuffer;
        private double distance;
        private int smoothness;
        private BufferCapType capstyle;
        private GeographyUnit mapUnit;
        private DistanceUnit distanceUnit;
        private bool dissolve;
        private string outputPathFileName;
        private string displayProjectionParameters;
        private FeatureSource featureSource;

        public BufferTaskPlugin()
        {
            columns = new Collection<FeatureSourceColumn>();
            featuresToBuffer = new Collection<Feature>();
        }

        public Collection<Feature> FeaturesToBuffer
        {
            get { return featuresToBuffer; }
            set { featuresToBuffer = value; }
        }

        public double Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public int Smoothness
        {
            get { return smoothness; }
            set { smoothness = value; }
        }

        public BufferCapType Capstyle
        {
            get { return capstyle; }
            set { capstyle = value; }
        }

        public GeographyUnit MapUnit
        {
            get { return mapUnit; }
            set { mapUnit = value; }
        }

        public DistanceUnit DistanceUnit
        {
            get { return distanceUnit; }
            set { distanceUnit = value; }
        }

        public bool Dissolve
        {
            get { return dissolve; }
            set { dissolve = value; }
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public string DisplayProjectionParameters
        {
            get { return displayProjectionParameters; }
            set { displayProjectionParameters = value; }
        }

        public FeatureSource FeatureSource
        {
            get { return featureSource; }
            set { featureSource = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("BufferTaskPluginOpreationName");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            if (FeatureSource is ShapeFileFeatureSource && !Dissolve)
            {
                BufferShapeFile();
            }
            else
            {
                BufferAllFeatures();
            }
        }

        private void BufferShapeFile()
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            var currentSource = (ShapeFileFeatureSource)FeatureSource;
            if (!currentSource.IsOpen) currentSource.Open();

            var canceled = false;
            var projectionWkt = Proj4Projection.ConvertProj4ToPrj(DisplayProjectionParameters);
            var helper = new ShapeFileHelper(ShapeFileType.Polygon, outputPathFileName, currentSource.GetColumns(), projectionWkt);
            helper.CapabilityToFlush = 1000;
            helper.ForEachFeatures(currentSource, (f, currentProgress, upperBound, percentage) =>
            {
                try
                {
                    if (f.GetWellKnownBinary() != null)
                    {
                        //Feature bufferedFeature = f.Buffer(Distance, Smoothness, Capstyle, MapUnit, DistanceUnit);
                        //Feature bufferedFeature = SqlTypesGeometryHelper.Buffer(f, Distance, Smoothness, Capstyle, MapUnit, DistanceUnit);
                        Feature bufferedFeature = BufferFeature(f);
                        helper.Add(bufferedFeature);
                    }

                    args = new UpdatingTaskProgressEventArgs(TaskState.Updating, percentage);
                    args.Current = currentProgress;
                    args.UpperBound = upperBound;
                    OnUpdatingProgress(args);

                    canceled = args.TaskState == TaskState.Canceled;
                    return canceled;
                }
                catch(Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    return false;
                }
            });

            helper.Commit();
        }

        private void BufferAllFeatures()
        {
            FeaturesToBuffer = GetFeaturesToBuffer(FeatureSource);

            int bufferdFeaturesCount = 0;
            Collection<Feature> bufferedFeatures = new Collection<Feature>();
            MultipolygonShape dissolvedShape = null;

            bool isCanceled = false;
            foreach (Feature feature in featuresToBuffer)
            {
                try
                {
                    BaseShape shape = feature.GetShape();
                    MultipolygonShape bufferedShape = BufferShape(shape);
                    Feature bufferedFeature = new Feature(bufferedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues);

                    bufferedFeature.Tag = feature.Tag;
                    bufferedFeatures.Add(bufferedFeature);

                    if (dissolve)
                    {
                        if (dissolvedShape == null)
                        {
                            dissolvedShape = bufferedShape;
                            dissolvedShape.Tag = feature.Tag;
                        }
                        else
                        {
                            //dissolvedShape = dissolvedShape.Union(bufferedShape);
                            dissolvedShape = (MultipolygonShape)SqlTypesGeometryHelper.Union(dissolvedShape, bufferedShape);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorEventArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    errorEventArgs.Error = new ExceptionInfo(string.Format(CultureInfo.InvariantCulture, "Feature id: {0}, {1}"
                        , feature.Id, ex.Message)
                        , ex.StackTrace
                        , ex.Source);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    errorEventArgs.Message = feature.Id;
                    OnUpdatingProgress(errorEventArgs);
                    continue;
                }

                Interlocked.Increment(ref bufferdFeaturesCount);
                int progressPercentage = bufferdFeaturesCount * 100 / featuresToBuffer.Count;
                var updatingArgs = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
                updatingArgs.Current = bufferdFeaturesCount;
                updatingArgs.UpperBound = featuresToBuffer.Count;
                OnUpdatingProgress(updatingArgs);

                if (updatingArgs.TaskState == TaskState.Canceled)
                {
                    isCanceled = true;
                    break;
                }
            }

            if (!isCanceled)
            {
                if (dissolve && dissolvedShape != null)
                {
                    bufferedFeatures.Clear();
                    bufferedFeatures.Add(new Feature(dissolvedShape.GetWellKnownBinary(), "1", new Dictionary<string, string> { { "OBJECTID", "1" }, { "SHAPE", "MULTIPOLYGON" } }) { Tag = dissolvedShape.Tag });
                }
                OnUpdatingProgress(new UpdatingTaskProgressEventArgs(TaskState.Updating) { Message = "Creating File" });
                Output(bufferedFeatures, OutputPathFileName, DisplayProjectionParameters, Dissolve);
            }
        }

        private MultipolygonShape BufferShape(BaseShape shape)
        {
            MultipolygonShape bufferedShape = null;
            if (capstyle == BufferCapType.Round)
            {
                bufferedShape = SqlTypesGeometryHelper.Buffer(shape, Distance, Smoothness, Capstyle, MapUnit, DistanceUnit);
            }
            else
            {
                bufferedShape = shape.Buffer(distance, smoothness, capstyle, mapUnit, distanceUnit);
            }
            return bufferedShape;
        }

        private Feature BufferFeature(Feature feature)
        {
            MultipolygonShape bufferedShape = BufferShape(feature.GetShape());
            Feature bufferedFeature = new Feature(bufferedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
            return bufferedFeature;
        }

        private Collection<Feature> GetFeaturesToBuffer(FeatureSource featureSource)
        {
            featureSource.Open();
            var allFeatures = featureSource.GetAllFeatures(featureSource.GetDistinctColumnNames());
            columns = featureSource.GetColumns();
            featureSource.Close();

            return allFeatures;
        }

        private void Output(IEnumerable<Feature> bufferedFeatures, string path, string projection, bool dissolve = false)
        {
            string projectionInWKT = Proj4Projection.ConvertProj4ToPrj(projection);

            if (dissolve)
            {
                columns = new Collection<FeatureSourceColumn>();
                columns.Add(new FeatureSourceColumn("OBJECTID", DbfColumnType.Character.ToString(), 1));
                columns.Add(new FeatureSourceColumn("SHAPE", DbfColumnType.Character.ToString(), 12));
            }

            FileExportInfo info = new FileExportInfo(bufferedFeatures, columns, path, projectionInWKT);
            Export(info);
        }
    }
}