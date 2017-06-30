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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GeoStyleViewModel : ViewModelBase
    {
        private string name;
        [NonSerialized]
        private ImageSource previewSource;
        private Style geoStyle;

        public GeoStyleViewModel(string name = null, Style geoStyle = null)
        {
            this.name = name;
            this.GeoStyle = geoStyle;
        }

        public string Name
        {
            get { return name; }
            set { name = value; RaisePropertyChanged(()=>Name); }
        }

        public ImageSource PreviewSource
        {
            get { return previewSource; }
            set { previewSource = value; RaisePropertyChanged(()=>PreviewSource); }
        }

        public Style GeoStyle
        {
            get { return geoStyle; }
            set
            {
                geoStyle = value; RaisePropertyChanged(()=>GeoStyle);
                PreviewSource = GetPreviewSourceFromStyle(geoStyle);
            }
        }

        private ImageSource GetPreviewSourceFromStyle(Style style)
        {
            if (style != null)
            {
                System.Drawing.Bitmap nativeImage = new System.Drawing.Bitmap(20, 20);
                MemoryStream streamSource = new MemoryStream();

                try
                {
                    var geoCanvas = new PlatformGeoCanvas();
                    geoCanvas.BeginDrawing(nativeImage, new RectangleShape(-10, 10, 10, -10), GeographyUnit.DecimalDegree);
                    geoCanvas.Clear(new GeoSolidBrush(GeoColor.StandardColors.White));
                    style.DrawSample(geoCanvas);
                    geoCanvas.EndDrawing();

                    nativeImage.Save(streamSource, System.Drawing.Imaging.ImageFormat.Png);
                    BitmapImage imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.StreamSource = streamSource;
                    imageSource.EndInit();

                    return imageSource;
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    return new BitmapImage();
                }
                finally
                {
                    nativeImage.Dispose();
                }
            }
            else
            {
                return null;
            }
        }
    }
}