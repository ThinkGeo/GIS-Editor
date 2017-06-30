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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// This class contains extension method(s) for sequences of features.
    /// </summary>
    public static class FeaturesExtension
    {
        /// <summary>
        /// Get all the features that will be drawn at the current zoom level.
        /// This method filter the input feature by using the containing layer's styles, .
        /// </summary>
        /// <param name="features">The input features.</param>
        /// <param name="containingLayer">The FeatureLayer that contains the features.</param>
        /// <returns>The features that are visible at current zoom level.</returns>
        public static IEnumerable<Feature> GetVisibleFeatures(this IEnumerable<Feature> features, ZoomLevelSet zoomLevelSet, RectangleShape boundingBox, double screenWidth, GeographyUnit mapUnit)
        {
            //ClassBreakStyle
            //ValueStyle
            ZoomLevel currentDrawingZoomLevel = zoomLevelSet.GetZoomLevelForDrawing(boundingBox, screenWidth, mapUnit);
            if (currentDrawingZoomLevel != null)
            {
                //if there are any default styles, then the features are visible
                //if there are any regular styles in the custom styles collection, then the features are visible
                if (ContainsCustomStyles(currentDrawingZoomLevel) || ContainsDefaultStyles(currentDrawingZoomLevel))
                {
                    foreach (var feature in features)
                    {
                        yield return feature;
                    }
                }
                //if there are no default styles and there are no regular styles in the custom styles collection, then we need
                //to check every style in the custom styles collection to determine if a feature is visible
                else
                {
                    var valueStyles = currentDrawingZoomLevel.CustomStyles.OfType<CompositeStyle>().SelectMany(c => c.Styles).OfType<ValueStyle>();
                    var classBreakStyles = currentDrawingZoomLevel.CustomStyles.OfType<CompositeStyle>().SelectMany(c => c.Styles).OfType<ClassBreakStyle>();
                    var filterStyles = currentDrawingZoomLevel.CustomStyles.OfType<CompositeStyle>().SelectMany(c => c.Styles).OfType<FilterStyle>();

                    foreach (var feature in features)
                    {
                        bool isFeatureValidForValueStyle = valueStyles.Any(valueStyle => ValidateFeature(feature, valueStyle));
                        bool isFeatureValidForClassBreakStyle = classBreakStyles.Any(classBreakStyle => ValidateFeature(feature, classBreakStyle));
                        bool isFeatureValidForFilterStyles = filterStyles.Any(filterStyle => ValidateFeature(feature, filterStyle));

                        if (isFeatureValidForValueStyle || isFeatureValidForClassBreakStyle || isFeatureValidForFilterStyles)
                        {
                            yield return feature;
                        }
                    }
                }
            }
        }

        public static Feature MakeValidUsingSqlTypes(this Feature feature)
        {
            var wellKnownType = feature.GetWellKnownType();
            SqlGeometry sqlGeometry = null;
            try
            {
                sqlGeometry = SqlGeometry.STGeomFromWKB(new SqlBytes(feature.GetWellKnownBinary()), 0);
            }
            catch
            {
                sqlGeometry = SqlGeometry.STGeomFromText(new SqlChars(feature.GetWellKnownText()), 0);
            }


            byte[] wkb = sqlGeometry.MakeValid().STAsBinary().Value;

            var result = new Feature(wkb, feature.Id, feature.ColumnValues);
            var resultWellKnownType = result.GetWellKnownType();

            if (wellKnownType != resultWellKnownType
                && resultWellKnownType == WellKnownType.GeometryCollection)
            {
                if (wellKnownType == WellKnownType.Polygon || wellKnownType == WellKnownType.Multipolygon)
                {
                    result = GetMultiPolygonFromCollection(result);
                }
                else if (wellKnownType == WellKnownType.Line || wellKnownType == WellKnownType.Multiline)
                {
                    result = GetMultiLineFromCollection(result);
                }
            }
            return result;
        }

        private static Feature GetMultiLineFromCollection(Feature feature)
        {
            var shapeColleciton = (GeometryCollectionShape)feature.GetShape();
            var multiline = new MultilineShape();

            foreach (var shape in shapeColleciton.Shapes)
            {
                var shapeWellKnownType = shape.GetWellKnownType();

                if (shapeWellKnownType == WellKnownType.Polygon)
                {
                    multiline.Lines.Add(shape as LineShape);
                }
                else if (shapeWellKnownType == WellKnownType.Multipolygon)
                {
                    foreach (var polygon in ((MultilineShape)shape).Lines)
                    {
                        multiline.Lines.Add(polygon);
                    }
                }
            }

            return new Feature(multiline.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
        }

        public static MultilineShape GetDifference(this LineShape masterLine, AreaBaseShape clippingArea)
        {
            MultilineShape resultMultiLineShape = new MultilineShape();
            var masterGeoLine = SqlGeometry.STGeomFromWKB(new SqlBytes(masterLine.GetWellKnownBinary()), 0);
            var clippingGeoArea = SqlGeometry.STGeomFromWKB(new SqlBytes(clippingArea.GetWellKnownBinary()), 0);
            var resultWkb = masterGeoLine.STSymDifference(clippingGeoArea).MakeValid().STAsBinary().Value;
            var resultFeature = new Feature(resultWkb);
            var resultShape = resultFeature.GetShape();
            if (resultShape is GeometryCollectionShape)
            {
                foreach (var line in ((GeometryCollectionShape)resultFeature.GetShape()).Shapes.OfType<LineShape>())
                {
                    resultMultiLineShape.Lines.Add(line);
                }
            }
            else if (resultShape is MultilineShape)
            {
                foreach (var item in ((MultilineShape)resultShape).Lines)
                {
                    resultMultiLineShape.Lines.Add(item);
                }
            }
            else if (resultShape is LineShape)
            {
                resultMultiLineShape.Lines.Add((LineShape)resultShape);
            }

            return resultMultiLineShape;
        }

        private static Feature GetMultiPolygonFromCollection(Feature feature)
        {
            var shapeColleciton = (GeometryCollectionShape)feature.GetShape();
            var multipolygon = new MultipolygonShape();

            foreach (var shape in shapeColleciton.Shapes)
            {
                var shapeWellKnownType = shape.GetWellKnownType();

                if (shapeWellKnownType == WellKnownType.Polygon)
                {
                    multipolygon.Polygons.Add(shape as PolygonShape);
                }
                else if (shapeWellKnownType == WellKnownType.Multipolygon)
                {
                    foreach (var polygon in ((MultipolygonShape)shape).Polygons)
                    {
                        multipolygon.Polygons.Add(polygon);
                    }
                }
            }

            return new Feature(multipolygon.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
        }

        private static bool ValidateFeature(Feature feature, ClassBreakStyle classBreakStyle)
        {
            double fieldValue = double.NaN;
            bool success = double.TryParse(feature.ColumnValues[classBreakStyle.ColumnName].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out fieldValue);

            if (success)
            {
                Collection<ClassBreak> sortedClassBreaks = GetSortedClassBreak(classBreakStyle.ClassBreaks);
                ClassBreak classBreak = GetClassBreak(fieldValue, classBreakStyle.BreakValueInclusion, sortedClassBreaks);

                return classBreak != null;
            }

            return false;
        }

        private static bool ValidateFeature(Feature feature, FilterStyle filterStyle)
        {
            return filterStyle.Conditions.FirstOrDefault().GetMatchingFeatures(new Feature[] { feature }).Count > 0;
        }

        private static bool ValidateFeature(Feature feature, ValueStyle valueStyle)
        {
            string fieldValue = feature.ColumnValues[valueStyle.ColumnName].Trim();
            return valueStyle.ValueItems.Any(valueItem => string.Compare(fieldValue, valueItem.Value, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private static bool ContainsDefaultStyles(ZoomLevel currentDrawingZoomLevel)
        {
            return currentDrawingZoomLevel.CustomStyles.Count == 0 && (currentDrawingZoomLevel.DefaultAreaStyle != null || currentDrawingZoomLevel.DefaultLineStyle != null || currentDrawingZoomLevel.DefaultPointStyle != null) && currentDrawingZoomLevel.CustomStyles.Any(s => s.IsActive);
        }

        private static bool ContainsCustomStyles(ZoomLevel currentDrawingZoomLevel)
        {
            var styles = currentDrawingZoomLevel.CustomStyles.OfType<CompositeStyle>().Where(s => s.IsActive).SelectMany(c => c.Styles);
            return styles.OfType<AreaStyle>().Any(s => s.IsActive)
                || styles.OfType<LineStyle>().Any(s => s.IsActive)
                || styles.OfType<PointStyle>().Any(s => s.IsActive)
                || styles.OfType<NoaaWeatherStationStyle>().Any(s => s.IsActive);
        }

        private static Collection<ClassBreak> GetSortedClassBreak(Collection<ClassBreak> classBreaks)
        {
            List<double> breaks = new List<double>();
            Dictionary<double, ClassBreak> unsortedClassBreaks = new Dictionary<double, ClassBreak>();
            foreach (ClassBreak classBreak in classBreaks)
            {
                breaks.Add(classBreak.Value);
                unsortedClassBreaks.Add(classBreak.Value, classBreak);
            }

            breaks.Sort();

            Collection<ClassBreak> sortedClassBreaks = new Collection<ClassBreak>();

            for (int i = 0; i < breaks.Count; i++)
            {
                sortedClassBreaks.Add(unsortedClassBreaks[breaks[i]]);
            }

            return sortedClassBreaks;
        }

        private static ClassBreak GetClassBreak(double columnValue, BreakValueInclusion breakValueInclusion, Collection<ClassBreak> sortedClassBreaks)
        {
            ClassBreak result = sortedClassBreaks[sortedClassBreaks.Count - 1];
            if (breakValueInclusion == BreakValueInclusion.IncludeValue)
            {
                if (columnValue <= sortedClassBreaks[0].Value)
                {
                    return null;
                }

                for (int i = 1; i < sortedClassBreaks.Count; i++)
                {
                    if (columnValue < sortedClassBreaks[i].Value)
                    {
                        result = sortedClassBreaks[i - 1];
                        break;
                    }
                }
            }
            else
            {
                if (columnValue < sortedClassBreaks[0].Value)
                {
                    return null;
                }

                for (int i = 1; i < sortedClassBreaks.Count; i++)
                {
                    if (columnValue <= sortedClassBreaks[i].Value)
                    {
                        result = sortedClassBreaks[i - 1];
                        break;
                    }
                }
            }

            return result;
        }
    }
}