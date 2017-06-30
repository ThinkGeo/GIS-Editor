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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DrawingLinearGradientBrushPicker : Control
    {
        private static readonly Color startColor = Color.White;
        private static readonly Color endColor = Color.White;
        private const double radius = 30d;
        private Arrow startArrow;
        private Arrow endArrow;
        private System.Windows.Shapes.Rectangle stopColorPanel;
        private StopType stopType;
        private Numeric alphaNumeric;
        private System.Windows.Shapes.Ellipse stopAnglePanel;
        private System.Windows.Shapes.Line stopAngleArrow;

        public static readonly DependencyProperty SelectedBrushProperty =
            DependencyProperty.Register("SelectedBrush", typeof(LinearGradientBrushEntity), typeof(DrawingLinearGradientBrushPicker), new UIPropertyMetadata(CreateDefaultBrush(), new PropertyChangedCallback(SelectedBrushChangedHandler)));

        public static readonly DependencyProperty PreviewImageProperty =
            DependencyProperty.Register("PreviewImage", typeof(System.Windows.Media.ImageSource), typeof(DrawingLinearGradientBrushPicker), new UIPropertyMetadata(null));

        public event EventHandler SelectedBrushChanged;
        private bool stopColorChangedEvent;

        public DrawingLinearGradientBrushPicker()
        {
            DefaultStyleKey = typeof(DrawingLinearGradientBrushPicker);
        }

        public System.Windows.Media.ImageSource PreviewImage
        {
            get { return (System.Windows.Media.ImageSource)GetValue(PreviewImageProperty); }
            set { SetValue(PreviewImageProperty, value); }
        }

        private void SelectedBrush_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Angle")
            {
                double newAngle = SelectedBrush.Angle;
                double x2 = radius + radius * Math.Cos(newAngle * Math.PI / 180d);
                double y2 = radius - radius * Math.Sin(newAngle * Math.PI / 180d);

                if (stopAngleArrow != null)
                {
                    stopAngleArrow.X2 = x2;
                    stopAngleArrow.Y2 = y2;
                }
            }
            else if (e.PropertyName == "EndColor" && stopColorPanel != null)
            {
                stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.EndColor);
            }
            else if (e.PropertyName == "StartColor" && stopColorPanel != null)
            {
                stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.StartColor);
            }

            SetPreviewImage();
        }

        private void SetPreviewImage()
        {
            Bitmap previewBitmap = new Bitmap(50, 50);
            Graphics previewCanvas = Graphics.FromImage(previewBitmap);
            MemoryStream previewStream = new MemoryStream();

            try
            {
                previewCanvas.FillRectangle(new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 50, 50),
                    SelectedBrush.StartColor,
                    SelectedBrush.EndColor,
                    (float)SelectedBrush.Angle), new Rectangle(0, 0, 50, 50));

                previewBitmap.Save(previewStream, System.Drawing.Imaging.ImageFormat.Png);
                System.Windows.Media.Imaging.BitmapImage imageSource = new System.Windows.Media.Imaging.BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = previewStream;
                imageSource.EndInit();

                PreviewImage = imageSource;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                previewBitmap.Dispose();
            }

            if (!stopColorChangedEvent)
            {
                OnSelectedBrushChanged();
            }
        }

        public LinearGradientBrushEntity SelectedBrush
        {
            get { return (LinearGradientBrushEntity)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            startArrow = (Arrow)GetTemplateChild("StartArrow");
            endArrow = (Arrow)GetTemplateChild("EndArrow");
            stopColorPanel = (System.Windows.Shapes.Rectangle)GetTemplateChild("StopColorPanel");
            alphaNumeric = (Numeric)GetTemplateChild("nA");
            stopAnglePanel = (System.Windows.Shapes.Ellipse)GetTemplateChild("StopAnglePanel");
            stopAngleArrow = (System.Windows.Shapes.Line)GetTemplateChild("StopAngleArrow");
            SelectedBrush.PropertyChanged += new PropertyChangedEventHandler(SelectedBrush_PropertyChanged);

            alphaNumeric.ValueChanged += new RoutedEventHandler(alphaNumeric_ValueChanged);

            stopColorPanel.MouseLeftButtonUp += new MouseButtonEventHandler(ArrowDoubleClick);
            startArrow.MouseDoubleClick += new MouseButtonEventHandler(ArrowDoubleClick);
            endArrow.MouseDoubleClick += new MouseButtonEventHandler(ArrowDoubleClick);

            startArrow.MouseLeftButtonUp += new MouseButtonEventHandler(ArrowMouseUp);
            endArrow.MouseLeftButtonUp += new MouseButtonEventHandler(ArrowMouseUp);

            stopAnglePanel.MouseDown += new MouseButtonEventHandler(Ellipse_MouseDown);
            stopAnglePanel.MouseMove += new MouseEventHandler(Ellipse_MouseMove);

            stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.StartColor);
            alphaNumeric.Value = SelectedBrush.StartColor.A;
            stopType = StopType.Start;

            FillAngleArrow();
            //SetPreviewImage();
        }

        protected void OnSelectedBrushChanged()
        {
            EventHandler handler = SelectedBrushChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        private void alphaNumeric_ValueChanged(object sender, RoutedEventArgs e)
        {
            int alpha = (int)((Numeric)sender).Value;
            switch (stopType)
            {
                case StopType.Start:
                    SelectedBrush.StartColor = Color.FromArgb(alpha, SelectedBrush.StartColor);
                    SetStopColor(SelectedBrush.StartColor);
                    break;
                case StopType.End:
                    SelectedBrush.EndColor = Color.FromArgb(alpha, SelectedBrush.EndColor);
                    SetStopColor(SelectedBrush.EndColor);
                    break;
            }
        }

        //private void ShowColorPickerHandler(object sender, MouseButtonEventArgs e)
        //{
        //    NormalColorPickerWindow window = new NormalColorPickerWindow();
        //    if (window.ShowDialog().GetValueOrDefault())
        //    {
        //        switch (stopType)
        //        {
        //            case StopType.Start:
        //                stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.StartColor);
        //                break;
        //            case StopType.End:
        //                stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.EndColor);
        //                break;
        //        }

        //        SetPreviewImage();
        //    }
        //}

        private void ArrowMouseUp(object sender, MouseButtonEventArgs e)
        {
            stopType = ((Arrow)sender).Name == "StartArrow" ? StopType.Start : StopType.End;
            switch (stopType)
            {
                case StopType.Start:
                    stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.StartColor);
                    alphaNumeric.Value = SelectedBrush.StartColor.A;
                    break;
                case StopType.End:
                    stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(SelectedBrush.EndColor);
                    alphaNumeric.Value = SelectedBrush.EndColor.A;
                    break;
            }
        }

        private void ArrowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                NormalColorPickerWindow colorPicker = new NormalColorPickerWindow();

                if (sender is Arrow)
                {
                    colorPicker.ColorPicker.SelectedColor = ((System.Windows.Media.SolidColorBrush)((Arrow)sender).Background).Color;
                }
                else if (sender is System.Windows.Shapes.Rectangle)
                {
                    colorPicker.ColorPicker.SelectedColor = ((System.Windows.Media.SolidColorBrush)((System.Windows.Shapes.Rectangle)sender).Fill).Color;
                }

                if (colorPicker.ShowDialog().GetValueOrDefault())
                {
                    switch (stopType)
                    {
                        case StopType.Start:
                            SelectedBrush.StartColor = DrawingColorToBrushConverter.ConvertBack(colorPicker.SelectedColorBrush);
                            SetStopColor(SelectedBrush.StartColor);
                            break;
                        case StopType.End:
                            SelectedBrush.EndColor = DrawingColorToBrushConverter.ConvertBack(colorPicker.SelectedColorBrush);
                            SetStopColor(SelectedBrush.EndColor);
                            break;
                    }

                    SetPreviewImage();
                }
            }));
        }

        private static LinearGradientBrushEntity CreateDefaultBrush()
        {
            LinearGradientBrushEntity brush = new LinearGradientBrushEntity
            {
                StartColor = startColor,
                EndColor = endColor,
                Angle = 0
            };

            return brush;
        }

        private void RewirePropertyChangedEvent()
        {
            SelectedBrush.PropertyChanged -= new PropertyChangedEventHandler(SelectedBrush_PropertyChanged);
            SelectedBrush.PropertyChanged += new PropertyChangedEventHandler(SelectedBrush_PropertyChanged);
        }

        private static void SelectedBrushChangedHandler(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DrawingLinearGradientBrushPicker currentInstance = (DrawingLinearGradientBrushPicker)sender;
            currentInstance.RewirePropertyChangedEvent();
            double newAngle = currentInstance.SelectedBrush.Angle;
            double x2 = radius + radius * Math.Cos(newAngle * Math.PI / 180d);
            double y2 = radius - radius * Math.Sin(newAngle * Math.PI / 180d);

            if (currentInstance.stopAngleArrow != null)
            {
                currentInstance.stopAngleArrow.X2 = x2;
                currentInstance.stopAngleArrow.Y2 = y2;
            }

            currentInstance.SetStopColor(currentInstance.stopType == StopType.End ? currentInstance.SelectedBrush.EndColor : currentInstance.SelectedBrush.StartColor);
            currentInstance.SetPreviewImage();
        }

        private void SetStopColor(Color color)
        {
            if (stopColorPanel != null)
            {
                stopColorPanel.Fill = DrawingColorToBrushConverter.Convert(color);
                alphaNumeric.Value = color.A;
                OnSelectedBrushChanged();
            }
        }

        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetAngle(e.GetPosition((UIElement)sender));
        }

        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetAngle(e.GetPosition((UIElement)sender));
            }
        }

        private void SetAngle(System.Windows.Point mousePosition)
        {
            double x = mousePosition.X - radius;
            double y = radius - mousePosition.Y;
            double angle = (int)(Math.Atan2(y, x) * 180 / Math.PI);
            if (angle < 0) angle += 360;
            SelectedBrush.Angle = (int)angle;
        }

        private void FillAngleArrow()
        {
            double newAngle = SelectedBrush.Angle;
            double x2 = radius + radius * Math.Cos(newAngle * Math.PI / 180d);
            double y2 = radius - radius * Math.Sin(newAngle * Math.PI / 180d);

            if (stopAngleArrow != null)
            {
                stopAngleArrow.X2 = x2;
                stopAngleArrow.Y2 = y2;
            }
        }

        private enum StopType { Start = 0, End = 1 }

        internal void SyncBaseColor(Color color)
        {
            stopColorChangedEvent = true;
            SelectedBrush.StartColor = color;
            stopColorChangedEvent = false;
        }
    }
}