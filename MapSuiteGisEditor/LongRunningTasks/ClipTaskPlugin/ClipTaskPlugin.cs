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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.GISEditor;
using ThinkGeo.MapSuite.WpfDesktopEdition.Extension;

namespace LongRunningTaskPlugins
{
    public class ClipTaskPlugin : LongRunningTaskPlugin
    {
        //private string masterLayerTempFilePath;
        //private string clippingLayerTempFilePath;
        private string outputPath;
        private string wkt;
        private ClipType clipType;
        private FeatureSource masterLayerFeatureSource;
        private FeatureSource[] clippingLayerFeatureSources;

        public ClipTaskPlugin()
        { }

        protected override void RunCore(Dictionary<string, string> parameters)
        {
            ParseParameters(parameters);
            var clippingFeatures = GetClippingFeatures();

            masterLayerFeatureSource.Open();
            var columns = masterLayerFeatureSource.GetColumns();
            masterLayerFeatureSource.Close();

            var clippedFeatures = Clip(masterLayerFeatureSource, clippingFeatures, clipType);
            ExportToFile(outputPath, clippedFeatures, columns);

            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
            args.Message = "Finished";
            OnUpdatingProgress(args);
        }

        private void ExportToFile(string fileName, IEnumerable<Feature> features, IEnumerable<FeatureSourceColumn> columns)
        {
            ShpFileExporter exporter = new ShpFileExporter();
            var results = features.Where(tmpFeature => tmpFeature.GetBoundingBox().UpperLeftPoint.X > -180 && tmpFeature.GetBoundingBox().UpperRightPoint.X < 180).ToList();
            Console.WriteLine(results.Count);
            exporter.ExportToFile(new FileExportInfo(features, columns, fileName, wkt));
        }

        private IEnumerable<Feature> GetClippingFeatures()
        {
            foreach (var clippingFeatureSource in clippingLayerFeatureSources)
            {
                clippingFeatureSource.Open();
                var allFeatures = clippingFeatureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
                foreach (var feature in allFeatures)
                {
                    yield return feature;
                }
                clippingFeatureSource.Close();
            }
        }

