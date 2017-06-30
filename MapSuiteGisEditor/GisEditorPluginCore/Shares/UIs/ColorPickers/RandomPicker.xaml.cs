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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class RandomPicker : UserControl
    {
        private static readonly int maxH = 360;
        private static readonly int maxV = 100;
        private static readonly int maxS = 1;
        private static readonly double cellWidth = 1;
        private static readonly double cellHeight = 1;
        private bool isColorChangedByMouse;

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(RandomPicker), new UIPropertyMetadata(Colors.Transparent, new PropertyChangedCallback(SelectedColorPropertyChanged)));

        public RandomPicker()
        {
            InitializeComponent();
            InitializeColorPanel();
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        private void InitializeColorPanel()
        {
            //MainImage.Source = new RenderTargetBitmap((int)(maxH * cellWidth), (int)(maxV * cellHeight), 96, 96, PixelFormats.Pbgra32);
            //Task task = new Task(() =>
            //{
            //    DrawingVisual drawingVisual = new DrawingVisual();
            //    DrawingContext context = drawingVisual.RenderOpen();
            //    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)(maxH * cellWidth), (int)(maxV * cellHeight), 96, 96, PixelFormats.Pbgra32);

            //    for (int h = 0; h < maxH; h++)
            //    {
            //        for (int v = 0; v < maxV; v++)
            //        {
            //            HSL hsl = new HSL(h, maxS, v / 100d);
            //            Color color = hsl.ToColor();
            //            SolidColorBrush b = new SolidColorBrush(color);
            //            context.DrawRectangle(b, new Pen(), new Rect(h * cellWidth, (maxV - v) * cellHeight, cellWidth, cellHeight));
            //        }
            //    }

            //    context.Close();
            //    bitmap.Render(drawingVisual);
            //    Application.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        PngBitmapEncoder encoder = new PngBitmapEncoder();
            //        encoder.Frames.Add(BitmapFrame.Create(bitmap));
            //        FileStream stream = File.Create(@"C:\Users\woodyyan\Desktop\1.png");
            //        encoder.Save(stream);
            //        stream.Close();
            //        MainImage.Source = bitmap;
            //    });
            //});

            //task.RunSynchronously();

            MainImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/GisEditorPluginCore;component/Images/RandomPickerBG.png"));
        }

        private static void SelectedColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RandomPicker picker = (RandomPicker)sender;
            if (e.NewValue != null && !picker.isColorChangedByMouse)
            {
                Color newColor = (Color)e.NewValue;
                picker.MoveCursorToColor(newColor);
            }
        }

        private void MainImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(MainImage);

            try
            {
                isColorChangedByMouse = true;
                MoveCursor(position);
                SelectedColor = PositionToColor(position, MainImage.ActualWidth, MainImage.ActualHeight);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                isColorChangedByMouse = false;
            }
        }

        private void MoveCursorToColor(Color color)
        {
            Point position = ColorToPosition(color, ActualWidth, ActualHeight);
            MoveCursor(position);
        }

        private void MoveCursor(Point position)
        {
            PickerCursor.Margin = new Thickness(position.X, position.Y, 0, 0);
        }

        private static Color PositionToColor(Point position, double imageWidth, double imageHeight)
        {
            int h = (int)(position.X * maxH / imageWidth);
            double v = (imageHeight - position.Y) * maxV / imageHeight;
            HSL hsl = new HSL(h, maxS, v / 100d);
            Color color = hsl.ToColor();
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static Point ColorToPosition(Color color, double imageWidth, double imageHeight)
        {
            RGB rgb = new RGB(color.R, color.G, color.B);
            HSL hsl = new HSL();
            ThinkGeo.MapSuite.GisEditor.Plugins.ColorConverter.RGB2HSL(rgb, hsl);
            double x = hsl.Hue * imageWidth / maxH;
            double y = imageHeight - hsl.Luminance * 100d * imageHeight / maxV;
            return new Point(x, y);
        }
    }
}