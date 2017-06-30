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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;

namespace BlendTaskPlugin
{
    public class BlendTaskPlugin : LongRunningTaskPlugin
    {
        private bool isIntersect;
        private bool isCombine;
        //private bool outputToMap;
        private bool outputToFile;
        private string outputFilePath;
        private string displayProjectionParameters;
        //private string tempShapeFilePath;
        private IEnumerable<FeatureSourceColumn> columnsToInclude;
        private FeatureSource[] featureSources;

        public BlendTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            bool allParametersValid = ParseParameters(parameters);

            if (allParametersValid)
            {
                IEnumerable<Feature> features = GetFeaturesFromTempFile();
                BlendFeatures(features);
            }
            else
            {
                //report error and return.
            }
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

            OutPutResults(resultFeatures);
        }

        private List<Feature> ShatterFeatures(List<Feature> features, Feature featureToBeShattered)
        {
            List<Feature> resultFeatures = new List<Feature>();

            bool isShattered = false;
            for (int i = 0; i < features.Count; i++)
            {
                Feature tmpFeature = features[i];
                if (NeedToShatter(featureToBeShattered, tmpFeature))
                {
                    foreach (var shatteredFeature in ShatterTwoFeatures(featureToBeShattered, tmpFeature))
                    {
                        resultFeatures.Add(BlurCopy(shatteredFeature));
                    }

                    try
                    {
                        var restShapeToBeShattered = ((AreaBaseShape)featureToBeShattered.GetShape()).GetDifference((AreaBaseShape)tmpFeature.GetShape());
                        if (restShapeToBeShattered != null)
                        {
                            featureToBeShattered = new Feature(restShapeToBeShattered, featureToBeShattered.ColumnValues);
                        }
                    }
                    catch (Exception e)
                    {
                        HandleExceptionFromInvalidFeature(tmpFeature.Id, e.Message);
                    }

                    isShattered = true;
                }
                else
                {
                    resultFeatures.Add(tmpFeature);
                }
            }

            if (!isShattered) resultFeatures.Add(BlurCopy(featureToBeShattered));
            return resultFeatures;
        }

        private static Feature BlurCopy(Feature feature)
        {
            return new Feature(feature.GetWellKnownBinary(), Guid.NewGuid().ToString(), feature.ColumnValues);
        }

        private IEnumerable<Feature> ShatterTwoFeatures(Feature feature1, Feature feature2)
        {
            var shape1 = (AreaBaseShape)feature1.GetShape();
            var shape2 = (AreaBaseShape)feature2.GetShape();

            bool intersects = !shape1.IsDisjointed(shape2);

            var columns = GetColumnValues(feature1, feature2);

            if (intersects)
            {
                MultipolygonShape intersection = null;
                MultipolygonShape difference12 = null;
                MultipolygonShape difference21 = null;
                try
                {
                    intersection = shape1.GetIntersection(shape2);

                    difference12 = shape1.GetDifference(shape2);

                    difference21 = shape2.GetDifference(shape1);
                }
                catch (Exception e)
                {
                    HandleExceptionFromInvalidFeature(feature1.Id, e.Message);
                    HandleExceptionFromInvalidFeature(feature2.Id, e.Message);
                }

                if (intersection != null)
                {
                    foreach (var item in intersection.Polygons)
                    {
                        yield return new Feature(item, columns);
                    }
                }

                if (difference12 != null)
                {
                    foreach (var item in difference12.Polygons)
                    {
                        yield return new Feature(item, columns);
                    }
                }
                else yield return new Feature(shape1, columns);


                if (difference21 != null)
                {
                    foreach (var item in difference21.Polygons)
                    {
                        yield return new Feature(item, columns);
                    }
                }
                else yield return new Feature(shape2, columns);
            }
            else
            {
                yield return feature1;
                yield return feature2;
            }
        }

        private bool NeedToShatter(Feature feature1, Feature feature2)
        {
            return NeedToShatter(new Feature[] { feature1, feature2 });
        }

        private bool NeedToShatter(IEnumerable<Feature> features)
        {
            var shapes = features.Select(f => (AreaBaseShape)f.GetShape()).ToArray();

            for (int i = 0; i < shapes.Length - 1; i++)
            {
                for (int j = i + 1; j < shapes.Length; j++)
                {
                    try
                    {
                        if (shapes[i].Overlaps(shapes[j]) || shapes[i].IsWithin(shapes[j]) || shapes[j].IsWithin(shapes[i]))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleExceptionFromInvalidFeature(shapes[i].Id, ex.Message);
                    }
                }
            }

            return false;
        }

        private IEnumerable<Feature> CombineFeatures(IEnumerable<Feature> features)
        {
            List<Feature> featuresList = features.ToList();

            List<Feature> resultFeatures = new List<Feature>();
            for (int i = 0; i < featuresList.Count; i++)
            {
                int progress = i * 100 / featuresList.Count;
                ReportProgress(progress);

                Feature feature = featuresList[i];
                var shatteredFeatures = ShatterFeatures(resultFeatures, feature);
                resultFeatures.Clear();
                for (int j = 0; j < shatteredFeatures.Count; j++)
                {
                    resultFeatures.Add(shatteredFeatures[j]);
                }
            }

            return resultFeatures;
        }

