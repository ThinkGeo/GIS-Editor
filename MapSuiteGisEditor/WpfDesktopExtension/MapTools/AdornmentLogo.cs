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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class AdornmentLogo : LogoMapTool
    {
        public static readonly DependencyProperty AdornmentLocationProperty =
            DependencyProperty.Register("AdornmentLocation", typeof(AdornmentLocation), typeof(AdornmentLogo), new UIPropertyMetadata(AdornmentLocation.LowerRight, new PropertyChangedCallback(OnAdornmentLocationPropertyChanged)));

        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register("Left", typeof(int), typeof(AdornmentLogo), new UIPropertyMetadata(0, new PropertyChangedCallback(OnLeftPropertyChanged)));

        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register("Top", typeof(int), typeof(AdornmentLogo), new UIPropertyMetadata(0, new PropertyChangedCallback(OnTopPropertyChanged)));

        [Obfuscation(Exclude = true)]
        private string logoPath;

        [Obfuscation(Exclude = true)]
        private int left;

        [Obfuscation(Exclude = true)]
        private int top;

        [Obfuscation(Exclude = true)]
        private AdornmentLocation adornmentLocation;

        [Obfuscation(Exclude = true)]
        private bool isEnabled;

        [Obfuscation(Exclude = true)]
        private double width;

        [Obfuscation(Exclude = true)]
        private double height;

        [Obfuscation(Exclude = true)]
        private byte[] imageBinary;

        public AdornmentLogo()
        { }

        public AdornmentLocation AdornmentLocation
        {
            get
            {
                adornmentLocation = (AdornmentLocation)GetValue(AdornmentLocationProperty);
                return adornmentLocation;
            }
            set
            {
                adornmentLocation = value;
                SetValue(AdornmentLocationProperty, adornmentLocation);
            }
        }

        public int Left
        {
            get
            {
                left = (int)GetValue(LeftProperty);
                return left;
            }
            set
            {
                left = value;
                SetValue(LeftProperty, left);
            }
        }

        public int Top
        {
            get
            {
                top = (int)GetValue(TopProperty);
                return top;
            }
            set
            {
                top = value;
                SetValue(TopProperty, top);
            }
        }

        public string LogoPath
        {
            get { return logoPath; }
            set { logoPath = value; }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == "IsEnabled")
            {
                isEnabled = (bool)e.NewValue;
            }
            else if (e.Property.Name == "Width")
            {
                width = (double)e.NewValue;
            }
            else if (e.Property.Name == "Height")
            {
                height = (double)e.NewValue;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Image image = (Image)GetTemplateChild("LogoImage");
        }

        [OnGeodeserialized]
        private void SetValuesOnDeserialized()
        {
            AdornmentLocation = adornmentLocation;
            IsEnabled = isEnabled;
            Width = width;
            Height = height;
            //use tmp variable to store the correct value, bacuse Left = tmpLeft; will change top to 0
            int tmpLeft = left;
            int tmpTop = top;
            Left = tmpLeft;
            Top = tmpTop;

            if (IsEnabled && !string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
            {
                Source = GetImageSource(File.ReadAllBytes(LogoPath));
            }
            else if (imageBinary != null && imageBinary.Length > 0)
            {
                Source = GetImageSource(imageBinary);
            }
        }

        [OnGeoserializing]
        private void SetValuesOnSerializing()
        {
            var bitmapImage = Source as BitmapImage;
            if (bitmapImage != null)
            {
                BinaryReader binaryReader = new BinaryReader(bitmapImage.StreamSource);
                imageBinary = new byte[binaryReader.BaseStream.Length];
                bitmapImage.StreamSource.Seek(0, SeekOrigin.Begin);
                imageBinary = binaryReader.ReadBytes(imageBinary.Length);
            }
        }

        private static void OnLeftPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AdornmentLogo currentLogo = (AdornmentLogo)sender;
            currentLogo.Margin = new Thickness((int)e.NewValue, currentLogo.Top, -(int)e.NewValue, -currentLogo.Top);
        }

        private static void OnTopPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AdornmentLogo currentLogo = (AdornmentLogo)sender;
            currentLogo.Margin = new Thickness(currentLogo.Left, (int)e.NewValue, -currentLogo.Left, -(int)e.NewValue);
        }

        private static void OnAdornmentLocationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AdornmentLogo currentLogo = (AdornmentLogo)sender;
            AdornmentLocation newLocation = (AdornmentLocation)e.NewValue;
            switch (newLocation)
            {
                case AdornmentLocation.UseOffsets:
                case AdornmentLocation.UpperLeft:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Left;
                    currentLogo.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case AdornmentLocation.UpperCenter:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Center;
                    currentLogo.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case AdornmentLocation.UpperRight:
                    currentLogo.VerticalAlignment = VerticalAlignment.Top;
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                case AdornmentLocation.CenterLeft:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Left;
                    currentLogo.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case AdornmentLocation.Center:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Center;
                    currentLogo.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case AdornmentLocation.CenterRight:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Right;
                    currentLogo.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case AdornmentLocation.LowerLeft:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Left;
                    currentLogo.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case AdornmentLocation.LowerCenter:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Center;
                    currentLogo.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case AdornmentLocation.LowerRight:
                default:
                    currentLogo.HorizontalAlignment = HorizontalAlignment.Right;
                    currentLogo.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
            }
        }

        private static ImageSource GetImageSource(byte[] imageBytes)
        {
            MemoryStream streamSource = new MemoryStream(imageBytes);
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = streamSource;
            imageSource.EndInit();
            imageSource.Freeze();

            return imageSource;
        }
    }
}