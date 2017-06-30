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
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class ImageHelper
    {
        public static BitmapImage GetImageSource(string imageSourcePath, int width, int height)
        {
            if (imageSourcePath == null)
            {
                return new BitmapImage();
            }

            string extension = Path.GetExtension(imageSourcePath);
            if (extension.Equals(".emf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".wmf", StringComparison.OrdinalIgnoreCase))
            {
                MemoryStream ms = new MemoryStream();
                System.Drawing.Image image = null;
                System.Drawing.Bitmap bitmap = null;
                try
                {
                    Stream stream = null;
                    if (imageSourcePath.StartsWith("pack://application"))
                        stream = Application.GetResourceStream(new Uri(imageSourcePath, UriKind.RelativeOrAbsolute)).Stream;
                    else
                        stream = new MemoryStream(File.ReadAllBytes(imageSourcePath));

                    if (width == 0 || height == 0)
                    {
                        image = System.Drawing.Image.FromStream(stream);
                        int origianlWidth = image.Width;
                        int origianlHeight = image.Height;
                        bitmap = new System.Drawing.Bitmap(image, (int)(origianlWidth * 1.7), (int)(origianlHeight * 1.7));
                    }
                    else
                    {
                        image = System.Drawing.Image.FromStream(stream);
                        bitmap = new System.Drawing.Bitmap(image, width, height);
                    }
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    if (bitmap != null)
                        bitmap.Dispose();
                    if (image != null)
                        image.Dispose();
                }

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                return bitmapImage;
            }
            else
            {
                Stream stream = null;
                if (imageSourcePath.StartsWith("pack:")) stream = Application.GetResourceStream(new Uri(imageSourcePath, UriKind.RelativeOrAbsolute)).Stream;
                else stream = new MemoryStream(File.ReadAllBytes(imageSourcePath));
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public static BitmapImage GetImageSource(string imageSourcePath)
        {
            return GetImageSource(imageSourcePath, 0, 0);
        }

        public static BitmapImage GetImageSource(GeoBrush geoBrush)
        {
            BitmapImage result = null;

            System.Drawing.Bitmap nativeImage = new System.Drawing.Bitmap(16, 16);
            PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas();
            RectangleShape area = new RectangleShape(-90, 90, 90, -90);
            geoCanvas.BeginDrawing(nativeImage, area, GeographyUnit.DecimalDegree);
            try
            {
                geoCanvas.DrawArea(area, geoBrush, DrawingLevel.LevelOne);
                geoCanvas.EndDrawing();

                MemoryStream streamSource = new MemoryStream();
                nativeImage.Save(streamSource, System.Drawing.Imaging.ImageFormat.Png);

                result = new BitmapImage();
                result.BeginInit();
                result.StreamSource = streamSource;
                result.EndInit();
                result.Freeze();
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                nativeImage.Dispose();
            }

            return result;
        }
    }
}