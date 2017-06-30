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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LegendItemViewModel : ViewModelBase
    {
        private static Dictionary<Type, SymbolStyleType> symbolStyleTypes;

        [Obfuscation(Exclude = true)]
        private LegendItemType legendItemType;

        [Obfuscation(Exclude = true)]
        private LegendItem coreItem;

        [Obfuscation]
        private GeoFontViewModel notifiedGeoFont;

        [NonSerialized]
        private Collection<SimpleCandidate> simpleCandidates;

        [NonSerialized]
        private LegendAdornmentLayerViewModel parent;

        [NonSerialized]
        private DispatcherTimer delayRenderTimer;

        [NonSerialized]
        private RelayCommand configureSymbolStyleCommand;

        [NonSerialized]
        private RelayCommand configureSymbolBackgroundCommand;

        [NonSerialized]
        private RelayCommand configureBackgroundCommand;

        [NonSerialized]
        private RelayCommand configureLabelBackgroundCommand;

        [NonSerialized]
        private RelayCommand applyCommand;

        static LegendItemViewModel()
        {
            symbolStyleTypes = new Dictionary<Type, SymbolStyleType>();
            symbolStyleTypes.Add(typeof(PointStyle), SymbolStyleType.Point_Simple);
            symbolStyleTypes.Add(typeof(FontPointStyle), SymbolStyleType.Point_Font);
            symbolStyleTypes.Add(typeof(SymbolPointStyle), SymbolStyleType.Point_Custom);
            symbolStyleTypes.Add(typeof(AreaStyle), SymbolStyleType.Area_Area);
            symbolStyleTypes.Add(typeof(LineStyle), SymbolStyleType.Line_Line);
        }

        public LegendItemViewModel()
            : this(new LegendItem())
        { }

        public LegendItemViewModel(LegendItem legendItem)
        {
            this.coreItem = legendItem;
            this.coreItem.ImageMask = new AreaStyle() { FillSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Transparent) };
            this.coreItem.TextMask = new AreaStyle() { FillSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Transparent) };
            this.coreItem.BackgroundMask = new AreaStyle() { FillSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Transparent) };
            this.coreItem.TextStyle = new TextStyle() { TextSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Black) };
            this.simpleCandidates = new Collection<SimpleCandidate>();
            InitLazyRenderTimer();
            if (NotifiedGeoFont == null) NotifiedGeoFont = new GeoFontViewModel { FontName = "Arial", FontSize = 10 };
            if (String.IsNullOrEmpty(Text)) Text = LegendHelper.GenerateLegendItemName(new GeoCollection<LegendItemViewModel>());
            NotifiedGeoFont.PropertyChanged += (s, e) => { LazyRenderPreview(); };
            Width = legendItem.Width;
            Height = legendItem.Height;
            TextLeftPadding = 10f;//coreItem.TextLeftPadding;
            TextTopPadding = 5f;//coreItem.TextTopPadding;
            TextRightPadding = 10f;
            ImageWidth = legendItem.ImageWidth;
            ImageHeight = legendItem.ImageHeight;
            ImageTopPadding = 5f;//coreItem.ImageTopPadding;
            ImageBottomPadding = 5f;//coreItem.ImageBottomPadding;
            ImageLeftPadding = 5f;//coreItem.ImageLeftPadding;
            ImageRightPadding = 5f;//coreItem.ImageRightPadding;
            TopPadding = 3f;
            BottomPadding = 0f;
            RightPadding = 0f;
            LeftPadding = 0f;

        }

        public static Dictionary<Type, SymbolStyleType> SymbolStyleTypes
        {
            get { return symbolStyleTypes; }
        }

        #region commands

        public RelayCommand ConfigureSymbolStyleCommand
        {
            get
            {
                if (configureSymbolStyleCommand == null)
                {
                    configureSymbolStyleCommand = new RelayCommand(() =>
                    {
                        LazyRenderPreview();
                        StyleBuilderArguments arguments = new StyleBuilderArguments();
                        arguments.AvailableUIElements = StyleBuilderUIElements.StyleList;

                        arguments.AppliedCallback = (editResult) =>
                        {
                            ImageStyle = editResult.CompositeStyle;
                        };

                        arguments.AvailableStyleCategories = StyleCategories.Line | StyleCategories.Point | StyleCategories.Area;
                        CompositeStyle componentStyle;
                        if (ImageStyle != null)
                        {
                            Style tempStyle = ImageStyle.CloneDeep();
                            componentStyle = new CompositeStyle(tempStyle);
                        }
                        else componentStyle = new CompositeStyle();
                        arguments.StyleToEdit = componentStyle;
                        var styleResult = GisEditor.StyleManager.EditStyle(arguments);
                        if (styleResult != null && !styleResult.Canceled)
                        {
                            ImageStyle = styleResult.CompositeStyle;
                            RaisePropertyChanged(()=>IsStyleGeneralTypePanelEnabled);
                        }
                    });
                }
                return configureSymbolStyleCommand;
            }
        }

        public RelayCommand ConfigureSymbolBackgroundCommand
        {
            get
            {
                if (configureSymbolBackgroundCommand == null)
                {
                    configureSymbolBackgroundCommand = new RelayCommand(() =>
                    {
                        LazyRenderPreview();
                        AreaStyle areaStyle = (AreaStyle)ImageMask.CloneDeep();
                        areaStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(areaStyle).Name;
                        StyleBuilderArguments arguments = new StyleBuilderArguments();
                        arguments.AvailableStyleCategories = StyleCategories.Area;
                        arguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
                        arguments.AppliedCallback = (editResult) =>
                        {
                            AreaStyle tempAreaStyle = new AreaStyle();
                            foreach (var item in editResult.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                tempAreaStyle.CustomAreaStyles.Add(item);
                            }
                            ImageStyle = tempAreaStyle;
                        };

                        var resultStyle = GisEditor.StyleManager.EditStyles(arguments, areaStyle);
                        if (resultStyle != null)
                            ImageMask = resultStyle;
                    });
                }
                return configureSymbolBackgroundCommand;
            }
        }

        public RelayCommand ConfigureBackgroundCommand
        {
            get
            {
                if (configureBackgroundCommand == null)
                {
                    configureBackgroundCommand = new RelayCommand(() =>
                    {
                        AreaStyle areaStyle = (AreaStyle)BackgroundMask.CloneDeep();
                        areaStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(areaStyle).Name;
                        StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                        styleArguments.AvailableStyleCategories = StyleCategories.Area;
                        styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;

                        styleArguments.AppliedCallback = (result) =>
                        {
                            AreaStyle tempAreaStyle = new AreaStyle();
                            foreach (var item in result.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                tempAreaStyle.CustomAreaStyles.Add(item);
                            }
                            BackgroundMask = tempAreaStyle;
                        };

                        var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, areaStyle);
                        if (resultStyle != null)
                            BackgroundMask = resultStyle;
                    });
                } return configureBackgroundCommand;
            }
        }

        public RelayCommand ConfigureLabelBackgroundCommand
        {
            get
            {
                if (configureLabelBackgroundCommand == null)
                {
                    configureLabelBackgroundCommand = new RelayCommand(() =>
                    {
                        AreaStyle areaStyle = (AreaStyle)TextMask.CloneDeep();
                        areaStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(areaStyle).Name;
                        StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                        styleArguments.AvailableStyleCategories = StyleCategories.Area;
                        styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;

                        styleArguments.AppliedCallback = (result) =>
                        {
                            AreaStyle tempAreaStyle = new AreaStyle();
                            foreach (var item in result.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                tempAreaStyle.CustomAreaStyles.Add(item);
                            }
                            TextMask = tempAreaStyle;
                        };

                        var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, areaStyle);
                        if (resultStyle != null)
                            TextMask = resultStyle;
                    });
                } return configureLabelBackgroundCommand;
            }
        }

        public RelayCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new RelayCommand(() =>
                    {
                        RaisePropertyChanged(()=>PreviewSource);
                        var layers = CollectNotifiedLegendLayers();
                        parent.Width = parent.Width;
                        //LegendManagerWindow.OnApplied(layers);
                    });
                } return applyCommand;
            }
        }

        #endregion commands

        public bool IsStyleGeneralTypePanelEnabled
        {
            get { return ImageStyle != null; }
        }

        #region for symbol

        public LegendItemType LegendItemType
        {
            get { return legendItemType; }
            set
            {
                legendItemType = value;
                RaisePropertyChanged(()=>LegendItemType);
                RaisePropertyChanged(()=>TextPreview);
                LazyRenderPreview();
            }
        }

        public string TextPreview
        {
            get
            {
                string suffix = String.Empty;
                switch (LegendItemType)
                {
                    case LegendItemType.Header:
                        return String.Format(CultureInfo.InvariantCulture, "{0} (Title)", Text);
                    case LegendItemType.Footer:
                        return String.Format(CultureInfo.InvariantCulture, "{0} (Footer)", Text);
                    case LegendItemType.Item:
                    default:
                        return Text;
                }
            }
        }

        public Style ImageStyle
        {
            get { return coreItem.ImageStyle; }
            set
            {
                coreItem.ImageStyle = value;
                RaisePropertyChanged(()=>ImageStyle);
                LazyRenderPreview();
            }
        }

        public AreaStyle ImageMask
        {
            get { return coreItem.ImageMask; }
            set
            {
                coreItem.ImageMask = value;
                RaisePropertyChanged(()=>ImageMask);
                LazyRenderPreview();
            }
        }

        public float ImageBottomPadding
        {
            get { return coreItem.ImageBottomPadding; }
            set
            {
                coreItem.ImageBottomPadding = value;
                RaisePropertyChanged(()=>ImageBottomPadding);
                LazyRenderPreview();
            }
        }

        public float ImageTopPadding
        {
            get { return coreItem.ImageTopPadding; }
            set
            {
                coreItem.ImageTopPadding = value;
                RaisePropertyChanged(()=>ImageTopPadding);
                LazyRenderPreview();
            }
        }

        public float ImageLeftPadding
        {
            get { return coreItem.ImageLeftPadding; }
            set
            {
                coreItem.ImageLeftPadding = value;
                RaisePropertyChanged(()=>ImageLeftPadding);
                LazyRenderPreview();
            }
        }

        public float ImageRightPadding
        {
            get { return coreItem.ImageRightPadding; }
            set
            {
                coreItem.ImageRightPadding = value;
                RaisePropertyChanged(()=>ImageRightPadding);
                LazyRenderPreview();
            }
        }

        public LegendImageJustificationMode ImageJustificationMode
        {
            get { return coreItem.ImageJustificationMode; }
            set
            {
                coreItem.ImageJustificationMode = value;
                RaisePropertyChanged(()=>ImageJustificationMode);
                LazyRenderPreview();
            }
        }

        #endregion for symbol

        #region for boundingbox

        public AreaStyle BackgroundMask
        {
            get { return coreItem.BackgroundMask; }
            set
            {
                coreItem.BackgroundMask = value;
                RaisePropertyChanged(()=>BackgroundMask);
                LazyRenderPreview();
            }
        }

        public float BottomPadding
        {
            get { return coreItem.BottomPadding; }
            set
            {
                coreItem.BottomPadding = value;
                RaisePropertyChanged(()=>BottomPadding);
                LazyRenderPreview();
            }
        }

        public float TopPadding
        {
            get { return coreItem.TopPadding; }
            set
            {
                coreItem.TopPadding = value;
                RaisePropertyChanged(()=>TopPadding);
                LazyRenderPreview();
            }
        }

        public float LeftPadding
        {
            get { return coreItem.LeftPadding; }
            set
            {
                coreItem.LeftPadding = value;
                RaisePropertyChanged(()=>LeftPadding);
                LazyRenderPreview();
            }
        }

        public float RightPadding
        {
            get { return coreItem.RightPadding; }
            set
            {
                coreItem.RightPadding = value;
                RaisePropertyChanged(()=>RightPadding);
                LazyRenderPreview();
            }
        }

        #endregion for boundingbox

        #region for label

        public string Text
        {
            get { return coreItem.TextStyle.TextColumnName; }
            set
            {
                coreItem.TextStyle.TextColumnName = value;
                RaisePropertyChanged(()=>Text);
                RaisePropertyChanged(()=>TextPreview);
                LazyRenderPreview();
            }
        }

        public string FontName
        {
            get { return notifiedGeoFont.FontName; }
            set
            {
                notifiedGeoFont.FontName = value;
                RaisePropertyChanged(()=>FontName);
                LazyRenderPreview();
            }
        }

        public bool IsUnderline
        {
            get { return notifiedGeoFont.IsUnderline; }
            set
            {
                notifiedGeoFont.IsUnderline = value;
                RaisePropertyChanged(()=>IsUnderline);
                LazyRenderPreview();
            }
        }

        public bool IsStrike
        {
            get { return notifiedGeoFont.IsStrike; }
            set
            {
                notifiedGeoFont.IsStrike = value;
                RaisePropertyChanged(()=>IsStrike);
                LazyRenderPreview();
            }
        }

        public bool IsItalic
        {
            get { return notifiedGeoFont.IsItalic; }
            set
            {
                notifiedGeoFont.IsItalic = value;
                RaisePropertyChanged(()=>IsItalic);
                LazyRenderPreview();
            }
        }

        public bool IsBold
        {
            get { return notifiedGeoFont.IsBold; }
            set
            {
                notifiedGeoFont.IsBold = value;
                RaisePropertyChanged(()=>IsBold);
                LazyRenderPreview();
            }
        }

        public int FontSize
        {
            get { return notifiedGeoFont.FontSize; }
            set
            {
                notifiedGeoFont.FontSize = value;
                RaisePropertyChanged(()=>FontSize);
                LazyRenderPreview();
            }
        }

        public GeoFontViewModel NotifiedGeoFont
        {
            get { return notifiedGeoFont; }
            set
            {
                notifiedGeoFont = value;
                RaisePropertyChanged(()=>NotifiedGeoFont);
                LazyRenderPreview();
            }
        }

        public GeoSolidBrush TextSolidBrush
        {
            get { return coreItem.TextStyle.TextSolidBrush; }
            set
            {
                coreItem.TextStyle.TextSolidBrush = value;
                RaisePropertyChanged(()=>TextSolidBrush);
                LazyRenderPreview();
            }
        }

        public float Width
        {
            get { return coreItem.Width; }
            set
            {
                coreItem.Width = value;
                RaisePropertyChanged(()=>Width);
                LazyRenderPreview();
            }
        }

        public float Height
        {
            get { return coreItem.Height; }
            set
            {
                coreItem.Height = value;
                RaisePropertyChanged(()=>Height);
                LazyRenderPreview();
            }
        }

        public float ImageWidth
        {
            get { return coreItem.ImageWidth; }
            set
            {
                coreItem.ImageWidth = value;
                RaisePropertyChanged(()=>ImageWidth);
                LazyRenderPreview();
            }
        }

        public float ImageHeight
        {
            get { return coreItem.ImageHeight; }
            set
            {
                coreItem.ImageHeight = value;
                RaisePropertyChanged(()=>ImageHeight);
                LazyRenderPreview();
            }
        }

        public AreaStyle TextMask
        {
            get { return coreItem.TextMask; }
            set
            {
                coreItem.TextMask = value;
                RaisePropertyChanged(()=>TextMask);
                LazyRenderPreview();
            }
        }

        public float TextLeftPadding
        {
            get { return coreItem.TextLeftPadding; }
            set
            {
                coreItem.TextLeftPadding = value;
                RaisePropertyChanged(()=>TextLeftPadding);
                LazyRenderPreview();
            }
        }

        public float TextRightPadding
        {
            get { return coreItem.TextRightPadding; }
            set
            {
                coreItem.TextRightPadding = value;
                RaisePropertyChanged(()=>TextRightPadding);
                LazyRenderPreview();
            }
        }

        public float TextTopPadding
        {
            get { return coreItem.TextTopPadding; }
            set
            {
                coreItem.TextTopPadding = value;
                RaisePropertyChanged(()=>TextTopPadding);
                LazyRenderPreview();
            }
        }

        public float TextBottomPadding
        {
            get { return coreItem.TextBottomPadding; }
            set
            {
                coreItem.TextBottomPadding = value;
                RaisePropertyChanged(()=>TextBottomPadding);
                LazyRenderPreview();
            }
        }

        #endregion for label

        public BitmapImage PreviewSource
        {
            get
            {
                return GetImageSource();
            }
        }

        public LegendAdornmentLayerViewModel Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        protected void LazyRenderPreview()
        {
            if (delayRenderTimer.IsEnabled) delayRenderTimer.Stop();
            delayRenderTimer.Start();
        }

        private IEnumerable<LegendAdornmentLayerViewModel> CollectNotifiedLegendLayers()
        {
            if (!Parent.LegendItems.Contains(this))
            {
                Parent.LegendItems.Add(this);
            }

            return Parent.CollectNotifiedLegendLayers();
        }

        private BitmapImage GetImageSource()
        {
            MemoryStream streamSource = null;
            if (Width != 0 && Height != 0)
            {
                PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas
                {
                    CompositingQuality = CompositingQuality.HighSpeed,
                    SmoothingMode = SmoothingMode.HighSpeed,
                    DrawingQuality = DrawingQuality.CanvasSettings,
                };
                SizeF measuredSize = Measure(geoCanvas);
                Bitmap nativeImage = new Bitmap((int)(measuredSize.Width + LeftPadding + RightPadding + 1), (int)(measuredSize.Height + TopPadding + BottomPadding + 1));
                geoCanvas.BeginDrawing(nativeImage, new RectangleShape(-10, 10, 10, -10), GeographyUnit.Meter);
                simpleCandidates.Clear();

                LegendItem newLegendItem = ToLegendItem();
                newLegendItem.Width = measuredSize.Width;
                newLegendItem.Height = measuredSize.Height;
                //newLegendItem.Draw(0, 0, 100, geoCanvas, simpleCandidates);
                newLegendItem.Draw(geoCanvas, simpleCandidates, new LegendDrawingParameters { XOffset = 0, YOffset = 0 });
                geoCanvas.EndDrawing();

                streamSource = new MemoryStream();
                nativeImage.Save(streamSource, ImageFormat.Png);
            }

            BitmapImage previewSource = new BitmapImage();
            if (streamSource != null)
            {
                previewSource.BeginInit();
                previewSource.StreamSource = streamSource;
                previewSource.EndInit();
                previewSource.Freeze();
            }

            return previewSource;
        }

        public LegendItemViewModel Clone()
        {
            LegendItemViewModel newItem = new LegendItemViewModel
            {
                NotifiedGeoFont = NotifiedGeoFont.Clone(),
                BackgroundMask = BackgroundMask == null ? null : (AreaStyle)BackgroundMask.CloneDeep(),
                BottomPadding = BottomPadding,
                Height = Height,
                ImageBottomPadding = ImageBottomPadding,
                ImageHeight = ImageHeight,
                ImageJustificationMode = ImageJustificationMode,
                ImageLeftPadding = ImageLeftPadding,
                ImageMask = ImageMask == null ? null : (AreaStyle)ImageMask.CloneDeep(),
                ImageRightPadding = ImageRightPadding,
                //ImageStyle = ImageStyle.CloneDeep(),
                ImageTopPadding = ImageTopPadding,
                ImageWidth = ImageWidth,
                LeftPadding = LeftPadding,
                LegendItemType = LegendItemType,
                TextBottomPadding = TextBottomPadding,
                TextLeftPadding = TextLeftPadding,
                TextMask = TextMask == null ? null : (AreaStyle)TextMask.CloneDeep(),
                TextRightPadding = TextRightPadding,
                TextSolidBrush = TextSolidBrush,
                TextTopPadding = TextTopPadding,
                Text = this.Text,
                TopPadding = TopPadding,
                Width = Width,
                Parent = Parent
            };

            if (ImageStyle != null)
                newItem.ImageStyle = ImageStyle.CloneDeep();

            return newItem;
        }

        private GeoFontViewModel ConvertToNotifiedGeoFont(GeoFont geoFont)
        {
            GeoFontViewModel newFont = new GeoFontViewModel();
            newFont.FromGeoFont(geoFont);
            return newFont;
        }

        public LegendItem ToLegendItem()
        {
            this.coreItem.TextStyle.Font = NotifiedGeoFont.ToGeoFont();

            LegendItem newLegendItem = new LegendItem((int)Width, (int)Height, ImageWidth, ImageHeight, ImageStyle,
                new TextStyle(Text, NotifiedGeoFont.ToGeoFont(), TextSolidBrush));
            newLegendItem.BackgroundMask = BackgroundMask;
            newLegendItem.BottomPadding = BottomPadding;
            newLegendItem.ImageBottomPadding = ImageBottomPadding;
            newLegendItem.ImageJustificationMode = ImageJustificationMode;
            newLegendItem.ImageLeftPadding = ImageLeftPadding;
            newLegendItem.ImageStyle = ImageStyle;
            newLegendItem.ImageMask = ImageMask;
            newLegendItem.ImageRightPadding = ImageRightPadding;
            newLegendItem.ImageTopPadding = ImageTopPadding;
            newLegendItem.LeftPadding = LeftPadding;
            newLegendItem.RightPadding = RightPadding;
            newLegendItem.TextBottomPadding = TextBottomPadding;
            newLegendItem.TextLeftPadding = TextLeftPadding;
            newLegendItem.TextMask = TextMask;
            newLegendItem.TextRightPadding = TextRightPadding;
            newLegendItem.TextTopPadding = TextTopPadding;
            newLegendItem.TopPadding = TopPadding;

            if (this.ImageStyle == null)
            {
                newLegendItem.ImageStyle = new PointStyle();
            }

            return newLegendItem;
        }

        public SizeF Measure(GeoCanvas geoCanvas)
        {
            DrawingRectangleF rect;
            try
            {
                rect = geoCanvas.MeasureText(Text, NotifiedGeoFont.ToGeoFont());
            }
            catch(Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                if (string.IsNullOrEmpty(Text.Trim())) rect = geoCanvas.MeasureText("a", new GeoFont("ARIAL", 10, NotifiedGeoFont.FontStyles));
                else rect = geoCanvas.MeasureText(Text, new GeoFont("ARIAL", NotifiedGeoFont.FontSize, NotifiedGeoFont.FontStyles));
            }

            float width = rect.Width;
            width += ImageLeftPadding;
            width += ImageWidth;
            width += ImageRightPadding;
            width += TextLeftPadding;
            width += TextRightPadding;

            float imageHeight = ImageTopPadding + ImageHeight + ImageBottomPadding;
            float textHeight = TextTopPadding + rect.Height + TextBottomPadding;
            float height = imageHeight > textHeight ? imageHeight : textHeight;

            return new SizeF(width, height);
        }

        private void InitLazyRenderTimer()
        {
            this.delayRenderTimer = new DispatcherTimer();
            this.delayRenderTimer.Interval = TimeSpan.FromMilliseconds(400);
            this.delayRenderTimer.Tick += (s, e) => { RaisePropertyChanged(()=>PreviewSource); ((DispatcherTimer)s).Stop(); };
        }
    }
}