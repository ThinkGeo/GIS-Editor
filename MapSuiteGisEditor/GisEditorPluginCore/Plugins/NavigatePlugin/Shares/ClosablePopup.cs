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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClosablePopup : Popup
    {
        [NonSerialized]
        private Grid popupContent;

        [NonSerialized]
        private TextBlock workingLayout;

        [Obfuscation(Exclude = true)]
        private WpfMap parentMap;

        [Obfuscation(Exclude = true)]
        private string textContent;

        //[Obfuscation(Exclude = true)]
        //private bool prohibitedAutoChangePosition;

        public ClosablePopup()
        { }

        public ClosablePopup(PointShape location)
            : this(location.X, location.Y)
        { }

        public ClosablePopup(double x, double y)
            : base(x, y)
        {
            Initialize();
        }

        public WpfMap ParentMap
        {
            get { return parentMap; }
            set { parentMap = value; }
        }

        public string WorkingContent
        {
            get { return workingLayout.Text; }
            set { workingLayout.Text = value; }
        }

        //protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        //{
        //     base.OnMouseDown(e);
        //     prohibitedAutoChangePosition = true;
        //}

        //protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    base.OnMouseUp(e);
        //    prohibitedAutoChangePosition = false;
        //}

        //protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        //{
        //    base.OnMouseMove(e);
        //    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        //    {
        //        FrameworkElement parent = GetParent(5);
        //        if (parent != null)
        //        {
        //            Focus();
        //            Point targetPoint = e.GetPosition(parent);
        //            Console.WriteLine("X: " + targetPoint.X + "  Y: " + targetPoint.Y);
        //            SetValue(Canvas.LeftProperty, targetPoint.X);
        //            SetValue(Canvas.TopProperty, targetPoint.Y + 30);
        //        }
        //    }
        //    Console.WriteLine("Popup Mouse Moving....");
        //}

        //protected override void OnPositionChanged(DependencyPropertyChangedEventArgs eventArgs)
        //{
        //    if (!prohibitedAutoChangePosition)
        //    {
        //        base.OnPositionChanged(eventArgs);
        //    }
        //}

        private FrameworkElement GetParent(int parentLevel)
        {
            FrameworkElement resultParent = this.Parent as FrameworkElement;
            for (int i = 0; i < parentLevel - 1; i++)
            {
                resultParent = resultParent.Parent as FrameworkElement;
                if (resultParent == null)
                {
                    break;
                }
            }
            return resultParent;
        }

        private static Image GetCloseButton()
        {
            Image image = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.None,
                Cursor = System.Windows.Input.Cursors.Arrow,
                Opacity = .1
            };

            image.BeginInit();
            image.Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/close.png", UriKind.Absolute));
            image.EndInit();
            return image;
        }

        private static void SwitchCloseButton(CloseButtonStatus status, Image closeButton)
        {
            switch (status)
            {
                case CloseButtonStatus.Enter:
                    closeButton.Opacity = 1;
                    break;
                case CloseButtonStatus.Leave:
                default:
                    closeButton.Opacity = .1;
                    break;
            }
        }

        private void Initialize()
        {
            popupContent = new Grid();
            workingLayout = new TextBlock { Margin = new Thickness(5), MaxWidth = 150, TextWrapping = TextWrapping.Wrap };
            Image closeButton = GetCloseButton();
            StackPanel closeButtonHolder = new StackPanel
            {
                Width = 25,
                Height = 15,
                Margin = new Thickness(0, 0, -24, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Colors.Transparent)
            };

            closeButtonHolder.Children.Add(closeButton);
            closeButton.MouseLeftButtonDown += (s, e) =>
            {
                if (ParentMap != null)
                {
                    var popupOverlays = ParentMap.Overlays.OfType<PopupOverlay>().Where(tmpPopupOverlay => tmpPopupOverlay.Popups.Contains(this));
                    foreach (var popupOverlay in popupOverlays)
                    {
                        popupOverlay.Popups.Remove(this);
                        popupOverlay.Refresh();
                    }
                    e.Handled = true;
                }
            };

            popupContent.Children.Add(workingLayout);
            popupContent.Children.Add(closeButtonHolder);
            MouseEnter += (s, e) => { SwitchCloseButton(CloseButtonStatus.Enter, closeButton); };
            MouseLeave += (s, e) => { SwitchCloseButton(CloseButtonStatus.Leave, closeButton); };
            Content = popupContent;
        }

        [OnGeodeserialized]
        private void RestoreState()
        {
            Initialize();
            if (!string.IsNullOrEmpty(textContent))
            {
                WorkingContent = textContent;
            }
        }

        [OnGeoserializing]
        private void SaveTextState()
        {
            textContent = WorkingContent;
        }
    }
}