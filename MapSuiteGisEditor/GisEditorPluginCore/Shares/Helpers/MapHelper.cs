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
using System.Windows;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class MapHelper
    {
        private const int dotsPerInch = 96;
        private const double feetsPerInch = 12.0;
        private const double meterPerInch = 39.3701;
        private const double decimalDegreePerInch = 4374754;

        public static bool ShowHintWindow(string key)
        {
            bool result = true;
            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                var tmpSettings = Application.Current.MainWindow.Tag as Dictionary<string, string>;
                if (tmpSettings != null && tmpSettings.ContainsKey("ShowHintSettings"))
                {
                    var xml = XDocument.Parse(tmpSettings["ShowHintSettings"]);
                    var targetElement = xml.Root.Element(key);
                    if (targetElement != null)
                    {
                        bool.TryParse(targetElement.Value, out result);
                    }
                    else
                    {
                        result = true;
                    }
                    return result;
                }
            }
            return result;
        }

        public static void SetShowHint(string key, bool result)
        {
            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                var tmpSettings = Application.Current.MainWindow.Tag as Dictionary<string, string>;
                if (tmpSettings.ContainsKey("ShowHintSettings"))
                {
                    var xml = XDocument.Parse(tmpSettings["ShowHintSettings"]);
                    if (xml.Root.HasElements)
                    {
                        var targetElement = xml.Root.Element(key);
                        if (targetElement != null)
                        {
                            targetElement.Value = result.ToString();
                        }
                        else
                        {
                            xml.Root.Add(new XElement(key, result.ToString()));
                        }
                        tmpSettings["ShowHintSettings"] = xml.ToString();
                    }
                    else
                    {
                        var tmpXml = new XElement("ShowHints");
                        tmpXml.Add(new XElement(key, result.ToString()));
                        tmpSettings["ShowHintSettings"] = tmpXml.ToString();
                    }
                }
            }
        }

        public static SimpleShapeType GetSimpleShapeType(WellKnownType wellKnownType)
        {
            switch (wellKnownType)
            {
                case WellKnownType.Point:
                case WellKnownType.Multipoint:
                    return SimpleShapeType.Point;

                case WellKnownType.Line:
                case WellKnownType.Multiline:
                    return SimpleShapeType.Line;

                case WellKnownType.Polygon:
                case WellKnownType.Multipolygon:
                    return SimpleShapeType.Area;

                case WellKnownType.Invalid:
                default:
                    return SimpleShapeType.Unknown;
            }
        }

        private static double GetResolutionFromScale(double scale, GeographyUnit unit)
        {
            return scale / (GetInchesByUnit(unit) * dotsPerInch);
        }

        private static double GetScaleFromResolution(double resolution, GeographyUnit unit)
        {
            return resolution * (GetInchesByUnit(unit) * dotsPerInch);
        }

        public static RectangleShape CalculateExtent(PointShape center, double scale, GeographyUnit mapUnit, double mapWidth, double mapHeight)
        {
            return CalculateExtent(new Point(center.X, center.Y), scale, mapUnit, mapWidth, mapHeight);
        }

        private static RectangleShape CalculateExtent(Point center, double scale, GeographyUnit mapUnit, double mapWidth, double mapHeight)
        {
            if (Double.IsNaN(mapWidth) || Double.IsNaN(mapHeight))
            {
                return null;
            }

            double resolution = GetResolutionFromScale(scale, mapUnit);
            double widthInDegree = mapWidth * resolution;
            double heightInDegree = mapHeight * resolution;
            double left = center.X - widthInDegree * .5;
            double right = center.X + widthInDegree * .5;
            double top = center.Y + heightInDegree * .5;
            double bottom = center.Y - heightInDegree * .5;
            return new RectangleShape(left, top, right, bottom);
        }

        private static double GetResolution(RectangleShape boundingBox, double widthInPixel, double heightInPixel)
        {
            return Math.Max(boundingBox.Width / widthInPixel, boundingBox.Height / heightInPixel);
        }

        private static double GetScale(GeographyUnit mapUnit, RectangleShape boundingBox, double widthInPixel, double heightInPixel)
        {
            double resolution = GetResolution(boundingBox, widthInPixel, heightInPixel);
            return GetScaleFromResolution(resolution, mapUnit);
        }

        private static double GetInchesByUnit(GeographyUnit unit)
        {
            switch (unit)
            {
                case GeographyUnit.Feet: return feetsPerInch;
                case GeographyUnit.Meter: return meterPerInch;
                case GeographyUnit.DecimalDegree: return decimalDegreePerInch;
                default: return double.NaN;
            }
        }

        internal static string GetBBoxString(RectangleShape rectangle)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", rectangle.LowerLeftPoint.X, rectangle.LowerLeftPoint.Y, rectangle.UpperRightPoint.X, rectangle.UpperRightPoint.Y);
        }

        internal static bool IsFuzzyEqual(double source, double target)
        {
            return Math.Round(source, 6) == Math.Round(target, 6);
        }

        internal static int GetSnappedZoomLevelIndex(RectangleShape extent, GeographyUnit mapUnit, Collection<double> zoomLevelScales, double actualWidth, double actualHeight)
        {
            double scale = GetScale(mapUnit, extent, actualWidth, actualHeight);
            return GetSnappedZoomLevelIndex(scale, zoomLevelScales);
        }

        internal static int GetSnappedZoomLevelIndex(double scale, Collection<double> zoomLevelScales)
        {
            return GetSnappedZoomLevelIndex(scale, zoomLevelScales, double.MinValue, double.MaxValue);
        }

        internal static int GetSnappedZoomLevelIndex(double scale, Collection<double> zoomLevelScales, double minimumScale, double maximumScale)
        {
            if (scale < minimumScale)
            {
                scale = minimumScale;
            }
            else if (scale > maximumScale)
            {
                scale = maximumScale;
            }

            int zoomLevel = -1;

            if (zoomLevelScales.Count > 0)
            {
                foreach (double tempScale in zoomLevelScales)
                {
                    if (tempScale >= scale || Math.Abs(tempScale - scale) < 0.1)
                    {
                        zoomLevel++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (zoomLevel >= zoomLevelScales.Count)
            {
                zoomLevel = zoomLevelScales.Count - 1;
            }

            return zoomLevel == -1 ? 0 : zoomLevel;
        }
    }
}