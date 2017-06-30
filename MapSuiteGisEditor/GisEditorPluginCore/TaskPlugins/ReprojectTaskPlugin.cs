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
using System.Globalization;
using System.IO;
using System.Linq;
using GeoAPI.Geometries;
using GeoAPI.IO;
using NetTopologySuite.IO;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ReprojectTaskPlugin : TaskPlugin
    {
        private Dictionary<string, string> shapePathFileNames;
        private string targetProjectionParameter;
        private string outputPathFileName;

        public ReprojectTaskPlugin()
        {
            shapePathFileNames = new Dictionary<string, string>();
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public string TargetProjectionParameter
        {
            get { return targetProjectionParameter; }
            set { targetProjectionParameter = value; }
        }

        public Dictionary<string, string> ShapePathFileNames
        {
            get { return shapePathFileNames; }
        }

        protected override void RunCore()
        {
            var existingShapePathFileNames = ShapePathFileNames.Where(s => CheckShapeFileExists(s.Key));
            var upperBounds = GetUpperBounds(existingShapePathFileNames);

            CreateOutputPath(OutputPathFileName);
            var currentIndex = 0;
            foreach (var currentShapePathFileName in existingShapePathFileNames)
            {
                try
                {
                    string currentShapeFileName = Path.GetFileName(currentShapePathFileName.Key);
                    string outputShapePathFileName = Path.Combine(OutputPathFileName, currentShapeFileName);
                    string outputTempIdxPathFileName = Path.Combine(OutputPathFileName, "TMP" + currentShapeFileName);
                    outputTempIdxPathFileName = Path.ChangeExtension(outputTempIdxPathFileName, ".idx");

                    DeleteRelatedFiles(outputShapePathFileName);

                    Proj4Projection projection = new Proj4Projection();
                    projection.InternalProjectionParametersString = currentShapePathFileName.Value;
                    projection.ExternalProjectionParametersString = TargetProjectionParameter;
                    projection.Open();

                    //CreateShapeFileWithIndex(currentShapePathFileName.Key, outputShapePathFileName, outputTempIdxPathFileName, projection, upperBounds, ref current);

                    var currentFeatureSource = new ShapeFileFeatureSource(currentShapePathFileName.Key);
                    currentFeatureSource.Open();
                    string projectionWkt = Proj4Projection.ConvertProj4ToPrj(TargetProjectionParameter);
                    ShapeFileHelper helper = new ShapeFileHelper(currentFeatureSource.GetShapeFileType(), outputShapePathFileName, currentFeatureSource.GetColumns(), projectionWkt);

                    helper.ForEachFeatures(currentFeatureSource, f =>
                    {
                        if (f.GetWellKnownBinary() != null)
                        {
                            var newFeature = projection.ConvertToExternalProjection(f);
                            if (newFeature.CanMakeValid)
                            {
                                newFeature = newFeature.MakeValid();
                            }

                            if (newFeature.GetWellKnownType() != WellKnownType.GeometryCollection)
                            {
                                helper.Add(newFeature);
                            }
                        }

                        currentIndex++;
                        UpdatingTaskProgressEventArgs args = new UpdatingTaskProgressEventArgs(TaskState.Updating, currentIndex * 100 / upperBounds);
                        args.Current = currentIndex;
                        args.UpperBound = upperBounds;
                        OnUpdatingProgress(args);
                        return args.TaskState == TaskState.Canceled;
                    });

                    helper.Commit();
                    CreateDbfFile(currentShapePathFileName.Key, outputShapePathFileName);
                    //CreatePrjFile(outputShapePathFileName, TargetProjectionParameter);
                }
                catch (Exception ex)
                {
                    UpdatingTaskProgressEventArgs errorArgs = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    errorArgs.Error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    OnUpdatingProgress(errorArgs);
                    continue;
                }
            }
        }

        /// <summary>
        /// This method raises when load this plugin.
        /// </summary>
        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("ReprojectionTaskPlguinOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginProcessingText") + " ";
        }

        private bool CheckShapeFileExists(string pathFileName)
        {
            bool result = true;
            foreach (var ext in GetShapeFileExtensions())
            {
                string tmpPathFileName = Path.ChangeExtension(pathFileName, ext);
                if (!File.Exists(tmpPathFileName))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private static IEnumerable<string> GetShapeFileExtensions(ShapeFileExtensionSelectionMode extensionSelectionMode = ShapeFileExtensionSelectionMode.Basic)
        {
            yield return ".shp";
            yield return ".dbf";
            yield return ".shx";

            if (extensionSelectionMode == ShapeFileExtensionSelectionMode.All)
            {
                yield return ".idx";
                yield return ".ids";
                yield return ".prj";
            }
        }

        private static void DeleteRelatedFiles(string shapePathFileName)
        {
            foreach (var ext in GetShapeFileExtensions(ShapeFileExtensionSelectionMode.All))
            {
                string pathFileName = Path.ChangeExtension(shapePathFileName, ext);
                if (File.Exists(pathFileName)) File.Delete(pathFileName);
            }
        }

        private int GetUpperBounds(IEnumerable<KeyValuePair<string, string>> existingShapePathFileNames)
        {
            int upperBounds = existingShapePathFileNames.Sum(tmpPathFileName =>
            {
                int featuresCount = 0;
                ShapeFileFeatureLayer featureLayer = new ShapeFileFeatureLayer(tmpPathFileName.Key);
                featureLayer.SafeProcess(() =>
                {
                    featuresCount = featureLayer.QueryTools.GetCount();
                });

                return featuresCount;
            });

            return upperBounds;
        }

        private static void CreateIndexFile(string idxPathFileName, string sourceShapeFilePath)
        {
            ShapeFileFeatureLayer sourceLayer = new ShapeFileFeatureLayer(sourceShapeFilePath);
            sourceLayer.RequireIndex = false;
            ShapeFileType shapeFileType = ShapeFileType.Null;
            sourceLayer.SafeProcess(() =>
            {
                shapeFileType = sourceLayer.GetShapeFileType();
            });
            //sourceLayer.Open();
            //ShapeFileType shapeFileType = sourceLayer.GetShapeFileType();
            //sourceLayer.Close();

            if (shapeFileType == ShapeFileType.Point || shapeFileType == ShapeFileType.PointZ || shapeFileType == ShapeFileType.PointM)
            {
                RtreeSpatialIndex.CreatePointSpatialIndex(idxPathFileName, RtreeSpatialIndexPageSize.EightKilobytes, RtreeSpatialIndexDataFormat.Float);
            }
            else
            {
                RtreeSpatialIndex.CreateRectangleSpatialIndex(idxPathFileName, RtreeSpatialIndexPageSize.EightKilobytes, RtreeSpatialIndexDataFormat.Float);
            }
        }

        private static void MoveFile(string sourcePathFileName, string targetPathFileName, bool copyTo = false)
        {
            if (File.Exists(targetPathFileName))
            {
                File.Delete(targetPathFileName);
            }

            if (File.Exists(sourcePathFileName))
            {
                if (copyTo)
                {
                    File.Copy(sourcePathFileName, targetPathFileName);
                }
                else
                {
                    File.Move(sourcePathFileName, targetPathFileName);
                }
                File.SetAttributes(targetPathFileName, FileAttributes.Normal);
            }
        }

        private static void CreateDbfFile(string shapePathFileName, string outputShapePathFileName)
        {
            string sourceDbfFileName = Path.ChangeExtension(shapePathFileName, ".dbf");
            string targetDbfFileName = Path.ChangeExtension(outputShapePathFileName, ".dbf");
            MoveFile(sourceDbfFileName, targetDbfFileName, true);
        }

        private static void CreateOutputPath(string outputPathName)
        {
            if (!Directory.Exists(outputPathName))
            {
                Directory.CreateDirectory(outputPathName);
            }
        }

        public enum ShapeFileExtensionSelectionMode
        {
            Default = 0,
            Basic = 1,
            All = 2
        }
    }
}