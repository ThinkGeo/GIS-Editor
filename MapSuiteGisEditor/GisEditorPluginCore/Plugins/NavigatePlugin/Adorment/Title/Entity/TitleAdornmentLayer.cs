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
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TitleAdornmentLayer : AdornmentLayer
    {
        [Obfuscation(Exclude = true)]
        private float rotation;

        [Obfuscation(Exclude = true)]
        private string title;

        [Obfuscation(Exclude = true)]
        private int maskMargin;

        [Obfuscation(Exclude = true)]
        private GeoBrush fontColor;

        [Obfuscation(Exclude = true)]
        private GeoPen haloPen;

        [Obfuscation(Exclude = true)]
        private GeoBrush maskFillColor;

        [Obfuscation(Exclude = true)]
        private GeoBrush maskOutlineColor;

        [Obfuscation(Exclude = true)]
        private float maskOutlineThickness;

        [Obfuscation(Exclude = true)]
        private GeoFont titleFont;

        public TitleAdornmentLayer()
        { }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public GeoFont TitleFont
        {
            get { return titleFont; }
            set { titleFont = value; }
        }

        public GeoBrush FontColor
        {
            get { return fontColor; }
            set { fontColor = value; }
        }

        public GeoPen HaloPen
        {
            get { return haloPen; }
            set { haloPen = value; }
        }

        public GeoBrush MaskFillColor
        {
            get { return maskFillColor; }
            set { maskFillColor = value; }
        }

        public GeoBrush MaskOutlineColor
        {
            get { return maskOutlineColor; }
            set { maskOutlineColor = value; }
        }

        public float MaskOutlineThickness
        {
            get { return maskOutlineThickness; }
            set { maskOutlineThickness = value; }
        }

        public int MaskMargin
        {
            get { return maskMargin; }
            set { maskMargin = value; }
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            TextStyle textStyle = GetTextStyle();
            DrawingRectangleF rect = canvas.MeasureText(Title, TitleFont);
            ScreenPointF screenPintF = GetDrawingLocation(canvas, rect.Width, rect.Height);

            float centerX = screenPintF.X;
            if (Location != AdornmentLocation.Center && Location != AdornmentLocation.LowerCenter && Location != AdornmentLocation.UpperCenter)
            {
                centerX += (float)(rect.Width * 0.5);
            }
            textStyle.DrawSample(canvas, new DrawingRectangleF(centerX, screenPintF.Y + (float)(rect.Height * 0.5), canvas.Width, canvas.Height));
        }

        public void DrawSample(Stream stream, double imageWidth, double imageHeight)
        {
            TextStyle textStyle = GetTextStyle();
            PlatformGeoCanvas canvas = new PlatformGeoCanvas();
            using (Bitmap bitmap = new Bitmap((int)imageWidth, (int)imageHeight))
            {
                canvas.BeginDrawing(bitmap, new RectangleShape(-180, 90, 180, -90), GeographyUnit.DecimalDegree);
                textStyle.DrawSample(canvas, new DrawingRectangleF((float)(imageWidth * 0.5), (float)(imageHeight * 0.5), (float)imageWidth, (float)imageHeight));

                canvas.EndDrawing();
                bitmap.Save(stream, ImageFormat.Png);
            }
        }

        private TextStyle GetTextStyle()
        {
            TextStyle textStyle = new TextStyle(Title, TitleFont, (GeoSolidBrush)FontColor) { XOffsetInPixel = XOffsetInPixel, YOffsetInPixel = YOffsetInPixel, RotationAngle = Rotation, HaloPen = HaloPen };
            if (MaskFillColor != null)
            {
                textStyle.Mask = new AreaStyle(new GeoPen(MaskOutlineColor, MaskOutlineThickness), (GeoSolidBrush)MaskFillColor);
                textStyle.MaskMargin = MaskMargin;
            }
            return textStyle;
        }
    }
}