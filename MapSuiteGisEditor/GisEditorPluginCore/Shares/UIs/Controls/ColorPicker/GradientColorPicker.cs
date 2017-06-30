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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GradientColorPicker : Control, INotifyPropertyChanged
    {
        private const double radius = 30d;
        private GradientBrushEntity gradientBrush;
        private Line stopAngleArrow;
        private Ellipse stopAnglePanel;
        private Rectangle stopColorPanel;
        private Canvas previewPanel;
        private Numeric nA, nL;
        private LinearGradientBrush linearGradientBrush;

        public event PropertyChangedEventHandler PropertyChanged;

        private ContextMenu stopCtx;

        public static readonly DependencyProperty SelectedGradientBrushProperty =
            DependencyProperty.Register("SelectedGradientBrush", typeof(Brush), typeof(GradientColorPicker));

        public GradientColorPicker()
        {
            DefaultStyleKey = typeof(GradientColorPicker);
            GradientBrush = new GradientBrushEntity();
            DataContext = this;

            MenuItem editStopMenuItem = new MenuItem { Header = GisEditor.LanguageManager.GetStringResource("GradientColorPickerEditStopHeader") };
            editStopMenuItem.Click += new RoutedEventHandler((s, e) => { SetCurrentStopColor(); });

            MenuItem removeStopMenuItem = new MenuItem { Header = GisEditor.LanguageManager.GetStringResource("GradientColorPickerRemoveStopHeader") };
            removeStopMenuItem.Click += new RoutedEventHandler((s, e) => { RemoveCurrentStop(); });

            stopCtx = new ContextMenu();
            stopCtx.Items.Add(editStopMenuItem);
            stopCtx.Items.Add(removeStopMenuItem);
        }

        public GradientBrushEntity GradientBrush
        {
            get { return gradientBrush; }
            set
            {
                gradientBrush = value;
                gradientBrush.PropertyChanged -= new PropertyChangedEventHandler(gradientBrush_PropertyChanged);
                gradientBrush.PropertyChanged += new PropertyChangedEventHandler(gradientBrush_PropertyChanged);
                gradientBrush.Angle = value.Angle;

                if (previewPanel != null)
                {
                    previewPanel.Children.Clear();
                    linearGradientBrush.GradientStops.Clear();
                    foreach (var stop in gradientBrush.GradientStops)
                    {
                        AddAStop(stop);
                    }
                }

                OnPropertyChanged("GradientBrush");
            }
        }

        public Brush SelectedGradientBrush
        {
            get { return (Brush)GetValue(SelectedGradientBrushProperty); }
            set
            {
                LinearGradientBrush tmpGradientBrush = linearGradientBrush.Clone();
                double angle = (double)GradientBrush.Angle * Math.PI / 180d;
                double x = Math.Cos(angle);
                double y = Math.Sin(angle);
                tmpGradientBrush.StartPoint = new Point(.5 - x, .5 + y);
                tmpGradientBrush.EndPoint = new Point(.5 + x, .5 - y);
                SetValue(SelectedGradientBrushProperty, tmpGradientBrush);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            stopAnglePanel = (Ellipse)GetTemplateChild("StopAnglePanel");
            stopColorPanel = (Rectangle)GetTemplateChild("StopColorPanel");
            stopAngleArrow = (Line)GetTemplateChild("StopAngleArrow");
            previewPanel = (Canvas)GetTemplateChild("PreviewPanel");
            linearGradientBrush = (LinearGradientBrush)GetTemplateChild("LinearGradientBrush");
            nA = (Numeric)GetTemplateChild("nA");
            nL = (Numeric)GetTemplateChild("nL");

            stopAnglePanel.MouseDown += new MouseButtonEventHandler(Ellipse_MouseDown);
            stopAnglePanel.MouseMove += new MouseEventHandler(Ellipse_MouseMove);
            previewPanel.MouseDown += new MouseButtonEventHandler(Canvas_MouseDown);
            stopColorPanel.MouseLeftButtonDown += new MouseButtonEventHandler(Rectangle_MouseLeftButtonDown);
            nA.ValueChanged += new RoutedEventHandler(Numeric_OpacityValueChanged);
            nL.ValueChanged += new RoutedEventHandler(Numeric_LocationValueChanged);

            GradientBrushEntity entity = new GradientBrushEntity();
            entity.GradientStops.Add(new Arrow(Colors.White, 0));
            entity.GradientStops.Add(new Arrow(Colors.Black, 1));

            GradientBrush = entity;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void AddAStop(Arrow arrow)
        {
            arrow.ContextMenu = stopCtx;
            arrow.Background = new SolidColorBrush(arrow.Color);
            InitArrowEvents(arrow);
            arrow.SetValue(Canvas.LeftProperty, previewPanel.ActualWidth * arrow.Offset);
            previewPanel.Children.Add(arrow);
            linearGradientBrush.GradientStops.Add(arrow.GradientStop);
            GradientBrush.SelectedArrow = arrow;

            SelectedGradientBrush = linearGradientBrush;
        }

        private void AddAStop(double offset, Color color)
        {
            GradientStop stop = new GradientStop(color, offset);
            Arrow arrow = new Arrow();
            arrow.ContextMenu = stopCtx;
            arrow.Background = new SolidColorBrush(color);
            arrow.Offset = offset;
            InitArrowEvents(arrow);
            arrow.SetValue(Canvas.LeftProperty, previewPanel.ActualWidth * offset);
            arrow.GradientStop = stop;
            previewPanel.Children.Add(arrow);
            linearGradientBrush.GradientStops.Add(stop);
            GradientBrush.GradientStops.Add(arrow);
            GradientBrush.SelectedArrow = arrow;

            SelectedGradientBrush = linearGradientBrush;
        }

        private void InitArrowEvents(Arrow arrow)
        {
            MouseEventManager eventManager = new MouseEventManager(arrow);
            eventManager.MouseButtonDown += new EventHandler<MouseButtonEventArgs>(arrow_MouseDown);
            eventManager.MouseMove += new EventHandler<MouseEventArgs>(arrow_MouseMove);
            eventManager.MouseButtonUp += new EventHandler<MouseButtonEventArgs>(arrow_MouseUp);
            eventManager.MouseClick += new EventHandler<MouseButtonEventArgs>(eventManager_MouseClick);
            eventManager.MouseDoubleClick += new EventHandler<MouseButtonEventArgs>(arrow_MouseDoubleClick);
            arrow.ContextMenuOpening += new ContextMenuEventHandler(arrow_ContextMenuOpening);
        }

        private void eventManager_MouseClick(object sender, MouseButtonEventArgs e)
        {
            GradientBrush.SelectedArrow = (Arrow)sender;
        }

        private void arrow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            GradientBrush.SelectedArrow = (Arrow)sender;
        }

        private void gradientBrush_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Angle")
            {
                double newAngle = gradientBrush.Angle;
                double x2 = radius + radius * Math.Cos(newAngle * Math.PI / 180d);
                double y2 = radius - radius * Math.Sin(newAngle * Math.PI / 180d);

                if (stopAngleArrow != null)
                {
                    stopAngleArrow.X2 = x2;
                    stopAngleArrow.Y2 = y2;
                    SelectedGradientBrush = linearGradientBrush;
                }
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            double newX = e.GetPosition(previewPanel).X;
            double newOffset = newX / previewPanel.ActualWidth;

            Color nearestColor = Colors.White;
            GradientStop nearestStop = null;
            if (linearGradientBrush.GradientStops.Count > 0)
            {
                nearestStop = linearGradientBrush.GradientStops[0];
                nearestColor = nearestStop.Color;

                for (int i = 1; i < GradientBrush.GradientStops.Count; i++)
                {
                    if (Math.Abs(nearestStop.Offset - newOffset) > Math.Abs(linearGradientBrush.GradientStops[i].Offset - newOffset))
                    {
                        nearestStop = linearGradientBrush.GradientStops[i];
                        nearestColor = nearestStop.Color;
                    }
                }
            }

            AddAStop(newOffset, nearestColor);
        }

        private void arrow_MouseMove(object sender, MouseEventArgs e)
        {
            if (GradientBrush.SelectedArrow != null && GradientBrush.SelectedArrow.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMousePosition = e.GetPosition(previewPanel);
                double newX = currentMousePosition.X;

                if (newX >= 0 && newX < previewPanel.ActualWidth)
                {
                    if (currentMousePosition.Y > 30 || currentMousePosition.Y < -30)
                    {
                        RemoveCurrentStop();
                    }
                    else
                    {
                        GradientStop pairedStop = GradientBrush.SelectedArrow.GradientStop;
                        pairedStop.Offset = newX / previewPanel.ActualWidth;
                        GradientBrush.SelectedArrow.Offset = pairedStop.Offset;
                        GradientBrush.SelectedArrow.SetValue(Canvas.LeftProperty, newX);

                        SelectedGradientBrush = linearGradientBrush;
                    }
                }
            }
        }

        private void arrow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (GradientBrush.SelectedArrow != null)
            {
                GradientBrush.SelectedArrow.ReleaseMouseCapture();
            }
        }

        private void arrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            GradientBrush.SelectedArrow = (Arrow)sender;
            GradientBrush.SelectedArrow.CaptureMouse();

            e.Handled = true;
        }

        private void RemoveCurrentStop()
        {
            GradientStop selectedStop = GradientBrush.SelectedArrow.GradientStop;
            linearGradientBrush.GradientStops.Remove(selectedStop);
            GradientBrush.GradientStops.Remove(GradientBrush.SelectedArrow);
            previewPanel.Children.Remove(GradientBrush.SelectedArrow);
            if (GradientBrush.GradientStops.Count > 0) GradientBrush.SelectedArrow = GradientBrush.GradientStops[0];
            else GradientBrush.SelectedArrow = null;

            SelectedGradientBrush = linearGradientBrush;
        }

        private void Numeric_LocationValueChanged(object sender, RoutedEventArgs e)
        {
            if (GradientBrush.SelectedArrow != null)
            {
                GradientBrush.SelectedArrow.SetValue(Canvas.LeftProperty, previewPanel.ActualWidth * GradientBrush.SelectedArrow.Offset);
            }
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetCurrentStopColor();
            e.Handled = true;
        }

        private void arrow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetCurrentStopColor();
            e.Handled = true;
        }

        private void SetCurrentStopColor()
        {
            NormalColorPickerWindow picker = new NormalColorPickerWindow();
            if (picker.ShowDialog().GetValueOrDefault())
            {
                GradientBrush.SelectedArrow.Background = picker.SelectedColorBrush;
                GradientBrush.SelectedArrow.Color = picker.SelectedColorBrush.Color;
                SelectedGradientBrush = linearGradientBrush;
            }
        }

        private void Numeric_OpacityValueChanged(object sender, RoutedEventArgs e)
        {
            if (GradientBrush.SelectedArrow != null)
            {
                GradientBrush.SelectedArrow.Color = ((SolidColorBrush)GradientBrush.SelectedArrow.Background).Color;
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

        private void SetAngle(Point mousePosition)
        {
            double x = mousePosition.X - radius;
            double y = radius - mousePosition.Y;
            double angle = (int)(Math.Atan2(y, x) * 180 / Math.PI);
            if (angle < 0) angle += 360;
            GradientBrush.Angle = (int)angle;
            SelectedGradientBrush = linearGradientBrush;
        }
    }
}