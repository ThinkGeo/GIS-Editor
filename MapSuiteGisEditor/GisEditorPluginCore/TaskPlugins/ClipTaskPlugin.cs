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
using System.Threading.Tasks;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClipTaskPlugin : GeoTaskPlugin
    {
        private FeatureLayer masterLayerFeatureSource;
        private List<FeatureLayer> clippingLayerFeatureSources;
        private string outputPathFileName;
        private string wkt;
        private bool isCanceled;
        private ClippingType clippingType;

        public ClipTaskPlugin()
        { }

        public FeatureLayer MasterLayerFeatureLayer
        {
            get { return masterLayerFeatureSource; }
            set { masterLayerFeatureSource = value; }
        }

        public List<FeatureLayer> ClippingLayerFeatureLayers
        {
            get { return clippingLayerFeatureSources; }
            set { clippingLayerFeatureSources = value; }
        }

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

        public ClippingType ClippingType
        {
            get { return clippingType; }
            set { clippingType = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("ClipTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            var clippingFeatures = GetClippingFeatures();

            masterLayerFeatureSource.Open();
            var columns = masterLayerFeatureSource.FeatureSource.GetColumns();
            masterLayerFeatureSource.Close();

            var clippedFeatures = Clip(masterLayerFeatureSource, clippingFeatures.Select(f => f.MakeValidIfCan()), clippingType);
            if (!isCanceled) ExportToFile(outputPathFileName, clippedFeatures, columns);
        }

        private void ExportToFile(string fileName, IEnumerable<Feature> features, IEnumerable<FeatureSourceColumn> columns)
        {
            var info = new FileExportInfo(features, columns, fileName, wkt);
            Export(info);
        }

        private Collection<Feature> GetClippingFeatures()
        {
            Collection<Feature> resultFeatures = new Collection<Feature>();
            foreach (var clippingFeatureSource in clippingLayerFeatureSources)
            {
                clippingFeatureSource.Open();
                var allFeatures = clippingFeatureSource.FeatureSource.GetAllFeatures(ReturningColumnsType.NoColumns);
                foreach (var feature in allFeatures)
                {
                    resultFeatures.Add(feature);
                }
                clippingFeatureSource.Close();
            }

            return resultFeatures;
        }

        public Collection<Feature> Clip(FeatureLayer featureSource, IEnumerable<Feature> areaBaseShape, ClippingType clipMode)
        {
            Collection<Feature> clipedFeatures = new Collection<Feature>();
            switch (clipMode)
            {
                case ClippingType.Inverse:
                    foreach (var feature in InverseClip(featureSource, areaBaseShape))
                    {
                        clipedFeatures.Add(feature);
                    }
                    break;

                case ClippingType.Standard:
                default:
                    foreach (var feature in StandardClip(featureSource, areaBaseShape))
                    {
                        clipedFeatures.Add(feature);
                    }
                    break;
            }

            return clipedFeatures;
        }

        private IEnumerable<Feature> InverseClip(FeatureLayer featureLayer, IEnumerable<Feature> clippingFeatures)
        {
            lock (featureLayer)
            {
                if (!featureLayer.IsOpen) featureLayer.Open();
                Collection<Feature> results = featureLayer.FeatureSource.GetFeaturesOutsideBoundingBox(ExtentHelper.GetBoundingBoxOfItems(clippingFeatures), featureLayer.GetDistinctColumnNames());
                Collection<Feature> sourceFeatures = new Collection<Feature>();
                SimpleShapeType simpleShapeType = GisEditor.LayerManager.GetFeatureSimpleShapeType(featureLayer);
                int index = 1;
                if (simpleShapeType == SimpleShapeType.Point)
                {
                    featureLayer.Open();
                    Collection<Feature> allFeatures = featureLayer.FeatureSource.GetAllFeatures(featureLayer.GetDistinctColumnNames());
                    featureLayer.Close();
                    foreach (Feature f in results)
                    {
                        allFeatures.Remove(f);
                    }
                    foreach (var f in InverseClipPoints(allFeatures, clippingFeatures, simpleShapeType))
                    {
                        results.Add(f);
                    }
                }
                else if (simpleShapeType == SimpleShapeType.Line)
                {
                    bool isOpen = false;
                    Proj4ProjectionInfo projectionInfo = featureLayer.GetProj4ProjectionInfo();
                    //MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(clippingFeatures));
                    List<AreaBaseShape> clippingAreaShapes = GetValidFeatures(clippingFeatures)
                        .Select(f => f.GetShape())
                        .OfType<AreaBaseShape>()
                        .ToList();
                    MultipolygonShape areaBaseShape = AreaBaseShape.Union(clippingAreaShapes);

                    if (projectionInfo != null && projectionInfo.CanProject)
                    {
                        if (featureLayer.IsOpen)
                        {
                            featureLayer.Close();
                            projectionInfo.Close();
                            isOpen = true;
                        }
                        featureLayer.FeatureSource.Projection = null;
                    }
                    featureLayer.Open();
                    featureLayer.FeatureSource.GetFeaturesInsideBoundingBox(areaBaseShape.GetBoundingBox(), featureLayer.GetDistinctColumnNames()).ForEach(f => { if (!areaBaseShape.Contains(f)) { sourceFeatures.Add(f); } });
                    int count = sourceFeatures.Count;
                    if (projectionInfo != null)
                    {
                        featureLayer.FeatureSource.Projection = projectionInfo.Projection;
                        if (isOpen) { featureLayer.Open(); }
                    }
                    if (featureLayer.IsOpen) featureLayer.Close();
                    foreach (var feature in sourceFeatures)
                    {
                        isCanceled = ReportProgress(index, count);
                        if (isCanceled) break;

                        index++;
                        try
                        {
                            //if (areaBaseShape.IsDisjointed(feature))
                            if (SqlTypesGeometryHelper.IsDisjointed(areaBaseShape, feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                MultilineShape multiLine = (MultilineShape)feature.GetShape();
                                MultilineShape resultShape = new MultilineShape();
                                foreach (LineShape lineShape in multiLine.Lines)
                                {
                                    //if (areaBaseShape.IsDisjointed(lineShape))
                                    if (SqlTypesGeometryHelper.IsDisjointed(areaBaseShape, lineShape))
                                    {
                                        resultShape.Lines.Add(lineShape);
                                    }
                                    else
                                    {
                                        var resultLine = lineShape.GetDifference(areaBaseShape);
                                        foreach (var line in resultLine.Lines)
                                        {
                                            resultShape.Lines.Add(line);
                                        }
                                    }
                                }
                                if (resultShape != null && resultShape.Lines.Count > 0)
                                    results.Add(new Feature(resultShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                            }
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else if (simpleShapeType == SimpleShapeType.Area)
                {
                    //MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(clippingFeatures));
                    List<AreaBaseShape> clippingAreaShapes = GetValidFeatures(clippingFeatures)
                        .Select(f => f.GetShape())
                        .OfType<AreaBaseShape>()
                        .ToList();

                    BaseShape unionResultShape = SqlTypesGeometryHelper.Union(clippingAreaShapes);
                    MultipolygonShape areaBaseShape = ConvertSqlQueryResultToMultiPolygonShape(unionResultShape);

                    bool isOpen = false;
                    Proj4ProjectionInfo projectionInfo = featureLayer.GetProj4ProjectionInfo();
                    if (projectionInfo != null && projectionInfo.CanProject)
                    {
                        if (featureLayer.IsOpen)
                        {
                            featureLayer.Close();
                            if (projectionInfo != null) projectionInfo.Close();
                            isOpen = true;
                        }
                        featureLayer.FeatureSource.Projection = null;
                    }
                    if (!featureLayer.IsOpen) featureLayer.Open();
                    featureLayer.FeatureSource.GetFeaturesInsideBoundingBox(areaBaseShape.GetBoundingBox(), featureLayer.GetDistinctColumnNames()).ForEach(f => sourceFeatures.Add(f));
                    if (featureLayer.IsOpen) featureLayer.Close();
                    if (projectionInfo != null)
                    {
                        featureLayer.FeatureSource.Projection = projectionInfo.Projection;
                        if (isOpen) { featureLayer.Open(); }
                    }

                    int count = sourceFeatures.Count;
                    foreach (var feature in sourceFeatures)
                    {
                        isCanceled = ReportProgress(index, count);
                        if (isCanceled) break;

                        index++;
                        try
                        {
                            //if (areaBaseShape.IsDisjointed(feature))
                            if (SqlTypesGeometryHelper.IsDisjointed(areaBaseShape, feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                //var clippedShape = ((AreaBaseShape)feature.GetShape()).GetDifference(areaBaseShape);
                                BaseShape differenceResultShape = SqlTypesGeometryHelper.GetDifference((AreaBaseShape)feature.GetShape(), areaBaseShape);
                                MultipolygonShape clippedShape = ConvertSqlQueryResultToMultiPolygonShape(differenceResultShape);
                                if (clippedShape != null && clippedShape.Polygons.Count > 0)
                                {
                                    results.Add(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
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

        private IEnumerable<Feature> InverseClipPoints(IEnumerable<Feature> masterFeatures, IEnumerable<Feature> clippingFeatures, SimpleShapeType simpleShapeType)
        {
            ConcurrentQueue<Feature> results = new ConcurrentQueue<Feature>();
            ConcurrentQueue<Feature> cqMasterFeatures = new ConcurrentQueue<Feature>(masterFeatures);
            int index = 1;
            int count = cqMasterFeatures.Count;
            if (simpleShapeType == SimpleShapeType.Point)
            {
                Parallel.ForEach(cqMasterFeatures, feature =>
                {
                    isCanceled = ReportProgress(index, count);
                    if (isCanceled) return;

                    index++;
                    if (!clippingFeatures.Any(f =>
                    {
                        try
                        {
                            //return f.GetShape().Intersects(feature);
                            return SqlTypesGeometryHelper.Intersects(f.GetShape(), feature);
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
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
                    isCanceled = ReportProgress(index, count);
                    if (isCanceled) return;

                    index++;
                    MultipointShape multiPoints = feature.GetShape() as MultipointShape;
                    if (multiPoints != null)
                    {
                        MultipointShape resultPoints = new MultipointShape();
                        Parallel.ForEach(multiPoints.Points, p =>
                        {
                            if (!clippingFeatures.Any(f =>
                            {
                                try //{ return f.GetShape().Intersects(p); }
                                {
                                    return SqlTypesGeometryHelper.Intersects(f.GetShape(), p);
                                }
                                catch (Exception ex)
                                {
                                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
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

        private IEnumerable<Feature> StandardClip(FeatureLayer featureLayer, IEnumerable<Feature> features)
        {
            lock (featureLayer)
            {
                Collection<Feature> results = new Collection<Feature>();

                #region replace project to null

                bool isOpen = false;
                Proj4ProjectionInfo projectionInfo = featureLayer.GetProj4ProjectionInfo();
                if (projectionInfo != null && projectionInfo.CanProject)
                {
                    if (featureLayer.IsOpen)
                    {
                        featureLayer.Close();
                        projectionInfo.Close();
                        isOpen = true;
                    }
                    featureLayer.FeatureSource.Projection = null;
                }

                #endregion replace project to null

                Collection<Feature> tmpFeatures = new Collection<Feature>();
                if (projectionInfo != null && projectionInfo.CanProject)
                {
                    projectionInfo.Open();
                    foreach (var item in features)
                    {
                        tmpFeatures.Add(projectionInfo.ConvertToInternalProjection(item));
                    }
                    projectionInfo.Close();
                }
                else
                {
                    tmpFeatures = new Collection<Feature>(features.ToList());
                }

                if (!featureLayer.IsOpen) featureLayer.Open();
                List<Feature> tmpSourceFeatures = featureLayer.FeatureSource.GetFeaturesInsideBoundingBox(ExtentHelper.GetBoundingBoxOfItems(tmpFeatures), featureLayer.GetDistinctColumnNames()).Select(f => f.MakeValidIfCan()).ToList();

                Collection<Feature> sourceFeatures = new Collection<Feature>(tmpSourceFeatures);
                if (projectionInfo != null)
                {
                    featureLayer.FeatureSource.Projection = projectionInfo.Projection;
                    if (isOpen)
                    {
                        featureLayer.Open();
                    }
                }

                SimpleShapeType simpleShapeType = SimpleShapeType.Unknown;
                var shapeAdapter = GisEditor.LayerManager.GetLayerPlugins(featureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                if (shapeAdapter != null) simpleShapeType = shapeAdapter.GetFeatureSimpleShapeType(featureLayer);
                if (featureLayer.IsOpen) featureLayer.Close();

                int index = 1;
                int count = sourceFeatures.Count;
                if (simpleShapeType == SimpleShapeType.Point)
                {
                    return StandardClipPoints(sourceFeatures, tmpFeatures);
                }
                else if (simpleShapeType == SimpleShapeType.Line)
                {
                    StandardClipLines(tmpFeatures, results, sourceFeatures, index, count);
                }
                else if (simpleShapeType == SimpleShapeType.Area)
                {
                    //MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(tmpFeatures));
                    List<AreaBaseShape> clippingAreaShapes = GetValidFeatures(tmpFeatures)
                        .Select(f => f.GetShape())
                        .OfType<AreaBaseShape>()
                        .ToList();

                    BaseShape unionResultShape = SqlTypesGeometryHelper.Union(clippingAreaShapes);
                    MultipolygonShape areaBaseShape = ConvertSqlQueryResultToMultiPolygonShape(unionResultShape);

                    foreach (var feature in sourceFeatures)
                    {
                        isCanceled = ReportProgress(index, count);
                        if (isCanceled) break;

                        try
                        {
                            index++;
                            //if (areaBaseShape.Contains(feature))
                            if (SqlTypesGeometryHelper.Contains(areaBaseShape, feature))
                            {
                                results.Add(feature);
                            }
                            else
                            {
                                //var clippedShape = areaBaseShape.GetIntersection(feature);
                                AreaBaseShape targetAreaShape = feature.GetShape() as AreaBaseShape;
                                if (targetAreaShape != null)
                                {
                                    var clippedShape = SqlTypesGeometryHelper.GetIntersection(areaBaseShape, targetAreaShape) as MultipolygonShape;
                                    if (clippedShape != null && clippedShape.Polygons.Count > 0)
                                    {
                                        results.Add(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("The ShapeFileType is not supported.");
                }

                Collection<Feature> convertedFeatures = new Collection<Feature>();
                if (projectionInfo != null && projectionInfo.CanProject)
                {
                    projectionInfo.Open();
                    foreach (var item in results)
                    {
                        convertedFeatures.Add(projectionInfo.ConvertToExternalProjection(item));
                    }
                    projectionInfo.Close();
                }
                else
                {
                    convertedFeatures = new Collection<Feature>(results.ToList());
                }

                return convertedFeatures;
            }
        }

        private static MultipolygonShape ConvertSqlQueryResultToMultiPolygonShape(BaseShape unionResultShape)
        {
            bool isUnionResultPolygon = unionResultShape is PolygonShape;
            bool isUnionResultMultiPolygon = unionResultShape is MultipolygonShape;
            bool isUnionResultGeometryCollectionShape = unionResultShape is GeometryCollectionShape;

            MultipolygonShape concreteUnionResult = new MultipolygonShape();
            if (isUnionResultPolygon) concreteUnionResult.Polygons.Add((PolygonShape)unionResultShape);
            else if (isUnionResultMultiPolygon) concreteUnionResult = (MultipolygonShape)unionResultShape;
            else if (isUnionResultGeometryCollectionShape)
            {
                GeometryCollectionShape geometryCollectionShape = (GeometryCollectionShape)unionResultShape;
                foreach (BaseShape shape in geometryCollectionShape.Shapes)
                {
                    if (shape is PolygonShape)
                    {
                        concreteUnionResult.Polygons.Add((PolygonShape)shape);
                    }
                    else if (shape is MultipolygonShape)
                    {
                        MultipolygonShape multiPolygonShape = (MultipolygonShape)shape;
                        foreach (PolygonShape polygonShape in multiPolygonShape.Polygons)
                        {
                            concreteUnionResult.Polygons.Add(polygonShape);
                        }
                    }
                }
            }

            return concreteUnionResult;
        }

        private int StandardClipLines(IEnumerable<Feature> features, Collection<Feature> results, Collection<Feature> sourceFeatures, int index, int count)
        {
            //MultipolygonShape areaBaseShape = AreaBaseShape.Union(GetValidFeatures(features));
            List<AreaBaseShape> clippingAreaShapes = GetValidFeatures(features).Select(f => f.GetShape()).OfType<AreaBaseShape>().ToList();
            MultipolygonShape areaBaseShape = (MultipolygonShape)SqlTypesGeometryHelper.Union(clippingAreaShapes);

            ConcurrentQueue<Feature> concurrentResult = new ConcurrentQueue<Feature>();
            Parallel.ForEach(sourceFeatures, feature =>
            {
                try
                {
                    //if (areaBaseShape.Contains(feature))
                    if (SqlTypesGeometryHelper.Contains(areaBaseShape, feature))
                    {
                        concurrentResult.Enqueue(feature);
                    }
                    else
                    {
                        //var clippedShape = ((LineBaseShape)feature.GetShape()).GetIntersection(areaBaseShape);
                        var clippedShape = SqlTypesGeometryHelper.GetIntersection(feature.GetShape(), areaBaseShape) as MultilineShape;
                        if (clippedShape != null && clippedShape.Lines.Count > 0)
                        {
                            concurrentResult.Enqueue(new Feature(clippedShape.GetWellKnownBinary(), feature.Id, feature.ColumnValues));
                        }
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    HandleExceptionFromInvalidFeature(feature.Id, ex.Message);
                }
                finally
                {
                    isCanceled = ReportProgress(index++, count);
                }
            });

            foreach (var feature in concurrentResult)
            {
                results.Add(feature);
            }
            return index;
        }

        private IEnumerable<Feature> StandardClipPoints(IEnumerable<Feature> masterFeatures, IEnumerable<Feature> clippingFeatures)
        {
            ConcurrentQueue<Feature> results = new ConcurrentQueue<Feature>();
            int index = 1;
            int count = masterFeatures.Count();
            ConcurrentQueue<Feature> cqMasterFeatures = new ConcurrentQueue<Feature>(masterFeatures);

            if (count > 0)
            {
                var firstFeature = masterFeatures.FirstOrDefault();
                var firstWktType = firstFeature.GetWellKnownType();
                if (firstWktType == WellKnownType.Point)
                {
                    Parallel.ForEach(cqMasterFeatures, feature =>
                    {
                        index++;
                        isCanceled = ReportProgress(index, count);
                        if (isCanceled) return;

                        if (clippingFeatures.Any(f =>
                        {
                            try //{ return f.GetShape().Intersects(feature); }
                            {
                                return SqlTypesGeometryHelper.Intersects(f.GetShape(), feature);
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
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
                        isCanceled = ReportProgress(index, count);
                        if (isCanceled) return;

                        index++;
                        MultipointShape multiPoints = feature.GetShape() as MultipointShape;
                        if (multiPoints != null)
                        {
                            MultipointShape resultPoints = new MultipointShape();
                            Parallel.ForEach(multiPoints.Points, p =>
                            {
                                if (clippingFeatures.Any(f =>
                                {
                                    try //{ return f.GetShape().Intersects(p); }
                                    {
                                        return SqlTypesGeometryHelper.Intersects(f.GetShape(), p);
                                    }
                                    catch (Exception ex)
                                    {
                                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
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
            }
            return results;
        }

        private Collection<Feature> GetValidFeatures(IEnumerable<Feature> features)
        {
            ConcurrentStack<Feature> validFeatures = new ConcurrentStack<Feature>();
            Parallel.ForEach(features, tmpFeature =>
            {
                var validateResult = tmpFeature.GetShape().Validate(ShapeValidationMode.Simple);
                if (validateResult.IsValid)
                {
                    validFeatures.Push(tmpFeature);
                }
                else
                {
                    HandleExceptionFromInvalidFeature(tmpFeature.Id, validateResult.ValidationErrors);
                }
            });
            return new Collection<Feature>(validFeatures.ToList());
        }

        private void HandleExceptionFromInvalidFeature(string featureId, string errorMessage)
        {
            var args = new UpdatingTaskProgressEventArgs(TaskState.Error);
            args.Message = featureId;
            args.Error = new ExceptionInfo(errorMessage, string.Empty, string.Empty);

            OnUpdatingProgress(args);
        }

        private bool ReportProgress(int current, int upperBound)
        {
            var progressPercentage = current * 100 / upperBound;
            var args = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
            args.Current = current;
            args.UpperBound = upperBound;
            OnUpdatingProgress(args);

            return args.TaskState == TaskState.Canceled;
        }
    }
}