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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class PeakStyle : Style
    {
        protected override void DrawCore(IEnumerable<Feature> features, GeoCanvas canvas, System.Collections.ObjectModel.Collection<SimpleCandidate> labelsInThisLayer, System.Collections.ObjectModel.Collection<SimpleCandidate> labelsInAllLayers)
        {
            foreach (var feature in features)
            {
                BaseShape shape = feature.GetShape();
                if (shape != null)
                {
                    foreach (var point in CollectPoints(shape))
                    {
                        canvas.DrawEllipse(point, 6f, 6f, new GeoPen(GeoColor.StandardColors.Gray, 1), new GeoSolidBrush(GeoColor.StandardColors.White), DrawingLevel.LevelThree);
                    }
                }
            }
        }

        private static IEnumerable<PointShape> CollectPoints(BaseShape shape)
        {
            WellKnownType wktype = shape.GetWellKnownType();
            if (wktype == WellKnownType.Point)
                yield return (PointShape)shape;
            else if (wktype == WellKnownType.Multipoint)
                foreach (var point in ((MultipointShape)shape).Points)
                {
                    yield return point;
                }
            else if (wktype == WellKnownType.Line)
                foreach (var point in CollectPoints((LineShape)shape))
                {
                    yield return point;
                }
            else if (wktype == WellKnownType.Multiline)
                foreach (var point in CollectPoints((MultilineShape)shape))
                {
                    yield return point;
                }
            else if (wktype == WellKnownType.Polygon)
                foreach (var point in CollectPoints((PolygonShape)shape))
                {
                    yield return point;
                }
            else if (wktype == WellKnownType.Multipolygon)
            {
                foreach (var point in CollectPoints((MultipolygonShape)shape))
                {
                    yield return point;
                }
            }
        }

        private static IEnumerable<PointShape> CollectPoints(LineShape line)
        {
            foreach (var vertex in line.Vertices)
            {
                yield return new PointShape(vertex);
            }
        }

        private static IEnumerable<PointShape> CollectPoints(MultilineShape mline)
        {
            foreach (var line in mline.Lines)
            {
                foreach (var point in CollectPoints(line))
                {
                    yield return point;
                }
            }
        }

        private static IEnumerable<PointShape> CollectPoints(RingShape ring)
        {
            foreach (var vertex in ring.Vertices)
            {
                yield return new PointShape(vertex);
            }
        }

        public static IEnumerable<PointShape> CollectPoints(PolygonShape polygon)
        {
            foreach (var point in CollectPoints(polygon.OuterRing))
            {
                yield return point;
            }

            foreach (var ring in polygon.InnerRings)
            {
                foreach (var point in CollectPoints(ring))
                {
                    yield return point;
                }
            }
        }

        public static IEnumerable<PointShape> CollectPoints(MultipolygonShape multipolygon)
        {
            foreach (var polygon in multipolygon.Polygons)
            {
                foreach (var point in CollectPoints(polygon))
                {
                    yield return point;
                }
            }
        }
    }
}