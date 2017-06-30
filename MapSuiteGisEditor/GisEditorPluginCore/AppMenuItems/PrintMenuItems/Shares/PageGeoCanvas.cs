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
using System.Drawing;
using System.IO;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    ///The main purpose of this class is to control the drawing of the layers to go from page coordinate system to screen coordinate system.
    ///This class is used by PrintLayer in DrawCore.
    /// </summary>
    internal class PageGeoCanvas : GeoCanvas
    {
        private double pageBaseUnitToPageUnitRatio = 12 * 96;

        private GeoCanvas canvas;
        private RectangleShape pageBoundingBox;
        private RectangleShape printBoundingBox;
        private double X1, X2, Y1, Y2, Xp1, Xp2, Yp1, Yp2;

        public PageGeoCanvas(RectangleShape pageBoundingBox, RectangleShape printBoundingBox)
            : base()
        {
            this.pageBoundingBox = pageBoundingBox;
            this.printBoundingBox = printBoundingBox;
        }

        protected override void BeginDrawingCore(object nativeImage, RectangleShape worldExtent, GeographyUnit drawingMapUnit)
        {
            //Sets the parameters to get the page to screen coordinates conversion properly on the canvas.
            canvas = (GeoCanvas)nativeImage;
            this.CurrentWorldExtent = worldExtent;
            this.pageBaseUnitToPageUnitRatio = PrinterHelper.GetPointsPerGeographyUnit(canvas.MapUnit);

            this.Width = (float)((pageBoundingBox.Width));
            this.Height = (float)((pageBoundingBox.Height));

            X1 = pageBoundingBox.LowerLeftPoint.X;
            X2 = pageBoundingBox.UpperRightPoint.X;
            Y1 = pageBoundingBox.UpperRightPoint.Y;
            Y2 = pageBoundingBox.LowerLeftPoint.Y;

            Xp1 = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, printBoundingBox.UpperLeftPoint, (float)canvas.Width, (float)canvas.Height).X;
            Xp2 = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, printBoundingBox.LowerRightPoint, (float)canvas.Width, (float)canvas.Height).X;
            Yp1 = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, printBoundingBox.LowerRightPoint, (float)canvas.Width, (float)canvas.Height).Y;
            Yp2 = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, printBoundingBox.UpperLeftPoint, (float)canvas.Width, (float)canvas.Height).Y;
        }

        protected override void DrawAreaCore(IEnumerable<ScreenPointF[]> screenPoints, GeoPen outlinePen, GeoBrush fillBrush, DrawingLevel drawingLevel, float xOffset, float yOffset, PenBrushDrawingOrder penBrushDrawingOrder)
        {
            foreach (ScreenPointF[] screenPointFs in screenPoints)
            {
                for (int i = 0; i < screenPointFs.Length; i++)
                {
                    screenPointFs[i] = GetScreenPoint(screenPointFs[i]);
                }
            }

            GeoPen scaledPen = GetScaledPen(outlinePen);

            canvas.DrawArea(screenPoints, scaledPen, fillBrush, drawingLevel, xOffset, yOffset, penBrushDrawingOrder);
        }

        protected override void DrawEllipseCore(ScreenPointF screenPoint, float width, float height, GeoPen outlinePen, GeoBrush fillBrush, DrawingLevel drawingLevel, float xOffset, float yOffset, PenBrushDrawingOrder penBrushDrawingOrder)
        {
            screenPoint = GetScreenPoint(screenPoint);

            GeoPen scaledPen = GetScaledPen(outlinePen); ;

            canvas.DrawEllipse(new ScreenPointF(screenPoint.X, screenPoint.Y), width, height, scaledPen, fillBrush, drawingLevel, xOffset, yOffset, penBrushDrawingOrder);
        }

        protected override void DrawLineCore(IEnumerable<ScreenPointF> screenPoints, GeoPen linePen, DrawingLevel drawingLevel, float xOffset, float yOffset)
        {
            Collection<ScreenPointF> convertedPoints = new Collection<ScreenPointF>();
            foreach (ScreenPointF item in screenPoints)
            {
                convertedPoints.Add(GetScreenPoint(item));
            }

            GeoPen scaledPen = GetScaledPen(linePen);

            canvas.DrawLine(convertedPoints, scaledPen, drawingLevel, xOffset, yOffset);
        }

        protected override void DrawScreenImageCore(GeoImage image, float centerXInScreen, float centerYInScreen, float widthInScreen, float heightInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle)
        {
            ScreenPointF newCenterPoint = GetScreenPoint(new ScreenPointF(centerXInScreen, centerYInScreen));

            float newWidth = (float)(GetScaledLength(widthInScreen));
            float newHeight = (float)(GetScaledLength(heightInScreen));

            canvas.DrawScreenImage(image, newCenterPoint.X, newCenterPoint.Y, newWidth, newHeight, drawingLevel, xOffset, yOffset, rotateAngle);
        }

        protected override void DrawScreenImageWithoutScalingCore(GeoImage image, float centerXInScreen, float centerYInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle)
        {
            DrawScreenImageCore(image, centerXInScreen, centerYInScreen, image.GetWidth(), image.GetHeight(), drawingLevel, xOffset, yOffset, rotateAngle);
        }

        protected override void DrawTextCore(string text, GeoFont font, GeoBrush fillBrush, GeoPen haloPen, IEnumerable<ScreenPointF> textPathInScreen, DrawingLevel drawingLevel, float xOffset, float yOffset, float rotateAngle)
        {
            List<ScreenPointF> screenPoints = new List<ScreenPointF>();

            foreach (ScreenPointF screenPointF in textPathInScreen)
            {
                screenPoints.Add(GetScreenPoint(screenPointF));
            }

            GeoFont scaledFont = GetScaledFont(font);

            canvas.DrawText(text, scaledFont, fillBrush, haloPen, screenPoints, drawingLevel, xOffset, yOffset, rotateAngle);
        }

        protected override DrawingRectangleF MeasureTextCore(string text, GeoFont font)
        {
            Bitmap bitmap = null;
            Graphics graphics = null;
            SizeF size;

            try
            {
                bitmap = new Bitmap(1, 1);
                bitmap.SetResolution(Dpi, Dpi);
                graphics = Graphics.FromImage(bitmap);

                size = graphics.MeasureString(text, GetGdiPlusFontFromGeoFont(font), new PointF(), StringFormat.GenericTypographic);
                if (size.Width == 0 && size.Height != 0 && text.Length != 0)
                {
                    size.Width = 1;
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                size = new SizeF(1, 1);
            }
            finally
            {
                if (graphics != null) { graphics.Dispose(); }
                if (bitmap != null) { bitmap.Dispose(); }
            }

            DrawingRectangleF drawingRectangleF = new DrawingRectangleF(size.Width / 2, size.Height / 2, size.Width, size.Height);

            return drawingRectangleF;
        }

        protected override GeoImage ToGeoImageCore(object nativeImage)
        {
            return canvas.ToGeoImage(nativeImage);
        }

        protected override object ToNativeImageCore(GeoImage image)
        {
            return canvas.ToNativeImage(image);
        }

        protected override void FlushCore()
        {
            canvas.Flush();
        }

        protected override float GetCanvasHeightCore(object nativeImage)
        {
            return (float)pageBoundingBox.Height;
        }

        protected override float GetCanvasWidthCore(object nativeImage)
        {
            return (float)pageBoundingBox.Width;
        }

        public override Stream GetStreamFromGeoImage(GeoImage image)
        {
            return canvas.GetStreamFromGeoImage(image);
        }

        private static Font GetGdiPlusFontFromGeoFont(GeoFont font)
        {
            if (font == null) { return null; }

            FontStyle gdiplusFontStyle = GetFontStyleFromDrawingFontStyle(font.Style);

            Font resultFont = new Font(font.FontName, font.Size, gdiplusFontStyle);

            return resultFont;
        }

        private static FontStyle GetFontStyleFromDrawingFontStyle(DrawingFontStyles style)
        {
            FontStyle returnFontStyle = FontStyle.Regular;

            int value = (int)style;

            if (value < 1 || value > (int)(DrawingFontStyles.Regular |
                                           DrawingFontStyles.Bold |
                                           DrawingFontStyles.Italic |
                                           DrawingFontStyles.Underline |
                                           DrawingFontStyles.Strikeout))
            {
                //throw new ArgumentOutOfRangeException("style", ExceptionDescription.EnumerationOutOfRange);
            }

            if ((style & DrawingFontStyles.Bold) != 0) { returnFontStyle = returnFontStyle | FontStyle.Bold; }
            if ((style & DrawingFontStyles.Italic) != 0) { returnFontStyle = returnFontStyle | FontStyle.Italic; }
            if ((style & DrawingFontStyles.Underline) != 0) { returnFontStyle = returnFontStyle | FontStyle.Underline; }
            if ((style & DrawingFontStyles.Strikeout) != 0) { returnFontStyle = returnFontStyle | FontStyle.Strikeout; }

            return returnFontStyle;
        }

        private GeoPen GetScaledPen(GeoPen geoPen)
        {
            GeoPen rtn = geoPen;
            if (rtn != null)
            {
                rtn = geoPen.CloneDeep();
                rtn.Width = (float)(rtn.Width / canvas.CurrentScale * pageBaseUnitToPageUnitRatio);
            }
            return rtn;
        }

        private GeoFont GetScaledFont(GeoFont geoFont)
        {
            GeoFont rtn = geoFont;

            if (rtn != null)
            {
                float newSize = (float)(rtn.Size / (canvas.CurrentScale) * pageBaseUnitToPageUnitRatio);
                rtn = new GeoFont(rtn.FontName, newSize, rtn.Style);
            }

            return rtn;
        }

        private double GetScaledLength(double length)
        {
            return length / (canvas.CurrentScale) * pageBaseUnitToPageUnitRatio;
        }

        private ScreenPointF GetScreenPoint(ScreenPointF pointF)
        {
            float x = (float)((((pointF.X - X1) * (Xp2 - Xp1)) / (X2 - X1)) + Xp1);
            float y = (float)((((pointF.Y - Y1) * (Yp2 - Yp1)) / (Y2 - Y1)) + Yp1);

            return new ScreenPointF(x, y);
        }
    }
}