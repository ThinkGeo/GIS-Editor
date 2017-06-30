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


using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class TrackOverlayExtension
    {
        public static void SetTrackMode(this TrackInteractiveOverlay trackOverlay, TrackMode mode)
        {
            if (trackOverlay.TrackMode != mode)
            {
                trackOverlay.TrackMode = mode;
                if (mode == TrackMode.Polygon || mode == TrackMode.Line || mode == TrackMode.Point || mode == TrackMode.Multipoint)
                {
                    SetStyle(trackOverlay.TrackShapeLayer);
                }
            }
        }

        private static void SetStyle(InMemoryFeatureLayer layer)
        {
            PointStyle pointStyle = PointStyles.CreateSimpleCircleStyle(GeoColor.FromArgb(100, GeoColor.StandardColors.White), 12, GeoColor.FromArgb(200, GeoColor.StandardColors.Black), 1);

            LineStyle lineStyle = LineStyles.CreateSimpleLineStyle(GeoColor.FromArgb(255, 0, 0, 255), 2, true);
            lineStyle.OuterPen.LineJoin = DrawingLineJoin.Round;
            lineStyle.InnerPen.LineJoin = DrawingLineJoin.Round;
            lineStyle.CenterPen.LineJoin = DrawingLineJoin.Round;

            AreaStyle areaStyle = AreaStyles.CreateSimpleAreaStyle(GeoColor.FromArgb(102, 0, 0, 255), GeoColor.FromArgb(255, 0, 0, 255), 2);
            areaStyle.OutlinePen.LineJoin = DrawingLineJoin.Round;

            layer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            layer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
            layer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
            layer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
            layer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;

            layer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(pointStyle);
            layer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(lineStyle);
            layer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(areaStyle);
        }
    }
}