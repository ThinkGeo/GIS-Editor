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
using System.Globalization;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal static class GisEditorWpfMapExtension
    {
        private static readonly ProjectionAbbreviations projectionAbbreviations;

        static GisEditorWpfMapExtension()
        {
            projectionAbbreviations = new ProjectionAbbreviations();
        }

        public static ContentPresenter GetMapBasicInformation(this GisEditorWpfMap map)
        {
            string projectionFullName = "Unknown";
            GeographyUnit mapUnit = GeographyUnit.Unknown;
            if (map.DisplayProjectionParameters != null)
            {
                string projectionShortName = map.DisplayProjectionParameters.Split(' ')[0].Replace("+proj=", string.Empty);
                projectionFullName = projectionAbbreviations[projectionShortName];
                mapUnit = GisEditorWpfMap.GetGeographyUnit(map.DisplayProjectionParameters);
            }

            double scaleValue = map.CurrentScale;
            int zoomLevel = map.GetSnappedZoomLevelIndex(scaleValue) + 1;

            bool isPreciseZoomLevel = map.ZoomLevelSet.GetZoomLevels()[zoomLevel - 1] is PreciseZoomLevel;

            TextBlock projectionTextBlock = new TextBlock();
            projectionTextBlock.SetResourceReference(TextBlock.TextProperty, "MapExtensionProjectionText");
            TextBlock projectionValueTextBlock = new TextBlock();
            projectionValueTextBlock.Text = string.Format(" {0} | ", projectionFullName);
            TextBlock mapUnitTextBlock = new TextBlock();
            mapUnitTextBlock.SetResourceReference(TextBlock.TextProperty, "MapExtensionMapUnitText");
            TextBlock mapUnitValueTextBlock = new TextBlock();
            mapUnitValueTextBlock.Text = string.Format(" {0} | ", mapUnit);
            TextBlock zoomLevelTextBlock = new TextBlock();
            zoomLevelTextBlock.SetResourceReference(TextBlock.TextProperty, "MapExtensionZoomLevelText");
            TextBlock zoomLevelValueTextBlock = new TextBlock();
            if (isPreciseZoomLevel)
                zoomLevelValueTextBlock.Text = string.Format(" Temporary Zoom Level | ");
            else
                zoomLevelValueTextBlock.Text = string.Format(" {0:D2} | ", zoomLevel);

            TextBlock scaleTextBlock = new TextBlock();
            scaleTextBlock.SetResourceReference(TextBlock.TextProperty, "MapExtensionScaleText");
            TextBlock scaleValueTextBlock = new TextBlock();
            scaleValueTextBlock.Text = string.Format(" 1:{0:N0}", scaleValue);

            StackPanel mapInformationStackPanel = new StackPanel();
            mapInformationStackPanel.Orientation = Orientation.Horizontal;
            mapInformationStackPanel.Children.Add(projectionTextBlock);
            mapInformationStackPanel.Children.Add(projectionValueTextBlock);
            mapInformationStackPanel.Children.Add(mapUnitTextBlock);
            mapInformationStackPanel.Children.Add(mapUnitValueTextBlock);
            mapInformationStackPanel.Children.Add(zoomLevelTextBlock);
            mapInformationStackPanel.Children.Add(zoomLevelValueTextBlock);
            mapInformationStackPanel.Children.Add(scaleTextBlock);
            mapInformationStackPanel.Children.Add(scaleValueTextBlock);

            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Content = mapInformationStackPanel;

            return contentPresenter;
        }

        public static string GetFormattedWorldCoordinate(this GisEditorWpfMap map, ScreenPointF screenPoint, MouseCoordinateType mouseCoordinateType)
        {
            PointShape lonlat = map.ToWorldCoordinate(screenPoint.X, screenPoint.Y);
            double xInCurrentProjection = lonlat.X;
            double yInCurrentProjection = lonlat.Y;
            string projectionFullName = "Unknown";
            if (map.DisplayProjectionParameters != null)
            {
                string projectionShortName = map.DisplayProjectionParameters.Split(' ')[0].Replace("+proj=", string.Empty);
                projectionFullName = projectionAbbreviations[projectionShortName];
            }

            GeographyUnit mapUnit = GisEditorWpfMap.GetGeographyUnit(map.DisplayProjectionParameters);
            if (projectionFullName == "Unknown" && mapUnit == GeographyUnit.Unknown)
            {
                return String.Format("X:{0}, Y:{1}", lonlat.X.ToString("N4", CultureInfo.InvariantCulture), lonlat.Y.ToString("N4", CultureInfo.InvariantCulture));
            }
            else
            {
                if (mapUnit != GeographyUnit.DecimalDegree)
                {
                    try
                    {
                        Proj4Projection proj = new Proj4Projection();
                        proj.InternalProjectionParametersString = map.DisplayProjectionParameters;
                        proj.ExternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                        proj.Open();
                        lonlat = proj.ConvertToExternalProjection(lonlat) as PointShape;
                        proj.Close();
                    }
                    catch
                    {
                        lonlat = new PointShape();
                    }
                }
                return GetMouseCoordinates(lonlat.X, lonlat.Y, mouseCoordinateType, map.DisplayProjectionParameters, xInCurrentProjection, yInCurrentProjection);
            }
        }

        private static string GetMouseCoordinates(double lon, double lat, MouseCoordinateType mouseCoordinateType, string displayProj4, double xInCurrentProjection = double.NaN,
            double yInCurrentProjection = double.NaN)
        {
            switch (mouseCoordinateType)
            {
                case MouseCoordinateType.Default:

                    //Vertex vertex = ConvertCurrentCoordinatesTo4326Coordinates(lon, lat, displayProj4);
                    return String.Format("X:{0}, Y:{1} | {2}", xInCurrentProjection.ToString("N4", CultureInfo.InvariantCulture), yInCurrentProjection.ToString("N4", CultureInfo.InvariantCulture), GetDmsString(lon, lat));
                case MouseCoordinateType.LatitudeLongitude:
                    return String.Format(CultureInfo.InvariantCulture, "Latitude: {0}, Longitude: {1}", Math.Round(lat, 4).ToString(CultureInfo.InvariantCulture), Math.Round(lon, 4).ToString(CultureInfo.InvariantCulture));
                case MouseCoordinateType.LongitudeLatitude:
                    return String.Format(CultureInfo.InvariantCulture, "Longitude: {0}, Latitude: {1}", Math.Round(lon, 4).ToString(CultureInfo.InvariantCulture), Math.Round(lat, 4).ToString(CultureInfo.InvariantCulture));
                case MouseCoordinateType.DegreesMinutesSeconds:
                    return GetDmsString(lon, lat);
                default:
                    return "--°--'--\"E  --°--'--\"N";
            }
        }

        private static Vertex ConvertCurrentCoordinatesTo4326Coordinates(double lon, double lat, string displayProj4)
        {
            Proj4Projection proj4Projection = new Proj4Projection();
            proj4Projection.InternalProjectionParametersString = displayProj4;
            proj4Projection.ExternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
            proj4Projection.Open();
            Vertex vertex = proj4Projection.ConvertToExternalProjection(lon, lat);
            proj4Projection.Close();
            return vertex;
        }

        private static string GetDmsString(double lon, double lat)
        {
            string latitude = ToDMS(lat);
            string longtitude = ToDMS(lon);
            if (lat < 0d)
            {
                latitude += "S";
            }
            else
            {
                latitude += "N";
            }
            if (lon < 0d)
            {
                longtitude += "W";
            }
            else
            {
                longtitude += "E";
            }
            return String.Format(CultureInfo.InvariantCulture, "{0}, {1}", longtitude, latitude);
        }

        private static string ToDMS(double coordinate)
        {
            // Work with a positive number
            coordinate = Math.Abs(coordinate);

            // Get d/m/s components
            double d = Math.Floor(coordinate);
            coordinate -= d;
            coordinate *= 60;
            double m = Math.Floor(coordinate);
            coordinate -= m;
            coordinate *= 60;
            double s = Math.Round(coordinate);

            // Create padding character
            char pad;
            bool flag = char.TryParse("0", out pad);

            if (flag)
            {
                // Create d/m/s strings
                string dd = d.ToString(CultureInfo.InvariantCulture);
                string mm = m.ToString(CultureInfo.InvariantCulture).PadLeft(2, pad);
                string ss = s.ToString(CultureInfo.InvariantCulture).PadLeft(2, pad);

                // Append d/m/s
                string dms = string.Format(CultureInfo.InvariantCulture, "{0}°{1}'{2}\"", dd, mm, ss);
                return dms;
            }
            else
            {
                return "Invalid coordinate.";
            }
        }
    }
}