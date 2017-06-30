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
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ColorPanel : Control, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.RegisterAttached("SelectedColor", typeof(Color), typeof(ColorPanel), new PropertyMetadata(Colors.Transparent, new PropertyChangedCallback(SelectedColorPropertyChanged)));

        public event EventHandler<SelectedColorChangedColorPanelEventArgs> SelectedColorChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        private BitmapImage bitmapSource;
        private Canvas colorCanvas;
        private Rectangle rectCursor;
        internal bool isColorChangedByMouse;
        private BackgroundWorker repositeThread;
        private Collection<System.Drawing.Color> colorSourceMatrix;

        public ColorPanel()
        {
            DefaultStyleKey = typeof(ColorPanel);
            SelectedColor = Colors.White;
            repositeThread = new BackgroundWorker();
            repositeThread.WorkerSupportsCancellation = true;
            repositeThread.DoWork += new DoWorkEventHandler(repositeThread_DoWork);
            repositeThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(repositeThread_RunWorkerCompleted);
        }

        private void repositeThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && e.Result != null)
            {
                Point newPosition = (Point)e.Result;
                MovePointTo(newPosition);
            }
            else
            {
                rectCursor.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void repositeThread_DoWork(object sender, DoWorkEventArgs e)
        {
            if (colorSourceMatrix == null) return;
            Tuple<Color, double, double> bitmapSourceMatrix = (Tuple<Color, double, double>)e.Argument;

            int i = 0;
            foreach (System.Drawing.Color tmpColor in colorSourceMatrix)
            {
                if (e.Cancel) { return; }
                Color tmpMediaColor = Color.FromRgb(tmpColor.R, tmpColor.G, tmpColor.B);
                if (SimmilarColor(tmpMediaColor, bitmapSourceMatrix.Item1))
                {
                    break;
                }
                i++;
            }

            if (i < colorSourceMatrix.Count)
            {
                int x = (int)((double)i / bitmapSourceMatrix.Item3);
                int y = (int)((double)i % bitmapSourceMatrix.Item2);
                e.Result = new Point(x, y);
            }
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            rectCursor = (Rectangle)GetTemplateChild("RectCursor");
            bitmapSource = (BitmapImage)GetTemplateChild("BitmapSource");
            colorCanvas = (Canvas)GetTemplateChild("ColorCanvas");
            colorCanvas.MouseDown -= new System.Windows.Input.MouseButtonEventHandler(colorCanvas_MouseDown);
            colorCanvas.MouseMove -= new System.Windows.Input.MouseEventHandler(colorCanvas_MouseMove);
            colorCanvas.MouseDown += new System.Windows.Input.MouseButtonEventHandler(colorCanvas_MouseDown);
            colorCanvas.MouseMove += new System.Windows.Input.MouseEventHandler(colorCanvas_MouseMove);

            if (colorSourceMatrix == null)
            {
                BinaryFormatter bf = new BinaryFormatter();
                Stream stream = Application.GetResourceStream(new Uri("/GisEditorPluginCore;component/Images/colors.res", UriKind.RelativeOrAbsolute)).Stream;
                colorSourceMatrix = (Collection<System.Drawing.Color>)bf.Deserialize(stream);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void OnSelectedColorChanged(Color newColor, Color oldColor)
        {
            EventHandler<SelectedColorChangedColorPanelEventArgs> handler = SelectedColorChanged;
            if (handler != null) handler(this, new SelectedColorChangedColorPanelEventArgs(newColor, oldColor));
        }

        private void colorCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                SetColorFromPoint(e.GetPosition(colorCanvas));
            }
        }

        private void colorCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetColorFromPoint(e.GetPosition(colorCanvas));
        }

        private void SetColorFromPoint(Point point)
        {
            isColorChangedByMouse = true;
            if (point.X >= 0 && point.Y >= 0 && (int)point.X < (int)bitmapSource.Width && (int)point.Y < (int)bitmapSource.Height)
            {
                MovePointTo(point);
                Color pointedColor = GetColorFromPoint(point, bitmapSource);
                SelectedColor = Color.FromArgb(SelectedColor.A, pointedColor.R, pointedColor.G, pointedColor.B);
            }
        }

        private static Color GetColorFromPoint(Point point, BitmapImage bitmapSource)
        {
            if (point.X >= 0 && point.Y >= 0 && (int)point.X < (int)bitmapSource.Width && (int)point.Y < (int)bitmapSource.Height)
            {
                CroppedBitmap cb = new CroppedBitmap(bitmapSource, new Int32Rect((int)point.X, (int)point.Y, 1, 1));
                byte[] color = new byte[4];
                cb.CopyPixels(color, 4, 0);
                return Color.FromArgb(255, color[2], color[1], color[0]);
            }
            else
            {
                return Colors.White;
            }
        }

        private void MovePointTo(Point point)
        {
            rectCursor.SetValue(Canvas.LeftProperty, point.X);
            rectCursor.SetValue(Canvas.TopProperty, point.Y);
            rectCursor.Visibility = System.Windows.Visibility.Visible;
        }

        private static void SelectedColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Color newColor = (Color)e.NewValue;
            ColorPanel colorPanel = (ColorPanel)sender;
            colorPanel.OnSelectedColorChanged((Color)e.NewValue, (Color)e.OldValue);

            if (colorPanel.repositeThread != null && colorPanel.colorSourceMatrix != null)
            {
                colorPanel.repositeThread.CancelAsync();
                while (colorPanel.repositeThread.IsBusy)
                {
                    System.Windows.Forms.Application.DoEvents();
                }

                if (!colorPanel.isColorChangedByMouse)
                {
                    Tuple<Color, double, double> parameters = new Tuple<Color, double, double>(colorPanel.SelectedColor, colorPanel.bitmapSource.Width, colorPanel.bitmapSource.Height);
                    colorPanel.repositeThread.RunWorkerAsync(parameters);
                }
                colorPanel.isColorChangedByMouse = false;
            }
        }

        private bool SimmilarColor(Color pointColor, Color selectedColor)
        {
            int diff = Math.Abs(pointColor.R - selectedColor.R) + Math.Abs(pointColor.G - selectedColor.G) + Math.Abs(pointColor.B - selectedColor.B);
            if (diff < 20) return true;
            else return false;
        }
    }
}