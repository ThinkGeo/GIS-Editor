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
using System.Drawing;
using System.Linq;
using System.Text;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal class Kml3DGeoCanvas : GeoCanvas
    {
        private const float virtualMapWidth = 800f;
        private const float virtualMapHeight = 600f;

        private StringBuilder kmlBuilder;
        private RectangleShape virtualWorldExtent = new RectangleShape();
        private Dictionary<int, GeoBrush> geoBrushDictionary = new Dictionary<int, GeoBrush>();
        private Dictionary<int, string> styleUrlDictionary = new Dictionary<int, string>();
        private string extrudeString = string.Empty;
        private string tessellateString = string.Empty;
        private string altitudeModeString = string.Empty;
        private StringBuilder contentStringBuildLevel1 = new StringBuilder();
        private StringBuilder contentStringBuildLevel2 = new StringBuilder();
        private StringBuilder contentStringBuildLevel3 = new StringBuilder();
        private StringBuilder contentStringBuildLevel4 = new StringBuilder();
        private StringBuilder contentStringBuildLabelLevel = new StringBuilder();

        public bool Extrude
        {
            get;
            set;
        }

        public bool Tessellate
        {
            get;
            set;
        }

        public int ZHeight { get; set; }

        protected override void BeginDrawingCore(object nativeImage, RectangleShape worldExtent, GeographyUnit unit)
        {
            kmlBuilder = (StringBuilder)nativeImage;

            virtualWorldExtent = worldExtent;

            extrudeString = Extrude ? @"<extrude>1</extrude>" : "";
            tessellateString = Tessellate ? @"<tessellate>1</tessellate>" : "";
            altitudeModeString = "relativeToGround";

            kmlBuilder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            kmlBuilder.AppendLine(@"<kml xmlns=""http://www.opengis.net/kml/2.2"">");
            kmlBuilder.AppendLine(@"<Document>");
        }

        protected override void DrawAreaCore(IEnumerable<ScreenPointF[]> screenPoints, GeoPen outlinePen, GeoBrush fillBrush, DrawingLevel drawingLevel, float xOffset, float yOffset, PenBrushDrawingOrder penBrushDrawingOrder)
        {
            if (fillBrush == null)
            {
                fillBrush = new GeoSolidBrush(GeoColor.SimpleColors.Transparent);
            }

            int id = 0;
            if (outlinePen != null)
            {
                id = outlinePen.GetHashCode();
            }
            else if (fillBrush != null)
            {
                id = id ^ fillBrush.GetHashCode();
            }

            if (!styleUrlDictionary.ContainsKey(id))
            {
                GeoSolidBrush brush = fillBrush as GeoSolidBrush;
                GeoLinearGradientBrush gradientBrush = fillBrush as GeoLinearGradientBrush;
                GeoHatchBrush hatchBrush = fillBrush as GeoHatchBrush;
                string kmlStyle = string.Empty;
                if (gradientBrush != null)
                {
                    kmlStyle = GetPolygonStyleKml(id, outlinePen, gradientBrush.StartColor);
                }
                else if (hatchBrush != null)
                {
                    kmlStyle = GetPolygonStyleKml(id, outlinePen, hatchBrush.BackgroundColor);
                }
                else
                {
                    kmlStyle = GetPolygonStyleKml(id, outlinePen, brush.Color);
                }
                kmlBuilder.Append(kmlStyle);
                styleUrlDictionary.Add(id, string.Format("<styleUrl>#{0}</styleUrl>", id));
            }

            StringBuilder contentStringBuilder = GetStringBuilder(drawingLevel);

            contentStringBuilder.AppendLine();
            contentStringBuilder.AppendLine(@"<Placemark>");
            contentStringBuilder.AppendLine(styleUrlDictionary[id]);
            contentStringBuilder.AppendLine(@"<Polygon>");

            contentStringBuilder.AppendLine(extrudeString);
            contentStringBuilder.AppendLine(tessellateString);
            contentStringBuilder.AppendLine(altitudeModeString);

            bool firstCoordinates = true;
            foreach (ScreenPointF[] screenPoint in screenPoints)
            {
                if (firstCoordinates)
                {
                    contentStringBuilder.AppendLine(@"<outerBoundaryIs>");
                    AppendLinearRing(screenPoint, xOffset, yOffset, contentStringBuilder);
                    contentStringBuilder.AppendLine(@"</outerBoundaryIs>");
                    firstCoordinates = false;
                }
                else
                {
                    contentStringBuilder.AppendLine(@"<innerBoundaryIs>");
                    AppendLinearRing(screenPoint, xOffset, yOffset, contentStringBuilder);
                    contentStringBuilder.AppendLine(@"</innerBoundaryIs>");
                }
            }
            contentStringBuilder.AppendLine(@"</Polygon>");
            contentStringBuilder.AppendLine(@"</Placemark>");

            foreach (ScreenPointF[] screenPoint in screenPoints)
            {
                contentStringBuilder.AppendLine(@"<Placemark>");
                AppendLinearRing(screenPoint, xOffset, yOffset, ZHeight, contentStringBuilder);
                contentStringBuilder.AppendLine(@"</Placemark>");
            }
        }

        protected override void DrawEllipseCore(ScreenPointF screenPoint, float width, float height, GeoPen outlinePen, GeoBrush fillBrush, DrawingLevel drawingLevel, float xOffset, float yOffset, PenBrushDrawingOrder penBrushDrawingOrder)
        {
            return;
        }

        protected override void DrawLineCore(IEnumerable<ScreenPointF> screenPoints, GeoPen linePen, DrawingLevel drawingLevel, float xOffset, float yOffset)
        {
            int id = linePen.GetHashCode();
            if (!styleUrlDictionary.ContainsKey(id))
            {
                string kmlStyle = GetLineStyleKml(id, linePen);
                kmlBuilder.Append(kmlStyle);
                styleUrlDictionary.Add(id, string.Format("<styleUrl>#{0}</styleUrl>", id));
            }

            StringBuilder contentStringBuilder = GetStringBuilder(drawingLevel);

            contentStringBuilder.AppendLine();
            contentStringBuilder.AppendLine(@"<Placemark>");
            contentStringBuilder.AppendLine(styleUrlDictionary[id]);
            contentStringBuilder.AppendLine(@"<LineString>");

            contentStringBuilder.AppendLine(@"<extrude>1</extrude>");
            contentStringBuilder.AppendLine(tessellateString);
            contentStringBuilder.AppendLine(@"<altitudeMode>");
            contentStringBuilder.AppendLine(altitudeModeString);
            contentStringBuilder.AppendLine(@"</altitudeMode>");

            AppendCoordinates(screenPoints, xOffset, yOffset, ZHeight, contentStringBuilder);
            contentStringBuilder.AppendLine(@"</LineString>");
            contentStringBuilder.AppendLine(@"</Placemark>");
        }

        protected override void DrawTextCore(string text, GeoFont font, GeoBrush fillBrush, GeoPen haloPen, IEnumerable<ScreenPointF> textPathInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle, DrawingTextAlignment drawingTextAlignment)
        {
            int id = 0;
            if (fillBrush != null)
            {
                id = fillBrush.GetHashCode();
            }

            if (!styleUrlDictionary.ContainsKey(id))
            {
                string kmlStyle = GetTextStyleKml(id, ((GeoSolidBrush)fillBrush).Color);
                kmlBuilder.Append(kmlStyle);
                styleUrlDictionary.Add(id, string.Format("<styleUrl>#{0}</styleUrl>", id));
            }

            StringBuilder contentStringBuilder = GetStringBuilder(drawingLevel);

            contentStringBuilder.AppendLine();
            contentStringBuilder.AppendLine(@"<Placemark>");
            contentStringBuilder.AppendLine(styleUrlDictionary[id]);
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            text = text.Replace("`", "&apos;");
            text = text.Replace("\"", "&quot;");
            text = text.Replace("&", "&amp;");
            contentStringBuilder.AppendLine(@"<name>" + text + @"</name>");

            if (textPathInScreen.Count() > 1)
            {
                contentStringBuilder.AppendLine(@"<LineString>");

                contentStringBuilder.AppendLine(extrudeString);
                contentStringBuilder.AppendLine(tessellateString);
                contentStringBuilder.AppendLine(altitudeModeString);
                AppendCoordinates(textPathInScreen, xOffset, yOffset, ZHeight, contentStringBuilder);
                contentStringBuilder.AppendLine(@"</LineString>");
            }
            else
            {
                contentStringBuilder.AppendLine(@"<Point>");

                contentStringBuilder.AppendLine(extrudeString);
                contentStringBuilder.AppendLine(tessellateString);
                contentStringBuilder.AppendLine(altitudeModeString);
                AppendCoordinates(textPathInScreen, xOffset, yOffset, contentStringBuilder);
                contentStringBuilder.AppendLine(@"</Point>");
            }

            contentStringBuilder.AppendLine(@"</Placemark>");
        }

        private void AppendCoordinates(IEnumerable<ScreenPointF> screenPoints, float xOffset, float yOffset, float height, StringBuilder contentStringBuilder)
        {
            contentStringBuilder.AppendLine(@"<coordinates>");

            foreach (ScreenPointF screenPoint in screenPoints)
            {
                PointShape pointShape = ExtentHelper.ToWorldCoordinate(virtualWorldExtent, screenPoint.X + xOffset, screenPoint.Y + yOffset, virtualMapWidth, virtualMapHeight);
                contentStringBuilder.AppendFormat(" {0},{1},{2} ", pointShape.X, pointShape.Y, height);
            }
            contentStringBuilder.AppendLine(@"</coordinates>");
        }

        private void AppendCoordinates(IEnumerable<ScreenPointF> screenPoints, float xOffset, float yOffset, StringBuilder contentStringBuilder)
        {
            contentStringBuilder.AppendLine(@"<coordinates>");

            foreach (ScreenPointF screenPoint in screenPoints)
            {
                PointShape pointShape = ExtentHelper.ToWorldCoordinate(virtualWorldExtent, screenPoint.X + xOffset, screenPoint.Y + yOffset, virtualMapWidth, virtualMapHeight);
                contentStringBuilder.AppendFormat(" {0},{1} ", pointShape.X, pointShape.Y);
            }
            contentStringBuilder.AppendLine(@"</coordinates>");
        }

        private void AppendLinearRing(IEnumerable<ScreenPointF> screenPoints, float xOffset, float yOffset, float height, StringBuilder contentStringBuilder)
        {
            contentStringBuilder.AppendLine(@"<LineString>");
            contentStringBuilder.AppendLine(@"<extrude>1</extrude>");
            contentStringBuilder.AppendLine(@"<altitudeMode>");
            contentStringBuilder.AppendLine(altitudeModeString);
            contentStringBuilder.AppendLine(@"</altitudeMode>");
            AppendCoordinates(screenPoints, xOffset, yOffset, height, contentStringBuilder);
            contentStringBuilder.AppendLine(@"</LineString>");
        }

        private void AppendLinearRing(IEnumerable<ScreenPointF> screenPoints, float xOffset, float yOffset, StringBuilder contentStringBuilder)
        {
            contentStringBuilder.AppendLine(@"<LinearRing>");
            AppendCoordinates(screenPoints, xOffset, yOffset, contentStringBuilder);
            contentStringBuilder.AppendLine(@"</LinearRing>");
        }

        private StringBuilder GetStringBuilder(DrawingLevel drawingLevel)
        {
            StringBuilder result = new StringBuilder();
            switch (drawingLevel)
            {
                case DrawingLevel.LevelOne:
                    result = contentStringBuildLevel1;
                    break;

                case DrawingLevel.LevelTwo:
                    result = contentStringBuildLevel2;
                    break;

                case DrawingLevel.LevelThree:
                    result = contentStringBuildLevel3;
                    break;

                case DrawingLevel.LevelFour:
                    result = contentStringBuildLevel4;
                    break;

                case DrawingLevel.LabelLevel:
                    result = contentStringBuildLabelLevel;
                    break;
            }
            return result;
        }

        private string GetLineStyleKml(int id, GeoPen linePen)
        {
            StringBuilder styleBuilder = new StringBuilder();
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<Style id=""{0}"">", id);
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"<IconStyle>");
            styleBuilder.AppendLine(@"<Icon>");
            styleBuilder.AppendLine(@"</Icon>");
            styleBuilder.AppendLine(@"</IconStyle>");
            styleBuilder.AppendLine(@"<LineStyle>");
            styleBuilder.AppendFormat(@"<color>{0}</color>", GetGoogleHTMLColor(linePen.Color));
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<width>{0}</width>", linePen.Width);
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<scale>{0}</scale>", 1);
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"</LineStyle>");
            styleBuilder.AppendLine(@"</Style>");

            return styleBuilder.ToString();
        }

        private string GetTextStyleKml(int id, GeoColor color)
        {
            StringBuilder styleBuilder = new StringBuilder();
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<Style id=""{0}"">", id);
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"<IconStyle>");
            styleBuilder.AppendLine(@"<Icon>");
            styleBuilder.AppendLine(@"</Icon>");
            styleBuilder.AppendLine(@"</IconStyle>");
            styleBuilder.AppendLine(@"<LabelStyle>");
            styleBuilder.AppendFormat(@"<color>{0}</color>", GetGoogleHTMLColor(color));
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<colorMode>{0}</colorMode>", 1);
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"</LabelStyle>");

            styleBuilder.AppendLine(@"</Style>");

            return styleBuilder.ToString();
        }

        private string GetPolygonStyleKml(int id, GeoPen outlinePen, GeoColor fillColor)
        {
            StringBuilder styleBuilder = new StringBuilder();
            styleBuilder.AppendLine();
            styleBuilder.AppendFormat(@"<Style id=""{0}"">", id);
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"<IconStyle>");
            styleBuilder.AppendLine(@"<Icon>");
            styleBuilder.AppendLine(@"</Icon>");
            styleBuilder.AppendLine(@"</IconStyle>");
            styleBuilder.AppendLine(@"<LineStyle>");
            if (outlinePen != null)
            {
                styleBuilder.AppendFormat(@"<color>{0}</color>", GetGoogleHTMLColor(outlinePen.Color));
                styleBuilder.AppendLine();
                styleBuilder.AppendFormat(@"<width>{0}</width>", outlinePen.Width);
            }
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"</LineStyle>");
            styleBuilder.AppendLine(@"<PolyStyle>");
            styleBuilder.AppendFormat(@"<color>{0}</color>", GetGoogleHTMLColor(fillColor));
            styleBuilder.AppendLine();
            styleBuilder.AppendLine(@"</PolyStyle>");

            styleBuilder.AppendLine(@"</Style>");

            return styleBuilder.ToString();
        }

        private string GetGoogleHTMLColor(GeoColor geoColor)
        {
            StringBuilder googleHtmlColor = new StringBuilder();
            googleHtmlColor.Append(GetColorComponentInHex(geoColor.AlphaComponent));
            googleHtmlColor.Append(GetColorComponentInHex(geoColor.BlueComponent));
            googleHtmlColor.Append(GetColorComponentInHex(geoColor.GreenComponent));
            googleHtmlColor.Append(GetColorComponentInHex(geoColor.RedComponent));
            return googleHtmlColor.ToString();
        }

        private string GetColorComponentInHex(byte component)
        {
            return Convert.ToInt32(component).ToString("X").PadLeft(2, '0');
        }

        protected override void DrawScreenImageCore(GeoImage image, float centerXInScreen, float centerYInScreen, float widthInScreen, float heightInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle)
        {
            throw new NotImplementedException();
        }

        protected override void DrawScreenImageWithoutScalingCore(GeoImage image, float centerXInScreen, float centerYInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle)
        {
            throw new NotImplementedException();
        }

        protected override float GetCanvasHeightCore(object nativeImage)
        {
            return virtualMapHeight;
        }

        protected override float GetCanvasWidthCore(object nativeImage)
        {
            return virtualMapWidth;
        }

        public override System.IO.Stream GetStreamFromGeoImage(GeoImage image)
        {
            throw new NotImplementedException();
        }

        protected override DrawingRectangleF MeasureTextCore(string text, GeoFont font)
        {
            PlatformGeoCanvas canvas = new PlatformGeoCanvas();
            canvas.BeginDrawing(new Bitmap((int)virtualMapWidth, (int)virtualMapHeight), virtualWorldExtent, MapUnit);

            return canvas.MeasureText(text, font);
        }

        protected override GeoImage ToGeoImageCore(object nativeImage)
        {
            throw new NotImplementedException();
        }

        protected override object ToNativeImageCore(GeoImage image)
        {
            throw new NotImplementedException();
        }

        protected override void EndDrawingCore()
        {
            kmlBuilder.Append(contentStringBuildLevel4.ToString());
            kmlBuilder.Append(contentStringBuildLevel3.ToString());
            kmlBuilder.Append(contentStringBuildLevel2.ToString());
            kmlBuilder.Append(contentStringBuildLevel1.ToString());
            kmlBuilder.Append(contentStringBuildLabelLevel.ToString());

            kmlBuilder.AppendFormat(@"</Document>");
            kmlBuilder.AppendFormat(@"</kml>");
        }

        protected override void FlushCore()
        {
            throw new NotImplementedException();
        }
    }
}