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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class BasEntityToFeatureEntityConverter
    {
        public static BasFeatureEntity Convert(BasEntity entity)
        {
            BasFeatureEntity basFeature = new BasFeatureEntity();
            basFeature.Offset = entity.Offset;
            ReadColumns(basFeature, entity.HeaderResult);
            ReadGeometry(basFeature, entity.CoordinatesResult);
            ReadAnnocation(basFeature, entity.AnnotationsResult);
            return basFeature;
        }

        private static void ReadColumns(BasFeatureEntity featureEntity, RecordHeader header)
        {
            foreach (KeyValuePair<string, string> item in header.HeaderColumns)
            {
                featureEntity.Columns.Add(item.Key, item.Value);
            }
        }

        private static void ReadGeometry(BasFeatureEntity featureEntiye, Collection<RecordCoordinate> coordinates)
        {
            if (coordinates.Count > 0)
            {
                //MultilineShape multilineShape = new MultilineShape();
                //foreach (var coord in coordinates)
                //{
                //    Vertex vertex1 = ConvertHelper.LatLonToVertex(coord.StartPointLonLat);
                //    Vertex vertex2 = ConvertHelper.LatLonToVertex(coord.EndPointLonLat);
                //    multilineShape.Lines.Add(new LineShape(new Collection<Vertex>() { vertex1, vertex2 }));
                //}
                //ShapeValidationResult validateResult = multilineShape.Validate(ShapeValidationMode.Simple);
                //if (!validateResult.IsValid)
                //{
                //    throw new ArgumentException();
                //}
                MultipolygonShape multiPolygonShape = new MultipolygonShape();

                PolygonShape tempPolygon = new PolygonShape();
                foreach (var coord in coordinates)
                {
                    Vertex vertex1 = ConvertHelper.LatLonToVertex(coord.StartPointLonLat);
                    Vertex vertex2 = ConvertHelper.LatLonToVertex(coord.EndPointLonLat);
                    tempPolygon.OuterRing.Vertices.Add(vertex1);
                    tempPolygon.OuterRing.Vertices.Add(vertex2);

                    if (coord.EndOfPolygonFlag)
                    {
                        multiPolygonShape.Polygons.Add(tempPolygon);
                        tempPolygon = new PolygonShape();
                    }
                }
                ShapeValidationResult validateResult = multiPolygonShape.Validate(ShapeValidationMode.Simple);
                if (!validateResult.IsValid)
                {
                    throw new ArgumentException();
                }
                featureEntiye.Shape = multiPolygonShape;
            }
        }

        private static void ReadAnnocation(BasFeatureEntity featureEntity, Collection<RecordAnnotation> annotations)
        {
            foreach (var item in annotations)
            {
                BasAnnotation basAnnotition = new BasAnnotation();
                basAnnotition.TextString = item.Text;
                basAnnotition.Position = new PointShape(ConvertHelper.LatLonToVertex(item.TextLocationLonlat));

                AnnotationFontStyle fontStyle = new AnnotationFontStyle();
                fontStyle.TextAngle = ConvertHelper.TextAngleToAngle(item.TextAngle);
                fontStyle.TextSize = float.Parse(item.TextSize);
                fontStyle.TextFont = int.Parse(item.TextFont);
                basAnnotition.FontStyle = fontStyle;

                featureEntity.Annotations.Add(basAnnotition);
            }
        }
    }
}