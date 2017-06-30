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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class HatchPicker : Control, INotifyPropertyChanged
    {
        private const int defaultPatternSize = 35;
        private readonly static Color defaultBackgroundColor = Color.White;
        private readonly static Color defaultForegroundColor = Color.Black;
        private ListBox listBox;
        private bool stopRaisingColorChangedEvent;
        private EventSetter solidColorListDoubleClickEventSetter;

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty BitmapSourcesProperty =
            DependencyProperty.Register("BitmapSources", typeof(ObservableCollection<BitmapSource>), typeof(HatchPicker), new UIPropertyMetadata(CreateDefaultPattern()));

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(Color), typeof(HatchPicker), new UIPropertyMetadata(defaultForegroundColor, new PropertyChangedCallback(ColorChanged)));

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Color), typeof(HatchPicker), new UIPropertyMetadata(defaultBackgroundColor, new PropertyChangedCallback(ColorChanged)));

        public static readonly DependencyProperty PatternSizeProperty =
            DependencyProperty.Register("PatternSize", typeof(int), typeof(HatchPicker), new UIPropertyMetadata(defaultPatternSize));

        public static readonly DependencyProperty SelectedBitmapSourceProperty =
            DependencyProperty.Register("SelectedBitmapSource", typeof(BitmapSource), typeof(HatchPicker), new PropertyMetadata(null, new PropertyChangedCallback(SelectedBitmapSourceChanged)));

        public event EventHandler SelectionChanged;

        public event MouseButtonEventHandler SelectedItemDoubleClick;

        protected void OnSelectionChanged()
        {
            EventHandler handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public HatchPicker()
        {
            DefaultStyleKey = typeof(HatchPicker);
        }

        public HatchStyle SelectedHatchStyle
        {
            get
            {
                HatchStyle selectedHatchStyle = (HatchStyle)0;
                if (SelectedBitmapSource != null)
                {
                    selectedHatchStyle = (HatchStyle)Enum.Parse(typeof(HatchStyle), SelectedBitmapSource.GetValue(Canvas.NameProperty).ToString(), true);
                }

                return selectedHatchStyle;
            }
            set
            {
                SelectedBitmapSource = BitmapSources.FirstOrDefault(s => { return s.GetValue(Canvas.NameProperty).Equals(value.ToString()); });
            }
        }

        public void SyncBaseColor(Color baseColor)
        {
            stopRaisingColorChangedEvent = true;
            ForegroundColor = baseColor;
            stopRaisingColorChangedEvent = false;
        }

        public BitmapSource SelectedBitmapSource
        {
            get { return (BitmapSource)GetValue(SelectedBitmapSourceProperty); }
            set { SetValue(SelectedBitmapSourceProperty, value); }
        }

        public ObservableCollection<BitmapSource> BitmapSources
        {
            get { return (ObservableCollection<BitmapSource>)GetValue(BitmapSourcesProperty); }
            set { SetValue(BitmapSourcesProperty, value); }
        }

        public Color ForegroundColor
        {
            get { return (Color)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public int PatternSize
        {
            get { return (int)GetValue(PatternSizeProperty); }
            set { SetValue(PatternSizeProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button btnForeground = (Button)GetTemplateChild("btnForeground");
            Button btnBackground = (Button)GetTemplateChild("btnBackground");
            listBox = (ListBox)GetTemplateChild("PatternListBox");

            if (solidColorListDoubleClickEventSetter == null)
            {
                solidColorListDoubleClickEventSetter = new EventSetter(ListBoxItem.MouseDoubleClickEvent, new MouseButtonEventHandler((s, e) => OnSelectedItemDoubleClick(s, e)));
            }

            if (listBox.ItemContainerStyle == null)
            {
                listBox.ItemContainerStyle = new Style(typeof(ListBoxItem));
                listBox.ItemContainerStyle.Setters.Add(solidColorListDoubleClickEventSetter);
                listBox.ItemContainerStyle.Setters.Add(new Setter(ListBoxItem.MarginProperty, new Thickness(2)));
            }

            btnForeground.Click -= new RoutedEventHandler(btnForeground_Click);
            btnBackground.Click -= new RoutedEventHandler(btnForeground_Click);

            btnForeground.Click += new RoutedEventHandler(btnForeground_Click);
            btnBackground.Click += new RoutedEventHandler(btnBackground_Click);
        }

        public void UnSelect()
        {
            if (listBox != null)
            {
                listBox.SelectedItem = null;
            }
        }

        protected virtual void OnSelectedItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var handler = SelectedItemDoubleClick;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void btnForeground_Click(object sender, RoutedEventArgs e)
        {
            SolidColorPicker colorPicker = new SolidColorPicker();
            colorPicker.SelectedColor = System.Windows.Media.Color.FromArgb(ForegroundColor.A, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B);
            ColorPickerWindow window = new ColorPickerWindow();
            window.SolidColorPanel.Children.Add(colorPicker);
            if (window.ShowDialog().GetValueOrDefault())
            {
                ForegroundColor = Color.FromArgb(colorPicker.SelectedColor.A, colorPicker.SelectedColor.R, colorPicker.SelectedColor.G, colorPicker.SelectedColor.B);
            }
        }

        private void btnBackground_Click(object sender, RoutedEventArgs e)
        {
            SolidColorPicker colorPicker = new SolidColorPicker();
            colorPicker.SelectedColor = System.Windows.Media.Color.FromArgb(BackgroundColor.A, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);

            ColorPickerWindow window = new ColorPickerWindow();
            window.SolidColorPanel.Children.Add(colorPicker);
            if (window.ShowDialog().GetValueOrDefault())
            {
                BackgroundColor = Color.FromArgb(colorPicker.SelectedColor.A, colorPicker.SelectedColor.R, colorPicker.SelectedColor.G, colorPicker.SelectedColor.B);
            }
        }

        private static ObservableCollection<BitmapSource> CreateDefaultPattern()
        {
            ObservableCollection<BitmapSource> patternSources = new ObservableCollection<BitmapSource>();
            UpdatePatternSources(patternSources, defaultPatternSize, defaultBackgroundColor, defaultForegroundColor);
            return patternSources;
        }

        private static void UpdatePatternSources(ObservableCollection<BitmapSource> patternSources, int patternSize, Color backgroundColor, Color foregroundColor)
        {
            string[] hatchStyleNames = Enum.GetNames(typeof(HatchStyle));
            Bitmap sampleBitmap = new Bitmap(patternSize, patternSize);
            Graphics sampleGraphic = Graphics.FromImage(sampleBitmap);

            try
            {
                patternSources.Clear();
                foreach (string hatchStyleName in hatchStyleNames)
                {
                    sampleGraphic.Clear(Color.Transparent);
                    sampleGraphic.FillRectangle(new HatchBrush((HatchStyle)Enum.Parse(typeof(HatchStyle), hatchStyleName), foregroundColor, backgroundColor), 0, 0, patternSize, patternSize);
                    sampleGraphic.Flush();

                    MemoryStream ms = new MemoryStream();
                    sampleBitmap.Save(ms, ImageFormat.Png);

                    BitmapImage patternSource = new BitmapImage();
                    patternSource.SetValue(Canvas.NameProperty, hatchStyleName);
                    patternSource.BeginInit();
                    patternSource.StreamSource = ms;
                    patternSource.EndInit();
                    patternSource.Freeze();
                    patternSources.Add(patternSource);
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                sampleBitmap.Dispose();
            }
        }

        private static void ColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;

            HatchPicker hatchPicker = (HatchPicker)sender;
            if (hatchPicker.BitmapSources != null)
            {
                string selectedHatchStyleName = HatchStyle.BackwardDiagonal.ToString();
                if (hatchPicker.SelectedBitmapSource != null)
                {
                    selectedHatchStyleName = hatchPicker.SelectedBitmapSource.GetValue(Canvas.NameProperty).ToString();
                }

                HatchPicker.UpdatePatternSources(hatchPicker.BitmapSources, hatchPicker.PatternSize, hatchPicker.BackgroundColor, hatchPicker.ForegroundColor);
                if (hatchPicker.SelectedBitmapSource == null)
                {
                    hatchPicker.SelectedBitmapSource = hatchPicker.BitmapSources.FirstOrDefault(s => { return s.GetValue(Canvas.NameProperty).Equals(selectedHatchStyleName); });
                }
                else
                {
                    string oldHatchStyleName = hatchPicker.SelectedBitmapSource.GetValue(Canvas.NameProperty).ToString();
                    if (oldHatchStyleName != selectedHatchStyleName)
                    {
                        hatchPicker.SelectedBitmapSource = hatchPicker.BitmapSources.FirstOrDefault(s => { return s.GetValue(Canvas.NameProperty).Equals(selectedHatchStyleName); });
                    }
                }
            }
        }

        private static void SelectedBitmapSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                HatchPicker hatchPicker = (HatchPicker)sender;
                hatchPicker.OnPropertyChanged("SelectedHatchStyle");

                if (!hatchPicker.stopRaisingColorChangedEvent)
                {
                    hatchPicker.OnSelectionChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}