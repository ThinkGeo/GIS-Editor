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
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class NormalColorPicker : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ColorPanel colorPanel;
        private Slider slA, slR, slG, slB, slH, slS, slL;
        private Numeric nA, nR, nG, nB, nH, nS, nL;
        private Color selectedColor;
        private bool isChangedByElement;

        public NormalColorPicker()
        {
            DefaultStyleKey = typeof(NormalColorPicker);
        }

        public Color SelectedColor
        {
            get
            {
                if (colorPanel != null)
                {
                    selectedColor = colorPanel.SelectedColor;
                }
                return selectedColor;
            }
            set
            {
                bool isChanged = false;
                if (selectedColor != value)
                {
                    isChanged = true;
                }

                selectedColor = value;
                OnPropertyChanged("SelectedColor");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (colorPanel != null && isChanged)
                    {
                        colorPanel.SelectedColor = value;
                    }
                }));
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            colorPanel = (ColorPanel)GetTemplateChild("CP1");
            colorPanel.SelectedColor = SelectedColor;
            colorPanel.SelectedColorChanged -= new System.EventHandler<SelectedColorChangedColorPanelEventArgs>(colorPanel_SelectedColorChanged);
            colorPanel.SelectedColorChanged += new System.EventHandler<SelectedColorChangedColorPanelEventArgs>(colorPanel_SelectedColorChanged);

            slA = (Slider)GetTemplateChild("slA");
            slR = (Slider)GetTemplateChild("slR");
            slG = (Slider)GetTemplateChild("slG");
            slB = (Slider)GetTemplateChild("slB");
            slH = (Slider)GetTemplateChild("slH");
            slS = (Slider)GetTemplateChild("slS");
            slL = (Slider)GetTemplateChild("slL");

            slA.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slR.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slG.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slB.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slH.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slS.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slL.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);

            slA.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slR.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slG.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slB.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slH.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slS.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);
            slL.ValueChanged += new RoutedPropertyChangedEventHandler<double>(Slider_ValueChanged);

            nA = (Numeric)GetTemplateChild("nA");
            nR = (Numeric)GetTemplateChild("nR");
            nG = (Numeric)GetTemplateChild("nG");
            nB = (Numeric)GetTemplateChild("nB");
            nH = (Numeric)GetTemplateChild("nH");
            nS = (Numeric)GetTemplateChild("nS");
            nL = (Numeric)GetTemplateChild("nL");

            nA.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nR.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nG.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nB.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nH.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nS.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);
            nL.ValueChanged -= new RoutedEventHandler(Numeric_ValueChanged);

            nA.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nR.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nG.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nB.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nH.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nS.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);
            nL.ValueChanged += new RoutedEventHandler(Numeric_ValueChanged);

            OnPropertyChanged("SelectedColor");
            //SyncWithColor(SelectedColor);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void colorPanel_SelectedColorChanged(object sender, SelectedColorChangedColorPanelEventArgs e)
        {
            if (SelectedColor != e.NewColor)
            //if(e.OldColor != e.NewColor)
            {
                SelectedColor = e.NewColor;
            }

            SyncWithColor(SelectedColor);
        }

        private void SetCurrentColorForSlider(string tagName)
        {
            byte a = (byte)slA.Value;
            byte r = (byte)slR.Value;
            byte g = (byte)slG.Value;
            byte b = (byte)slB.Value;
            int h = (int)slH.Value;
            double s = slS.Value / 100d;
            double l = slL.Value / 100d;
            SetColor(tagName, a, r, g, b, h, s, l);
        }

        private void SetCurrentColorForNumeric(string tagName)
        {
            byte a = (byte)nA.Value;
            byte r = (byte)nR.Value;
            byte g = (byte)nG.Value;
            byte b = (byte)nB.Value;
            int h = (int)nH.Value;
            double s = (double)nS.Value / 100d;
            double l = (double)nL.Value / 100d;
            SetColor(tagName, a, r, g, b, h, s, l);
        }

        private void SetColor(string tagName, byte a, byte r, byte g, byte b, int h, double s, double l)
        {
            Color color = ColorConverter.FromHSL(h, s, l);

            switch (tagName)
            {
                case "A":
                case "R":
                case "G":
                case "B":
                    colorPanel.SelectedColor = Color.FromArgb(a, r, g, b);
                    //SyncWithARGB(a, r, g, b);
                    HSL hsl = colorPanel.SelectedColor.GetHSL();
                    SyncWithHSL(hsl.Hue, hsl.Saturation * 100, hsl.Luminance * 100);
                    break;
                case "H":
                case "S":
                case "L":
                    colorPanel.SelectedColor = Color.FromArgb(a, color.R, color.G, color.B);
                    //SyncWithHSL(h, s, l);
                    SyncWithARGB(a, color.R, color.G, color.B);
                    break;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            isChangedByElement = true;
            Slider slider = (Slider)sender;
            if (!slider.IsFocused) return;

            string tagName = slider.Tag.ToString();
            SetCurrentColorForSlider(tagName);
            isChangedByElement = false;
        }

        private void Numeric_ValueChanged(object sender, RoutedEventArgs e)
        {
            isChangedByElement = true;
            Numeric numeric = (Numeric)sender;
            if (!numeric.IsFocused) return;

            string tagName = numeric.Tag.ToString();
            SetCurrentColorForNumeric(tagName);
            isChangedByElement = false;
        }

        private void SyncWithHSL(int h, double s, double l)
        {
            slH.Value = h;
            slS.Value = s;
            slL.Value = l;
            nH.Value = h;
            nS.Value = (decimal)s;
            nL.Value = (decimal)l;
        }

        private void SyncWithARGB(byte a, byte r, byte g, byte b)
        {
            slA.Value = a;
            slR.Value = r;
            slG.Value = g;
            slB.Value = b;
            nA.Value = a;
            nR.Value = r;
            nG.Value = g;
            nB.Value = b;
        }

        private void SyncWithColor(Color color)
        {
            if (isChangedByElement && !colorPanel.isColorChangedByMouse) return;
            HSL hsl = color.GetHSL();
            SyncWithARGB(color.A, color.R, color.G, color.B);
            SyncWithHSL(hsl.Hue, hsl.Saturation * 100, hsl.Luminance * 100);
        }
    }
}