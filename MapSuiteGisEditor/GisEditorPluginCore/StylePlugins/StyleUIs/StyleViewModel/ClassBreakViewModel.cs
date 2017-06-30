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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakViewModel : ViewModelBase
    {
        private int classesCount;
        private GeoBrush baseBrush;
        private GeoBrush endBrush;
        private ClassBreakBy selectedColorField;
        private IEnumerable<String> colorFields;
        private bool invertColorOrder;
        private double lowValue;
        private double highValue;
        private string startColorName;
        private string endColorName;
        private double minimum;
        private double currentValue;
        private double maximum;
        private Visibility colorPickerVisibility;
        private Visibility sliderVisibility;

        public ClassBreakViewModel()
        {
            minimum = 0;
            classesCount = 5;
            baseBrush = new GeoSolidBrush(GeoColor.FromHtml("#9ACD32"));
            endBrush = new GeoSolidBrush(GeoColor.StandardColors.Red);
            colorFields = new String[] { "Hue", "Saturation", "Lightness" };
            selectedColorField = ClassBreakBy.Hue;
            startColorName = "Start Color";
            endColorName = "End Color";
            colorPickerVisibility = Visibility.Visible;
            sliderVisibility = Visibility.Collapsed;
        }

        public double HighValue
        {
            get { return highValue; }
            set
            {
                highValue = value;
                RaisePropertyChanged(()=>HighValue);
            }
        }

        public double LowValue
        {
            get { return lowValue; }
            set
            {
                lowValue = value;
                RaisePropertyChanged(()=>LowValue);
            }
        }

        public GeoBrush BaseBrush
        {
            get { return baseBrush; }
            set
            {
                baseBrush = value;
                if (!selectedColorField.Equals("Hue"))
                {
                    var solidBrush = (GeoSolidBrush)baseBrush;
                    HSL hsl = GetHslFromRgb(solidBrush.Color.RedComponent, solidBrush.Color.GreenComponent, solidBrush.Color.BlueComponent);
                    Maximum = selectedColorField.Equals("Saturation") ? hsl.Saturation : hsl.Luminance;
                    if (currentValue >= maximum)
                    {
                        CurrentValue = Minimum;
                    }
                }
                RaisePropertyChanged(()=>BaseBrush);
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public GeoBrush EndBrush
        {
            get { return endBrush; }
            set
            {
                endBrush = value;
                RaisePropertyChanged(()=>EndBrush);
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public int ClassesCount
        {
            get { return classesCount; }
            set
            {
                classesCount = value;
                RaisePropertyChanged(()=>ClassesCount);
                if (value > 0) RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public ClassBreakBy SelectedColorField
        {
            get { return selectedColorField; }
            set
            {
                selectedColorField = value;
                RaisePropertyChanged(()=>SelectedColorField);
                RaisePropertyChanged(()=>PreviewSource);
                switch (selectedColorField)
                {
                    case ClassBreakBy.Hue:
                        StartColorName = "Start Color";
                        EndColorName = "End Color";
                        ColorPickerVisibility = Visibility.Visible;
                        SliderVisibility = Visibility.Collapsed;
                        break;
                    default:
                        StartColorName = "Base Color";
                        EndColorName = "Min Value";
                        SliderVisibility = Visibility.Visible;
                        ColorPickerVisibility = Visibility.Collapsed;
                        BaseBrush = baseBrush;
                        break;
                }
            }
        }

        public IEnumerable<String> ColorFields
        {
            get { return colorFields; }
        }

        public string StartColorName
        {
            get { return startColorName; }
            set
            {
                startColorName = value;
                RaisePropertyChanged(()=>StartColorName);
            }
        }

        public string EndColorName
        {
            get { return endColorName; }
            set
            {
                endColorName = value;
                RaisePropertyChanged(()=>EndColorName);
            }
        }

        public ImageSource PreviewSource
        {
            get { return GetPreviewSource(); }
        }

        public bool InvertColorOrder
        {
            get { return invertColorOrder; }
            set
            {
                invertColorOrder = value;
                RaisePropertyChanged(()=>InvertColorOrder);
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public double Minimum
        {
            get { return minimum; }
        }

        public double CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value;
                RaisePropertyChanged(()=>CurrentValue);
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public double Maximum
        {
            get { return maximum; }
            set
            {
                maximum = value;
                RaisePropertyChanged(()=>Maximum);
            }
        }

        public Visibility ColorPickerVisibility
        {
            get { return colorPickerVisibility; }
            set
            {
                colorPickerVisibility = value;
                RaisePropertyChanged(()=>ColorPickerVisibility);
            }
        }

        public Visibility SliderVisibility
        {
            get { return sliderVisibility; }
            set
            {
                sliderVisibility = value;
                RaisePropertyChanged(()=>SliderVisibility);
            }
        }

        public Collection<GeoSolidBrush> CollectBrushes()
        {
            if (ClassesCount > 0 && BaseBrush is GeoSolidBrush)
            {
                Collection<GeoSolidBrush> brushes = GetBreakdownBrushes(ClassesCount, (GeoSolidBrush)BaseBrush, (GeoSolidBrush)endBrush, selectedColorField);

                if (InvertColorOrder) brushes = new Collection<GeoSolidBrush>(brushes.Reverse().ToList());
                return brushes;
            }
            else
            {
                return null;
            }
        }

        private ImageSource GetPreviewSource()
        {
            var previewWidth = 300.0;
            var previewHeight = 25.0;
            var brushes = CollectBrushes();
            var blockWidth = previewWidth / brushes.Count;

            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();

            double start = 0;
            foreach (var brush in brushes)
            {
                byte a = brush.Color.AlphaComponent;
                byte r = brush.Color.RedComponent;
                byte g = brush.Color.GreenComponent;
                byte b = brush.Color.BlueComponent;
                var fillBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
                var fillRect = new Rect(start, 0, blockWidth, previewHeight);
                drawingContext.DrawRectangle(fillBrush, null, fillRect);
                start += blockWidth;
            }

            drawingContext.Close();

            RenderTargetBitmap imageSource = new RenderTargetBitmap((int)previewWidth, (int)previewHeight, 96, 96, PixelFormats.Pbgra32);
            imageSource.Render(drawingVisual);

            return imageSource;
        }

        private Collection<GeoSolidBrush> GetBreakdownBrushes(int breakCount, GeoSolidBrush baseBrush, GeoSolidBrush endBrush, ClassBreakBy breakColorMode)
        {
            Collection<GeoSolidBrush> brushes = new Collection<GeoSolidBrush>();
            HSL baseHsl = GetHslFromRgb(baseBrush.Color.RedComponent, baseBrush.Color.GreenComponent, baseBrush.Color.BlueComponent);
            HSL endHsl = null;
            if (breakColorMode == ClassBreakBy.Hue)
            {
                endHsl = GetHslFromRgb(endBrush.Color.RedComponent, endBrush.Color.GreenComponent, endBrush.Color.BlueComponent);
                var averageH = (endHsl.Hue - baseHsl.Hue) / (breakCount - 1);
                var averageS = (endHsl.Saturation - baseHsl.Saturation) / (breakCount - 1);
                var averageL = (endHsl.Luminance - baseHsl.Luminance) / (breakCount - 1);
                var averageAlpha = (endBrush.Color.AlphaComponent - baseBrush.Color.AlphaComponent) / (breakCount - 1);
                brushes.Add(baseBrush);
                for (int i = 1; i <= breakCount - 2; i++)
                {
                    HSL generatedHsl = new HSL(baseHsl.Hue, baseHsl.Saturation, baseHsl.Luminance);
                    generatedHsl.Hue += averageH * i;
                    generatedHsl.Saturation += averageS * i;
                    generatedHsl.Luminance += averageL * i;

                    brushes.Add(GetSolidBrushFromHsl((byte)((int)baseBrush.Color.AlphaComponent + averageAlpha * i), generatedHsl));
                }
                brushes.Add(endBrush);
            }
            else
            {
                endHsl = new HSL(baseHsl.Hue, baseHsl.Saturation, baseHsl.Luminance);
                if (breakColorMode == ClassBreakBy.Saturation)
                {
                    endHsl.Saturation = this.currentValue;
                    var averageS = (baseHsl.Saturation - currentValue) / (breakCount - 1);
                    brushes.Add(GetSolidBrushFromHsl(baseBrush.Color.AlphaComponent, endHsl));
                    for (int i = 1; i <= breakCount - 2; i++)
                    {
                        HSL generatedHsl = new HSL(endHsl.Hue, endHsl.Saturation, endHsl.Luminance);
                        generatedHsl.Saturation += averageS * i;
                        brushes.Add(GetSolidBrushFromHsl(baseBrush.Color.AlphaComponent, generatedHsl));
                    }
                    brushes.Add(baseBrush);
                }
                else
                {
                    endHsl.Luminance = this.currentValue;
                    var averageL = (baseHsl.Luminance - currentValue) / (breakCount - 1);
                    brushes.Add(GetSolidBrushFromHsl(baseBrush.Color.AlphaComponent, endHsl));
                    for (int i = 1; i <= breakCount - 2; i++)
                    {
                        HSL generatedHsl = new HSL(endHsl.Hue, endHsl.Saturation, endHsl.Luminance);
                        generatedHsl.Luminance += averageL * i;

                        brushes.Add(GetSolidBrushFromHsl(baseBrush.Color.AlphaComponent, generatedHsl));
                    }
                    brushes.Add(baseBrush);
                }
            }
            return brushes;
        }

        private static HSL GetHslFromRgb(byte r, byte g, byte b)
        {
            RGB rgb = new RGB(r, g, b);
            HSL hsl = new HSL();
            ColorConverter.RGB2HSL(rgb, hsl);
            return hsl;
        }

        private static GeoSolidBrush GetSolidBrushFromHsl(byte alpha, HSL hsl)
        {
            RGB rgb = new RGB();
            ColorConverter.HSL2RGB(hsl, rgb);
            return new GeoSolidBrush(new GeoColor(alpha, rgb.Red, rgb.Green, rgb.Blue));
        }
    }
}