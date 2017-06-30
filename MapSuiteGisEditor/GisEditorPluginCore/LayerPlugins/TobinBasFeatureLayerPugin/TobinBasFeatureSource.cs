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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TobinBasFeatureSource : FeatureSource
    {
        [Obfuscation(Exclude = true)]
        private string tobinBasFilePathName;

        [Obfuscation(Exclude = true)]
        private string indexPathFileName;

        [NonSerialized]
        private RtreeSpatialIndex rTreeIndex;

        [NonSerialized]
        private BasReader basReader;

        [NonSerialized]
        private Encoding encoding;

        [Obfuscation(Exclude = true)]
        private bool requireIndex;

        [Obfuscation(Exclude = true)]
        private FileAccess fileAccess;

        [NonSerialized]
        private GeoFileReadWriteMode shapeFileReadWriteMode;

        [Obfuscation(Exclude = true)]
        private Collection<Feature> annotationFeatures;

        [Obfuscation(Exclude = true)]
        private SortedList<string, BasFeatureEntity> cachedBasFeatureEntities;

        [Obfuscation(Exclude = true)]
        private int maxCacheCount = 4000;

        public Collection<Feature> AnnotationFeatures
        {
            get
            {
                if (annotationFeatures == null)
                {
                    annotationFeatures = new Collection<Feature>();
                }
                return annotationFeatures;
            }
            set { annotationFeatures = value; }
        }

        public Encoding Encoding
        {
            get { return encoding; }
            set { encoding = value; }
        }

        public bool RequireIndex
        {
            get { return requireIndex; }
            set { requireIndex = value; }
        }

        public string TobinBasFileName
        {
            get
            {
                return tobinBasFilePathName;
            }
            set
            {
                tobinBasFilePathName = value;
                indexPathFileName = Path.ChangeExtension(tobinBasFilePathName, ".idx");
            }
        }

        public TobinBasFeatureSource(string tobinBasFilePathName)
        {
            TobinBasFileName = tobinBasFilePathName;
            annotationFeatures = new Collection<Feature>();
            this.requireIndex = true;
            cachedBasFeatureEntities = new SortedList<string, BasFeatureEntity>();
        }

        protected override void OpenCore()
        {
            fileAccess = FileAccess.Read;
            GeoFileReadWriteMode rTreeFileAccess = GeoFileReadWriteMode.Read;
            if (shapeFileReadWriteMode == GeoFileReadWriteMode.ReadWrite)
            {
                fileAccess = FileAccess.ReadWrite;
                rTreeFileAccess = GeoFileReadWriteMode.ReadWrite;
            }

            Validator.CheckTobinBasFileName(tobinBasFilePathName);

            basReader = new BasReader(this.tobinBasFilePathName, fileAccess);

            OpenRtree(rTreeFileAccess);
        }

        protected override void CloseCore()
        {
            if (basReader != null)
            {
                basReader.Close();
            }
        }

        protected override Collection<FeatureSourceColumn> GetColumnsCore()
        {
            Collection<FeatureSourceColumn> columns = new Collection<FeatureSourceColumn>();

            Collection<string> basColumns = basReader.GetColumns();
            foreach (var col in basColumns)
            {
                columns.Add(new FeatureSourceColumn(col));
            }

            // TODO: Need to consider whether add the annotation columns

            columns.Add(new FeatureSourceColumn("TextString"));
            columns.Add(new FeatureSourceColumn("TextAngle"));
            columns.Add(new FeatureSourceColumn("TextFont"));
            columns.Add(new FeatureSourceColumn("TextSize"));

            return columns;
        }

        protected override Collection<Feature> GetAllFeaturesCore(IEnumerable<string> returningColumnNames)
        {
            Collection<Feature> allFeatures = new Collection<Feature>();
            annotationFeatures.Clear();

            Collection<BasFeatureEntity> basFeatures = basReader.GetAllFeatureEntities();
            foreach (var item in basFeatures)
            {
                // add shape feature.
                if (item.Shape != null)
                {
                    Feature feature = new Feature(item.Shape, item.Columns);
                    // just a temporary way to assign the id value.
                    feature.Id = item.Offset + "";
                    allFeatures.Add(feature);
                }
                // add annotation feature.
                int index = 0;
                foreach (var annotation in item.Annotations)
                {
                    Dictionary<string, string> annotationColumns = GetAnnotationColumns(annotation);

                    Feature feature = new Feature(annotation.Position, annotationColumns);
                    // just a temporary way to assign the id value.
                    feature.Id = item.Offset + "_" + index;

                    annotationFeatures.Add(feature);
                    index++;
                }
            }

            foreach (var item in annotationFeatures)
            {
                allFeatures.Add(item);
            }
            return allFeatures;
        }

        protected override Collection<Feature> GetFeaturesInsideBoundingBoxCore(RectangleShape boundingBox, IEnumerable<string> returningColumnNames)
        {
            //Validators.CheckFeatureSourceIsOpen(IsOpen);
            //Validators.CheckParameterIsNotNull(boundingBox, "boungingBox");
            //Validators.CheckShapeIsValidForOperation(boundingBox);

            Collection<Feature> returnValues = new Collection<Feature>();
            //if (rTreeIndex.HasIdx)
            if (rTreeIndex != null)
            {
                Collection<string> ids = rTreeIndex.GetFeatureIdsIntersectingBoundingBox(boundingBox);
                returnValues = GetFeaturesByIdsCore(ids, returningColumnNames);
            }
            else
            {
                returnValues = base.GetFeaturesInsideBoundingBoxCore(boundingBox, returningColumnNames);
            }
            return returnValues;
        }

        public static void BuildIndexFile(string shapePathFilename)
        {
            //Validators.CheckParameterIsNotNullOrEmpty(shapePathFilename, "pathFileName");
            //Validators.CheckShapeFileNameIsValid(shapePathFilename, "shapePathFileName");

            BuildIndexFile(shapePathFilename, Path.ChangeExtension(shapePathFilename, ".idx"), null, string.Empty, string.Empty, BuildIndexMode.DoNotRebuild);
        }

        public static void BuildIndexFile(string shapePathFilename, string indexPathFilename, Projection projection, string columnName, string regularExpression, BuildIndexMode buildIndexMode)
        {
            //Validators.CheckParameterIsNotNullOrEmpty(shapePathFilename, "shapePathFileName");
            //Validators.CheckShapeFileNameIsValid(shapePathFilename, "shapePathFileName");
            //Validators.CheckParameterIsNotNullOrEmpty(indexPathFilename, "indexPathFileName");
            //Validators.CheckParameterIsNotNull(columnName, "columnName");
            //Validators.CheckParameterIsNotNull(regularExpression, "regularExpression");
            //Validators.CheckBuildIndexModeIsValid(buildIndexMode, "buildIndexMode");

            BuildIndexFile(shapePathFilename, indexPathFilename, projection, columnName, regularExpression, buildIndexMode, Encoding.Default);
        }

        public static void BuildIndexFile(string basPathFilename, string indexPathFilename, Projection projection, string columnName, string regularExpression, BuildIndexMode buildIndexMode, Encoding encoding)
        {
            //Validators.CheckParameterIsNotNullOrEmpty(basPathFilename, "shapePathFileName");
            //Validators.CheckShapeFileNameIsValid(basPathFilename, "shapePathFileName");
            //Validators.CheckParameterIsNotNullOrEmpty(indexPathFilename, "indexPathFileName");
            //Validators.CheckParameterIsNotNull(columnName, "columnName");
            //Validators.CheckParameterIsNotNull(regularExpression, "regularExpression");
            //Validators.CheckParameterIsNotNull(encoding, "encoding");
            //Validators.CheckBuildIndexModeIsValid(buildIndexMode, "buildIndexMode");

            string tmpIndexPathFilename = Path.GetDirectoryName(indexPathFilename) + "\\TMP" + Path.GetFileName(indexPathFilename);
            if (!(File.Exists(indexPathFilename) && File.Exists(Path.ChangeExtension(indexPathFilename, ".ids"))) || buildIndexMode == BuildIndexMode.Rebuild)
            {
                RtreeSpatialIndex rTreeIndex = new RtreeSpatialIndex(tmpIndexPathFilename, GeoFileReadWriteMode.ReadWrite);

                TobinBasFeatureSource featureSource = new TobinBasFeatureSource(basPathFilename);
                featureSource.Encoding = encoding;
                featureSource.RequireIndex = false;
                featureSource.Open();
                if (projection != null)
                {
                    if (!projection.IsOpen)
                    {
                        projection.Open();
                    }
                }
                try
                {
                    //// TODO Make sure the rtree will open only once.
                    //ShapeFileType shapeType = ShapeFileType.Null;

                    //ShapeFileType currentRecordType = featureSource.basReader.GetShapeFileType();
                    //shapeType = currentRecordType;
                    RtreeSpatialIndex.CreateRectangleSpatialIndex(tmpIndexPathFilename, RtreeSpatialIndexPageSize.EightKilobytes, RtreeSpatialIndexDataFormat.Float);
                    rTreeIndex.Open();

                    Collection<BasFeatureEntity> allFeatureEntities = featureSource.basReader.GetAllFeatureEntities();

                    int recordCount = allFeatureEntities.Count;

                    DateTime startProcessTime = DateTime.Now;

                    for (int i = 0; i < recordCount; i++)
                    {
                        BasFeatureEntity currentShape = null;
                        currentShape = allFeatureEntities[i];

                        long offset = currentShape.Offset;
                        string id = offset.ToString(CultureInfo.InvariantCulture); ;

                        if (currentShape != null)
                        {
                            bool isMatch = false;
                            if (string.IsNullOrEmpty(columnName) && string.IsNullOrEmpty(regularExpression))
                            {
                                isMatch = true;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(columnName))
                                {
                                    string columnValue = featureSource.GetValueByColumnName(id.ToString(CultureInfo.InvariantCulture), columnName);
                                    isMatch = Regex.IsMatch(columnValue, regularExpression, RegexOptions.IgnoreCase);
                                }
                            }

                            if (isMatch)
                            {
                                currentShape.Id = id;

                                BuildingIndexBasFileFeatureSourceEventArgs buildingIndexBasFileFeatureSourceEventArgs = new BuildingIndexBasFileFeatureSourceEventArgs(recordCount, offset, new Feature(currentShape.Shape), startProcessTime, false, basPathFilename, i);
                                OnBuildingIndex(buildingIndexBasFileFeatureSourceEventArgs);
                                if (!buildingIndexBasFileFeatureSourceEventArgs.Cancel)
                                {
                                    BuildIndex(currentShape, rTreeIndex, projection);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    rTreeIndex.Flush();
                    rTreeIndex.Close();
                }
                finally
                {
                    if (rTreeIndex != null)
                    {
                        rTreeIndex.Close();
                    }
                    if (featureSource != null)
                    {
                        featureSource.Close();
                    }
                    if (projection != null)
                    {
                        projection.Close();
                    }

                    // Replace the old file.
                    MoveFile(tmpIndexPathFilename, indexPathFilename);
                    MoveFile(Path.ChangeExtension(tmpIndexPathFilename, ".ids"), Path.ChangeExtension(indexPathFilename, ".ids"));

                    DeleteFile(tmpIndexPathFilename);
                    DeleteFile(Path.ChangeExtension(tmpIndexPathFilename, ".ids"));
                }
            }
        }

        public static event EventHandler<BuildingIndexBasFileFeatureSourceEventArgs> BuildingIndex;

        private void PushToCache(BasFeatureEntity entity)
        {
            if (!cachedBasFeatureEntities.ContainsKey(entity.Id))
            {
                if (cachedBasFeatureEntities.Count > maxCacheCount)
                {
                    cachedBasFeatureEntities.RemoveAt(0);
                }

                cachedBasFeatureEntities.Add(entity.Id, entity);
            }
        }

        protected override Collection<Feature> GetFeaturesByIdsCore(IEnumerable<string> ids, IEnumerable<string> returningColumnNames)
        {
            Collection<Feature> featuresToDraw = new Collection<Feature>();
            annotationFeatures.Clear();

            foreach (string id in ids)
            {
                long offset;
                if (id.IndexOf('_') < 0 && long.TryParse(id, out offset))
                {
                    // TODO: Add column logic
                    //Swallow the exception to avoid pink tiles.
                    //If a shape is not valid, the we don't add it to the results.
                    try
                    {
                        BasFeatureEntity basFeature;
                        if (!cachedBasFeatureEntities.ContainsKey(id))
                        {
                            basFeature = basReader.GetFeatureEntityByOffset(offset);
                            PushToCache(basFeature);
                        }
                        else
                        {
                            basFeature = cachedBasFeatureEntities[id];
                        }

                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        foreach (var col in basFeature.Columns)
                        {
                            if (returningColumnNames.Contains(col.Key))
                            {
                                dict.Add(col.Key, col.Value);
                            }
                        }
                        Feature feature = new Feature(basFeature.Shape.GetWellKnownBinary(), id, dict);
                        feature.Id = id;
                        featuresToDraw.Add(feature);
                        foreach (var annotation in basFeature.Annotations)
                        {
                            Dictionary<string, string> annotationDict = GetAnnotationColumns(annotation);
                            Feature annotationFeature = new Feature(annotation.Position, annotationDict);

                            annotationFeatures.Add(annotationFeature);
                        }
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    string[] annotationIds = id.Split('_');
                    if (annotationIds.Length == 2)
                    {
                        long parentOffset = long.Parse(annotationIds[0]);
                        BasFeatureEntity basFeature;
                        if (!cachedBasFeatureEntities.ContainsKey(annotationIds[0]))
                        {
                            basFeature = basReader.GetFeatureEntityByOffset(parentOffset);
                        }
                        else
                        {
                            basFeature = cachedBasFeatureEntities[id];
                        }

                        int currentAnnotationIndex = int.Parse(annotationIds[1]);

                        if (currentAnnotationIndex < basFeature.Annotations.Count)
                        {
                            BasAnnotation currentAnnotation = basFeature.Annotations[currentAnnotationIndex];
                            Dictionary<string, string> annotationDict = GetAnnotationColumns(currentAnnotation);
                            Feature annotationFeature = new Feature(currentAnnotation.Position, annotationDict);
                            annotationFeature.Id = id;
                            annotationFeatures.Add(annotationFeature);
                        }
                    }
                }
            }

            foreach (var item in annotationFeatures)
            {
                featuresToDraw.Add(item);
            }
            return featuresToDraw;
        }

        protected static void OnBuildingIndex(BuildingIndexBasFileFeatureSourceEventArgs e)
        {
            EventHandler<BuildingIndexBasFileFeatureSourceEventArgs> handler = BuildingIndex;

            if (handler != null)
            {
                handler(null, e);
            }
        }

        private void OpenRtree(GeoFileReadWriteMode rTreeFileAccess)
        {
            if (rTreeIndex == null && requireIndex)
            {
                rTreeIndex = new RtreeSpatialIndex(indexPathFileName, rTreeFileAccess);
            }

            //rTreeIndex.StreamLoading += new EventHandler<StreamLoadingEventArgs>(ShapeFileFeatureSource_StreamLoading);
            //rTreeIndex.IdsEngine.StreamLoading += new EventHandler<StreamLoadingEventArgs>(ShapeFileFeatureSource_StreamLoading);

            if (requireIndex)
            {
                rTreeIndex.Open();
            }

            //if (!rTreeIndex.HasIdx && requireIndex)
            //{
            //    throw new InvalidOperationException(ExceptionDescription.IndexFileNotExisted);
            //}
        }

        private static Dictionary<string, string> GetAnnotationColumns(BasAnnotation annotation)
        {
            Dictionary<string, string> annotationDict = new Dictionary<string, string>();
            annotationDict.Add("TextString", annotation.TextString);
            annotationDict.Add("TextAngle", annotation.FontStyle.TextAngle.ToString());
            annotationDict.Add("TextFont", annotation.FontStyle.TextFont.ToString());
            annotationDict.Add("TextSize", annotation.FontStyle.TextSize.ToString());
            return annotationDict;
        }

        private string GetValueByColumnName(string p, string columnName)
        {
            throw new NotImplementedException();
        }

        private static void DeleteFile(string pathFileName)
        {
            if (File.Exists(pathFileName))
            {
                File.SetAttributes(pathFileName, FileAttributes.Normal);
                File.Delete(pathFileName);
            }
        }

        private static void MoveFile(string sourcePathFileName, string targetPathFileName)
        {
            DeleteFile(targetPathFileName);

            if (File.Exists(sourcePathFileName))
            {
                File.Move(sourcePathFileName, targetPathFileName);
            }
        }

        private static void BuildIndex(BasFeatureEntity featureEntity, RtreeSpatialIndex openedRtree, Projection openedProjection)
        {
            if (featureEntity != null)
            {
                BaseShape newShape = featureEntity.Shape;
                if (openedProjection != null)
                {
                    newShape = openedProjection.ConvertToExternalProjection(featureEntity.Shape);
                    newShape.Id = featureEntity.Id;
                }

                openedRtree.Add(newShape);
            }
        }
    }
}