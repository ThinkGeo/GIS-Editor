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
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class StyleHelper
    {
        public static System.Windows.Controls.Image GetImageFromStyle(IEnumerable<Style> styles)
        {
            if (styles.Count() > 1)
            {
                CompositeStyle componentStyle = new CompositeStyle(styles) { Name = GisEditor.LanguageManager.GetStringResource("SampleStyleName") };
                BitmapImage imageSource = componentStyle.GetPreviewImage(34, 34);
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = imageSource;
                return image;
            }
            else if (styles.Count() == 0)
            {
                return new System.Windows.Controls.Image();
            }
            else return GetImageFromStyle(styles.FirstOrDefault());
        }

        public static System.Windows.Controls.Image GetImageFromStyle(Style style)
        {
            BitmapSource bitmapSource = new BitmapImage();
            var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
            if (styleItem != null) bitmapSource = styleItem.GetPreviewSource(34, 34);
            return new System.Windows.Controls.Image { Source = bitmapSource, Stretch = Stretch.Fill };
        }

        public static IEnumerable<Type> GetCompositedStylePluginTypes()
        {
            yield return typeof(TextFilterStylePlugin);

            //yield return typeof(DotDensityAreaStylePlugin);
        }

        public static byte[] GetImageBufferFromStyle(Style style)
        {
            return style.GetPreviewBinary(34, 34);
            //GdiPlusGeoCanvas canvas = new GdiPlusGeoCanvas();
            //Bitmap bitmap = new Bitmap(34, 34);
            //var drawingWidth = bitmap.Width - 2;
            //var drawingHeight = bitmap.Height - 2;
            //using (bitmap)
            //{
            //    canvas.BeginDrawing(bitmap, new RectangleShape(-10, 10, 10, -10), GeographyUnit.DecimalDegree);
            //    if (style is TextStyle)
            //    {
            //        TextStyle textStyle = (TextStyle)style;
            //        if (textStyle.CustomTextStyles.Count == 0)
            //        {
            //            canvas.DrawTextWithWorldCoordinate("A", textStyle.Font, textStyle.TextSolidBrush, textStyle.HaloPen, 0, 0, DrawingLevel.LabelLevel);
            //        }
            //        else
            //        {
            //            canvas.DrawTextWithWorldCoordinate("A", textStyle.CustomTextStyles[0].Font, textStyle.CustomTextStyles[0].TextSolidBrush, textStyle.CustomTextStyles[0].HaloPen, 0, 0, DrawingLevel.LabelLevel);
            //        }
            //    }
            //    else if (style is DotDensityStyle)
            //    {
            //        ((DotDensityStyle)style).CustomPointStyle
            //            .DrawSample(canvas, new DrawingRectangleF(drawingWidth * .5f + .5f, drawingHeight * .5f, drawingWidth, drawingHeight));
            //    }
            //    else
            //    {
            //        style.DrawSample(canvas, new DrawingRectangleF(drawingWidth * .5f + .5f, drawingHeight * .5f, drawingWidth, drawingHeight));
            //    }
            //    canvas.EndDrawing();
            //    MemoryStream stream = new MemoryStream();
            //    bitmap.Save(stream, ImageFormat.Png);
            //    return stream.ToArray();
            //}
        }

        public static BitmapImage ConvertToImageSource(byte[] imageBuffer)
        {
            var streamSource = new MemoryStream(imageBuffer);
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = streamSource;
            imageSource.EndInit();
            imageSource.Freeze();
            return imageSource;
        }

        public static bool IsImageValid(string imagePath)
        {
            bool isImageValid = true;
            System.Drawing.Image image = null;
            try
            {
                image = System.Drawing.Image.FromStream(new MemoryStream(File.ReadAllBytes(imagePath)));
                isImageValid = image != null && image.Width != 0 && image.Height != 0;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                isImageValid = false;
            }
            finally
            {
                if (image != null) image.Dispose();
            }
            return isImageValid;
        }

        //public static BitmapImage GetPreview(Style style, int width, int height)
        //{
        //    int drawingWidth = width == 0 ? 102 : width;
        //    int drawingHeight = height == 0 ? 32 : height;

        //    TextStyle textStyle = style as TextStyle;
        //    PointPlacement tempTextPlacement = PointPlacement.CenterRight;

        //    bool isValid = true;
        //    if (textStyle != null)
        //    {
        //        string drawingText = textStyle.TextColumnName;
        //        isValid = !string.IsNullOrEmpty(drawingText);
        //        if (isValid)
        //        {
        //            DrawingRectangleF drawingRegion = new GdiPlusGeoCanvas().MeasureText(drawingText, textStyle.Font);
        //            drawingWidth = (int)drawingRegion.Width;
        //            drawingHeight = (int)drawingRegion.Height;
        //            if (textStyle.Mask != null)
        //            {
        //                drawingWidth += (Convert.ToInt32(textStyle.Mask.OutlinePen.Width) + textStyle.MaskMargin) * 2;
        //                drawingHeight += (Convert.ToInt32(textStyle.Mask.OutlinePen.Width) + textStyle.MaskMargin) * 2;
        //            }
        //            tempTextPlacement = textStyle.PointPlacement;
        //            textStyle.PointPlacement = PointPlacement.Center;
        //        }
        //    }

        //    BitmapImage bitmapImage = new BitmapImage();
        //    if (style != null && isValid)
        //    {
        //        drawingWidth = drawingWidth == 0 ? 102 : drawingWidth;
        //        drawingHeight = drawingHeight == 0 ? 32 : drawingHeight;
        //        using (Bitmap bitmap = new Bitmap(drawingWidth, drawingHeight))
        //        {
        //            GdiPlusGeoCanvas canvas = new GdiPlusGeoCanvas();
        //            canvas.BeginDrawing(bitmap, new RectangleShape(-10, 10, 10, -10), GeographyUnit.DecimalDegree);
        //            style.DrawSample(canvas, new DrawingRectangleF(drawingWidth * .5f - 0.5f, drawingHeight * .5f - 0.5f, drawingWidth - 1, drawingHeight - 1));
        //            canvas.EndDrawing();

        //            MemoryStream stream = new MemoryStream();
        //            bitmap.Save(stream, ImageFormat.Png);

        //            bitmapImage.BeginInit();
        //            bitmapImage.StreamSource = stream;
        //            bitmapImage.EndInit();
        //            bitmapImage.Freeze();
        //        }
        //    }

        //    if (textStyle != null)
        //    {
        //        textStyle.PointPlacement = tempTextPlacement;
        //    }

        //    return bitmapImage;
        //}

        //public static BitmapImage GetPreview(Style style)
        //{
        //    return GetPreview(style, 102, 32);
        //}

        internal static string GetStyleWindowTitle(StyleProviderWindowType styleWindowType)
        {
            switch (styleWindowType)
            {
                case StyleProviderWindowType.AddInnerPredefinedPointStyle:
                    return GisEditor.LanguageManager.GetStringResource("PredefinedPointStyleWindowTitle");
                case StyleProviderWindowType.AddInnerSimplePointStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimplePointStyleWindowTitle");
                case StyleProviderWindowType.AddInnerCustomSymbolPointStyle:
                    return GisEditor.LanguageManager.GetStringResource("CustomPointStyleWindowTitle");
                case StyleProviderWindowType.AddInnerFontPointStyle:
                    return GisEditor.LanguageManager.GetStringResource("FontPointStyleWindowTitle");
                case StyleProviderWindowType.AddInnerPredefinedLineStyle:
                    return GisEditor.LanguageManager.GetStringResource("PredefinedLineStyleWindowTitle");
                case StyleProviderWindowType.AddInnerSimpleLineStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimpleLineStyleWindowTitle");
                case StyleProviderWindowType.AddInnerAdvancedLineStyle:
                    return GisEditor.LanguageManager.GetStringResource("AdvancedLineStyleWindowTitle");
                case StyleProviderWindowType.AddInnerPredefinedAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("PredefinedAreaStyleWindowTitle");
                case StyleProviderWindowType.AddInnerSimpleAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimpleAreaStyleWindowTitle");
                case StyleProviderWindowType.AddInnerAdvancedAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("AdvancedAreaStyleWindowTitle");
                case StyleProviderWindowType.AddInnerDotDensityAreaStyle:
                    return GisEditor.LanguageManager.GetStringResource("DotDensityAreaStyleWindowTitle");
                case StyleProviderWindowType.AddInnerClassBreakStyle:
                    return GisEditor.LanguageManager.GetStringResource("ClassbreakStyleWindowTitle");
                case StyleProviderWindowType.AddInnerIconTextStyle:
                    return GisEditor.LanguageManager.GetStringResource("SimpleTextStyleWindowTitle");
                case StyleProviderWindowType.AddInnerFilterStyle:
                    return GisEditor.LanguageManager.GetStringResource("FilterStylewindowTitle");
                default:
                    return GisEditor.LanguageManager.GetStringResource("InnerStyleWindowTitle");
            }
        }

        internal static string GetStyleWindowTitle(Style style)
        {
            //var provider = Globals.GetStyleProvider(style);
            var provider = GisEditor.StyleManager.GetStylePluginByStyle(style);
            if (provider != null) return provider.Name;
            else return "Style";
        }

        public static string NewName<T>(string prefix, IEnumerable<T> objectArray, Func<T, string> getNameFunc
            , StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            int max = 0;
            foreach (var item in objectArray)
            {
                var name = getNameFunc(item);
                if (name.StartsWith(prefix, comparison))
                {
                    int fromIndex = name.LastIndexOf(" ");
                    if (fromIndex > -1)
                    {
                        string suffix = name.Substring(fromIndex + 1);
                        int tmpNumber = Int32.MinValue;
                        Int32.TryParse(suffix, out tmpNumber);
                        if (tmpNumber > max)
                        {
                            max = tmpNumber;
                        }
                    }
                }
            }

            return String.Format(CultureInfo.InvariantCulture, "{0} {1}", prefix, max + 1);
        }

        public static IEnumerable<IGrouping<string, string>> GroupColumnValues(string requireColumnName
            , ShapeFileFeatureLayer featureLayer)
        {
            Collection<string> fieldValues = new Collection<string>();

            if (!String.IsNullOrEmpty(requireColumnName))
                featureLayer.SafeProcess(() =>
                {
                    string dbfPath = Path.ChangeExtension(featureLayer.ShapePathFilename, ".dbf");
                    if (File.Exists(dbfPath))
                    {
                        string orignalColumnName = requireColumnName;
                        using (GeoDbf dbf = new GeoDbf(dbfPath))
                        {
                            dbf.Open();
                            for (int i = 1; i <= dbf.RecordCount; i++)
                            {
                                fieldValues.Add(dbf.ReadFieldAsString(i, orignalColumnName));
                            }
                            dbf.Close();
                        }
                    }
                });

            return fieldValues.GroupBy(fieldValue => fieldValue);
        }

        public static int CalculateSpeceficValuesCount(string requireColumnName, string fieldValue, ShapeFileFeatureLayer featureLayer)
        {
            int count = 0;
            if (!String.IsNullOrEmpty(requireColumnName))
                featureLayer.SafeProcess(() =>
                {
                    string dbfPath = Path.ChangeExtension(featureLayer.ShapePathFilename, ".dbf");
                    if (File.Exists(dbfPath))
                    {
                        using (GeoDbf dbf = new GeoDbf(dbfPath))
                        {
                            dbf.Open();
                            string orignalColumnName = requireColumnName;
                            for (int i = 1; i <= dbf.RecordCount; i++)
                            {
                                var currentFieldValue = dbf.ReadFieldAsString(i, orignalColumnName);
                                if (currentFieldValue.Equals(fieldValue, StringComparison.Ordinal))
                                {
                                    count++;
                                }
                            }
                            dbf.Close();
                        }
                    }
                });
            return count;
        }
    }
}