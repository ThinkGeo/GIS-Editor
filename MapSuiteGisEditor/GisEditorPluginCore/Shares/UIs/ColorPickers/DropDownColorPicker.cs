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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DropDownColorPicker : Control
    {
        private static Collection<DropDownColorPicker> dropDownColorPickers;
        private TabColorPicker colorPicker;
        private ToggleButton toggleButton;
        private Grid colorPickerPanel;
        private Brush tmpStroke;
        private Slider slider;

        public static readonly DependencyProperty PreviewSourceProperty = DependencyProperty.Register("PreviewSource", typeof(ImageSource), typeof(DropDownColorPicker), new UIPropertyMetadata(new BitmapImage()));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(int), typeof(DropDownColorPicker), new UIPropertyMetadata(1));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(DropDownColorPicker), new UIPropertyMetadata(new SolidColorBrush(Colors.DarkGray)));

        public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register("Alpha", typeof(int), typeof(DropDownColorPicker), new UIPropertyMetadata(255, new PropertyChangedCallback(OnAlphaPropertyChanged)));

        public static readonly DependencyProperty AlphaVisibilityProperty = DependencyProperty.Register("AlphaVisibility", typeof(Visibility), typeof(DropDownColorPicker), new UIPropertyMetadata());

        public static readonly DependencyProperty AlphaSliderWidthProperty = DependencyProperty.Register("AlphaSliderWidth", typeof(int), typeof(DropDownColorPicker), new UIPropertyMetadata());

        public static readonly DependencyProperty PartialEnabledProperty = DependencyProperty.Register("PartialEnabled", typeof(bool), typeof(DropDownColorPicker), new UIPropertyMetadata(true));

        public static readonly DependencyProperty PreviewSourceNameProperty = DependencyProperty.Register("PreviewSourceName", typeof(string), typeof(DropDownColorPicker), new UIPropertyMetadata(string.Empty));

        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register("SelectedBrush", typeof(GeoBrush), typeof(DropDownColorPicker), new UIPropertyMetadata(new GeoSolidBrush(GeoColor.StandardColors.White), new PropertyChangedCallback(OnSelectedBrushPropertyChanged)));

        public static readonly DependencyProperty IsDroppedProperty = DependencyProperty.Register("IsDropped", typeof(bool), typeof(DropDownColorPicker), new UIPropertyMetadata(false, new PropertyChangedCallback(IsDroppedPropertyChanged)));

        static DropDownColorPicker()
        {
            dropDownColorPickers = new Collection<DropDownColorPicker>();
        }

        public DropDownColorPicker()
        {
            DefaultStyleKey = typeof(DropDownColorPicker);
            IsSolidColorBrushTabEnabled = true;
            IsGradientColorBrushTabEnabled = true;
            IsHatchBrushTabEnabled = true;
            IsTextureBrushTabEnabled = true;
            AlphaVisibility = Visibility.Visible;
            AlphaSliderWidth = 150;
            PartialEnabled = true;
        }

        public bool IsSolidColorBrushTabEnabled { get; set; }

        public bool IsGradientColorBrushTabEnabled { get; set; }

        public bool IsHatchBrushTabEnabled { get; set; }

        public bool IsTextureBrushTabEnabled { get; set; }

        public ImageSource PreviewSource
        {
            get { return (ImageSource)GetValue(PreviewSourceProperty); }
            set { SetValue(PreviewSourceProperty, value); }
        }

        public int StrokeThickness
        {
            get { return (int)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public int Alpha
        {
            get { return (int)GetValue(AlphaProperty); }
            set { SetValue(AlphaProperty, value); }
        }

        public bool PartialEnabled
        {
            get { return (bool)GetValue(PartialEnabledProperty); }
            set { SetValue(PartialEnabledProperty, value); }
        }

        public Visibility AlphaVisibility
        {
            get { return (Visibility)GetValue(AlphaVisibilityProperty); }
            set { SetValue(AlphaVisibilityProperty, value); }
        }

        public int AlphaSliderWidth
        {
            get { return (int)GetValue(AlphaSliderWidthProperty); }
            set { SetValue(AlphaSliderWidthProperty, value); }
        }

        public string PreviewSourceName
        {
            get { return (string)GetValue(PreviewSourceNameProperty); }
            set
            {
                if (UseCustomPreviewName && !String.IsNullOrEmpty(CustomPreviewName))
                {
                    SetValue(PreviewSourceNameProperty, CustomPreviewName);
                }
                else
                {
                    SetValue(PreviewSourceNameProperty, value);
                }
            }
        }

        public GeoBrush SelectedBrush
        {
            get { return (GeoBrush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public bool IsDropped
        {
            get { return (bool)GetValue(IsDroppedProperty); }
            set { SetValue(IsDroppedProperty, value); }
        }

        public bool UseCustomPreviewName { get; set; }

        public string CustomPreviewName { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            colorPicker = GetTemplateChild("ColorPicker") as TabColorPicker;
            colorPicker.IsSolidColorBrushTabEnabled = IsSolidColorBrushTabEnabled;
            colorPicker.IsGradientColorBrushTabEnabled = IsGradientColorBrushTabEnabled;
            colorPicker.IsHatchBrushTabEnabled = IsHatchBrushTabEnabled;
            colorPicker.IsTextureBrushTabEnabled = IsTextureBrushTabEnabled;
            colorPicker.SelectedItemDoubleClick -= new MouseButtonEventHandler(ColorPicker_SelectedItemDoubleClick);
            colorPicker.SelectedItemDoubleClick += new MouseButtonEventHandler(ColorPicker_SelectedItemDoubleClick);

            toggleButton = GetTemplateChild("ToggleButton") as ToggleButton;
            slider = GetTemplateChild("Slider") as Slider;

            colorPickerPanel = GetTemplateChild("ColorPickerPanel") as Grid;
            if (colorPickerPanel != null)
            {
                colorPickerPanel.MouseEnter -= new MouseEventHandler(ColorPickerPanel_MouseEnter);
                colorPickerPanel.MouseEnter += new MouseEventHandler(ColorPickerPanel_MouseEnter);
                colorPickerPanel.MouseLeave -= new MouseEventHandler(ColorPickerPanel_MouseLeave);
                colorPickerPanel.MouseLeave += new MouseEventHandler(ColorPickerPanel_MouseLeave);
            }
        }

        private void ColorPickerPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (toggleButton.IsEnabled)
            {
                tmpStroke = Stroke;
                Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 183, 0));
                LinearGradientBrush background = new LinearGradientBrush();
                background.StartPoint = new Point(0, .1);
                background.EndPoint = new Point(0, .9);
                background.GradientStops.Add(new GradientStop(Color.FromArgb(255, 254, 243, 229), 0));
                background.GradientStops.Add(new GradientStop(Color.FromArgb(255, 254, 243, 229), 0.4));
                background.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 207, 104), 0.5));
                background.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 230, 164), 0.5));
                Background = background;
            }
        }

        private void ColorPickerPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (toggleButton.IsEnabled)
            {
                Stroke = tmpStroke;
                Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void ColorPicker_SelectedItemDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            toggleButton.IsChecked = false;
        }

        private static void OnAlphaPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownColorPicker picker = (DropDownColorPicker)sender;
            picker.UpdateAlpha();
        }

        private static void OnSelectedBrushPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownColorPicker picker = (DropDownColorPicker)sender;
            picker.RefreshPreviews();
            if (picker.slider != null)
            {
                picker.slider.IsEnabled = !(e.NewValue is GeoTextureBrush);
            }
        }

        private static void IsDroppedPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DropDownColorPicker currentPicker = (DropDownColorPicker)sender;
            if ((bool)e.NewValue)
            {
                if (!dropDownColorPickers.Contains(currentPicker))
                {
                    dropDownColorPickers.Add(currentPicker);
                }

                foreach (var item in dropDownColorPickers)
                {
                    if (item != currentPicker)
                    {
                        item.IsDropped = false;
                    }
                }
            }
            else if (currentPicker.colorPicker != null)
            {
                currentPicker.colorPicker.UnSelect();
            }
        }

        private void UpdateAlpha()
        {
            if (colorPicker != null)
            {
                if (colorPicker.SelectedBrush is GeoSolidBrush)
                {
                    colorPicker.SelectedBrush = new GeoSolidBrush(GeoColor.FromArgb(Alpha, ((GeoSolidBrush)SelectedBrush).Color.RedComponent, ((GeoSolidBrush)SelectedBrush).Color.GreenComponent, ((GeoSolidBrush)SelectedBrush).Color.BlueComponent));
                }
                else if (colorPicker.SelectedBrush is GeoLinearGradientBrush)
                {
                    GeoLinearGradientBrush gradientBrush = (GeoLinearGradientBrush)colorPicker.SelectedBrush;
                    colorPicker.SelectedBrush = new GeoLinearGradientBrush(GeoColor.FromArgb(Alpha, gradientBrush.StartColor.RedComponent, gradientBrush.StartColor.GreenComponent, gradientBrush.StartColor.BlueComponent), gradientBrush.EndColor, gradientBrush.DirectionAngle);
                }
                else if (colorPicker.SelectedBrush is GeoHatchBrush)
                {
                    GeoHatchBrush geoHatchBrush = (GeoHatchBrush)colorPicker.SelectedBrush;
                    GeoColor newForegroundColor = new GeoColor(Alpha, geoHatchBrush.ForegroundColor.RedComponent, geoHatchBrush.ForegroundColor.GreenComponent, geoHatchBrush.ForegroundColor.BlueComponent);
                    GeoColor newBackgroundColor = new GeoColor(Alpha, geoHatchBrush.BackgroundColor.RedComponent, geoHatchBrush.BackgroundColor.GreenComponent, geoHatchBrush.BackgroundColor.BlueComponent);
                    colorPicker.SelectedBrush = new GeoHatchBrush(geoHatchBrush.HatchStyle, newForegroundColor, newBackgroundColor);
                }
                RefreshPreviews();
            }
        }

        private void RefreshPreviews()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(20, 20);
            MemoryStream streamSource = new MemoryStream();
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);

            try
            {
                if (SelectedBrush is GeoSolidBrush)
                {
                    GeoColor geoColor = ((GeoSolidBrush)SelectedBrush).Color;
                    Alpha = geoColor.AlphaComponent;
                    ((GeoSolidBrush)SelectedBrush).Color = new GeoColor(Alpha, ((GeoSolidBrush)SelectedBrush).Color);
                    geoColor = ((GeoSolidBrush)SelectedBrush).Color;

                    PreviewSourceName = Color.FromArgb(geoColor.AlphaComponent, geoColor.RedComponent, geoColor.GreenComponent, geoColor.BlueComponent).ToString();
                    g.FillRectangle(new System.Drawing.SolidBrush(GeoColor2DrawingColorConverter.Convert(geoColor)), new System.Drawing.Rectangle(0, 0, 20, 20));
                }
                else if (SelectedBrush is GeoHatchBrush)
                {
                    PreviewSourceName = ((GeoHatchBrush)SelectedBrush).HatchStyle.ToString();
                    GeoHatchBrush geoHatchBrush = (GeoHatchBrush)SelectedBrush;
                    Alpha = geoHatchBrush.ForegroundColor.AlphaComponent;
                    System.Drawing.Drawing2D.HatchBrush hatchBrush = new System.Drawing.Drawing2D.HatchBrush(
                        GeoHatchStyle2DrawingHatchStyle.Convert(geoHatchBrush.HatchStyle),
                        GeoColor2DrawingColorConverter.Convert(geoHatchBrush.ForegroundColor),
                        GeoColor2DrawingColorConverter.Convert(geoHatchBrush.BackgroundColor));

                    g.FillRectangle(hatchBrush, new System.Drawing.Rectangle(0, 0, 20, 20));
                }
                else if (SelectedBrush is GeoTextureBrush)
                {
                    string fileName = ((GeoTextureBrush)SelectedBrush).GeoImage.GetPathFilename();
                    PreviewSourceName = new System.IO.FileInfo(fileName).Name;
                    System.Drawing.Image im = System.Drawing.Image.FromStream(((GeoTextureBrush)SelectedBrush).GeoImage.GetImageStream(new PlatformGeoCanvas()));
                    g.DrawImage(im, new System.Drawing.Rectangle(0, 0, 20, 20));
                }
                else if (SelectedBrush is GeoLinearGradientBrush)
                {
                    GeoLinearGradientBrush gradientBrush = (GeoLinearGradientBrush)SelectedBrush;
                    Alpha = gradientBrush.StartColor.AlphaComponent;
                    PreviewSourceName = String.Format("Gradients Angle:{2}",
                        GeoColor2MediaColorConverter.Convert(gradientBrush.StartColor),
                        GeoColor2MediaColorConverter.Convert(gradientBrush.EndColor),
                        gradientBrush.DirectionAngle);
                    System.Drawing.Drawing2D.LinearGradientBrush drawingGradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new System.Drawing.Rectangle(0, 0, 20, 20),
                        GeoColor2DrawingColorConverter.Convert(gradientBrush.StartColor),
                        GeoColor2DrawingColorConverter.Convert(gradientBrush.EndColor),
                        gradientBrush.DirectionAngle);

                    g.FillRectangle(drawingGradientBrush, new System.Drawing.Rectangle(0, 0, 20, 20));
                }

                g.Flush();
                bitmap.Save(streamSource, System.Drawing.Imaging.ImageFormat.Png);
                streamSource.Seek(0, SeekOrigin.Begin);
                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = streamSource;
                imageSource.EndInit();
                imageSource.Freeze();

                PreviewSource = imageSource;
            }
            finally
            {
                bitmap.Dispose();
                g.Dispose();
            }
        }
    }
}