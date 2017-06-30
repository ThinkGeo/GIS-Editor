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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// ColorPicker control
    /// 
    /// </summary>
    public class SolidColorPicker : Control, INotifyPropertyChanged
    {
        // todo: 
        // - localize strings...
        // - more palettes
        // - persist palette - in static list, and static load/save methods?
        //   - the user can also bind the Palette and do the persisting 
        // - 'automatic' color? 'IncludeAutoColor' dependency property?

        public static readonly DependencyProperty ShowAsHexProperty =
            DependencyProperty.Register("ShowAsHex", typeof(bool), typeof(SolidColorPicker), new UIPropertyMetadata(false));

        public static readonly DependencyProperty CustomPaletteProperty =
            DependencyProperty.Register("CustomPalette", typeof(ObservableCollection<Color>), typeof(SolidColorPicker),
                                        new UIPropertyMetadata(CreateEmptyPalette()));

        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register("Palette", typeof(ObservableCollection<Color>), typeof(SolidColorPicker),
                                        new UIPropertyMetadata(CreateDefaultPalette()));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(SolidColorPicker),
                                        new UIPropertyMetadata(false, IsDropDownOpenChanged));

        public static readonly DependencyProperty IsPickingProperty =
            DependencyProperty.Register("IsPicking", typeof(bool), typeof(SolidColorPicker),
                                        new UIPropertyMetadata(false, IsPickingChanged));

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.RegisterAttached("SelectedColor", typeof(Color), typeof(SolidColorPicker),
                                        new FrameworkPropertyMetadata(Color.FromArgb(80, 255, 255, 0),
                                                                      FrameworkPropertyMetadataOptions.
                                                                          BindsTwoWayByDefault,
                                                                      SelectedColorChanged));

        public static readonly DependencyProperty SelectedCustomColorProperty = DependencyProperty.RegisterAttached("SelectedCustomColor"
            , typeof(Color), typeof(SolidColorPicker), new FrameworkPropertyMetadata(Color.FromArgb(80, 255, 255, 0)
                , FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedCustomColorChanged));

        public event MouseButtonEventHandler SelectedItemDoubleClick;

        private byte brightness;
        private byte hue;
        private byte saturation;
        private bool updateHSV = true;
        private DispatcherTimer pickingTimer;
        public event EventHandler SelectionChanged;
        public event MouseButtonEventHandler ColorPanelMouseDoubleClick;

        protected void OnSelectionChanged()
        {
            EventHandler handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public SolidColorPicker()
        {
            Loaded += new RoutedEventHandler(SolidColorPicker_Loaded);
            Unloaded += new RoutedEventHandler(SolidColorPicker_Unloaded);
        }

        static SolidColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SolidColorPicker), new FrameworkPropertyMetadata(typeof(SolidColorPicker)));
        }

        /// <summary>
        /// Gets or sets a value indicating whether show as color names as hex strings.
        /// </summary>
        /// <value><c>true</c> if show as hex; otherwise, <c>false</c>.</value>
        public bool ShowAsHex
        {
            get { return (bool)GetValue(ShowAsHexProperty); }
            set { SetValue(ShowAsHexProperty, value); }
        }

        public ObservableCollection<Color> CustomPalette
        {
            get { return (ObservableCollection<Color>)GetValue(CustomPaletteProperty); }
            set { SetValue(CustomPaletteProperty, value); }
        }

        public ObservableCollection<Color> Palette
        {
            get { return (ObservableCollection<Color>)GetValue(PaletteProperty); }
            set { SetValue(PaletteProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the color picker popup is open.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this popup is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets if picking colors from the screen is active.
        /// Use the 'SHIFT' button to select colors when this mode is active.
        /// </summary>
        public bool IsPicking
        {
            get { return (bool)GetValue(IsPickingProperty); }
            set { SetValue(IsPickingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        /// <value>The color of the selected.</value>
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public Color SelectedCustomColor
        {
            get { return (Color)GetValue(SelectedCustomColorProperty); }
            set { SetValue(SelectedCustomColorProperty, value); }
        }

        public Brush AlphaGradient
        {
            get { return new LinearGradientBrush(Colors.Transparent, Color.FromRgb(Red, Green, Blue), 0); }
        }

        public Brush SaturationGradient
        {
            get
            {
                return new LinearGradientBrush(ColorHelper.HsvToColor(Alpha, Hue, 0, Brightness),
                                               ColorHelper.HsvToColor(Alpha, Hue, 255, Brightness), 0);
            }
        }

        public Brush BrightnessGradient
        {
            get
            {
                return new LinearGradientBrush(ColorHelper.HsvToColor(Alpha, Hue, Saturation, 0),
                                               ColorHelper.HsvToColor(Alpha, Hue, Saturation, 255), 0);
            }
        }

        public string ColorName
        {
            get
            {
                if (ShowAsHex)
                    return ColorHelper.ColorToHex(SelectedColor);
                var t = typeof(Colors);
                var fields = t.GetProperties(BindingFlags.Public | BindingFlags.Static);
                string nearestColor = "Custom";
                double nearestDist = 30;
                // find the color that is closest
                foreach (var fi in fields)
                {
                    var c = (Color)fi.GetValue(null, null);
                    if (SelectedColor == c)
                        return fi.Name;
                    double d = ColorHelper.ColorDifference(SelectedColor, c);
                    if (d < nearestDist)
                    {
                        nearestColor = "~ " + fi.Name; // 'kind of'
                        nearestDist = d;
                    }
                }
                if (SelectedColor.A < 255)
                {
                    return String.Format("{0}, {1:0} %", nearestColor, SelectedColor.A / 2.55);
                }
                return nearestColor;
            }
        }

        public byte Red
        {
            get { return SelectedCustomColor.R; }
            set { SelectedCustomColor = Color.FromArgb(Alpha, value, Green, Blue); }
        }

        public byte Green
        {
            get { return SelectedCustomColor.G; }
            set { SelectedCustomColor = Color.FromArgb(Alpha, Red, value, Blue); }
        }

        public byte Blue
        {
            get { return SelectedCustomColor.B; }
            set { SelectedCustomColor = Color.FromArgb(Alpha, Red, Green, value); }
        }

        public byte Alpha
        {
            get { return SelectedCustomColor.A; }
            set { SelectedCustomColor = Color.FromArgb(value, Red, Green, Blue); }
        }

        public byte Hue
        {
            get { return hue; }
            set
            {
                updateHSV = false;
                SelectedCustomColor = ColorHelper.HsvToColor(Alpha, value, Saturation, Brightness);
                hue = value;
                OnPropertyChanged("Hue");
            }
        }

        public byte Saturation
        {
            get { return saturation; }
            set
            {
                updateHSV = false;
                SelectedCustomColor = ColorHelper.HsvToColor(Alpha, Hue, value, Brightness);
                updateHSV = true;
                saturation = value;
                OnPropertyChanged("Saturation");
            }
        }

        public byte Brightness
        {
            get { return brightness; }
            set
            {
                updateHSV = false;
                SelectedCustomColor = ColorHelper.HsvToColor(Alpha, Hue, Saturation, value);
                updateHSV = true;
                brightness = value;
                OnPropertyChanged("Brightness");
            }
        }

        private ListBox solidColorList;
        private RandomPicker randomPicker;
        private EventSetter solidColorListDoubleClickEventSetter;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            solidColorList = (ListBox)GetTemplateChild("SolidColorList");
            InitializeSolidColorListDoubleClickEvent();

            randomPicker = (RandomPicker)GetTemplateChild("RandomPicker");
            InitializeRandomPanelDoubleClickEvent();
        }

        private void InitializeRandomPanelDoubleClickEvent()
        {
            randomPicker.MouseDoubleClick -= RandomPicker_MouseDoubleClick;
            randomPicker.MouseDoubleClick += RandomPicker_MouseDoubleClick;
        }

        private void RandomPicker_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnColorPanelMouseDoubleClick(e);
        }

        protected virtual void OnColorPanelMouseDoubleClick(MouseButtonEventArgs e)
        {
            MouseButtonEventHandler handler = ColorPanelMouseDoubleClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void InitializeSolidColorListDoubleClickEvent()
        {
            if (solidColorListDoubleClickEventSetter == null)
            {
                solidColorListDoubleClickEventSetter = new EventSetter(ListBoxItem.MouseDoubleClickEvent, new MouseButtonEventHandler((s, e) => OnSelectedItemDoubleClick(s, e)));
            }

            if (solidColorList.ItemContainerStyle == null)
            {
                solidColorList.ItemContainerStyle = new Style(typeof(ListBoxItem));
                solidColorList.ItemContainerStyle.Setters.Add(solidColorListDoubleClickEventSetter);
            }
        }

        public void UnSelect()
        {
            if (solidColorList != null)
            {
                SelectedColor = Color.FromArgb(0, 1, 1, 1);
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private bool stopColorChangedEvent;

        #endregion

        private static void IsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SolidColorPicker)d).IsDropDownOpenChanged();
        }

        private void IsDropDownOpenChanged()
        {
            // turn off picking when drop down is closed
            if (!IsDropDownOpen && IsPicking)
                IsPicking = false;
        }

        private static void IsPickingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SolidColorPicker)d).IsPickingChanged((bool)e.NewValue);
        }

        private void IsPickingChanged(bool isPicking)
        {
            if (isPicking && pickingTimer == null)
            {
                StartPickingTimer();
            }
            if (!isPicking && pickingTimer != null)
            {
                StopPickingTimer();
            }
        }

        private void Pick(object sender, EventArgs e)
        {
            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            if (!isShiftDown || !IsPicking)
            {
                if (!IsPicking && pickingTimer != null)
                {
                    StopPickingTimer();
                }
                else
                {
                    CoverWindowManager.HideAllCoverWindows();
                }
                return;
            }
            else
            {
                CoverWindowManager.CoverUpAllScreens();
            }

            try
            {
                Point pt = CaptureScreenshot.GetMouseScreenPosition();
                BitmapSource bmp = CaptureScreenshot.Capture(new Rect(pt, new Size(1, 1)));
                var pixels = new byte[4];
                bmp.CopyPixels(pixels, 4, 0);
                SelectedColor = Color.FromArgb(0xFF, pixels[2], pixels[1], pixels[0]);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }

        private static void SelectedCustomColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SolidColorPicker colorPicker = (SolidColorPicker)d;
            colorPicker.SelectedColor = (Color)e.NewValue;
        }

        private static void SelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && ((Color)e.NewValue) != Color.FromArgb(0, 1, 1, 1))
            {
                SolidColorPicker colorPicker = (SolidColorPicker)d;

                if (!colorPicker.stopColorChangedEvent)
                {
                    colorPicker.OnSelectedValueChanged();
                    colorPicker.OnSelectionChanged();
                }
            }
        }

        private void OnSelectedValueChanged()
        {
            // don't update the HSV controls if the original change was H, S or V.
            if (updateHSV)
            {
                byte[] hsv = ColorHelper.ColorToHsvBytes(SelectedColor);
                hue = hsv[0];
                saturation = hsv[1];
                brightness = hsv[2];
                OnPropertyChanged("Hue");
                OnPropertyChanged("Saturation");
                OnPropertyChanged("Brightness");
            }
            OnPropertyChanged("Red");
            OnPropertyChanged("Green");
            OnPropertyChanged("Blue");
            OnPropertyChanged("Alpha");
            OnPropertyChanged("ColorName");
            OnPropertyChanged("AlphaGradient");
            OnPropertyChanged("SaturationGradient");
            OnPropertyChanged("BrightnessGradient");
        }

        protected virtual void OnSelectedItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var handler = SelectedItemDoubleClick;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static ObservableCollection<Color> CreateEmptyPalette()
        {
            var palette = new ObservableCollection<Color>();
            palette.Add(Colors.Transparent);
            palette.Add(ColorConverter.FromHtml("#FFFFFFFF"));
            palette.Add(ColorConverter.FromHtml("#FFCCCCCC"));
            palette.Add(ColorConverter.FromHtml("#FF999999"));
            palette.Add(ColorConverter.FromHtml("#FF666666"));
            palette.Add(ColorConverter.FromHtml("#FF000000"));
            palette.Add(ColorConverter.FromHtml("#FF990000"));
            palette.Add(ColorConverter.FromHtml("#FFCC0000"));
            palette.Add(ColorConverter.FromHtml("#FFFF0000"));
            palette.Add(ColorConverter.FromHtml("#FFFF9999"));
            palette.Add(ColorConverter.FromHtml("#FFFF6600"));
            palette.Add(ColorConverter.FromHtml("#FFFF9900"));
            palette.Add(ColorConverter.FromHtml("#FFCC9900"));
            palette.Add(ColorConverter.FromHtml("#FFFFCC33"));
            palette.Add(ColorConverter.FromHtml("#FFFFFF00"));
            palette.Add(ColorConverter.FromHtml("#FFCCCC99"));
            palette.Add(ColorConverter.FromHtml("#FF99CC33"));
            palette.Add(ColorConverter.FromHtml("#FF009900"));
            palette.Add(ColorConverter.FromHtml("#FF336600"));
            palette.Add(ColorConverter.FromHtml("#FF339999"));

            return palette;
        }

        public static ObservableCollection<Color> CreateDefaultPalette()
        {
            var palette = new ObservableCollection<Color>();
            palette.Add(Colors.Transparent);
            palette.Add(ColorConverter.FromHtml("#FFFFFFFF"));
            palette.Add(ColorConverter.FromHtml("#FFCCCCCC"));
            palette.Add(ColorConverter.FromHtml("#FF999999"));
            palette.Add(ColorConverter.FromHtml("#FF666666"));
            palette.Add(ColorConverter.FromHtml("#FF000000"));
            palette.Add(ColorConverter.FromHtml("#FF990000"));
            palette.Add(ColorConverter.FromHtml("#FFCC0000"));
            palette.Add(ColorConverter.FromHtml("#FFFF0000"));
            palette.Add(ColorConverter.FromHtml("#FFFF9999"));
            palette.Add(ColorConverter.FromHtml("#FFFF6600"));
            palette.Add(ColorConverter.FromHtml("#FFFF9900"));
            palette.Add(ColorConverter.FromHtml("#FFCC9900"));

            palette.Add(ColorConverter.FromHtml("#FFFFCC33"));
            palette.Add(ColorConverter.FromHtml("#FFFFFF00"));
            palette.Add(ColorConverter.FromHtml("#FFCCCC99"));
            palette.Add(ColorConverter.FromHtml("#FF99CC33"));
            palette.Add(ColorConverter.FromHtml("#FF009900"));
            palette.Add(ColorConverter.FromHtml("#FF336600"));
            palette.Add(ColorConverter.FromHtml("#FF339999"));
            palette.Add(ColorConverter.FromHtml("#FFCCFFFF"));
            palette.Add(ColorConverter.FromHtml("#FF99CCFF"));
            palette.Add(ColorConverter.FromHtml("#FF3399FF"));
            palette.Add(ColorConverter.FromHtml("#FF0066CC"));
            palette.Add(ColorConverter.FromHtml("#FF9933CC"));
            palette.Add(ColorConverter.FromHtml("#FF191970"));
            return palette;
        }

        private void SolidColorPicker_Unloaded(object sender, RoutedEventArgs e)
        {
            StopPickingTimer();
        }

        private void SolidColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsPicking && pickingTimer == null)
            {
                StartPickingTimer();
            }
        }

        private void StartPickingTimer()
        {
            pickingTimer = new DispatcherTimer();
            pickingTimer.Interval = TimeSpan.FromMilliseconds(100);
            pickingTimer.Tick += Pick;
            pickingTimer.Start();
        }

        private void StopPickingTimer()
        {
            if (pickingTimer != null)
            {
                pickingTimer.Tick -= Pick;
                pickingTimer.Stop();
                pickingTimer = null;
            }
        }

        internal void SyncBaseColor(System.Drawing.Color color)
        {
            stopColorChangedEvent = true;
            SelectedColor = DrawingColorToMediaColorConverter.Convert(color);
            stopColorChangedEvent = false;
        }
    }
}
