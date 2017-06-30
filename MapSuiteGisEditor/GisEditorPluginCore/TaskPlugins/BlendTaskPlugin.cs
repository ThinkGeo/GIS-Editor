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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BlendTaskPlugin : GeoTaskPlugin
    {

        private bool isIntersect;
        private bool isCombine;
        private bool outputToFile;
        private bool isCanceled;
        private string outputPathFileName;
        private StringBuilder exceptionMessage;
        private string displayProjectionParameters;
        private List<FeatureLayer> featureSources;
        private List<FeatureSource> featuresToBlend;
        private IEnumerable<FeatureSourceColumn> columnsToInclude;
        private Dictionary<string, Collection<Tuple<string, string>>> renameDictionary;

        public BlendTaskPlugin()
        { }

        public List<FeatureSource> FeaturesToBlend
        {
            get { return featuresToBlend; }
            set { featuresToBlend = value; }
        }

        public bool IsIntersect
        {
            get { return isIntersect; }
            set { isIntersect = value; }
        }

        public bool IsCombine
        {
            get { return isCombine; }
            set { isCombine = value; }
        }

        public bool OutputToFile
        {
            get { return outputToFile; }
            set { outputToFile = value; }
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

        public IEnumerable<FeatureSourceColumn> ColumnsToInclude
        {
            get { return columnsToInclude; }
            set { columnsToInclude = value; }
        }

        public List<FeatureLayer> FeatureLayers
        {
            get { return featureSources; }
            set { featureSources = value; }
        }

        public Dictionary<string, Collection<Tuple<string, string>>> RenameDictionary
        {
            get { return renameDictionary; }
            set { renameDictionary = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("BlendTaskPluginOperationName");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            FeaturesToBlend = FeatureLayers.Select(f => f.FeatureSource).ToList();

            IEnumerable<Feature> features = GetFeaturesFromTempFile();
            BlendFeatures(features);
        }

        private void BlendFeatures(IEnumerable<Feature> sourceFeatures)
        {
            IEnumerable<Feature> resultFeatures = null;

            if (isIntersect)
            {
                resultFeatures = IntersectFeatures(sourceFeatures);
            }
            else if (isCombine)
            {
                resultFeatures = CombineFeatures(sourceFeatures);
            }

            if (!isCanceled)
            {
                if (exceptionMessage != null)
                {
                    UpdatingTaskProgressEventArgs e = new UpdatingTaskProgressEventArgs(TaskState.Error);
                    e.Error = new ExceptionInfo(exceptionMessage.ToString(), string.Empty, string.Empty);
                    OnUpdatingProgress(e);
                }
                OutPutResults(resultFeatures);
            }
        }

        private List<Feature> ShatterFeatures(List<Feature> features, Feature featureToBeShattered)
        {
            List<Feature> resultFeatures = new List<Feature>();

            if (featureToBeShattered != null && featureToBeShattered.CanMakeValid) featureToBeShattered = featureToBeShattered.MakeValid();

            for (int i = 0; i < features.Count; i++)
            {
                Feature tmpFeature = features[i];
                tmpFeature = GetValidPolygonFeature(tmpFeature);
                if (tmpFeature == null) continue;

                if (tmpFeature.CanMakeValid) tmpFeature = tmpFeature.MakeValid();
                if (featureToBeShattered != null)
                {
                    //if (featureToBeShattered.IsDisjointed(tmpFeature))
                    if (SqlTypesGeometryHelper.IsDisjointed(featureToBeShattered, tmpFeature))
                    {
                        resultFeatures.Add(tmpFeature);
                    }
                    else
                    {
                        foreach (var shatteredFeature in ShatterTwoFeatures(ref featureToBeShattered, tmpFeature).ToArray())
                        {
                            resultFeatures.Add(shatteredFeature);
                        }
                    }
                }
                else resultFeatures.Add(tmpFeature);
            }

            if (featureToBeShattered != null)
            {
                AddValidFeature(resultFeatures, featureToBeShattered);
            }

            return resultFeatures;
        }

        private static Feature BlurCopy(Feature feature)
        {
            return new Feature(feature.GetWellKnownBinary(), Guid.NewGuid().ToString(), feature.ColumnValues);
        }

        private IEnumerable<Feature> ShatterTwoFeatures(ref Feature feature1, Feature feature2)
        {
            Collection<Feature> resultFeatures = new Collection<Feature>();
            var columns = GetColumnValues(feature1, feature2);
            // var diff1 = feature1.GetDifference(feature2);
            feature1 = SqlTypesGeometryHelper.MakeValid(feature1);
            feature2 = SqlTypesGeometryHelper.MakeValid(feature2);
            var diff1 = SqlTypesGeometryHelper.GetDifference(feature1, feature2);
            //var diff2 = feature2.GetDifference(feature1);
            var diff2 = SqlTypesGeometryHelper.GetDifference(feature2, feature1);
            //var inter = feature1.GetIntersection(feature2);
            var inter = SqlTypesGeometryHelper.GetIntersection(feature1, feature2);

            feature1 = diff1;
            if (inter != null)
            {
                inter = new Feature(inter.GetWellKnownBinary(), inter.Id, columns);
                AddValidFeature(resultFeatures, inter);
            }

            if (diff2 != null)
            {
                AddValidFeature(resultFeatures, diff2);
            }

            return resultFeatures;
        }

        private void AddValidFeature(IList<Feature> resultFeatures, Feature feature)
        {
            var validFeature = GetValidPolygonFeature(feature);
            if (validFeature != null)
            {
                resultFeatures.Add(validFeature);
            }
        }

        private Feature GetValidPolygonFeature(Feature feature)
        {
            var featureType = feature.GetWellKnownType();
            if (featureType == WellKnownType.Polygon || featureType == WellKnownType.Multipolygon)
            {
                return feature;
            }
            if (featureType == WellKnownType.GeometryCollection)
            {
                MultipolygonShape multipolygonShape = new MultipolygonShape();
                GeometryCollectionShape geometryCollectionShape = (GeometryCollectionShape)feature.GetShape();
                foreach (var innerShape in geometryCollectionShape.Shapes)
                {
                    var innerShapeType = innerShape.GetWellKnownType();
                    if (innerShapeType == WellKnownType.Polygon)
                    {
                        multipolygonShape.Polygons.Add((PolygonShape)innerShape);
                    }
                    else if (innerShapeType == WellKnownType.Multipolygon)
                    {
                        foreach (var innerPolygon in ((MultipolygonShape)innerShape).Polygons)
                        {
                            multipolygonShape.Polygons.Add(innerPolygon);
                        }
                    }
                }

                if (multipolygonShape.Polygons.Count > 0) return new Feature(multipolygonShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
            }

            return null;
        }

        public IEnumerable<Feature> CombineFeatures(IEnumerable<Feature> features)
        {
            List<Feature> featuresList = features.ToList();

            List<Feature> resultFeatures = new List<Feature>();
            for (int i = 0; i < featuresList.Count; i++)
            {
                int progress = i * 100 / featuresList.Count;
                isCanceled = ReportProgress(progress, i, featuresList.Count);
                if (isCanceled) break;

                Feature feature = featuresList[i];
                var shatteredFeatures = ShatterFeatures(resultFeatures, feature);
                resultFeatures.Clear();
                for (int j = 0; j < shatteredFeatures.Count; j++)
                {
                    resultFeatures.Add(BlurCopy(shatteredFeatures[j]));
                }
            }

            return resultFeatures;
        }

        public IEnumerable<Feature> IntersectFeatures(IEnumerable<Feature> features)
        {
            List<Feature> featuresList = features.ToList();

            var results = new ConcurrentStack<Feature>();
            var featureGroups = featuresList.GroupBy(feature => feature.Tag).ToList();
            int index = 1;
            for (int i = 0; i < featureGroups.Count; i++)
            {
                var group = featureGroups[i];
                foreach (var feature in group)
                {
                    int progress = index * 100 / featuresList.Count;
                    isCanceled = ReportProgress(progress, index, featuresList.Count);
                    if (isCanceled) break;

                    var otherFeatures = featureGroups.Where(g => featureGroups.IndexOf(g) > i).SelectMany(f => f);
                    Parallel.ForEach(otherFeatures, otherFeature =>
                    {
                        try
                        {
                            AreaBaseShape originalShape = (AreaBaseShape)feature.GetShape();
                            AreaBaseShape matchShape = (AreaBaseShape)otherFeature.GetShape();

                            //if (originalShape.Intersects(matchShape))
                            if (SqlTypesGeometryHelper.Intersects(originalShape, matchShape))
                            {
                                //AreaBaseShape resultShape = originalShape.GetIntersection(matchShape);
                                AreaBaseShape resultShape = (AreaBaseShape)SqlTypesGeometryHelper.GetIntersection(originalShape, matchShape);
                                if (resultShape != null)
                                {
                                    var columnValues = GetColumnValues(feature, otherFeature);
                                    Feature resultFeature = new Feature(resultShape, columnValues);
                                    results.Push(resultFeature);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    });

                    index++;
                }
            }

            return results;
        }

        private bool ReportProgress(int progress, int current, int upperBound)
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progress);
            args.Current = current;
            args.UpperBound = upperBound;

            OnUpdatingProgress(args);

            return args.TaskState == TaskState.Canceled;
        }

        private void HandleExceptionFromInvalidFeature(string id, string message)
        {
            if (exceptionMessage == null) exceptionMessage = new StringBuilder();
            exceptionMessage.AppendLine(String.Format(CultureInfo.InvariantCulture, "Invalid feature Id: {0}; {1}", id, message));
        }

        private Dictionary<string, string> GetColumnValues(Feature first, Feature second)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            if (columnsToInclude != null)
            {
                foreach (var column in columnsToInclude)
                {
                    string columnNameCaps = column.ColumnName.ToUpperInvariant();
                    if (!results.ContainsKey(columnNameCaps))
                    {
                        if (first.ColumnValues.ContainsKey(column.ColumnName))
                        {
                            results.Add(columnNameCaps, first.ColumnValues[column.ColumnName]);
                        }
                        else if (second.ColumnValues.ContainsKey(column.ColumnName))
                        {
                            results.Add(columnNameCaps, second.ColumnValues[column.ColumnName]);
                        }
                    }
                }
            }
            return results;
        }

        private void OutPutResults(IEnumerable<Feature> resultFeatures)
        {
            string exportPath = string.Empty;
            if (outputToFile && !string.IsNullOrEmpty(outputPathFileName))
            {
                exportPath = outputPathFileName;

                var args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
                args.Message = "Creating File";
                OnUpdatingProgress(args);
            }

            FileExportInfo info = new FileExportInfo(resultFeatures.Where(f =>
            {
                var wellKnownType = f.GetWellKnownType();
                return wellKnownType == WellKnownType.Polygon || wellKnownType == WellKnownType.Multipolygon;
            }), columnsToInclude, exportPath, Proj4Projection.ConvertProj4ToPrj(displayProjectionParameters));

            Export(info);
            //try
            //{
            //    ShpFileExporter exporter = new ShpFileExporter();
            //    exporter.ExportToFile(info);
            //}
            //catch (Exception ex)
            //{
            //    UpdatingTaskProgressEventArgs e = new UpdatingTaskProgressEventArgs(TaskState.Canceled);
            //    e.Error = new ExceptionInfo(ex.Message, ex.StackTrace, ex.Source);
            //    OnUpdatingProgress(e);
            //}
        }

        private Collection<Feature> GetFeaturesFromTempFile()
        {
            Collection<Feature> resultFeatures = new Collection<Feature>();

            if (isCombine)
            {
                foreach (var featureLayer in FeatureLayers)
                {
                    featureLayer.Open();
                    var allFeatures = featureLayer.FeatureSource.GetAllFeatures(featureLayer.FeatureSource.GetDistinctColumnNames());
                    featureLayer.Close();

                    foreach (var feature in allFeatures)
                    {
                        if (RenameDictionary.ContainsKey(featureLayer.Name))
                        {
                            foreach (var item in RenameDictionary[featureLayer.Name])
                            {
                                if (feature.ColumnValues.ContainsKey(item.Item1))
                                {
                                    feature.ColumnValues[item.Item2] = feature.ColumnValues[item.Item1];
                                    feature.ColumnValues.Remove(item.Item1);
                                }
                            }
                        }
                        resultFeatures.Add(feature);
                    }
                }
            }
            else if (isIntersect)
            {
                //string tempDir = Path.GetDirectoryName(tempShapeFilePath);
                //string fileName = Path.GetFileNameWithoutExtension(tempShapeFilePath);

                //string[] files = Directory.GetFiles(tempDir, fileName + "*.shp");
                foreach (var featureLayer in FeatureLayers)
                {
                    featureLayer.Open();
                    var features = featureLayer.FeatureSource.GetAllFeatures(featureLayer.FeatureSource.GetDistinctColumnNames()).ToArray();
                    //if (columnsToInclude == null)
                    //{
                    //    columnsToInclude = featureSource.GetColumns();
                    //}
                    featureLayer.Close();

                    for (int i = 0; i < features.Length; i++)
                    {
                        features[i].Tag = featureLayer;

                        if (RenameDictionary.ContainsKey(featureLayer.Name))
                        {
                            foreach (var item in RenameDictionary[featureLayer.Name])
                            {
                                if (features[i].ColumnValues.ContainsKey(item.Item1))
                                {
                                    features[i].ColumnValues[item.Item2] = features[i].ColumnValues[item.Item1];
                                    features[i].ColumnValues.Remove(item.Item1);
                                }
                            }
                        }

                        resultFeatures.Add(features[i]);
                    }
                }
            }
            return resultFeatures;
        }
    }
}