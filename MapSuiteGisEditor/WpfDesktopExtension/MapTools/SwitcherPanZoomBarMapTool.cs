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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class SwitcherPanZoomBarMapTool : PanZoomBarMapTool
    {
        private const string noselect = "/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Resources/modedisc_no_selection.png";
        private const string pan = "/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Resources/modedisc_pan.png";
        private const string trackZoom = "/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Resources/modedisc_track_zoom.png";
        private const string identify = "/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Resources/modedisc_identify.png";
        private const double panIconSize = 60;
        private const double globeIconSize = 15;

        [NonSerialized]
        private bool isSwitchedByMouse;

        [NonSerialized]
        private bool isGlobeButtonEnabled;

        [NonSerialized]
        private Image operationImage;

        [Obfuscation(Exclude = true)]
        private SwitcherMode switcherMode;

        public event EventHandler<SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs> SwitcherModeChanged;

        public static readonly DependencyProperty OperationImageSourceProperty =
            DependencyProperty.Register("OperationImageSource", typeof(ImageSource), typeof(SwitcherPanZoomBarMapTool), new UIPropertyMetadata(
                new BitmapImage()));

        public static readonly DependencyProperty SwitcherModeProperty =
            DependencyProperty.Register("SwitcherMode", typeof(SwitcherMode), typeof(SwitcherPanZoomBarMapTool),
            new UIPropertyMetadata(SwitcherMode.None, new PropertyChangedCallback(SwitcherModePropertyChanged)));

        public SwitcherPanZoomBarMapTool()
            : base()
        {
            isGlobeButtonEnabled = true;
            DefaultStyleKey = typeof(SwitcherPanZoomBarMapTool);
            OperationImageSource = new BitmapImage(new Uri(noselect, UriKind.RelativeOrAbsolute));
        }

        public ImageSource OperationImageSource
        {
            get { return (ImageSource)GetValue(OperationImageSourceProperty); }
            set { SetValue(OperationImageSourceProperty, value); }
        }

        public bool IsGlobeButtonEnabled
        {
            get { return isGlobeButtonEnabled; }
            set { isGlobeButtonEnabled = value; }
        }

        public SwitcherMode SwitcherMode
        {
            get { return (SwitcherMode)GetValue(SwitcherModeProperty); }
            set
            {
                SetValue(SwitcherModeProperty, value);
                switcherMode = value;
            }
        }

        public bool DisableModeChangedEvent { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            operationImage = GetTemplateChild("OperationImage") as Image;
            if (operationImage != null)
            {
                operationImage.MouseDown -= new System.Windows.Input.MouseButtonEventHandler(operationImage_MouseDown);
                operationImage.MouseDown += new System.Windows.Input.MouseButtonEventHandler(operationImage_MouseDown);
            }
        }

        protected virtual void OnSwitcherModeChanged(SwitcherMode newSwitcherMode)
        {
            EventHandler<SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs> handler = SwitcherModeChanged;
            if (handler != null)
            {
                handler(this, new SwitcherModeChangedSwitcherPanZoomBarMapToolEventArgs { NewSwitcherMode = newSwitcherMode, IsSwitchedByMouse = isSwitchedByMouse });
                isSwitchedByMouse = false;
            }
        }

        private void operationImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(operationImage);
            double panPanelRadius = panIconSize * .5;
            double globeRadius = globeIconSize * .5;
            Point center = new Point(panPanelRadius, panPanelRadius);
            double distanceFromMouseToCenter = Math.Sqrt(Math.Pow(mousePoint.X - center.X, 2) + Math.Pow(mousePoint.Y - center.Y, 2));
            if (distanceFromMouseToCenter <= globeRadius)
            {
                // zoom to whole world.
                if (CurrentMap != null && isGlobeButtonEnabled)
                {
                    Collection<BaseShape> rectangles = new Collection<BaseShape>();
                    foreach (Overlay overlay in CurrentMap.Overlays)
                    {
                        RectangleShape rect = overlay.GetBoundingBox();
                        if (rect != null)
                        {
                            rectangles.Add(rect);
                        }
                    }

                    if (rectangles.Count != 0)
                    {
                        RectangleShape targetExtent = OnGlobeButtonClick(ExtentHelper.GetBoundingBoxOfItems(rectangles));
                        if (targetExtent != null)
                        {
                            CurrentMap.CurrentExtent = targetExtent;
                            CurrentMap.Refresh();
                        }
                    }
                }
                e.Handled = true;
            }
            else if (distanceFromMouseToCenter <= panPanelRadius)
            {
                // change switcher mode.
                double degree = GetDegree(mousePoint.X - center.X, center.Y - mousePoint.Y);
                if ((degree >= 0 && degree < 90) || (degree <= 0 && degree >= -30))
                {
                    isSwitchedByMouse = true;
                    SwitcherMode = SwitcherMode.TrackZoom;
                }
                else if ((degree >= 90 && degree <= 180) || (degree < -150 && degree >= -180))
                {
                    isSwitchedByMouse = true;
                    SwitcherMode = SwitcherMode.Pan;
                }
                else
                {
                    isSwitchedByMouse = true;
                    SwitcherMode = SwitcherMode.Identify;
                }

                e.Handled = true;
            }
        }

        private static double GetDegree(double x, double y)
        {
            return Math.Atan2(y, x) * 180d / Math.PI;
        }

        private static void SwitcherModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var newMode = (SwitcherMode)e.NewValue;
            SwitcherPanZoomBarMapTool zoomBar = sender as SwitcherPanZoomBarMapTool;
            if (zoomBar != null)
            {
                SetIconAndRaiseEvent(newMode, zoomBar);
            }
        }

        [OnGeodeserialized]
        private void Derialized()
        {
            SetIconAndRaiseEvent(switcherMode, this);
            SwitcherMode = switcherMode;
        }

        private static void SetIconAndRaiseEvent(SwitcherMode mode, SwitcherPanZoomBarMapTool zoomBar)
        {
            switch (mode)
            {
                case SwitcherMode.Pan:
                    zoomBar.OperationImageSource = new BitmapImage(new Uri(pan, UriKind.RelativeOrAbsolute));
                    break;
                case SwitcherMode.TrackZoom:
                    zoomBar.OperationImageSource = new BitmapImage(new Uri(trackZoom, UriKind.RelativeOrAbsolute));
                    break;
                case SwitcherMode.Identify:
                    zoomBar.OperationImageSource = new BitmapImage(new Uri(identify, UriKind.RelativeOrAbsolute));
                    break;
                case SwitcherMode.None:
                default:
                    zoomBar.OperationImageSource = new BitmapImage(new Uri(noselect, UriKind.RelativeOrAbsolute));
                    break;
            }

            if (!zoomBar.DisableModeChangedEvent)
            {
                zoomBar.OnSwitcherModeChanged(mode);
            }
        }
    }
}