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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NorthArrowMapTool : MapTool
    {
        [Obfuscation(Exclude = true)]
        private string imagePath;

        [Obfuscation(Exclude = true)]
        private double width;

        [Obfuscation(Exclude = true)]
        private double height;

        [Obfuscation(Exclude = true)]
        private double left;

        [Obfuscation(Exclude = true)]
        private double top;

        [Obfuscation(Exclude = true)]
        private HorizontalAlignment horizontalAlignment;

        [Obfuscation(Exclude = true)]
        private VerticalAlignment verticalAlignment;

        [Obfuscation(Exclude = true)]
        private byte[] imageBinary;

        public string ImagePath
        {
            get { return imagePath; }
            set { imagePath = value; }
        }

        public NorthArrowMapTool()
        {
            IsEnabled = true;
        }

        public NorthArrowMapTool(string imageSourcePath, double rotateAngle)
        {
            ImagePath = imagePath;
            Content = new Image { Source = new BitmapImage(new Uri(imageSourcePath, UriKind.RelativeOrAbsolute)) };
            Height = ((Image)Content).Source.Height;
            Width = ((Image)Content).Source.Width;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            Margin = new System.Windows.Thickness(20, 0, 0, 60);
            RenderTransform = new RotateTransform(rotateAngle);
            IsEnabled = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == "Width")
            {
                width = (double)e.NewValue;
            }
            else if (e.Property.Name == "Height")
            {
                height = (double)e.NewValue;
            }
            else if (e.Property.Name == "Margin")
            {
                Thickness newValue = (Thickness)e.NewValue;
                left = newValue.Left;
                top = newValue.Top;
            }
            else if (e.Property.Name == "HorizontalAlignment")
            {
                horizontalAlignment = (HorizontalAlignment)e.NewValue;
            }
            else if (e.Property.Name == "VerticalAlignment")
            {
                verticalAlignment = (VerticalAlignment)e.NewValue;
            }
        }

        [OnGeodeserialized]
        private void SetValuesOnDeserialized()
        {
            Width = width;
            Height = height;
            Margin = new Thickness(left, top, -left, -top);
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;

            if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
            {
                Content = new Image { Source = ImageHelper.GetImageSource(ImagePath, (int)Width, (int)Height) };
            }
            else if (imageBinary != null && imageBinary.Length > 0)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(imageBinary);
                bitmapImage.EndInit();
                Content = new Image { Source = bitmapImage };
            }
        }

        [OnGeoserializing]
        private void SetValuesOnSerializing()
        {
            var img = Content as Image;
            if (img != null)
            {
                var bitmapImage = img.Source as BitmapImage;
                if (bitmapImage != null)
                {
                    Stream stream = null;
                    if (bitmapImage.StreamSource != null)
                        stream = bitmapImage.StreamSource;
                    else
                        stream = new MemoryStream(File.ReadAllBytes(bitmapImage.UriSource.OriginalString));
                    stream.Seek(0, SeekOrigin.Begin);
                    var binaryReader = new BinaryReader(stream);
                    imageBinary = new byte[binaryReader.BaseStream.Length];
                    imageBinary = binaryReader.ReadBytes(imageBinary.Length);
                }
            }
        }
    }
}