        private IEnumerable<Feature> IntersectFeatures(IEnumerable<Feature> features)
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
                    ReportProgress(progress);
                    var otherFeatures = featureGroups.Where(g => featureGroups.IndexOf(g) > i).SelectMany(f => f);
                    Parallel.ForEach(otherFeatures, otherFeature =>
                    {
                        try
                        {
                            AreaBaseShape originalShape = (AreaBaseShape)feature.GetShape();
                            AreaBaseShape matchShape = (AreaBaseShape)otherFeature.GetShape();

                            if (originalShape.Intersects(matchShape))
                            {
                                AreaBaseShape resultShape = originalShape.GetIntersection(matchShape);
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
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    });
                    index++;
                }
            }

            return results;
        }

        private void ReportProgress(int progress)
        {
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
            args.Current = progress;

            OnUpdatingProgress(args);
        }

        private void HandleExceptionFromInvalidFeature(string id, string message)
        {
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
            args.ExceptionInfo = new LongRunningTaskExceptionInfo(message, null);
            args.Message = id;

            OnUpdatingProgress(args);
        }

        private Dictionary<string, string> GetColumnValues(Feature first, Feature second)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

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

            return results;
        }

        private void OutPutResults(IEnumerable<Feature> resultFeatures)
        {
            string exportPath = string.Empty;

            //if (outputToMap)
            //{
            //    string dir = Path.GetDirectoryName(tempShapeFilePath);
            //    string fileName = Path.GetFileNameWithoutExtension(tempShapeFilePath);
            //    string outputToMapTempFilePath = Path.Combine(dir, fileName + "OutPutToMapResult.shp");

            //    exportPath = outputToMapTempFilePath;

            //    var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
            //    args.Parameters.Add("OutputToMapTempFilePath", exportPath);
            //    OnUpdatingProgress(args);
            //}
            //else
            if (outputToFile && !string.IsNullOrEmpty(outputFilePath))
            {
                exportPath = outputFilePath;

                var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
                args.Message = "Creating File";
                OnUpdatingProgress(args);
            }

            FileExportInfo info = new FileExportInfo(resultFeatures, columnsToInclude, exportPath, displayProjectionParameters);
            ShpFileExporter exporter = new ShpFileExporter();
            exporter.ExportToFile(info);

            OnUpdatingProgress(new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating) { Message = "Finished" });
        }

        private IEnumerable<Feature> GetFeaturesFromTempFile()
        {
            if (isCombine)
            {
                foreach (var featureSource in featureSources)
                {
                    featureSource.Open();
                    var allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
                    featureSource.Close();

                    foreach (var feature in allFeatures)
                    {
                        yield return feature;
                    }
                }
            }
            else if (isIntersect)
            {
                //string tempDir = Path.GetDirectoryName(tempShapeFilePath);
                //string fileName = Path.GetFileNameWithoutExtension(tempShapeFilePath);

                //string[] files = Directory.GetFiles(tempDir, fileName + "*.shp");
                foreach (var featureSource in featureSources)
                {
                    featureSource.Open();
                    var features = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns).ToArray();
                    if (columnsToInclude == null)
                    {
                        columnsToInclude = featureSource.GetColumns();
                    }
                    featureSource.Close();

                    for (int i = 0; i < features.Length; i++)
                    {
                        features[i].Tag = featureSource;
                        yield return features[i];
                    }
                }
            }
        }

        private object GetObjectFromString(string objectInString)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(objectInString)))
            {
                return formatter.Deserialize(stream);
            }
        }

        private bool ParseParameters(Dictionary<string, string> parameters)
        {
            bool areAllParametersValid = false;

            string[] parameterNames = 
            { 
                "FeatureSourcesInString", 
                "IsIntersect", 
                "IsCombine",
                //"OutputToMap", 
                "OutputToFile", 
                "OutputFilePath", 
                "DisplayProjectionParameters" 
            };

            bool allParametersExist = parameterNames.All(parameterName => parameters.ContainsKey(parameterName));

            if (allParametersExist)
            {
                featureSources = (FeatureSource[])GetObjectFromString(parameters[parameterNames[0]]);
                isIntersect = parameters[parameterNames[1]] == bool.TrueString ? true : false;
                isCombine = parameters[parameterNames[2]] == bool.TrueString ? true : false;
                //outputToMap = parameters[parameterNames[3]] == bool.TrueString ? true : false;
                outputToFile = parameters[parameterNames[3]] == bool.TrueString ? true : false;
                outputFilePath = parameters[parameterNames[4]];
                displayProjectionParameters = parameters[parameterNames[5]];

                areAllParametersValid = true;
            }

            return areAllParametersValid;
        }
    }
}
