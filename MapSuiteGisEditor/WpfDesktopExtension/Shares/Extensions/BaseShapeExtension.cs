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


using Microsoft.SqlServer.Types;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class BaseShapeExtension
    {
        public static Collection<AreaBaseShape> GetSplitResult(this AreaBaseShape polygon, Collection<LineShape> lines)
        {
            SqlGeometry polygonGeom = SqlGeometry.STGeomFromWKB(new SqlBytes(polygon.GetWellKnownBinary()), 0);
            if (!polygonGeom.STIsValid()) polygonGeom = polygonGeom.MakeValid();
            //GeometryLibrary library = BaseShape.GeometryLibrary;
            //BaseShape.GeometryLibrary = GeometryLibrary.Unmanaged;
            foreach (var item in lines)
            {
                //MultipolygonShape multipolygon = item.Buffer(0.2, 8, BufferCapType.Square, GeographyUnit.DecimalDegree, DistanceUnit.Meter);
                MultipolygonShape multipolygon = SqlTypesGeometryHelper.Buffer(item, 0.2, 8, BufferCapType.Square, GeographyUnit.DecimalDegree, DistanceUnit.Meter);
                SqlGeometry multipolygonGeom = SqlGeometry.STGeomFromWKB(new SqlBytes(multipolygon.GetWellKnownBinary()), 0);
                if (!multipolygonGeom.STIsValid()) multipolygonGeom = multipolygonGeom.MakeValid();

                polygonGeom = polygonGeom.STDifference(multipolygonGeom);
            }

            //BaseShape.GeometryLibrary = library;

            byte[] bytes = polygonGeom.STAsBinary().Value;
            BaseShape shape = BaseShape.CreateShapeFromWellKnownData(bytes) as AreaBaseShape;

            MultipolygonShape splittedMultipolygonShape = shape as MultipolygonShape;
            PolygonShape splittedPolygonShape = shape as PolygonShape;
            if (splittedMultipolygonShape == null && splittedPolygonShape != null)
            {
                splittedMultipolygonShape = new MultipolygonShape();
                splittedMultipolygonShape.Polygons.Add(splittedPolygonShape);
                shape = splittedMultipolygonShape;
            }

            Collection<AreaBaseShape> shapes = new Collection<AreaBaseShape>();
            if (splittedMultipolygonShape != null)
            {
                CloseSplittedPolygons(polygon, splittedMultipolygonShape, lines[0], GeographyUnit.DecimalDegree, DistanceUnit.Meter, .3);
                FillShapes(shapes, shape);
            }
            return shapes;
        }

        public static void CloseSplittedPolygons(AreaBaseShape sourcePolygon, MultipolygonShape splitedPolygon, LineShape splitLine, GeographyUnit mapUnit, DistanceUnit distanceUnit, double distance)
        {
            var multiline = splitLine.GetIntersection(sourcePolygon);
            foreach (var vertex in multiline.Lines.SelectMany(l => l.Vertices))
            {
                var closeArea = new PointShape(vertex).Buffer(distance, mapUnit, distanceUnit).GetBoundingBox();
                foreach (var polygon in splitedPolygon.Polygons)
                {
                    CloseSplittedRing(vertex, closeArea, polygon.OuterRing);
                    foreach (var ring in polygon.InnerRings)
                    {
                        CloseSplittedRing(vertex, closeArea, ring);
                    }
                }
            }
        }

        private static void CloseSplittedRing(Vertex vertex, RectangleShape closeArea, RingShape ring)
        {
            for (int i = 0; i < ring.Vertices.Count; i++)
            {
                var currentVertex = ring.Vertices[i];
                if (closeArea.Contains(new PointShape(currentVertex)))
                {
                    ring.Vertices[i] = new Vertex(vertex.X, vertex.Y);
                }
            }
        }

        private static void FillShapes(Collection<AreaBaseShape> shapes, BaseShape shape)
        {
            Feature feature = new Feature(shape);
            feature.MakeValidUsingSqlTypes();
            shape = feature.GetShape();

            if (shape is PolygonShape)
            {
                shapes.Add((PolygonShape)shape);
            }
            else if (shape is MultipolygonShape)
            {
                foreach (var item in ((MultipolygonShape)shape).Polygons)
                {
                    shapes.Add(item);
                }
            }
            else if (shape is GeometryCollectionShape)
            {
                foreach (var item in (shape as GeometryCollectionShape).Shapes)
                {
                    FillShapes(shapes, item);
                }
            }
        }
    }
}
