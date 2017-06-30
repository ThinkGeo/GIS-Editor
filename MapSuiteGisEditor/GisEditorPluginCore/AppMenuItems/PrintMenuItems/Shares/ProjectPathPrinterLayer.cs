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
using System.Collections;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ProjectPathPrinterLayer : PrinterLayer
    {
        [Obfuscation(Exclude = true)]
        private RectangleShape lastBoundingBox = null;
        [Obfuscation(Exclude = true)]
        private GeoBrush textBrush = null;
        [Obfuscation(Exclude = true)]
        private GeoFont font = null;
        [Obfuscation(Exclude = true)]
        private string projectPath = null;
        [Obfuscation(Exclude = true)]
        private PrinterWrapMode printerWrapMode = PrinterWrapMode.AutoSizeText;

        //private float minFontSize = 1;
        [Obfuscation(Exclude = true)]
        private float maxFontSize = 200;

        public ProjectPathPrinterLayer()
            : this("", new GeoFont("Arial", 10), new GeoSolidBrush(GeoColor.StandardColors.Black)) { }

        public ProjectPathPrinterLayer(string text, GeoFont font, GeoBrush textBrush)
            : base()
        {
            this.projectPath = text;
            this.font = font;
            this.textBrush = textBrush;
        }

        public GeoFont Font
        {
            get { return font; }
            set { font = value; }
        }

        public GeoBrush TextBrush
        {
            get { return textBrush; }
            set { textBrush = value; }
        }

        public string ProjectPath
        {
            get { return projectPath; }
            set { projectPath = value; }
        }

        public PrinterWrapMode PrinterWrapMode
        {
            get { return printerWrapMode; }
            set { printerWrapMode = value; }
        }

        [Obfuscation(Exclude = true)]
        GeoFont drawingFont = null;
        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            if (String.IsNullOrEmpty(projectPath))
            {
                return;
            }
            base.DrawCore(canvas, labelsInAllLayers);

            double oneToOneScale = PrinterHelper.GetPointsPerGeographyUnit(canvas.MapUnit);
            RectangleShape currentBoundingBox = GetBoundingBox();

            string drawingText = projectPath;
            if (printerWrapMode == PrinterWrapMode.WrapText)
            {
                float drawingSize = (float)(Font.Size * oneToOneScale / canvas.CurrentScale);
                drawingFont = new GeoFont(Font.FontName, drawingSize, Font.Style);

                drawingText = WrapText(canvas, currentBoundingBox, drawingFont, projectPath);
            }
            else
            {
                // 1st time draw the layer, change the fontsize to match up the boundingBox
                if (lastBoundingBox == null)
                {
                    float drawingSize = font.Size;
                    if (currentBoundingBox.GetWellKnownText() != new RectangleShape().GetWellKnownText())
                    {
                        drawingSize = GetFontSizeByBoundingBox(canvas, Font, drawingText, currentBoundingBox);
                    }
                    drawingFont = new GeoFont(font.FontName, drawingSize, font.Style);

                    drawingSize = (float)(drawingSize / oneToOneScale * canvas.CurrentScale);
                    Font = new GeoFont(font.FontName, drawingSize, font.Style);
                }
                else
                {
                    // change the boundingBox, change the fontsize to match up the boundingBox
                    if ((Math.Round(lastBoundingBox.Width, 8) != Math.Round(currentBoundingBox.Width, 8))
                        || (Math.Round(lastBoundingBox.Height, 8) != Math.Round(currentBoundingBox.Height, 8)))  // Change font size when resize
                    {
                        float drawingSize = GetFontSizeByBoundingBox(canvas, drawingFont, drawingText, currentBoundingBox);
                        drawingFont = new GeoFont(font.FontName, drawingSize, font.Style);

                        drawingSize = (float)(drawingSize / oneToOneScale * canvas.CurrentScale);
                        Font = new GeoFont(font.FontName, drawingSize, font.Style);
                    }
                    else
                    {
                        float drawingSize = (float)(Font.Size * oneToOneScale / canvas.CurrentScale);
                        drawingFont = new GeoFont(font.FontName, drawingSize, font.Style);
                    }
                }

                lastBoundingBox = currentBoundingBox;
            }

            canvas.DrawTextWithWorldCoordinate(drawingText, drawingFont, TextBrush, currentBoundingBox.GetCenterPoint().X, currentBoundingBox.GetCenterPoint().Y, DrawingLevel.LabelLevel);
        }

        private static string WrapText(GeoCanvas canvas, RectangleShape drawingBoundingBox, GeoFont drawingFont, string text)
        {
            StringBuilder sb = new StringBuilder();

            //ScreenPointF textCenterOnScreen = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, drawingBoundingBox.GetCenterPoint(), canvas.Width, canvas.Height);
            DrawingRectangleF drawingRect = canvas.MeasureText(text, drawingFont);
            ScreenPointF upperLeftOnScreen = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, drawingBoundingBox.UpperLeftPoint, canvas.Width, canvas.Height);
            ScreenPointF lowerRightOnScreen = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, drawingBoundingBox.UpperRightPoint, canvas.Width, canvas.Height);
            int drawingRectWidthOnScreen = (int)(lowerRightOnScreen.X - upperLeftOnScreen.X);

            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            FontStyle fontStyle = GetFontStyleFromDrawingFontStyle(drawingFont.Style);

            SizeF textSize = g.MeasureString(text, new Font(drawingFont.FontName, drawingFont.Size, fontStyle), new PointF(), StringFormat.GenericTypographic);

            if (drawingRect.Width > drawingRectWidthOnScreen)
            {
                text = text.Replace("\n", " ");
                text = text.Replace("\r", " ");
                text = text.Replace(".", ". ");
                text = text.Replace(">", "> ");
                text = text.Replace("\t", " ");
                text = text.Replace(",", ", ");
                text = text.Replace(";", "; ");
                text = text.Replace("<br>", " ");

                int maxStringLength = GetMaxStringLength(text, textSize.Width, drawingRectWidthOnScreen);
                string[] texts = Wrap(text, maxStringLength);
                foreach (string item in texts)
                {
                    if (item == "")
                    {
                        sb.Append(item);
                    }
                    else
                    {
                        sb.AppendLine(item);
                    }
                }
            }
            else
            {
                sb.Append(text);
            }

            return sb.ToString();
        }

        internal static FontStyle GetFontStyleFromDrawingFontStyle(DrawingFontStyles style)
        {
            FontStyle returnFontStyle = FontStyle.Regular;

            int value = (int)style;

            if (value < 1 || value > (int)(DrawingFontStyles.Regular |
                                           DrawingFontStyles.Bold |
                                           DrawingFontStyles.Italic |
                                           DrawingFontStyles.Underline |
                                           DrawingFontStyles.Strikeout))
            {
                throw new ArgumentOutOfRangeException("style", "The value for the enumeration is not one of the valid values.");
            }

            if ((style & DrawingFontStyles.Bold) != 0) { returnFontStyle = returnFontStyle | FontStyle.Bold; }
            if ((style & DrawingFontStyles.Italic) != 0) { returnFontStyle = returnFontStyle | FontStyle.Italic; }
            if ((style & DrawingFontStyles.Underline) != 0) { returnFontStyle = returnFontStyle | FontStyle.Underline; }
            if ((style & DrawingFontStyles.Strikeout) != 0) { returnFontStyle = returnFontStyle | FontStyle.Strikeout; }

            return returnFontStyle;
        }

        //Function to get the maximum length of the string without having a new line.
        private static int GetMaxStringLength(string text, float currentWidth, double boundingBoxScreenWidth)
        {
            //For now, the value is 10. You can write your own logic to calculate that value based on 
            // the size of the text and the bounding box to hold it.
            int result = (int)(text.Length * boundingBoxScreenWidth / currentWidth);
            if (result <= 0) { result = 1; }
            return result;
        }

        //Function taken from http://www.velocityreviews.com/forums/t20370-word-wrap-line-break-code-and-algorithm-for-c.html
        private static string[] Wrap(string text, int maxLength)
        {
            string[] Words = text.Split(' ');
            int currentLineLength = 0;
            ArrayList Lines = new ArrayList(text.Length / maxLength);
            string currentLine = "";
            bool InTag = false;

            foreach (string currentWord in Words)
            {
                //ignore html
                if (currentWord.Length > 0)
                {
                    if (currentWord.Substring(0, 1) == "<")
                        InTag = true;

                    if (InTag)
                    {
                        //handle filenames inside html tags
                        if (currentLine.EndsWith("."))
                        {
                            currentLine += currentWord;
                        }
                        else
                            currentLine += " " + currentWord;

                        if (currentWord.IndexOf(">") > -1)
                            InTag = false;
                    }
                    else
                    {
                        if (currentLineLength + currentWord.Length + 1 < maxLength)
                        {
                            currentLine += " " + currentWord;
                            currentLineLength += (currentWord.Length + 1);
                        }
                        else
                        {
                            Lines.Add(currentLine);
                            currentLine = currentWord;
                            currentLineLength = currentWord.Length;
                        }
                    }
                }

            }
            if (currentLine != "")
                Lines.Add(currentLine);

            string[] textLinesStr = new string[Lines.Count];
            Lines.CopyTo(textLinesStr, 0);
            return textLinesStr;
        }

        private float GetFontSizeByBoundingBox(GeoCanvas canvas, GeoFont font, string drawingText, RectangleShape boundingBox)
        {
            float rtn = font.Size;

            ScreenPointF boundingBoxPointFUL = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, boundingBox.UpperLeftPoint, canvas.Width, canvas.Height);
            ScreenPointF boundingBoxPointFUR = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, boundingBox.UpperRightPoint, canvas.Width, canvas.Height);
            ScreenPointF boundingBoxPointFLL = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, boundingBox.LowerLeftPoint, canvas.Width, canvas.Height);

            double widthInScreen = boundingBoxPointFUR.X - boundingBoxPointFUL.X;
            double heightInScreen = boundingBoxPointFLL.Y - boundingBoxPointFUL.Y;

            DrawingRectangleF textRectInScreen = canvas.MeasureText(drawingText, font);

            if (textRectInScreen.Width > widthInScreen || textRectInScreen.Height > heightInScreen)
            {
                while (textRectInScreen.Width > widthInScreen || textRectInScreen.Height > heightInScreen)
                {
                    rtn = rtn * 9 / 10;
                    textRectInScreen = canvas.MeasureText(drawingText, new GeoFont(font.FontName, rtn, font.Style));
                }
            }
            else
            {
                while (textRectInScreen.Width < widthInScreen && textRectInScreen.Height < heightInScreen)
                {
                    rtn = rtn * 10 / 9;
                    textRectInScreen = canvas.MeasureText(drawingText, new GeoFont(font.FontName, rtn, font.Style));
                }
                rtn = rtn * 9 / 10;
            }
            if (rtn > maxFontSize)
            {
                rtn = maxFontSize;
            }

            return rtn;
        }
    }
}
