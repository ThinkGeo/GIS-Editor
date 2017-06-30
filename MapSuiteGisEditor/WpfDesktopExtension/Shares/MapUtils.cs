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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class MapUtils
    {
        private const int dotsPerInch = 96;
        public const double Feet = 12.0;
        public const double Meter = 39.3701;
        public const double DecimalDegree = 4374754;

        public static double GetResolutionFromScale(double scale, GeographyUnit unit)
        {
            return scale / (GetInchesByUnit(unit) * dotsPerInch);
        }

        public static double GetScaleFromResolution(double resolution, GeographyUnit unit)
        {
            return resolution * (GetInchesByUnit(unit) * dotsPerInch);
        }

        public static RectangleShape CalculateExtent(PointShape worldCenter, double scale, GeographyUnit mapUnit, double mapWidth, double mapHeight)
        {
            return CalculateExtent(worldCenter.X, worldCenter.Y, scale, mapUnit, mapWidth, mapHeight);
        }

        public static RectangleShape CalculateExtent(Point worldCenter, double scale, GeographyUnit mapUnit, double mapWidth, double mapHeight)
        {
            return CalculateExtent(worldCenter.X, worldCenter.Y, scale, mapUnit, mapWidth, mapHeight);
        }

        public static RectangleShape CalculateExtent(double worldCenterX, double worldCenterY, double scale, GeographyUnit mapUnit, double mapWidth, double mapHeight)
        {
            if (Double.IsNaN(mapWidth) || Double.IsNaN(mapHeight))
            {
                return null;
            }

            double resolution = GetResolutionFromScale(scale, mapUnit);
            double worldWidth = mapWidth * resolution;
            double worldHeight = mapHeight * resolution;
            double left = worldCenterX - worldWidth * .5;
            double right = worldCenterX + worldWidth * .5;
            double top = worldCenterY + worldHeight * .5;
            double bottom = worldCenterY - worldHeight * .5;
            return new RectangleShape(left, top, right, bottom);
        }

        public static RectangleShape GetDefaultMaxExtent(GeographyUnit mapUnit)
        {
            RectangleShape maxExtent = new RectangleShape();
            switch (mapUnit)
            {
                case GeographyUnit.DecimalDegree:
                    maxExtent = new RectangleShape(-180, 90, 180, -90);
                    break;
                case GeographyUnit.Meter:
                    BitmapTileCache meterCache = new FileBitmapTileCache();
                    meterCache.TileMatrix.BoundingBoxUnit = GeographyUnit.Meter;
                    meterCache.TileMatrix.BoundingBox = new RectangleShape(-1000000000, 1000000000, 1000000000, -1000000000);
                    maxExtent = meterCache.TileMatrix.BoundingBox;
                    break;
                case GeographyUnit.Feet:
                    BitmapTileCache feetCache = new FileBitmapTileCache();
                    feetCache.TileMatrix.BoundingBoxUnit = GeographyUnit.Feet;
                    feetCache.TileMatrix.BoundingBox = new RectangleShape(-1000000000, 1000000000, 1000000000, -1000000000);
                    maxExtent = feetCache.TileMatrix.BoundingBox;
                    break;
                default:
                    break;
            }
            return maxExtent;
        }

        public static double GetResolution(RectangleShape boundingBox, double widthInPixel, double heightInPixel)
        {
            return Math.Max(boundingBox.Width / widthInPixel, boundingBox.Height / heightInPixel);
        }

        public static double GetScale(GeographyUnit mapUnit, RectangleShape boundingBox, double widthInPixel, double heightInPixel)
        {
            double resolution = GetResolution(boundingBox, widthInPixel, heightInPixel);
            return GetScaleFromResolution(resolution, mapUnit);
        }

        public static Point ToScreenCoordinate(RectangleShape currentExtent, double worldX, double worldY, double actualWidth, double actualHeight)
        {
            double widthFactor = actualWidth / currentExtent.Width;
            double heighFactor = actualHeight / currentExtent.Height;

            double pointX = (worldX - currentExtent.UpperLeftPoint.X) * widthFactor;
            double pointY = (currentExtent.UpperLeftPoint.Y - worldY) * heighFactor;

            return new Point(pointX, pointY);
        }

        public static Point ToWorldCoordinate(RectangleShape currentExtent, double screenX, double screenY, double screenWidth, double screenHeight)
        {
            double widthFactor = currentExtent.Width / screenWidth;
            double heightFactor = currentExtent.Height / screenHeight;

            double pointX = currentExtent.UpperLeftPoint.X + screenX * widthFactor;
            double pointY = currentExtent.UpperLeftPoint.Y - screenY * heightFactor;

            return new Point(pointX, pointY);
        }

        public static double GetInchesByUnit(GeographyUnit unit)
        {
            switch (unit)
            {
                case GeographyUnit.Feet: return Feet;
                case GeographyUnit.Meter: return Meter;
                case GeographyUnit.DecimalDegree: return DecimalDegree;
                default: return double.NaN;
            }
        }

        public static double GetDistance(PointShape fromPoint, PointShape toPoint)
        {
            double horizenDistance = Math.Abs((fromPoint.X - toPoint.X));
            double verticalDistance = Math.Abs((fromPoint.Y - toPoint.Y));

            double result = Math.Sqrt(Math.Pow(horizenDistance, 2) + Math.Pow(verticalDistance, 2));

            return result;
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

        internal static int GetSnappedZoomLevelIndex(double scale, ZoomLevelSet zoomLevelSet)
        {
            int zoomLevel = -1;

            foreach (ZoomLevel item in zoomLevelSet.GetZoomLevels())
            {
                if (item.Scale >= scale || Math.Abs(item.Scale - scale) < 0.1)
                {
                    zoomLevel++;
                }
                else
                {
                    break;
                }
            }

            return zoomLevel == -1 ? 0 : zoomLevel;
        }

        internal static double GetClosestScale(double scale, ZoomLevelSet zoomLevelSet)
        {
            double closestScale = double.NaN;
            double closestScaleDiff = Double.MaxValue;

            foreach (ZoomLevel item in zoomLevelSet.GetZoomLevels())
            {
                double currentScaleDiff = Math.Abs(item.Scale - scale);
                if (currentScaleDiff < closestScaleDiff)
                {
                    closestScaleDiff = currentScaleDiff;
                    closestScale = item.Scale;
                }
            }

            return closestScale;
        }

        internal static string GetTemporaryFolder()
        {
            string returnValue = string.Empty;
            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable("Temp");
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = Environment.GetEnvironmentVariable("Tmp");
            }

            if (string.IsNullOrEmpty(returnValue))
            {
                returnValue = @"c:\MapSuiteTemp";
            }
            else
            {
                returnValue = returnValue + "\\" + "MapSuite";
            }

            return returnValue;
        }

        internal static object GetImageSourceFromNativeImage(object nativeImage)
        {
            object imageSource = nativeImage;
            if (nativeImage is System.Drawing.Bitmap)
            {
                System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)nativeImage;
                MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);
                imageSource = memoryStream;
            }

            return imageSource;
        }

        internal static void FreezeElement(Freezable freezable)
        {
            if (freezable.CanFreeze)
            {
                freezable.Freeze();
            }
        }

        internal static IEnumerable<T> AsEnumerable<T>(this UIElementCollection children)
        {
            foreach (var child in children)
            {
                if (child is T)
                    yield return (T)child;
            }
        }
    }
}