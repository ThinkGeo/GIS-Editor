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


using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ImagePrinterLayerAdapter : PrinterLayerAdapter
    {
        private NorthArrowMapTool northArrowMapTool;
        private AdornmentLogo adornmentLogo;

        public ImagePrinterLayerAdapter(AdornmentLogo adornmentLogo)
        {
            this.adornmentLogo = adornmentLogo;
        }

        public ImagePrinterLayerAdapter(NorthArrowMapTool northArrowMapTool)
        {
            this.northArrowMapTool = northArrowMapTool;
        }

        protected override PrinterLayer GetPrinterLayerFromActiveMapCore(RectangleShape boudingBox)
        {
            if ((northArrowMapTool != null && !string.IsNullOrEmpty(northArrowMapTool.ImagePath))
                || (adornmentLogo != null && !string.IsNullOrEmpty(adornmentLogo.LogoPath)))
            {
                GeoImage geoImage = null;

                double widthInPixel = 0, heightInPixel = 0;

                if (northArrowMapTool != null && !string.IsNullOrEmpty(northArrowMapTool.ImagePath))
                {
                    geoImage = GetGeoImage(northArrowMapTool);
                    widthInPixel = (int)northArrowMapTool.Width;
                    heightInPixel = (int)northArrowMapTool.Height;
                }
                else if (adornmentLogo != null && !string.IsNullOrEmpty(adornmentLogo.LogoPath))
                {
                    geoImage = GetGeoImage(adornmentLogo);
                    widthInPixel = (int)adornmentLogo.Width;
                    heightInPixel = (int)adornmentLogo.Height;
                }

                ImagePrinterLayer imagePrinterLayer = new ImagePrinterLayer(geoImage, 0, 0, PrintingUnit.Inch) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
                imagePrinterLayer.Open();
                AdornmentLocation location = GetLocation();
                double width = PrinterHelper.ConvertLength((double)widthInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double height = PrinterHelper.ConvertLength((double)heightInPixel, PrintingUnit.Point, PrintingUnit.Inch);
                double left = 0;
                double top = 0;
                if (northArrowMapTool != null)
                {
                    left = PrinterHelper.ConvertLength(northArrowMapTool.Margin.Left, PrintingUnit.Point, PrintingUnit.Inch);
                    top = PrinterHelper.ConvertLength(northArrowMapTool.Margin.Top, PrintingUnit.Point, PrintingUnit.Inch);
                }
                if (adornmentLogo != null)
                {
                    left = PrinterHelper.ConvertLength(adornmentLogo.Left, PrintingUnit.Point, PrintingUnit.Inch);
                    top = PrinterHelper.ConvertLength(adornmentLogo.Top, PrintingUnit.Point, PrintingUnit.Inch);
                }
                SetPosition(location, boudingBox, imagePrinterLayer, width, height, left, top);
                return imagePrinterLayer;
            }
            else return null;
        }

        protected override void LoadFromActiveMapCore(PrinterLayer printerlayer)
        {
            ImagePrinterLayer imagePrinterLayer = null;
            if ((imagePrinterLayer = printerlayer as ImagePrinterLayer) != null)
            {
                var logoTool = GisEditor.ActiveMap.MapTools.OfType<AdornmentLogo>().FirstOrDefault();
                var northTool = GisEditor.ActiveMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
                if (northTool != null && !string.IsNullOrEmpty(northTool.ImagePath))
                {
                    imagePrinterLayer.Image = GetGeoImage(northTool);
                }
                else if (logoTool != null && !string.IsNullOrEmpty(logoTool.LogoPath))
                {
                    imagePrinterLayer.Image = GetGeoImage(logoTool);
                }
            }
        }

        private AdornmentLocation GetLocation()
        {
            if (northArrowMapTool != null) return NorthArrowViewModel.GetAdornmentLocation(northArrowMapTool.HorizontalAlignment, northArrowMapTool.VerticalAlignment);
            else return adornmentLogo.AdornmentLocation;
        }

        private GeoImage GetGeoImage(NorthArrowMapTool northArrowMapTool)
        {
            if (northArrowMapTool.ImagePath.StartsWith("pack:"))
            {
                return GetGeoImage((northArrowMapTool.Content as Image).Source as BitmapImage);
            }
            else if (File.Exists(northArrowMapTool.ImagePath))
            {
                var imgStream = new MemoryStream(File.ReadAllBytes(northArrowMapTool.ImagePath));
                imgStream.Seek(0, SeekOrigin.Begin);
                return new GeoImage(imgStream);
            }
            else
            {
                var image = northArrowMapTool.Content as Image;
                BitmapImage bitmapImage = null;
                if (image != null && (bitmapImage = image.Source as BitmapImage) != null)
                {
                    return GetGeoImage(bitmapImage);
                }
                else return new GeoImage(new MemoryStream());
            }
        }

        private GeoImage GetGeoImage(AdornmentLogo adornmentLogo)
        {
            if (File.Exists(adornmentLogo.LogoPath))
            {
                var imgStream = new MemoryStream(File.ReadAllBytes(adornmentLogo.LogoPath));
                imgStream.Seek(0, SeekOrigin.Begin);
                return new GeoImage(imgStream);
            }
            else
            {
                var img = adornmentLogo.Source as BitmapImage;
                return GetGeoImage(img);
            }
        }

        private GeoImage GetGeoImage(BitmapImage image)
        {
            Stream imgStream = new MemoryStream();
            var stream = image.StreamSource;
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(imgStream);
            imgStream.Seek(0, SeekOrigin.Begin);
            return new GeoImage(imgStream);
        }
    }
}
