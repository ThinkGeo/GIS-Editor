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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class BoundingBoxSelectorMapTool : MapTool, INotifyPropertyChanged
    {
        private static readonly double sizeRatio = .8;
        private static Collection<Tuple<DistanceUnit, string>> units;

        public event EventHandler<BoundingBoxSelectCompletedEventArgs> BoundingBoxSelectCompletedClick;

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty HandleCornerBrushProperty =
            DependencyProperty.Register("HandleCornerBrush", typeof(Brush), typeof(BoundingBoxSelectorMapTool)
            , new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public static readonly DependencyProperty HandleCornerWidthProperty =
            DependencyProperty.Register("HandleCornerWidth", typeof(double)
            , typeof(BoundingBoxSelectorMapTool)
            , new UIPropertyMetadata(16d, new PropertyChangedCallback(HandleCornerWidthChanged)));
        [NonSerialized]
        private bool isLoadedScale;

        [NonSerialized]
        private RectangleShape resultBoundingBox;

        [Obfuscation]
        private double cornerHandleOffset = -8;

        [NonSerialized]
        private Collection<FrameworkElement> cornerHandles;

        [NonSerialized]
        private Image dragHandler;

        [NonSerialized]
        private Point mouseDownPosition;

        [NonSerialized]
        private Button cancelButton;

        [NonSerialized]
        private Button doneButton;

        [NonSerialized]
        private ComboBox scaleComboBox;

        [NonSerialized]
        private ComboBox unitComboBox;

        [NonSerialized]
        private TextBlock displayTxt;

        [NonSerialized]
        private Cursor cursor = null;

        [NonSerialized]
        private bool preserveSizeRatio;

        public BoundingBoxSelectorMapTool()
        {
            preserveSizeRatio = true;
            DefaultStyleKey = typeof(BoundingBoxSelectorMapTool);
            HandleCornerWidth = 16;
            Width = 256;
            Height = 256;
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Colors.Black);
            cornerHandles = new Collection<FrameworkElement>();
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            SizeChanged += new SizeChangedEventHandler(BoundingBoxSelectorMapTool_SizeChanged);
        }

        public bool PreserveSizeRatio
        {
            get { return preserveSizeRatio; }
            set
            {
                preserveSizeRatio = value;
                if (scaleComboBox != null && displayTxt != null && unitComboBox != null)
                {
                    if (!preserveSizeRatio)
                    {
                        if (isLoadedScale)
                        {
                            scaleComboBox.Visibility = Visibility.Visible;
                            displayTxt.Visibility = Visibility.Visible;
                            unitComboBox.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            InitializeScaleControls();
                        }
                    }
                    else
                    {
                        scaleComboBox.Visibility = Visibility.Collapsed;
                        displayTxt.Visibility = Visibility.Collapsed;
                        unitComboBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        public RectangleShape ResultBoundingBox
        {
            get { return resultBoundingBox; }
        }

        public Brush HandleCornerBrush
        {
            get { return (Brush)GetValue(HandleCornerBrushProperty); }
            set { SetValue(HandleCornerBrushProperty, value); }
        }

        public double HandleCornerWidth
        {
            get { return (int)GetValue(HandleCornerWidthProperty); }
            set { SetValue(HandleCornerWidthProperty, value); }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            cornerHandles.Clear();
            cornerHandles.Add((FrameworkElement)GetTemplateChild("UpperLeftCornerHandler"));
            cornerHandles.Add((FrameworkElement)GetTemplateChild("UpperRightCornerHandler"));
            cornerHandles.Add((FrameworkElement)GetTemplateChild("LowerLeftCornerHandler"));
            cornerHandles.Add((FrameworkElement)GetTemplateChild("LowerRightCornerHandler"));
            dragHandler = (Image)GetTemplateChild("DragHandler");
            cancelButton = (Button)GetTemplateChild("CancelButton");
            doneButton = (Button)GetTemplateChild("DoneButton");
            scaleComboBox = (ComboBox)GetTemplateChild("ScaleComboBox");
            displayTxt = (TextBlock)GetTemplateChild("DisplayTxt");
            unitComboBox = (ComboBox)GetTemplateChild("UnitComboBox");

            InitializeComponents();
            InitializePosition();
        }

        private void InitializePosition()
        {
            Grid parentGrid = (Grid)Parent;
            Margin = new Thickness(parentGrid.ActualWidth * .5 - Width * .5, parentGrid.ActualHeight * .5 - Height * .5, 0, 0);
        }

        private void InitializeComponents()
        {
            AdjustSizeRatio();

            dragHandler.MouseDown -= new MouseButtonEventHandler(DragHandler_MouseDown);
            dragHandler.MouseMove -= new MouseEventHandler(DragHandler_MouseMove);
            dragHandler.MouseUp -= new MouseButtonEventHandler(DragHandler_MouseUp);
            cancelButton.Click -= new RoutedEventHandler(CancelButton_Click);
            doneButton.Click -= new RoutedEventHandler(DoneButton_Click);

            cancelButton.MouseEnter -= new MouseEventHandler(DoneButton_MouseEnter);
            cancelButton.MouseEnter += new MouseEventHandler(DoneButton_MouseEnter);
            cancelButton.MouseLeave -= new MouseEventHandler(DoneButton_MouseLeave);
            cancelButton.MouseLeave += new MouseEventHandler(DoneButton_MouseLeave);

            doneButton.MouseEnter -= new MouseEventHandler(DoneButton_MouseEnter);
            doneButton.MouseEnter += new MouseEventHandler(DoneButton_MouseEnter);
            doneButton.MouseLeave -= new MouseEventHandler(DoneButton_MouseLeave);
            doneButton.MouseLeave += new MouseEventHandler(DoneButton_MouseLeave);

            dragHandler.MouseDown += new MouseButtonEventHandler(DragHandler_MouseDown);
            dragHandler.MouseMove += new MouseEventHandler(DragHandler_MouseMove);
            dragHandler.MouseUp += new MouseButtonEventHandler(DragHandler_MouseUp);
            cancelButton.Click += new RoutedEventHandler(CancelButton_Click);
            doneButton.Click += new RoutedEventHandler(DoneButton_Click);

            CurrentMap.SizeChanged -= CurrentMap_SizeChanged;
            CurrentMap.SizeChanged += CurrentMap_SizeChanged;

            foreach (var resizeHandler in cornerHandles)
            {
                resizeHandler.MouseDown -= new MouseButtonEventHandler(ResizeHandler_MouseDown);
                resizeHandler.MouseMove -= new MouseEventHandler(ResizeHandler_MouseMove);
                resizeHandler.MouseUp -= new MouseButtonEventHandler(ResizeHandler_MouseUp);

                resizeHandler.MouseDown += new MouseButtonEventHandler(ResizeHandler_MouseDown);
                resizeHandler.MouseMove += new MouseEventHandler(ResizeHandler_MouseMove);
                resizeHandler.MouseUp += new MouseButtonEventHandler(ResizeHandler_MouseUp);
            }
            if (!preserveSizeRatio)
            {
                InitializeScaleControls();

                isLoadedScale = true;
            }
        }

        private void InitializeScaleControls()
        {
            scaleComboBox.Visibility = Visibility.Visible;
            displayTxt.Visibility = Visibility.Visible;
            unitComboBox.Visibility = Visibility.Visible;

            CurrentMap.CurrentScaleChanged -= CurrentMap_CurrentScaleChanged;
            CurrentMap.CurrentScaleChanged += CurrentMap_CurrentScaleChanged;
            scaleComboBox.IsVisibleChanged -= scaleComboBox_IsVisibleChanged;
            scaleComboBox.IsVisibleChanged += scaleComboBox_IsVisibleChanged;

            unitComboBox.ItemsSource = GetDistanceUnits();
            unitComboBox.SelectionChanged -= new SelectionChangedEventHandler(UnitComboBox_SelectionChanged);
            unitComboBox.SelectionChanged += new SelectionChangedEventHandler(UnitComboBox_SelectionChanged);
            unitComboBox.SelectedItem = units.FirstOrDefault(u => u.Item1 == DistanceUnit.Feet);
        }

        private void CurrentMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WpfMap wpfMap = (WpfMap)sender;
            bool boxIsOffRight = (Margin.Left + Width) > wpfMap.ActualWidth;
            bool boxIsOffBottom = (Margin.Top + Height) > wpfMap.ActualHeight;
            if (boxIsOffRight || boxIsOffBottom)
            {
                double leftMargin = Margin.Left;
                double topMargin = Margin.Top;
                if (boxIsOffRight)
                {
                    leftMargin = wpfMap.ActualWidth - Width;
                    if (leftMargin < 0)
                    {
                        leftMargin = 0;
                        Width = wpfMap.ActualWidth;
                    }
                }
                if (boxIsOffBottom)
                {
                    topMargin = wpfMap.ActualHeight - Height;
                    if (topMargin < 0)
                    {
                        topMargin = 0;
                        Height = wpfMap.ActualHeight;
                    }
                }
                Margin = new Thickness(leftMargin, topMargin, 0, 0);
            }
        }

        private void scaleComboBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                UnitComboBox_SelectionChanged(null, null);
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scaleComboBox.Items.Clear();
            DistanceUnit unit = ((Tuple<DistanceUnit, string>)unitComboBox.SelectedItem).Item1;
            foreach (var zoomLevel in CurrentMap.ZoomLevelSet.GetZoomLevels())
            {
                double tempScale = Conversion.ConvertMeasureUnits(zoomLevel.Scale, DistanceUnit.Inch, unit);
                scaleComboBox.Items.Add(new ScaleWrapper(tempScale, GetSimplifiedNumber(tempScale)));
            }
            double currentScale = Conversion.ConvertMeasureUnits(CurrentMap.CurrentScale, DistanceUnit.Inch, unit);
            scaleComboBox.SelectedItem = scaleComboBox.Items.OfType<ScaleWrapper>().FirstOrDefault(s => Math.Abs(s.Scale - currentScale) < 1);
        }

        private double GetSimplifiedNumber(double value)
        {
            if (value >= 1)
            {
                int valueInt = (int)value;
                double result = value - valueInt;
                if (result >= 0.5) return valueInt + 1;
                else return valueInt;
            }
            else
            {
                double resultNumber = 0;
                int decimals = 4;
                while ((resultNumber = Math.Round(value, decimals)) == 0)
                {
                    decimals += 2;
                }
                return resultNumber;
            }
        }

        private void CurrentMap_CurrentScaleChanged(object sender, CurrentScaleChangedWpfMapEventArgs e)
        {
            if (scaleComboBox.Visibility == Visibility.Visible)
            {
                DistanceUnit unit = ((Tuple<DistanceUnit, string>)unitComboBox.SelectedItem).Item1;
                double currentScale = Conversion.ConvertMeasureUnits(e.CurrentScale, DistanceUnit.Inch, unit);
                scaleComboBox.SelectedItem = scaleComboBox.Items.OfType<ScaleWrapper>().FirstOrDefault(s => Math.Abs(s.Scale - currentScale) < 1);
            }
        }

        private void DoneButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CurrentMap.Cursor = cursor;
        }

        private void DoneButton_MouseEnter(object sender, MouseEventArgs e)
        {
            cursor = CurrentMap.Cursor;
            CurrentMap.Cursor = GisEditorCursors.Normal;
        }

        public void AdjustSizeRatio()
        {
            double maxWidth = CurrentMap.ActualWidth * sizeRatio;
            double maxHeight = CurrentMap.ActualHeight * sizeRatio;
            double resizeRatio = 1;
            if (Width > maxWidth || Height > maxHeight)
            {
                if (Width / Height > maxWidth / maxHeight)
                {
                    resizeRatio = maxWidth / Width;
                }
                else
                {
                    resizeRatio = maxHeight / Height;
                }
            }

            Width *= resizeRatio;
            Height *= resizeRatio;

            SyncCornerHandlers();
        }

        protected virtual void OnBoundingBoxSelectCompletedClick(BoundingBoxSelectCompletedEventArgs e)
        {
            EventHandler<BoundingBoxSelectCompletedEventArgs> handler = BoundingBoxSelectCompletedClick;
            if (handler != null) handler(this, e);
        }

        public static byte[] GetCroppedMapPreviewImage(WpfMap wpfMap, Int32Rect drawingRect)
        {
            Canvas rootCanvas = wpfMap.ToolsGrid.Parent as Canvas;
            byte[] imageBytes = null;
            if (rootCanvas != null)
            {
                Canvas eventCanvas = rootCanvas.FindName("EventCanvas") as Canvas;
                if (eventCanvas != null)
                {
                    Canvas overlayCanvas = eventCanvas.FindName("OverlayCanvas") as Canvas;
                    if (overlayCanvas != null)
                    {
                        RenderTargetBitmap imageSource = new RenderTargetBitmap((int)wpfMap.RenderSize.Width, (int)wpfMap.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
                        imageSource.Render(overlayCanvas);

                        Canvas popupCanvas = eventCanvas.FindName("PopupCanvas") as Canvas;
                        if (popupCanvas != null) imageSource.Render(popupCanvas);

                        Canvas adornmentCanvas = eventCanvas.FindName("AdornmentCanvas") as Canvas;
                        if (adornmentCanvas != null) imageSource.Render(adornmentCanvas);

                        CroppedBitmap croppedSource = new CroppedBitmap(imageSource, drawingRect);
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(croppedSource));
                        using (var streamSource = new MemoryStream())
                        {
                            encoder.Save(streamSource);
                            imageBytes = streamSource.ToArray();
                        }
                    }
                }
            }

            return imageBytes;
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentMap.Cursor = cursor;
            IsEnabled = false;
            double screenLeft = Margin.Left;
            double screenTop = Margin.Top;
            PointShape upperLeftPoint = CurrentMap.ToWorldCoordinate(screenLeft, screenTop);
            PointShape lowerRightPoint = CurrentMap.ToWorldCoordinate(screenLeft + Width, screenTop + Height);
            resultBoundingBox = new RectangleShape(upperLeftPoint, lowerRightPoint);

            BoundingBoxSelectCompletedEventArgs args = new BoundingBoxSelectCompletedEventArgs(resultBoundingBox);
            if (!preserveSizeRatio)
            {
                double scale = -1;
                if (!string.IsNullOrEmpty(scaleComboBox.Text) && scaleComboBox.SelectedValue != null)
                {
                    ScaleWrapper scaleWrapper = scaleComboBox.SelectedValue as ScaleWrapper;
                    if (scaleWrapper.DisplayScale.ToString() == scaleComboBox.Text)
                    {
                        scale = scaleWrapper.Scale;
                    }
                    else
                    {
                        double.TryParse(scaleComboBox.Text, out scale);
                    }
                }

                if (scale > 0)
                {
                    DistanceUnit unit = ((Tuple<DistanceUnit, string>)unitComboBox.SelectedItem).Item1;
                    scale = Conversion.ConvertMeasureUnits(scale, unit, DistanceUnit.Inch);
                    args = new BoundingBoxSelectCompletedEventArgs(resultBoundingBox, scale);
                }
            }

            CurrentMap.SizeChanged -= CurrentMap_SizeChanged;

            args.ImageBytes = GetCroppedMapPreviewImage(CurrentMap, new Int32Rect((int)Margin.Left, (int)Margin.Top, (int)Width, (int)Height));
            OnBoundingBoxSelectCompletedClick(args);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentMap.Cursor = cursor;
            IsEnabled = false;
            resultBoundingBox = null;

            CurrentMap.SizeChanged -= CurrentMap_SizeChanged;

            BoundingBoxSelectCompletedEventArgs args = new BoundingBoxSelectCompletedEventArgs(true);
            OnBoundingBoxSelectCompletedClick(args);
        }

        private void SyncCornerHandlers()
        {
            SetPosition(cornerHandles[0], 0, 0, cornerHandleOffset);
            SetPosition(cornerHandles[1], Width, 0, cornerHandleOffset);
            SetPosition(cornerHandles[2], 0, Height, cornerHandleOffset);
            SetPosition(cornerHandles[3], Width, Height, cornerHandleOffset);
            cornerHandles[0].Cursor = Cursors.SizeNWSE;
            cornerHandles[1].Cursor = Cursors.SizeNESW;
            cornerHandles[2].Cursor = Cursors.SizeNESW;
            cornerHandles[3].Cursor = Cursors.SizeNWSE;
        }

        private void ResizeHandler_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProcessCurrentRectangle(sender, rect =>
            {
                if (rect.IsMouseCaptured)
                {
                    rect.ReleaseMouseCapture();
                    e.Handled = true;
                }
            });
        }

        private void ResizeHandler_MouseMove(object sender, MouseEventArgs e)
        {
            ProcessCurrentRectangle(sender, rect =>
            {
                if (rect.IsMouseCaptured)
                {
                    if (preserveSizeRatio)
                    {
                        ResizeBoxWithSizeRatioPreserved(e, rect);
                    }
                    else
                    {
                        ResizeBoxWithoutSizeRatioPreserved(e, rect);
                    }
                    e.Handled = true;
                }
            });
        }

        private void ResizeBoxWithoutSizeRatioPreserved(MouseEventArgs e, Rectangle rect)
        {
            var currentMousePosition = e.GetPosition((Grid)Parent);
            if (mouseDownPosition != currentMousePosition)
            {
                var offsetX = currentMousePosition.X - mouseDownPosition.X;
                var offsetY = currentMousePosition.Y - mouseDownPosition.Y;
                //double ratio = Width / Height;
                //offsetY = offsetX / ratio;

                mouseDownPosition = currentMousePosition;
                var currentLeft = Canvas.GetLeft(rect);
                var currentTop = Canvas.GetTop(rect);
                var restRectangles = cornerHandles.Where(c => c != rect).ToArray();
                bool isLeft = restRectangles.Any(c => Canvas.GetLeft(c) > currentLeft);
                bool isTop = restRectangles.Any(c => Canvas.GetTop(c) > currentTop);

                //if (!isLeft && isTop || isLeft && !isTop) offsetY *= -1;

                var newWidth = Math.Abs(Width + (isLeft ? -1 : 1) * offsetX);
                if (newWidth > 120)
                {
                    var newHeight = Math.Abs(Height + (isTop ? -1 : 1) * offsetY);
                    if (newHeight > 120 && Margin.Left + newWidth <= CurrentMap.ActualWidth && Margin.Top + newHeight <= CurrentMap.ActualHeight)
                    {
                        Width = newWidth;
                        Height = newHeight;
                        double right = restRectangles.Select(r => Canvas.GetLeft(r)).Max();
                        double left = restRectangles.Select(r => Canvas.GetLeft(r)).Min();
                        double top = restRectangles.Select(r => Canvas.GetTop(r)).Min();
                        double bottom = restRectangles.Select(r => Canvas.GetTop(r)).Max();

                        var newLeftMargin = Margin.Left + offsetX;
                        var newTopMargin = Margin.Top + offsetY;
                        if (isLeft && isTop && newLeftMargin >= 0 && newTopMargin >= 0)
                        {
                            var widthOutOfRange = newLeftMargin + Width >= CurrentMap.ActualWidth;
                            var heightOutOfRange = newTopMargin + Height >= CurrentMap.ActualHeight;
                            Margin = new Thickness(widthOutOfRange ? Margin.Left : newLeftMargin, heightOutOfRange ? Margin.Left : newTopMargin, 0, 0);
                        }
                        else if (isLeft && !isTop && newLeftMargin >= 0 && Margin.Top >= 0)
                        {
                            var widthOutOfRange = newLeftMargin + Width >= CurrentMap.ActualWidth;
                            Margin = new Thickness(widthOutOfRange ? Margin.Left : newLeftMargin, Margin.Top, 0, 0);
                        }
                        else if (!isLeft && isTop && Margin.Left >= 0 && newTopMargin >= 0)
                        {
                            var heightOutOfRange = Margin.Top + Height >= CurrentMap.ActualHeight;
                            Margin = new Thickness(Margin.Left, heightOutOfRange ? Margin.Left : newTopMargin, 0, 0);
                        }
                        SyncCornerHandlers();
                    }
                }
            }
        }

        private void ResizeBoxWithSizeRatioPreserved(MouseEventArgs e, Rectangle rect)
        {
            var currentMousePosition = e.GetPosition((Grid)Parent);
            if (mouseDownPosition != currentMousePosition)
            {
                var offsetX = currentMousePosition.X - mouseDownPosition.X;
                var offsetY = currentMousePosition.Y - mouseDownPosition.Y;
                double ratio = Width / Height;
                offsetY = offsetX / ratio;

                mouseDownPosition = currentMousePosition;
                var currentLeft = Canvas.GetLeft(rect);
                var currentTop = Canvas.GetTop(rect);
                var restRectangles = cornerHandles.Where(c => c != rect).ToArray();
                bool isLeft = restRectangles.Any(c => Canvas.GetLeft(c) > currentLeft);
                bool isTop = restRectangles.Any(c => Canvas.GetTop(c) > currentTop);

                if (!isLeft && isTop || isLeft && !isTop) offsetY *= -1;

                var newWidth = Math.Abs(Width + (isLeft ? -1 : 1) * offsetX);
                if (newWidth > 175)
                {
                    var newHeight = Math.Abs(Height + (isTop ? -1 : 1) * offsetY);
                    if (Margin.Left + newWidth <= CurrentMap.ActualWidth && Margin.Top + newHeight <= CurrentMap.ActualHeight)
                    {
                        Width = newWidth;
                        Height = newHeight;
                        double right = restRectangles.Select(r => Canvas.GetLeft(r)).Max();
                        double left = restRectangles.Select(r => Canvas.GetLeft(r)).Min();
                        double top = restRectangles.Select(r => Canvas.GetTop(r)).Min();
                        double bottom = restRectangles.Select(r => Canvas.GetTop(r)).Max();

                        var newLeftMargin = Margin.Left + offsetX;
                        var newTopMargin = Margin.Top + offsetY;
                        if (isLeft && isTop && newLeftMargin >= 0 && newTopMargin >= 0)
                        {
                            var widthOutOfRange = newLeftMargin + Width >= CurrentMap.ActualWidth;
                            var heightOutOfRange = newTopMargin + Height >= CurrentMap.ActualHeight;
                            Margin = new Thickness(widthOutOfRange ? Margin.Left : newLeftMargin, heightOutOfRange ? Margin.Left : newTopMargin, 0, 0);
                        }
                        else if (isLeft && !isTop && newLeftMargin >= 0 && Margin.Top >= 0)
                        {
                            var widthOutOfRange = newLeftMargin + Width >= CurrentMap.ActualWidth;
                            Margin = new Thickness(widthOutOfRange ? Margin.Left : newLeftMargin, Margin.Top, 0, 0);
                        }
                        else if (!isLeft && isTop && Margin.Left >= 0 && newTopMargin >= 0)
                        {
                            var heightOutOfRange = Margin.Top + Height >= CurrentMap.ActualHeight;
                            Margin = new Thickness(Margin.Left, heightOutOfRange ? Margin.Left : newTopMargin, 0, 0);
                        }
                        SyncCornerHandlers();
                    }
                }
            }
        }

        private void ResizeHandler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessCurrentRectangle(sender, rect =>
            {
                rect.CaptureMouse();
                mouseDownPosition = e.GetPosition((Grid)Parent);
                e.Handled = true;
            });
        }

        private void ProcessCurrentRectangle(object sender, Action<Rectangle> process)
        {
            var resizeHandler = sender as Rectangle;
            if (resizeHandler != null && process != null)
            {
                process(resizeHandler);
            }
        }

        private void DragHandler_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (dragHandler.IsMouseCaptured)
            {
                dragHandler.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void DragHandler_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragHandler.IsMouseCaptured)
            {
                var currentMousePosition = e.GetPosition(this);
                var offsetX = currentMousePosition.X - mouseDownPosition.X;
                var offsetY = currentMousePosition.Y - mouseDownPosition.Y;

                var currentX = Margin.Left + offsetX;
                var currentY = Margin.Top + offsetY;

                if (currentX >= 0 && currentX + Width <= CurrentMap.ActualWidth)
                    Margin = new Thickness(currentX, Margin.Top, 0, 0);
                if (currentY >= 0 && currentY + Height <= CurrentMap.ActualHeight)
                    Margin = new Thickness(Margin.Left, currentY, 0, 0);
                e.Handled = true;
            }
        }

        private void DragHandler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dragHandler.CaptureMouse();
            mouseDownPosition = e.GetPosition(this);
            e.Handled = true;
        }

        private void BoundingBoxSelectorMapTool_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (dragHandler != null)
            {
                SetPosition(dragHandler, Width * .5 - dragHandler.Width * .5, Height * .5 - dragHandler.Height * .5);
            }
        }

        private static void SetPosition(UIElement element, double x, double y, double offset = 0)
        {
            Canvas.SetLeft(element, x + offset);
            Canvas.SetTop(element, y + offset);
        }

        private static void HandleCornerWidthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BoundingBoxSelectorMapTool currentInstance = sender as BoundingBoxSelectorMapTool;
            if (currentInstance != null)
            {
                currentInstance.cornerHandleOffset = currentInstance.HandleCornerWidth * -.5;
            }
        }

        private static Collection<Tuple<DistanceUnit, string>> GetDistanceUnits()
        {
            if (units == null)
            {
                units = new Collection<Tuple<DistanceUnit, string>>();
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Meter, "m"));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Feet, "ft."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Kilometer, "km"));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Mile, "mi."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.UsSurveyFeet, "us-ft."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Yard, "yd."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.NauticalMile, "nmi."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Inch, "in."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Link, "li."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Chain, "ch."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Pole, "pole"));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Rod, "rd."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Furlong, "fur."));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Vara, "vara"));
                units.Add(new Tuple<DistanceUnit, string>(DistanceUnit.Arpent, "arpent"));
            }
            return units;
        }
    }
}