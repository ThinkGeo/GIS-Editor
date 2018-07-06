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
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class StyleExtension
    {
        public static BitmapImage GetPreviewImage(this Style style)
        {
            return GetPreviewImage(style, 102, 32);
        }

        public static BitmapImage GetPreview(this Style style, int width, int height)
        {
            int drawingWidth = width == 0 ? 102 : width;
            int drawingHeight = height == 0 ? 32 : height;
            return GetPreviewImage(style, drawingWidth, drawingHeight);
        }

        public static BitmapImage GetPreviewImage(this Style style, int width, int height)
        {
            byte[] bytes = GetPreviewBinary(style, width, height);
            return GetImageFromBinary(bytes);
        }

        public static byte[] GetPreviewBinary(this Style style, int width = 23, int height = 23)
        {
            using (var bitmap = new Bitmap(width, height))
            {
                var canvas = new PlatformGeoCanvas();
                canvas.BeginDrawing(bitmap, new RectangleShape(-10, 10, 10, -10), GeographyUnit.DecimalDegree);
                DrawStyleSamples(style, width, height, canvas);
                canvas.EndDrawing();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    return memoryStream.ToArray();
                }
            }
        }

        public static Collection<string> ParseColumnNamesInExpression(string expression)
        {
            var columnNames = new Collection<string>();
            if (expression.Contains("feature.ColumnValues"))
            {
                var r = new Regex("feature.ColumnValues\\[\"\\w+\"\\]");
                MatchCollection matches = r.Matches(expression);

                foreach (var match in matches)
                {
                    string matchValue = match.ToString();
                    matchValue = matchValue.Replace("feature.ColumnValues", string.Empty);
                    matchValue = matchValue.Replace("\"", string.Empty);
                    matchValue = matchValue.TrimStart('[').TrimEnd(']');
                    columnNames.Add(matchValue);
                }
            }
            else
            {
                ParseSegments(expression, '[', ']', segment =>
                {
                    columnNames.Add(segment.Trim('"'));
                });
            }

            return columnNames;
        }

        [Obsolete("This method is obsoleted, please use ParseColumnNamesInExpression(string expression) instead. This property is obsolete and may be removed in or after version 9.0.")]
        public static string ParseDlrExpression(string expression, string variableName = null)
        {
            var columnNames = ParseColumnNamesInExpression(expression);
            var replacePairs = new Dictionary<string, string>();
            int index = 0;
            foreach (var match in columnNames)
            {
                string matchValue = string.Format(CultureInfo.InvariantCulture, "[{0}]", match);
                string replaceValue = "{" + index + "}";
                if (!replacePairs.ContainsKey(matchValue))
                {
                    replacePairs.Add(matchValue, replaceValue);
                    expression = expression.Replace(matchValue, replaceValue);
                    index++;
                }
            }

            var prefix = string.IsNullOrEmpty(variableName) ? "" : variableName + ".";
            //if (getColumnValuesPropertyName == null)
            //{
            //    getColumnValuesPropertyName = str => "ColumnValues";
            //}

            //var replaceArgs = replacePairs.Select(p => p.Key.Replace("[", prefix + getColumnValuesPropertyName(p.Key) + "[\"").Replace("]", "\"]")).ToArray();
            //return String.Format(CultureInfo.InvariantCulture, expression, replaceArgs);
            return prefix;
        }

        public static string FormalizeLinkColumnValue(string text, string separator)
        {
            string temp = string.Empty;
            Collection<string> results = new Collection<string>();
            if (!string.IsNullOrEmpty(text))
            {
                bool isStart = false;
                bool isEnd = false;
                foreach (var item in text)
                {
                    if (item == '{')
                    {
                        isStart = true;
                        continue;
                    }
                    if (item == '}')
                    {
                        isEnd = true;
                        isStart = false;
                    }
                    if (item != '}' && isStart)
                    {
                        temp += item.ToString();
                    }
                    else if (isEnd)
                    {
                        results.Add(temp);
                        temp = string.Empty;
                        isEnd = false;
                    }
                }
            }

            if (results.All(r => !string.IsNullOrEmpty(r)))
            {
                for (int i = 0; i < results.Count; i++)
                {
                    string tempValue = "|";
                    for (int j = 0; j < i; j++)
                    {
                        tempValue += "|";
                    }
                    text = text.Replace("{" + results[i] + "}", "{" + tempValue + "}");
                }

                if (results.Count > 0)
                {
                    int count = 0;
                    foreach (var item in results)
                    {
                        string[] values = item.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length > count)
                        {
                            count = values.Length;
                        }
                    }

                    Collection<Collection<string>> allValues = new Collection<Collection<string>>();
                    for (int i = 0; i < count; i++)
                    {
                        Collection<string> elements = new Collection<string>();
                        foreach (var item in results)
                        {
                            string[] values = item.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                            if (values.Length > i)
                            {
                                elements.Add(values[i]);
                            }
                            else
                            {
                                elements.Add(values.FirstOrDefault());
                            }
                        }
                        allValues.Add(elements);
                    }

                    string result = text;
                    string value = string.Empty;
                    foreach (var values in allValues)
                    {
                        for (int i = 0; i < values.Count; i++)
                        {
                            string tempValue = "|";
                            for (int k = 0; k < i; k++)
                            {
                                tempValue += "|";
                            }
                            result = result.Replace("{" + tempValue + "}", values[i]);
                        }
                        value += result + "\r\n";
                        result = text;
                    }

                    text = value;
                }
            }
            return text;
        }

        private static void DrawStyleSamples(Style style, int width, int height, PlatformGeoCanvas canvas)
        {
            var drawingRectangleF = new DrawingRectangleF(width * .5f, height * .5f, width, height);
            if (style is CompositeStyle)
            {
                foreach (var subStyle in ((CompositeStyle)style).Styles)
                {
                    DrawStyleSamples(subStyle, width, height, canvas);
                }
            }
            else if (style is ClassBreakStyle || style is ValueStyle || style is FilterStyle)
            {
                style.DrawSample(canvas, drawingRectangleF);
            }
            else if (style is DotDensityStyle)
            {
                var dotDensityStyle = ((DotDensityStyle)style).CustomPointStyle;
                DrawDotDensityStyle(dotDensityStyle, canvas, drawingRectangleF);
            }

            //RegexStyle is the Filter Style in Gis Editor
            else if (style is RegexStyle)
            {
                DrawStaticImage(canvas, "pack://,,,/GisEditorPluginCore;component/Images/FilterStyle.png", drawingRectangleF);
            }
            else
            {
                DrawNormalStyle(style, canvas, drawingRectangleF);
            }
        }

        private static void DrawStaticImage(PlatformGeoCanvas canvas, string uri, DrawingRectangleF drawingRectangleF)
        {
            var streamInfo = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.RelativeOrAbsolute));
            if (streamInfo != null && streamInfo.Stream != null)
            {
                canvas.DrawScreenImage(new GeoImage(streamInfo.Stream), drawingRectangleF.CenterX, drawingRectangleF.CenterY, drawingRectangleF.Width, drawingRectangleF.Height, DrawingLevel.LevelOne, 0, 0, 0);
            }
        }

        private static BitmapImage GetImageFromBinary(byte[] bytes)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private static byte[] GetComplicatedStyleSampleImageBinary(IEnumerable<Style> styles, int width, int height)
        {
            int loopCount = styles.Count() > 4 ? 4 : styles.Count();
            using (Bitmap bitmap = new Bitmap(width, height))
            {
                PlatformGeoCanvas canvas = new PlatformGeoCanvas();
                canvas.BeginDrawing(bitmap, new RectangleShape(-180, 90, 180, -90), GeographyUnit.DecimalDegree);

                Collection<Tuple<int, int>> tuples = new Collection<Tuple<int, int>>() { new Tuple<int, int>(1, 1), new Tuple<int, int>(3, 1), new Tuple<int, int>(1, 3), new Tuple<int, int>(3, 3) };
                float tileWidth = width * 0.5f;
                float tileHeight = height * 0.5f;
                float centerXParameter = tileWidth * 0.5f;
                float centerYParameter = tileHeight * 0.5f;

                for (int i = 0; i < loopCount; i++)
                {
                    Style customStyle = styles.ElementAt(i);

                    float centerX = tuples[i].Item1 * centerXParameter;
                    float centerY = tuples[i].Item2 * centerYParameter;
                    if (customStyle != null)
                    {
                        customStyle.DrawSample(canvas, new DrawingRectangleF(centerX, centerY, tileWidth, tileHeight));
                    }
                }
                canvas.EndDrawing();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    return memoryStream.ToArray();
                }
            }
        }

        private static void DrawDotDensityStyle(PointStyle pointStyle, PlatformGeoCanvas canvas, DrawingRectangleF drawingRectangleF)
        {
            if (pointStyle == null) return;

            var tmpSymbolSize = pointStyle.SymbolSize;
            var tmpSymbolPenWidth = pointStyle.SymbolPen.Width;
            var tmpFontSize = pointStyle.CharacterFont.Size;
            Bitmap originalBitmap = null;
            Bitmap newBitmap = null;

            try
            {
                var customerSymbolStyle = pointStyle as SymbolPointStyle;
                if (customerSymbolStyle != null)
                {
                    originalBitmap = canvas.ToNativeImage(customerSymbolStyle.Image) as Bitmap;
                    newBitmap = new Bitmap(originalBitmap, 9, 9);
                    customerSymbolStyle.Image = canvas.ToGeoImage(newBitmap);
                }
                else
                {
                    pointStyle.SymbolSize = 3;
                    pointStyle.SymbolPen.Width = 1;
                    pointStyle.CharacterFont = new GeoFont(pointStyle.CharacterFont.FontName, 6, pointStyle.CharacterFont.Style);
                }

                var halfWidth = drawingRectangleF.Width * 0.5f;
                var halfHeight = drawingRectangleF.Height * 0.5f;
                float[] centersX = new float[7];
                float[] centersY = new float[7];
                centersX[0] = halfWidth * 0.5f - 1;
                centersY[0] = halfHeight * 0.5f - 1;
                centersX[1] = centersX[0] + 2;
                centersY[1] = centersY[0] + 2;
                centersX[2] = halfWidth;
                centersY[2] = halfHeight;
                centersX[3] = halfWidth + halfWidth * 0.5f;
                centersY[3] = halfHeight;
                centersX[4] = halfWidth + halfWidth * 0.5f;
                centersY[4] = halfHeight + halfHeight * 0.5f;
                centersX[5] = centersX[4] - 1;
                centersY[5] = centersY[4] - 1;
                centersX[6] = centersX[4] + 1;
                centersY[6] = centersY[4] + 1;
                for (int i = 0; i < 7; i++)
                {
                    float centerX = centersX[i];
                    float centerY = centersY[i];
                    if (pointStyle != null)
                    {
                        pointStyle.DrawSample(canvas, new DrawingRectangleF(centerX, centerY, 11, 11));
                    }
                }

                if (originalBitmap != null)
                {
                    customerSymbolStyle.Image = canvas.ToGeoImage(originalBitmap);
                }
                else
                {
                    pointStyle.SymbolSize = tmpSymbolSize;
                    pointStyle.SymbolPen.Width = tmpSymbolPenWidth;
                    pointStyle.CharacterFont = new GeoFont(pointStyle.CharacterFont.FontName, tmpFontSize, pointStyle.CharacterFont.Style);
                }
            }
            finally
            {
                if (originalBitmap != null) originalBitmap.Dispose();
                if (newBitmap != null) newBitmap.Dispose();
            }
        }

        private static void DrawNormalStyle(this Style style, PlatformGeoCanvas canvas, DrawingRectangleF drawingRectangleF)
        {
            try
            {
                if (style is LineStyle)
                {
                    LineStyle lineStyle = (LineStyle)style;

                    if (lineStyle.CenterPen.Width <= 2 && lineStyle.InnerPen.Width <= 2 && lineStyle.OuterPen.Width <= 2)
                    {
                        lineStyle = (LineStyle)lineStyle.CloneDeep();

                        lineStyle.CenterPen.Width += 1;
                        lineStyle.InnerPen.Width += 1;
                        lineStyle.OuterPen.Width += 1;

                        LineShape line = GenerateStraightHorizontalLineShape(canvas.CurrentWorldExtent);
                        line.Rotate(line.GetCenterPoint(), 270);
                        lineStyle.Draw(new BaseShape[] { line }, canvas, new Collection<SimpleCandidate>(), new Collection<SimpleCandidate>());
                    }
                    else
                    {
                        lineStyle.DrawSample(canvas, drawingRectangleF);
                    }
                }
                else if (style is FontPointStyle)
                {
                    var fontStyle = (FontPointStyle)style;
                    var tmpsize = fontStyle.CharacterFont.Size;
                    if (tmpsize > 26)
                        fontStyle.CharacterFont = new GeoFont(fontStyle.CharacterFont.FontName, 26, fontStyle.CharacterFont.Style);
                    fontStyle.DrawSample(canvas, drawingRectangleF);
                    if (tmpsize > 26)
                        fontStyle.CharacterFont = new GeoFont(fontStyle.CharacterFont.FontName, tmpsize, fontStyle.CharacterFont.Style);
                }
                else if (style is PointStyle)
                {
                    var pointStyle = (PointStyle)style;
                    var tmpSymbolSize = pointStyle.SymbolSize;
                    if (tmpSymbolSize > 22)
                        pointStyle.SymbolSize = 22;
                    pointStyle.DrawSample(canvas, drawingRectangleF);
                    if (tmpSymbolSize > 22)
                        pointStyle.SymbolSize = tmpSymbolSize;
                }
                else if (style != null)
                {
                    style.DrawSample(canvas, drawingRectangleF);
                }
            }
            catch
            {
                return;
            }
        }

        private static LineShape GenerateStraightHorizontalLineShape(RectangleShape extent)
        {
            LineShape line = new LineShape();
            Vertex leftVertex = new Vertex((extent.LowerLeftPoint.Y + extent.UpperLeftPoint.Y) / 2, extent.LowerLeftPoint.X);
            Vertex rightVertex = new Vertex((extent.LowerRightPoint.Y + extent.UpperRightPoint.Y) / 2, extent.LowerRightPoint.X);
            line.Vertices.Add(leftVertex);
            line.Vertices.Add(rightVertex);

            return line;
        }

        private static void ParseSegments(string content, char start, char end, Action<string> oneParsed)
        {
            int startIndex = content.IndexOf(start);

            while (startIndex != -1)
            {
                int endIndex = content.IndexOf(end, startIndex + 1);
                if (endIndex != -1)
                {
                    string tempContent = content.Substring(startIndex + 1, endIndex - startIndex - 1);
                    if (oneParsed != null)
                    {
                        oneParsed(tempContent);
                    }
                    startIndex = content.IndexOf(start, endIndex + 1);
                }
                else break;
            }
        }

        private static void ParseSegments(string content, string start, string end, Action<string> oneParsed)
        {
            int startIndex = content.IndexOf(start);

            while (startIndex != -1)
            {
                int endIndex = content.IndexOf(end, startIndex + start.Length);
                if (endIndex != -1)
                {
                    string tempContent = content.Substring(startIndex + start.Length, endIndex - startIndex - start.Length);
                    if (oneParsed != null)
                    {
                        oneParsed(tempContent);
                    }
                    startIndex = content.IndexOf(start, endIndex + end.Length);
                }
                else break;
            }
        }
    }
}