        private void ParseParameters(Dictionary<string, string> parameters)
        {
            string[] parameterNames = 
            {
                "MasterLayerFeatureSourceInString",
                "ClippingLayerFeatureSourcesInString",
                "OutputPath",
                "Wkt",
                "ClipType",
            };

            bool allExist = parameterNames.All(name => parameters.ContainsKey(name));

            if (allExist)
            {
                masterLayerFeatureSource = (FeatureSource)GetObjectFromString(parameters[parameterNames[0]]);
                clippingLayerFeatureSources = (FeatureSource[])GetObjectFromString(parameters[parameterNames[1]]);
                outputPath = parameters[parameterNames[2]];
                wkt = parameters[parameterNames[3]];
                Enum.TryParse<ClipType>(parameters[parameterNames[4]], out clipType);
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

        public Collection<Feature> Clip(FeatureSource featureSource, IEnumerable<Feature> areaBaseShape, ClipType clipMode)
        {
            Collection<Feature> clipedFeatures = new Collection<Feature>();
            switch (clipMode)
            {
                case ClipType.Inverse:
                    foreach (var feature in InverseClip(featureSource, areaBaseShape))
                    {
                        clipedFeatures.Add(feature);
                    }
                    break;
                case ClipType.Standard:
                default:
                    foreach (var feature in StandardClip(featureSource, areaBaseShape))
                    {
                        clipedFeatures.Add(feature);
                    }
                    break;
            }

            return clipedFeatures;
        }

        private IEnumerable<Feature> InverseClip(FeatureSource featureSource, IEnumerable<Feature> clippingFeatures)
        {
            lock (featureSource)
            {
                if (!featureSource.IsOpen) featureSource.Open();
                Collection<Feature> results = featureSource.GetFeaturesOutsideBoundingBox(ExtentHelper.GetBoundingBoxOfItems(clippingFeatures), ReturningColumnsType.AllColumns);
                Collection<Feature> sourceFeatures = new Collection<Feature>();
                ShapeFileType shapeFileType = ((ShapeFileFeatureSource)featureSource).GetShapeFileType();
                int index = 1;
                if (shapeFileType == ShapeFileType.Point || shapeFileType == ShapeFileType.Multipoint)
                {
                    featureSource.Open();
                    Collection<Feature> allFeatures = featureSource.GetAllFeatures(ReturningColumnsType.AllColumns);
                    featureSource.Close();
                    foreach (Feature f in results)
                    {
                        allFeatures.Remove(f);
                    }
                    foreach (var f in InverseClipPoints(allFeatures, clippingFeatures, shapeFileType))
                    {
                        results.Add(f);
                    }
                }
                else if (shapeFileType == ShapeFileType.Polyline)
                {
                    bool isOpen = false;
                    Projection tmpProjection = null;
                    MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(clippingFeatures));
                    if (featureSource.Projection != null
                        && featureSource.Projection is GISEditorManagedProj4Projection
                        && ((GISEditorManagedProj4Projection)featureSource.Projection).IsProjectionParametersEqual)
                    {
                        tmpProjection = featureSource.Projection;
                        if (featureSource.IsOpen)
                        {
                            featureSource.Close();
                            featureSource.Projection.Close();
                            isOpen = true;
                        }
                        featureSource.Projection = null;
                    }
                    featureSource.Open();
                    featureSource.GetFeaturesInsideBoundingBox(areaBaseShape.GetBoundingBox(), ReturningColumnsType.AllColumns).ForEach(f => { if (!areaBaseShape.Contains(f)) { sourceFeatures.Add(f); } });
                    int count = sourceFeatures.Count;
                    if (tmpProjection != null)
                    {
                        featureSource.Projection = tmpProjection;
                        if (isOpen) { featureSource.Open(); }
                    }
                    if (featureSource.IsOpen) featureSource.Close();
                    foreach (var feature in sourceFeatures)
                    {
                        ReportProgress(index * 100 / count);
                        index++;
                        try
                        {
                            if (areaBaseShape.IsDisjointed(feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                MultilineShape multiLine = (MultilineShape)feature.GetShape();
                                MultilineShape resultShape = new MultilineShape();
                                foreach (LineShape lineShape in multiLine.Lines)
                                {
                                    if (areaBaseShape.IsDisjointed(lineShape))
                                    {
                                        resultShape.Lines.Add(lineShape);
                                    }
                                    else
                                    {
                                        Collection<PointShape> points = new Collection<PointShape>();
                                        points.Add(new PointShape(lineShape.Vertices[0]));
                                        lineShape.GetIntersection(areaBaseShape).Lines.ForEach(l =>
                                        {
                                            PointShape p1 = new PointShape(l.Vertices[0]);
                                            if (points.Count(p => p.X == p1.X && p.Y == p1.Y && p.Z == p1.Z) <= 0)
                                            {
                                                points.Add(p1);
                                            }
                                            PointShape p2 = new PointShape(l.Vertices[l.Vertices.Count - 1]);
                                            if (points.Count(p => p.X == p2.X && p.Y == p2.Y && p.Z == p2.Z) <= 0)
                                            {
                                                points.Add(p2);
                                            }
                                        });
                                        PointShape endPoint = new PointShape(lineShape.Vertices[lineShape.Vertices.Count - 1]);
                                        if (points.Count(p => p.X == endPoint.X && p.Y == endPoint.Y && p.Z == endPoint.Z) <= 0)
                                        {
                                            points.Add(endPoint);
                                        }

                                        for (int i = 0; i < points.Count; i++)
                                        {
                                            if (i != points.Count - 1)
                                            {
                                                LineBaseShape lineBaseShape = lineShape.GetLineOnALine(points[i], points[i + 1]);

                                                if (!areaBaseShape.Intersects(lineBaseShape.GetCenterPoint()))
                                                {
                                                    resultShape.Lines.Add((LineShape)lineBaseShape);
                                                }
                                            }
                                        }
                                    }
                                }
                                if (resultShape != null && resultShape.Lines.Count > 0)
                                    results.Add(new Feature(resultShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else if (shapeFileType == ShapeFileType.Polygon)
                {
                    MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(clippingFeatures));

                    bool isOpen = false;
                    Projection tmpProjection = null;
                    if (featureSource.Projection != null
                        && featureSource.Projection is GISEditorManagedProj4Projection
                        && ((GISEditorManagedProj4Projection)featureSource.Projection).IsProjectionParametersEqual)
                    {
                        tmpProjection = featureSource.Projection;
                        if (featureSource.IsOpen)
                        {
                            featureSource.Close();
                            featureSource.Projection.Close();
                            isOpen = true;
                        }
                        featureSource.Projection = null;
                    }
                    if (!featureSource.IsOpen) featureSource.Open();
                    featureSource.GetFeaturesInsideBoundingBox(areaBaseShape.GetBoundingBox(), ReturningColumnsType.AllColumns).ForEach(f => { if (!areaBaseShape.IsDisjointed(f)) { sourceFeatures.Add(f); } });
                    if (featureSource.IsOpen) featureSource.Close();
                    if (tmpProjection != null)
                    {
                        featureSource.Projection = tmpProjection;
                        if (isOpen) { featureSource.Open(); }
                    }

                    int count = sourceFeatures.Count;
                    foreach (var feature in sourceFeatures)
                    {
                        ReportProgress(index * 100 / count);
                        index++;
                        try
                        {
                            if (areaBaseShape.IsDisjointed(feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                var clippedShape = ((AreaBaseShape)feature.GetShape()).GetDifference(areaBaseShape);
                                if (clippedShape != null && clippedShape.Polygons.Count > 0)
                                {
                                    results.Add(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("The ShapeFileType is not supported.");
                }
                return results;
            }
        }

        private IEnumerable<Feature> InverseClipPoints(IEnumerable<Feature> masterFeatures, IEnumerable<Feature> clippingFeatures, ShapeFileType shpFileType)
        {
            ConcurrentQueue<Feature> results = new ConcurrentQueue<Feature>();
            ConcurrentQueue<Feature> cqMasterFeatures = new ConcurrentQueue<Feature>(masterFeatures);
            int index = 1;
            int count = cqMasterFeatures.Count;
            if (shpFileType == ShapeFileType.Point)
            {
                Parallel.ForEach(cqMasterFeatures, feature =>
                {
                    ReportProgress(index * 100 / count);
                    index++;
                    if (!clippingFeatures.Any(f =>
                    {
                        try
                        {
                            return f.GetShape().Intersects(feature);
                        }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                            return false;
                        }
                    }))
                    {
                        results.Enqueue(feature);
                    }
                });
            }
            else
            {
                Parallel.ForEach(cqMasterFeatures, feature =>
                {
                    ReportProgress(index * 100 / count);
                    index++;
                    MultipointShape multiPoints = feature.GetShape() as MultipointShape;
                    if (multiPoints != null)
                    {
                        MultipointShape resultPoints = new MultipointShape();
                        Parallel.ForEach(multiPoints.Points, p =>
                        {
                            if (!clippingFeatures.Any(f =>
                            {
                                try { return f.GetShape().Intersects(p); }
                                catch (Exception ex)
                                {
                                    HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                                    return false;
                                }
                            }))
                            {
                                resultPoints.Points.Add(p);
                            }
                        });
                        if (resultPoints.Points.Count > 0)
                        {
                            results.Enqueue(new Feature(resultPoints.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                        }
                    }
                });
            }
            return results;
        }

        private IEnumerable<Feature> StandardClip(FeatureSource featureSource, IEnumerable<Feature> features)
        {
            lock (featureSource)
            {
                Collection<Feature> results = new Collection<Feature>();

                //There is a bug about projection boundingbox, here is a workaround for it.
                bool isOpen = false;
                Projection tmpProjection = null;
                if (featureSource.Projection != null
                    && featureSource.Projection is GISEditorManagedProj4Projection
                    && ((GISEditorManagedProj4Projection)featureSource.Projection).IsProjectionParametersEqual)
                {
                    tmpProjection = featureSource.Projection;
                    if (featureSource.IsOpen)
                    {
                        featureSource.Close();
                        featureSource.Projection.Close();
                        isOpen = true;
                    }
                    featureSource.Projection = null;
                }

                if (!featureSource.IsOpen) featureSource.Open();
                Collection<Feature> sourceFeatures = featureSource.GetFeaturesInsideBoundingBox(ExtentHelper.GetBoundingBoxOfItems(features), ReturningColumnsType.AllColumns);

                if (tmpProjection != null)
                {
                    featureSource.Projection = tmpProjection;
                    if (isOpen)
                    {
                        featureSource.Open();
                    }
                }

                ShapeFileType shapeFileType = ((ShapeFileFeatureSource)featureSource).GetShapeFileType();
                if (featureSource.IsOpen) featureSource.Close();

                int index = 1;
                int count = sourceFeatures.Count;
                if (shapeFileType == ShapeFileType.Point || shapeFileType == ShapeFileType.Multipoint)
                {
                    return StandardClipPoints(sourceFeatures, features, shapeFileType);
                }
                else if (shapeFileType == ShapeFileType.Polyline)
                {
                    MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(features));
                    foreach (var feature in sourceFeatures)
                    {
                        ReportProgress(index * 100 / count);
                        index++;
                        try
                        {
                            if (areaBaseShape.Contains(feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                var clippedShape = ((LineBaseShape)feature.GetShape()).GetIntersection(areaBaseShape);
                                if (clippedShape != null && clippedShape.Lines.Count > 0)
                                {
                                    results.Add(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else if (shapeFileType == ShapeFileType.Polygon)
                {
                    MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(features));
                    foreach (var feature in sourceFeatures)
                    {
                        ReportProgress(index * 100 / count);
                        try
                        {
                            index++;
                            if (areaBaseShape.Contains(feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                var clippedShape = areaBaseShape.GetIntersection(feature);
                                if (clippedShape != null && clippedShape.Polygons.Count > 0)
                                {
                                    results.Add(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("The ShapeFileType is not supported.");
                }
                return results;
            }
        }

        private IEnumerable<Feature> StandardClipPoints(IEnumerable<Feature> masterFeatures, IEnumerable<Feature> clippingFeatures, ShapeFileType shpFileType)
        {
            ConcurrentQueue<Feature> results = new ConcurrentQueue<Feature>();
            int index = 1;
            int count = masterFeatures.Count();
            ConcurrentQueue<Feature> cqMasterFeatures = new ConcurrentQueue<Feature>(masterFeatures);
            if (shpFileType == ShapeFileType.Point)
            {
                Parallel.ForEach(cqMasterFeatures, feature =>
                {
                    index++;
                    ReportProgress(index * 100 / count);
                    if (clippingFeatures.Any(f =>
                    {
                        try { return f.GetShape().Intersects(feature); }
                        catch (Exception ex)
                        {
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                            return false;
                        }
                    }))
                    {
                        results.Enqueue(feature);
                    }
                });
            }
            else
            {
                Parallel.ForEach(cqMasterFeatures, feature =>
                {
                    ReportProgress(index * 100 / count);
                    index++;
                    MultipointShape multiPoints = feature.GetShape() as MultipointShape;
                    if (multiPoints != null)
                    {
                        MultipointShape resultPoints = new MultipointShape();
                        Parallel.ForEach(multiPoints.Points, p =>
                        {
                            if (clippingFeatures.Any(f =>
                            {
                                try { return f.GetShape().Intersects(p); }
                                catch (Exception ex)
                                {
                                    HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                                    return false;
                                }
                            }))
                            {
                                resultPoints.Points.Add(p);
                            }
                        });
                        if (resultPoints.Points.Count > 0)
                        {
                            results.Enqueue(new Feature(resultPoints.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                        }
                    }
                });
            }
            return results;
        }

        private Collection<Feature> GetValidFeatures(IEnumerable<Feature> features)
        {
            Collection<Feature> validFeatures = new Collection<Feature>();
            Parallel.ForEach(features, tmpFeature =>
            {
                var validateResult = tmpFeature.GetShape().Validate(ShapeValidationMode.Simple);
                if (validateResult.IsValid)
                {
                    validFeatures.Add(tmpFeature);
                }
                else
                {
                    HandleExceptionFromInvalidFeature(tmpFeature.Id, validateResult.ValidationErrors);
                }
            });
            return validFeatures;
        }

        private void HandleExceptionFromInvalidFeature(string featureId, string errorMessage)
        {
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Error);
            args.Message = featureId;
            args.ExceptionInfo = new LongRunningTaskExceptionInfo(errorMessage, null);

            OnUpdatingProgress(args);
        }

        private void ReportProgress(int progress)
        {
            var args = new UpdatingProgressLongRunningTaskPluginEventArgs(LongRunningTaskState.Updating);
            args.Current = progress;

            OnUpdatingProgress(args);
        }
    }
}
