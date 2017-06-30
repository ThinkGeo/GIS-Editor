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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class FontPicker : Control
    {
        private const int defaultFontSize = 24;
        private const int defaultPreviewSize = 45;
        public static readonly int FromCharactorIndex = 33;
        public static readonly int ToCharacterIndex = 255;

        public static readonly DependencyProperty CharactersProperty =
            DependencyProperty.Register("Characters", typeof(ObservableCollection<int>), typeof(FontPicker), new UIPropertyMetadata(GetDefaultCharacters()));

        public static readonly DependencyProperty SelectedCharacterIndexProperty =
            DependencyProperty.Register("SelectedCharacterIndex", typeof(int), typeof(FontPicker), new UIPropertyMetadata(33, new PropertyChangedCallback(PreviewImageChanged)));

        public static readonly DependencyProperty FontFamiliesProperty =
            DependencyProperty.Register("FontFamilies", typeof(ObservableCollection<FontFamily>), typeof(FontPicker), new UIPropertyMetadata(GetDefaultFontFamilies()));

        public static readonly DependencyProperty SelectedFontFamilyProperty =
            DependencyProperty.Register("SelectedFontFamily", typeof(FontFamily), typeof(FontPicker), new UIPropertyMetadata(Fonts.SystemFontFamilies.First(), new PropertyChangedCallback(PreviewImageChanged)));

        public static readonly DependencyProperty FontSizesProperty =
            DependencyProperty.Register("FontSizes", typeof(ObservableCollection<int>), typeof(FontPicker), new UIPropertyMetadata(GetDefaultFontSizes()));

        public static readonly DependencyProperty SelectedFontSizeProperty =
            DependencyProperty.Register("SelectedFontSize", typeof(int), typeof(FontPicker), new UIPropertyMetadata(defaultFontSize, new PropertyChangedCallback(PreviewImageChanged)));

        public static readonly DependencyProperty FontStylesProperty =
            DependencyProperty.Register("FontStyles", typeof(ObservableCollection<DrawingFontStyles>), typeof(FontPicker), new UIPropertyMetadata(GetDefaultFontStyle()));

        public static readonly DependencyProperty SelectedFontStyleProperty =
            DependencyProperty.Register("SelectedFontStyle", typeof(DrawingFontStyles), typeof(FontPicker), new UIPropertyMetadata(DrawingFontStyles.Regular, new PropertyChangedCallback(PreviewImageChanged)));

        public static readonly DependencyProperty PreviewImageProperty =
            DependencyProperty.Register("PreviewImage", typeof(BitmapImage), typeof(FontPicker), new UIPropertyMetadata(new BitmapImage()));

        static FontPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontPicker), new FrameworkPropertyMetadata(typeof(FontPicker)));
        }

        public ObservableCollection<int> Characters
        {
            get { return (ObservableCollection<int>)GetValue(CharactersProperty); }
            set { SetValue(CharactersProperty, value); }
        }

        public int SelectedCharacterIndex
        {
            get { return (int)GetValue(SelectedCharacterIndexProperty); }
            set { SetValue(SelectedCharacterIndexProperty, value); }
        }

        public ObservableCollection<FontFamily> FontFamilies
        {
            get { return (ObservableCollection<FontFamily>)GetValue(FontFamiliesProperty); }
            set { SetValue(FontFamiliesProperty, value); }
        }

        public FontFamily SelectedFontFamily
        {
            get { return (FontFamily)GetValue(SelectedFontFamilyProperty); }
            set { SetValue(SelectedFontFamilyProperty, value); }
        }

        public ObservableCollection<int> FontSizes
        {
            get { return (ObservableCollection<int>)GetValue(FontSizesProperty); }
            set { SetValue(FontSizesProperty, value); }
        }

        public int SelectedFontSize
        {
            get { return (int)GetValue(SelectedFontSizeProperty); }
            set { SetValue(SelectedFontSizeProperty, value); }
        }

        public ObservableCollection<DrawingFontStyles> FontStyles
        {
            get { return (ObservableCollection<DrawingFontStyles>)GetValue(FontStylesProperty); }
            set { SetValue(FontStylesProperty, value); }
        }

        public DrawingFontStyles SelectedFontStyle
        {
            get { return (DrawingFontStyles)GetValue(SelectedFontStyleProperty); }
            set { SetValue(SelectedFontStyleProperty, value); }
        }

        public BitmapImage PreviewImage
        {
            get { return (BitmapImage)GetValue(PreviewImageProperty); }
            set { SetValue(PreviewImageProperty, value); }
        }

        public static ObservableCollection<int> GetDefaultCharacters()
        {
            ObservableCollection<int> defaultCharactors = new ObservableCollection<int>();
            for (int i = FromCharactorIndex; i <= ToCharacterIndex; i++)
            {
                defaultCharactors.Add(i);
            }
            return defaultCharactors;
        }

        private static ObservableCollection<FontFamily> GetDefaultFontFamilies()
        {
            ObservableCollection<FontFamily> defaultFontFamilies = new ObservableCollection<FontFamily>();
            foreach (var fontFamily in Fonts.SystemFontFamilies.OrderBy(f => { return f.Source.First(); }))
            {
                defaultFontFamilies.Add(fontFamily);
            }

            return defaultFontFamilies;
        }

        private static object GetDefaultFontSizes()
        {
            return new ObservableCollection<int>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
        }

        private static object GetDefaultFontStyle()
        {
            ObservableCollection<DrawingFontStyles> fontStyles = new ObservableCollection<DrawingFontStyles>();
            fontStyles.Add(DrawingFontStyles.Bold);
            fontStyles.Add(DrawingFontStyles.Italic);
            fontStyles.Add(DrawingFontStyles.Regular);
            fontStyles.Add(DrawingFontStyles.Strikeout);
            fontStyles.Add(DrawingFontStyles.Underline);

            return fontStyles;
        }

        private static void PreviewImageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            FontPicker currentInstance = (FontPicker)sender;
            int selectedCharIndex = currentInstance.SelectedCharacterIndex;

            PointStyle pointStyle = new PointStyle(
                new GeoFont(currentInstance.SelectedFontFamily.Source, defaultFontSize, currentInstance.SelectedFontStyle),
                currentInstance.SelectedCharacterIndex,
                new GeoSolidBrush(GeoColor.StandardColors.Black));

            System.Drawing.Bitmap nativeImage = new System.Drawing.Bitmap(defaultPreviewSize, defaultPreviewSize);
            var geoCanvas = new PlatformGeoCanvas();

            try
            {
                geoCanvas.BeginDrawing(nativeImage, new RectangleShape(-90, 90, 90, -90), GeographyUnit.DecimalDegree);
                pointStyle.DrawSample(geoCanvas);
                geoCanvas.EndDrawing();

                MemoryStream streamSource = new MemoryStream();
                nativeImage.Save(streamSource, System.Drawing.Imaging.ImageFormat.Png);

                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = streamSource;
                imageSource.EndInit();

                currentInstance.PreviewImage = imageSource;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                nativeImage.Dispose();
            }
        }
    }
}