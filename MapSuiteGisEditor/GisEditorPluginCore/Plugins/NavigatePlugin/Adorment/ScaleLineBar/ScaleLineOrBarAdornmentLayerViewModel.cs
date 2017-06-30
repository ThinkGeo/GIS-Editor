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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ScaleLineOrBarAdornmentLayerViewModel : ViewModelBase
    {
        private ScaleType scaleType;
        private string name;
        private int width;
        private int height;
        private AdornmentLocation location;
        private int offsetX;
        private int offsetY;
        private int maximumWidth;
        private int thickness;
        private GeoBrush foreColor;
        private GeoBrush alteColor;
        private UnitSystem selectedScaleBarUnitSystem;
        private ScaleLineAdornmentLayer scaleLine;
        private ScaleBarAdornmentLayer scaleBar;
        private AreaStyle backMask;
        private ScaleNumericFormatType numericFormatType;
        private ScaleLineUnitSystem selectedScaleLineUnitSystem;

        [NonSerialized]
        private DispatcherTimer delayPreviewTimer;
        [NonSerialized]
        private RelayCommand configureBackgroundMaskModeCommand;

        public ScaleLineOrBarAdornmentLayerViewModel()
        {
            ForeColor = new GeoSolidBrush(GeoColor.SimpleColors.Black);
            AlteColor = new GeoSolidBrush(GeoColor.StandardColors.White);
            BackMask = new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent));
            Width = 392;
            height = 32;
            MaximumWidth = 392;
            Thickness = 8;
            selectedScaleLineUnitSystem = ScaleLineUnitSystem.ImperialAndMetric;
            InitializeDelayProcess();
        }

        public RelayCommand ConfigureBackgroundMaskModeCommand
        {
            get
            {
                if (configureBackgroundMaskModeCommand == null)
                {
                    configureBackgroundMaskModeCommand = new RelayCommand(ConfigureBackgroundMaskMode);
                }

                return configureBackgroundMaskModeCommand;
            }
        }

        public BitmapImage PreviewSource
        {
            get
            {
                AdornmentLayer drawingLayer = ToActualAdornmentLayer();
                lock (drawingLayer)
                {
                    using (System.Drawing.Bitmap nativeImage = new System.Drawing.Bitmap(450, 80))
                    {
                        PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas
                        {
                            DrawingQuality = DrawingQuality.HighQuality,
                        };

                        AdornmentLocation location = drawingLayer.Location;
                        drawingLayer.Location = AdornmentLocation.CenterLeft;
                        geoCanvas.BeginDrawing(nativeImage, new RectangleShape(-170, 85, 0, 0), GeographyUnit.DecimalDegree);
                        drawingLayer.Draw(geoCanvas, new Collection<SimpleCandidate>());
                        geoCanvas.EndDrawing();
                        drawingLayer.Location = location;

                        MemoryStream streamSource = new MemoryStream();
                        nativeImage.Save(streamSource, ImageFormat.Png);
                        BitmapImage imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.StreamSource = streamSource;
                        imageSource.EndInit();
                        imageSource.Freeze();

                        return imageSource;
                    }
                }
            }
        }

        public ScaleNumericFormatType NumericFormatType
        {
            get { return numericFormatType; }
            set
            {
                numericFormatType = value;
                RaisePropertyChanged(()=>NumericFormatType);
                LazyPreviewSourceChanged();
            }
        }

        public ScaleType ScaleType
        {
            get { return scaleType; }
            set
            {
                scaleType = value;
                RaisePropertyChanged(()=>ScaleType);
                RaisePropertyChanged(()=>IsCommonSettinEnabled);
                RaisePropertyChanged(()=>IsSpecialSettingEnabled);
                RaisePropertyChanged(()=>ScaleLineUnitVisibility);
                RaisePropertyChanged(()=>ScaleBarUnitVisibility);
                LazyPreviewSourceChanged();
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(()=>Name);
            }
        }

        public int Width
        {
            get { return width; }
            set
            {
                width = value;
                RaisePropertyChanged(()=>Width);
                LazyPreviewSourceChanged();
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                height = value;
                RaisePropertyChanged(()=>Height);
                LazyPreviewSourceChanged();
            }
        }

        public AdornmentLocation Location
        {
            get { return location; }
            set
            {
                location = value;
                RaisePropertyChanged(()=>Location);
                LazyPreviewSourceChanged();
            }
        }

        public int OffsetX
        {
            get { return offsetX; }
            set
            {
                offsetX = value;
                RaisePropertyChanged(()=>OffsetX);
                LazyPreviewSourceChanged();
            }
        }

        public int OffsetY
        {
            get { return offsetY; }
            set
            {
                offsetY = value;
                RaisePropertyChanged(()=>OffsetY);
                LazyPreviewSourceChanged();
            }
        }

        public int MaximumWidth
        {
            get { return maximumWidth; }
            set
            {
                maximumWidth = value;
                RaisePropertyChanged(()=>MaximumWidth);
                LazyPreviewSourceChanged();
            }
        }

        public int Thickness
        {
            get { return thickness; }
            set
            {
                thickness = value;
                RaisePropertyChanged(()=>Thickness);
                LazyPreviewSourceChanged();
            }
        }

        public GeoBrush ForeColor
        {
            get { return foreColor; }
            set
            {
                foreColor = value;
                RaisePropertyChanged(()=>ForeColor);
                LazyPreviewSourceChanged();
            }
        }

        public GeoBrush AlteColor
        {
            get { return alteColor; }
            set
            {
                alteColor = value;
                RaisePropertyChanged(()=>AlteColor);
                LazyPreviewSourceChanged();
            }
        }

        public AreaStyle BackMask
        {
            get { return backMask; }
            set
            {
                backMask = value;
                RaisePropertyChanged(()=>BackMask);
                LazyPreviewSourceChanged();
            }
        }

        public ScaleLineUnitSystem SelectedScaleLineUnitSystem
        {
            get { return selectedScaleLineUnitSystem; }
            set
            {
                selectedScaleLineUnitSystem = value;
                RaisePropertyChanged(()=>SelectedScaleLineUnitSystem);
                LazyPreviewSourceChanged();
            }
        }

        public UnitSystem SelectedScaleBarUnitSystem
        {
            get { return selectedScaleBarUnitSystem; }
            set
            {
                selectedScaleBarUnitSystem = value;
                RaisePropertyChanged(()=>SelectedScaleBarUnitSystem);
                LazyPreviewSourceChanged();
            }
        }

        public Visibility ScaleLineUnitVisibility
        {
            get { return scaleType == ScaleType.ScaleBar ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility ScaleBarUnitVisibility
        {
            get { return scaleType == ScaleType.ScaleBar ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsCommonSettinEnabled { get { return ScaleType == ScaleType.ScaleLine; } }

        public bool IsSpecialSettingEnabled { get { return ScaleType == ScaleType.ScaleBar; } }

        private void ConfigureBackgroundMaskMode()
        {
            LazyPreviewSourceChanged();

            if (BackMask == null) SetDefaultStyles();
            AreaStyle editingStyle = (AreaStyle)BackMask.CloneDeep();
            editingStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(editingStyle).Name;
            if (editingStyle.CustomAreaStyles.Count == 0)
            {
                var tempStyle = new AreaStyle();
                tempStyle.Name = editingStyle.Name;
                tempStyle.CustomAreaStyles.Add(editingStyle);
                editingStyle = tempStyle;
            }

            StyleBuilderArguments args = new StyleBuilderArguments();
            args.AvailableUIElements = StyleBuilderUIElements.StyleList;
            args.AvailableStyleCategories = StyleCategories.Area;
            args.AppliedCallback = (editResult) =>
            {
                editingStyle.Name = editResult.CompositeStyle.Name;
                editingStyle.CustomAreaStyles.Clear();
                ((CompositeStyle)editResult.CompositeStyle).Styles.OfType<AreaStyle>().ForEach(s => editingStyle.CustomAreaStyles.Add(s));
                BackMask = (AreaStyle)editingStyle;
            };
            var resultStyle = GisEditor.StyleManager.EditStyles(args, editingStyle);
            if (resultStyle != null) BackMask = resultStyle;
        }

        public AdornmentLayer ToActualAdornmentLayer()
        {
            if (ScaleType == ScaleType.ScaleBar)
            {
                if (scaleBar == null)
                {
                    scaleBar = new ScaleBarAdornmentLayer();
                }

                SetCommonProperties(scaleBar);
                SetSpecialProperties(scaleBar);
                scaleBar.UnitFamily = selectedScaleBarUnitSystem;
                return scaleBar;
            }
            else
            {
                if (scaleLine == null)
                {
                    scaleLine = new ScaleLineAdornmentLayer();
                }

                SetCommonProperties(scaleLine);
                scaleLine.UnitSystem = selectedScaleLineUnitSystem;
                return scaleLine;
            }
        }

        public static ScaleLineOrBarAdornmentLayerViewModel CreateInstance(AdornmentLayer scaleLayer)
        {
            ScaleLineOrBarAdornmentLayerViewModel newLayer = new ScaleLineOrBarAdornmentLayerViewModel();
            if (scaleLayer is ScaleBarAdornmentLayer)
            {
                newLayer.ScaleType = ScaleType.ScaleBar;
                newLayer.NumericFormatType = ScaleNumericFormatType.None;
                string formatString = ((ScaleBarAdornmentLayer)scaleLayer).TextStyle.NumericFormat.ToUpperInvariant();
                switch (formatString)
                {
                    case "C": newLayer.NumericFormatType = ScaleNumericFormatType.Currency; break;
                    case "D": newLayer.NumericFormatType = ScaleNumericFormatType.Decimal; break;
                    case "E": newLayer.NumericFormatType = ScaleNumericFormatType.Scientific; break;
                    case "F": newLayer.NumericFormatType = ScaleNumericFormatType.FixedPoint; break;
                    case "G": newLayer.NumericFormatType = ScaleNumericFormatType.General; break;
                    case "N": newLayer.NumericFormatType = ScaleNumericFormatType.Number; break;
                    case "P": newLayer.NumericFormatType = ScaleNumericFormatType.Percent; break;
                    case "R": newLayer.NumericFormatType = ScaleNumericFormatType.RoundTrip; break;
                    case "X": newLayer.NumericFormatType = ScaleNumericFormatType.Hexadecimal; break;
                    default:
                        break;
                }
                newLayer.selectedScaleBarUnitSystem = ((ScaleBarAdornmentLayer)scaleLayer).UnitFamily;
            }
            else
            {
                newLayer.ScaleType = ScaleType.ScaleLine;
                newLayer.selectedScaleLineUnitSystem = ((ScaleLineAdornmentLayer)scaleLayer).UnitSystem;
            }

            newLayer.Name = scaleLayer.Name;
            newLayer.Width = (int)scaleLayer.Width;
            newLayer.Location = scaleLayer.Location;
            newLayer.OffsetX = (int)scaleLayer.XOffsetInPixel;
            newLayer.OffsetY = (int)scaleLayer.YOffsetInPixel;

            newLayer.BackMask = scaleLayer.BackgroundMask;
            newLayer.Height = (int)scaleLayer.Height;

            if (scaleLayer is ScaleBarAdornmentLayer)
            {
                ScaleBarAdornmentLayer scaleBarLayer = (ScaleBarAdornmentLayer)scaleLayer;
                newLayer.MaximumWidth = scaleBarLayer.MaxWidth;
                newLayer.Thickness = scaleBarLayer.Thickness;
                newLayer.ForeColor = scaleBarLayer.BarBrush;
                newLayer.AlteColor = scaleBarLayer.AlternateBarBrush;
                newLayer.SelectedScaleBarUnitSystem = scaleBarLayer.UnitFamily;
            }

            return newLayer;
        }

        private void SetDefaultStyles()
        {
            BackMask = new AreaStyle(new GeoSolidBrush(GeoColor.StandardColors.Transparent));
            BackMask.Name = GisEditor.StyleManager.GetStylePluginByStyle(BackMask).Name;
        }

        private void SetCommonProperties(AdornmentLayer layer)
        {
            layer.Name = Name;
            layer.Width = width;
            layer.Height = Height;
            layer.Location = Location;
            layer.XOffsetInPixel = offsetX;
            layer.YOffsetInPixel = offsetY;

            layer.BackgroundMask = BackMask;
        }

        private void SetSpecialProperties(ScaleBarAdornmentLayer layer)
        {
            layer.MaxWidth = MaximumWidth;
            layer.Thickness = Thickness;
            layer.BarBrush = ForeColor;
            layer.AlternateBarBrush = AlteColor;
            layer.UnitFamily = SelectedScaleBarUnitSystem;
            layer.TextStyle.NumericFormat = "###,###";
            switch (NumericFormatType)
            {
                case ScaleNumericFormatType.Currency:
                    layer.TextStyle.NumericFormat = "C";
                    break;
                case ScaleNumericFormatType.Decimal:
                    layer.TextStyle.NumericFormat = "D";
                    break;
                case ScaleNumericFormatType.Scientific:
                    layer.TextStyle.NumericFormat = "E";
                    break;
                case ScaleNumericFormatType.FixedPoint:
                    layer.TextStyle.NumericFormat = "F";
                    break;
                case ScaleNumericFormatType.General:
                    layer.TextStyle.NumericFormat = "G";
                    break;
                case ScaleNumericFormatType.Number:
                    layer.TextStyle.NumericFormat = "N";
                    break;
                case ScaleNumericFormatType.Percent:
                    layer.TextStyle.NumericFormat = "P";
                    break;
                case ScaleNumericFormatType.RoundTrip:
                    layer.TextStyle.NumericFormat = "R";
                    break;
                case ScaleNumericFormatType.Hexadecimal:
                    layer.TextStyle.NumericFormat = "X";
                    break;
                case ScaleNumericFormatType.None:
                default:
                    break;
            }
        }

        private void InitializeDelayProcess()
        {
            delayPreviewTimer = new DispatcherTimer();
            delayPreviewTimer.Interval = TimeSpan.FromMilliseconds(400);
            delayPreviewTimer.Tick += (s, e) => { RaisePropertyChanged(()=>PreviewSource); delayPreviewTimer.Stop(); };
        }

        private void LazyPreviewSourceChanged()
        {
            if (delayPreviewTimer != null)
            {
                if (delayPreviewTimer.IsEnabled)
                {
                    delayPreviewTimer.Stop();
                }

                delayPreviewTimer.Start();
            }
        }
    }
}