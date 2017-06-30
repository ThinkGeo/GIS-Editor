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
using System.Linq;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class GeoProcessHelper
    {
        public static Feature IntersectFeatures(IEnumerable<Feature> features)
        {
            try
            {
                Feature baseFeature = null;
                if (features.Count() > 0)
                {
                    baseFeature = features.FirstOrDefault();
                    foreach (var feature in features.Skip(1).ToArray())
                    {
                        baseFeature = baseFeature.MakeValidUsingSqlTypes();
                        var feature1 = feature.MakeValidUsingSqlTypes();
                        baseFeature = baseFeature.GetIntersection(feature1);
                        if (baseFeature == null) break;
                    }
                }

                return baseFeature;
            }

            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return null;
            }
        }

        public static AreaBaseShape[] SubtractAreas(IEnumerable<Feature> features)
        {
            AreaBaseShape[] polygons = features.Select(f => f.MakeValidUsingSqlTypes().GetShape())
                .OfType<AreaBaseShape>()
                .Select(s => s.ToPolygonOrMultiPolygon()).ToArray();

            for (int i = 0; i < polygons.Length; i++)
            {
                AreaBaseShape firstLoopShape = polygons[i];
                for (int j = i + 1; j < polygons.Length; j++)
                {
                    AreaBaseShape secondLoopShape = polygons[j];
                    var tempSharp = firstLoopShape.GetDifference(secondLoopShape);
                    if (tempSharp != null)
                        firstLoopShape = tempSharp;
                }
                if (firstLoopShape != null)
                    polygons[i] = firstLoopShape;
            }

            return polygons;
        }

        public static Feature UnionAreaFeatures(IEnumerable<Feature> features)
        {
            Collection<Feature> tempFeatuers = new Collection<Feature>();
            foreach (var item in features)
            {
                if (!SqlTypesGeometryHelper.IsValid(item))
                {
                    tempFeatuers.Add(SqlTypesGeometryHelper.MakeValid(item));
                }
                else
                {
                    tempFeatuers.Add(item);
                }
            }

            var unionresult = AreaBaseShape.Union(tempFeatuers);
            return new Feature(unionresult);
        }

        public static Feature CombineFeatures(IEnumerable<Feature> featuresToBeCombined)
        {
            Feature[] features = featuresToBeCombined.ToArray();

            Feature finalResult = default(Feature);

            if (features.Length > 0)
            {
                Feature firstFeature = features[0];
                Type type = firstFeature.GetShape().GetType();

                if (type.IsSubclassOf(typeof(AreaBaseShape)))
                {
                    finalResult = CombineAreaShapes(features);
                }
                else if (type.IsSubclassOf(typeof(LineBaseShape)))
                {
                    finalResult = CombineLineShapes(features);
                }
                else if (type.IsSubclassOf(typeof(MultipointShape)) || type == typeof(MultipointShape))
                {
                    finalResult = new Feature(new MultipointShape(features.SelectMany(f => ((MultipointShape)f.GetShape()).Points)));
                }
            }

            return finalResult;
        }

        private static Feature CombineLineShapes(Feature[] features)
        {
            var finalResult = new Feature(new MultilineShape(features.SelectMany<Feature, LineShape>(f =>
            {
                var shape = f.GetShape();
                if (shape is LineShape)
                {
                    return new LineShape[] { (LineShape)shape };
                }
                else if (shape is MultilineShape)
                {
                    return ((MultilineShape)shape).Lines;
                }
                return null;
            })));

            return finalResult;
        }

        private static Feature CombineAreaShapes(Feature[] features)
        {
            MultipolygonShape result = new MultipolygonShape();
            foreach (var feature in features)
            {
                PolygonShape polygon = feature.GetShape() as PolygonShape;
                MultipolygonShape multipolygon = feature.GetShape() as MultipolygonShape;
                if (polygon != null)
                {
                    result.Polygons.Add(polygon);
                }
                else if (multipolygon != null)
                {
                    foreach (var item in multipolygon.Polygons)
                    {
                        result.Polygons.Add(item);
                    }
                }
            }

            return new Feature(result);
        }

        private static IEnumerable<PolygonShape> ToPolygons(this AreaBaseShape area)
        {
            Collection<PolygonShape> results = null;

            PolygonShape polygon = area as PolygonShape;
            MultipolygonShape multipolygonShape = area as MultipolygonShape;

            if (polygon != null)
            {
                results = new Collection<PolygonShape> { polygon };
            }
            else if (multipolygonShape != null)
            {
                results = multipolygonShape.Polygons;
            }
            else
            {
                var polygonShape = area.GetType().GetMethod("ToPolygon").Invoke(null, null) as PolygonShape;
                results = new Collection<PolygonShape> { polygonShape };
            }

            return results;
        }

        private static AreaBaseShape ToPolygonOrMultiPolygon(this AreaBaseShape area)
        {
            AreaBaseShape result = null;

            PolygonShape polygon = area as PolygonShape;
            MultipolygonShape multipolygonShape = area as MultipolygonShape;

            if (polygon != null)
            {
                result = polygon;
            }
            else if (multipolygonShape != null)
            {
                result = multipolygonShape;
            }
            else
            {
                result = area.GetType().GetMethod("ToPolygon").Invoke(null, null) as PolygonShape;
            }

            return result;
        }
    }
}